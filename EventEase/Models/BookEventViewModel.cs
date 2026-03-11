using EventEase.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace EventEase.ViewModels
{
    public class BookEventViewModel
    {
        // Pre-populated from route — not a form field
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select an event.")]
        [Display(Name = "Event")]
        public int EventId { get; set; }

        // Derived from the selected event — not independently chosen
        public int VenueId { get; set; }
        public string VenueName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Booking date is required.")]
        [DataType(DataType.Date)]
        [Display(Name = "Booking Date")]
        public DateTime BookingDate { get; set; } = DateTime.Today;

        // Dropdown data — only events needed now
        public SelectList? Events { get; set; }
    }
}