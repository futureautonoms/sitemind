using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sitemind_ingestion.Services;
using sitemind_shared.Data;
using sitemind_shared.Enums;
using System.Text.Json;

namespace sitemind_ingestion.Jobs;

public class ProcessPagesJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProcessPagesJob> _logger;

    public ProcessPagesJob(
        IServiceProvider serviceProvider,
        ILogger<ProcessPagesJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync()
    {
        _logger.LogInformation("Starting ProcessPagesJob to process content with AI");

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SiteMindDbContext>();
        var n8nClient = scope.ServiceProvider.GetRequiredService<IN8nClient>();

        // Find pages that need processing: Processing status, has RawContent, but no MarkdownContent
        // IgnoreQueryFilters() kullan çünkü job'da HttpContext yok ve tüm organization'ları işlememiz gerekiyor
        var pagesToProcess = await dbContext.Pages
            .IgnoreQueryFilters()
            .Where(p => p.VectorStatus == VectorStatus.Processing 
                && !string.IsNullOrEmpty(p.RawContent) 
                && string.IsNullOrEmpty(p.MarkdownContent))
            .Take(10) // Process 10 pages at a time
            .ToListAsync();

        if (!pagesToProcess.Any())
        {
            _logger.LogInformation("No pages found that need processing. Skipping.");
            return;
        }

        _logger.LogInformation(
            "Found {Count} page(s) that need processing. Processing...",
            pagesToProcess.Count);

        var successCount = 0;
        var errorCount = 0;

        foreach (var page in pagesToProcess)
        {
            try
            {
                _logger.LogInformation(
                    "Processing page content with AI. PageId: {PageId}, WebsiteId: {WebsiteId}, Url: {Url}",
                    page.Id,
                    page.WebsiteId,
                    page.Url);

                // Process content with AI
                var processResponse = await n8nClient.ProcessContentAsync(page.RawContent!);

                if (processResponse == null)
                {
                    _logger.LogWarning(
                        "Process response is null for page. PageId: {PageId}",
                        page.Id);
                    page.VectorStatus = VectorStatus.Failed;
                    await dbContext.SaveChangesAsync();
                    errorCount++;
                    continue;
                }

                // Store processed content
                page.MarkdownContent = processResponse.MarkdownContent;
                page.Summary = processResponse.Metadata?.Summary;

                // Store keywords as JSON array
                if (processResponse.Metadata?.Keywords != null && processResponse.Metadata.Keywords.Any())
                {
                    page.KeywordsJson = JsonSerializer.Serialize(processResponse.Metadata.Keywords);
                }

                // Status remains Processing (ready for next stage)
                await dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Successfully processed page content. PageId: {PageId}, MarkdownLength: {Length}",
                    page.Id,
                    page.MarkdownContent?.Length ?? 0);

                successCount++;
            }
            catch (Exception ex)
            {
                errorCount++;
                _logger.LogError(
                    ex,
                    "Error processing page content. PageId: {PageId}, WebsiteId: {WebsiteId}",
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
            "ProcessPagesJob completed. Success: {SuccessCount}, Errors: {ErrorCount}",
            successCount,
            errorCount);
    }
}

