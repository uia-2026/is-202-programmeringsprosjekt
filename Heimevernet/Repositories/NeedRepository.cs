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
        public async Task<IEnumerable<Need>> GetAllAsync() =>
            await _context.Needs
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.Deadline)
                .ToListAsync();

        public async Task<Need?> GetByIdAsync(int id) =>
            await _context.Needs.FirstOrDefaultAsync(n => n.Id == id);

        public async Task AddAsync(Need need)
        {
            _context.Needs.Add(need);
            await _context.SaveChangesAsync();
        }
    }
}