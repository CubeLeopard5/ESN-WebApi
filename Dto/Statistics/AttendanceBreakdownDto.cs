namespace Dto.Statistics;

/// <summary>
/// DTO containing attendance status breakdown
/// </summary>
public class AttendanceBreakdownDto
{
    /// <summary>
    /// Number of participants marked as present
    /// </summary>
    public int PresentCount { get; set; }

    /// <summary>
    /// Number of participants marked as absent
    /// </summary>
    public int AbsentCount { get; set; }

    /// <summary>
    /// Number of participants marked as excused
    /// </summary>
    public int ExcusedCount { get; set; }

    /// <summary>
    /// Number of registrations not yet validated
    /// </summary>
    public int NotValidatedCount { get; set; }

    /// <summary>
    /// Total registrations count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Percentage of present attendees (of validated registrations)
    /// </summary>
    public decimal PresentPercentage { get; set; }

    /// <summary>
    /// Percentage of absent attendees (of validated registrations)
    /// </summary>
    public decimal AbsentPercentage { get; set; }

    /// <summary>
    /// Percentage of excused attendees (of validated registrations)
    /// </summary>
    public decimal ExcusedPercentage { get; set; }
}
