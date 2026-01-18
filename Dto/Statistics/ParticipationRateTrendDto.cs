namespace Dto.Statistics;

/// <summary>
/// DTO containing participation rate trend over time
/// </summary>
public class ParticipationRateTrendDto
{
    /// <summary>
    /// Data points for participation rate per month
    /// </summary>
    public List<ParticipationRatePointDto> DataPoints { get; set; } = [];

    /// <summary>
    /// Average participation rate across the period
    /// </summary>
    public decimal AverageRate { get; set; }
}
