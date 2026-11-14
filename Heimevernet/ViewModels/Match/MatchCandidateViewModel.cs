namespace Heimevernet.ViewModels.Match;

/// <summary>
/// A candidate Resource suggested as a possible match for a Need,
/// shown on the Propose-match picker page.
/// </summary>
public class MatchCandidateViewModel
{
    /// <summary>ID of the candidate resource.</summary>
    public int ResourceId { get; set; }

    /// <summary>Title of the resource.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional description of the resource.</summary>
    public string? Description { get; set; }

    /// <summary>Category name of the resource.</summary>
    public string CategoryName { get; set; } = string.Empty;

    /// <summary>Region of the resource.</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Contact details for the resource.</summary>
    public string ContactPoint { get; set; } = string.Empty;

    /// <summary>Total quantity the resource offers.</summary>
    public int QuantityOffered { get; set; }

    /// <summary>When the resource becomes available.</summary>
    public DateTime AvailableFrom { get; set; }

    /// <summary>When the resource stops being available, or <see langword="null"/>.</summary>
    public DateTime? AvailableTo { get; set; }

    /// <summary>Straight-line distance from the need, in kilometres. The list is sorted by this.</summary>
    public double DistanceKm { get; set; }
}