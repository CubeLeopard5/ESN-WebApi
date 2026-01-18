namespace Dto.Statistics;

/// <summary>
/// DTO containing events count over time data
/// </summary>
public class EventsOverTimeDto
{
    /// <summary>
    /// Data points for events per month
    /// </summary>
    public List<TimeSeriesDataPointDto> DataPoints { get; set; } = [];

    /// <summary>
    /// Total events in the period
    /// </summary>
    public int TotalInPeriod { get; set; }
}
