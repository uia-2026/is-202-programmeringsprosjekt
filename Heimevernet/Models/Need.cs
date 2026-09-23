using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Models

{
    public enum NeedPriority
    {
        [Display(Name = "Low")]
        Low,
        [Display(Name = "Midium")]
        Medium,
        [Display(Name = "High")]
        High
    }

    public enum NeedStatus
    {
        [Display(Name = "New")]
        New,
        [Display(Name = "Under Review")]
        UnderReview,
        [Display(Name = "In Progress")]
        InProgress,
        [Display(Name = "Completed")]
        Completed
    }

    public static class NeedTypes
    {
        public const string Other = "Other";

        public static readonly List<string> All = new()
        {
            "Transport",
            "Drone Observation",
            "Power/Generator",
            "Snow Removal",
            "Sand/Gravel",
            "Machinery",
            "Evacuation",
            "Communications",
            "Personnel",
            "Facilities",
            Other
        };
    }
    public class Need
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? OtherType { get; set;}
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;

        public string FullAddress =>
            $"{Street}, {PostalCode}, {City}, {County}, {Country}";

        public DateTime Deadline { get; set; }
        public NeedPriority Priority { get; set; } = NeedPriority.Medium;
        public string ContactName { get; set; } = string.Empty;
        public string? ContactRole { get; set; }
        public string?ContactPhone { get; set; }
        public string? ContactEmail { get; set; }
        public string? Description { get; set; }
        public NeedStatus Status { get; set; } = NeedStatus.New;
    }
    
}

    
