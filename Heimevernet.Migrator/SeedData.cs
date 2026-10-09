using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

public static class SeedData
{
    public static async Task SeedAsync(
        AppDbContext db,
        IAttachmentService attachmentService,
        IHostEnvironment environment,
        CancellationToken ct)
    {
        var passwordHasher = new PasswordHasher<User>();
        const string devPassword = "DevPassword123!";

        // Seed users
        var publicActor = await db.Users
            .FirstOrDefaultAsync(u => u.Username == "dev_public_actor", ct);

        if (publicActor is null)
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

        if (resourceProvider is null)
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
        var equipmentCategory = await GetOrCreateCategoryAsync(
            db, "Equipment", CategoryAppliesTo.Both, ct);

        var transportCategory = await GetOrCreateCategoryAsync(
            db, "Transport", CategoryAppliesTo.Both, ct);

        await GetOrCreateCategoryAsync(
            db, "Personnel", CategoryAppliesTo.Need, ct);

        await db.SaveChangesAsync(ct);

        // Seed resources
        var tractor = await db.Resources
            .FirstOrDefaultAsync(r => r.Title == "Dev Test Tractor", ct);

        if (tractor is null)
        {
            tractor = new Resource
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
            };

            db.Resources.Add(tractor);
        }

        var drone = await db.Resources
            .FirstOrDefaultAsync(r => r.Title == "Dev Test Drone", ct);

        if (drone is null)
        {
            drone = new Resource
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
            };

            db.Resources.Add(drone);
        }

        await db.SaveChangesAsync(ct);

        // Seed resource images
        await SeedAttachmentAsync(
            db, attachmentService, environment, tractor, "isbil.jpg", ct);

        await SeedAttachmentAsync(
            db, attachmentService, environment, drone, "drone.jpg", ct);

        // Seed needs
        if (!await db.Needs.AnyAsync(
                n => n.Title == "Dev Test Transport Need", ct))
        {
            db.Needs.Add(new Need
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
            });
        }

        if (!await db.Needs.AnyAsync(
                n => n.Title == "Dev Test Aerial Observation Need", ct))
        {
            db.Needs.Add(new Need
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
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<Category> GetOrCreateCategoryAsync(
        AppDbContext db,
        string name,
        CategoryAppliesTo appliesTo,
        CancellationToken ct)
    {
        var category = await db.Categories
            .FirstOrDefaultAsync(c => c.Name == name, ct);

        if (category is not null)
            return category;

        category = new Category
        {
            Name = name,
            AppliesTo = appliesTo
        };

        db.Categories.Add(category);
        return category;
    }

    private static async Task SeedAttachmentAsync(
        AppDbContext db,
        IAttachmentService attachmentService,
        IHostEnvironment environment,
        Resource resource,
        string fileName,
        CancellationToken ct)
    {
        // Assumes each seeded resource should have exactly one seed image.
        if (await db.Attachments.AnyAsync(
                a => a.ResourceId == resource.Id, ct))
        {
            return;
        }

        var filePath = Path.Combine(
            environment.ContentRootPath, "SeedAssets", fileName);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                $"Seed image was not found: {filePath}", filePath);
        }

        await using var stream = File.OpenRead(filePath);

        var result = await attachmentService.UploadAsync(
            resource.Id,
            stream,
            fileName,
            stream.Length,
            resource.UserId,
            ct);

        if (!result.Success)
        {
            throw new InvalidOperationException(
                $"Failed to seed image '{fileName}': {result.Error}");
        }
    }
}