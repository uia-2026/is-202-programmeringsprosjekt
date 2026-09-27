using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Mappers;

/// <summary>
/// Mapping contract for Resource-related conversions between domain entities and view models.
/// Implementations should provide conversions for create models and view models.
/// </summary>
public interface IResourceMapper
{
    /// <summary>
    /// Maps a create view model to a Resource entity.
    /// </summary>
    /// <param name="model">The create view model with input data.</param>
    /// <param name="userId">The id of the user creating the resource.</param>
    /// <returns>A mapped <see cref="Resource"/> entity.</returns>
    Resource ToEntity(ResourceCreateViewModel model, int userId);

    /// <summary>
    /// Maps a Resource entity to a ResourceViewModel.
    /// </summary>
    /// <param name="resource">The resource entity to map.</param>
    /// <returns>A <see cref="ResourceViewModel"/> instance.</returns>
    ResourceViewModel ToViewModel(Resource resource);

    /// <summary>
    /// Maps a sequence of Resource entities to view models.
    /// </summary>
    /// <param name="resources">The sequence of resources to map.</param>
    /// <returns>Sequence of <see cref="ResourceViewModel"/>.</returns>
    IEnumerable<ResourceViewModel> ToViewModels(IEnumerable<Resource> resources);
}
