namespace Heimevernet.DTOs.Search;

using System.Text.Json.Serialization;

/// <summary>
/// What a search hit points to. Sent as text ("Need" or "Resource").
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchResultType
{
    /// <summary>The hit is a need.</summary>
    Need,
    /// <summary>The hit is a resource.</summary>
    Resource
}

/// <summary>
/// One hit in the search results.
/// </summary>
/// <param name="Type">Whether the hit is a need or a resource.</param>
/// <param name="Id">ID of the need or resource.</param>
/// <param name="Title">Title of the item.</param>
/// <param name="Subtitle">Short extra info, e.g. priority, region and status.</param>
/// <param name="Latitude">Latitude, so the map can move to the hit.</param>
/// <param name="Longitude">Longitude, so the map can move to the hit.</param>
/// <param name="Url">Relative URL of the item's details page.</param>
public sealed record SearchResultDto(
    SearchResultType Type,
    int Id,
    string Title,
    string Subtitle,
    double Latitude,
    double Longitude,
    string Url
);

/// <summary>
/// Response body of the search API.
/// </summary>
/// <param name="Results">Need hits followed by resource hits.</param>
public sealed record SearchResponseDto(
    IReadOnlyList<SearchResultDto> Results
);