using Heimevernet.Infrastructure.Geo;
using Heimevernet.Exceptions;
using Heimevernet.Mappers;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.Services.Rules;
using Heimevernet.ViewModels.Map;
using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;
using Heimevernet.DTOs.Resources;

namespace Heimevernet.Services;

/// <summary>
/// Application service handling operations related to Resources.
/// Orchestrates repository access, mapping and unit-of-work commits.
/// </summary>
public class ResourceService : IResourceService
{
    private readonly IResourceRepository _resourceRepository;
    private readonly ICategoryRepository _categoryRepository;
    private readonly IAttachmentService _attachmentService;
    private readonly IResourceMapper _resourceMapper;
    private readonly IMapMarkerMapper _mapMarkerMapper;
    private readonly IUnitOfWork _unitOfWork;
    private readonly INearbyResourceMapper _nearbyResourceMapper;

    public ResourceService(
        IResourceRepository resourceRepository,
        ICategoryRepository categoryRepository,
        IAttachmentService attachmentService,
        IResourceMapper resourceMapper,
        IMapMarkerMapper mapMarkerMapper,
        INearbyResourceMapper nearbyResourceMapper,
        IUnitOfWork unitOfWork)
    {
        _resourceRepository = resourceRepository;
        _categoryRepository = categoryRepository;
        _attachmentService = attachmentService;
        _resourceMapper = resourceMapper;
        _mapMarkerMapper = mapMarkerMapper;
        _nearbyResourceMapper = nearbyResourceMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ResourceViewModel>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        var resources =
            await _resourceRepository.GetAllAsync(cancellationToken);
        // Document params/returns via interface; implementation intentionally unchanged.
        return resources
            .Select(r => _resourceMapper.ToViewModel(r))
            .ToList();
    }

    public async Task<IReadOnlyList<ResourceViewModel>> GetFilteredAsync(
    ResourceFilterDto filter,
    CancellationToken cancellationToken = default)
    {
        var resources =
            await _resourceRepository.GetFilteredAsync(filter, cancellationToken);

        return resources
            .Select(r => _resourceMapper.ToViewModel(r))
            .ToList();
    }

    public async Task<IReadOnlyList<string>> GetDistinctRegionsAsync(CancellationToken cancellationToken = default) =>
    await _resourceRepository.GetDistinctRegionsAsync(cancellationToken);

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

    public async Task<IReadOnlyList<ResourceViewModel>> GetAvailableAsync(
    CancellationToken cancellationToken = default)
    {
        var resources =
            await _resourceRepository.GetAvailableAsync(cancellationToken);

        return resources
            .Select(r => _resourceMapper.ToViewModel(r))
            .ToList();
    }

    public async Task<int> CreateAsync(
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource =
            _resourceMapper.ToEntity(model, userId);

        await _resourceRepository.AddAsync(
            resource,
            cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return resource.Id;
    }

    public async Task<IReadOnlyList<MapMarkerViewModel>> GetMapMarkersAsync(
        GeoBox box, int? ownerUserId, bool availableOnly, int limit,
        CancellationToken cancellationToken = default)
    {
        var dtos = await _resourceRepository.GetMapMarkersAsync(box, ownerUserId, availableOnly, limit, cancellationToken);
        return _mapMarkerMapper.ToViewModels(dtos);
    }

    public async Task<IReadOnlyList<ResourceViewModel>> GetByUserIdAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resources =
            await _resourceRepository.GetByUserIdAsync(userId, cancellationToken);

        return resources
            .Select(r => _resourceMapper.ToViewModel(r))
            .ToList();
    }

    public async Task<IReadOnlyList<ResourceOwnerOptionViewModel>> GetOwnersAsync(
        CancellationToken cancellationToken = default)
    {
        var owners = await _resourceRepository.GetOwnersAsync(cancellationToken);

        return owners
            .Select(o => new ResourceOwnerOptionViewModel { UserId = o.UserId, Username = o.Username })
            .ToList();
    }

    public async Task<IReadOnlyList<NearbyResourceViewModel>>
     GetCompatibleNearbyAsync(
         NeedViewModel needViewModel,
         double radiusKm,
         int maxResults,
         CancellationToken cancellationToken = default)
    {
        var dtos = await _resourceRepository.GetCompatibleNearbyAsync(
            needViewModel.CategoryId,
            needViewModel.Latitude,
            needViewModel.Longitude,
            needViewModel.Deadline,
            radiusKm,
            maxResults,
            cancellationToken);

        return dtos
            .Select(_nearbyResourceMapper.ToViewModel)
            .ToList();
    }

    public async Task<(OwnedRecordMutationResult Result, string Message)> UpdateAsync(
        int id,
        ResourceCreateViewModel model,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (resource is null)
        {
            return (OwnedRecordMutationResult.NotFound, "Resource not found.");
        }

        var decision = OwnedRecordPolicy.CanEditResource(resource, model, userId);

        if (!decision.Allowed)
        {
            var result = decision.FailureCode == OwnedRecordFailureCode.NotOwner
                ? OwnedRecordMutationResult.Forbidden
                : OwnedRecordMutationResult.Conflict;

            return (result, decision.Message);
        }

        if (model.CategoryId is not { } categoryId
            || await _categoryRepository.GetByIdAsync(categoryId) is null)
        {
            return (OwnedRecordMutationResult.ValidationError, "Please select a valid category.");
        }

        _resourceMapper.ApplyEditableFields(resource, model);
        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return (OwnedRecordMutationResult.Conflict, "This resource changed while you were editing it. Please reload and try again.");
        }

        return (OwnedRecordMutationResult.Success, "Resource updated.");
    }

    public async Task<(OwnedRecordMutationResult Result, string Message)> DeleteAsync(
        int id,
        int userId,
        CancellationToken cancellationToken = default)
    {
        var resource = await _resourceRepository.GetByIdForUpdateAsync(id, cancellationToken);

        if (resource is null)
        {
            return (OwnedRecordMutationResult.NotFound, "Resource not found.");
        }

        var decision = OwnedRecordPolicy.CanDeleteResource(resource, userId);

        if (!decision.Allowed)
        {
            var result = decision.FailureCode == OwnedRecordFailureCode.NotOwner
                ? OwnedRecordMutationResult.Forbidden
                : OwnedRecordMutationResult.Conflict;

            return (result, decision.Message);
        }

        var storageKeys = resource.Attachments.Select(a => a.StorageKey).ToArray();
        _resourceRepository.Remove(resource);
        try
        {
            await _unitOfWork.CommitAsync(cancellationToken);
        }
        catch (ConcurrencyConflictException)
        {
            return (OwnedRecordMutationResult.Conflict, "This resource changed while you were deleting it. Please reload and try again.");
        }

        await _attachmentService.DeleteStorageObjectsAsync(storageKeys);

        return (OwnedRecordMutationResult.Success, "Resource deleted.");
    }
}