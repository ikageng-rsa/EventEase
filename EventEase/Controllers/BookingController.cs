using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    [Authorize]
    [Route("bookings")]
    public class BookingController : Controller
    {
        private readonly AppDbContext _context;

        public BookingController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /bookings
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var bookings = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Venue)
                .Include(b => b.Event)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();

            return View(bookings);
        }

        // GET: /bookings/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        // POST: /bookings/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("CustomerId,VenueId,EventId,BookingDate")] Booking booking)
        {
            if (ModelState.IsValid)
            {
                _context.Add(booking);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Booking #{booking.Id} was created successfully.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(booking.CustomerId, booking.VenueId, booking.EventId);
            return View(booking);
        }

        // GET: /bookings/edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(booking.CustomerId, booking.VenueId, booking.EventId);
            return View(booking);
        }

        // POST: /bookings/edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,VenueId,EventId,BookingDate")] Booking booking)
        {
            if (id != booking.Id)
            {
                TempData["ErrorMessage"] = "Booking ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(booking);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"Booking #{booking.Id} was updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BookingExists(booking.Id))
                    {
                        TempData["ErrorMessage"] = "Booking not found.";
                        return RedirectToAction(nameof(Index));
                    }
                    throw;
                }
            }

            await PopulateDropdowns(booking.CustomerId, booking.VenueId, booking.EventId);
            return View(booking);
        }

        // POST: /bookings/delete/5
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var booking = await _context.Bookings.FindAsync(id);
            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Bookings.Remove(booking);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Booking #{id} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool BookingExists(int id)
        {
            return _context.Bookings.Any(b => b.Id == id);
        }

        // Populates all three dropdowns, pre-selecting current values if provided
        private async Task PopulateDropdowns(
            int? selectedCustomerId = null,
            int? selectedVenueId = null,
            int? selectedEventId = null)
        {
            var customers = await _context.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();

            var venues = await _context.Venues
                .OrderBy(v => v.VenueName)
                .ToListAsync();

            var events = await _context.Events
                .Include(e => e.Venue)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            ViewBag.CustomerId = new SelectList(customers, "Id", "FullName", selectedCustomerId);
            ViewBag.VenueId = new SelectList(venues, "Id", "VenueName", selectedVenueId);

            // Show event name with date for clarity in the dropdown
            ViewBag.EventId = new SelectList(
                events.Select(e => new
                {
                    e.Id,
                    Display = $"{e.EventName} — {e.EventDate:dd MMM yyyy}"
                }),
                "Id",
                "Display",
                selectedEventId
            );
        }
    }
}