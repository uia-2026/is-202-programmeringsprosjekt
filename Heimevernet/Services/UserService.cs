using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;

namespace Heimevernet.Services;

/// <summary>
/// Default <see cref="IUserService"/> backed by the user repository.
/// </summary>
/// <param name="userRepository">Repository for users.</param>
public class UserService(IUserRepository userRepository) : IUserService
{
    private readonly IUserRepository _userRepository = userRepository;

    /// <inheritdoc />
    public async Task<AccountViewModel?> GetAccountAsync(
        int userId,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(
            userId,
            cancellationToken);

        if (user == null)
            return null;

        return new AccountViewModel
        {
            Username = user.Username,
            Email = user.Email,
            Role = user.Role,
            TwoFactorEnabled = user.TwoFactorEnabled,
            EmailNotifications = user.EmailNotifications
        };
    }

    /// <inheritdoc />
    public Task<bool> SetEmailNotificationsAsync(
        int userId,
        bool enabled,
        CancellationToken cancellationToken = default) => _userRepository.SetEmailNotificationsAsync(userId, enabled, cancellationToken);
}