using Hangfire;
using Hangfire.PostgreSql;
using sitemind_ingestion.Filters;
using sitemind_ingestion.Jobs;
using sitemind_ingestion.Services;
using sitemind_shared.Extensions;
using sitemind_shared.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Add SiteMind DbContext
builder.Services.AddSiteMindDbContext(builder.Configuration);

// Add Hangfire
var hangfireConnectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddHangfire(config =>
{
    config.UsePostgreSqlStorage(hangfireConnectionString, new PostgreSqlStorageOptions
    {
        SchemaName = "hangfire"
    });
});

builder.Services.AddHangfireServer();

// Add HttpClient for N8nClient
builder.Services.AddHttpClient<IN8nClient, N8nClient>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Register N8nClient
builder.Services.AddScoped<IN8nClient, N8nClient>();

// Register Ingestion Job
builder.Services.AddScoped<IngestWebsiteJob>();

var app = builder.Build();

// Configure the HTTP request pipeline
// Note: UseHttpsRedirection removed for testing - can cause header issues
// app.UseHttpsRedirection();

// Add Organization Middleware for Multi-Tenancy (must be early in pipeline)
app.UseMiddleware<OrganizationMiddleware>();

// Add Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

app.UseAuthorization();
app.MapControllers();

app.Run();
