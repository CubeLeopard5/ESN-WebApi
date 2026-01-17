using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository pour les feedbacks d'événements
/// </summary>
public class EventFeedbackRepository(EsnDevContext context) : Repository<EventFeedbackBo>(context), IEventFeedbackRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<EventFeedbackBo>> GetByEventIdAsync(int eventId)
    {
        return await context.EventFeedbacks
            .AsNoTracking()
            .Where(f => f.EventId == eventId)
            .Include(f => f.User)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<EventFeedbackBo?> GetByEventAndUserAsync(int eventId, int userId)
    {
        return await context.EventFeedbacks
            .AsNoTracking()
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.EventId == eventId && f.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<bool> HasUserSubmittedFeedbackAsync(int eventId, int userId)
    {
        return await context.EventFeedbacks
            .AnyAsync(f => f.EventId == eventId && f.UserId == userId);
    }

    /// <inheritdoc />
    public async Task<int> CountByEventIdAsync(int eventId)
    {
        return await context.EventFeedbacks
            .CountAsync(f => f.EventId == eventId);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventFeedbackBo>> GetByUserIdAsync(int userId)
    {
        return await context.EventFeedbacks
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .Include(f => f.Event)
            .OrderByDescending(f => f.SubmittedAt)
            .ToListAsync();
    }
}
