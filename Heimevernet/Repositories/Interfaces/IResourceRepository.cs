using Heimevernet.Models;

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
    /// Adds a resource to the underlying data store.
    /// </summary>
    /// <param name="resource">The resource to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken = default);
}