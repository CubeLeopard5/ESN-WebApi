using Bo.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository Calendar
/// </summary>
public class CalendarRepository(EsnDevContext context)
    : Repository<CalendarBo>(context), Interfaces.ICalendarRepository
{
    public async Task<IEnumerable<CalendarBo>> GetAllCalendarsWithDetailsAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Event)
                .ThenInclude(e => e.User)
            .Include(c => c.MainOrganizer)
            .Include(c => c.EventManager)
            .Include(c => c.ResponsableCom)
            .Include(c => c.CalendarSubOrganizers)
                .ThenInclude(cso => cso.User)
            .AsSplitQuery()
            .ToListAsync();
    }

    public async Task<CalendarBo?> GetCalendarWithDetailsAsync(int calendarId)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(c => c.Event)
                .ThenInclude(e => e.User)
            .Include(c => c.MainOrganizer)
            .Include(c => c.EventManager)
            .Include(c => c.ResponsableCom)
            .Include(c => c.CalendarSubOrganizers)
                .ThenInclude(cso => cso.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(c => c.Id == calendarId);
    }

    public async Task<IEnumerable<CalendarBo>> GetCalendarsByEventIdAsync(int? eventId)
    {
        var query = _dbSet
            .AsNoTracking()
            .Include(c => c.Event)
                .ThenInclude(e => e.User)
            .Include(c => c.MainOrganizer)
            .Include(c => c.EventManager)
            .Include(c => c.ResponsableCom)
            .Include(c => c.CalendarSubOrganizers)
                .ThenInclude(cso => cso.User)
            .AsSplitQuery()
            .AsQueryable();

        if (eventId.HasValue)
        {
            query = query.Where(c => c.EventId == eventId.Value);
        }

        return await query.ToListAsync();
    }

    public async Task<CalendarBo?> GetCalendarByEventIdAsync(int eventId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.EventId == eventId);
    }

    public async Task<IEnumerable<CalendarBo>> GetCalendarsWithoutEventAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(c => c.EventId == null)
            .Include(c => c.MainOrganizer)
            .Include(c => c.EventManager)
            .Include(c => c.ResponsableCom)
            .OrderByDescending(c => c.EventDate)
            .ToListAsync();
    }

    public async Task<(List<CalendarBo> Items, int TotalCount)> GetPagedAsync(int skip, int take)
    {
        var totalCount = await _dbSet.CountAsync();

        var items = await _dbSet
            .AsNoTracking()
            .Include(c => c.Event)
                .ThenInclude(e => e.User)
            .Include(c => c.MainOrganizer)
            .Include(c => c.EventManager)
            .Include(c => c.ResponsableCom)
            .Include(c => c.CalendarSubOrganizers)
                .ThenInclude(cso => cso.User)
            .OrderByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .AsSplitQuery()
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<CalendarBo?> GetCalendarSimpleAsync(int calendarId)
    {
        return await _dbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == calendarId);
    }

    public async Task<(List<CalendarBo> Items, int TotalCount)> GetPagedSimpleAsync(int skip, int take)
    {
        var totalCount = await _dbSet.CountAsync();
        var items = await _dbSet
            .AsNoTracking()
            .OrderByDescending(c => c.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
        return (items, totalCount);
    }
}
