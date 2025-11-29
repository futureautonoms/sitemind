namespace sitemind_shared.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual ICollection<UserOrganizationMembership> UserMemberships { get; set; } = new List<UserOrganizationMembership>();
    public virtual ICollection<Website> Websites { get; set; } = new List<Website>();
}

