namespace Heimevernet.Models;

public class Match
{
    public int Id { get; set; }

    public int NeedId { get; set; }
    public Need Need { get; set; } = null!;

    public int ResourceId { get; set; }
    public Resource Resource { get; set; } = null!;

    public MatchStatus Status { get; set; } = MatchStatus.UnderReview;

    public int MatchedByUserId { get; set; }
    public User MatchedByUser { get; set; } = null!;

    public DateTime MatchedAt { get; set; } = DateTime.UtcNow;
}