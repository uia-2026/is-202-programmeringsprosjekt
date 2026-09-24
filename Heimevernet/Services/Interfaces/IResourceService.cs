using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services.Interfaces;

public interface IResourceService
{
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