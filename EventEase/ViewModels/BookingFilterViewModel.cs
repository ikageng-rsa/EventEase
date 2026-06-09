using EventEase.Models;

namespace EventEase.ViewModels
{
    public class BookingFilterViewModel
    {
        // Filter inputs
        public string? SearchTerm { get; set; }
        public int? EventTypeId { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool AvailableVenuesOnly { get; set; } = false;

        // Results
        public IEnumerable<Booking> Results { get; set; } = new List<Booking>();
        public bool SearchPerformed { get; set; } = false;
        public int ResultCount => Results.Count();

        // Dropdown data
        public IEnumerable<EventType> EventTypes { get; set; } = new List<EventType>();
    }
}