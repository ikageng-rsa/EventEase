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
    [Route("customers")]
    public class CustomerController : Controller
    {
        private readonly AppDbContext _context;

        public CustomerController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /customers
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers.ToListAsync();
            return View(customers);
        }

        // GET: /customers/profile/5
        [HttpGet("profile/{id}")]
        public async Task<IActionResult> Profile(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            var bookings = await _context.Bookings
                .Include(b => b.Event)
                .Include(b => b.Venue)
                .Where(b => b.CustomerId == id)
                .OrderByDescending(b => b.Event!.EventDate)
                .ToListAsync();

            var viewModel = new CustomerProfileViewModel
            {
                Customer = customer,
                Bookings = bookings
            };

            return View(viewModel);
        }

        // GET: /customers/5/book
        [HttpGet("{id}/book")]
        public async Task<IActionResult> BookEvent(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = new BookEventViewModel
            {
                CustomerId = id,
                CustomerName = customer.FullName,
                BookingDate = DateTime.Today,
                Events = await BuildEventsSelectList()
            };

            return View(viewModel);
        }

        // POST: /customers/5/book
        [HttpPost("{id}/book")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BookEvent(int id, BookEventViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                // Load the event to get its VenueId — venue is enforced from the event
                var selectedEvent = await _context.Events
                    .Include(e => e.Venue)
                    .FirstOrDefaultAsync(e => e.Id == viewModel.EventId);

                if (selectedEvent == null)
                {
                    ModelState.AddModelError("EventId", "The selected event no longer exists.");
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
                        var booking = new Booking
                        {
                            CustomerId = id,
                            EventId = selectedEvent.Id,
                            VenueId = selectedEvent.VenueId,
                            BookingDate = selectedEvent.EventDate
                        };

                        _context.Bookings.Add(booking);
                        await _context.SaveChangesAsync();

                        TempData["SuccessMessage"] = $"Booking #{booking.Id.ToString("D4")} was created successfully for {viewModel.CustomerName}.";
                        return RedirectToAction("Index", "Booking");
                    }
                }
            }

            // Repopulate on validation failure
            var customer = await _context.Customers.FindAsync(id);
            viewModel.CustomerName = customer?.FullName ?? string.Empty;
            viewModel.Events = await BuildEventsSelectList(viewModel.EventId);

            // Re-fetch venue name if an event was selected
            if (viewModel.EventId > 0)
            {
                var ev = await _context.Events.Include(e => e.Venue).FirstOrDefaultAsync(e => e.Id == viewModel.EventId);
                if (ev?.Venue != null)
                {
                    viewModel.VenueId = ev.VenueId;
                    viewModel.VenueName = ev.Venue.VenueName;
                }
            }

            return View(viewModel);
        }

        // GET: /customers/create
        [HttpGet("create")]
        public IActionResult Create() => View();

        // POST: /customers/create
        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FullName,Email,Phone")] Customer customer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(customer);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"{customer.FullName} was added successfully.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // GET: /customers/edit/5
        [HttpGet("edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }
            return View(customer);
        }

        // POST: /customers/edit/5
        [HttpPost("edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,FullName,Email,Phone")] Customer customer)
        {
            if (id != customer.Id)
            {
                TempData["ErrorMessage"] = "Customer ID mismatch.";
                return RedirectToAction(nameof(Index));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(customer);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = $"{customer.FullName} was updated successfully.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(customer.Id))
                    {
                        TempData["ErrorMessage"] = "Customer not found.";
                        return RedirectToAction(nameof(Index));
                    }
                    throw;
                }
            }
            return View(customer);
        }

        // POST: /customers/delete/5
        [HttpPost("delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var customer = await _context.Customers.FindAsync(id);
            if (customer == null)
            {
                TempData["ErrorMessage"] = "Customer not found.";
                return RedirectToAction(nameof(Index));
            }

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"{customer.FullName} was deleted successfully.";
            return RedirectToAction(nameof(Index));
        }

        private bool CustomerExists(int id) =>
            _context.Customers.Any(c => c.Id == id);

        private async Task<SelectList> BuildEventsSelectList(int selectedId = 0)
        {
            var events = await _context.Events
                .Include(e => e.Venue)
                .OrderBy(e => e.EventDate)
                .ToListAsync();

            var items = events.Select(e => new
            {
                e.Id,
                Display = $"{e.EventName} — {e.EventDate:dd MMM yyyy} @ {e.Venue?.VenueName ?? "No venue"}"
            });

            return new SelectList(items, "Id", "Display", selectedId == 0 ? (object?)null : selectedId);
        }
    }
}