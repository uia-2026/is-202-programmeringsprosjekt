using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

public sealed record ResourceViewModel(
    int Id,
    string Type,
    double Latitude,
    double Longitude,
    DateTime AvailableFrom,
    string ContactName,
    string ContactInfo,
    ResourceStatus Status
);
