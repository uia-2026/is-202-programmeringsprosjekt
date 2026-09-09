// SeedData.cs
using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct)
    {
        if (await db.Resources.AnyAsync(ct)) return; // already seeded

        db.Resources.AddRange(
            new Resource
            {
                Type = "Traktor",
                Latitude = 58.1467,
                Longitude = 7.9956,
                AvailableFrom = DateTime.UtcNow,
                ContactName = "Ola Nordmann",
                ContactInfo = "ola@example.com",
                Status = ResourceStatus.New
            },
            new Resource
            {
                Type = "Drone",
                Latitude = 58.1599,
                Longitude = 8.0182,
                AvailableFrom = DateTime.UtcNow,
                ContactName = "Kari Nordmann",
                ContactInfo = "kari@example.com",
                Status = ResourceStatus.New
            }
        );

        await db.SaveChangesAsync(ct);
    }
}