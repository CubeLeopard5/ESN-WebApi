using Dto.Calendar;
using Dto.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Middlewares;
using Web.Extensions;


namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class CalendarsController(Business.Interfaces.ICalendarService calendarService, ILogger<CalendarsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<CalendarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<CalendarDto>>> GetCalendars([FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetCalendars request received - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var calendars = await calendarService.GetAllCalendarsAsync(pagination);

        logger.LogInformation("GetCalendars successful - Returned {Count} of {TotalCount} calendars",
            calendars.Items.Count, calendars.TotalCount);

        return Ok(calendars);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CalendarDto>> GetCalendar(int id)
    {
        logger.LogInformation("GetCalendar request received for {Id}", id);

        var calendar = await calendarService.GetCalendarByIdAsync(id);

        if (calendar == null)
        {
            logger.LogInformation("GetCalendar - Calendar {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetCalendar successful for {Id}", id);

        return Ok(calendar);
    }

    [HttpGet("ByEvent/{eventId}")]
    [ProducesResponseType(typeof(IEnumerable<CalendarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CalendarDto>>> GetCalendarsByEvent(int eventId)
    {
        logger.LogInformation("GetCalendarsByEvent request received for EventId {EventId}", eventId);

        var calendars = await calendarService.GetCalendarsByEventIdAsync(eventId);

        logger.LogInformation("GetCalendarsByEvent successful for EventId {EventId}", eventId);

        return Ok(calendars);
    }

    [HttpGet("available")]
    [ProducesResponseType(typeof(IEnumerable<CalendarDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<CalendarDto>>> GetAvailableCalendars()
    {
        logger.LogInformation("GetAvailableCalendars request received");

        var calendars = await calendarService.GetAvailableCalendarsAsync();

        logger.LogInformation("GetAvailableCalendars successful - Returned {Count} available calendars",
            calendars.Count());

        return Ok(calendars);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(CalendarDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CalendarDto>> PostCalendar(CalendarCreateDto createDto)
    {
        logger.LogInformation("PostCalendar request received");

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var calendar = await calendarService.CreateCalendarAsync(createDto);

        logger.LogInformation("PostCalendar successful for CalendarId {Id}", calendar.Id);

        return CreatedAtAction(nameof(GetCalendar), new { id = calendar.Id }, calendar);
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(CalendarDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<CalendarDto>> PutCalendar(int id, CalendarUpdateDto updateDto)
    {
        logger.LogInformation("PutCalendar request received for {Id}", id);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = User.GetUserEmailOrThrow();
            var calendar = await calendarService.UpdateCalendarAsync(id, updateDto, email);

            if (calendar == null)
            {
                logger.LogInformation("PutCalendar - Calendar {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("PutCalendar successful for {Id}", id);

            return Ok(calendar);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PutCalendar - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteCalendar(int id)
    {
        logger.LogInformation("DeleteCalendar request received for {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var calendar = await calendarService.DeleteCalendarAsync(id, email);

            if (calendar == null)
            {
                logger.LogInformation("DeleteCalendar - Calendar {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("DeleteCalendar successful for {Id}", id);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "DeleteCalendar - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }
}
