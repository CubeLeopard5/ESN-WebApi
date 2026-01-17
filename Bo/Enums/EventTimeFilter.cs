namespace Bo.Enums;

/// <summary>
/// Filter for event time period
/// </summary>
public enum EventTimeFilter
{
    /// <summary>
    /// Only future events (Calendar.EventDate >= now)
    /// </summary>
    Future = 0,

    /// <summary>
    /// Only past events (Calendar.EventDate &lt; now)
    /// </summary>
    Past = 1,

    /// <summary>
    /// All events regardless of date
    /// </summary>
    All = 2
}
