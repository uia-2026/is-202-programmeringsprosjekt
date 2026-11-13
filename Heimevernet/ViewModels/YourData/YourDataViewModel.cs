using Heimevernet.Models;

namespace Heimevernet.ViewModels.YourData;

/// <summary>
/// Data shown on the "Your data" self-service page for the logged-in user.
/// </summary>
public class YourDataViewModel
{
    /// <summary>Number of activity entries shown before the "View full activity" expander.</summary>
    public const int VisibleActivityCount = 5;

    /// <summary>The user's display name.</summary>
    public string Username { get; set; } = string.Empty;
    /// <summary>The user's email address.</summary>
    public string Email { get; set; } = string.Empty;
    /// <summary>The user's role.</summary>
    public UserRole Role { get; set; }
    /// <summary>Whether 2FA is turned on.</summary>
    public bool TwoFactorEnabled { get; set; }

    /// <summary>The user's own audit history, newest first.</summary>
    public IReadOnlyList<ActivityItemViewModel> Activity { get; set; } = [];
}

/// <summary>
/// A single audit entry rendered as a human-readable activity row.
/// </summary>
public class ActivityItemViewModel
{
    /// <summary>What happened.</summary>
    public AuditAction Action { get; set; }
    /// <summary>Short readable text for the row, e.g. "Need created — Agder".</summary>
    public string Label { get; set; } = string.Empty;
    /// <summary>When it happened (UTC).</summary>
    public DateTime OccurredAt { get; set; }
}