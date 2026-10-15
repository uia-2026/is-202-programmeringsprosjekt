namespace Heimevernet.ViewModels.Map;

public record MapMarkerViewModel(
    int Id,
    string Kind,
    string Title,
    string Category,
    string? Description,
    string Region,
    string Priority,
    string Status,
    string MarkerColor,
    DateTime? When,
    string ContactInfo,
    double Latitude,
    double Longitude

);



    