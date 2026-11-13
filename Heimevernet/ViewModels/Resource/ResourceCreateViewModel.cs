using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model used to create a new Resource.
/// Contains validation attributes and a list of available categories for selection.
/// </summary>
public class ResourceCreateViewModel : IValidatableObject
{
    /// <summary>Chosen category. Required, and must exist (checked by the service).</summary>
    [Required(ErrorMessage = "Please select a category.")]
    public int? CategoryId { get; set; }

    /// <summary>Categories for the drop-down list. Not posted back; filled again by the controller.</summary>
    public IEnumerable<Category> AvailableCategories { get; set; }
        = Enumerable.Empty<Category>();

    /// <summary>Short title. Required, max <see cref="FieldLimits.Title"/> characters.</summary>
    [Required(ErrorMessage = "Title is required.")]
    [StringLength(FieldLimits.Title, ErrorMessage = "Title cannot exceed {1} characters.")]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description, max <see cref="FieldLimits.Description"/> characters.</summary>
    [StringLength(FieldLimits.Description, ErrorMessage = "Description cannot exceed {1} characters.")]
    public string? Description { get; set; }

    /// <summary>Latitude picked on the map, -90 to 90.</summary>
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    /// <summary>Longitude picked on the map, -180 to 180.</summary>
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    /// <summary>
    /// Region name. Set by the controller from the map location through reverse-geocoding;
    /// a validation error is added if the location is at sea or cannot be looked up.
    /// </summary>
    [Required(ErrorMessage = "Bad location.")]
    [StringLength(FieldLimits.Region, ErrorMessage = "Region cannot exceed {1} characters.")]
    public string Region { get; set; } = string.Empty;

    /// <summary>When the resource becomes available. Required; defaults to now.</summary>
    [Required(ErrorMessage = "Available from is required.")]
    public DateTime AvailableFrom { get; set; } = DateTime.Now;

    /// <summary>When the resource stops being available. Optional, but must not be before <see cref="AvailableFrom"/>.</summary>
    public DateTime? AvailableTo { get; set; }

    /// <summary>Contact details. Required, max <see cref="FieldLimits.ContactPoint"/> characters.</summary>
    [Required(ErrorMessage = "Contact point is required.")]
    [StringLength(FieldLimits.ContactPoint, ErrorMessage = "Contact point cannot exceed {1} characters.")]
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>How many units are offered, 1 to <see cref="FieldLimits.MaxQuantity"/>.</summary>
    [Range(1, FieldLimits.MaxQuantity, ErrorMessage = "Quantity offered must be between 1 and 10000.")]
    public int QuantityOffered { get; set; } = 1;

    /// <summary>Optional photo uploaded with the form. Only used on create.</summary>
    public IFormFile? Image { get; set; }

    /// <summary>
    /// Checks that <see cref="AvailableTo"/> is not before <see cref="AvailableFrom"/>.
    /// </summary>
    /// <param name="validationContext">The validation context (not used).</param>
    /// <returns>A validation error for <see cref="AvailableTo"/>, or nothing when the dates are valid.</returns>
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (AvailableTo.HasValue && AvailableTo.Value < AvailableFrom)
        {
            yield return new ValidationResult(
                "Available until cannot be before available from.",
                [nameof(AvailableTo)]);
        }
    }
}