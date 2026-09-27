using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Service abstraction for working with <see cref="Resource"/> entities.
/// Implementations should handle persistence concerns and mapping.
/// </summary>
public interface IResourceService
{

    /// <summary>Returns all resources as a read-only list.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<ResourceViewModel>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<ResourceViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task CreateAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default);
}
