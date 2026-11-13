using Heimevernet.DTOs.Map;
using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.DTOs.Needs;
using Heimevernet.DTOs.Notifications;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access contract for Need entities.
/// Implementations provide methods to query and persist Need instances.
/// </summary>
public interface INeedRepository
{
    /// <summary>
    /// Returns all needs with their category, highest priority first, then by deadline.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The needs.</returns>
    Task<IEnumerable<Need>> GetAllAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds a need by ID, with its category loaded.
    /// </summary>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The need, or <see langword="null"/> if it does not exist.</returns>
    Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds a need by ID for editing, with its category and matches loaded and tracked.
    /// </summary>
    /// <param name="id">ID of the need.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The tracked need, or <see langword="null"/> if it does not exist.</returns>
    Task<Need?> GetByIdForUpdateAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a need to the context. It is saved on the next unit of work commit.
    /// </summary>
    /// <param name="need">The need to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the need has been added to the context.</returns>
    Task AddAsync(Need need, CancellationToken cancellationToken = default);
    /// <summary>
    /// Marks a need for deletion. It is deleted on the next unit of work commit.
    /// </summary>
    /// <param name="need">The need to remove.</param>
    void Remove(Need need);
    /// <summary>
    /// Returns open needs (New, UnderReview or Assigned) inside a map area, critical needs first.
    /// </summary>
    /// <param name="box">The map area.</param>
    /// <param name="limit">Maximum number of markers.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The markers as <see cref="MapMarkerDto"/> items.</returns>
    Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(GeoBox box, int limit, CancellationToken cancellationToken = default);
    /// <summary>
    /// Sets the version EF Core compares against on save. Used with the version from an edit form,
    /// so a save fails if someone else changed the need after the form was opened.
    /// </summary>
    /// <param name="need">The tracked need.</param>
    /// <param name="version">The version the user saw.</param>
    void SetOriginalVersion(Need need, int version);
    /// <summary>
    /// Forces EF Core to check the need's version on save, even if nothing else on it changed.
    /// </summary>
    /// <param name="need">The tracked need.</param>
    void MarkForConcurrencyCheck(Need need);
    /// <summary>
    /// Returns needs that match the filter (status, priority, category, region and search text), sorted as asked.
    /// </summary>
    /// <param name="filter">The filter and sort options.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matching needs, with their category loaded.</returns>
    Task<IReadOnlyList<Need>> GetFilteredAsync(NeedFilterDto filter, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns every region used by at least one need, sorted.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The region names.</returns>
    Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds users who own an open need that a newly registered resource could fill, within a radius,
    /// and who have email notifications on. One entry per user (their nearest need), nearest first.
    /// </summary>
    /// <param name="categoryId">Category of the resource.</param>
    /// <param name="latitude">Latitude of the resource.</param>
    /// <param name="longitude">Longitude of the resource.</param>
    /// <param name="availableFrom">When the resource becomes available.</param>
    /// <param name="availableTo">When the resource stops being available, or <see langword="null"/>.</param>
    /// <param name="now">The current UTC time; needs with a past deadline are skipped.</param>
    /// <param name="excludeUserId">The resource owner, who should not be notified.</param>
    /// <param name="radiusKm">Search radius in kilometres. Must be greater than 0.</param>
    /// <param name="maxRecipients">Maximum number of users to return.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The recipients as <see cref="NeedNotificationRecipientDto"/> items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radiusKm"/> is 0 or negative.</exception>
    Task<IReadOnlyList<NeedNotificationRecipientDto>> GetNotificationRecipientsAsync(
    int categoryId,
    double latitude,
    double longitude,
    DateTime availableFrom,
    DateTime? availableTo,
    DateTime now,
    int excludeUserId,
    double radiusKm,
    int maxRecipients,
    CancellationToken cancellationToken = default);
}