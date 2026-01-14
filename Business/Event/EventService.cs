using AutoMapper;
using Bo.Constants;
using Bo.Models;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Common;
using Dto.Event;
using Microsoft.Extensions.Logging;

namespace Business.Event;

/// <summary>
/// Service de gestion des événements et inscriptions
/// </summary>
public class EventService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<EventService> logger)
    : IEventService
{
    /// <inheritdoc />
    [Obsolete("Use GetAllEventsAsync(PaginationParams pagination) instead for better performance and memory management")]
    public async Task<IEnumerable<EventDto>> GetAllEventsAsync()
    {
        logger.LogInformation("EventService.GetAllEventsAsync called (non-paginated - deprecated)");

        var events = await unitOfWork.Events.GetAllEventsWithDetailsAsync();

        var eventDtos = events.Select(e =>
        {
            var dto = mapper.Map<EventDto>(e);
            dto.RegisteredCount = e.EventRegistrations.Count(r => r.Status == RegistrationStatus.Registered);
            return dto;
        });

        logger.LogInformation("EventService.GetAllEventsAsync completed, returning {Count} events", events.Count());

        return eventDtos;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventDto>> GetAllEventsForAdminAsync()
    {
        logger.LogInformation("EventService.GetAllEventsForAdminAsync called - Admin access");

        var events = await unitOfWork.Events.GetAllEventsForAdminAsync();

        var eventDtos = events.Select(e =>
        {
            var dto = mapper.Map<EventDto>(e);
            dto.RegisteredCount = e.EventRegistrations.Count(r => r.Status == RegistrationStatus.Registered);
            return dto;
        });

        logger.LogInformation("EventService.GetAllEventsForAdminAsync completed, returning {Count} events (including past events)", events.Count());

        return eventDtos;
    }

    /// <inheritdoc />
    public async Task<PagedResult<EventDto>> GetAllEventsAsync(PaginationParams pagination, string? userEmail = null)
    {
        logger.LogInformation("EventService.GetAllEventsAsync (paginated) called - Page {PageNumber}, Size {PageSize}, UserEmail {UserEmail}",
            pagination.PageNumber, pagination.PageSize, userEmail ?? "anonymous");

        // Récupération paginée optimisée - évite le problème N+1
        var (events, totalCount) = await unitOfWork.Events.GetEventsPagedAsync(
            pagination.Skip,
            pagination.PageSize);

        // Mapping avec calcul du RegisteredCount
        var eventDtos = events.Select(e =>
        {
            var dto = mapper.Map<EventDto>(e);
            dto.RegisteredCount = e.EventRegistrations.Count(r => r.Status == RegistrationStatus.Registered);
            return dto;
        }).ToList();

        // Optimisation N+1: récupérer toutes les inscriptions de l'utilisateur en une seule requête
        if (!string.IsNullOrEmpty(userEmail))
        {
            var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
            if (user != null)
            {
                var eventIds = eventDtos.Select(e => e.Id).ToArray();

                // UNE SEULE requête pour toutes les inscriptions de l'utilisateur
                var userRegistrations = await unitOfWork.EventRegistrations
                    .GetByUserAndEventsAsync(user.Id, eventIds);

                // Mapper dans un dictionnaire pour lookup O(1)
                var registrationDict = userRegistrations
                    .Where(r => r.Status == RegistrationStatus.Registered)
                    .ToDictionary(r => r.EventId);

                // Assigner IsCurrentUserRegistered à chaque EventDto
                foreach (var eventDto in eventDtos)
                {
                    eventDto.IsCurrentUserRegistered = registrationDict.ContainsKey(eventDto.Id);
                }

                logger.LogInformation("EventService.GetAllEventsAsync - User {Email} has {Count} registrations in this page",
                    userEmail, registrationDict.Count);
            }
        }

        logger.LogInformation("EventService.GetAllEventsAsync (paginated) completed - Returned {Count} of {TotalCount} events",
            eventDtos.Count, totalCount);

        return new PagedResult<EventDto>(eventDtos, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetEventByIdAsync(int id, string? userEmail = null)
    {
        logger.LogInformation("EventService.GetEventByIdAsync called for EventId {Id}, UserEmail {UserEmail}",
            id, userEmail ?? "anonymous");

        var evt = await unitOfWork.Events.GetEventWithDetailsAsync(id);

        if (evt == null)
        {
            logger.LogWarning("EventService.GetEventByIdAsync - Event {Id} not found", id);
            return null;
        }

        var eventDto = mapper.Map<EventDto>(evt);
        eventDto.RegisteredCount = evt.EventRegistrations.Count(r => r.Status == RegistrationStatus.Registered);

        // Vérifier si l'utilisateur est inscrit à cet événement
        if (!string.IsNullOrEmpty(userEmail))
        {
            var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
            if (user != null)
            {
                var registration = await unitOfWork.Events.GetRegistrationAsync(id, user.Id);
                eventDto.IsCurrentUserRegistered = registration?.Status == RegistrationStatus.Registered;

                logger.LogInformation("EventService.GetEventByIdAsync - User {Email} registration status for EventId {Id}: {IsRegistered}",
                    userEmail, id, eventDto.IsCurrentUserRegistered);
            }
        }

        logger.LogInformation("EventService.GetEventByIdAsync completed for EventId {Id}", id);

        return eventDto;
    }

    /// <inheritdoc />
    public async Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, string userEmail)
    {
        logger.LogInformation("EventService.CreateEventAsync called with Title {Title} by {Email}",
            createEventDto.Title, userEmail);

        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("EventService.CreateEventAsync failed - user not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        // Valider CalendarId si fourni
        Bo.Models.CalendarBo? calendar = null;
        if (createEventDto.CalendarId.HasValue)
        {
            calendar = await unitOfWork.Calendars.GetByIdAsync(createEventDto.CalendarId.Value);
            if (calendar == null)
            {
                logger.LogError("EventService.CreateEventAsync - Calendar {CalendarId} not found",
                    createEventDto.CalendarId.Value);
                throw new ArgumentException($"Calendar not found: {createEventDto.CalendarId.Value}");
            }

            if (calendar.EventId.HasValue)
            {
                logger.LogError("EventService.CreateEventAsync - Calendar {CalendarId} already linked to Event {EventId}",
                    calendar.Id, calendar.EventId.Value);
                throw new InvalidOperationException(
                    $"Calendar '{calendar.Title}' is already linked to another event");
            }
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var evt = mapper.Map<Bo.Models.EventBo>(createEventDto);
            evt.UserId = user.Id;
            evt.CreatedAt = DateTime.UtcNow;

            await unitOfWork.Events.AddAsync(evt);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("EventService.CreateEventAsync - Event created with Id {Id}", evt.Id);

            // Lier calendar si fourni
            if (calendar != null)
            {
                calendar.EventId = evt.Id;
                unitOfWork.Calendars.Update(calendar);
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation(
                    "EventService.CreateEventAsync - Calendar {CalendarId} linked to Event {EventId}",
                    calendar.Id, evt.Id);
            }

            await unitOfWork.CommitTransactionAsync();

            // Reload with navigation properties
            var createdEvent = await unitOfWork.Events.GetEventWithDetailsAsync(evt.Id);

            var responseDto = mapper.Map<EventDto>(createdEvent!);
            responseDto.RegisteredCount = 0;

            logger.LogInformation("EventService.CreateEventAsync completed for {Title} by {Email}",
                evt.Title, user.Email);

            return responseDto;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<EventDto?> UpdateEventAsync(int id, EventDto eventDto, string userEmail)
    {
        logger.LogInformation("EventService.UpdateEventAsync called for EventId {Id} with Title {Title} by {Email}",
            id, eventDto.Title, userEmail);

        var existing = await unitOfWork.Events.GetEventWithDetailsAsync(id);

        if (existing == null)
        {
            logger.LogWarning("EventService.UpdateEventAsync - Event {Id} not found", id);
            return null;
        }

        // Check if the current user is the event creator or an esn_member
        var currentUser = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (currentUser == null || (existing.UserId != currentUser.Id && currentUser.StudentType != Bo.Constants.StudentType.EsnMember))
        {
            logger.LogWarning("EventService.UpdateEventAsync - Unauthorized update attempt for EventId {Id} by {Email}",
                id, userEmail);
            throw new UnauthorizedAccessException($"User {userEmail} is not authorized to update this event");
        }

        // Map only editable fields
        existing.Title = eventDto.Title;
        existing.Description = eventDto.Description;
        existing.Location = eventDto.Location;
        existing.StartDate = eventDto.StartDate;
        existing.EndDate = eventDto.EndDate;
        existing.MaxParticipants = eventDto.MaxParticipants;
        existing.EventfrogLink = eventDto.EventfrogLink;
        existing.SurveyJsData = eventDto.SurveyJsData;

        unitOfWork.Events.Update(existing);

        try
        {
            // Get current calendar linked to this event
            var currentCalendar = await unitOfWork.Calendars.GetCalendarByEventIdAsync(id);
            var currentCalendarId = currentCalendar?.Id;

            // Handle calendar update if changed
            if (eventDto.CalendarId != currentCalendarId)
            {
                // Detach old calendar if exists
                if (currentCalendar != null)
                {
                    currentCalendar.EventId = null;
                    unitOfWork.Calendars.Update(currentCalendar);
                    logger.LogInformation("EventService.UpdateEventAsync - Detached Calendar {CalendarId} from Event {EventId}",
                        currentCalendar.Id, id);
                }

                // Attach new calendar if provided
                if (eventDto.CalendarId.HasValue)
                {
                    var newCalendar = await unitOfWork.Calendars.GetByIdAsync(eventDto.CalendarId.Value);
                    if (newCalendar == null)
                    {
                        logger.LogError("EventService.UpdateEventAsync - Calendar {CalendarId} not found",
                            eventDto.CalendarId.Value);
                        throw new ArgumentException($"Calendar not found: {eventDto.CalendarId.Value}");
                    }

                    if (newCalendar.EventId.HasValue && newCalendar.EventId.Value != id)
                    {
                        logger.LogError("EventService.UpdateEventAsync - Calendar {CalendarId} already linked to Event {EventId}",
                            newCalendar.Id, newCalendar.EventId.Value);
                        throw new InvalidOperationException(
                            $"Calendar '{newCalendar.Title}' is already linked to another event");
                    }

                    newCalendar.EventId = id;
                    unitOfWork.Calendars.Update(newCalendar);
                    logger.LogInformation("EventService.UpdateEventAsync - Linked Calendar {CalendarId} to Event {EventId}",
                        newCalendar.Id, id);
                }
            }

            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("EventService.UpdateEventAsync - Event {Id} updated successfully", id);
        }
        catch (Exception ex)
        {
            if (!await unitOfWork.Events.AnyAsync(e => e.Id == id))
            {
                logger.LogWarning("EventService.UpdateEventAsync - Concurrency failure for EventId {Id}, record not found", id);
                return null;
            }
            logger.LogError(ex, "EventService.UpdateEventAsync - Exception for EventId {Id}", id);
            throw;
        }

        var responseDto = mapper.Map<EventDto>(existing);
        responseDto.RegisteredCount = existing.EventRegistrations.Count(r => r.Status == RegistrationStatus.Registered);

        logger.LogInformation("EventService.UpdateEventAsync completed for EventId {Id}", id);

        return responseDto;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteEventAsync(int id, string userEmail)
    {
        logger.LogInformation("EventService.DeleteEventAsync called for EventId {Id} by {Email}", id, userEmail);

        var evt = await unitOfWork.Events.GetEventWithDetailsAsync(id);

        if (evt == null)
        {
            logger.LogWarning("EventService.DeleteEventAsync - Event {Id} not found", id);
            return false;
        }

        // Check if the current user is the event creator or an esn_member
        var currentUser = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (currentUser == null || (evt.UserId != currentUser.Id && currentUser.StudentType != Bo.Constants.StudentType.EsnMember))
        {
            logger.LogWarning("EventService.DeleteEventAsync - Unauthorized delete attempt for EventId {Id} by {Email}",
                id, userEmail);
            throw new UnauthorizedAccessException($"User {userEmail} is not authorized to delete this event");
        }

        // Hard delete (registrations will be cascade deleted)
        unitOfWork.Events.Delete(evt);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("EventService.DeleteEventAsync completed for EventId {Id} by {Email}",
            id, evt.User.Email);

        return true;
    }

    /// <inheritdoc />
    public async Task<string> RegisterForEventAsync(int eventId, string userEmail, string? surveyJsData)
    {
        logger.LogInformation("EventService.RegisterForEventAsync called for EventId {Id} by {Email}",
            eventId, userEmail);

        await unitOfWork.BeginTransactionAsync();
        try
        {
            var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
            if (user == null)
            {
                logger.LogError("EventService.RegisterForEventAsync failed - user not found for {Email}", userEmail);
                throw new UnauthorizedAccessException($"User not found: {userEmail}");
            }

            var evt = await unitOfWork.Events.GetEventWithDetailsAsync(eventId);

            if (evt == null)
            {
                logger.LogWarning("EventService.RegisterForEventAsync - Event {Id} not found", eventId);
                throw new KeyNotFoundException($"Event not found: {eventId}");
            }

            // Check if registration period is open
            var now = DateTime.UtcNow;
            if (now < evt.StartDate)
            {
                logger.LogInformation("EventService.RegisterForEventAsync - Registration not yet open for EventId {Id}", eventId);
                throw new InvalidOperationException("Registration period has not started yet");
            }

            if (evt.EndDate.HasValue && now > evt.EndDate.Value)
            {
                logger.LogInformation("EventService.RegisterForEventAsync - Registration closed for EventId {Id}", eventId);
                throw new InvalidOperationException("Registration period has ended");
            }

            // Check if already registered
            var existingRegistration = await unitOfWork.Events.GetRegistrationAsync(eventId, user.Id);

            if (existingRegistration != null)
            {
                if (existingRegistration.Status == RegistrationStatus.Registered)
                {
                    logger.LogInformation("EventService.RegisterForEventAsync - User {Email} already registered for EventId {Id}",
                        userEmail, eventId);
                    throw new InvalidOperationException("Already registered for this event");
                }
                else
                {
                    // Reactivate cancelled registration
                    logger.LogInformation("EventService.RegisterForEventAsync - Reactivating cancelled registration for User {Email}, EventId {Id}",
                        userEmail, eventId);
                    existingRegistration.Status = RegistrationStatus.Registered;
                    existingRegistration.RegisteredAt = DateTime.UtcNow;
                    unitOfWork.EventRegistrations.Update(existingRegistration);
                }
            }
            else
            {
                // Check max participants - transaction garantit l'atomicité
                if (evt.MaxParticipants.HasValue)
                {
                    var currentCount = await unitOfWork.Events.GetRegisteredCountAsync(eventId);
                    if (currentCount >= evt.MaxParticipants.Value)
                    {
                        logger.LogInformation("EventService.RegisterForEventAsync - Event {Id} is full", eventId);
                        throw new InvalidOperationException("Event is full");
                    }
                }

                var registration = new EventRegistrationBo
                {
                    UserId = user.Id,
                    EventId = eventId,
                    RegisteredAt = DateTime.UtcNow,
                    SurveyJsData = surveyJsData,
                    Status = RegistrationStatus.Registered
                };

                await unitOfWork.EventRegistrations.AddAsync(registration);
                logger.LogInformation("EventService.RegisterForEventAsync - Creating new registration for User {Email}, EventId {Id}",
                    userEmail, eventId);
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            logger.LogInformation("EventService.RegisterForEventAsync completed for EventId {Id} by {Email}",
                eventId, userEmail);

            return "Successfully registered for event";
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<string> UnregisterFromEventAsync(int eventId, string userEmail)
    {
        logger.LogInformation("EventService.UnregisterFromEventAsync called for EventId {Id} by {Email}",
            eventId, userEmail);

        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("EventService.UnregisterFromEventAsync failed - user not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        var registration = await unitOfWork.Events.GetRegistrationAsync(eventId, user.Id);

        if (registration == null || registration.Status == RegistrationStatus.Cancelled)
        {
            logger.LogWarning("EventService.UnregisterFromEventAsync - No active registration found for User {Email}, EventId {Id}",
                userEmail, eventId);
            throw new KeyNotFoundException("No active registration found");
        }

        registration.Status = RegistrationStatus.Cancelled;
        unitOfWork.EventRegistrations.Update(registration);

        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("EventService.UnregisterFromEventAsync completed for EventId {Id} by {Email}",
            eventId, userEmail);

        return "Successfully unregistered from event";
    }

    /// <inheritdoc />
    public async Task<EventWithRegistrationsDto?> GetEventRegistrationsAsync(int eventId)
    {
        logger.LogInformation("EventService.GetEventRegistrationsAsync called for EventId {Id}", eventId);

        var evt = await unitOfWork.Events.GetEventWithDetailsAsync(eventId);

        if (evt == null)
        {
            logger.LogWarning("EventService.GetEventRegistrationsAsync - Event {Id} not found", eventId);
            return null;
        }

        var result = mapper.Map<EventWithRegistrationsDto>(evt);

        logger.LogInformation("EventService.GetEventRegistrationsAsync completed for EventId {Id}, returning {Count} registrations",
            eventId, result.Registrations.Count);

        return result;
    }
}
