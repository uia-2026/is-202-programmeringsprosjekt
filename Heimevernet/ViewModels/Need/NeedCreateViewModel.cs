
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

    /// <summary>How urgent the need is. Required; defaults to Low.</summary>
    [Required(ErrorMessage = "Please select a priority.")]
    public NeedPriority Priority { get; set; } = NeedPriority.Low;

    /// <summary>Optional deadline. Must be in the future, except when editing and the deadline is unchanged.</summary>
    [DataType(DataType.DateTime)]
    [FutureDate(ErrorMessage = "Deadline must be in the future.")]
    public DateTime? Deadline { get; set; }

    /// <summary>Contact details. Required, max <see cref="FieldLimits.ContactPoint"/> characters.</summary>
    [Required(ErrorMessage = "Contact point is required.")]
    [StringLength(FieldLimits.ContactPoint, ErrorMessage = "Contact point cannot exceed {1} characters.")]
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>How many units are needed, 1 to <see cref="FieldLimits.MaxQuantity"/>.</summary>
    [Range(1, FieldLimits.MaxQuantity, ErrorMessage = "Quantity needed must be between 1 and 10000.")]
    public int QuantityNeeded { get; set; } = 1;
}