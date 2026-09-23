namespace Heimevernet.Models;

public enum UserRole
{
    PublicActor,
    ResourceProvider
    // ? Maybe we can add Admin role later
}

public enum NeedPriority
{
    Urgent,
    Planned
}

public enum NeedStatus
{
    New,
    UnderReview,
    Assigned,
    Resolved
}

public enum ResourceStatus
{
    Available,
    Busy
}

public enum MatchStatus
{
    UnderReview,
    Assigned,
    Resolved
}

public enum CategoryAppliesTo
{
    Need,
    Resource,
    Both
}