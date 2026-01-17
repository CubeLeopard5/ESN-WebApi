using Dto.Common;
using Dto.Event;
using Dto.EventTemplate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Middlewares;

namespace Web.Controllers;

/// <summary>
/// Controller for managing event templates
/// </summary>
[Route("api/templates")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class EventTemplatesController(
    Business.Interfaces.IEventTemplateService eventTemplateService,
    ILogger<EventTemplatesController> logger) : ControllerBase
{
    /// <summary>
    /// Gets all event templates with pagination
    /// </summary>
    /// <param name="pagination">Pagination parameters</param>
    /// <returns>Paginated list of event templates</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<EventTemplateDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<EventTemplateDto>>> GetAllTemplates([FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetAllTemplates request received - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var templates = await eventTemplateService.GetAllTemplatesAsync(pagination);

        logger.LogInformation("GetAllTemplates successful - Returned {Count} of {TotalCount} templates",
            templates.Items.Count, templates.TotalCount);

        return Ok(templates);
    }

    /// <summary>
    /// Gets a specific event template by ID
    /// </summary>
    /// <param name="id">Template ID</param>
    /// <returns>The event template if found</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventTemplateDto>> GetTemplateById(int id)
    {
        logger.LogInformation("GetTemplateById request received for {Id}", id);

        var template = await eventTemplateService.GetTemplateByIdAsync(id);

        if (template == null)
        {
            logger.LogInformation("GetTemplateById - Template {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetTemplateById successful for {Id}", id);

        return Ok(template);
    }

    /// <summary>
    /// Creates a new event template
    /// </summary>
    /// <param name="createTemplateDto">Template data</param>
    /// <returns>The created template</returns>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventTemplateDto>> CreateTemplate(CreateEventTemplateDto createTemplateDto)
    {
        logger.LogInformation("CreateTemplate request received with Title {Title}", createTemplateDto.Title);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var template = await eventTemplateService.CreateTemplateAsync(createTemplateDto);

            logger.LogInformation("CreateTemplate successful for {Title}", template.Title);

            return CreatedAtAction(nameof(GetTemplateById), new { id = template.Id }, template);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CreateTemplate - Error creating template");
            return StatusCode(500, "An error occurred while creating the template");
        }
    }

    /// <summary>
    /// Updates an existing event template
    /// </summary>
    /// <param name="id">Template ID to update</param>
    /// <param name="templateDto">Updated template data</param>
    /// <returns>The updated template</returns>
    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<EventTemplateDto>> UpdateTemplate(int id, EventTemplateDto templateDto)
    {
        logger.LogInformation("UpdateTemplate request received for {Id} with Title {Title}", id, templateDto.Title);

        try
        {
            var responseDto = await eventTemplateService.UpdateTemplateAsync(id, templateDto);

            if (responseDto == null)
            {
                logger.LogInformation("UpdateTemplate - Template {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("UpdateTemplate successful for {Id}", id);

            return Ok(responseDto);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "UpdateTemplate - Error updating template {Id}", id);
            return StatusCode(500, "An error occurred while updating the template");
        }
    }

    /// <summary>
    /// Deletes an event template
    /// </summary>
    /// <param name="id">Template ID to delete</param>
    /// <returns>NoContent if successful</returns>
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult> DeleteTemplate(int id)
    {
        logger.LogInformation("DeleteTemplate request received for {Id}", id);

        try
        {
            var result = await eventTemplateService.DeleteTemplateAsync(id);

            if (!result)
            {
                logger.LogInformation("DeleteTemplate - Template {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("DeleteTemplate successful for {Id}", id);

            return NoContent();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "DeleteTemplate - Error deleting template {Id}", id);
            return StatusCode(500, "An error occurred while deleting the template");
        }
    }

    /// <summary>
    /// Creates a new event from a template
    /// </summary>
    /// <param name="id">Template ID to apply</param>
    /// <param name="applyDto">Event-specific data (dates, location, capacity)</param>
    /// <returns>The created event</returns>
    [Authorize]
    [HttpPost("{id}/apply")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventDto>> ApplyTemplate(int id, ApplyTemplateDto applyDto)
    {
        logger.LogInformation("ApplyTemplate request received for TemplateId {TemplateId} with Title {Title}",
            id, applyDto.Title);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized("User email not found in token");
            }

            // Convert ApplyTemplateDto to CreateEventFromTemplateDto
            var createEventFromTemplateDto = new CreateEventFromTemplateDto
            {
                TemplateId = id,
                Title = applyDto.Title,
                Description = applyDto.Description,
                Location = applyDto.Location,
                StartDate = applyDto.StartDate,
                EndDate = applyDto.EndDate,
                MaxParticipants = applyDto.MaxParticipants,
                EventfrogLink = applyDto.EventfrogLink,
                SurveyJsData = applyDto.SurveyJsData
            };

            var eventDto = await eventTemplateService.CreateEventFromTemplateAsync(createEventFromTemplateDto, email);

            logger.LogInformation("ApplyTemplate successful for TemplateId {TemplateId}", id);

            return CreatedAtAction(
                actionName: "GetEvent",
                controllerName: "Events",
                routeValues: new { id = eventDto.Id },
                value: eventDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "ApplyTemplate - Unauthorized");
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "ApplyTemplate - Template or user not found");
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "ApplyTemplate - Invalid template");
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Saves an existing event as a new template
    /// </summary>
    /// <param name="eventId">Event ID to save as template</param>
    /// <returns>The created template</returns>
    [Authorize]
    [HttpPost("from-event/{eventId}")]
    [ProducesResponseType(typeof(EventTemplateDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<EventTemplateDto>> SaveEventAsTemplate(int eventId)
    {
        logger.LogInformation("SaveEventAsTemplate request received for EventId {Id}", eventId);

        try
        {
            var templateDto = await eventTemplateService.SaveEventAsTemplateAsync(eventId);

            logger.LogInformation("SaveEventAsTemplate successful for EventId {Id}", eventId);

            return CreatedAtAction(nameof(GetTemplateById), new { id = templateDto.Id }, templateDto);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "SaveEventAsTemplate - Event {Id} not found", eventId);
            return NotFound(ex.Message);
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "SaveEventAsTemplate - Event {Id} not found", eventId);
            return NotFound(ex.Message);
        }
    }
}
