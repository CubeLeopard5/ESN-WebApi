using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Repository spécialisé pour les co-organisateurs de calendriers
/// </summary>
public interface ICalendarSubOrganizerRepository : IRepository<CalendarSubOrganizerBo>
{
    /// <summary>
    /// Récupère tous les co-organisateurs d'un calendrier
    /// </summary>
    Task<IEnumerable<CalendarSubOrganizerBo>> GetByCalendarIdAsync(int calendarId);

    /// <summary>
    /// Récupère tous les calendriers dont un utilisateur est co-organisateur
    /// </summary>
    Task<IEnumerable<CalendarSubOrganizerBo>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Vérifie si un utilisateur est co-organisateur d'un calendrier
    /// </summary>
    Task<bool> IsSubOrganizerAsync(int calendarId, int userId);

    /// <summary>
    /// Supprime tous les co-organisateurs d'un calendrier
    /// </summary>
    Task DeleteByCalendarIdAsync(int calendarId);
}
