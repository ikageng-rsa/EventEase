using System.ComponentModel.DataAnnotations;

namespace EventEase.Models
{
    public class EventType
    {
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Event Type")]
        public string Name { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Description { get; set; }

        // Navigation — one EventType has many Events
        public ICollection<Event> Events { get; set; } = new List<Event>();
    }
}