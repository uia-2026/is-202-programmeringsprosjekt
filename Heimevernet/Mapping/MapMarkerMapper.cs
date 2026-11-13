using Heimevernet.DTOs.Map;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.ViewModels.Map;

namespace Heimevernet.Mappers;

/// <summary>
/// Default <see cref="IMapMarkerMapper"/> that copies each field one to one.
/// </summary>
public class MapMarkerMapper : IMapMarkerMapper
{
    /// <inheritdoc />
    public MapMarkerViewModel ToViewModel(MapMarkerDto dto)
    {
        return new MapMarkerViewModel(
            dto.Id,
            dto.Kind,
            dto.Title,
            dto.Category,
            dto.Description,
            dto.Region,
            dto.Priority,
            dto.IsUrgent,
            dto.Status,
            dto.When,
            dto.ContactInfo,
            dto.Quantity,
            dto.Latitude,
            dto.Longitude);
    }

    /// <inheritdoc />
    public IReadOnlyList<MapMarkerViewModel> ToViewModels(
        IEnumerable<MapMarkerDto> dtos)
    {
        return dtos.Select(ToViewModel).ToList();
    }
}