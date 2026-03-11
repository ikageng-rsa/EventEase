using EventEase.Data;
using EventEase.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    [Authorize]
    [Route("dashboard")]
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);

            // ── Headline counts 
            var totalVenues = await _context.Venues.CountAsync();
            var totalEvents = await _context.Events.CountAsync();
            var totalBookings = await _context.Bookings.CountAsync();
            var totalCustomers = await _context.Customers.CountAsync();

            // ── Upcoming events (event date >= today) 
            var upcomingEvents = await _context.Events
                .CountAsync(e => e.EventDate.Date >= today);

            // ── Bookings made this calendar month 
            var bookingsThisMonth = await _context.Bookings
                .CountAsync(b => b.BookingDate >= startOfMonth);

            // ── Recent bookings (last 5 created) 
            var recentBookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .OrderByDescending(b => b.Id)
                .Take(5)
                .ToListAsync();

            // ── Next upcoming events 
            var nextEvents = await _context.Events
                .Include(e => e.Venue)
                .Where(e => e.EventDate.Date >= today)
                .OrderBy(e => e.EventDate)
                .Take(5)
                .ToListAsync();

            // ── Booking count per event (for capacity indicators) 
            var eventBookingCounts = await _context.Bookings
                .GroupBy(b => b.EventId)
                .Select(g => new { EventId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.EventId, x => x.Count);

            // ── Top venues by booking count 
            var venueBookingCounts = await _context.Bookings
                .GroupBy(b => b.VenueId)
                .Select(g => new { VenueId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.VenueId, x => x.Count);

            var allVenues = await _context.Venues.ToListAsync();

            var topVenues = allVenues
                .Select(v => new VenueBookingStat
                {
                    Venue = v,
                    BookingCount = venueBookingCounts.GetValueOrDefault(v.Id, 0)
                })
                .OrderByDescending(v => v.BookingCount)
                .Take(4)
                .ToList();

            var viewModel = new DashboardViewModel
            {
                TotalVenues = totalVenues,
                TotalEvents = totalEvents,
                TotalBookings = totalBookings,
                TotalCustomers = totalCustomers,
                UpcomingEvents = upcomingEvents,
                BookingsThisMonth = bookingsThisMonth,
                RecentBookings = recentBookings,
                NextEvents = nextEvents,
                EventBookingCounts = eventBookingCounts,
                TopVenues = topVenues
            };

            return View(viewModel);
        }
    }
}