using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sitemind_ingestion.Services;
using sitemind_shared.Data;
using sitemind_shared.Entities;
using sitemind_shared.Enums;

namespace sitemind_ingestion.Jobs;

public class FetchUrlsJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<FetchUrlsJob> _logger;

    public FetchUrlsJob(
        IServiceProvider serviceProvider,
        ILogger<FetchUrlsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting FetchUrlsJob to discover URLs for websites");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SiteMindDbContext>();
        var n8nClient = scope.ServiceProvider.GetRequiredService<IN8nClient>();

        var now = DateTime.UtcNow;
        
        // Find websites that need processing:
        // 1. Created status (new websites)
        // 2. Error status: 
        //    - If LastCrawledAt is null, retry immediately (never processed before)
        //    - If LastCrawledAt exists, retry after 1 hour
        // 3. Stuck Crawling status:
        //    - If LastCrawledAt is null, retry immediately (stuck from start)
        //    - If LastCrawledAt exists, retry after 30 minutes
        // IgnoreQueryFilters() kullan çünkü job'da HttpContext yok ve tüm organization'ları işlememiz gerekiyor
        var websitesToProcess = await dbContext.Websites
            .IgnoreQueryFilters()
            .Where(w => 
                w.Status == WebsiteStatus.Created 
                || (w.Status == WebsiteStatus.Error 
                    && (w.LastCrawledAt == null || w.LastCrawledAt.Value < now.AddHours(0)))
                || (w.Status == WebsiteStatus.Crawling 
                    && (w.LastCrawledAt == null || w.LastCrawledAt.Value < now.AddMinutes(-30)))
            )
            .ToListAsync();

        if (!websitesToProcess.Any())
        {
            _logger.LogInformation("No websites found that need URL fetching. Skipping.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} website(s) to process (Created, Error retry, or Stuck Crawling). Processing...",
            websitesToProcess.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var website in websitesToProcess)
        {
            try
            {
                var previousStatus = website.Status;
                _logger.LogInformation(
                    "Fetching URLs for website. WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}, Url: {Url}, PreviousStatus: {PreviousStatus}",
                    website.Id,
                    website.OrganizationId,
                    website.BaseUrl,
                    previousStatus);

                // Update website status to Crawling and set LastCrawledAt for retry tracking
                website.Status = WebsiteStatus.Crawling;
                website.LastCrawledAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();

                // Fetch URLs from n8n
                var urls = await n8nClient.FetchWebsiteUrlsAsync(website.BaseUrl);

                if (urls == null || !urls.Any())
                {
                    _logger.LogWarning(
                        "No URLs found for website. WebsiteId: {WebsiteId}",
                        website.Id);
                    website.Status = WebsiteStatus.Error;
                    website.LastCrawledAt = DateTime.UtcNow; // Update for retry mechanism
                    await dbContext.SaveChangesAsync();
                    errorCount++;
                    continue;
                }

                _logger.LogInformation(
                    "Fetched {Count} URLs for website. WebsiteId: {WebsiteId}",
                    urls.Count,
                    website.Id);

                // Create Page entities for each URL
                var existingUrls = await dbContext.Pages
                    .IgnoreQueryFilters()
                    .Where(p => p.WebsiteId == website.Id)
                    .Select(p => p.Url)
                    .ToListAsync();

                var newPages = new List<Page>();
                foreach (var url in urls)
                {
                    // Skip if page already exists
                    if (existingUrls.Contains(url))
                    {
                        continue;
                    }

                    var page = new Page
                    {
                        WebsiteId = website.Id,
                        Url = url,
                        VectorStatus = VectorStatus.Pending
                    };

                    newPages.Add(page);
                }

                if (newPages.Any())
                {
                    await dbContext.Pages.AddRangeAsync(newPages);
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "Created {Count} new pages for website. WebsiteId: {WebsiteId}",
                        newPages.Count,
                        website.Id);
                }
                else
                {
                    _logger.LogInformation(
                        "All URLs already exist as pages. WebsiteId: {WebsiteId}",
                        website.Id);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(
                    ex,
                    "Error fetching URLs for website. WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}",
                    website.Id,
                    website.OrganizationId);

                // Update website status to Error and set LastCrawledAt for retry mechanism
                try
                {
                    var websiteToUpdate = await dbContext.Websites
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(w => w.Id == website.Id && w.OrganizationId == website.OrganizationId);

                    if (websiteToUpdate != null)
                    {
                        websiteToUpdate.Status = WebsiteStatus.Error;
                        websiteToUpdate.LastCrawledAt = DateTime.UtcNow; // Update for retry mechanism
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(
                        updateEx,
                        "Failed to update website status to Error. WebsiteId: {WebsiteId}",
                        website.Id);
                }
            }
        }

        _logger.LogInformation(
            "FetchUrlsJob completed. Success: {SuccessCount}, Errors: {ErrorCount}",
            successCount,
            errorCount);
    }
}

