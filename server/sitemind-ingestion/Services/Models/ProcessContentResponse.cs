using System.Text.Json.Serialization;

namespace sitemind_ingestion.Services.Models;

public class ProcessContentResponse
{
    [JsonPropertyName("markdown_content")]
    public string? MarkdownContent { get; set; }

    [JsonPropertyName("metadata")]
    public ProcessContentMetadata? Metadata { get; set; }
}

public class ProcessContentMetadata
{
    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("keywords")]
    public List<string>? Keywords { get; set; }
}

