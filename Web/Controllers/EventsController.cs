using Dto.Common;
using Dto.Event;
using Dto.EventTemplate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Web.Extensions;

using Web.Middlewares;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class EventsController(Business.Interfaces.IEventService eventService, Business.Interfaces.IEventTemplateService eventTemplateService, ILogger<EventsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventDto>>> GetEvents([FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetEvents request received - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var userEmail = User.GetUserEmail(); // Nullable, retourne null si non authentifié
        var events = await eventService.GetAllEventsAsync(pagination, userEmail);

        logger.LogInformation("GetEvents successful - Returned {Count} of {TotalCount} events for user {Email}",
            events.Items.Count, events.TotalCount, userEmail ?? "anonymous");

        return Ok(events);
    }

    [Authorize]
    [HttpGet("admin/all")]
    [ProducesResponseType(typeof(IEnumerable<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetAllEventsForAdmin()
    {
        logger.LogInformation("GetAllEventsForAdmin request received");

        // Vérifier si l'utilisateur est Admin ou ESN Member
        var isAdmin = User.IsInRole("Admin");
        var studentType = User.Claims.FirstOrDefault(c => c.Type == "studentType")?.Value;
        var isEsnMember = studentType == Bo.Constants.StudentType.EsnMember;

        if (!isAdmin && !isEsnMember)
        {
            logger.LogWarning("GetAllEventsForAdmin - Forbidden: User is not Admin or ESN Member");
            return Forbid();
        }

        var events = await eventService.GetAllEventsForAdminAsync();

        logger.LogInformation("GetAllEventsForAdmin successful - Returned {Count} events (including past events)", events.Count());

        return Ok(events);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> GetEvent(int id)
    {
        logger.LogInformation("GetEvent request received for {Id}", id);

        var userEmail = User.GetUserEmail(); // Nullable, retourne null si non authentifié
        var eventDto = await eventService.GetEventByIdAsync(id, userEmail);

        if (eventDto == null)
        {
            logger.LogInformation("GetEvent - Event {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetEvent successful for {Id}, user {Email}", id, userEmail ?? "anonymous");

        return Ok(eventDto);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> PostEvent(CreateEventDto createEventDto)
    {
        logger.LogInformation("PostEvent request received with Title {Title}", createEventDto.Title);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = User.GetUserEmailOrThrow();
            var eventDto = await eventService.CreateEventAsync(createEventDto, email);

            logger.LogInformation("PostEvent successful for {Title}", eventDto.Title);

            return CreatedAtAction(nameof(GetEvent), new { id = eventDto.Id }, eventDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PostEvent - Unauthorized");
            return Unauthorized(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "PostEvent - Calendar already linked");
            return Conflict(ex.Message);
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message?.Contains("IX_Calendars_EventId_Unique") == true)
        {
            logger.LogWarning(ex, "PostEvent - Unique constraint violation");
            return Conflict("The selected calendar is already linked to another event");
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "PostEvent - Invalid argument");
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> PutEvent(int id, EventDto eventDto)
    {
        logger.LogInformation("PutEvent request received for {Id} with Title {Title}", id, eventDto.Title);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var responseDto = await eventService.UpdateEventAsync(id, eventDto, email);

            if (responseDto == null)
            {
                logger.LogInformation("PutEvent - Event {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("PutEvent successful for {Id}", id);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PutEvent - Unauthorized for {Id}", id);
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult> DeleteEvent(int id)
    {
        logger.LogInformation("DeleteEvent request received for {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await eventService.DeleteEventAsync(id, email);

            if (!result)
            {
                logger.LogInformation("DeleteEvent - Event {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("DeleteEvent successful for {Id}", id);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "DeleteEvent - Unauthorized for {Id}", id);
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{id}/register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> RegisterForEvent(int id, RegisterEventDto surveyJsData)
    {
        logger.LogInformation("RegisterForEvent request received for EventId {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var message = await eventService.RegisterForEventAsync(id, email, surveyJsData.SurveyJsData);

            logger.LogInformation("RegisterForEvent successful for EventId {Id}", id);

            return Ok(new { message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "RegisterForEvent - Unauthorized");
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "RegisterForEvent - Event {Id} not found", id);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "RegisterForEvent - Invalid operation for EventId {Id}", id);
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id}/register")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UnregisterFromEvent(int id)
    {
        logger.LogInformation("UnregisterFromEvent request received for EventId {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var message = await eventService.UnregisterFromEventAsync(id, email);

            logger.LogInformation("UnregisterFromEvent successful for EventId {Id}", id);

            return Ok(new { message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "UnregisterFromEvent - Unauthorized");
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "UnregisterFromEvent - No active registration found for EventId {Id}", id);
            return NotFound(ex.Message);
        }
    }

    [Authorize]
    [HttpGet("{id}/registrations")]
    [ProducesResponseType(typeof(EventWithRegistrationsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventWithRegistrationsDto>> GetEventRegistrations(int id)
    {
        logger.LogInformation("GetEventRegistrations request received for EventId {Id}", id);

        var result = await eventService.GetEventRegistrationsAsync(id);

        if (result == null)
        {
            logger.LogInformation("GetEventRegistrations - Event {Id} not found", id);
            return NotFound("Event not found");
        }

        logger.LogInformation("GetEventRegistrations successful for EventId {Id}", id);

        return Ok(result);
    }

    // EventTemplate routes
    [HttpGet("templates")]
    [ProducesResponseType(typeof(PagedResult<EventTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventTemplateDto>>> GetEventTemplates([FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetEventTemplates request received - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var templates = await eventTemplateService.GetAllTemplatesAsync(pagination);

        logger.LogInformation("GetEventTemplates successful - Returned {Count} of {TotalCount} templates",
            templates.Items.Count, templates.TotalCount);

        return Ok(templates);
    }

    [HttpGet("templates/{id}")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTemplateDto>> GetEventTemplate(int id)
    {
        logger.LogInformation("GetEventTemplate request received for {Id}", id);

        var template = await eventTemplateService.GetTemplateByIdAsync(id);

        if (template == null)
        {
            logger.LogInformation("GetEventTemplate - Template {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetEventTemplate successful for {Id}", id);

        return Ok(template);
    }

    [Authorize]
    [HttpPost("templates")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventTemplateDto>> PostEventTemplate(CreateEventTemplateDto createTemplateDto)
    {
        logger.LogInformation("PostEventTemplate request received with Title {Title}", createTemplateDto.Title);

        try
        {
            var template = await eventTemplateService.CreateTemplateAsync(createTemplateDto);

            logger.LogInformation("PostEventTemplate successful for {Title}", template.Title);

            return CreatedAtAction(nameof(GetEventTemplate), new { id = template.Id }, template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PostEventTemplate - Error creating template");
            return StatusCode(500, "An error occurred while creating the template");
        }
    }

    [Authorize]
    [HttpPut("templates/{id}")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventTemplateDto>> PutEventTemplate(int id, EventTemplateDto templateDto)
    {
        logger.LogInformation("PutEventTemplate request received for {Id} with Title {Title}", id, templateDto.Title);

        try
        {
            var responseDto = await eventTemplateService.UpdateTemplateAsync(id, templateDto);

            if (responseDto == null)
            {
                logger.LogInformation("PutEventTemplate - Template {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("PutEventTemplate successful for {Id}", id);

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "PutEventTemplate - Error updating template {Id}", id);
            return StatusCode(500, "An error occurred while updating the template");
        }
    }

    [Authorize]
    [HttpDelete("templates/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteEventTemplate(int id)
    {
        logger.LogInformation("DeleteEventTemplate request received for {Id}", id);

        try
        {
            var result = await eventTemplateService.DeleteTemplateAsync(id);

            if (!result)
            {
                logger.LogInformation("DeleteEventTemplate - Template {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("DeleteEventTemplate successful for {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteEventTemplate - Error deleting template {Id}", id);
            return StatusCode(500, "An error occurred while deleting the template");
        }
    }

    [Authorize]
    [HttpPost("from-template")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventDto>> CreateEventFromTemplate(CreateEventFromTemplateDto createEventFromTemplateDto)
    {
        logger.LogInformation("CreateEventFromTemplate request received with TemplateId {TemplateId} and Title {Title}",
            createEventFromTemplateDto.TemplateId, createEventFromTemplateDto.Title);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var eventDto = await eventTemplateService.CreateEventFromTemplateAsync(createEventFromTemplateDto, email);

            logger.LogInformation("CreateEventFromTemplate successful for TemplateId {TemplateId}", createEventFromTemplateDto.TemplateId);

            return CreatedAtAction(nameof(GetEvent), new { id = eventDto.Id }, eventDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "CreateEventFromTemplate - Unauthorized");
            return Unauthorized(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "CreateEventFromTemplate - Invalid template");
            return BadRequest(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{id}/save-as-template")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTemplateDto>> SaveEventAsTemplate(int id)
    {
        logger.LogInformation("SaveEventAsTemplate request received for EventId {Id}", id);

        try
        {
            var templateDto = await eventTemplateService.SaveEventAsTemplateAsync(id);

            logger.LogInformation("SaveEventAsTemplate successful for EventId {Id}", id);

            return CreatedAtAction(nameof(GetEventTemplate), new { id = templateDto.Id }, templateDto);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "SaveEventAsTemplate - Event {Id} not found", id);
            return NotFound(ex.Message);
        }
    }
}