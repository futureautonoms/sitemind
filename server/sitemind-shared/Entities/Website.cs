using sitemind_shared.Enums;

namespace sitemind_shared.Entities;

public class Website : BaseTenantEntity
{
    public string Name { get; set; } = string.Empty;
    public string BaseUrl { get; set; } = string.Empty;
    public WebsiteStatus Status { get; set; } = WebsiteStatus.Created;
    public DateTime? LastCrawledAt { get; set; }
    
    // Navigation property
    public virtual ICollection<Page> Pages { get; set; } = new List<Page>();
}

