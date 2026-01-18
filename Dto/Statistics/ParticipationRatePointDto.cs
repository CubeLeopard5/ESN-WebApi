namespace Dto.Statistics;

/// <summary>
/// DTO representing a participation rate data point
/// </summary>
public class ParticipationRatePointDto
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
    /// Number of registrations in this period
    /// </summary>
    public int RegisteredCount { get; set; }

    /// <summary>
    /// Number of attendees marked as present in this period
    /// </summary>
    public int AttendedCount { get; set; }

    /// <summary>
    /// Participation rate percentage (attended / registered * 100)
    /// </summary>
    public decimal ParticipationRate { get; set; }
}
