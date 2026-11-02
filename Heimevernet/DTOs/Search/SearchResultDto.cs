namespace Heimevernet.DTOs.Search;

using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum SearchResultType
{
    Need,
    Resource
}

public sealed record SearchResultDto(
    SearchResultType Type,
    int Id,
    string Title,
    string Subtitle,
    double Latitude,
    double Longitude,
    string Url
);

public sealed record SearchResponseDto(
    IReadOnlyList<SearchResultDto> Results
);