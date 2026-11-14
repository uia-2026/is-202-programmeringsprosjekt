using Heimevernet.Infrastructure.Geo;
using Heimevernet.Models;
using Heimevernet.ViewModels.Map;
using Heimevernet.ViewModels.Need;
using Heimevernet.DTOs.Needs;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Service contract for operations related to Needs.
/// Provides methods for creating, querying and retrieving detailed needs.
/// </summary>
public interface INeedService
{
    /// <summary>
    /// Creates a new Need from the provided create model for the specified user.
    /// </summary>
    /// <param name="model">The create view model containing need details.</param>
    /// <param name="userId">The id of the user creating the need.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The ID of the new need.</returns>
    Task<int> CreateAsync(NeedCreateViewModel model, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all needs mapped to view models.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A sequence of <see cref="NeedViewModel"/>.</returns>
    Task<IEnumerable<NeedViewModel>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns needs that match the given filter and sort options.
    /// </summary>
    /// <param name="filter">Filter and sort options from the needs list page.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The matching needs as <see cref="NeedViewModel"/> items.</returns>
    Task<IEnumerable<NeedViewModel>> GetFilteredAsync(NeedFilterDto filter, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns every region used by at least one need, for the region filter.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The region names, without duplicates.</returns>
    Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns summary view models for all needs.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A sequence of <see cref="NeedSummaryViewModel"/>.</returns>
    Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves a single need by id and returns it mapped to a view model, or null when not found.
    /// </summary>
    /// <param name="id">The id of the need to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The mapped <see cref="NeedViewModel"/> or null.</returns>
    Task<NeedViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);


    /// <summary>
    /// Returns map markers for needs inside the given map area.
    /// </summary>
    /// <param name="box">The visible map area.</param>
    /// <param name="limit">Maximum number of markers to return.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The markers as <see cref="MapMarkerViewModel"/> items.</returns>
    Task<IReadOnlyList<MapMarkerViewModel>> GetMapMarkersAsync(GeoBox box, int limit, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates a need owned by the user. Category, location and deadline cannot be changed while
    /// the need has pending or assigned matches, and resolved needs cannot be edited
    /// (see <see cref="Heimevernet.Services.Rules.OwnedRecordPolicy"/>).
    /// </summary>
    /// <param name="id">ID of the need to update.</param>
    /// <param name="model">The submitted form with the new values.</param>
    /// <param name="userId">ID of the user editing. Must own the need.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result code and a user-facing message.</returns>
    Task<(OwnedRecordMutationResult Result, string Message)> UpdateAsync(
        int id,
        NeedCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a need owned by the user. Needs that have any matches cannot be deleted.
    /// </summary>
    /// <param name="id">ID of the need to delete.</param>
    /// <param name="userId">ID of the user deleting. Must own the need.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result code and a user-facing message.</returns>
    Task<(OwnedRecordMutationResult Result, string Message)> DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks an assigned need as resolved, and marks its assigned matches as resolved too.
    /// </summary>
    /// <param name="id">ID of the need to resolve.</param>
    /// <param name="userId">ID of the user. Must own the need.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A result code and a user-facing message.</returns>
    Task<(OwnedRecordMutationResult Result, string Message)> ResolveAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default);
}