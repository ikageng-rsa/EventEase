using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using EventEase.Validation;

namespace EventEase.Models
{
    public class Event
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Event name is required.")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Event name must be between 2 and 100 characters.")]
        [Display(Name = "Event Name")]
        public string EventName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Event date is required.")]
        [DataType(DataType.Date)]
        [FutureDate]
        [Display(Name = "Event Date")]
        public DateTime EventDate { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 1000 characters.")]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Event Image")]
        public string? ImageUrl { get; set; }

        [Required(ErrorMessage = "Please select a venue.")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        // Navigation property
        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        [Required(ErrorMessage = "Please select an event type.")]
        [Display(Name = "Event Type")]
        public int EventTypeId { get; set; }

        [ForeignKey("EventTypeId")]
        public EventType? EventType { get; set; }
    }
}