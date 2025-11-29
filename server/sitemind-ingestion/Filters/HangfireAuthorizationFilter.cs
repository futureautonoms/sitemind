using Hangfire.Dashboard;

namespace sitemind_ingestion.Filters;

public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        // For development, allow all access
        // In production, implement proper authentication/authorization here
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return environment == "Development" || environment == null;
    }
}

