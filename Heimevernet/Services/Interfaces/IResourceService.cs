using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services.Interfaces;

public interface IResourceService
{
    Task<IReadOnlyList<ResourceViewModel>> GetAllAsync(
        CancellationToken cancellationToken);

    Task<ResourceViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken);

    Task AddAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken);
}