namespace sitemind_shared.Entities;

public class UserOrganizationMembership
{
    public Guid UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Role { get; set; } = "member"; // admin, member, viewer
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual User? User { get; set; }
    public virtual Organization? Organization { get; set; }
}

