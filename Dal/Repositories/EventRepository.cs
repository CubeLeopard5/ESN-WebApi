using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository Event
/// </summary>
public class EventRepository(EsnDevContext context) : Repository<EventBo>(context), IEventRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<EventBo>> GetAllEventsWithDetailsAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .Include(e => e.Calendars)
            .Where(e => e.Calendars.Any(c => c.EventDate >= now))
            .Include(e => e.EventRegistrations)
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .AsSplitQuery()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventBo>> GetAllEventsForAdminAsync()
    {
        return await _dbSet
            .Include(e => e.EventRegistrations)
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<(List<EventBo> Events, int TotalCount)> GetEventsPagedAsync(int skip, int take, EventTimeFilter timeFilter = EventTimeFilter.Future)
    {
        var now = DateTime.UtcNow;

        // Base query: only events with linked calendars
        var query = _dbSet
            .Include(e => e.Calendars)
            .Where(e => e.Calendars.Any());

        // Apply time filter based on Calendar.EventDate
        query = timeFilter switch
        {
            EventTimeFilter.Future => query.Where(e => e.Calendars.Any(c => c.EventDate >= now)),
            EventTimeFilter.Past => query.Where(e => e.Calendars.All(c => c.EventDate < now)),
            EventTimeFilter.All => query,
            _ => query.Where(e => e.Calendars.Any(c => c.EventDate >= now))
        };

        // Total count with filter
        var totalCount = await query.CountAsync();

        // Order by calendar event date for better UX
        var events = await query
            .Include(e => e.User)
            .Include(e => e.EventRegistrations)
            .OrderBy(e => e.Calendars.Min(c => c.EventDate))
            .Skip(skip)
            .Take(take)
            .AsSplitQuery()
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<EventBo?> GetEventWithDetailsAsync(int eventId)
    {
        return await _dbSet
            .Include(e => e.EventRegistrations)
                .ThenInclude(er => er.User)
            .Include(e => e.User)
            .AsSplitQuery()
            .FirstOrDefaultAsync(e => e.Id == eventId);
    }

    public async Task<IEnumerable<EventBo>> GetEventsByUserEmailAsync(string userEmail)
    {
        return await _dbSet
            .Include(e => e.User)
            .Where(e => e.User.Email == userEmail)
            .ToListAsync();
    }

    public async Task<EventRegistrationBo?> GetRegistrationAsync(int eventId, int userId)
    {
        return await _context.EventRegistrations
            .FirstOrDefaultAsync(er => er.EventId == eventId && er.UserId == userId);
    }

    public async Task<int> GetRegisteredCountAsync(int eventId)
    {
        return await _context.EventRegistrations
            .CountAsync(er => er.EventId == eventId && er.Status == RegistrationStatus.Registered);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventBo>> GetEventsCreatedAfterAsync(DateTime date)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(e => e.CreatedAt >= date)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<List<EventBo>> GetEventsWithRegistrationCountAsync(int count)
    {
        return await _dbSet
            .AsNoTracking()
            .Include(e => e.EventRegistrations.Where(er => er.Status == RegistrationStatus.Registered))
            .OrderByDescending(e => e.EventRegistrations.Count(er => er.Status == RegistrationStatus.Registered))
            .Take(count)
            .ToListAsync();
    }
}
