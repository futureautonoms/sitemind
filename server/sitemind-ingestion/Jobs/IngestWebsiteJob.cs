using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using sitemind_ingestion.Services;
using sitemind_shared.Data;
using sitemind_shared.Entities;
using sitemind_shared.Enums;

namespace sitemind_ingestion.Jobs;

public class IngestWebsiteJob
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<IngestWebsiteJob> _logger;

    public IngestWebsiteJob(
        IServiceProvider serviceProvider,
        ILogger<IngestWebsiteJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    [AutomaticRetry(Attempts = 3)]
    public async Task ExecuteAsync(Guid websiteId, Guid organizationId, string url)
    {
        _logger.LogInformation(
            "Starting ingestion job for WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}, Url: {Url}",
            websiteId,
            organizationId,
            url);

        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<SiteMindDbContext>();
        var n8nClient = scope.ServiceProvider.GetRequiredService<IN8nClient>();

        try
        {
            // Update website status to Crawling
            var website = await dbContext.Websites
                .FirstOrDefaultAsync(w => w.Id == websiteId && w.OrganizationId == organizationId);

            if (website == null)
            {
                _logger.LogError(
                    "Website not found. WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}",
                    websiteId,
                    organizationId);
                throw new InvalidOperationException($"Website with Id {websiteId} not found.");
            }

            website.Status = WebsiteStatus.Crawling;
            await dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Updated website status to Crawling. WebsiteId: {WebsiteId}",
                websiteId);

            // Trigger n8n webhook
            await n8nClient.TriggerIngestionAsync(websiteId, organizationId, url);

            _logger.LogInformation(
                "Successfully triggered n8n ingestion. WebsiteId: {WebsiteId}",
                websiteId);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error during ingestion job execution. WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}",
                websiteId,
                organizationId);

            // Update website status to Error
            try
            {
                var website = await dbContext.Websites
                    .FirstOrDefaultAsync(w => w.Id == websiteId && w.OrganizationId == organizationId);

                if (website != null)
                {
                    website.Status = WebsiteStatus.Error;
                    await dbContext.SaveChangesAsync();
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(
                    updateEx,
                    "Failed to update website status to Error. WebsiteId: {WebsiteId}",
                    websiteId);
            }

            throw;
        }
    }
}

