namespace Dto.Statistics;

/// <summary>
/// DTO containing registration trend data over time
/// </summary>
public class RegistrationTrendDto
{
    /// <summary>
    /// Data points for registrations per month
    /// </summary>
    public List<TimeSeriesDataPointDto> DataPoints { get; set; } = [];

    /// <summary>
    /// Total registrations in the period
    /// </summary>
    public int TotalInPeriod { get; set; }
}
