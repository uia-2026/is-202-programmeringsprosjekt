using Heimevernet.Models;

namespace Heimevernet.Services.Interfaces;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
}

