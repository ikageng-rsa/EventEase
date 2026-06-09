using EventEase.Data;
using EventEase.Models;
using EventEase.ViewModels;
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
        public async Task<IActionResult> Create([Bind("CustomerId,EventId")] Booking booking)
        {

            if (ModelState.IsValid)
            {
                // Load the event to enforce venue and date server-side
                var selectedEvent = await _context.Events
                    .Include(e => e.Venue)
                    .FirstOrDefaultAsync(e => e.Id == booking.EventId);

                if (!selectedEvent.Venue!.IsAvailable)
                {
                    ModelState.AddModelError("EventId",
                        $"{selectedEvent.Venue.VenueName} is currently unavailable for booking.");
                }
                else
                {

                    if (selectedEvent == null)
                    {
                        ModelState.AddModelError("EventId", "The selected event no longer exists.");
                    }
                    else
                    {
                        // Prevent same customer booking the same event twice
                        var alreadyBooked = await _context.Bookings.AnyAsync(b =>
                            b.CustomerId == booking.CustomerId &&
                            b.EventId == selectedEvent.Id);

                        if (alreadyBooked)
                        {
                            ModelState.AddModelError("EventId",
                                "This customer already has a booking for this event.");
                        }
                        else
                        {
                            // Capacity check — count existing bookings for this event
                            var existingBookings = await _context.Bookings
                                .CountAsync(b => b.EventId == selectedEvent.Id);

                            if (existingBookings >= selectedEvent.Venue!.Capacity)
                            {
                                ModelState.AddModelError("EventId",
                                    $"This event is fully booked. {selectedEvent.Venue.VenueName} has a capacity of {selectedEvent.Venue.Capacity} and all spots are taken.");
                            }
                            else
                            {
                                booking.VenueId = selectedEvent.VenueId;
                                booking.BookingDate = selectedEvent.EventDate;

                                _context.Add(booking);
                                await _context.SaveChangesAsync();
                                TempData["SuccessMessage"] = $"Booking #{booking.Id.ToString("D4")} was created successfully.";
                                return RedirectToAction(nameof(Index));
                            }
                        }
                    }
                }
            }

            await PopulateDropdowns(booking.CustomerId, booking.EventId);
            return View(booking);
        }

        // GET: /bookings/edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Event)
                .ThenInclude(e => e!.Venue)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(booking.CustomerId, booking.EventId);
            return View(booking);
        }

        // POST: /bookings/edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,CustomerId,EventId")] Booking booking)
        {
            if (id != booking.Id)
            {
                TempData["ErrorMessage"] = "Booking ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                // Load the event to enforce venue and date server-side
                var selectedEvent = await _context.Events
                    .Include(e => e.Venue)
                    .FirstOrDefaultAsync(e => e.Id == booking.EventId);

                if (selectedEvent == null)
                {
                    ModelState.AddModelError("EventId", "The selected event no longer exists.");
                }
                else
                {
                    // Prevent same customer booking the same event twice (exclude current booking)
                    var alreadyBooked = await _context.Bookings.AnyAsync(b =>
                        b.CustomerId == booking.CustomerId &&
                        b.EventId == selectedEvent.Id &&
                        b.Id != booking.Id);

                    if (alreadyBooked)
                    {
                        ModelState.AddModelError("EventId",
                            "This customer already has a booking for this event.");
                    }
                    else
                    {
                        // Capacity check — exclude the current booking from the count
                        // so editing a booking for the same event doesn't falsely trigger the limit
                        var existingBookings = await _context.Bookings
                            .CountAsync(b => b.EventId == selectedEvent.Id && b.Id != booking.Id);

                        if (existingBookings >= selectedEvent.Venue!.Capacity)
                        {
                            ModelState.AddModelError("EventId",
                                $"This event is fully booked. {selectedEvent.Venue.VenueName} has a capacity of {selectedEvent.Venue.Capacity} and all spots are taken.");
                        }
                        else
                        {
                            booking.VenueId = selectedEvent.VenueId;
                            booking.BookingDate = selectedEvent.EventDate;

                            try
                            {
                                _context.Update(booking);
                                await _context.SaveChangesAsync();
                                TempData["SuccessMessage"] = $"Booking #{booking.Id.ToString("D4")} was updated successfully.";
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
                    }
                }
            }

            await PopulateDropdowns(booking.CustomerId, booking.EventId);
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
            TempData["SuccessMessage"] = $"Booking #{id.ToString("D4")} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /bookings/search
        [HttpGet("search")]
        public async Task<IActionResult> Search(
            string? q,
            int? eventTypeId,
            DateTime? dateFrom,
            DateTime? dateTo,
            bool availableVenuesOnly = false)
        {
            var eventTypes = await _context.EventTypes
                .OrderBy(et => et.Name)
                .ToListAsync();

            var viewModel = new BookingFilterViewModel
            {
                SearchTerm = q,
                EventTypeId = eventTypeId,
                DateFrom = dateFrom,
                DateTo = dateTo,
                AvailableVenuesOnly = availableVenuesOnly,
                EventTypes = eventTypes,
                SearchPerformed = q != null ||
                    eventTypeId.HasValue ||
                    dateFrom.HasValue ||
                    dateTo.HasValue ||
                    availableVenuesOnly
            };

            if (viewModel.SearchPerformed)
            {
                var query = _context.Bookings
                    .Include(b => b.Customer)
                    .Include(b => b.Event)
                        .ThenInclude(e => e!.EventType)
                    .Include(b => b.Event)
                        .ThenInclude(e => e!.Venue)
                    .Include(b => b.Venue)
                    .AsQueryable();

                // Filter by Booking ID or Event Name
                if (!string.IsNullOrWhiteSpace(q))
                {
                    var term = q.Trim();
                    bool isNumeric = int.TryParse(term, out int bookingId);
                    query = isNumeric
                        ? query.Where(b => b.Id == bookingId)
                        : query.Where(b => b.Event!.EventName.Contains(term));
                }

                // Filter by EventType
                if (eventTypeId.HasValue)
                    query = query.Where(b => b.Event!.EventTypeId == eventTypeId.Value);

                // Filter by date range
                if (dateFrom.HasValue)
                    query = query.Where(b => b.Event!.EventDate.Date >= dateFrom.Value.Date);

                if (dateTo.HasValue)
                    query = query.Where(b => b.Event!.EventDate.Date <= dateTo.Value.Date);

                // Filter by venue availability
                if (availableVenuesOnly)
                    query = query.Where(b => b.Venue!.IsAvailable);

                viewModel.Results = await query
                    .OrderByDescending(b => b.BookingDate)
                    .ToListAsync();
            }

            return View(viewModel);
        }

        // GET: /bookings/details/5
        [HttpGet("details/{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var booking = await _context.Bookings
                .Include(b => b.Customer)
                .Include(b => b.Event)
                    .ThenInclude(e => e!.Venue)
                .Include(b => b.Venue)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (booking == null)
            {
                TempData["ErrorMessage"] = "Booking not found.";
                return RedirectToAction(nameof(Search));
            }

            return View(booking);
        }

        // GET: /bookings/event-details/5
        // Returns venue and date for a given event as JSON — called by Create/Edit forms via JS
        [HttpGet("event-details/{eventId}")]
        public async Task<IActionResult> GetEventDetails(int eventId)
        {
            var ev = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == eventId);

            if (ev == null)
                return Json(new { venueId = 0, venueName = "", eventDate = "", eventDateDisplay = "" });

            return Json(new
            {
                venueId = ev.VenueId,
                venueName = ev.Venue?.VenueName ?? "",
                eventDate = ev.EventDate.ToString("yyyy-MM-dd"),
                eventDateDisplay = ev.EventDate.ToString("dd MMM yyyy")
            });
        }

        private bool BookingExists(int id) =>
            _context.Bookings.Any(b => b.Id == id);

        private async Task PopulateDropdowns(int? selectedCustomerId = null, int? selectedEventId = null)
        {
            var customers = await _context.Customers
                .OrderBy(c => c.FullName)
                .ToListAsync();

            var events = await _context.Events
                .Include(e => e.Venue)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            ViewBag.CustomerId = new SelectList(customers, "Id", "FullName", selectedCustomerId);

            ViewBag.EventId = new SelectList(
                events.Select(e => new
                {
                    e.Id,
                    Display = $"{e.EventName} — {e.EventDate:dd MMM yyyy} @ {e.Venue?.VenueName ?? "No venue"}"
                }),
                "Id", "Display", selectedEventId
            );
        }
    }
}