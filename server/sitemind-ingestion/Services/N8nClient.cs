using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using sitemind_ingestion.Services.Models;

namespace sitemind_ingestion.Services;

public class N8nClient : IN8nClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<N8nClient> _logger;
    private readonly IConfiguration _configuration;
    private readonly AsyncRetryPolicy _retryPolicy;

    public N8nClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<N8nClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _configuration = configuration;

        // Configure Polly retry policy: 3 retries with exponential backoff (2s, 4s, 8s)
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                onRetry: (exception, timespan) =>
                {
                    var errorMessage = exception?.Message ?? "Unknown error";
                    _logger.LogWarning(
                        "Retrying n8n webhook call after {Delay}s. Error: {Error}",
                        timespan.TotalSeconds,
                        errorMessage);
                });
    }

    private string GetWebhookUrl(string configKey, string envVarName)
    {
        return Environment.GetEnvironmentVariable(envVarName) 
            ?? _configuration[configKey] 
            ?? throw new InvalidOperationException($"{configKey} configuration is required.");
    }

    public async Task<List<string>> FetchWebsiteUrlsAsync(string url)
    {
        var mapWebsiteUrl = GetWebhookUrl("N8n:MapWebsiteUrl", "N8N_MAP_WEBSITE_URL");

        var payload = new
        {
            url = url
        };

        _logger.LogInformation(
            "Fetching website URLs from n8n for Url: {Url}",
            url);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(mapWebsiteUrl, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "n8n get-urls webhook call failed with status {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    errorContent);
                
                response.EnsureSuccessStatusCode();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var urlList = JsonSerializer.Deserialize<List<string>>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (urlList == null)
            {
                _logger.LogWarning(
                    "n8n get-urls webhook returned null or invalid response. Response: {Response}",
                    responseContent);
                return new List<string>();
            }

            _logger.LogInformation(
                "Successfully fetched {Count} URLs from n8n for Url: {Url}",
                urlList.Count,
                url);

            return urlList;
        });
    }

    public async Task<ScrapeUrlResponse> ScrapeUrlAsync(string url)
    {
        var scrapeWebhookUrl = GetWebhookUrl("N8n:ScrapeUrl", "N8N_SCRAPE_URL");

        var payload = new
        {
            url = url
        };

        _logger.LogInformation(
            "Scraping URL from n8n for Url: {Url}",
            url);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(scrapeWebhookUrl, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "n8n scrape-url webhook call failed with status {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    errorContent);
                
                response.EnsureSuccessStatusCode();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var scrapeResponse = JsonSerializer.Deserialize<ScrapeUrlResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (scrapeResponse == null)
            {
                _logger.LogWarning(
                    "n8n scrape-url webhook returned null or invalid response. Response: {Response}",
                    responseContent);
                throw new InvalidOperationException("Failed to deserialize scrape response from n8n");
            }

            _logger.LogInformation(
                "Successfully scraped URL from n8n for Url: {Url}",
                url);

            return scrapeResponse;
        });
    }

    public async Task<ProcessContentResponse> ProcessContentAsync(string content)
    {
        var processWebhookUrl = GetWebhookUrl("N8n:ProcessContentUrl", "N8N_PROCESS_CONTENT_URL");

        var payload = new
        {
            content = content
        };

        _logger.LogInformation(
            "Processing content with AI from n8n. Content length: {Length}",
            content?.Length ?? 0);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(processWebhookUrl, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "n8n process-content webhook call failed with status {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    errorContent);
                
                response.EnsureSuccessStatusCode();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var processResponse = JsonSerializer.Deserialize<ProcessContentResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (processResponse == null)
            {
                _logger.LogWarning(
                    "n8n process-content webhook returned null or invalid response. Response: {Response}",
                    responseContent);
                throw new InvalidOperationException("Failed to deserialize process content response from n8n");
            }

            _logger.LogInformation(
                "Successfully processed content with AI from n8n");

            return processResponse;
        });
    }

    public async Task<IngestRagResponse> IngestRagAsync(Guid companyId, string url, Dictionary<string, object> metadata, string markdownContent)
    {
        var ingestWebhookUrl = GetWebhookUrl("N8n:IngestRagUrl", "N8N_INGEST_RAG_URL");

        var payload = new
        {
            company_id = companyId.ToString(),
            url = url,
            metadata = metadata,
            markdown_content = markdownContent
        };

        _logger.LogInformation(
            "Ingesting content to RAG vector store from n8n. CompanyId: {CompanyId}, Url: {Url}",
            companyId,
            url);

        return await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(ingestWebhookUrl, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "n8n ingest-rag webhook call failed with status {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    errorContent);
                
                response.EnsureSuccessStatusCode();
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var ingestResponse = JsonSerializer.Deserialize<IngestRagResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (ingestResponse == null)
            {
                _logger.LogWarning(
                    "n8n ingest-rag webhook returned null or invalid response. Response: {Response}",
                    responseContent);
                throw new InvalidOperationException("Failed to deserialize ingest RAG response from n8n");
            }

            _logger.LogInformation(
                "Successfully ingested content to RAG vector store. CompanyId: {CompanyId}",
                companyId);

            return ingestResponse;
        });
    }
}

