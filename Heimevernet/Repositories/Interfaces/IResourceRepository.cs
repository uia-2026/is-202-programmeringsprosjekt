using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IResourceRepository
{
    Task<IReadOnlyList<Resource>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<Resource?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken);
}