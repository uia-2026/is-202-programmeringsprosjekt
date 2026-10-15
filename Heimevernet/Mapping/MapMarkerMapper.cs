using Heimevernet.DTOs.Map;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.ViewModels.Map;

namespace Heimevernet.Mappers;

public class MapMarkerMapper : IMapMarkerMapper
{
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
            dto.Latitude,
            dto.Longitude);
    }

    public IReadOnlyList<MapMarkerViewModel> ToViewModels(
        IEnumerable<MapMarkerDto> dtos)
    {
        return dtos.Select(ToViewModel).ToList();
    }
}