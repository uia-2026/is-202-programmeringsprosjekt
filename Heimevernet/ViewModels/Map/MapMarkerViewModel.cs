using System.Text.Json.Serialization;

namespace Heimevernet.ViewModels.Map;

/// <summary>
/// What a map marker shows. Sent to the browser as text ("Need" or "Resource").
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MapMarkerKind
{
    /// <summary>The marker shows a need.</summary>
    Need,

    /// <summary>The marker shows a resource.</summary>
    Resource
}

/// <summary>
/// One marker on the map, sent to the browser as JSON.
/// </summary>
/// <param name="Id">ID of the need or resource.</param>
/// <param name="Kind">Whether the marker is a need or a resource.</param>
/// <param name="Title">Title shown in the popup.</param>
/// <param name="Category">Category name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Region">Region name.</param>
/// <param name="Priority">Priority name for needs; <see langword="null"/> for resources.</param>
/// <param name="IsUrgent"><see langword="true"/> for critical needs, so they get a special marker.</param>
/// <param name="Status">Status name.</param>
/// <param name="When">Deadline for needs, or available-from date for resources.</param>
/// <param name="ContactInfo">Contact point text.</param>
/// <param name="Quantity">Quantity needed or offered.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
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
    int Quantity,
    double Latitude,
    double Longitude);