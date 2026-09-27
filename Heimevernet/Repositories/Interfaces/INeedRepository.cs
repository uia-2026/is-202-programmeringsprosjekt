using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface INeedRepository
{
    Task<IEnumerable<Need>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(Need need, CancellationToken cancellationToken = default);
}

