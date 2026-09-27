using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Mappers;

/// <summary>
/// Maps between Resource domain entities and Resource view models.
/// Handles mapping for create models, detailed view models and collections.
/// </summary>
public class ResourceMapper : IResourceMapper
{
    /// <summary>
    /// Maps a <see cref="ResourceCreateViewModel"/> to a <see cref="Resource"/> entity.
    /// </summary>
    /// <param name="model">Create view model to map from.</param>
    /// <param name="userId">Id of the user creating the resource.</param>
    /// <returns>A populated <see cref="Resource"/> entity.</returns>
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

    /// <summary>
    /// Maps a <see cref="Resource"/> entity to a <see cref="ResourceViewModel"/>.
    /// </summary>
    /// <param name="resource">Entity to map.</param>
    /// <returns>A <see cref="ResourceViewModel"/>.</returns>
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

    /// <summary>
    /// Maps a sequence of <see cref="Resource"/> entities to view models.
    /// </summary>
    /// <param name="resources">Sequence of resources to map.</param>
    /// <returns>Sequence of <see cref="ResourceViewModel"/>.</returns>
    public IEnumerable<ResourceViewModel> ToViewModels(
        IEnumerable<Resource> resources)
    {
        return resources.Select(ToViewModel);
    }

}