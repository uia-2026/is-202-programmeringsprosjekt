namespace Heimevernet.ViewModels.Match;

/// <summary>
/// A candidate Resource suggested as a possible match for a Need,
/// shown on the Propose-match picker page.
/// </summary>
public class MatchCandidateViewModel
{
    public int ResourceId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string CategoryName { get; set; } = string.Empty;

    public string Region { get; set; } = string.Empty;

    public string ContactPoint { get; set; } = string.Empty;

    public DateTime AvailableFrom { get; set; }

    public DateTime? AvailableTo { get; set; }

    public double DistanceKm { get; set; }
}