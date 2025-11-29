namespace sitemind_ingestion.Services;

public interface IN8nClient
{
    Task TriggerIngestionAsync(Guid websiteId, Guid organizationId, string url);
}

