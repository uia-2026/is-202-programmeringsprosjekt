using Heimevernet.Mappers;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services;

public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly IResourceMapper _resourceMapper;
    private readonly IUnitOfWork _unitOfWork;

    public ResourceService(
        IResourceRepository resourceRepository,
        IResourceMapper resourceMapper,
        IUnitOfWork unitOfWork)
    {
        _resourceRepository = resourceRepository;
        _resourceMapper = resourceMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ResourceViewModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var resources =
            await _resourceRepository.GetAllAsync(cancellationToken);

        return resources
            .Select(_resourceMapper.ToViewModel)
            .ToList();
    }

    public async Task<ResourceViewModel?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var resource =
            await _resourceRepository.GetByIdAsync(
                id,
                cancellationToken);

        return resource == null
            ? null
            : _resourceMapper.ToViewModel(resource);
    }

    public async Task CreateAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource =
            _resourceMapper.ToEntity(model, userId);

        await _resourceRepository.AddAsync(
            resource,
            cancellationToken);
        await _unitOfWork.CommitAsync();
    }
}