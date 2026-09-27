namespace Heimevernet.Models;

/// <summary>
/// Roles that may be assigned to application users.
/// </summary>
public enum UserRole
{
    PublicActor,
    ResourceProvider
    // ? Maybe we can add Admin role later
}

/// <summary>
/// Priority levels for reported needs.
/// </summary>
public enum NeedPriority
{
    Urgent,
    Planned
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
    UnderReview,
    Assigned,
    Resolved
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
