namespace Heimevernet.Repositories.Interfaces;

/// <summary>
/// Unit of work abstraction for committing transactional changes to the data store.
/// Implementations typically wrap an EF Core DbContext.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Persists any pending changes to the underlying store.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the changes are saved.</returns>
    /// <exception cref="Heimevernet.Exceptions.ConcurrencyConflictException">Thrown when someone else changed a versioned row first.</exception>
    /// <exception cref="Heimevernet.Exceptions.DuplicateRecordException">Thrown when the save breaks a unique constraint.</exception>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs an operation inside a database transaction. The transaction is committed if the
    /// operation finishes, and rolled back (and the exception re-thrown) if it fails.
    /// </summary>
    /// <param name="operation">The work to run. It gets the cancellation token as its argument.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>A task that completes when the transaction has been committed.</returns>
    Task ExecuteInTransactionAsync(
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default);
}