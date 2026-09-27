using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task SeedAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        // Seed users
        var ola = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "ola", ct);

        if (ola == null)
        {
            ola = new User
            {
                Username = "ola",
                Email = "ola@example.com",
                PasswordHash = "dev-seed-password",
                Role = UserRole.ResourceProvider,
                ActorType = "Private",
                TwoFactorEnabled = false
            };

            db.Users.Add(ola);
        }

        var kari = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "kari", ct);

        if (kari == null)
        {
            kari = new User
            {
                Username = "kari",
                Email = "kari@example.com",
                PasswordHash = "dev-seed-password",
                Role = UserRole.ResourceProvider,
                ActorType = "Private",
                TwoFactorEnabled = false
            };

            db.Users.Add(kari);
        }

        var publicActor = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "public-actor", ct);

        if (publicActor == null)
        {
            publicActor = new User
            {
                Username = "public-actor",
                Email = "public@example.com",
                PasswordHash = "dev-seed-password",
                Role = UserRole.PublicActor,
                ActorType = "Public Authority",
                TwoFactorEnabled = false
            };

            db.Users.Add(publicActor);
        }

        await db.SaveChangesAsync(ct);

        // Seed categories
        var equipmentCategory = await db.Categories
            .FirstOrDefaultAsync(c => c.Name == "Equipment", ct);

        if (equipmentCategory == null)
        {
            equipmentCategory = new Category
            {
                Name = "Equipment",
                AppliesTo = CategoryAppliesTo.Both
            };

            db.Categories.Add(equipmentCategory);
        }

        var transportCategory = await db.Categories
            .FirstOrDefaultAsync(c => c.Name == "Transport", ct);

        if (transportCategory == null)
        {
            transportCategory = new Category
            {
                Name = "Transport",
                AppliesTo = CategoryAppliesTo.Both
            };

            db.Categories.Add(transportCategory);
        }

        var personnelCategory = await db.Categories
            .FirstOrDefaultAsync(c => c.Name == "Personnel", ct);

        if (personnelCategory == null)
        {
            personnelCategory = new Category
            {
                Name = "Personnel",
                AppliesTo = CategoryAppliesTo.Need
            };

            db.Categories.Add(personnelCategory);
        }

        await db.SaveChangesAsync(ct);

        // Seed resources
        if (!await db.Resources.AnyAsync(ct))
        {
            db.Resources.AddRange(
                new Resource
                {
                    UserId = ola.Id,
                    CategoryId = transportCategory.Id,
                    Title = "Traktor",
                    Description = "Tractor available for transport and clearing work.",
                    Latitude = 58.1467,
                    Longitude = 7.9956,
                    Region = "Kristiansand",
                    AvailableFrom = DateTime.UtcNow,
                    ContactPoint = "ola@example.com",
                    Status = ResourceStatus.Available
                },
                new Resource
                {
                    UserId = kari.Id,
                    CategoryId = equipmentCategory.Id,
                    Title = "Drone",
                    Description = "Drone available for aerial observation.",
                    Latitude = 58.1599,
                    Longitude = 8.0182,
                    Region = "Kristiansand",
                    AvailableFrom = DateTime.UtcNow,
                    ContactPoint = "kari@example.com",
                    Status = ResourceStatus.Available
                }
            );

            await db.SaveChangesAsync(ct);
        }

        // Seed needs
        if (!await db.Needs.AnyAsync(ct))
        {
            db.Needs.AddRange(
                new Need
                {
                    UserId = publicActor.Id,
                    CategoryId = transportCategory.Id,
                    Title = "Need for transport",
                    Description = "Transport assistance is needed for emergency supplies.",
                    Latitude = 58.1500,
                    Longitude = 7.9980,
                    Region = "Kristiansand",
                    Priority = NeedPriority.Urgent,
                    Deadline = DateTime.UtcNow.AddDays(1),
                    ContactPoint = "public@example.com",
                    Status = NeedStatus.New
                },
                new Need
                {
                    UserId = publicActor.Id,
                    CategoryId = equipmentCategory.Id,
                    Title = "Need for aerial observation",
                    Description = "Aerial overview of the affected area is needed.",
                    Latitude = 58.1550,
                    Longitude = 8.0100,
                    Region = "Kristiansand",
                    Priority = NeedPriority.Planned,
                    Deadline = DateTime.UtcNow.AddDays(3),
                    ContactPoint = "public@example.com",
                    Status = NeedStatus.New
                }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}