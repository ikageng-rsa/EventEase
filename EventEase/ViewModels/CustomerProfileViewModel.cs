using EventEase.Models;

namespace EventEase.ViewModels
{
    public class CustomerProfileViewModel
    {
        public Customer Customer { get; set; } = null!;
        public IEnumerable<Booking> Bookings { get; set; } = new List<Booking>();

        // Stats
        public int TotalBookings => Bookings.Count();
        public int UpcomingEvents => Bookings.Count(b => b.Event?.EventDate.Date >= DateTime.Today);
        public int PastEvents => Bookings.Count(b => b.Event?.EventDate.Date < DateTime.Today);
    }
}