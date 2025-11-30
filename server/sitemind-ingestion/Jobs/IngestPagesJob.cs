using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sitemind_ingestion.Services;
using sitemind_shared.Data;
using sitemind_shared.Enums;
using System.Text.Json;

namespace sitemind_ingestion.Jobs;

public class IngestPagesJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IngestPagesJob> _logger;

    public IngestPagesJob(
        IServiceProvider serviceProvider,
        ILogger<IngestPagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting IngestPagesJob to ingest processed pages into vector store");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SiteMindDbContext>();
        var n8nClient = scope.ServiceProvider.GetRequiredService<IN8nClient>();

        // Find pages that need ingestion: Processing status and has MarkdownContent
        // IgnoreQueryFilters() kullan çünkü job'da HttpContext yok ve tüm organization'ları işlememiz gerekiyor
        var pagesToIngest = await dbContext.Pages
            .IgnoreQueryFilters()
            .Where(p => p.VectorStatus == VectorStatus.Processing 
                && !string.IsNullOrEmpty(p.MarkdownContent))
            .Include(p => p.Website)
            .Take(10) // Process 10 pages at a time
            .ToListAsync();

        if (!pagesToIngest.Any())
        {
            _logger.LogInformation("No pages found that need ingestion. Skipping.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} page(s) that need ingestion. Processing...",
            pagesToIngest.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var page in pagesToIngest)
        {
            try
            {
                _logger.LogInformation(
                    "Ingesting page into vector store. PageId: {PageId}, WebsiteId: {WebsiteId}, Url: {Url}",
                    page.Id,
                    page.WebsiteId,
                    page.Url);

                // Prepare metadata for RAG ingestion
                var metadata = new Dictionary<string, object>();
                
                if (!string.IsNullOrEmpty(page.Summary))
                {
                    metadata["summary"] = page.Summary;
                }

                if (!string.IsNullOrEmpty(page.KeywordsJson))
                {
                    try
                    {
                        var keywords = JsonSerializer.Deserialize<List<string>>(page.KeywordsJson);
                        if (keywords != null && keywords.Any())
                        {
                            metadata["keywords"] = keywords;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(
                            ex,
                            "Failed to parse keywords JSON for page. PageId: {PageId}",
                            page.Id);
                    }
                }

                metadata["source_url"] = page.Url;

                // Ingest into vector store
                var ingestResponse = await n8nClient.IngestRagAsync(
                    page.Website.OrganizationId,
                    page.Url,
                    metadata,
                    page.MarkdownContent!);

                if (ingestResponse == null || !ingestResponse.Success)
                {
                    _logger.LogWarning(
                        "Ingest response indicates failure for page. PageId: {PageId}, Message: {Message}",
                        page.Id,
                        ingestResponse?.Message ?? "Unknown error");
                    page.VectorStatus = VectorStatus.Failed;
                    await dbContext.SaveChangesAsync();
                    errorCount++;
                    continue;
                }

                // Update page status to Completed
                page.VectorStatus = VectorStatus.Completed;
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully ingested page into vector store. PageId: {PageId}",
                    page.Id);

                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(
                    ex,
                    "Error ingesting page into vector store. PageId: {PageId}, WebsiteId: {WebsiteId}",
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

        // Check if any websites have all pages completed and update their status
        await CheckAndUpdateWebsiteStatusesAsync(dbContext);

        _logger.LogInformation(
            "IngestPagesJob completed. Success: {SuccessCount}, Errors: {ErrorCount}",
            successCount,
            errorCount);
    }

    private async Task CheckAndUpdateWebsiteStatusesAsync(SiteMindDbContext dbContext)
    {
        try
        {
            // Find websites that are still Crawling
            // IgnoreQueryFilters() kullan çünkü job'da HttpContext yok
            var crawlingWebsites = await dbContext.Websites
                .IgnoreQueryFilters()
                .Where(w => w.Status == WebsiteStatus.Crawling)
                .ToListAsync();

            foreach (var website in crawlingWebsites)
            {
                // Check if all pages are completed (Completed or Failed)
                var totalPages = await dbContext.Pages
                    .IgnoreQueryFilters()
                    .Where(p => p.WebsiteId == website.Id)
                    .CountAsync();

                if (totalPages == 0)
                {
                    // No pages yet, skip
                    continue;
                }

                var completedOrFailedPages = await dbContext.Pages
                    .IgnoreQueryFilters()
                    .Where(p => p.WebsiteId == website.Id 
                        && (p.VectorStatus == VectorStatus.Completed || p.VectorStatus == VectorStatus.Failed))
                    .CountAsync();

                // If all pages are completed or failed, mark website as Active
                if (completedOrFailedPages == totalPages)
                {
                    website.Status = WebsiteStatus.Active;
                    website.LastCrawledAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync();

                    _logger.LogInformation(
                        "All pages completed for website. WebsiteId: {WebsiteId}, Status updated to Active",
                        website.Id);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error checking and updating website statuses");
        }
    }
}

