using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Data access for <see cref="User"/> accounts.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Finds a user by username.
    /// </summary>
    /// <param name="username">The username to look for.</param>
    /// <param name="cancellationToken">Cancellation token for the query.</param>
    /// <returns>The tracked user, or <see langword="null"/>.</returns>
    Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default);
    /// <summary>
    /// Adds a user to the context. It is saved on the next unit of work commit.
    /// </summary>
    /// <param name="user">The user to add.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the user has been added to the context.</returns>
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    /// <summary>
    /// Finds a user by ID.
    /// </summary>
    /// <param name="id">ID of the user.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The tracked user, or <see langword="null"/>.</returns>
    Task<User?> GetByIdAsync(int id, CancellationToken ct);
    /// <summary>
    /// Finds a user by email address. Used for login and to stop duplicate registrations.
    /// </summary>
    /// <param name="email">The email address.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The tracked user, or <see langword="null"/>.</returns>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct);

    /// <summary>
    /// Loads a read-only user together with the needs and resources they own
    /// (with categories), for the "Your data" overview and export.
    /// </summary>
    /// <param name="id">ID of the user.</param>
    /// <param name="ct">Cancellation token for the query.</param>
    /// <returns>The user with needs and resources loaded, or <see langword="null"/>.</returns>
    Task<User?> GetWithOwnedDataAsync(int id, CancellationToken ct = default);

    /// <summary>Sets the email notification flag. Returns false when the user does not exist.</summary>
    /// <remarks>The change is only tracked; it is saved on the next unit of work commit.</remarks>
    /// <param name="userId">ID of the user.</param>
    /// <param name="enabled"><see langword="true"/> to turn notifications on.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns><see langword="true"/> if the user was found.</returns>
    Task<bool> SetEmailNotificationsAsync(int userId, bool enabled, CancellationToken cancellationToken = default);
}