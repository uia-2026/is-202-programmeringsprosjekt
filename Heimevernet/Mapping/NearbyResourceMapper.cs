using Heimevernet.DTOs.Resources;
using Heimevernet.Mappers.Interfaces;
using Heimevernet.ViewModels.Need;

namespace Heimevernet.Mappers;

public class NearbyResourceMapper : INearbyResourceMapper
{
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