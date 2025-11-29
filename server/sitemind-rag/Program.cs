using sitemind_shared.Extensions;
using sitemind_shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SiteMind DbContext
builder.Services.AddSiteMindDbContext(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseHttpsRedirection();

// Add Organization Middleware for Multi-Tenancy
app.UseMiddleware<OrganizationMiddleware>();

app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", service = "sitemind-rag" }))
    .WithName("HealthCheck");

app.Run();
