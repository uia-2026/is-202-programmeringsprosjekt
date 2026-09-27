using Heimevernet.Data;
using Heimevernet.Repositories.Interfaces;

namespace Heimevernet.Repositories;

/// <summary>
/// Simple unit-of-work implementation that wraps AppDbContext.SaveChangesAsync.
/// Used to commit transactional changes made through repositories.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Commits any pending changes in the DbContext.
    /// </summary>
    /// <returns>A task that completes when changes are saved.</returns>
    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();
    }
}