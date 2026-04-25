using EventEase.Data;
using EventEase.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    [Authorize]
    [Route("events")]
    public class EventController : Controller
    {
        private readonly AppDbContext _context;

        public EventController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /events
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return View(events);
        }

        // GET: /events/create
        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateVenuesDropdown();
            return View();
        }

        // POST: /events/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EventName,EventDate,Description,VenueId")] Event eventModel)
        {
            if (ModelState.IsValid)
            {
                // Block duplicate: same venue, same date
                var conflict = await _context.Events.AnyAsync(e =>
                    e.VenueId == eventModel.VenueId &&
                    e.EventDate.Date == eventModel.EventDate.Date);

                if (conflict)
                {
                    var venue = await _context.Venues.FindAsync(eventModel.VenueId);
                    ModelState.AddModelError("EventDate",
                        $"A venue conflict exists: {venue?.VenueName ?? "This venue"} already has an event on {eventModel.EventDate:dd MMM yyyy}. Please choose a different date or venue.");
                }
                else
                {
                    _context.Add(eventModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{eventModel.EventName} was added successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await PopulateVenuesDropdown(eventModel.VenueId);
            return View(eventModel);
        }

        // GET: /events/edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateVenuesDropdown(eventModel.VenueId);
            return View(eventModel);
        }

        // POST: /events/edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EventName,EventDate,Description,VenueId")] Event eventModel)
        {
            if (id != eventModel.Id)
            {
                TempData["ErrorMessage"] = "Event ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                // Block duplicate: same venue, same date, excluding this event
                var conflict = await _context.Events.AnyAsync(e =>
                    e.VenueId == eventModel.VenueId &&
                    e.EventDate.Date == eventModel.EventDate.Date &&
                    e.Id != eventModel.Id);

                if (conflict)
                {
                    var venue = await _context.Venues.FindAsync(eventModel.VenueId);
                    ModelState.AddModelError("EventDate",
                        $"A venue conflict exists: {venue?.VenueName ?? "This venue"} already has an event on {eventModel.EventDate:dd MMM yyyy}. Please choose a different date or venue.");
                }
                else
                {
                    try
                    {
                        _context.Update(eventModel);
                        await _context.SaveChangesAsync();
                        TempData["SuccessMessage"] = $"{eventModel.EventName} was updated successfully.";
                        return RedirectToAction(nameof(Index));
                    }
                    catch (DbUpdateConcurrencyException)
                    {
                        if (!EventExists(eventModel.Id))
                        {
                            TempData["ErrorMessage"] = "Event not found.";
                            return RedirectToAction(nameof(Index));
                        }
                        throw;
                    }
                }
            }

            await PopulateVenuesDropdown(eventModel.VenueId);
            return View(eventModel);
        }

        // POST: /events/delete/5
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var eventModel = await _context.Events
                .Include(e => e.Venue)
                .FirstOrDefaultAsync(e => e.Id == id);

            if (eventModel == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            // Block deletion if any bookings are linked to this event
            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"\"{eventModel.EventName}\" cannot be deleted because it has active bookings. Remove those bookings first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Events.Remove(eventModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{eventModel.EventName} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id) =>
            _context.Events.Any(e => e.Id == id);

        private async Task PopulateVenuesDropdown(int? selectedVenueId = null)
        {
            var venues = await _context.Venues
                .OrderBy(v => v.VenueName)
                .ToListAsync();

            ViewBag.VenueId = new SelectList(venues, "Id", "VenueName", selectedVenueId);
        }
    }
}