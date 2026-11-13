using Heimevernet.DTOs.Map;
using Heimevernet.ViewModels.Map;

namespace Heimevernet.Mappers.Interfaces;

/// <summary>
/// Maps map marker DTOs from the database to view models sent to the browser.
/// </summary>
public interface IMapMarkerMapper
{
    /// <summary>
    /// Maps one marker.
    /// </summary>
    /// <param name="dto">The marker data from the database.</param>
    /// <returns>The marker as a <see cref="MapMarkerViewModel"/>.</returns>
    MapMarkerViewModel ToViewModel(MapMarkerDto dto);

    /// <summary>
    /// Maps a list of markers, keeping the order.
    /// </summary>
    /// <param name="dtos">The marker data from the database.</param>
    /// <returns>The markers as view models.</returns>
    IReadOnlyList<MapMarkerViewModel> ToViewModels(
        IEnumerable<MapMarkerDto> dtos);
}