using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ResourceMapper _resourceMapper;

    public ResourceService(
        IResourceRepository resourceRepository,
        ResourceMapper resourceMapper)
    {
        _resourceRepository = resourceRepository;
        _resourceMapper = resourceMapper;
    }

    public async Task<IReadOnlyList<ResourceViewModel>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        var resources =
            await _resourceRepository.GetAllAsync(cancellationToken);

        return resources
            .Select(_resourceMapper.ToViewModel)
            .ToList();
    }

    public async Task<ResourceViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        var resource =
            await _resourceRepository.GetByIdAsync(
                id,
                cancellationToken);

        return resource == null
            ? null
            : _resourceMapper.ToViewModel(resource);
    }

    public async Task AddAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken)
    {
        var resource =
            _resourceMapper.ToEntity(model, userId);

        await _resourceRepository.AddAsync(
            resource,
            cancellationToken);
    }
}