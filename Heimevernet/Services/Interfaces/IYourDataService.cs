using Heimevernet.DTOs.YourData;
using Heimevernet.ViewModels.YourData;

namespace Heimevernet.Services.Interfaces;

/// <summary>Outcome of a self-service account deletion.</summary>
public enum DeleteAccountResult
{
    Deleted,
    NotFound,
    /// <summary>The user is part of an Assigned match, so nothing was deleted.</summary>
    BlockedByActiveMatch
}

/// <summary>
/// Self-service operations a logged-in user can perform on their own data.
/// </summary>
public interface IYourDataService
{
    /// <summary>Returns account details and own activity, or null if the user no longer exists.</summary>
    Task<YourDataViewModel?> GetOverviewAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Returns a full export of the user's account, listings and activity, or null if the user no longer exists.</summary>
    Task<YourDataExportDto?> ExportAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the account and its needs/resources. Audit entries are kept with ActorUserId set to null.
    /// Deletion is blocked if the user is part of any Assigned match, so the other party's
    /// need/resource is never left in a stale Assigned state.
    /// </summary>
    Task<DeleteAccountResult> DeleteAccountAsync(int userId, CancellationToken cancellationToken = default);
}
