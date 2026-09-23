using Heimevernet.ViewModels.Need;

namespace Heimevernet.Services.Interfaces;

public interface INeedService
{
    Task CreateAsync(NeedCreateViewModel model, int userId);
    Task<IEnumerable<NeedViewModel>> GetAllAsync();
    Task<NeedViewModel?> GetByIdAsync(int id);
}