using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository pour les co-organisateurs de calendriers
/// </summary>
public class CalendarSubOrganizerRepository(EsnDevContext context) : Repository<CalendarSubOrganizerBo>(context), ICalendarSubOrganizerRepository
{
    public async Task<IEnumerable<CalendarSubOrganizerBo>> GetByCalendarIdAsync(int calendarId)
    {
        return await context.CalendarSubOrganizers
            .Where(cso => cso.CalendarId == calendarId)
            .Include(cso => cso.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<CalendarSubOrganizerBo>> GetByUserIdAsync(int userId)
    {
        return await context.CalendarSubOrganizers
            .Where(cso => cso.UserId == userId)
            .Include(cso => cso.Calendar)
            .ToListAsync();
    }

    public async Task<bool> IsSubOrganizerAsync(int calendarId, int userId)
    {
        return await context.CalendarSubOrganizers
            .AnyAsync(cso => cso.CalendarId == calendarId && cso.UserId == userId);
    }

    public async Task DeleteByCalendarIdAsync(int calendarId)
    {
        var subOrganizers = await context.CalendarSubOrganizers
            .Where(cso => cso.CalendarId == calendarId)
            .ToListAsync();

        context.CalendarSubOrganizers.RemoveRange(subOrganizers);
    }
}
