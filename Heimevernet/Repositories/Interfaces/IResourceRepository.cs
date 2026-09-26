using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IResourceRepository
{
    Task<IReadOnlyList<Resource>> GetAllAsync(
        CancellationToken cancellationToken = default);

    Task<Resource?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken = default);
}