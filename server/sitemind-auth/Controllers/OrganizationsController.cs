using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using sitemind_shared.Data;
using sitemind_shared.Entities;
using sitemind_shared.Abstractions;

namespace sitemind_auth.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrganizationsController : ControllerBase
{
    private readonly SiteMindDbContext _context;
    private readonly IOrganizationProvider _organizationProvider;
    private readonly IConfiguration _configuration;

    public OrganizationsController(
        SiteMindDbContext context,
        IOrganizationProvider organizationProvider,
        IConfiguration configuration)
    {
        _context = context;
        _organizationProvider = organizationProvider;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<ActionResult<Organization>> GetCurrentOrganization()
    {
        var organizationId = _organizationProvider.GetOrganizationId();
        
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == organizationId);

        if (organization == null)
        {
            return NotFound(new { error = "Organization not found" });
        }

        return Ok(organization);
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<Organization>>> GetAvailableOrganizations()
    {
        // Get current user from JWT claim
        var username = User?.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { error = "User not found" });
        }

        // Find user by username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Get all organizations where user is a member through UserOrganizationMemberships
        var organizations = await _context.UserOrganizationMemberships
            .Where(m => m.UserId == user.Id)
            .Select(m => m.Organization!)
            .OrderByDescending(o => o.CreatedAt)
            .AsNoTracking()
            .ToListAsync();

        return Ok(organizations);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Organization>> GetOrganization(Guid id)
    {
        var organizationId = _organizationProvider.GetOrganizationId();
        
        // Users can only access their own organization
        if (id != organizationId)
        {
            return Forbid();
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
        {
            return NotFound();
        }

        return Ok(organization);
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<Organization>> CreateOrganization(CreateOrganizationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Organization name is required" });
        }

        // Check if organization name already exists
        var existingOrganization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Name == request.Name);

        if (existingOrganization != null)
        {
            return BadRequest(new { error = "Organization name already exists" });
        }

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            CreatedAt = DateTime.UtcNow
        };

        _context.Organizations.Add(organization);
        await _context.SaveChangesAsync();

        // If user is authenticated, add them as admin to the new organization
        var username = User?.Identity?.Name;
        if (!string.IsNullOrEmpty(username))
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username);

            if (user != null)
            {
                var membership = new UserOrganizationMembership
                {
                    UserId = user.Id,
                    OrganizationId = organization.Id,
                    Role = "admin",
                    JoinedAt = DateTime.UtcNow
                };

                _context.UserOrganizationMemberships.Add(membership);
                await _context.SaveChangesAsync();
            }
        }

        return CreatedAtAction(nameof(GetOrganization), new { id = organization.Id }, organization);
    }

    [HttpPost("switch")]
    public async Task<ActionResult<object>> SwitchOrganization(SwitchOrganizationRequest request)
    {
        // Verify the new organization exists
        var newOrganization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == request.OrganizationId);

        if (newOrganization == null)
        {
            return NotFound(new { error = "Organization not found" });
        }

        // Get current user from JWT claim
        var username = User?.Identity?.Name;
        if (string.IsNullOrEmpty(username))
        {
            return Unauthorized(new { error = "User not found" });
        }

        // Find user by username
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Check if user is already a member of the organization
        var existingMembership = await _context.UserOrganizationMemberships
            .FirstOrDefaultAsync(m => m.UserId == user.Id && m.OrganizationId == request.OrganizationId);

        if (existingMembership == null)
        {
            // User is not a member, create membership with default role "member"
            var membership = new UserOrganizationMembership
            {
                UserId = user.Id,
                OrganizationId = request.OrganizationId,
                Role = "member",
                JoinedAt = DateTime.UtcNow
            };

            _context.UserOrganizationMemberships.Add(membership);
            await _context.SaveChangesAsync();
        }

        // Generate new JWT token with new organization ID
        var jwtSettings = _configuration.GetSection("Jwt");
        var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey is not configured");
        var issuer = jwtSettings["Issuer"] ?? "sitemind-auth";
        var audience = jwtSettings["Audience"] ?? "sitemind-api";
        var expirationMinutes = int.Parse(jwtSettings["ExpirationMinutes"] ?? "60");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(secretKey);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("organization_id", request.OrganizationId.ToString()),
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
        var newToken = tokenHandler.WriteToken(token);

        return Ok(new
        {
            message = "Organization switched successfully",
            token = newToken,
            organizationId = request.OrganizationId.ToString(),
            expires_in = expirationMinutes * 60,
            organization = newOrganization
        });
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Organization>> UpdateOrganization(Guid id, UpdateOrganizationRequest request)
    {
        var organizationId = _organizationProvider.GetOrganizationId();
        
        // Users can only update their own organization
        if (id != organizationId)
        {
            return Forbid();
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
        {
            return NotFound();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Organization name is required" });
        }

        // Check if new name already exists (excluding current organization)
        var existingOrganization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Name == request.Name && o.Id != id);

        if (existingOrganization != null)
        {
            return BadRequest(new { error = "Organization name already exists" });
        }

        organization.Name = request.Name;
        organization.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(organization);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteOrganization(Guid id)
    {
        var organizationId = _organizationProvider.GetOrganizationId();
        
        // Users can only delete their own organization
        if (id != organizationId)
        {
            return Forbid();
        }

        var organization = await _context.Organizations
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
        {
            return NotFound();
        }

        // Check if organization has memberships or websites
        var hasMemberships = await _context.UserOrganizationMemberships.AnyAsync(m => m.OrganizationId == id);
        var hasWebsites = await _context.Websites.AnyAsync(w => w.OrganizationId == id);

        if (hasMemberships || hasWebsites)
        {
            return BadRequest(new { error = "Cannot delete organization with existing members or websites" });
        }

        _context.Organizations.Remove(organization);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateOrganizationRequest(string Name);
public record UpdateOrganizationRequest(string Name);
public record SwitchOrganizationRequest(Guid OrganizationId);

