using Heimevernet.Models;

namespace Heimevernet.Services
{
    public interface INeedRepository
    {
        IEnumerable<Need> GetAll();
        Need? GetById(int id);
        void Add(Need need);
    }
}
