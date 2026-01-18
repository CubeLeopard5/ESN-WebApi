namespace Dto.Statistics;

/// <summary>
/// DTO containing global statistics counts and averages
/// </summary>
public class GlobalStatsDto
{
    /// <summary>
    /// Total number of events
    /// </summary>
    public int TotalEvents { get; set; }

    /// <summary>
    /// Total number of users
    /// </summary>
    public int TotalUsers { get; set; }

    /// <summary>
    /// Total number of event registrations
    /// </summary>
    public int TotalRegistrations { get; set; }

    /// <summary>
    /// Average attendance rate across all events (percentage)
    /// </summary>
    public decimal AverageAttendanceRate { get; set; }
}
