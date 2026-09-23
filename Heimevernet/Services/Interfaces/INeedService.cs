using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services.Interfaces;

public interface INeedService
{
    Task CreateAsync(NeedCreateViewModel model, int userId);
    Task<IEnumerable<NeedViewModel>> GetAllAsync();
    Task<IEnumerable<NeedSummaryViewModel>> GetSummariesAsync();
    Task<NeedViewModel?> GetByIdAsync(int id);
}