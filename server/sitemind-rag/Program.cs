using sitemind_shared.Extensions;
using sitemind_shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SiteMind DbContext
builder.Services.AddSiteMindDbContext(builder.Configuration);

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

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Configure CORS
app.UseCors("AllowFrontend");

// Add Organization Middleware for Multi-Tenancy
app.UseMiddleware<OrganizationMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "sitemind-rag" }))
    .WithName("HealthCheck");

app.Run();
