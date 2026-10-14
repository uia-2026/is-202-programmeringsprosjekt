namespace Heimevernet.ViewModels.Need;


public record NearbyResourceViewModel(
    int Id,
    string Title,
    string CategoryName,
    string Region,
    string Status,
    string ContactPoint,
    double DistanceKm
);