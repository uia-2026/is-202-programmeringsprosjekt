using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    public static async Task SeedAsync(
        AppDbContext db,
        CancellationToken ct)
    {
        // Seed users
        var passwordHasher = new PasswordHasher<User>();
        const string devPassword = "DevPassword123!";

        var publicActor = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "dev_public_actor", ct);

        if (publicActor == null)
        {
            publicActor = new User
            {
                Username = "dev_public_actor",
                Email = "dev.public.actor@example.com",
                Role = UserRole.PublicActor,
                ActorType = "Public Authority",
                TwoFactorEnabled = false
            };

            publicActor.PasswordHash =
                passwordHasher.HashPassword(publicActor, devPassword);

            db.Users.Add(publicActor);
        }

        var resourceProvider = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "dev_resource_provider", ct);

        if (resourceProvider == null)
        {
            resourceProvider = new User
            {
                Username = "dev_resource_provider",
                Email = "dev.resource.provider@example.com",
                Role = UserRole.ResourceProvider,
                ActorType = "Private",
                TwoFactorEnabled = false
            };

            resourceProvider.PasswordHash =
                passwordHasher.HashPassword(resourceProvider, devPassword);

            db.Users.Add(resourceProvider);
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
                    UserId = resourceProvider.Id,
                    CategoryId = transportCategory.Id,
                    Title = "Dev Test Tractor",
                    Description = "Development test resource for transport and clearing work.",
                    Latitude = 58.1467,
                    Longitude = 7.9956,
                    Region = "Kristiansand",
                    AvailableFrom = DateTime.UtcNow,
                    ContactPoint = resourceProvider.Email,
                    Status = ResourceStatus.Available
                },
                new Resource
                {
                    UserId = resourceProvider.Id,
                    CategoryId = equipmentCategory.Id,
                    Title = "Dev Test Drone",
                    Description = "Development test resource for aerial observation.",
                    Latitude = 58.1599,
                    Longitude = 8.0182,
                    Region = "Kristiansand",
                    AvailableFrom = DateTime.UtcNow,
                    ContactPoint = resourceProvider.Email,
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
                    Title = "Dev Test Transport Need",
                    Description = "Development test need for emergency transport.",
                    Latitude = 58.1500,
                    Longitude = 7.9980,
                    Region = "Kristiansand",
                    Priority = NeedPriority.Urgent,
                    Deadline = DateTime.UtcNow.AddDays(1),
                    ContactPoint = publicActor.Email,
                    Status = NeedStatus.New
                },
                new Need
                {
                    UserId = publicActor.Id,
                    CategoryId = equipmentCategory.Id,
                    Title = "Dev Test Aerial Observation Need",
                    Description = "Development test need for aerial observation.",
                    Latitude = 58.1550,
                    Longitude = 8.0100,
                    Region = "Kristiansand",
                    Priority = NeedPriority.Planned,
                    Deadline = DateTime.UtcNow.AddDays(3),
                    ContactPoint = publicActor.Email,
                    Status = NeedStatus.New
                }
            );

            await db.SaveChangesAsync(ct);
        }
    }
}