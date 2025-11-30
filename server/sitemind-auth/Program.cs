using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using sitemind_shared.Data;
using sitemind_shared.Entities;
using sitemind_shared.Extensions;
using sitemind_shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });
builder.Services.AddEndpointsApiExplorer();

// Add SiteMind DbContext
builder.Services.AddSiteMindDbContext(builder.Configuration);

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
var issuer = jwtSettings["Issuer"] ?? "sitemind-auth";
var audience = jwtSettings["Audience"] ?? "sitemind-api";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:5173", 
                "http://localhost:3000", 
                "http://localhost:8080",
                "https://sitemind.futureautonoms.com",
                "http://sitemind.futureautonoms.com"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Apply database migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<SiteMindDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline
app.UseCors("AllowFrontend");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Add Organization Middleware for Multi-Tenancy (after authentication to read JWT claims)
app.UseMiddleware<OrganizationMiddleware>();

app.MapControllers();

// Helper method to generate JWT token
string GenerateJwtToken(string username, string organizationId, IConfiguration configuration)
{
    var jwtSettings = configuration.GetSection("Jwt");
    var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
    var issuer = jwtSettings["Issuer"] ?? "sitemind-auth";
    var audience = jwtSettings["Audience"] ?? "sitemind-api";
    var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes(secretKey);

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, username),
        new Claim("organization_id", organizationId),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
    };

    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(claims),
        Expires = DateTime.UtcNow.AddMinutes(expirationMinutes),
        Issuer = issuer,
        Audience = audience,
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    var token = tokenHandler.CreateToken(tokenDescriptor);
    return tokenHandler.WriteToken(token);
}

// Register endpoint
app.MapPost("/api/auth/register", async (RegisterRequest request, SiteMindDbContext context, IConfiguration configuration) =>
{
    // Validation
    if (string.IsNullOrWhiteSpace(request.Email) || 
        string.IsNullOrWhiteSpace(request.Username) || 
        string.IsNullOrWhiteSpace(request.Password) ||
        string.IsNullOrWhiteSpace(request.OrganizationName))
    {
        return Results.BadRequest(new { error = "Email, username, password, and organization name are required" });
    }

    if (request.Password.Length < 6)
    {
        return Results.BadRequest(new { error = "Password must be at least 6 characters long" });
    }

    // Create new Organization
    var organization = new Organization
    {
        Id = Guid.NewGuid(),
        Name = request.OrganizationName,
        CreatedAt = DateTime.UtcNow
    };
    
    context.Organizations.Add(organization);
    var organizationId = organization.Id;

    // Check if email or username already exists (globally unique now)
    var existingUser = await context.Users
        .FirstOrDefaultAsync(u => u.Email == request.Email || u.Username == request.Username);

    if (existingUser != null)
    {
        return Results.BadRequest(new { error = "Email or username already exists" });
    }

    // Hash password
    var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

    // Create user (without OrganizationId)
    var user = new User
    {
        Id = Guid.NewGuid(),
        Email = request.Email,
        Username = request.Username,
        PasswordHash = passwordHash,
        IsActive = true,
        CreatedAt = DateTime.UtcNow
    };

    context.Users.Add(user);
    await context.SaveChangesAsync();

    // Create UserOrganizationMembership with admin role
    var membership = new UserOrganizationMembership
    {
        UserId = user.Id,
        OrganizationId = organizationId,
        Role = "admin",
        JoinedAt = DateTime.UtcNow
    };

    context.UserOrganizationMemberships.Add(membership);
    await context.SaveChangesAsync();

    // Generate JWT token
    var token = GenerateJwtToken(user.Username, organizationId.ToString(), configuration);
    var expirationMinutes = int.Parse(configuration.GetSection("Jwt")["ExpirationMinutes"] ?? "60");

    return Results.Ok(new
    {
        token = token,
        organization_id = organizationId.ToString(),
        expires_in = expirationMinutes * 60,
        user = new
        {
            id = user.Id,
            email = user.Email,
            username = user.Username
        }
    });
})
.WithName("Register")
.WithOpenApi();

// Login endpoint (updated with database authentication)
app.MapPost("/api/auth/login", async (LoginRequest request, SiteMindDbContext context, IConfiguration configuration) =>
{
    // Validation
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required" });
    }

    // Find user by username or email
    var user = await context.Users
        .FirstOrDefaultAsync(u => u.Username == request.Username || u.Email == request.Username);

    if (user == null)
    {
        return Results.Unauthorized();
    }

    // Check if user is active
    if (!user.IsActive)
    {
        return Results.Unauthorized();
    }

    // Verify password
    if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
    {
        return Results.Unauthorized();
    }

    // Get user's first organization membership (or default organization)
    var membership = await context.UserOrganizationMemberships
        .Where(m => m.UserId == user.Id)
        .OrderBy(m => m.JoinedAt)
        .FirstOrDefaultAsync();

    if (membership == null)
    {
        return Results.BadRequest(new { error = "User is not a member of any organization" });
    }

    // Generate JWT token with organization ID from membership
    var token = GenerateJwtToken(user.Username, membership.OrganizationId.ToString(), configuration);
    var expirationMinutes = int.Parse(configuration.GetSection("Jwt")["ExpirationMinutes"] ?? "60");

    return Results.Ok(new
    {
        token = token,
        organization_id = membership.OrganizationId.ToString(),
        expires_in = expirationMinutes * 60,
        user = new
        {
            id = user.Id,
            email = user.Email,
            username = user.Username
        }
    });
})
.WithName("Login")
.WithOpenApi();

app.Run();

record LoginRequest(string Username, string Password);
record RegisterRequest(string Email, string Username, string Password, string OrganizationName);
