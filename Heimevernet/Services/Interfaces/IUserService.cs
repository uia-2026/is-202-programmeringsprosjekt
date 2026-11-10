using Heimevernet.ViewModels.Account;

namespace Heimevernet.Services.Interfaces;

public interface IUserService
{
  Task<AccountViewModel?> GetAccountAsync(
    int userId,
    CancellationToken cancellationToken = default);

  Task<bool> SetEmailNotificationsAsync(int userId, bool enabled, CancellationToken ct = default);
}

