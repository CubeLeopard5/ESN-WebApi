using Bo.Constants;
using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository Event
/// </summary>
public class EventRepository(EsnDevContext context) : Repository<EventBo>(context), IEventRepository
{
    public async Task<IEnumerable<EventBo>> GetAllEventsWithDetailsAsync()
    {
        var now = DateTime.UtcNow;

        return await _dbSet
            .Include(e => e.Calendars)
            .Where(e => !e.Calendars.Any() || e.Calendars.Any(c => c.EventDate >= now))
            .Include(e => e.EventRegistrations)
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventBo>> GetAllEventsForAdminAsync()
    {
        return await _dbSet
            .Include(e => e.EventRegistrations)
            .Include(e => e.User)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<EventBo> Events, int TotalCount)> GetEventsPagedAsync(int skip, int take)
    {
        var now = DateTime.UtcNow;

        // Filtrer les événements passés (basé sur la date du calendrier lié)
        var query = _dbSet
            .Include(e => e.Calendars)
            .Where(e => !e.Calendars.Any() || e.Calendars.Any(c => c.EventDate >= now));

        // Compte total avec filtre
        var totalCount = await query.CountAsync();

        // Charge uniquement la page demandée avec projection pour éviter N+1
        var events = await query
            .Include(e => e.User)
            .Include(e => e.EventRegistrations) // Nécessaire pour le mapping
            .OrderByDescending(e => e.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (events, totalCount);
    }

    public async Task<EventBo?> GetEventWithDetailsAsync(int eventId)
    {
        return await _dbSet
            .Include(e => e.EventRegistrations)
            .Include(e => e.User)
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
}
