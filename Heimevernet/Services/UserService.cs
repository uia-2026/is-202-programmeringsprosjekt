using Heimevernet.Repositories.Interfaces;
using Heimevernet.Services.Interfaces;
using Heimevernet.ViewModels.Account;

namespace Heimevernet.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

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
            TwoFactorEnabled = user.TwoFactorEnabled
        };
    }
}
