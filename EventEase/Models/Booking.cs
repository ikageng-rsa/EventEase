using EventEase.Validation;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EventEase.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Please select a customer.")]
        [Display(Name = "Customer")]
        public int CustomerId { get; set; }

        [Required(ErrorMessage = "Please select a venue.")]
        [Display(Name = "Venue")]
        public int VenueId { get; set; }

        [Required(ErrorMessage = "Please select an event.")]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        [Required(ErrorMessage = "Booking date is required.")]
        [DataType(DataType.Date)]
        [FutureDate(ErrorMessage = "Booking date must be in the future.")]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.Today;

        // Navigation properties
        [ForeignKey("CustomerId")]
        public Customer? Customer { get; set; }

        [ForeignKey("VenueId")]
        public Venue? Venue { get; set; }

        [ForeignKey("EventId")]
        public Event? Event { get; set; }
    }
}