using EventEase.Models;

namespace EventEase.Models
{
    public class BookingSearchViewModel
    {
        // The search term entered by the user
        public string? SearchTerm { get; set; }

        // Results returned
        public IEnumerable<Booking> Results { get; set; } = new List<Booking>();

        // Whether a search has been performed (vs. landing on the page fresh)
        public bool SearchPerformed { get; set; } = false;

        public int ResultCount => Results.Count();
    }
}