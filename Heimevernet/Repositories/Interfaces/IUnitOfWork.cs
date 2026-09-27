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
    Task CommitAsync();
}
