using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services.Interfaces;

public interface INeedService
{
    Task CreateAsync(NeedCreateViewModel model, int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<NeedViewModel>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync(CancellationToken cancellationToken = default);
    Task<NeedViewModel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}