using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

/// <summary>
/// Repository implementation for Resources.
/// Provides data access methods used by services and controllers.
/// </summary>
public class ResourceRepository : IResourceRepository
{
    private readonly AppDbContext _context;

    public ResourceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Resource>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        /// <summary>
        /// Retrieves all resources from the database ordered by creation time.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token for the query.</param>
        /// <returns>A read-only list of <see cref="Resource"/> entities.</returns>
        return await _context.Resources
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Resource?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        /// <summary>
        /// Retrieves a single Resource by id, including its Category navigation property.
        /// </summary>
        /// <param name="id">The resource id to look up.</param>
        /// <param name="cancellationToken">Cancellation token for the query.</param>
        /// <returns>The matching <see cref="Resource"/> or null.</returns>
        return await _context.Resources
            .Include(r => r.Category)
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken = default)
    {
        /// <summary>
        /// Adds a Resource entity to the context for insertion.
        /// </summary>
        /// <param name="resource">The resource entity to add.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        await _context.Resources.AddAsync(
            resource,
            cancellationToken);
    }
}
