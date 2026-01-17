using Dto.User;
using System.ComponentModel.DataAnnotations;

namespace Dto.Event
{
    public class EventDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public string Location { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int MaxParticipants { get; set; }

        [Required]
        public string EventfrogLink { get; set; } = string.Empty;

        public string? SurveyJsData { get; set; }

        public int? CalendarId { get; set; }

        public int? UserId { get; set; }
        public DateTime CreatedAt { get; set; }
        public int RegisteredCount { get; set; }

        /// <summary>
        /// Indicates if the current authenticated user is registered for this event
        /// null = user not authenticated or info not calculated
        /// true = user is registered
        /// false = user is not registered
        /// </summary>
        public bool? IsCurrentUserRegistered { get; set; }

        /// <summary>
        /// JSON data for the feedback form (SurveyJS schema)
        /// </summary>
        public string? FeedbackFormData { get; set; }

        /// <summary>
        /// Deadline for submitting feedback
        /// </summary>
        public DateTime? FeedbackDeadline { get; set; }

        // Navigation properties (for GET responses)
        public UserDto? User { get; set; }
    }
}
