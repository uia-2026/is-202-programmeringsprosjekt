namespace Heimevernet.Models;


public class Need
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

    public NeedPriority Priority { get; set; }

    public DateTime? Deadline { get; set; }

    public string ContactPoint { get; set; } = string.Empty;

    public NeedStatus Status { get; set; } = NeedStatus.New;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Match> Matches { get; set; } = new List<Match>();
}