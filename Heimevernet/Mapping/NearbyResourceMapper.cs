using Heimevernet.DTOs.Resources;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers;

/// <summary>
/// Default <see cref="INearbyResourceMapper"/> that copies each field one to one.
/// </summary>
public class NearbyResourceMapper : INearbyResourceMapper
{
    /// <inheritdoc />
    public NearbyResourceViewModel ToViewModel(NearbyResourceDto dto)
    {
        return new NearbyResourceViewModel(
            Id: dto.Id,
            Title: dto.Title,
            CategoryName: dto.CategoryName,
            Region: dto.Region,
            Status: dto.Status,
            ContactPoint: dto.ContactPoint,
            QuantityOffered: dto.QuantityOffered,
            DistanceKm: dto.DistanceKm);
    }
}