using Heimevernet.Models;
using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services.Rules;

/// <summary>
/// Why <see cref="OwnedRecordPolicy"/> refused a change.
/// </summary>
public enum OwnedRecordFailureCode
{
    /// <summary>The change is allowed.</summary>
    None,
    /// <summary>The user does not own the record.</summary>
    NotOwner,
    /// <summary>The need is resolved and can no longer be edited.</summary>
    Resolved,
    /// <summary>The record has at least one match and cannot be deleted.</summary>
    ProtectedByMatch,
    /// <summary>The record has a pending or assigned match, so key fields (category, location, dates) cannot change.</summary>
    ProtectedByPendingOrAssignedMatch
}

/// <summary>
/// Result of an <see cref="OwnedRecordPolicy"/> check.
/// </summary>
/// <param name="Allowed"><see langword="true"/> when the change may go ahead.</param>
/// <param name="FailureCode">Why the change was refused, or <see cref="OwnedRecordFailureCode.None"/>.</param>
/// <param name="Message">User-facing message explaining a refusal; empty on success.</param>
public readonly record struct OwnedRecordDecision(
    bool Allowed,
    OwnedRecordFailureCode FailureCode,
    string Message)
{
    /// <summary>
    /// Creates a decision that allows the change.
    /// </summary>
    /// <returns>An allowed <see cref="OwnedRecordDecision"/>.</returns>
    public static OwnedRecordDecision Success() =>
        new(true, OwnedRecordFailureCode.None, string.Empty);

    /// <summary>
    /// Creates a decision that refuses the change.
    /// </summary>
    /// <param name="failureCode">Why the change is refused.</param>
    /// <param name="message">User-facing message.</param>
    /// <returns>A refused <see cref="OwnedRecordDecision"/>.</returns>
    public static OwnedRecordDecision Fail(OwnedRecordFailureCode failureCode, string message) =>
        new(false, failureCode, message);
}

/// <summary>
/// Ownership and state rules for editing and deleting needs and resources.
/// Pure functions with no database access, so they are easy to unit test.
/// </summary>
public static class OwnedRecordPolicy
{
    /// <summary>
    /// Checks if a user may edit a need. Only the owner may edit, resolved needs are locked, and
    /// category, location and deadline are locked while there are pending or assigned matches.
    /// </summary>
    /// <param name="need">The current need, with its matches loaded.</param>
    /// <param name="updated">The submitted new values.</param>
    /// <param name="userId">ID of the user editing.</param>
    /// <returns>The <see cref="OwnedRecordDecision"/>.</returns>
    public static OwnedRecordDecision CanEditNeed(
        Need need,
        NeedCreateViewModel updated,
        int userId)
    {
        if (need.UserId != userId)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.NotOwner,
                "You can only edit your own needs.");
        }

        if (need.Status == NeedStatus.Resolved)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.Resolved,
                "Resolved needs cannot be edited.");
        }

        if (HasPendingOrAssignedMatch(need.Matches) && ChangesProtectedNeedFields(need, updated))
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.ProtectedByPendingOrAssignedMatch,
                "Category, location, and deadline cannot be changed while there are pending proposals or active assignments.");
        }

        return OwnedRecordDecision.Success();
    }

    /// <summary>
    /// Checks if a user may delete a need. Only the owner may delete, and only if the need has no matches.
    /// </summary>
    /// <param name="need">The need, with its matches loaded.</param>
    /// <param name="userId">ID of the user deleting.</param>
    /// <returns>The <see cref="OwnedRecordDecision"/>.</returns>
    public static OwnedRecordDecision CanDeleteNeed(
        Need need,
        int userId)
    {
        if (need.UserId != userId)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.NotOwner,
                "You can only delete your own needs.");
        }

        if (HasAnyMatch(need.Matches))
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.ProtectedByMatch,
                "Needs with associated matches cannot be deleted.");
        }

        return OwnedRecordDecision.Success();
    }

    /// <summary>
    /// Checks if a user may edit a resource. Only the owner may edit, and category, location and
    /// availability are locked while there are pending or assigned matches.
    /// </summary>
    /// <param name="resource">The current resource, with its matches loaded.</param>
    /// <param name="updated">The submitted new values.</param>
    /// <param name="userId">ID of the user editing.</param>
    /// <returns>The <see cref="OwnedRecordDecision"/>.</returns>
    public static OwnedRecordDecision CanEditResource(
        Resource resource,
        ResourceCreateViewModel updated,
        int userId)
    {
        if (resource.UserId != userId)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.NotOwner,
                "You can only edit your own resources.");
        }

        if (HasPendingOrAssignedMatch(resource.Matches) && ChangesProtectedResourceFields(resource, updated))
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.ProtectedByPendingOrAssignedMatch,
                "Category, location, and availability cannot be changed while there are pending proposals or active assignments.");
        }

        return OwnedRecordDecision.Success();
    }

    /// <summary>
    /// Checks if a user may delete a resource. Only the owner may delete, and only if the resource has no matches.
    /// </summary>
    /// <param name="resource">The resource, with its matches loaded.</param>
    /// <param name="userId">ID of the user deleting.</param>
    /// <returns>The <see cref="OwnedRecordDecision"/>.</returns>
    public static OwnedRecordDecision CanDeleteResource(
        Resource resource,
        int userId)
    {
        if (resource.UserId != userId)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.NotOwner,
                "You can only delete your own resources.");
        }

        if (HasAnyMatch(resource.Matches))
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.ProtectedByMatch,
                "Resources with associated matches cannot be deleted.");
        }

        return OwnedRecordDecision.Success();
    }

    private static bool HasAnyMatch(IEnumerable<Match> matches) =>
        matches.Any();

    private static bool HasPendingOrAssignedMatch(IEnumerable<Match> matches) =>
        matches.Any(m => m.Status is MatchStatus.UnderReview or MatchStatus.Assigned);

    private static bool ChangesProtectedNeedFields(Need current, NeedCreateViewModel updated)
    {
        return current.CategoryId != updated.CategoryId
            || current.Latitude != updated.Latitude
            || current.Longitude != updated.Longitude
            || !string.Equals(current.Region, updated.Region?.Trim(), StringComparison.Ordinal)
            || current.Deadline != updated.Deadline;
    }

    private static bool ChangesProtectedResourceFields(Resource current, ResourceCreateViewModel updated)
    {
        return current.CategoryId != updated.CategoryId
            || current.Latitude != updated.Latitude
            || current.Longitude != updated.Longitude
            || !string.Equals(current.Region, updated.Region?.Trim(), StringComparison.Ordinal)
            || current.AvailableFrom != updated.AvailableFrom
            || current.AvailableTo != updated.AvailableTo;
    }
}