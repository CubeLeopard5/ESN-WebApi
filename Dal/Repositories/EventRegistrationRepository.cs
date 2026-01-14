using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository pour les inscriptions aux événements
/// </summary>
public class EventRegistrationRepository(EsnDevContext context) : Repository<EventRegistrationBo>(context), IEventRegistrationRepository
{
    public async Task<IEnumerable<EventRegistrationBo>> GetByEventIdAsync(int eventId)
    {
        return await context.EventRegistrations
            .Where(er => er.EventId == eventId)
            .Include(er => er.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<EventRegistrationBo>> GetByUserIdAsync(int userId)
    {
        return await context.EventRegistrations
            .Where(er => er.UserId == userId)
            .Include(er => er.Event)
            .ToListAsync();
    }

    public async Task<bool> IsUserRegisteredAsync(int eventId, int userId)
    {
        return await context.EventRegistrations
            .AnyAsync(er => er.EventId == eventId && er.UserId == userId);
    }

    public async Task<EventRegistrationBo?> GetByEventAndUserAsync(int eventId, int userId)
    {
        return await context.EventRegistrations
            .FirstOrDefaultAsync(er => er.EventId == eventId && er.UserId == userId);
    }

    public async Task<IEnumerable<EventRegistrationBo>> GetByUserAndEventsAsync(int userId, int[] eventIds)
    {
        return await context.EventRegistrations
            .Where(er => er.UserId == userId && eventIds.Contains(er.EventId))
            .ToListAsync();
    }
}
