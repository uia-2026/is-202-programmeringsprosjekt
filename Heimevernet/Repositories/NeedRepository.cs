using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories
{
    /// <summary>
    /// Repository implementation for Needs.
    /// Provides data access operations used by NeedService and controllers.
    /// </summary>
    public class NeedRepository : INeedRepository
    {
        private readonly AppDbContext _context;
        public NeedRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<Need>> GetAllAsync(CancellationToken cancellationToken = default) =>
            await _context.Needs
                .Include(n => n.Category)
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.Deadline)
                .ToListAsync(cancellationToken);

        /// <summary>
        /// Retrieves a single Need by id including Category navigation.
        /// </summary>
        /// <param name="id">The id of the need.</param>
        /// <param name="cancellationToken">Cancellation token for the query.</param>
        /// <returns>The matching <see cref="Need"/> or null.</returns>
        public async Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            await _context.Needs
                .Include(n => n.Category)
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        /// <summary>
        /// Adds a Need entity to the context for insertion.
        /// </summary>
        /// <param name="need">The need entity to add.</param>
        /// <param name="cancellationToken">Cancellation token for the operation.</param>
        public async Task AddAsync(Need need, CancellationToken cancellationToken = default)
        {
            await _context.Needs.AddAsync(need, cancellationToken);
        }
    }
}