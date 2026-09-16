using Heimevernet.Models;

namespace Heimevernet.Services
{
    public class InMemoryNeedRepository : INeedRepository
    {
        private readonly List<Need> _needs = new()
        {
            new Need
            {
                Id = 1,
                Title = "Snow Removal - E39",
                Type = "Transport",
                Street = "E39",
                City = "Kristiansand",
                PostalCode = "4611",
                County = "Agder",
                Country = "Norway",
                Deadline = DateTime.Now.AddDays(2),
                Priority = NeedPriority.High,
                ContactName = "Martin Ola",
                ContactEmail = "martino@heimevernet.no",
                ContactRole= "Crisis Leader",
                ContactPhone = "+47 123 45 678",
                Description ="Need for snow removal on E39 near Mandal. Large amounts of snow make the road difficult to pass.",
                Status = NeedStatus.UnderReview
            },
            new Need
            {
                Id = 2,
                Title = "Mini van",
                Type = "Transport",
                Street = "Evjemoen",
                City = "Evje",
                PostalCode = "4735",
                County = "Agder",
                Country = "Norway",
                Deadline = DateTime.Now.AddHours(6),
                Priority = NeedPriority.Medium,
                ContactName = "Enne Marte",
                ContactEmail = "ennemar@gmail.com",
                ContactRole = "Manager",
                ContactPhone = "+47 900 00 000",
                Description = "Need for transportation of equipment and supplies to the evacuation center.",
                Status = NeedStatus.New
            },
            new Need
            {
                Id = 3,
                Title = "Drone Observation",
                Type = "Drone Observation",
                Street = "Høvågveien 1",
                City = "Lillesand",
                PostalCode = "4790",
                County = "Agder",
                Country = "Norway",
                Deadline = DateTime.Now.AddDays(1),
                Priority = NeedPriority.Low,
                ContactName = "Dani Nemee",
                ContactRole = "Crisis Leader",
                ContactEmail = "daniel@heimevernet.example",
                Description = "Need for drone observation to assess the extent of flooding in the area.",
                Status = NeedStatus.New
            }
        };

        private int _nextId = 4;
        public IEnumerable<Need> GetAll() => _needs.OrderByDescending(n => n.Priority).ThenBy(n => n.Deadline);
        public Need? GetById(int id) => _needs.FirstOrDefault(n => n.Id == id);
        public void Add(Need need)
        {
            need.Id = _nextId++;
            _needs.Add(need);
        }
    }
}
