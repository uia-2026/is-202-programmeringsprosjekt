using Heimevernet.Data;
using Heimevernet.Models;
using Microsoft.EntityFrameworkCore;

namespace Heimevernet.Services
{
    public class EfNeedRepository : INeedRepository
    {
        private readonly AppDbContext _context;
        public EfNeedRepository(AppDbContext context)
        {
            _context = context;
        }
        public IEnumerable<Need> GetAll() =>
            _context.Needs
                .OrderByDescending(n => n.Priority)
                .ThenBy(n => n.Deadline)
                .ToList();

        public Need? GetById(int id) =>
            _context.Needs.FirstOrDefault(n => n.Id == id);

        public void Add(Need need)
        {
            _context.Needs.Add(need);
            _context.SaveChanges();

        }
    }
}