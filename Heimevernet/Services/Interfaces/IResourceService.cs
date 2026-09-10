using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

public interface IResourceService
{
    Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken);
}
