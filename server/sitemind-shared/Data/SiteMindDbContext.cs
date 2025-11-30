using Microsoft.EntityFrameworkCore;
using sitemind_shared.Abstractions;
using sitemind_shared.Entities;

namespace sitemind_shared.Data;

public class SiteMindDbContext : DbContext
{
    private readonly IOrganizationProvider _organizationProvider;

    public SiteMindDbContext(
        DbContextOptions<SiteMindDbContext> options,
        IOrganizationProvider organizationProvider)
        : base(options)
    {
        _organizationProvider = organizationProvider;
    }

    public DbSet<Organization> Organizations { get; set; }
    public DbSet<Website> Websites { get; set; }
    public DbSet<Page> Pages { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<UserOrganizationMembership> UserOrganizationMemberships { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Organization entity
        modelBuilder.Entity<Organization>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            // One-to-many relationship with Websites
            entity.HasMany(e => e.Websites)
                .WithOne()
                .HasForeignKey(w => w.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);
            
            // One-to-many relationship with UserOrganizationMemberships
            entity.HasMany(e => e.UserMemberships)
                .WithOne(m => m.Organization)
                .HasForeignKey(m => m.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Website entity
        modelBuilder.Entity<Website>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(500);
            entity.Property(e => e.BaseUrl).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // One-to-many relationship with Page
            entity.HasMany(e => e.Pages)
                .WithOne(e => e.Website)
                .HasForeignKey(e => e.WebsiteId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure Page entity
        modelBuilder.Entity<Page>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Url).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.RawContent).HasColumnType("text");
            entity.Property(e => e.MarkdownContent).HasColumnType("text");
            entity.Property(e => e.Summary).HasColumnType("text");
            entity.Property(e => e.KeywordsJson).HasColumnType("text");
            entity.Property(e => e.VectorStatus).IsRequired();
            entity.Property(e => e.OrganizationId).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Index for performance
            entity.HasIndex(e => e.WebsiteId);
            entity.HasIndex(e => e.OrganizationId);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Username).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(500);
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();

            // Unique constraints (Email and Username are globally unique)
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Configure UserOrganizationMembership entity
        modelBuilder.Entity<UserOrganizationMembership>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.OrganizationId });
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);
            entity.Property(e => e.JoinedAt).IsRequired();

            // Foreign keys
            entity.HasOne(e => e.User)
                .WithMany(u => u.OrganizationMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Organization)
                .WithMany(o => o.UserMemberships)
                .HasForeignKey(e => e.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes for performance
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.OrganizationId);
        });

        // Apply Global Query Filters for Multi-Tenancy
        ApplyGlobalQueryFilters(modelBuilder);
    }

    private void ApplyGlobalQueryFilters(ModelBuilder modelBuilder)
    {
        // Global Query Filters - OrganizationId will be evaluated at query time
        // Using a method that gets OrganizationId dynamically at runtime
        modelBuilder.Entity<Website>().HasQueryFilter(e => 
            e.OrganizationId == GetCurrentOrganizationId());
        
        modelBuilder.Entity<Page>().HasQueryFilter(e => 
            e.OrganizationId == GetCurrentOrganizationId());
        
        // User entity no longer has OrganizationId, so no global query filter needed
        // User filtering is done through UserOrganizationMemberships
    }

    private Guid GetCurrentOrganizationId()
    {
        try
        {
            return _organizationProvider.GetOrganizationId();
        }
        catch
        {
            // During migrations, OrganizationProvider might not be available
            // Return empty GUID to disable filtering during migrations
            return Guid.Empty;
        }
    }

    public override int SaveChanges()
    {
        // Automatically set OrganizationId for new BaseTenantEntity entities (Website, Page)
        var entries = ChangeTracker.Entries<BaseTenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity.OrganizationId == Guid.Empty)
            {
                try
                {
                    entry.Entity.OrganizationId = _organizationProvider.GetOrganizationId();
                }
                catch
                {
                    // During migrations, this might fail - that's okay
                }
            }
        }

        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatically set OrganizationId for new BaseTenantEntity entities (Website, Page)
        var entries = ChangeTracker.Entries<BaseTenantEntity>()
            .Where(e => e.State == EntityState.Added);

        foreach (var entry in entries)
        {
            if (entry.Entity.OrganizationId == Guid.Empty)
            {
                try
                {
                    entry.Entity.OrganizationId = _organizationProvider.GetOrganizationId();
                }
                catch
                {
                    // During migrations, this might fail - that's okay
                }
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}

