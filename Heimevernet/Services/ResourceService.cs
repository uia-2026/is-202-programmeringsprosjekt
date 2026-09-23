using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Services;

/// <summary>
/// EF-backed implementation of <see cref="IResourceService"/>.
/// </summary>
public sealed class ResourceService : IResourceService
{
    private readonly AppDbContext _db;

    /// <summary>Creates a new instance using the provided <see cref="AppDbContext"/>.</summary>
    public ResourceService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Returns all resources ordered by id.</summary>
    public async Task<IReadOnlyList<Resource>> GetAllAsync(CancellationToken cancellationToken)
    {
        return await _db.Resources
            .AsNoTracking()
            .OrderBy(r => r.Id)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Adds a resource to the database and persists changes.</summary>
    public async Task AddAsync(Resource resource, CancellationToken cancellationToken)
    {
        _db.Resources.Add(resource);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
