using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access contract for Need entities.
/// Implementations provide methods to query and persist Need instances.
/// </summary>
public interface INeedRepository
{
    Task<IEnumerable<Need>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Need need, CancellationToken cancellationToken = default);
}

