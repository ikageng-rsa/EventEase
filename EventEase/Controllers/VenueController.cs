using EventEase.Data;
using EventEase.Models;
using EventEase.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Controllers
{
    [Authorize]

    [Route("venues")]
    public class VenueController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IBlobStorageService _blob;

        private static readonly string[] AllowedTypes =
           { "image/jpeg", "image/png", "image/webp", "image/gif" };

        private const long MaxBytes = 5 * 1024 * 1024;

        public VenueController(AppDbContext context, IBlobStorageService blob)
        {
            _context = context;
            _blob = blob;
        }

        // GET: /venues
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var venues = await _context.Venues.ToListAsync();
            return View(venues);
        }

        // GET: /venues/create
        [HttpGet("create")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: /venues/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("VenueName,Location,Capacity")] Venue venue,
            IFormFile? imageFile)
        {
            // Handle image upload before model validation affects ImageUrl
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadError = ValidateImage(imageFile);
                if (uploadError != null)
                {
                    ModelState.AddModelError("imageFile", uploadError);
                    return View(venue);
                }

                using var stream = imageFile.OpenReadStream();
                venue.ImageUrl = await _blob.UploadAsync(stream, imageFile.FileName, imageFile.ContentType);
            }

            if (ModelState.IsValid)
            {
                _context.Add(venue);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{venue.VenueName} was added successfully.";
                return RedirectToAction(nameof(Index));
            }

            return View(venue);
        }

        // GET: /venues/edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(venue);
        }

        // POST: /venues/edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,VenueName,Location,Capacity,ImageUrl")] Venue venue,
            IFormFile? imageFile)
        {
            if (id != venue.Id)
            {
                TempData["ErrorMessage"] = "Venue ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            // If a new file was uploaded, replace the existing blob
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadError = ValidateImage(imageFile);
                if (uploadError != null)
                {
                    ModelState.AddModelError("imageFile", uploadError);
                    return View(venue);
                }

                // Delete old blob (safe if null)
                await _blob.DeleteAsync(venue.ImageUrl);

                using var stream = imageFile.OpenReadStream();
                venue.ImageUrl = await _blob.UploadAsync(stream, imageFile.FileName, imageFile.ContentType);
            }
            // else: keep existing ImageUrl (already bound from hidden field in the form)

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(venue);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{venue.VenueName} was updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VenueExists(venue.Id))
                    {
                        TempData["ErrorMessage"] = "Venue not found.";
                        return RedirectToAction(nameof(Index));
                    }
                    throw;
                }
            }

            return View(venue);
        }

        // POST: /venues/delete/5
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var venue = await _context.Venues.FindAsync(id);
            if (venue == null)
            {
                TempData["ErrorMessage"] = "Venue not found.";
                return RedirectToAction(nameof(Index));
            }

            // Block deletion if any bookings reference this venue
            var hasBookings = await _context.Bookings.AnyAsync(b => b.VenueId == id);
            if (hasBookings)
            {
                TempData["ErrorMessage"] = $"\"{venue.VenueName}\" cannot be deleted because it has active bookings. Remove those bookings first.";
                return RedirectToAction(nameof(Index));
            }

            // Block deletion if any events are scheduled at this venue
            var hasEvents = await _context.Events.AnyAsync(e => e.VenueId == id);
            if (hasEvents)
            {
                TempData["ErrorMessage"] = $"\"{venue.VenueName}\" cannot be deleted because it has events scheduled. Remove or reassign those events first.";
                return RedirectToAction(nameof(Index));
            }

            _context.Venues.Remove(venue);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{venue.VenueName} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool VenueExists(int id)
        {
            return _context.Venues.Any(v => v.Id == id);
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