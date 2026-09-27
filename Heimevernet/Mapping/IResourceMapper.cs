using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Mappers;

public interface IResourceMapper
{
    Resource ToEntity(ResourceCreateViewModel model, int userId);
    ResourceViewModel ToViewModel(Resource resource);
    IEnumerable<ResourceViewModel> ToViewModels(IEnumerable<Resource> resources);
}
