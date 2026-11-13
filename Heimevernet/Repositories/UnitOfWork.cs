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

    /// <summary>
    /// Creates the unit of work.
    /// </summary>
    /// <param name="context">The database context whose changes are saved.</param>
    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Commits any pending changes in the DbContext.
    /// </summary>
    /// <returns>A task that completes when changes are saved.</returns>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <exception cref="ConcurrencyConflictException">Thrown when someone else changed a versioned row first.</exception>
    /// <exception cref="DuplicateRecordException">Thrown when the save breaks a unique constraint (MySQL error 1062).</exception>
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

    /// <inheritdoc />
    public async Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }
}