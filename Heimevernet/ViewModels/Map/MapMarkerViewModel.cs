using System.Text.Json.Serialization;

namespace Heimevernet.ViewModels.Map;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MapMarkerKind { Need, Resource }

public sealed record MapMarkerViewModel(
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


