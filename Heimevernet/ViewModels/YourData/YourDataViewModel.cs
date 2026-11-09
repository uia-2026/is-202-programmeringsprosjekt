using Heimevernet.Models;

namespace Heimevernet.ViewModels.YourData;

/// <summary>
/// Data shown on the "Your data" self-service page for the logged-in user.
/// </summary>
public class YourDataViewModel
{
    /// <summary>Number of activity entries shown before the "View full activity" expander.</summary>
    public const int VisibleActivityCount = 5;

    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public bool TwoFactorEnabled { get; set; }

    /// <summary>The user's own audit history, newest first.</summary>
    public IReadOnlyList<ActivityItemViewModel> Activity { get; set; } = [];
}

/// <summary>
/// A single audit entry rendered as a human-readable activity row.
/// </summary>
public class ActivityItemViewModel
{
    public AuditAction Action { get; set; }
    public string Label { get; set; } = string.Empty;
    public DateTime OccurredAt { get; set; }
}
