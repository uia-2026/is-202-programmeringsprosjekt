using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserRepository"/>.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    /// <summary>
    /// Creates the repository.
    /// </summary>
    /// <param name="context">The database context.</param>
    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<User?> GetWithOwnedDataAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.Needs).ThenInclude(n => n.Category)
            .Include(u => u.Resources).ThenInclude(r => r.Category)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> SetEmailNotificationsAsync(
    int userId,
    bool enabled,
    CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
            return false;

        user.EmailNotifications = enabled;
        return true;
    }
}