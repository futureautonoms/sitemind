using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace sitemind_ingestion.Services;

public class N8nClient : IN8nClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<N8nClient> _logger;
    private readonly string _webhookUrl;
    private readonly AsyncRetryPolicy _retryPolicy;

    public N8nClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<N8nClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        
        // Read webhook URL from configuration (environment variable override support)
        _webhookUrl = Environment.GetEnvironmentVariable("N8N_WEBHOOK_URL") 
            ?? configuration["N8n:WebhookUrl"] 
            ?? throw new InvalidOperationException("N8n:WebhookUrl configuration is required.");

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

    public async Task TriggerIngestionAsync(Guid websiteId, Guid organizationId, string url)
    {
        var payload = new
        {
            website_id = websiteId.ToString(),
            organization_id = organizationId.ToString(),
            url = url
        };

        _logger.LogInformation(
            "Triggering n8n ingestion for WebsiteId: {WebsiteId}, OrganizationId: {OrganizationId}, Url: {Url}",
            websiteId,
            organizationId,
            url);

        await _retryPolicy.ExecuteAsync(async () =>
        {
            var response = await _httpClient.PostAsJsonAsync(_webhookUrl, payload);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError(
                    "n8n webhook call failed with status {StatusCode}. Response: {Response}",
                    response.StatusCode,
                    errorContent);
                
                response.EnsureSuccessStatusCode();
            }

            _logger.LogInformation(
                "Successfully triggered n8n ingestion for WebsiteId: {WebsiteId}",
                websiteId);
        });
    }
}

