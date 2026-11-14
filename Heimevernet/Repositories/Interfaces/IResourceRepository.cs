using Heimevernet.DTOs.Map;
using Heimevernet.DTOs.Notifications;
using Heimevernet.DTOs.Resources;
using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access contract for Resource entities.
/// Implementations provide methods to query and persist Resource instances.
/// </summary>
public interface IResourceRepository
{
    /// <summary>
    /// Retrieves all resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>A read-only list of <see cref="Resource"/> entities.</returns>
    Task<IReadOnlyList<Resource>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a resource by id.
    /// </summary>
    /// <param name="id">The resource id.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matching <see cref="Resource"/> or null.</returns>
    Task<Resource?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds a resource by ID for editing or deleting, with its category, attachments and matches loaded and tracked.
    /// </summary>
    /// <param name="id">ID of the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The tracked resource, or <see langword="null"/> if it does not exist.</returns>
    Task<Resource?> GetByIdForUpdateAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all resources with status Available, filtered at the database level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>A read-only list of available <see cref="Resource"/> entities.</returns>
    Task<IReadOnlyList<Resource>> GetAvailableAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the resources owned by a user, newest first. The owner filter runs in the database.
    /// </summary>
    /// <param name="userId">ID of the owner.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The user's resources.</returns>
    Task<IReadOnlyList<Resource>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every user who owns at least one resource, sorted by username.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The owners as <see cref="ResourceOwnerDto"/> items (ID and username only).</returns>
    Task<IReadOnlyList<ResourceOwnerDto>> GetOwnersAsync(
        CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a resource to the underlying data store.
    /// </summary>
    /// <param name="resource">The resource to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the resource has been added to the context.</returns>
    Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a resource for deletion. It is deleted on the next unit of work commit.
    /// </summary>
    /// <param name="resource">The resource to remove.</param>
    void Remove(Resource resource);

    /// <summary>
    /// Returns resources inside a map area, newest first.
    /// </summary>
    /// <param name="box">The map area.</param>
    /// <param name="ownerUserId">Only include resources owned by this user, or <see langword="null"/> for all owners.</param>
    /// <param name="availableOnly">When <see langword="true"/>, only Available resources are included.</param>
    /// <param name="limit">Maximum number of markers.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The markers as <see cref="MapMarkerDto"/> items.</returns>
    Task<IReadOnlyList<MapMarkerDto>> GetMapMarkersAsync(
            GeoBox box, int? ownerUserId, bool availableOnly, int limit,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds Available resources of a category within a radius of a point, nearest first.
    /// When a deadline is given, the resource must be available on that date.
    /// </summary>
    /// <param name="categoryId">Category the resource must have.</param>
    /// <param name="latitude">Latitude of the centre point.</param>
    /// <param name="longitude">Longitude of the centre point.</param>
    /// <param name="deadline">The need's deadline, or <see langword="null"/> for no date check.</param>
    /// <param name="radiusKm">Search radius in kilometres. Must be greater than 0.</param>
    /// <param name="maxResults">Maximum number of resources to return.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The resources as <see cref="NearbyResourceDto"/> items, with distance in km.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radiusKm"/> is 0 or negative.</exception>
    Task<IReadOnlyList<NearbyResourceDto>> GetCompatibleNearbyAsync(
        int categoryId,
        double latitude,
        double longitude,
        DateTime? deadline,
        double radiusKm,
        int maxResults,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns resources that can be proposed for a need: same category, Available, not expired,
    /// available before the deadline, and not already matched to this need.
    /// </summary>
    /// <param name="needId">ID of the need.</param>
    /// <param name="categoryId">Category of the need.</param>
    /// <param name="deadline">The need's deadline, or <see langword="null"/>.</param>
    /// <param name="now">The current UTC time.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The candidate resources, with their category loaded.</returns>
    Task<IReadOnlyList<Resource>> GetCandidateResourcesAsync(
            int needId,
            int categoryId,
            DateTime? deadline,
            DateTime now,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the version EF Core compares against on save. Used with the version from an edit form,
    /// so a save fails if someone else changed the resource after the form was opened.
    /// </summary>
    /// <param name="resource">The tracked resource.</param>
    /// <param name="version">The version the user saw.</param>
    void SetOriginalVersion(Resource resource, int version);
    /// <summary>
    /// Forces EF Core to check the resource's version on save, even if nothing else on it changed.
    /// </summary>
    /// <param name="resource">The tracked resource.</param>
    void MarkForConcurrencyCheck(Resource resource);

    /// <summary>
    /// Returns resources that match the filter, sorted as asked.
    /// </summary>
    /// <param name="filter">The filter and sort options.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The matching resources, with their category loaded.</returns>
    Task<IReadOnlyList<Resource>> GetFilteredAsync(ResourceFilterDto filter, CancellationToken cancellationToken = default);
    /// <summary>
    /// Returns every region used by at least one resource, sorted.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The region names.</returns>
    Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// Providers who have an available, unexpired resource of the given category within radiusKm of the point,
    /// and who have email notifications on. One entry per provider, with their nearest resource, nearest first.
    /// </summary>
    /// <param name="categoryId">Category of the need.</param>
    /// <param name="latitude">Latitude of the need.</param>
    /// <param name="longitude">Longitude of the need.</param>
    /// <param name="deadline">The need's deadline, or <see langword="null"/>.</param>
    /// <param name="now">The current UTC time; expired resources are skipped.</param>
    /// <param name="excludeUserId">The need owner, who should not be notified.</param>
    /// <param name="radiusKm">Search radius in kilometres. Must be greater than 0.</param>
    /// <param name="maxRecipients">Maximum number of providers to return.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The recipients as <see cref="NotificationRecipientDto"/> items.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="radiusKm"/> is 0 or negative.</exception>
    Task<IReadOnlyList<NotificationRecipientDto>> GetNotificationRecipientsAsync(
        int categoryId,
        double latitude,
        double longitude,
        DateTime? deadline,
        DateTime now,
        int excludeUserId,
        double radiusKm,
        int maxRecipients,
        CancellationToken cancellationToken = default);
}