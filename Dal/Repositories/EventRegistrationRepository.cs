using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository pour les inscriptions aux événements
/// </summary>
public class EventRegistrationRepository(EsnDevContext context) : Repository<EventRegistrationBo>(context), IEventRegistrationRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<EventRegistrationBo>> GetByEventIdAsync(int eventId)
    {
        return await context.EventRegistrations
            .AsNoTracking()
            .Where(er => er.EventId == eventId)
            .Include(er => er.User)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventRegistrationBo>> GetByUserIdAsync(int userId)
    {
        return await context.EventRegistrations
            .AsNoTracking()
            .Where(er => er.UserId == userId)
            .Include(er => er.Event)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<bool> IsUserRegisteredAsync(int eventId, int userId)
    {
        return await context.EventRegistrations
            .AnyAsync(er => er.EventId == eventId && er.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<EventRegistrationBo?> GetByEventAndUserAsync(int eventId, int userId)
    {
        return await context.EventRegistrations
            .AsNoTracking()
            .FirstOrDefaultAsync(er => er.EventId == eventId && er.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventRegistrationBo>> GetByUserAndEventsAsync(int userId, int[] eventIds)
    {
        return await context.EventRegistrations
            .AsNoTracking()
            .Where(er => er.UserId == userId && eventIds.Contains(er.EventId))
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventRegistrationBo>> GetByEventIdWithAttendanceAsync(int eventId)
    {
        return await context.EventRegistrations
            .AsNoTracking()
            .Where(er => er.EventId == eventId && er.Status == RegistrationStatus.Registered)
            .Include(er => er.User)
            .Include(er => er.AttendanceValidatedBy)
            .OrderBy(er => er.User.LastName)
            .ThenBy(er => er.User.FirstName)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EventRegistrationBo?> GetByIdWithDetailsAsync(int registrationId)
    {
        return await context.EventRegistrations
            .Include(er => er.User)
            .Include(er => er.Event)
            .Include(er => er.AttendanceValidatedBy)
            .FirstOrDefaultAsync(er => er.Id == registrationId);
    }

    /// <inheritdoc />
    public async Task<Dictionary<AttendanceStatus?, int>> GetAttendanceStatsAsync(int eventId)
    {
        return await context.EventRegistrations
            .AsNoTracking()
            .Where(er => er.EventId == eventId && er.Status == RegistrationStatus.Registered)
            .GroupBy(er => er.AttendanceStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count);
    }

    /// <inheritdoc />
    public async Task<Dictionary<int, EventRegistrationBo>> GetByIdsAsync(IEnumerable<int> registrationIds)
    {
        var idList = registrationIds.ToList();
        return await context.EventRegistrations
            .Where(er => idList.Contains(er.Id))
            .ToDictionaryAsync(er => er.Id, er => er);
    }
}
