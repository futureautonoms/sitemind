using sitemind_shared.Enums;

namespace sitemind_shared.Entities;

public class Page : BaseTenantEntity
{
    public Guid WebsiteId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? RawContent { get; set; }
    public VectorStatus VectorStatus { get; set; } = VectorStatus.Pending;
    
    // Navigation property
    public virtual Website Website { get; set; } = null!;
}

