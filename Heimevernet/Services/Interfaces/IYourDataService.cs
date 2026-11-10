using Heimevernet.DTOs.YourData;
using Heimevernet.ViewModels.YourData;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Self-service operations a logged-in user can perform on their own data.
/// </summary>
public interface IYourDataService
{
    /// <summary>Returns account details and own activity, or null if the user no longer exists.</summary>
    Task<YourDataViewModel?> GetOverviewAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Returns a full export of the user's account, listings and activity, or null if the user no longer exists.</summary>
    Task<YourDataExportDto?> ExportAsync(int userId, CancellationToken cancellationToken = default);
}
