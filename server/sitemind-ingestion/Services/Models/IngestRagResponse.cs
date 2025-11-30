using System.Text.Json.Serialization;

namespace sitemind_ingestion.Services.Models;

public class IngestRagResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("company_id")]
    public string? CompanyId { get; set; }
}

