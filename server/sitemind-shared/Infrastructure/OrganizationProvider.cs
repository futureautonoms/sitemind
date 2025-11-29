using Microsoft.AspNetCore.Http;
using sitemind_shared.Abstractions;

namespace sitemind_shared.Infrastructure;

public class OrganizationProvider : IOrganizationProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private Guid? _cachedOrganizationId;

    public OrganizationProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetOrganizationId()
    {
        if (_cachedOrganizationId.HasValue)
        {
            return _cachedOrganizationId.Value;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            throw new InvalidOperationException("HttpContext is not available.");
        }

        // First, try to get from header
        if (httpContext.Request.Headers.TryGetValue("X-Organization-Id", out var orgIdHeader))
        {
            if (Guid.TryParse(orgIdHeader.ToString(), out var orgId))
            {
                _cachedOrganizationId = orgId;
                return orgId;
            }
        }

        // Then, try to get from JWT claim (for future use)
        var organizationIdClaim = httpContext.User?.FindFirst("organization_id")?.Value;
        if (!string.IsNullOrEmpty(organizationIdClaim) && Guid.TryParse(organizationIdClaim, out var orgIdFromClaim))
        {
            _cachedOrganizationId = orgIdFromClaim;
            return orgIdFromClaim;
        }

        throw new InvalidOperationException("OrganizationId not found in request headers or JWT claims.");
    }
}

