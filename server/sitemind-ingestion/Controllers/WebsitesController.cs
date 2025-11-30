using Hangfire;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using sitemind_ingestion.Jobs;
using sitemind_shared.Abstractions;
using sitemind_shared.Data;
using sitemind_shared.Entities;
using sitemind_shared.Enums;

namespace sitemind_ingestion.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WebsitesController : ControllerBase
{
    private readonly SiteMindDbContext _context;
    private readonly IOrganizationProvider _organizationProvider;

    public WebsitesController(
        SiteMindDbContext context,
        IOrganizationProvider organizationProvider)
    {
        _context = context;
        _organizationProvider = organizationProvider;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Website>>> GetWebsites()
    {
        // Global Query Filter automatically filters by OrganizationId
        var websites = await _context.Websites.ToListAsync();
        return Ok(websites);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Website>> GetWebsite(Guid id)
    {
        // Global Query Filter automatically filters by OrganizationId
        var website = await _context.Websites.FindAsync(id);

        if (website == null)
        {
            return NotFound();
        }

        return Ok(website);
    }

    [HttpPost]
    public async Task<ActionResult<Website>> CreateWebsite(CreateWebsiteRequest request)
    {
        var organizationId = _organizationProvider.GetOrganizationId();

        var website = new Website
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            BaseUrl = request.BaseUrl,
            Status = WebsiteStatus.Created,
            CreatedAt = DateTime.UtcNow
            // OrganizationId will be automatically set by SaveChanges override
        };

        _context.Websites.Add(website);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetWebsite), new { id = website.Id }, website);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<Website>> UpdateWebsite(Guid id, UpdateWebsiteRequest request)
    {
        var website = await _context.Websites.FindAsync(id);

        if (website == null)
        {
            return NotFound();
        }

        website.Name = request.Name;
        website.BaseUrl = request.BaseUrl;

        await _context.SaveChangesAsync();

        return Ok(website);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebsite(Guid id)
    {
        var website = await _context.Websites.FindAsync(id);

        if (website == null)
        {
            return NotFound();
        }

        _context.Websites.Remove(website);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public record CreateWebsiteRequest(string Name, string BaseUrl);
public record UpdateWebsiteRequest(string Name, string BaseUrl);

