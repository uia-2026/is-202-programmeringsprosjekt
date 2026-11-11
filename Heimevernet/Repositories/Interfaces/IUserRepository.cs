using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Loads a read-only user together with the needs and resources they own
    /// (with categories), for the "Your data" overview and export.
    /// </summary>
    Task<User?> GetWithOwnedDataAsync(int id, CancellationToken ct = default);

    /// <summary>Sets the email notification flag. Returns false when the user does not exist.</summary>
    Task<bool> SetEmailNotificationsAsync(int userId, bool enabled, CancellationToken cancellationToken = default);
}