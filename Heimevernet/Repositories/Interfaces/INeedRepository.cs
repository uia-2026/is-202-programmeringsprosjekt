using Heimevernet.Models;

namespace Heimevernet.Repositories.Interfaces;

public interface INeedRepository
{
    Task<IEnumerable<Need>> GetAllAsync();
    Task<Need?> GetByIdAsync(int id);
    Task AddAsync(Need need);
}

