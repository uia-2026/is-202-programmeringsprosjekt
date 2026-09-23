using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// Lightweight view model used to render resource information in the UI.
/// </summary>
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
