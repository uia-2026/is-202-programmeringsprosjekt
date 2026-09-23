using Microsoft.EntityFrameworkCore;
using Heimevernet.Models;

namespace Heimevernet.Data;

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
        modelBuilder.Entity<User>()
                 .Property(u => u.Role)
                 .HasConversion<string>();

        modelBuilder.Entity<Category>()
            .Property(c => c.AppliesTo)
            .HasConversion<string>();

        modelBuilder.Entity<Need>()
            .Property(n => n.Priority)
            .HasConversion<string>();

        modelBuilder.Entity<Need>()
            .Property(n => n.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Resource>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Match>()
            .Property(m => m.Status)
            .HasConversion<string>();

        // DB duplicate protection against the same need/resource pairing
        modelBuilder.Entity<Match>()
            .HasIndex(m => new { m.NeedId, m.ResourceId })
            .IsUnique();

        // Avoid multiple cascade paths on Match (Need, Resource, MatchedByUser all -> Match)
        // to prevent SQL Server/MariaDB "multiple cascade paths" errors on delete. 
        modelBuilder.Entity<Match>()
            .HasOne(m => m.MatchedByUser)
            .WithMany(u => u.MatchesCreated)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Attachment>()
            .HasOne(a => a.UploadedByUser)
            .WithMany(u => u.UploadedAttachments)
            .OnDelete(DeleteBehavior.Restrict);
    }
}