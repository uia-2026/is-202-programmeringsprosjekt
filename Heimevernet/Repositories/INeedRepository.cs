using Heimevernet.Models;

namespace Heimevernet.Repositories
{
    public interface INeedRepository
    {
        Task<IEnumerable<Need>> GetAllAsync();
        Task<Need?> GetByIdAsync(int id);
        Task AddAsync (Need need);
    }
}
