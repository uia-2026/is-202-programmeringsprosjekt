using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories
{
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

        public async Task<Need?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            await _context.Needs
                .Include(n => n.Category)
                .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);

        public async Task AddAsync(Need need, CancellationToken cancellationToken = default)
        {
            await _context.Needs.AddAsync(need, cancellationToken);
        }
    }
}