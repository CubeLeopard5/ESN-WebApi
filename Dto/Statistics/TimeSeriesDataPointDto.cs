namespace Dto.Statistics;

/// <summary>
/// DTO representing a single data point in a time series
/// </summary>
public class TimeSeriesDataPointDto
{
    /// <summary>
    /// Label for the data point (e.g., "Jan 2026")
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// Year of the data point
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Month of the data point (1-12)
    /// </summary>
    public int Month { get; set; }

    /// <summary>
    /// Value for this data point
    /// </summary>
    public int Value { get; set; }
}
