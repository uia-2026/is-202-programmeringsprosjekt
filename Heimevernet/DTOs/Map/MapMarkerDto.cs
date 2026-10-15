using Heimevernet.ViewModels.Map;

namespace Heimevernet.DTOs.Map;

public sealed record MapMarkerDto(
    int Id,
    MapMarkerKind Kind,
    string Title,
    string Category,
    string? Description,
    string Region,
    string? Priority,
    bool IsUrgent,
    string Status,
    DateTime? When,
    string ContactInfo,
    double Latitude,
    double Longitude);