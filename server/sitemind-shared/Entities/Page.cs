using sitemind_shared.Enums;

namespace sitemind_shared.Entities;

public class Page : BaseTenantEntity
{
    public Guid WebsiteId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? RawContent { get; set; }
    public string? MarkdownContent { get; set; }
    public string? Summary { get; set; }
    public string? KeywordsJson { get; set; } // JSON array olarak keywords saklanır
    public VectorStatus VectorStatus { get; set; } = VectorStatus.Pending;
    
    // Navigation property
    public virtual Website Website { get; set; } = null!;
}

