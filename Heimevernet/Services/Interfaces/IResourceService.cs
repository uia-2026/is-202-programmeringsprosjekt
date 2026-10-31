using Heimevernet.Infrastructure.Geo;
using Heimevernet.ViewModels.Map;
using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;
using Heimevernet.DTOs.Resources;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Service contract for operations related to Resources.
/// Provides methods for querying, retrieving and creating resources.
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Returns all resources mapped to view models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A read-only list of <see cref="ResourceViewModel"/>.</returns>
    Task<IReadOnlyList<ResourceViewModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns resources matching the given filter/sort criteria, mapped to view models.
    /// </summary>
    Task<IReadOnlyList<ResourceViewModel>> GetFilteredAsync(
        ResourceFilterDto filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single resource by id and returns it mapped to a view model, or null when not found.
    /// </summary>
    /// <param name="id">The id of the resource to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The mapped <see cref="ResourceViewModel"/> or null.</returns>
    Task<ResourceViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all Available resources, mapped to view models, filtered at the database level.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A read-only list of available <see cref="ResourceViewModel"/>.</returns>
    Task<IReadOnlyList<ResourceViewModel>> GetAvailableAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the resources owned by the given user, newest first. Filtered in the database.
    /// </summary>
    Task<IReadOnlyList<ResourceViewModel>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every user who owns at least one resource, for the owner filter.
    /// </summary>
    Task<IReadOnlyList<ResourceOwnerOptionViewModel>> GetOwnersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Resource from the provided create model for the specified user.
    /// </summary>
    /// <param name="model">The create view model containing resource details.</param>
    /// <param name="userId">The id of the user creating the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task<int> CreateAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MapMarkerViewModel>> GetMapMarkersAsync(
            GeoBox box, int? ownerUserId, bool availableOnly, int limit,
            CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NearbyResourceViewModel>>
       GetCompatibleNearbyAsync(
       NeedViewModel needViewModel,
       double radiusKm,
       int maxResults,
       CancellationToken cancellationToken = default);

    Task<(OwnedRecordMutationResult Result, string Message)> UpdateAsync(
        int id,
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);

    Task<(OwnedRecordMutationResult Result, string Message)> DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);
}
