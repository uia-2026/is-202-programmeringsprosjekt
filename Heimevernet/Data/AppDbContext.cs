using Microsoft.EntityFrameworkCore;
using Heimevernet.Models;

namespace Heimevernet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<Need> Needs => Set<Need>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resource>()
            .Property(r => r.Status)
            .HasConversion<string>();

        modelBuilder.Entity<Need>()
            .Property(n => n.Status)
            .HasConversion<string>();

        modelBuilder.Entity <Need>()
            .Property(n => n.Priority)
            .HasConversion<string>();
    }
}