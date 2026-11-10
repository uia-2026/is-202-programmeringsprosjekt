using Microsoft.EntityFrameworkCore;
using Heimevernet.Models;
using Heimevernet.Models.Interfaces;

namespace Heimevernet.Data;

/// <summary>
/// Entity Framework Core DbContext for the application.
/// Exposes DbSet properties for Users, Categories, Needs, Resources, Attachments, Matches and AuditLogs
/// and configures model conversions and constraints used by the database provider.
/// </summary>
/// <remarks>
/// Conventions used throughout:
/// - Enums are stored as strings for readability in the DB.
/// - String max lengths come from <see cref="FieldLimits"/> so validation attributes and DB columns stay in sync.
/// - Relationships are configured on the dependent (FK-owning) entity.
/// </remarks>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Need> Needs => Set<Need>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Match> Matches => Set<Match>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        ConfigureUser(modelBuilder);
        ConfigureCategory(modelBuilder);
        ConfigureNeed(modelBuilder);
        ConfigureResource(modelBuilder);
        ConfigureAttachment(modelBuilder);
        ConfigureMatch(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureOutboxMessage(modelBuilder);
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(b =>
        {
            // Properties
            b.Property(u => u.Username).HasMaxLength(FieldLimits.Username);
            b.Property(u => u.Email).HasMaxLength(FieldLimits.Email);
            b.Property(u => u.PasswordHash).HasMaxLength(FieldLimits.PasswordHash);
            b.Property(u => u.ActorType).HasMaxLength(FieldLimits.ActorType);
            b.Property(u => u.TwoFactorSecret).HasMaxLength(FieldLimits.TwoFactorSecret);
            b.Property(u => u.Role).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);

            // Indexes: DB-level protection for unique usernames and emails
            b.HasIndex(u => u.Username).IsUnique();
            b.HasIndex(u => u.Email).IsUnique();
        });
    }

    private static void ConfigureCategory(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>(b =>
        {
            // Properties
            b.Property(c => c.Name).HasMaxLength(FieldLimits.CategoryName);
            b.Property(c => c.AppliesTo).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);

            // Indexes
            b.HasIndex(c => c.Name).IsUnique();
        });
    }

    private static void ConfigureNeed(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Need>(b =>
        {
            // Table constraints
            b.ToTable(t => t.HasCheckConstraint("CK_Needs_QuantityNeeded_Positive", "QuantityNeeded >= 1"));

            // Properties
            b.Property(n => n.Title).HasMaxLength(FieldLimits.Title);
            b.Property(n => n.Description).HasMaxLength(FieldLimits.Description);
            b.Property(n => n.Region).HasMaxLength(FieldLimits.Region);
            b.Property(n => n.ContactPoint).HasMaxLength(FieldLimits.ContactPoint);
            b.Property(n => n.Priority).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(n => n.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(n => n.QuantityNeeded).IsRequired().HasDefaultValue(1);

            // Concurrency
            b.Property(n => n.Version).IsConcurrencyToken();

            // Indexes
            // Map: equality on Status first, then one range column (Latitude); Longitude is filtered inside the index
            b.HasIndex(n => new { n.Status, n.Latitude, n.Longitude });
            // Region dropdown (DISTINCT) and region filter
            b.HasIndex(n => n.Region);

            // Relationships
            // Prevent deleting a user if they have created needs.
            b.HasOne(n => n.User)
                .WithMany(u => u.Needs)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent deleting a category if it is referenced by existing needs.
            b.HasOne(n => n.Category)
                .WithMany(c => c.Needs)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureResource(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resource>(b =>
        {
            // Table constraints
            b.ToTable(t => t.HasCheckConstraint("CK_Resources_QuantityOffered_Positive", "QuantityOffered >= 1"));

            // Properties
            b.Property(r => r.Title).HasMaxLength(FieldLimits.Title);
            b.Property(r => r.Description).HasMaxLength(FieldLimits.Description);
            b.Property(r => r.Region).HasMaxLength(FieldLimits.Region);
            b.Property(r => r.ContactPoint).HasMaxLength(FieldLimits.ContactPoint);
            b.Property(r => r.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(r => r.QuantityOffered).IsRequired().HasDefaultValue(1);

            // Concurrency
            b.Property(r => r.Version).IsConcurrencyToken();

            // Indexes
            // Map for public actors (Available + box) and available lists
            b.HasIndex(r => new { r.Status, r.Latitude, r.Longitude });
            // Nearby search and candidate matching; also replaces the FK index on CategoryId
            b.HasIndex(r => new { r.CategoryId, r.Status, r.Latitude, r.Longitude });
            // Region dropdown (DISTINCT) and region filter
            b.HasIndex(r => r.Region);

            // Relationships
            // Prevent deleting a user if they have created resources.
            b.HasOne(r => r.User)
                .WithMany(u => u.Resources)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent deleting a category if it is referenced by existing resources.
            b.HasOne(r => r.Category)
                .WithMany(c => c.Resources)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAttachment(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Attachment>(b =>
        {
            // Properties
            b.Property(a => a.StorageKey).HasMaxLength(FieldLimits.StorageKey);

            // Relationships
            // Prevent deleting a user if they have uploaded attachments.
            b.HasOne(a => a.UploadedByUser)
                .WithMany(u => u.UploadedAttachments)
                .OnDelete(DeleteBehavior.Restrict);

            // Automatically delete attachments when their associated resource is deleted.
            b.HasOne(a => a.Resource)
                .WithMany(r => r.Attachments)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureMatch(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Match>(b =>
        {
            // Table constraints
            b.ToTable(t => t.HasCheckConstraint("CK_Matches_Quantity_Positive", "Quantity >= 1"));

            // Properties
            b.Property(m => m.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(m => m.Quantity).IsRequired().HasDefaultValue(1);

            // Concurrency
            b.Property(m => m.Version).IsConcurrencyToken();

            // Indexes: DB duplicate protection against the same need/resource pairing
            b.HasIndex(m => new { m.NeedId, m.ResourceId }).IsUnique();

            // Relationships
            // Prevent deleting a user if they have created matches.
            b.HasOne(m => m.MatchedByUser)
                .WithMany(u => u.MatchesCreated)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent deleting a need if it has associated matches.
            b.HasOne(m => m.Need)
                .WithMany(n => n.Matches)
                .OnDelete(DeleteBehavior.Restrict);

            // Prevent deleting a resource if it has associated matches.
            b.HasOne(m => m.Resource)
                .WithMany(r => r.Matches)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(b =>
        {
            // Properties
            b.Property(a => a.ActorName).HasMaxLength(FieldLimits.AuditActorName);
            b.Property(a => a.Action).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(a => a.EntityType).HasMaxLength(FieldLimits.AuditEntityType);
            b.Property(a => a.Details).HasMaxLength(FieldLimits.AuditDetails);
            b.Property(a => a.TraceId).HasMaxLength(FieldLimits.AuditTraceId);

            // Indexes
            b.HasIndex(a => new { a.EntityType, a.EntityId });
            b.HasIndex(a => a.OccurredAt);

            // Relationships
            // Preserve audit logs when the associated user is deleted by setting ActorUserId to NULL.
            b.HasOne(a => a.ActorUser)
                .WithMany(u => u.AuditLogs)
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureOutboxMessage(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.Property(m => m.Type).HasMaxLength(FieldLimits.EnumText);
            b.Property(m => m.DedupKey).HasMaxLength(FieldLimits.DedupKey);
            b.Property(m => m.Payload).HasMaxLength(FieldLimits.OutboxPayload);
            b.Property(m => m.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(m => m.LastError).HasMaxLength(FieldLimits.OutboxError);

            b.HasIndex(m => m.DedupKey).IsUnique();                       // the same event is never queued twice
            b.HasIndex(m => new { m.Status, m.NextAttemptAt });           // the worker's "what is due?" query

            // Removing a user must not delete or block their queued messages
            b.HasOne<User>().WithMany().HasForeignKey(m => m.RecipientUserId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Save pipeline
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Automatically increments the Version property of any modified <see cref="IVersioned"/> entities,
    /// to support optimistic concurrency control.
    /// </summary>
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries<IVersioned>()
                     .Where(e => e.State == EntityState.Modified))
        {
            entry.Entity.Version++;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}