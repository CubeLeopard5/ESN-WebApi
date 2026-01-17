using Bo.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dto.Event;

/// <summary>
/// Filter parameters for events
/// </summary>
public class EventFilterDto
{
    /// <summary>
    /// Filter by time period
    /// </summary>
    /// <remarks>
    /// Future = only future events (default),
    /// Past = only past events,
    /// All = all events
    /// </remarks>
    [EnumDataType(typeof(EventTimeFilter))]
    public EventTimeFilter TimeFilter { get; set; } = EventTimeFilter.Future;
}
