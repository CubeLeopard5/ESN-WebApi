namespace Dto.Statistics;

/// <summary>
/// DTO representing a top event by registration count
/// </summary>
public class TopEventDto
{
    /// <summary>
    /// Event ID
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Event title
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Event start date
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Number of registrations for this event
    /// </summary>
    public int RegistrationCount { get; set; }

    /// <summary>
    /// Maximum participants allowed (null if unlimited)
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Fill rate percentage (registrations / max participants * 100)
    /// </summary>
    public decimal? FillRate { get; set; }

    /// <summary>
    /// Attendance rate percentage (present / registered * 100)
    /// </summary>
    public decimal AttendanceRate { get; set; }
}
