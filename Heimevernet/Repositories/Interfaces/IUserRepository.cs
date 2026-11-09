using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Loads a tracked user together with the needs, resources, attachments and
    /// matches they own, so the whole account can be exported or removed.
    /// </summary>
    Task<User?> GetWithOwnedDataAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Marks a user loaded via <see cref="GetWithOwnedDataAsync"/> for deletion.
    /// Needs, resources and their attachments/matches are removed by cascade;
    /// matches and attachments the user created on other people's records are
    /// removed explicitly because those relationships use Restrict.
    /// Audit log entries are kept and their ActorUserId is set to null by the database.
    /// </summary>
    void RemoveAccount(User user);
}