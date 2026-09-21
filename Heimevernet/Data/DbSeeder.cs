using Heimevernet.Models;

namespace Heimevernet.Data;

public static class DbSeeder
{
    public static void SeedNeeds(AppDbContext context)
    {
        if (context.Needs.Any())
        {
            return;
        }

        context.Needs.AddRange(
            new Need
            {
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
                ContactRole = "Crisis Leader",
                ContactPhone = "+47 123 45 678",
                Description = "Need for snow removal on E39 near Mandal.",
                Status = NeedStatus.UnderReview
            },
            new Need
            {
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
                ContactEmail = "daniel@heimevernet.example",
                ContactRole = "Crisis Leader",
                Description = "Need for drone observation to assess the extent of flooding in the area.",
                Status = NeedStatus.New
            }
        );

        context.SaveChanges();
    }
}