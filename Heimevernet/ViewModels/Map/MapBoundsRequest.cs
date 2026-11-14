using System.ComponentModel.DataAnnotations;
using Heimevernet.Infrastructure.Geo;

namespace Heimevernet.ViewModels.Map;

/// <summary>The visible map area, sent by the browser as query-string parameters.</summary>
public sealed class MapBoundsRequest : IValidatableObject
{
    /// <summary>Southern edge (min latitude). Required, -90 to 90.</summary>
    [Required, Range(-90, 90)] public double? South { get; set; }
    /// <summary>Northern edge (max latitude). Required, -90 to 90. Must not be less than <see cref="South"/>.</summary>
    [Required, Range(-90, 90)] public double? North { get; set; }
    /// <summary>Western edge (min longitude). Required, -180 to 180.</summary>
    [Required, Range(-180, 180)] public double? West { get; set; }
    /// <summary>Eastern edge (max longitude). Required, -180 to 180.</summary>
    [Required, Range(-180, 180)] public double? East { get; set; }

    /// <summary>
    /// Checks that <see cref="South"/> is not greater than <see cref="North"/>.
    /// </summary>
    /// <param name="context">The validation context (not used).</param>
    /// <returns>A validation error for <see cref="South"/>, or nothing when the area is valid.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (South > North)
            yield return new ValidationResult("South must not be greater than North.", new[] { nameof(South) });
    }

    /// <summary>
    /// Converts the request to a <see cref="GeoBox"/>. Only call this after validation has passed.
    /// </summary>
    /// <returns>The map area as a <see cref="GeoBox"/>.</returns>
    public GeoBox ToGeoBox() => new(South!.Value, North!.Value, West!.Value, East!.Value);
}