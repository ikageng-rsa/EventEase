using EventEase.Models;

namespace EventEase.ViewModels
{
    public class DashboardViewModel
    {
        // ── Headline stats 
        public int TotalVenues { get; set; }
        public int TotalEvents { get; set; }
        public int TotalBookings { get; set; }
        public int TotalCustomers { get; set; }

        // ── Upcoming / capacity 
        public int UpcomingEvents { get; set; }
        public int BookingsThisMonth { get; set; }

        // ── Recent bookings (last 5) 
        public IEnumerable<Booking> RecentBookings { get; set; } = new List<Booking>();

        // ── Upcoming events (next 5) 
        public IEnumerable<Event> NextEvents { get; set; } = new List<Event>();

        // ── Fully booked events 
        // EventId → booking count
        public Dictionary<int, int> EventBookingCounts { get; set; } = new();

        // ── Most booked venues (top 4) 
        public IEnumerable<VenueBookingStat> TopVenues { get; set; } = new List<VenueBookingStat>();
    }

    public class VenueBookingStat
    {
        public Venue Venue { get; set; } = null!;
        public int BookingCount { get; set; }
        public double OccupancyPercent => Venue.Capacity > 0
            ? Math.Min(100, Math.Round((double)BookingCount / Venue.Capacity * 100, 1))
            : 0;
    }
}