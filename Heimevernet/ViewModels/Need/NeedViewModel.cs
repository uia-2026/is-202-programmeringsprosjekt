using System.ComponentModel.DataAnnotations;
using Heimevernet.Models;

namespace Heimevernet.ViewModels.Need
{
    public class  NeedViewModel
    {
        [Required(ErrorMessage = "You must specify what the need is about.")]
        [StringLength(100, ErrorMessage = "The title cannot be longer than 100 characters.")]
        [Display(Name = "Need")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Need type is required.")]
        [Display(Name = "Type of Need")]
        public string Type { get; set; } = string.Empty;

        [StringLength(50)]
        [Display(Name = "Other Type (specify)")]
        public string? OtherType { get; set; }

        [Required(ErrorMessage = "Street is required.")]
        [StringLength(100)]
        [Display(Name = "Street")]
        public string Street { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required.")]
        [StringLength(80)]
        [Display(Name = "City")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(6)]
        [Display(Name = "Postal Code")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "County is required.")]
        [StringLength(80)]
        [Display(Name = "County")]
        public string County { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(80)]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "You must set a deadline or desired start time.")]
        [Display(Name = "Time/Deadline")]
        [DataType(DataType.DateTime)]
        public DateTime Deadline { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public NeedPriority Priority { get; set; } = NeedPriority.Medium;

        [Required(ErrorMessage = "Contact person's name is required.")]
        [StringLength(100)]
        [Display(Name = "Name")]
        public string ContactName { get; set; } = string.Empty;

   
        [StringLength(80)]
        [Display(Name = "Role")]
        public string? ContactRole { get; set; }

        [Phone(ErrorMessage = "Please provide a valid phone number.")]
        [Display(Name = "Phone")]
        public string? ContactPhone { get; set; }

        [EmailAddress(ErrorMessage = "Please provide a valid email address.")]
        [Display(Name = "Email")]
        public string? ContactEmail { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot be longer than 500 characters.")]
        [Display(Name = "Description")]
        public string? Description { get; set; }

        [Display(Name = "Status")]
        public NeedStatus Status { get; set; } = NeedStatus.New;

    }
}