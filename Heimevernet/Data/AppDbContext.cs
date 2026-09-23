using Microsoft.EntityFrameworkCore;
using Heimevernet.Models;

namespace Heimevernet.Data;

/// <summary>
/// Entity Framework database context for the application.
/// Contains DbSet properties for persisted entities.
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Creates a new <see cref="AppDbContext"/> with the provided options.
    /// </summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>
    /// DbSet representing registered resources.
    /// </summary>
    public DbSet<Resource> Resources => Set<Resource>();

    /// <summary>
    /// Configure the EF model. Conversions and additional mapping are configured here.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Persist enum as string for readability in the database
        modelBuilder.Entity<Resource>()
            .Property(r => r.Status)
            .HasConversion<string>();
    }
}
