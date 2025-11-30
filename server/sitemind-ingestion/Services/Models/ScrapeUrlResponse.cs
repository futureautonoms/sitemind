using System.Text.Json.Serialization;

namespace sitemind_ingestion.Services.Models;

public class ScrapeUrlResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, object>? Metadata { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("links")]
    public List<object>? Links { get; set; }

    [JsonPropertyName("html_length")]
    public int HtmlLength { get; set; }

    [JsonPropertyName("html_content")]
    public string? HtmlContent { get; set; }

    [JsonPropertyName("markdown_content")]
    public string? MarkdownContent { get; set; }
}

