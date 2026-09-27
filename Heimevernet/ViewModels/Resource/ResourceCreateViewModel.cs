using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Resource;

public class ResourceCreateViewModel
{
    [Required(ErrorMessage = "Please select a category.")]
    public int? CategoryId { get; set; }

    public IEnumerable<Category> AvailableCategories { get; set; }
        = Enumerable.Empty<Category>();

    [Required(ErrorMessage = "Title is required.")]
    [StringLength(150, ErrorMessage = "Title cannot exceed 150 characters.")]
    public string Title { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters.")]
    public string? Description { get; set; }

    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90.")]
    public double Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180.")]
    public double Longitude { get; set; }

    [Required(ErrorMessage = "Region is required.")]
    [StringLength(100, ErrorMessage = "Region cannot exceed 100 characters.")]
    public string Region { get; set; } = string.Empty;

    [Required(ErrorMessage = "Available from is required.")]
    public DateTime AvailableFrom { get; set; } = DateTime.Now;

    public DateTime? AvailableTo { get; set; }

    [Required(ErrorMessage = "Contact point is required.")]
    [StringLength(200, ErrorMessage = "Contact point cannot exceed 200 characters.")]
    public string ContactPoint { get; set; } = string.Empty;
}