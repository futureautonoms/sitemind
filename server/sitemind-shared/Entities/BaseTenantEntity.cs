namespace sitemind_shared.Entities;

public abstract class BaseTenantEntity
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

