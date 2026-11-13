namespace Heimevernet.Models;


/// <summary>
/// Domain model representing a category that applies to Needs or Resources.
/// Used to group items and limit where categories can be applied.
/// </summary>
public class Category
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>Display name of the category, e.g. "Water". Unique; max <see cref="FieldLimits.CategoryName"/> characters.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Whether the category can be used for needs, resources or both.</summary>
    public CategoryAppliesTo AppliesTo { get; set; }

    /// <summary>Needs in this category.</summary>
    public ICollection<Need> Needs { get; set; } = new List<Need>();
    /// <summary>Resources in this category.</summary>
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}