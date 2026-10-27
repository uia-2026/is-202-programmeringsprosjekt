namespace Heimevernet.DTOs.Resources;

public record NearbyResourceDto(
    int Id,
    string Title,
    string CategoryName,
    string Region,
    string Status,
    string ContactPoint,
    int QuantityOffered,
    double DistanceKm
);