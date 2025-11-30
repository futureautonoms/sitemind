using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sitemind_ingestion.Services;
using sitemind_shared.Data;
using sitemind_shared.Enums;
using System.Text.Json;

namespace sitemind_ingestion.Jobs;

public class ScrapePagesJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScrapePagesJob> _logger;

    public ScrapePagesJob(
        IServiceProvider serviceProvider,
        ILogger<ScrapePagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting ScrapePagesJob to scrape content for Pending pages");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SiteMindDbContext>();
        var n8nClient = scope.ServiceProvider.GetRequiredService<IN8nClient>();

        // Find pages that need scraping: Pending status and no RawContent
        // IgnoreQueryFilters() kullan çünkü job'da HttpContext yok ve tüm organization'ları işlememiz gerekiyor
        var pagesToScrape = await dbContext.Pages
            .IgnoreQueryFilters()
            .Where(p => p.VectorStatus == VectorStatus.Pending && p.RawContent == null)
            .Take(10) // Process 10 pages at a time to avoid Firecrawl rate limiting
            .ToListAsync();

        if (!pagesToScrape.Any())
        {
            _logger.LogInformation("No pages found that need scraping. Skipping.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} page(s) that need scraping. Processing...",
            pagesToScrape.Count);

        var successCount = 0;
        var errorCount = 0;

        for (int i = 0; i < pagesToScrape.Count; i++)
        {
            var page = pagesToScrape[i];
            
            try
            {
                // Add delay between requests to avoid Firecrawl rate limiting (except for first request)
                if (i > 0)
                {
                    _logger.LogInformation(
                        "Waiting 5 seconds before next scrape request to avoid rate limiting...");
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }

                _logger.LogInformation(
                    "Scraping page. PageId: {PageId}, WebsiteId: {WebsiteId}, Url: {Url}",
                    page.Id,
                    page.WebsiteId,
                    page.Url);

                // Update status to Processing
                page.VectorStatus = VectorStatus.Processing;
                await dbContext.SaveChangesAsync();

                // Scrape content from n8n
                var scrapeResponse = await n8nClient.ScrapeUrlAsync(page.Url);

                if (scrapeResponse == null)
                {
                    _logger.LogWarning(
                        "Scrape response is null for page. PageId: {PageId}",
                        page.Id);
                    page.VectorStatus = VectorStatus.Failed;
                    await dbContext.SaveChangesAsync();
                    errorCount++;
                    continue;
                }

                // Store raw content (HTML or markdown, whichever is available)
                if (!string.IsNullOrEmpty(scrapeResponse.MarkdownContent))
                {
                    page.RawContent = scrapeResponse.MarkdownContent;
                }
                else if (!string.IsNullOrEmpty(scrapeResponse.HtmlContent))
                {
                    page.RawContent = scrapeResponse.HtmlContent;
                }
                else
                {
                    _logger.LogWarning(
                        "No content found in scrape response for page. PageId: {PageId}",
                        page.Id);
                    page.VectorStatus = VectorStatus.Failed;
                    await dbContext.SaveChangesAsync();
                    errorCount++;
                    continue;
                }

                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully scraped page. PageId: {PageId}, ContentLength: {Length}",
                    page.Id,
                    page.RawContent?.Length ?? 0);

                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(
                    ex,
                    "Error scraping page. PageId: {PageId}, WebsiteId: {WebsiteId}",
                    page.Id,
                    page.WebsiteId);

                // Update page status to Failed
                try
                {
                    var pageToUpdate = await dbContext.Pages
                        .IgnoreQueryFilters()
                        .FirstOrDefaultAsync(p => p.Id == page.Id);

                    if (pageToUpdate != null)
                    {
                        pageToUpdate.VectorStatus = VectorStatus.Failed;
                        await dbContext.SaveChangesAsync();
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(
                        updateEx,
                        "Failed to update page status to Failed. PageId: {PageId}",
                        page.Id);
                }
            }
        }

        _logger.LogInformation(
            "ScrapePagesJob completed. Success: {SuccessCount}, Errors: {ErrorCount}",
            successCount,
            errorCount);
    }
}

