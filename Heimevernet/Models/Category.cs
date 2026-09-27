namespace Heimevernet.Models;


public class Category
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public CategoryAppliesTo AppliesTo { get; set; }

    public ICollection<Need> Needs { get; set; } = new List<Need>();
    public ICollection<Resource> Resources { get; set; } = new List<Resource>();
}
