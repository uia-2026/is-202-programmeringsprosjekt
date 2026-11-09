using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _context;

    public UserRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetWithOwnedDataAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .Include(u => u.Needs).ThenInclude(n => n.Category)
            .Include(u => u.Resources).ThenInclude(r => r.Category)
            .Include(u => u.Resources).ThenInclude(r => r.Attachments)
            .Include(u => u.MatchesCreated)
            .Include(u => u.UploadedAttachments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public void RemoveAccount(User user)
    {
        _context.Matches.RemoveRange(user.MatchesCreated);
        _context.Attachments.RemoveRange(user.UploadedAttachments);
        _context.Users.Remove(user);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }
}