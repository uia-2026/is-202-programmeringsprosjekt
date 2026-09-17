using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

public interface IResourceService
{
    Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken);
    Task AddAsync(Resource resource, CancellationToken cancellationToken);
}
