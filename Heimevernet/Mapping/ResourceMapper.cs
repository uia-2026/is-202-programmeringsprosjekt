using Heimevernet.Models;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Mapping;

/// <summary>
/// Mapping helpers to convert domain entities into view models used by the UI.
/// </summary>
public static class ResourceMapper
{
    /// <summary>Converts a single <see cref="Resource"/> entity into a <see cref="ResourceViewModel"/>.</summary>
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

    /// <summary>Converts a sequence of <see cref="Resource"/> entities into view models.</summary>
    public static IReadOnlyList<ResourceViewModel> ToViewModel(this IEnumerable<Resource> entities) =>
        entities.Select(ToViewModel).ToList();
}
