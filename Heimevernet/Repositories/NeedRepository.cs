using Heimevernet.Data;
using Heimevernet.DTOs.Map;
using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.ViewModels.Map;
using Microsoft.EntityFrameworkCore;
using Heimevernet.DTOs.Needs;
using Heimevernet.DTOs.Notifications;

namespace Heimevernet.Repositories;

/// <summary>
/// Repository implementation for Needs.
/// Provides data access operations used by NeedService and controllers.
/// </summary>
public class NeedRepository : INeedRepository
{
    private readonly AppDbContext _context;
    public NeedRepository(AppDbContext context)
    {
        _context = context;
    }
    public async Task<IEnumerable<Need>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _context.Needs
            .Include(n => n.Category)
            .OrderByDescending(n => n.Priority)
            .ThenBy(n => n.Deadline)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Retrieves a single Need by id including Category navigation.
    /// </summary>
    /// <param name="id">The id of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matching <see cref="Need"/> or null.</returns>
    public async Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Needs
            .Include(n => n.Category)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    public async Task<Need?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default) =>
        await _context.Needs
            .Include(n => n.Category)
            .Include(n => n.Matches)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

    /// <summary>
    /// Adds a Need entity to the context for insertion.
    /// </summary>
    /// <param name="need">The need entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task AddAsync(Need need, CancellationToken cancellationToken = default)
    {
        await _context.Needs.AddAsync(need, cancellationToken);
    }

    public void Remove(Need need)
    {
        _context.Needs.Remove(need);
    }

    public async Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GeoBox box, int limit, CancellationToken cancellationToken = default)
    {
        return await _context.Needs
            .AsNoTracking()
            .Where(n => (n.Status == NeedStatus.New
                      || n.Status == NeedStatus.UnderReview
                      || n.Status == NeedStatus.Assigned)
                     && n.Latitude >= box.MinLat && n.Latitude <= box.MaxLat
                     && n.Longitude >= box.MinLng && n.Longitude <= box.MaxLng)
            .OrderByDescending(n => n.Priority == NeedPriority.Critical)
            .ThenByDescending(n => n.CreatedAt)
            .Take(limit)
            .Select(n => new MapMarkerDto(
                n.Id, MapMarkerKind.Need, n.Title, n.Category.Name, n.Description, n.Region,
                n.Priority.ToString(), n.Priority == NeedPriority.Critical, n.Status.ToString(),
                n.Deadline, n.ContactPoint, n.QuantityNeeded, n.Latitude, n.Longitude))
            .ToListAsync(cancellationToken);
    }

    public void SetOriginalVersion(Need need, int version)
    {
        _context.Entry(need)
            .Property(n => n.Version)
            .OriginalValue = version;
    }

    // ---------- NYTT I STEG 2 ----------

    /// <summary>
    /// Forces EF Core to check Need.Version on save, even if nothing else on the need changed.
    /// Prevents two users from over-allocating the same need at the same time.
    /// </summary>
    public void MarkForConcurrencyCheck(Need need)
    {
        _context.Entry(need)
            .Property(n => n.Version)
            .IsModified = true;
    }

    public async Task<IReadOnlyList<Need>> GetFilteredAsync(NeedFilterDto filter, CancellationToken cancellationToken = default)
    {
        var query = _context.Needs
            .AsNoTracking()
            .Include(n => n.Category)
            .AsQueryable();

        if (filter.Status.HasValue)
            query = query.Where(n => n.Status == filter.Status.Value);

        if (filter.Priority.HasValue)
            query = query.Where(n => n.Priority == filter.Priority.Value);

        if (filter.CategoryId.HasValue)
            query = query.Where(n => n.CategoryId == filter.CategoryId.Value);

        if (filter.OwnerUserId.HasValue)
            query = query.Where(n => n.UserId == filter.OwnerUserId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Region))
            query = query.Where(n => n.Region == filter.Region);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim();
            query = query.Where(n =>
                n.Title.Contains(term) ||
                (n.Description != null && n.Description.Contains(term)) ||
                n.Region.Contains(term));
        }

        query = filter.Sort switch
        {
            NeedSortOption.CreatedAtAscending => query.OrderBy(n => n.CreatedAt),
            NeedSortOption.PriorityDescending => query.OrderByDescending(n => n.Priority).ThenBy(n => n.Deadline),
            NeedSortOption.DeadlineAscending => query.OrderBy(n => n.Deadline),
            _ => query.OrderByDescending(n => n.CreatedAt),
        };

        return await query.ToListAsync(cancellationToken);


    }

    public async Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default) =>
    await _context.Needs
        .AsNoTracking()
        .Select(n => n.Region)
        .Distinct()
        .OrderBy(r => r)
        .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<NeedNotificationRecipientDto>> GetNotificationRecipientsAsync(
    int categoryId,
    double latitude,
    double longitude,
    DateTime availableFrom,
    DateTime? availableTo,
    DateTime now,
    int excludeUserId,
    double radiusKm,
    int maxRecipients,
    CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(radiusKm);

        if (maxRecipients <= 0)
            return [];

        var box = GeoMath.BoundingBox(latitude, longitude, radiusKm);

        // Same suitability rules as the candidate search: open, same category, and the resource
        // is available no later than the deadline. Only critical needs trigger mail.
        var candidates = await _context.Needs
            .AsNoTracking()
            .Where(n =>
                n.CategoryId == categoryId &&
                (n.Status == NeedStatus.New || n.Status == NeedStatus.UnderReview) &&
                n.UserId != excludeUserId &&
                n.User.EmailNotifications &&
                (n.Deadline == null || (n.Deadline >= now && n.Deadline >= availableFrom)) &&
                n.Latitude >= box.MinLat && n.Latitude <= box.MaxLat &&
                n.Longitude >= box.MinLng && n.Longitude <= box.MaxLng)
            .Select(n => new { n.Id, n.UserId, n.Title, n.Latitude, n.Longitude })
            .ToListAsync(cancellationToken);

        return candidates
            .Select(n => new NeedNotificationRecipientDto(
                n.UserId,
                n.Id,
                n.Title,
                GeoMath.DistanceKm(latitude, longitude, n.Latitude, n.Longitude)))
            .Where(r => r.DistanceKm <= radiusKm)
            .GroupBy(r => r.UserId)                                  // one email per person
            .Select(g => g.OrderBy(r => r.DistanceKm).First())       // their nearest need
            .OrderBy(r => r.DistanceKm)
            .Take(maxRecipients)
            .ToList();
    }

}

