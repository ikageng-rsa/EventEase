using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
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
        private readonly IBlobStorageService _blob;

        private static readonly string[] AllowedTypes =
            { "image/jpeg", "image/png", "image/webp", "image/gif" };

        private const long MaxBytes = 5 * 1024 * 1024;

        public EventController(AppDbContext context, IBlobStorageService blob)
        {
            _context = context;
            _blob = blob;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .Include(e => e.EventType)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            return View(events);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            await PopulateDropdowns();
            return View();
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
             [Bind("EventName,EventDate,Description,VenueId,EventTypeId")] Event eventModel,
            IFormFile? imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadError = ValidateImage(imageFile);
                if (uploadError != null)
                {
                    ModelState.AddModelError("imageFile", uploadError);
                    await PopulateDropdowns(eventModel.VenueId, eventModel.EventTypeId);
                    return View(eventModel);
                }

                using var stream = imageFile.OpenReadStream();
                eventModel.ImageUrl = await _blob.UploadAsync(
                    stream, imageFile.FileName, imageFile.ContentType);
            }

            if (ModelState.IsValid)
            {
                var conflict = await _context.Events.AnyAsync(e =>
                    e.VenueId == eventModel.VenueId &&
                    e.EventDate.Date == eventModel.EventDate.Date);

                if (conflict)
                {
                    var venue = await _context.Venues.FindAsync(eventModel.VenueId);
                    ModelState.AddModelError("EventDate",
                        $"A venue conflict exists: {venue?.VenueName ?? "This venue"} already has an event on {eventModel.EventDate:dd MMM yyyy}. Choose a different date or venue.");
                }
                else
                {
                    _context.Add(eventModel);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{eventModel.EventName} was added successfully.";
                    return RedirectToAction(nameof(Index));
                }
            }

            await PopulateDropdowns(eventModel.VenueId, eventModel.EventTypeId);
            return View(eventModel);
        }

        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var eventModel = await _context.Events.FindAsync(id);
            if (eventModel == null)
            {
                TempData["ErrorMessage"] = "Event not found.";
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdowns(eventModel.VenueId, eventModel.EventTypeId);
            return View(eventModel);
        }

        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,EventName,EventDate,Description,VenueId,EventTypeId,ImageUrl")] Event eventModel,
            IFormFile? imageFile)
        {
            if (id != eventModel.Id)
            {
                TempData["ErrorMessage"] = "Event ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadError = ValidateImage(imageFile);
                if (uploadError != null)
                {
                    ModelState.AddModelError("imageFile", uploadError);
                    await PopulateDropdowns(eventModel.VenueId, eventModel.EventTypeId);
                    return View(eventModel);
                }

                await _blob.DeleteAsync(eventModel.ImageUrl);

                using var stream = imageFile.OpenReadStream();
                eventModel.ImageUrl = await _blob.UploadAsync(
                    stream, imageFile.FileName, imageFile.ContentType);
            }

            if (ModelState.IsValid)
            {
                var conflict = await _context.Events.AnyAsync(e =>
                    e.VenueId == eventModel.VenueId &&
                    e.EventDate.Date == eventModel.EventDate.Date &&
                    e.Id != eventModel.Id);

                if (conflict)
                {
                    var venue = await _context.Venues.FindAsync(eventModel.VenueId);
                    ModelState.AddModelError("EventDate",
                        $"A venue conflict exists: {venue?.VenueName ?? "This venue"} already has an event on {eventModel.EventDate:dd MMM yyyy}. Choose a different date or venue.");
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

            await PopulateDropdowns(eventModel.VenueId, eventModel.EventTypeId);
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

            var hasBookings = await _context.Bookings.AnyAsync(b => b.EventId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"\"{eventModel.EventName}\" cannot be deleted because it has active bookings. Remove those bookings first.";
                return RedirectToAction(nameof(Index));
            }

            await _blob.DeleteAsync(eventModel.ImageUrl);

            _context.Events.Remove(eventModel);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{eventModel.EventName} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool EventExists(int id) =>
            _context.Events.Any(e => e.Id == id);

        private async Task PopulateDropdowns(
            int? selectedVenueId = null,
            int? selectedEventTypeId = null)
        {
            var venues = await _context.Venues
                .OrderBy(v => v.VenueName)
                .ToListAsync();

            var eventTypes = await _context.EventTypes
                .OrderBy(et => et.Name)
                .ToListAsync();

            ViewBag.VenueId = new SelectList(venues, "Id", "VenueName", selectedVenueId);
            ViewBag.EventTypeId = new SelectList(eventTypes, "Id", "Name", selectedEventTypeId);
        }

        private static string? ValidateImage(IFormFile file)
        {
            if (!AllowedTypes.Contains(file.ContentType.ToLower()))
                return "Only JPEG, PNG, WebP, or GIF images are allowed.";

            if (file.Length > MaxBytes)
                return "Image must be smaller than 5 MB.";

            return null;
        }
    }
}