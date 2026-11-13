using Heimevernet.DTOs.Resources;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers.Interfaces;

/// <summary>
/// Maps nearby-resource DTOs to view models for the need details page.
/// </summary>
public interface INearbyResourceMapper
{
    /// <summary>
    /// Maps one nearby resource.
    /// </summary>
    /// <param name="dto">The data from the database.</param>
    /// <returns>The resource as a <see cref="NearbyResourceViewModel"/>.</returns>
    NearbyResourceViewModel ToViewModel(NearbyResourceDto dto);
}