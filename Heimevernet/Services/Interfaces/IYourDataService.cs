using Heimevernet.DTOs.YourData;
using Heimevernet.ViewModels.YourData;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Self-service operations a logged-in user can perform on their own data.
/// </summary>
public interface IYourDataService
{
    /// <summary>Returns account details and own activity, or null if the user no longer exists.</summary>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The <see cref="YourDataViewModel"/>, or <see langword="null"/>.</returns>
    Task<YourDataViewModel?> GetOverviewAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>Returns a full export of the user's account, listings and activity, or null if the user no longer exists.</summary>
    /// <param name="userId">ID of the signed-in user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The <see cref="YourDataExportDto"/>, or <see langword="null"/>.</returns>
    Task<YourDataExportDto?> ExportAsync(int userId, CancellationToken cancellationToken = default);
}
