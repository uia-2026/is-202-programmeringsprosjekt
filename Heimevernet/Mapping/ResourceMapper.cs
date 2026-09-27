using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Mappers;

/// <summary>
/// Mapping helpers to convert domain entities into view models used by the UI.
/// </summary>

public class ResourceMapper : IResourceMapper
{
    /// <summary>Converts a single <see cref="Resource"/> entity into a <see cref="ResourceViewModel"/>.</summary>
    public Resource ToEntity(
        ResourceCreateViewModel model,
        int userId)
    {
        return new Resource
        {
            UserId = userId,
            CategoryId = model.CategoryId!.Value,
            Title = model.Title.Trim(),
            Description = model.Description?.Trim(),
            Latitude = model.Latitude,
            Longitude = model.Longitude,
            Region = model.Region.Trim(),
            AvailableFrom = model.AvailableFrom,
            AvailableTo = model.AvailableTo,
            ContactPoint = model.ContactPoint.Trim()
        };
    }

    /// <summary>Converts a sequence of <see cref="Resource"/> entities into view models.</summary>

    public ResourceViewModel ToViewModel(Resource resource)
    {
        return new ResourceViewModel
        {
            Id = resource.Id,
            Title = resource.Title,
            Description = resource.Description,
            CategoryName = resource.Category.Name,
            Latitude = resource.Latitude,
            Longitude = resource.Longitude,
            Region = resource.Region,
            AvailableFrom = resource.AvailableFrom,
            AvailableTo = resource.AvailableTo,
            ContactPoint = resource.ContactPoint,
            Status = resource.Status,
            CreatedAt = resource.CreatedAt
        };
    }

    public IEnumerable<ResourceViewModel> ToViewModels(
        IEnumerable<Resource> resources)
    {
        return resources.Select(ToViewModel);
    }
}
