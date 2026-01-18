using AutoMapper;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Common;
using Dto.Event;
using Dto.EventTemplate;
using Microsoft.Extensions.Logging;

namespace Business.EventTemplate;

/// <summary>
/// Interface de gestion des templates d'événements réutilisables
/// </summary>
public class EventTemplateService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<EventTemplateService> logger)
    : IEventTemplateService
{
    /// <inheritdoc />
    [Obsolete("Use GetAllTemplatesAsync(PaginationParams pagination) instead for better performance and memory management")]
    public async Task<IEnumerable<EventTemplateDto>> GetAllTemplatesAsync()
    {
        logger.LogInformation("EventTemplateService.GetAllTemplatesAsync called (non-paginated - deprecated)");

        var templates = await unitOfWork.EventTemplates.GetAllTemplatesAsync();

        var templateDtos = mapper.Map<IEnumerable<EventTemplateDto>>(templates);

        logger.LogInformation("EventTemplateService.GetAllTemplatesAsync completed, returning {Count} templates", templates.Count());

        return templateDtos;
    }

    /// <inheritdoc />
    public async Task<PagedResult<EventTemplateDto>> GetAllTemplatesAsync(PaginationParams pagination)
    {
        logger.LogInformation("EventTemplateService.GetAllTemplatesAsync (paginated) called - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var (items, totalCount) = await unitOfWork.EventTemplates.GetPagedAsync(
            pagination.Skip,
            pagination.PageSize);

        var dtos = mapper.Map<List<EventTemplateDto>>(items);

        logger.LogInformation("EventTemplateService.GetAllTemplatesAsync (paginated) completed - Returned {Count} of {TotalCount}",
            dtos.Count, totalCount);

        return new PagedResult<EventTemplateDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <inheritdoc />
    public async Task<EventTemplateDto?> GetTemplateByIdAsync(int id)
    {
        logger.LogInformation("EventTemplateService.GetTemplateByIdAsync called for TemplateId {Id}", id);

        var template = await unitOfWork.EventTemplates.GetByIdAsync(id);

        if (template == null)
        {
            logger.LogWarning("EventTemplateService.GetTemplateByIdAsync - Template {Id} not found", id);
            return null;
        }

        var templateDto = mapper.Map<EventTemplateDto>(template);

        logger.LogInformation("EventTemplateService.GetTemplateByIdAsync completed for TemplateId {Id}", id);

        return templateDto;
    }

    /// <inheritdoc />
    public async Task<EventTemplateDto> CreateTemplateAsync(CreateEventTemplateDto createTemplateDto)
    {
        logger.LogInformation("EventTemplateService.CreateTemplateAsync called with Title {Title}",
            createTemplateDto.Title);

        var template = mapper.Map<Bo.Models.EventTemplateBo>(createTemplateDto);

        await unitOfWork.EventTemplates.AddAsync(template);
        await unitOfWork.SaveChangesAsync();

        var templateDto = mapper.Map<EventTemplateDto>(template);

        logger.LogInformation("EventTemplateService.CreateTemplateAsync completed, created TemplateId {Id}",
            template.Id);

        return templateDto;
    }

    /// <inheritdoc />
    public async Task<EventTemplateDto?> UpdateTemplateAsync(int id, EventTemplateDto templateDto)
    {
        logger.LogInformation("EventTemplateService.UpdateTemplateAsync called for TemplateId {Id}", id);

        var template = await unitOfWork.EventTemplates.GetByIdAsync(id);

        if (template == null)
        {
            logger.LogWarning("EventTemplateService.UpdateTemplateAsync - Template {Id} not found", id);
            return null;
        }

        template.Title = templateDto.Title;
        template.Description = templateDto.Description;
        template.SurveyJsData = templateDto.SurveyJsData;
        template.OrganizerNotes = templateDto.OrganizerNotes;

        unitOfWork.EventTemplates.Update(template);
        await unitOfWork.SaveChangesAsync();

        var updatedTemplateDto = mapper.Map<EventTemplateDto>(template);

        logger.LogInformation("EventTemplateService.UpdateTemplateAsync completed for TemplateId {Id}", id);

        return updatedTemplateDto;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteTemplateAsync(int id)
    {
        logger.LogInformation("EventTemplateService.DeleteTemplateAsync called for TemplateId {Id}", id);

        var template = await unitOfWork.EventTemplates.GetByIdAsync(id);

        if (template == null)
        {
            logger.LogWarning("EventTemplateService.DeleteTemplateAsync - Template {Id} not found", id);
            return false;
        }

        unitOfWork.EventTemplates.Delete(template);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("EventTemplateService.DeleteTemplateAsync completed for TemplateId {Id}", id);

        return true;
    }

    /// <inheritdoc />
    public async Task<EventDto> CreateEventFromTemplateAsync(CreateEventFromTemplateDto createEventFromTemplateDto, string userEmail)
    {
        logger.LogInformation("EventTemplateService.CreateEventFromTemplateAsync called with TemplateId {TemplateId} by {Email}",
            createEventFromTemplateDto.TemplateId, userEmail);

        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("EventTemplateService.CreateEventFromTemplateAsync failed - user not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        var template = await unitOfWork.EventTemplates.GetByIdAsync(createEventFromTemplateDto.TemplateId);
        if (template == null)
        {
            logger.LogError("EventTemplateService.CreateEventFromTemplateAsync failed - template {TemplateId} not found",
                createEventFromTemplateDto.TemplateId);
            throw new ArgumentException($"Template not found: {createEventFromTemplateDto.TemplateId}");
        }

        var evt = new Bo.Models.EventBo
        {
            Title = createEventFromTemplateDto.Title,
            Description = createEventFromTemplateDto.Description ?? template.Description,
            Location = createEventFromTemplateDto.Location,
            StartDate = createEventFromTemplateDto.StartDate,
            EndDate = createEventFromTemplateDto.EndDate,
            MaxParticipants = createEventFromTemplateDto.MaxParticipants,
            EventfrogLink = createEventFromTemplateDto.EventfrogLink,
            SurveyJsData = createEventFromTemplateDto.SurveyJsData ?? template.SurveyJsData,
            OrganizerNotes = createEventFromTemplateDto.OrganizerNotes ?? template.OrganizerNotes,
            UserId = user.Id
        };

        await unitOfWork.Events.AddAsync(evt);
        await unitOfWork.SaveChangesAsync();

        var eventDto = mapper.Map<EventDto>(evt);

        logger.LogInformation("EventTemplateService.CreateEventFromTemplateAsync completed, created EventId {Id}",
            evt.Id);

        return eventDto;
    }

    /// <inheritdoc />
    public async Task<EventTemplateDto> SaveEventAsTemplateAsync(int eventId)
    {
        logger.LogInformation("EventTemplateService.SaveEventAsTemplateAsync called for EventId {EventId}", eventId);

        var evt = await unitOfWork.Events.GetByIdAsync(eventId);
        if (evt == null)
        {
            logger.LogError("EventTemplateService.SaveEventAsTemplateAsync failed - event {EventId} not found", eventId);
            throw new ArgumentException($"Event not found: {eventId}");
        }

        var template = new Bo.Models.EventTemplateBo
        {
            Title = evt.Title,
            Description = evt.Description ?? string.Empty,
            SurveyJsData = evt.SurveyJsData ?? string.Empty,
            OrganizerNotes = evt.OrganizerNotes
        };

        await unitOfWork.EventTemplates.AddAsync(template);
        await unitOfWork.SaveChangesAsync();

        var templateDto = mapper.Map<EventTemplateDto>(template);

        logger.LogInformation("EventTemplateService.SaveEventAsTemplateAsync completed, created TemplateId {Id}",
            template.Id);

        return templateDto;
    }
}
