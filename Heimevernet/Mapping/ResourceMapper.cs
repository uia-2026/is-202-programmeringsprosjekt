using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Mapping;

public static class ResourceMapper
{
    public static ResourceViewModel ToViewModel(this Resource entity) => new(
        entity.Id,
        entity.Type,
        entity.Latitude,
        entity.Longitude,
        entity.AvailableFrom,
        entity.ContactName,
        entity.ContactInfo,
        entity.Status
    );

    public static IReadOnlyList<ResourceViewModel> ToViewModel(this IEnumerable<Resource> entities) =>
        entities.Select(ToViewModel).ToList();
}
