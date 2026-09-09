using Microsoft.EntityFrameworkCore;
using Heimevernet.Models;

namespace Heimevernet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Resource> Resources => Set<Resource>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Resource>()
            .Property(r => r.Status)
            .HasConversion<string>();
    }
}