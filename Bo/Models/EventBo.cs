using System.ComponentModel.DataAnnotations;

namespace Bo.Models;

public partial class EventBo
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public string? Location { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? MaxParticipants { get; set; }
    public string? EventfrogLink { get; set; }
    public int UserId { get; set; }
    public DateTime? CreatedAt { get; set; }

    [MaxLength(100000)]
    public string? SurveyJsData { get; set; }

    /// <summary>
    /// Schéma du formulaire de feedback (JSON SurveyJS)
    /// </summary>
    [MaxLength(100000)]
    public string? FeedbackFormData { get; set; }

    /// <summary>
    /// Date limite pour soumettre un feedback (null = pas de limite)
    /// </summary>
    public DateTime? FeedbackDeadline { get; set; }

    public virtual UserBo User { get; set; } = null!;
    public virtual ICollection<EventRegistrationBo> EventRegistrations { get; set; } = [];
    public virtual ICollection<CalendarBo> Calendars { get; set; } = [];

    /// <summary>
    /// Collection des feedbacks soumis pour cet événement
    /// </summary>
    public virtual ICollection<EventFeedbackBo> Feedbacks { get; set; } = [];
}
