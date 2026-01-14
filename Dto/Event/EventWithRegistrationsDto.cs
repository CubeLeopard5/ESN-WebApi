using Dto.User;

namespace Dto.Event
{
    public class EventWithRegistrationsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? MaxParticipants { get; set; }
        public string? EventfrogLink { get; set; }
        public string? SurveyJsData { get; set; }
        public DateTime? CreatedAt { get; set; }
        public UserDto? Organizer { get; set; } // The event creator
        public List<EventRegistrationDto> Registrations { get; set; } = new();
        public int TotalRegistered { get; set; }
    }
}
