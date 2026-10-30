namespace Heimevernet.Models;

/// <summary>
/// Roles that may be assigned to application users.
/// </summary>
public enum UserRole
{
    PublicActor,
    ResourceProvider
}

/// <summary>
/// Priority levels for reported needs.
/// </summary>
public enum NeedPriority
{
    Low,
    Medium,
    Critical
}

/// <summary>
/// Lifecycle status for a Need.
/// </summary>
public enum NeedStatus
{
    New,
    UnderReview,
    Assigned,
    Resolved
}

/// <summary>
/// Availability/status for a Resource.
/// </summary>
public enum ResourceStatus
{
    Available,
    Busy
}

/// <summary>
/// Status for a Match between a Need and a Resource.
/// </summary>
public enum MatchStatus
{
    UnderReview, // Proposed and awaiting the Resource Provider's decision.
    Assigned,    // Accepted; the match quantity has been allocated.
    Resolved,    // The Need was marked as resolved; all assigned matches become resolved.
    Rejected,    // The Resource Provider rejected the proposal.
    Cancelled    // The proposal is no longer needed because the Need was deleted or fully allocated.
}

/// <summary>
/// Indicates whether a category applies to Needs, Resources or both.
/// </summary>
public enum CategoryAppliesTo
{
    Need,
    Resource,
    Both
}
