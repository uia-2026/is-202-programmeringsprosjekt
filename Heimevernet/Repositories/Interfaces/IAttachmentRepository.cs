using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface IAttachmentRepository
{
    Task AddAsync(Attachment attachment, CancellationToken ct = default);
    Task<Attachment?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Attachment>> GetByResourceIdAsync(int resourceId, CancellationToken ct = default);
    void Remove(Attachment attachment);
}