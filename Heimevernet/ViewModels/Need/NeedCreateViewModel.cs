using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;
using Heimevernet.ViewModels.Validation;

namespace Heimevernet.ViewModels.Need;

/// <summary>
/// View model used to create a new Need.
/// Contains validation attributes and a list of available categories for selection.
/// </summary>
public class NeedCreateViewModel
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

    [Required(ErrorMessage = "Please select a priority.")]
    public NeedPriority Priority { get; set; } = NeedPriority.Low;

    [DataType(DataType.DateTime)]
    [FutureDate(ErrorMessage = "Deadline must be in the future.")]
    public DateTime? Deadline { get; set; }

    [Required(ErrorMessage = "Contact point is required.")]
    [StringLength(FieldLimits.ContactPoint, ErrorMessage = "Contact point cannot exceed {1} characters.")]
    public string ContactPoint { get; set; } = string.Empty;

    [Range(1, FieldLimits.MaxQuantity, ErrorMessage = "Quantity needed must be between 1 and 10000.")]
    public int QuantityNeeded { get; set; } = 1;
}