using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

/// <summary>
/// View model used to create a new Resource.
/// Contains validation attributes and a list of available categories for selection.
/// </summary>
public class ResourceCreateViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Please select a category.")]
    public int? CategoryId { get; set; }

    public IEnumerable<Category> AvailableCategories { get; set; }
        = Enumerable.Empty<Category>();

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(FieldLimits.Title, ErrorMessage = "Title cannot exceed {1} characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(FieldLimits.Description, ErrorMessage = "Description cannot exceed {1} characters.")]
    public string? Description { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [Required(ErrorMessage = "Bad location.")]
    [StringLength(FieldLimits.Region, ErrorMessage = "Region cannot exceed {1} characters.")]
    public string Region { get; set; } = string.Empty;

    [Required(ErrorMessage = "Available from is required.")]
    public DateTime AvailableFrom { get; set; } = DateTime.Now;

    public DateTime? AvailableTo { get; set; }

    [Required(ErrorMessage = "Contact point is required.")]
    [StringLength(FieldLimits.ContactPoint, ErrorMessage = "Contact point cannot exceed {1} characters.")]
    public string ContactPoint { get; set; } = string.Empty;

    public IFormFile? Image { get; set; }

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