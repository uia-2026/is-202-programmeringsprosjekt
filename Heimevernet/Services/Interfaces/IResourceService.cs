using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Service abstraction for working with <see cref="Resource"/> entities.
/// Implementations should handle persistence concerns and mapping.
/// </summary>
public interface IResourceService
{
    /// <summary>Returns all resources as a read-only list.</summary>
    /// <param name="cancellationToken">Token used to cancel the operation.</param>
    Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>Adds a resource to the underlying store.</summary>
    /// <param name="resource">Resource to add.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddAsync(Resource resource, CancellationToken cancellationToken);
}
