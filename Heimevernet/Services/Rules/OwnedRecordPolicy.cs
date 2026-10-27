using Heimevernet.Models;
using Heimevernet.ViewModels.Need;
using Heimevernet.ViewModels.Resource;

namespace Heimevernet.Services.Rules;

public enum OwnedRecordFailureCode
{
    None,
    NotOwner,
    Resolved,
    ProtectedByPendingOrAssignedMatch
}

public readonly record struct OwnedRecordDecision(
    bool Allowed,
    OwnedRecordFailureCode FailureCode,
    string Message)
{
    public static OwnedRecordDecision Success() =>
        new(true, OwnedRecordFailureCode.None, string.Empty);

    public static OwnedRecordDecision Fail(OwnedRecordFailureCode failureCode, string message) =>
        new(false, failureCode, message);
}

public static class OwnedRecordPolicy
{
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

    public static OwnedRecordDecision CanDeleteNeed(Need need, int userId)
    {
        if (need.UserId != userId)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.NotOwner,
                "You can only delete your own needs.");
        }

        if (HasPendingOrAssignedMatch(need.Matches))
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.ProtectedByPendingOrAssignedMatch,
                "Needs with pending proposals or active assignments cannot be deleted.");
        }

        return OwnedRecordDecision.Success();
    }

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

    public static OwnedRecordDecision CanDeleteResource(Resource resource, int userId)
    {
        if (resource.UserId != userId)
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.NotOwner,
                "You can only delete your own resources.");
        }

        if (HasPendingOrAssignedMatch(resource.Matches))
        {
            return OwnedRecordDecision.Fail(
                OwnedRecordFailureCode.ProtectedByPendingOrAssignedMatch,
                "Resources with pending proposals or active assignments cannot be deleted.");
        }

        return OwnedRecordDecision.Success();
    }

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
