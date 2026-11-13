using Heimevernet.ViewModels.Map;

namespace Heimevernet.DTOs.Map;

/// <summary>
/// Marker data read from the database for one need or resource. Mapped to
/// <see cref="Heimevernet.ViewModels.Map.MapMarkerViewModel"/> before it is sent to the browser.
/// </summary>
/// <param name="Id">ID of the need or resource.</param>
/// <param name="Kind">Whether the marker is a need or a resource.</param>
/// <param name="Title">Title of the item.</param>
/// <param name="Category">Category name.</param>
/// <param name="Description">Optional description.</param>
/// <param name="Region">Region name.</param>
/// <param name="Priority">Priority name for needs; <see langword="null"/> for resources.</param>
/// <param name="IsUrgent"><see langword="true"/> for critical needs.</param>
/// <param name="Status">Status name.</param>
/// <param name="When">Deadline for needs, or available-from date for resources.</param>
/// <param name="ContactInfo">Contact point text.</param>
/// <param name="Quantity">Quantity needed or offered.</param>
/// <param name="Latitude">Latitude in decimal degrees.</param>
/// <param name="Longitude">Longitude in decimal degrees.</param>
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
    int Quantity,
    double Latitude,
    double Longitude);