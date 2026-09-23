using System.ComponentModel.DataAnnotations;

namespace Heimevernet.Models
{
    /// <summary>
    /// Priority level for a reported need. Used to sort and triage requests.
    /// </summary>
    public enum NeedPriority
    {
        /// <summary>Low priority.</summary>
        [Display(Name = "Low")]
        Low,
        /// <summary>Medium priority.</summary>
        [Display(Name = "Midium")]
        Medium,
        /// <summary>High priority.</summary>
        [Display(Name = "High")]
        High
    }

    /// <summary>
    /// Current lifecycle status of a need.
    /// </summary>
    public enum NeedStatus
    {
        /// <summary>Initial state when a need is created.</summary>
        [Display(Name = "New")]
        New,
        /// <summary>The need is under review by responders.</summary>
        [Display(Name = "Under Review")]
        UnderReview,
        /// <summary>The need is currently being worked on.</summary>
        [Display(Name = "In Progress")]
        InProgress,
        /// <summary>The need has been completed/resolved.</summary>
        [Display(Name = "Completed")]
        Completed
    }

    /// <summary>
    /// Predefined need types used by the UI and validation logic.
    /// </summary>
    public static class NeedTypes
    {
        /// <summary>Marker value indicating a custom 'other' need type.</summary>
        public const string Other = "Other";

        /// <summary>
        /// A curated list of common need types used by the UI.
        /// </summary>
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

    /// <summary>
    /// Represents a user-submitted need / request containing contact and location information.
    /// </summary>
    public class Need
    {
        /// <summary>
        /// Database identity for the need.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Short title describing the need.
        /// </summary>
        [Required(ErrorMessage = "You must specify what the need is about.")]
        [StringLength(100, ErrorMessage = "The title cannot be longer than 100 characters.")]
        [Display(Name = "Need")]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type/category of the need (one of <see cref="NeedTypes"/> values or a custom value).
        /// </summary>
        [Required(ErrorMessage = "Need type is required.")]
        [Display(Name = "Type of Need")]
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// Optional free-text detail when <see cref="Type"/> is set to Other.
        /// </summary>
        [StringLength(50)]
        [Display(Name = "Other Type (specify)")]
        public string? OtherType { get; set; }

        /// <summary>The street portion of the location where the need applies.</summary>
        [Required(ErrorMessage = "Street is required.")]
        [StringLength(120)]
        [Display(Name = "Street")]
        public string Street { get; set; } = string.Empty;

        /// <summary>City of the need location.</summary>
        [Required(ErrorMessage = "City is required.")]
        [StringLength(80)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        /// <summary>Postal / ZIP code for the location.</summary>
        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(20)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        /// <summary>Administrative area or county.</summary>
        [Required(ErrorMessage = "County is required.")]
        [StringLength(80)]
        [Display(Name = "County")]
        public string County { get; set; } = string.Empty;

        /// <summary>Country of the location.</summary>
        [Required(ErrorMessage = "Country is required.")]
        [StringLength(80)]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// Human friendly concatenated address used for display purposes.
        /// </summary>
        public string FullAddress =>
            $"{Street}, {PostalCode} {City}, {County}, {Country}";

        /// <summary>
        /// Requested deadline or desired start time for the need.
        /// </summary>
        [Required(ErrorMessage = "You must set a deadline or desired start time.")]
        [Display(Name = "Time/Deadline")]
        [DataType(DataType.DateTime)]
        public DateTime Deadline { get; set; }

        /// <summary>Priority used to triage the need.</summary>
        [Required]
        [Display(Name = "Priority")]
        public NeedPriority Priority { get; set; } = NeedPriority.Medium;

        /// <summary>Contact person's full name for follow up.</summary>
        [Required(ErrorMessage = "Contact person's name is required.")]
        [StringLength(80)]
        [Display(Name = "Name")]
        public string ContactName { get; set; } = string.Empty;

        /// <summary>Optional role or title of the contact person.</summary>
        [StringLength(80)]
        [Display(Name = "Role")]
        public string? ContactRole { get; set; }

        /// <summary>Optional contact phone number.</summary>
        [Phone(ErrorMessage = "Please provide a valid phone number.")]
        [Display(Name = "Phone")]
        public string? ContactPhone { get; set; }

        /// <summary>Optional contact email address.</summary>
        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [Display(Name = "Email")]
        public string? ContactEmail { get; set; }

        /// <summary>Optional longer description of the need (max 500 chars).</summary>
        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        /// <summary>Current workflow status for the need.</summary>
        [Display(Name = "Status")]
        public NeedStatus Status { get; set; } = NeedStatus.New;
    }
}

    
