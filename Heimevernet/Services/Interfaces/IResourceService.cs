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
    /// <param name="filter">Filter and sort options from the resources list page.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The matching resources as <see cref="ResourceViewModel"/> items.</returns>
    Task<IReadOnlyList<ResourceViewModel>> GetFilteredAsync(
        ResourceFilterDto filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every region used by at least one resource, for the region filter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The region names, without duplicates.</returns>
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
    /// <param name="userId">ID of the owner.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The user's resources as <see cref="ResourceViewModel"/> items.</returns>
    Task<IReadOnlyList<ResourceViewModel>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every user who owns at least one resource, for the owner filter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>One option per owner, for a drop-down list.</returns>
    Task<IReadOnlyList<ResourceOwnerOptionViewModel>> GetOwnersAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Resource from the provided create model for the specified user.
    /// </summary>
    /// <param name="model">The create view model containing resource details.</param>
    /// <param name="userId">The id of the user creating the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The ID of the new resource.</returns>
    Task<int> CreateAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns map markers for resources inside the given map area.
    /// </summary>
    /// <param name="box">The visible map area.</param>
    /// <param name="ownerUserId">Only include resources owned by this user, or <see langword="null"/> for all owners.</param>
    /// <param name="availableOnly">When <see langword="true"/>, only resources with status Available are included.</param>
    /// <param name="limit">Maximum number of markers to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The markers as <see cref="MapMarkerViewModel"/> items.</returns>
    Task<IReadOnlyList<MapMarkerViewModel>> GetMapMarkersAsync(
            GeoBox box, int? ownerUserId, bool availableOnly, int limit,
            CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds available resources that fit a need (same category and a matching availability period)
    /// within a radius of the need, nearest first.
    /// </summary>
    /// <param name="needViewModel">The need to find resources for.</param>
    /// <param name="radiusKm">Search radius in kilometres.</param>
    /// <param name="maxResults">Maximum number of resources to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The nearby resources as <see cref="NearbyResourceViewModel"/> items.</returns>
    Task<IReadOnlyList<NearbyResourceViewModel>>
       GetCompatibleNearbyAsync(
       NeedViewModel needViewModel,
       double radiusKm,
       int maxResults,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a resource owned by the user. Category, location and availability cannot be changed
    /// while the resource has pending or assigned matches
    /// (see <see cref="Heimevernet.Services.Rules.OwnedRecordPolicy"/>).
    /// </summary>
    /// <param name="id">ID of the resource to update.</param>
    /// <param name="model">The submitted form with the new values.</param>
    /// <param name="userId">ID of the user editing. Must own the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result code and a user-facing message.</returns>
    Task<(OwnedRecordMutationResult Result, string Message)> UpdateAsync(
        int id,
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a resource owned by the user, together with its stored images.
    /// Resources that have any matches cannot be deleted.
    /// </summary>
    /// <param name="id">ID of the resource to delete.</param>
    /// <param name="userId">ID of the user deleting. Must own the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result code and a user-facing message.</returns>
    Task<(OwnedRecordMutationResult Result, string Message)> DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);
}