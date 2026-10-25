using Heimevernet.Data;
using Heimevernet.Exceptions;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

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
    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(ex);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is MySqlException { Number: 1062 })
        {
            throw new DuplicateRecordException(ex);
        }
    }
}