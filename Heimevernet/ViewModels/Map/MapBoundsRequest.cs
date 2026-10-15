using System.ComponentModel.DataAnnotations;
using Heimevernet.Infrastructure.Geo;

namespace Heimevernet.ViewModels.Map;

/// <summary>The visible map area, sent by the browser as query-string parameters.</summary>
public sealed class MapBoundsRequest : IValidatableObject
{
    [Required, Range(-90, 90)] public double? South { get; set; }
    [Required, Range(-90, 90)] public double? North { get; set; }
    [Required, Range(-180, 180)] public double? West { get; set; }
    [Required, Range(-180, 180)] public double? East { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (South > North)
            yield return new ValidationResult("South must not be greater than North.", new[] { nameof(South) });
    }

    public GeoBox ToGeoBox() => new(South!.Value, North!.Value, West!.Value, East!.Value);
}