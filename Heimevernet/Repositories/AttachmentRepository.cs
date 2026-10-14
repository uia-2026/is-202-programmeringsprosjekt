
using Heimevernet.Data;
using Heimevernet.Models;
using Heimevernet.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Repositories;

public sealed class AttachmentRepository
    : IAttachmentRepository
{
    private readonly AppDbContext _context;

    public AttachmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        Attachment attachment,
        CancellationToken ct = default)
    {
        await _context.Attachments.AddAsync(attachment, ct);
    }

    public Task<Attachment?> GetByIdAsync(
        int id,
        CancellationToken ct = default)
    {
        return _context.Attachments
            .FirstOrDefaultAsync(a => a.Id == id, ct);
    }

    public async Task<IReadOnlyList<Attachment>> GetByResourceIdAsync(
        int resourceId,
        CancellationToken ct = default)
    {
        return await _context.Attachments
            .Where(a => a.ResourceId == resourceId)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(ct);
    }

    public void Remove(Attachment attachment)
    {
        _context.Attachments.Remove(attachment);
    }
}