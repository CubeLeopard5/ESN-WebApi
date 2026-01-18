namespace Dto.Statistics;

/// <summary>
/// DTO containing all dashboard statistics aggregated
/// </summary>
public class DashboardStatsDto
{
    /// <summary>
    /// Global statistics (totals and averages)
    /// </summary>
    public GlobalStatsDto GlobalStats { get; set; } = new();

    /// <summary>
    /// Events over time data
    /// </summary>
    public EventsOverTimeDto EventsOverTime { get; set; } = new();

    /// <summary>
    /// Registration trend data
    /// </summary>
    public RegistrationTrendDto RegistrationTrend { get; set; } = new();

    /// <summary>
    /// Attendance breakdown data
    /// </summary>
    public AttendanceBreakdownDto AttendanceBreakdown { get; set; } = new();

    /// <summary>
    /// Participation rate trend data
    /// </summary>
    public ParticipationRateTrendDto ParticipationTrend { get; set; } = new();

    /// <summary>
    /// Top events by registration count
    /// </summary>
    public List<TopEventDto> TopEvents { get; set; } = [];
}
