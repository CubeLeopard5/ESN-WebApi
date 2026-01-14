using AutoMapper;
using Bo.Models;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Calendar;
using Dto.Common;
using Microsoft.Extensions.Logging;

namespace Business.Calendar;

/// <summary>
/// Interface de gestion des calendriers et organisation d'événements
/// </summary>
public class CalendarService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<CalendarService> logger)
    : ICalendarService
{
    /// <inheritdoc />
    [Obsolete("Use GetAllCalendarsAsync(PaginationParams pagination) instead for better performance and memory management")]
    public async Task<IEnumerable<CalendarDto>> GetAllCalendarsAsync()
    {
        logger.LogInformation("CalendarService.GetAllCalendarsAsync called (non-paginated - deprecated)");

        var calendars = await unitOfWork.Calendars.GetAllCalendarsWithDetailsAsync();

        logger.LogInformation("CalendarService.GetAllCalendarsAsync completed, returning {Count} calendars", calendars.Count());

        return mapper.Map<IEnumerable<CalendarDto>>(calendars);
    }

    /// <inheritdoc />
    public async Task<PagedResult<CalendarDto>> GetAllCalendarsAsync(PaginationParams pagination)
    {
        logger.LogInformation("CalendarService.GetAllCalendarsAsync (paginated) called - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var (items, totalCount) = await unitOfWork.Calendars.GetPagedAsync(
            pagination.Skip,
            pagination.PageSize);

        var dtos = mapper.Map<List<CalendarDto>>(items);

        logger.LogInformation("CalendarService.GetAllCalendarsAsync (paginated) completed - Returned {Count} of {TotalCount}",
            dtos.Count, totalCount);

        return new PagedResult<CalendarDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <inheritdoc />
    public async Task<CalendarDto?> GetCalendarByIdAsync(int id)
    {
        logger.LogInformation("CalendarService.GetCalendarByIdAsync called for CalendarId {Id}", id);

        var calendar = await unitOfWork.Calendars.GetCalendarWithDetailsAsync(id);

        if (calendar == null)
        {
            logger.LogWarning("CalendarService.GetCalendarByIdAsync - Calendar {Id} not found", id);
            return null;
        }

        logger.LogInformation("CalendarService.GetCalendarByIdAsync completed for CalendarId {Id}", id);

        return mapper.Map<CalendarDto>(calendar);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarDto>> GetCalendarsByEventIdAsync(int eventId)
    {
        logger.LogInformation("CalendarService.GetCalendarsByEventIdAsync called for EventId {EventId}", eventId);

        var calendars = await unitOfWork.Calendars.GetCalendarsByEventIdAsync(eventId);

        logger.LogInformation("CalendarService.GetCalendarsByEventIdAsync completed for EventId {EventId}, returning {Count} calendars",
            eventId, calendars.Count());

        return mapper.Map<IEnumerable<CalendarDto>>(calendars);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<CalendarDto>> GetAvailableCalendarsAsync()
    {
        logger.LogInformation("CalendarService.GetAvailableCalendarsAsync called");

        var calendars = await unitOfWork.Calendars.GetCalendarsWithoutEventAsync();

        logger.LogInformation("CalendarService.GetAvailableCalendarsAsync completed, returning {Count} calendars",
            calendars.Count());

        return mapper.Map<IEnumerable<CalendarDto>>(calendars);
    }

    /// <inheritdoc />
    public async Task<CalendarDto> CreateCalendarAsync(CalendarCreateDto createDto)
    {
        logger.LogInformation("CalendarService.CreateCalendarAsync called with Title {Title}", createDto.Title);

        try
        {
            await unitOfWork.BeginTransactionAsync();

            var calendar = mapper.Map<Bo.Models.CalendarBo>(createDto);

            await unitOfWork.Calendars.AddAsync(calendar);
            await unitOfWork.SaveChangesAsync();

            logger.LogInformation("CalendarService.CreateCalendarAsync - Calendar created with Id {Id}", calendar.Id);

            // Add sub-organizers if provided
            if (createDto.SubOrganizerIds != null && createDto.SubOrganizerIds.Any())
            {
                logger.LogInformation("CalendarService.CreateCalendarAsync - Adding {Count} sub-organizers",
                    createDto.SubOrganizerIds.Count);

                foreach (var userId in createDto.SubOrganizerIds)
                {
                    await unitOfWork.CalendarSubOrganizers.AddAsync(new CalendarSubOrganizerBo
                    {
                        CalendarId = calendar.Id,
                        UserId = userId
                    });
                }
                await unitOfWork.SaveChangesAsync();
            }

            await unitOfWork.CommitTransactionAsync();

            // Reload with includes to return complete data
            calendar = await unitOfWork.Calendars.GetCalendarWithDetailsAsync(calendar.Id);

            logger.LogInformation("CalendarService.CreateCalendarAsync completed for CalendarId {Id}", calendar!.Id);

            return mapper.Map<CalendarDto>(calendar);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CalendarService.CreateCalendarAsync - Error creating calendar, rolling back transaction");
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<CalendarDto?> UpdateCalendarAsync(int id, CalendarUpdateDto updateDto, string userEmail)
    {
        logger.LogInformation("CalendarService.UpdateCalendarAsync called for CalendarId {Id} by {Email}", id, userEmail);

        var calendar = await unitOfWork.Calendars.GetByIdAsync(id);

        if (calendar == null)
        {
            logger.LogWarning("CalendarService.UpdateCalendarAsync - Calendar {Id} not found", id);
            return null;
        }

        // Get the user to verify ownership
        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("CalendarService.UpdateCalendarAsync - User not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        // Verify ownership (MainOrganizer)
        if (calendar.MainOrganizerId != user.Id)
        {
            logger.LogWarning("CalendarService.UpdateCalendarAsync - User {Email} (ID: {UserId}) tried to update Calendar {Id} owned by UserId {OwnerId}",
                userEmail, user.Id, id, calendar.MainOrganizerId);
            throw new UnauthorizedAccessException("You don't have permission to update this calendar");
        }

        try
        {
            await unitOfWork.BeginTransactionAsync();

            mapper.Map(updateDto, calendar);

            // Update sub-organizers if provided
            if (updateDto.SubOrganizerIds != null)
            {
                logger.LogInformation("CalendarService.UpdateCalendarAsync - Updating sub-organizers for CalendarId {Id}", id);

                // Remove existing sub-organizers
                var existingSubOrganizers = await unitOfWork.CalendarSubOrganizers.FindAsync(cso => cso.CalendarId == id);
                foreach (var subOrganizer in existingSubOrganizers)
                {
                    unitOfWork.CalendarSubOrganizers.Delete(subOrganizer);
                }

                // Add new sub-organizers
                foreach (var userId in updateDto.SubOrganizerIds)
                {
                    await unitOfWork.CalendarSubOrganizers.AddAsync(new CalendarSubOrganizerBo
                    {
                        CalendarId = calendar.Id,
                        UserId = userId
                    });
                }
            }

            unitOfWork.Calendars.Update(calendar);
            await unitOfWork.SaveChangesAsync();

            await unitOfWork.CommitTransactionAsync();

            logger.LogInformation("CalendarService.UpdateCalendarAsync - Calendar {Id} updated successfully", id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "CalendarService.UpdateCalendarAsync - Error updating calendar, rolling back transaction");
            await unitOfWork.RollbackTransactionAsync();

            if (!await unitOfWork.Calendars.AnyAsync(e => e.Id == id))
            {
                logger.LogWarning("CalendarService.UpdateCalendarAsync - Concurrency failure for CalendarId {Id}, record not found", id);
                return null;
            }
            throw;
        }

        // Reload with includes
        calendar = await unitOfWork.Calendars.GetCalendarWithDetailsAsync(id);

        logger.LogInformation("CalendarService.UpdateCalendarAsync completed for CalendarId {Id}", id);

        return mapper.Map<CalendarDto>(calendar);
    }

    /// <inheritdoc />
    public async Task<CalendarDto?> DeleteCalendarAsync(int id, string userEmail)
    {
        logger.LogInformation("CalendarService.DeleteCalendarAsync called for CalendarId {Id} by {Email}", id, userEmail);

        var calendar = await unitOfWork.Calendars.GetCalendarWithDetailsAsync(id);

        if (calendar == null)
        {
            logger.LogWarning("CalendarService.DeleteCalendarAsync - Calendar {Id} not found", id);
            return null;
        }

        // Get the user to verify ownership
        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogError("CalendarService.DeleteCalendarAsync - User not found for {Email}", userEmail);
            throw new UnauthorizedAccessException($"User not found: {userEmail}");
        }

        // Verify ownership (MainOrganizer)
        if (calendar.MainOrganizerId != user.Id)
        {
            logger.LogWarning("CalendarService.DeleteCalendarAsync - User {Email} (ID: {UserId}) tried to delete Calendar {Id} owned by UserId {OwnerId}",
                userEmail, user.Id, id, calendar.MainOrganizerId);
            throw new UnauthorizedAccessException("You don't have permission to delete this calendar");
        }

        unitOfWork.Calendars.Delete(calendar);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("CalendarService.DeleteCalendarAsync completed for CalendarId {Id}", id);

        return mapper.Map<CalendarDto>(calendar);
    }

}
