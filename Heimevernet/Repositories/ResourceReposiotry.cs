using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

public class ResourceRepository : IResourceRepository
{
    private readonly AppDbContext _context;

    public ResourceRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<Resource>> GetAllAsync(
        CancellationToken cancellationToken)
    {
        return await _context.Resources
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Resource?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken)
    {
        return await _context.Resources
            .Include(r => r.Category)
            .FirstOrDefaultAsync(
                r => r.Id == id,
                cancellationToken);
    }

    public async Task AddAsync(
        Resource resource,
        CancellationToken cancellationToken)
    {
        await _context.Resources.AddAsync(
            resource,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
