using Heimevernet.ViewModels.Account;

namespace Heimevernet.Services.Interfaces;

/// <summary>
/// Reads and updates the signed-in user's account settings.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Returns the account page model for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The <see cref="AccountViewModel"/>, or <see langword="null"/> if the user does not exist.</returns>
    Task<AccountViewModel?> GetAccountAsync(
        int userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Turns email notifications on or off for a user.
    /// </summary>
    /// <param name="userId">ID of the user.</param>
    /// <param name="enabled"><see langword="true"/> to turn notifications on.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns><see langword="true"/> if the user was found and updated.</returns>
    Task<bool> SetEmailNotificationsAsync(int userId, bool enabled, CancellationToken ct = default);
}