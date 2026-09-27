namespace Heimevernet.Models;


/// <summary>
/// Domain model representing a category that applies to Needs or Resources.
/// Used to group items and limit where categories can be applied.
/// </summary>
public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CategoryAppliesTo AppliesTo { get; set; }

    public ICollection<Need> Needs { get; set; } = new List<Need>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
