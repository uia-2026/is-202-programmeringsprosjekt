namespace Heimevernet.Models;

/// <summary>
/// Roles that may be assigned to application users.
/// </summary>
public enum UserRole
{
    /// <summary>A public body (e.g. a municipality) that reports needs and proposes matches.</summary>
    PublicActor,
    /// <summary>A person or organisation that offers resources and accepts or rejects proposed matches.</summary>
    ResourceProvider
}

/// <summary>
/// Priority levels for reported needs.
/// </summary>
public enum NeedPriority
{
    /// <summary>Can wait.</summary>
    Low,
    /// <summary>Should be handled soon.</summary>
    Medium,
    /// <summary>Urgent. Creating a critical need sends email to nearby providers.</summary>
    Critical
}

/// <summary>
/// Lifecycle status for a Need.
/// </summary>
public enum NeedStatus
{
    /// <summary>Just registered, with no proposals yet.</summary>
    New,
    /// <summary>At least one match has been proposed and is waiting for a decision.</summary>
    UnderReview,
    /// <summary>The full quantity has been allocated through accepted matches.</summary>
    Assigned,
    /// <summary>The need owner has marked the need as done.</summary>
    Resolved
}

/// <summary>
/// Availability/status for a Resource.
/// </summary>
public enum ResourceStatus
{
    /// <summary>The resource can be proposed for needs.</summary>
    Available,
    /// <summary>The resource is in use and cannot be proposed.</summary>
    Busy
}

/// <summary>
/// Status for a Match between a Need and a Resource.
/// </summary>
public enum MatchStatus
{
    /// <summary>Proposed and waiting for the resource provider's decision.</summary>
    UnderReview, // Proposed and awaiting the Resource Provider's decision.
    /// <summary>Accepted; the match quantity has been allocated.</summary>
    Assigned,    // Accepted; the match quantity has been allocated.
    /// <summary>The need was marked as resolved.</summary>
    Resolved,    // The Need was marked as resolved; all assigned matches become resolved.
    /// <summary>The resource provider rejected the proposal.</summary>
    Rejected,    // The Resource Provider rejected the proposal.
    /// <summary>No longer needed because the need was deleted or fully allocated.</summary>
    Cancelled    // The proposal is no longer needed because the Need was deleted or fully allocated.
}

/// <summary>
/// Indicates whether a category applies to Needs, Resources or both.
/// </summary>
public enum CategoryAppliesTo
{
    /// <summary>Only for needs.</summary>
    Need,
    /// <summary>Only for resources.</summary>
    Resource,
    /// <summary>For both needs and resources.</summary>
    Both
}

/// <summary>
/// Meaningful application events recorded in the audit history.
/// </summary>
public enum AuditAction
{
    /// <summary>A user signed in.</summary>
    LoginSucceeded,
    /// <summary>A sign-in failed (wrong password or 2FA code, or unknown email).</summary>
    LoginFailed,
    /// <summary>A user turned on two-factor authentication.</summary>
    TwoFactorEnabled,

    /// <summary>A need was registered.</summary>
    NeedCreated,
    /// <summary>A need was changed.</summary>
    NeedEdited,
    /// <summary>A need was deleted.</summary>
    NeedDeleted,
    /// <summary>A need was marked as resolved.</summary>
    NeedResolved,

    /// <summary>A resource was registered.</summary>
    ResourceCreated,
    /// <summary>A resource was changed.</summary>
    ResourceEdited,
    /// <summary>A resource was deleted.</summary>
    ResourceDeleted,

    /// <summary>A photo was added to a resource.</summary>
    AttachmentUploaded,
    /// <summary>A photo was removed from a resource.</summary>
    AttachmentDeleted,

    /// <summary>A resource was proposed for a need.</summary>
    MatchProposed,
    /// <summary>A proposed match was accepted.</summary>
    MatchAccepted,
    /// <summary>A proposed match was rejected.</summary>
    MatchRejected,
    /// <summary>A match was resolved together with its need.</summary>
    MatchResolved
}


/// <summary>
/// Lifecycle of an outbox message.
/// Pending is waiting to be sent, Processing is claimed by a worker (see LockedUntil),
/// Done is finished or skipped, and Failed has used up all attempts.
/// </summary>
public enum OutboxStatus
{
    /// <summary>Waiting to be sent.</summary>
    Pending,
    /// <summary>Claimed by a worker until <see cref="OutboxMessage.LockedUntil"/>.</summary>
    Processing,
    /// <summary>Sent or skipped.</summary>
    Done,
    /// <summary>All attempts used up.</summary>
    Failed
}