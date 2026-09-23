namespace Heimevernet.Models;

public class Resource
{
    public int Id { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string Region { get; set; } = string.Empty;

    public DateTime AvailableFrom { get; set; }

    public DateTime? AvailableTo { get; set; }

    public string ContactPoint { get; set; } = string.Empty;

    public ResourceStatus Status { get; set; } = ResourceStatus.Available;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
    public ICollection<Match> Matches { get; set; } = new List<Match>();
}