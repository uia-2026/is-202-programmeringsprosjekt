using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public static class SeedData
{
    private const string DevPassword = "DevPassword123!";

    private static readonly (string Name, CategoryAppliesTo AppliesTo)[] Categories =
    {
        ("Transport",     CategoryAppliesTo.Both),
        ("Machinery",     CategoryAppliesTo.Both),
        ("Equipment",     CategoryAppliesTo.Both),
        ("Power",         CategoryAppliesTo.Both),
        ("Communication", CategoryAppliesTo.Both),
        ("Personnel",     CategoryAppliesTo.Both),
        ("Premises",      CategoryAppliesTo.Both),
        ("Materials",     CategoryAppliesTo.Both),
    };

    /// <summary>
    /// Data the application cannot work without. Runs in every environment, on every start.
    /// </summary>
    public static async Task SeedReferenceDataAsync(AppDbContext db, CancellationToken ct)
    {
        foreach (var (name, appliesTo) in Categories)
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == name, ct);

            if (category is null)
                db.Categories.Add(new Category { Name = name, AppliesTo = appliesTo });
            else if (category.AppliesTo != appliesTo)
                category.AppliesTo = appliesTo;
        }

        await db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Development-only fixtures: two users, two resources with images, two needs.
    /// </summary>
    public static async Task SeedDevDataAsync(
        AppDbContext db,
        IAttachmentService attachmentService,
        CancellationToken ct)
    {
        var publicActor = await GetOrCreateUserAsync(
            db, "dev_public_actor", "dev.public.actor@example.com",
            UserRole.PublicActor, "Public Authority", ct);

        var provider = await GetOrCreateUserAsync(
            db, "dev_resource_provider", "dev.resource.provider@example.com",
            UserRole.ResourceProvider, "Private", ct);

        await db.SaveChangesAsync(ct); // new users need their ids below

        var transport = await GetCategoryAsync(db, "Transport", ct);
        var equipment = await GetCategoryAsync(db, "Equipment", ct);

        var tractor = await db.Resources.FirstOrDefaultAsync(r => r.Title == "Dev Test Tractor", ct)
            ?? db.Resources.Add(new Resource
            {
                UserId = provider.Id,
                CategoryId = transport.Id,
                Title = "Dev Test Tractor",
                Description = "Development test resource for transport and clearing work.",
                Latitude = 58.1467,
                Longitude = 7.9956,
                Region = "Kristiansand",
                AvailableFrom = DateTime.UtcNow,
                ContactPoint = provider.Email,
                Status = ResourceStatus.Available
            }).Entity;

        var drone = await db.Resources.FirstOrDefaultAsync(r => r.Title == "Dev Test Drone", ct)
            ?? db.Resources.Add(new Resource
            {
                UserId = provider.Id,
                CategoryId = equipment.Id,
                Title = "Dev Test Drone",
                Description = "Development test resource for aerial observation.",
                Latitude = 58.1599,
                Longitude = 8.0182,
                Region = "Kristiansand",
                AvailableFrom = DateTime.UtcNow,
                ContactPoint = provider.Email,
                Status = ResourceStatus.Available
            }).Entity;

        var transportNeed = await db.Needs.FirstOrDefaultAsync(n => n.Title == "Dev Test Transport Need", ct)
            ?? db.Needs.Add(new Need
            {
                UserId = publicActor.Id,
                CategoryId = transport.Id,
                Title = "Dev Test Transport Need",
                Description = "Development test need for emergency transport.",
                Latitude = 58.1500,
                Longitude = 7.9980,
                Region = "Kristiansand",
                Priority = NeedPriority.Urgent,
                Deadline = DateTime.UtcNow.AddDays(1),
                ContactPoint = publicActor.Email,
                Status = NeedStatus.New
            }).Entity;

        var aerialNeed = await db.Needs.FirstOrDefaultAsync(n => n.Title == "Dev Test Aerial Observation Need", ct)
            ?? db.Needs.Add(new Need
            {
                UserId = publicActor.Id,
                CategoryId = equipment.Id,
                Title = "Dev Test Aerial Observation Need",
                Description = "Development test need for aerial observation.",
                Latitude = 58.1550,
                Longitude = 8.0100,
                Region = "Kristiansand",
                Priority = NeedPriority.Planned,
                Deadline = DateTime.UtcNow.AddDays(3),
                ContactPoint = publicActor.Email,
                Status = NeedStatus.New
            }).Entity;

        RenewDeadlineIfExpired(transportNeed, days: 1);
        RenewDeadlineIfExpired(aerialNeed, days: 3);

        await db.SaveChangesAsync(ct);

        await SeedAttachmentAsync(db, attachmentService, tractor, "isbil.jpg", ct);
        await SeedAttachmentAsync(db, attachmentService, drone, "drone.jpg", ct);
    }

    private static async Task<User> GetOrCreateUserAsync(
        AppDbContext db,
        string username,
        string email,
        UserRole role,
        string actorType,
        CancellationToken ct)
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email, ct);
        if (user is not null) return user;

        user = new User
        {
            Username = username,
            Email = email,
            Role = role,
            ActorType = actorType,
            TwoFactorEnabled = false
        };
        user.PasswordHash = new PasswordHasher<User>().HashPassword(user, DevPassword);

        db.Users.Add(user);
        return user;
    }

    private static async Task<Category> GetCategoryAsync(AppDbContext db, string name, CancellationToken ct) =>
        await db.Categories.FirstOrDefaultAsync(c => c.Name == name, ct)
            ?? throw new InvalidOperationException(
                $"Category '{name}' is missing. SeedReferenceDataAsync must run first.");

    private static void RenewDeadlineIfExpired(Need need, int days)
    {
        if (need.Deadline is null || need.Deadline < DateTime.UtcNow)
            need.Deadline = DateTime.UtcNow.AddDays(days);
    }

    private static async Task SeedAttachmentAsync(
        AppDbContext db,
        IAttachmentService attachmentService,
        Resource resource,
        string fileName,
        CancellationToken ct)
    {
        if (await db.Attachments.AnyAsync(a => a.ResourceId == resource.Id, ct))
            return;

        var path = Path.Combine(AppContext.BaseDirectory, "SeedAssets", fileName);
        if (!File.Exists(path))
        {
            Console.WriteLine($"Warning: seed image not found: {path}");
            return;
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var result = await attachmentService.UploadAsync(
                resource.Id, stream, fileName, stream.Length, resource.UserId, ct);

            if (!result.Success)
                Console.WriteLine($"Warning: seed image '{fileName}' was not uploaded: {result.Error}");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"Warning: seed image '{fileName}' failed: {ex.Message}");
        }
    }
}