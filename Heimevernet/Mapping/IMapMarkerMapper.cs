using Heimevernet.DTOs.Map;
using Heimevernet.ViewModels.Map;

namespace Heimevernet.Mappers.Interfaces;

public interface IMapMarkerMapper
{
    MapMarkerViewModel ToViewModel(MapMarkerDto dto);

    IReadOnlyList<MapMarkerViewModel> ToViewModels(
        IEnumerable<MapMarkerDto> dtos);
}