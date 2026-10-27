using Heimevernet.Data;
using Heimevernet.DTOs.Map;
using Heimevernet.DTOs.Resources;
using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.ViewModels.Map;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

/// <summary>
/// Repository implementation for Resources.
/// Provides data access methods used by services and controllers.
/// </summary>
public class ResourceRepository : IResourceRepository
{
    private readonly AppDbContext _context;

    public ResourceRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Retrieves all resources from the database ordered by creation time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>A read-only list of <see cref="Resource"/> entities.</returns>
    public async Task<IReadOnlyList<Resource>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Retrieves a single Resource by id, including its Category navigation property.
    /// </summary>
    /// <param name="id">The resource id to look up.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matching <see cref="Resource"/> or null.</returns>
    public async Task<Resource?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.Category)
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);
    }

    public async Task<Resource?> GetByIdForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.Category)
            .Include(r => r.Attachments)
            .Include(r => r.Matches)
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);
    }

    /// <summary>
    /// Retrieves all Available resources from the database, including Category, ordered by creation time.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>A read-only list of available <see cref="Resource"/> entities.</returns>
    public async Task<IReadOnlyList<Resource>> GetAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.Resources
            .Include(r => r.Category)
            .Where(r => r.Status == ResourceStatus.Available)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }


    /// <summary>
    /// Adds a Resource entity to the context for insertion.
    /// </summary>
    /// <param name="resource">The resource entity to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken = default)
    {
        await _context.Resources.AddAsync(
            resource,
            cancellationToken);
    }

    public void Remove(Resource resource)
    {
        _context.Resources.Remove(resource);
    }

    public async Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
        GeoBox box, int? ownerUserId, bool availableOnly, int limit,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Resources
            .AsNoTracking()
            .Where(r => r.Latitude >= box.MinLat && r.Latitude <= box.MaxLat
                     && r.Longitude >= box.MinLng && r.Longitude <= box.MaxLng);

        if (ownerUserId is { } owner)
            query = query.Where(r => r.UserId == owner);

        if (availableOnly)
            query = query.Where(r => r.Status == ResourceStatus.Available);

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Take(limit)
            .Select(r => new MapMarkerDto(
                r.Id, MapMarkerKind.Resource, r.Title, r.Category.Name, r.Description, r.Region,
                null, false, r.Status.ToString(), r.AvailableFrom, r.ContactPoint, r.QuantityOffered, r.Latitude, r.Longitude))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NearbyResourceDto>> GetCompatibleNearbyAsync(
    int categoryId,
    double latitude,
    double longitude,
    DateTime? deadline,
    double radiusKm,
    int maxResults,
    CancellationToken cancellationToken = default)
    {
        if (radiusKm <= 0)
            throw new ArgumentOutOfRangeException(nameof(radiusKm));

        if (maxResults <= 0)
            return [];

        var box = GeoMath.BoundingBox(latitude, longitude, radiusKm);

        var candidates = await _context.Resources
            .AsNoTracking()
            .Where(r =>
                r.CategoryId == categoryId &&
                r.Status == ResourceStatus.Available &&
                (deadline == null ||
                (r.AvailableFrom <= deadline &&
                (r.AvailableTo == null || r.AvailableTo >= deadline))) &&
                r.Latitude >= box.MinLat &&
                r.Latitude <= box.MaxLat &&
                r.Longitude >= box.MinLng &&
                r.Longitude <= box.MaxLng)
            .Select(r => new
            {
                r.Id,
                r.Title,
                CategoryName = r.Category.Name,
                r.Region,
                r.Status,
                r.ContactPoint,
                r.QuantityOffered,
                r.Latitude,
                r.Longitude
            })
            .ToListAsync(cancellationToken);

        return candidates
            .Select(r => new NearbyResourceDto(
                Id: r.Id,
                Title: r.Title,
                CategoryName: r.CategoryName,
                Region: r.Region,
                Status: r.Status.ToString(),
                ContactPoint: r.ContactPoint,
                QuantityOffered: r.QuantityOffered,
                DistanceKm: GeoMath.DistanceKm(
                    latitude,
                    longitude,
                    r.Latitude,
                    r.Longitude)))
            .Where(r => r.DistanceKm <= radiusKm)
            .OrderBy(r => r.DistanceKm)
            .Take(maxResults)
            .ToList();
    }

    public async Task<IReadOnlyList<Resource>> GetCandidateResourcesAsync(
        int needId,
        int categoryId,
        DateTime? deadline,
        DateTime now,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Resources
            .AsNoTracking()
            .Include(r => r.Category)
            .Where(r => r.CategoryId == categoryId
                     && r.Status == ResourceStatus.Available
                     && (r.AvailableTo == null || r.AvailableTo >= now)
                     && !r.Matches.Any(m => m.NeedId == needId));

        if (deadline is { } d)
            query = query.Where(r => r.AvailableFrom <= d);

        return await query.ToListAsync(cancellationToken);
    }

    public void SetOriginalVersion(Resource resource, int version)
    {
        _context.Entry(resource)
            .Property(r => r.Version)
            .OriginalValue = version;
    }

    public void MarkForConcurrencyCheck(Resource resource)
    {
        _context.Entry(resource)
            .Property(r => r.Version)
            .OriginalValue = resource.Version;
    }
}
