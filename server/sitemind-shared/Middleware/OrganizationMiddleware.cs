using System.Text.Json;
using Microsoft.AspNetCore.Http;
using sitemind_shared.Abstractions;

namespace sitemind_shared.Middleware;

public class OrganizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HashSet<string> _whitelistedPaths;

    public OrganizationMiddleware(RequestDelegate next)
    {
        _next = next;
        _whitelistedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "/api/auth/login",
            "/api/auth/register",
            "/health",
            "/healthz",
            "/api/health"
        };
    }

    public async Task InvokeAsync(HttpContext context, IOrganizationProvider organizationProvider)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        // Skip validation for whitelisted paths
        if (_whitelistedPaths.Any(whitelisted => path.StartsWith(whitelisted, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        // Allow POST to /api/organizations (create organization - public endpoint)
        if ((path.Equals("/api/organizations", StringComparison.OrdinalIgnoreCase) || 
             path.Equals("/api/organizations/", StringComparison.OrdinalIgnoreCase)) && 
            method.Equals("POST", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        try
        {
            Guid organizationId = Guid.Empty;
            bool found = false;
            
            // Try to get from header - ASP.NET Core headers are case-insensitive
            if (context.Request.Headers.ContainsKey("X-Organization-Id"))
            {
                var headerValue = context.Request.Headers["X-Organization-Id"].ToString();
                if (!string.IsNullOrWhiteSpace(headerValue) && Guid.TryParse(headerValue, out var orgId))
                {
                    organizationId = orgId;
                    found = true;
                }
            }
            
            // If not found in header, try JWT claim
            if (!found && context.User?.FindFirst("organization_id")?.Value is string orgIdClaim)
            {
                if (Guid.TryParse(orgIdClaim, out var orgIdFromClaim))
                {
                    organizationId = orgIdFromClaim;
                    found = true;
                }
            }
            
            // If still not found, try OrganizationProvider as fallback
            if (!found)
            {
                organizationId = organizationProvider.GetOrganizationId();
                found = true;
            }
            
            if (!found || organizationId == Guid.Empty)
            {
                throw new InvalidOperationException("OrganizationId not found in request headers or JWT claims.");
            }
            
            // Store in context items for potential future use
            context.Items["OrganizationId"] = organizationId;
            
            await _next(context);
        }
        catch (InvalidOperationException)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/json";
            var errorResponse = new { error = "OrganizationId is required. Please provide X-Organization-Id header or valid JWT token with organization_id claim." };
            var json = JsonSerializer.Serialize(errorResponse);
            await context.Response.WriteAsync(json);
            return;
        }
    }
}
