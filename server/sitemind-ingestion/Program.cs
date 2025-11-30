using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sitemind_ingestion.Filters;
using sitemind_ingestion.Jobs;
using sitemind_ingestion.Services;
using sitemind_shared.Data;
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

// Register Ingestion Jobs
builder.Services.AddScoped<FetchUrlsJob>();
builder.Services.AddScoped<ScrapePagesJob>();
builder.Services.AddScoped<ProcessPagesJob>();
builder.Services.AddScoped<IngestPagesJob>();

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
        var migrationLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        migrationLogger.LogError(ex, "An error occurred while migrating the database.");
    }
}

// Configure the HTTP request pipeline
// Note: UseHttpsRedirection removed for testing - can cause header issues
// app.UseHttpsRedirection();

// Configure CORS
app.UseCors("AllowFrontend");

// Add Organization Middleware for Multi-Tenancy (must be early in pipeline)
app.UseMiddleware<OrganizationMiddleware>();

// Add Hangfire Dashboard
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = new[] { new HangfireAuthorizationFilter() }
});

// Setup recurring jobs for website ingestion pipeline
// Wrap in try-catch to handle distributed lock timeouts when multiple instances are running
var logger = app.Services.GetRequiredService<ILogger<Program>>();

try
{
    // FetchUrlsJob: Runs every 5 minutes to discover URLs for Created websites
    RecurringJob.AddOrUpdate<FetchUrlsJob>(
        "fetch-urls",
        job => job.ExecuteAsync(),
        "*/10 * * * *"); // Every 1 minute

    logger.LogInformation("Recurring job 'fetch-urls' registered successfully");
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to register recurring job 'fetch-urls'. Another instance may have already registered it.");
}

RecurringJob.TriggerJob("fetch-urls");

try
{
    // ScrapePagesJob: Runs every 1 minute to scrape content for Pending pages
    RecurringJob.AddOrUpdate<ScrapePagesJob>(
        "scrape-pages",
        job => job.ExecuteAsync(),
        "*/1 * * * *"); // Every 1 minute

    logger.LogInformation("Recurring job 'scrape-pages' registered successfully");
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to register recurring job 'scrape-pages'. Another instance may have already registered it.");
}

try
{
    // ProcessPagesJob: Runs every 1 minute to process scraped content with AI
    RecurringJob.AddOrUpdate<ProcessPagesJob>(
        "process-pages",
        job => job.ExecuteAsync(),
        "*/1 * * * *"); // Every 1 minute

    logger.LogInformation("Recurring job 'process-pages' registered successfully");
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to register recurring job 'process-pages'. Another instance may have already registered it.");
}

try
{
    // IngestPagesJob: Runs every 1 minute to ingest processed pages into vector store
    RecurringJob.AddOrUpdate<IngestPagesJob>(
        "ingest-pages",
        job => job.ExecuteAsync(),
        "*/1 * * * *"); // Every 1 minute

    logger.LogInformation("Recurring job 'ingest-pages' registered successfully");
}
catch (Exception ex)
{
    logger.LogWarning(ex, "Failed to register recurring job 'ingest-pages'. Another instance may have already registered it.");
}

app.UseAuthorization();
app.MapControllers();

app.Run();
