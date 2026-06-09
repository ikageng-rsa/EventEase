using EventEase.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace EventEase.Data
{
    public class AppDbContext : IdentityDbContext<IdentityUser>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<Customer> Customers{ get; set; }
        public DbSet<Venue> Venues{ get; set; }
        public DbSet<Event> Events{ get; set; }
        public DbSet<Booking> Bookings{ get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<EventType> EventTypes { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            
            base.OnModelCreating(builder);

            builder.Entity<Booking>()
                .HasOne(b => b.Customer)
                .WithMany()
                .HasForeignKey(b => b.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.Venue)
                .WithMany()
                .HasForeignKey(b => b.VenueId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Booking>()
                .HasOne(b => b.Event)
                .WithMany()
                .HasForeignKey(b => b.EventId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<UserProfile>()
                .HasIndex(p => p.UserId)
                .IsUnique();

            builder.Entity<Event>()
                .HasOne(e => e.EventType)
                .WithMany(et => et.Events)
                .HasForeignKey(e => e.EventTypeId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed EventTypes
            builder.Entity<EventType>().HasData(
                new EventType { Id = 1, Name = "Conference", Description = "Professional conferences and summits" },
                new EventType { Id = 2, Name = "Wedding", Description = "Wedding ceremonies and receptions" },
                new EventType { Id = 3, Name = "Concert", Description = "Live music and performance events" },
                new EventType { Id = 4, Name = "Corporate", Description = "Corporate functions and team events" },
                new EventType { Id = 5, Name = "Exhibition", Description = "Trade shows and exhibitions" },
                new EventType { Id = 6, Name = "Workshop", Description = "Training sessions and workshops" },
                new EventType { Id = 7, Name = "Social", Description = "Social gatherings and celebrations" },
                new EventType { Id = 8, Name = "Sporting", Description = "Sporting events and competitions" }
            );
        }
    }
}