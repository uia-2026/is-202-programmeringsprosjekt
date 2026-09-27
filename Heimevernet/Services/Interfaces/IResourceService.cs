using Heimevernet.ViewModels.Resource;

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
    /// Retrieves a single resource by id and returns it mapped to a view model, or null when not found.
    /// </summary>
    /// <param name="id">The id of the resource to retrieve.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The mapped <see cref="ResourceViewModel"/> or null.</returns>
    Task<ResourceViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new Resource from the provided create model for the specified user.
    /// </summary>
    /// <param name="model">The create view model containing resource details.</param>
    /// <param name="userId">The id of the user creating the resource.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task CreateAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);
}
