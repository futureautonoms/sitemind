using sitemind_ingestion.Services.Models;

namespace sitemind_ingestion.Services;

public interface IN8nClient
{
    Task<List<string>> FetchWebsiteUrlsAsync(string url);
    Task<ScrapeUrlResponse> ScrapeUrlAsync(string url);
    Task<ProcessContentResponse> ProcessContentAsync(string content);
    Task<IngestRagResponse> IngestRagAsync(Guid companyId, string url, Dictionary<string, object> metadata, string markdownContent);
}

