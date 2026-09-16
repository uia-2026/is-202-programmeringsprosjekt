using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Services;

public sealed class ResourceService : IResourceService
{
    private readonly AppDbContext _db;

    public ResourceService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Resources
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Resource resource, CancellationToken cancellationToken)
    {
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
