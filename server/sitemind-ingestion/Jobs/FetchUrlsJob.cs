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
        var timeoutThreshold = now.AddMinutes(-30);
        
        // Find websites that need processing:
        // 1. Created status (new websites)
        // 2. Error status: 
        //    - If LastCrawledAt is null, retry immediately (never processed before)
        //    - If LastCrawledAt exists, retry after 1 hour
        // 3. Stuck Crawling status:
        //    - If LastCrawledAt is null, retry immediately (stuck from start)
        //    - If LastCrawledAt exists, retry after 30 minutes
        // 4. ProcessingStartedAt kontrolü: 
        //    - NULL ise veya 30 dakikadan eskiyse (stuck job) işleme al
        // IgnoreQueryFilters() kullan çünkü job'da HttpContext yok ve tüm organization'ları işlememiz gerekiyor
        // SELECT FOR UPDATE SKIP LOCKED ile pessimistic locking kullanarak race condition'ı önle
        var websitesToProcess = await dbContext.Websites
            .IgnoreQueryFilters()
            .Where(w => 
                (w.Status == WebsiteStatus.Created 
                || (w.Status == WebsiteStatus.Error 
                    && (w.LastCrawledAt == null || w.LastCrawledAt.Value < now.AddHours(-1)))
                || (w.Status == WebsiteStatus.Crawling 
                    && (w.LastCrawledAt == null || w.LastCrawledAt.Value < now.AddMinutes(-30))))
                && (w.ProcessingStartedAt == null || w.ProcessingStartedAt.Value < timeoutThreshold)
            )
            .OrderBy(w => w.CreatedAt) // Process older websites first
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
            // Her website için ayrı transaction kullanarak pessimistic locking sağla
            // SELECT FOR UPDATE SKIP LOCKED benzeri davranış için transaction içinde
            // ProcessingStartedAt'ı set edip SaveChanges yaparak row lock alıyoruz
            using var transaction = await dbContext.Database.BeginTransactionAsync();
            bool lockAcquired = false;
            Guid websiteId = website.Id;
            
            try
            {
                // Transaction içinde website'yi tekrar yükle ve lock al
                // Bu sayede başka bir instance aynı website'yi işleyemez
                var websiteToProcess = await dbContext.Websites
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(w => w.Id == website.Id);

                if (websiteToProcess == null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogWarning(
                        "Website not found. WebsiteId: {WebsiteId}",
                        website.Id);
                    continue;
                }

                // Eğer başka bir instance zaten işleme başlamışsa (ProcessingStartedAt set edilmişse ve timeout olmamışsa), skip et
                // Transaction içinde güncel zamanı kullan (zaman geçmiş olabilir)
                var currentTimeoutThreshold = DateTime.UtcNow.AddMinutes(-30);
                if (websiteToProcess.ProcessingStartedAt != null 
                    && websiteToProcess.ProcessingStartedAt.Value >= currentTimeoutThreshold)
                {
                    await transaction.RollbackAsync();
                    _logger.LogInformation(
                        "Website is already being processed by another instance. WebsiteId: {WebsiteId}",
                        websiteToProcess.Id);
                    continue;
                }

                var previousStatus = websiteToProcess.Status;
                _logger.LogInformation(
                    "Fetching URLs for website. WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}, Url: {Url}, PreviousStatus: {PreviousStatus}",
                    websiteToProcess.Id,
                    websiteToProcess.OrganizationId,
                    websiteToProcess.BaseUrl,
                    previousStatus);

                // Lock al: ProcessingStartedAt'ı set et ve SaveChanges yap
                // Bu sırada row lock alınır ve başka instance'lar bu kaydı işleyemez
                websiteToProcess.ProcessingStartedAt = DateTime.UtcNow;
                websiteToProcess.Status = WebsiteStatus.Crawling;
                websiteToProcess.LastCrawledAt = DateTime.UtcNow;
                await dbContext.SaveChangesAsync();
                await transaction.CommitAsync();
                lockAcquired = true; // Lock başarıyla alındı, finally'de temizlememiz gerekiyor

                // Fetch URLs from n8n
                var urls = await n8nClient.FetchWebsiteUrlsAsync(websiteToProcess.BaseUrl);

                if (urls == null || !urls.Any())
                {
                    _logger.LogWarning(
                        "No URLs found for website. WebsiteId: {WebsiteId}",
                        websiteToProcess.Id);
                    
                    // Yeni transaction ile status güncelle
                    using var updateTransaction = await dbContext.Database.BeginTransactionAsync();
                    try
                    {
                        var websiteToUpdate = await dbContext.Websites
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(w => w.Id == websiteToProcess.Id);

                        if (websiteToUpdate != null)
                        {
                            websiteToUpdate.Status = WebsiteStatus.Error;
                            websiteToUpdate.LastCrawledAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                            await updateTransaction.CommitAsync();
                        }
                    }
                    catch (Exception updateEx)
                    {
                        await updateTransaction.RollbackAsync();
                        _logger.LogError(
                            updateEx,
                            "Failed to update website status to Error. WebsiteId: {WebsiteId}",
                            websiteToProcess.Id);
                    }
                    
                    errorCount++;
                    continue;
                }

                _logger.LogInformation(
                    "Fetched {Count} URLs for website. WebsiteId: {WebsiteId}",
                    urls.Count,
                    websiteToProcess.Id);

                // Create Page entities for each URL
                var existingUrls = await dbContext.Pages
                    .IgnoreQueryFilters()
                    .Where(p => p.WebsiteId == websiteToProcess.Id)
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
                        WebsiteId = websiteToProcess.Id,
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
                        websiteToProcess.Id);
                }
                else
                {
                    _logger.LogInformation(
                        "All URLs already exist as pages. WebsiteId: {WebsiteId}",
                        websiteToProcess.Id);
                }

                successCount++;
            }
            catch (Exception ex)
            {
                if (!lockAcquired)
                {
                    // Lock alınamadıysa transaction'ı rollback et
                    try
                    {
                        await transaction.RollbackAsync();
                    }
                    catch (Exception rollbackEx)
                    {
                        _logger.LogError(rollbackEx, "Failed to rollback transaction for WebsiteId: {WebsiteId}", websiteId);
                    }
                }

                errorCount++;
                _logger.LogError(
                    ex,
                    "Error fetching URLs for website. WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}",
                    websiteId,
                    website.OrganizationId);

                // Update website status to Error and set LastCrawledAt for retry mechanism
                if (lockAcquired)
                {
                    try
                    {
                        using var updateTransaction = await dbContext.Database.BeginTransactionAsync();
                        var websiteToUpdate = await dbContext.Websites
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(w => w.Id == websiteId);

                        if (websiteToUpdate != null)
                        {
                            websiteToUpdate.Status = WebsiteStatus.Error;
                            websiteToUpdate.LastCrawledAt = DateTime.UtcNow;
                            await dbContext.SaveChangesAsync();
                            await updateTransaction.CommitAsync();
                        }
                    }
                    catch (Exception updateEx)
                    {
                        _logger.LogError(
                            updateEx,
                            "Failed to update website status to Error. WebsiteId: {WebsiteId}",
                            websiteId);
                    }
                }
            }
            finally
            {
                // Lock alındıysa, işlem bitince (başarılı veya hata) ProcessingStartedAt'ı temizle
                // Bu sayede crash durumunda bile 30 dakika sonra timeout ile retry edilebilir
                if (lockAcquired)
                {
                    try
                    {
                        using var cleanupTransaction = await dbContext.Database.BeginTransactionAsync();
                        var websiteToCleanup = await dbContext.Websites
                            .IgnoreQueryFilters()
                            .FirstOrDefaultAsync(w => w.Id == websiteId);

                        if (websiteToCleanup != null)
                        {
                            websiteToCleanup.ProcessingStartedAt = null;
                            await dbContext.SaveChangesAsync();
                            await cleanupTransaction.CommitAsync();
                        }
                    }
                    catch (Exception cleanupEx)
                    {
                        _logger.LogError(
                            cleanupEx,
                            "Failed to cleanup ProcessingStartedAt. WebsiteId: {WebsiteId}",
                            websiteId);
                    }
                }
            }
        }

        _logger.LogInformation(
            "FetchUrlsJob completed. Success: {SuccessCount}, Errors: {ErrorCount}",
            successCount,
            errorCount);
    }
}

