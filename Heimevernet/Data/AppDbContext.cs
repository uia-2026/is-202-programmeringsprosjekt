using Microsoft.EntityFrameworkCore;
using Heimevernet.Models;

namespace Heimevernet.Data;

/// <summary>
/// Entity Framework Core DbContext for the application.
/// Exposes DbSet properties for Users, Categories, Needs, Resources, Attachments and Matches
/// and configures model conversions and constraints used by the database provider.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Need> Needs => Set<Need>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Attachment> Attachments => Set<Attachment>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Store all enums as strings rather than integers, for readability in the DB
        // and setup max lengths for string fields to match the FieldLimits constants, so that validation attributes and DB columns are in sync.
        modelBuilder.Entity<User>(b =>
        {
            b.Property(u => u.Username).HasMaxLength(FieldLimits.Username);
            b.Property(u => u.Email).HasMaxLength(FieldLimits.Email);
            b.Property(u => u.PasswordHash).HasMaxLength(FieldLimits.PasswordHash);
            b.Property(u => u.ActorType).HasMaxLength(FieldLimits.ActorType);
            b.Property(u => u.TwoFactorSecret).HasMaxLength(FieldLimits.TwoFactorSecret);
            b.Property(u => u.Role).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
        });

        modelBuilder.Entity<Need>(b =>
        {
            b.Property(n => n.Title).HasMaxLength(FieldLimits.Title);
            b.Property(n => n.Description).HasMaxLength(FieldLimits.Description);
            b.Property(n => n.Region).HasMaxLength(FieldLimits.Region);
            b.Property(n => n.ContactPoint).HasMaxLength(FieldLimits.ContactPoint);
            b.Property(n => n.Priority).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.Property(n => n.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
        });

        modelBuilder.Entity<Resource>(b =>
        {
            b.Property(r => r.Title).HasMaxLength(FieldLimits.Title);
            b.Property(r => r.Description).HasMaxLength(FieldLimits.Description);
            b.Property(r => r.Region).HasMaxLength(FieldLimits.Region);
            b.Property(r => r.ContactPoint).HasMaxLength(FieldLimits.ContactPoint);
            b.Property(r => r.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
        });

        modelBuilder.Entity<Category>(b =>
        {
            b.Property(c => c.Name).HasMaxLength(FieldLimits.CategoryName);
            b.Property(c => c.AppliesTo).HasConversion<string>().HasMaxLength(FieldLimits.EnumText);
            b.HasIndex(c => c.Name).IsUnique();
        });

        modelBuilder.Entity<Attachment>(b =>
            b.Property(a => a.StorageKey).HasMaxLength(FieldLimits.StorageKey));

        modelBuilder.Entity<Match>(b =>
            b.Property(m => m.Status).HasConversion<string>().HasMaxLength(FieldLimits.EnumText));

        // DB duplicate protection against the same need/resource pairing
        modelBuilder.Entity<Match>()
            .HasIndex(m => new { m.NeedId, m.ResourceId })
            .IsUnique();

        // deleting a user must never silently delete matches or attachments that other people’s records depend on.
        modelBuilder.Entity<Match>()
            .HasOne(m => m.MatchedByUser)
            .WithMany(u => u.MatchesCreated)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.UploadedByUser)
            .WithMany(u => u.UploadedAttachments)
            .OnDelete(DeleteBehavior.Restrict);

        // DB lvl protection for unique usernames and emails for users
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
    }
}