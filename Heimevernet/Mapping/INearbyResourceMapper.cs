using Heimevernet.DTOs.Resources;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers.Interfaces;

public interface INearbyResourceMapper
{
    NearbyResourceViewModel ToViewModel(NearbyResourceDto dto);
}