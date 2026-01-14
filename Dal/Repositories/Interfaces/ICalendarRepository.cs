using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface pour le repository Calendar avec méthodes spécifiques
/// </summary>
public interface ICalendarRepository : IRepository<CalendarBo>
{
    /// <summary>
    /// Récupère tous les calendriers avec tous les détails (use with caution)
    /// </summary>
    Task<IEnumerable<CalendarBo>> GetAllCalendarsWithDetailsAsync();

    /// <summary>
    /// Récupère un calendrier par ID avec tous ses détails
    /// </summary>
    Task<CalendarBo?> GetCalendarWithDetailsAsync(int calendarId);

    /// <summary>
    /// Récupère un calendrier par ID (sans includes - optimisé)
    /// </summary>
    Task<CalendarBo?> GetCalendarSimpleAsync(int calendarId);

    /// <summary>
    /// Récupère les calendriers filtrés par événement
    /// </summary>
    Task<IEnumerable<CalendarBo>> GetCalendarsByEventIdAsync(int? eventId);

    /// <summary>
    /// Récupère le calendrier lié à un event spécifique (retourne le premier ou null)
    /// </summary>
    Task<CalendarBo?> GetCalendarByEventIdAsync(int eventId);

    /// <summary>
    /// Récupère les calendriers sans événement lié (EventId = NULL)
    /// </summary>
    Task<IEnumerable<CalendarBo>> GetCalendarsWithoutEventAsync();

    /// <summary>
    /// Récupère les calendriers paginés avec détails limités
    /// </summary>
    Task<(List<CalendarBo> Items, int TotalCount)> GetPagedAsync(int skip, int take);

    /// <summary>
    /// Récupère les calendriers paginés sans détails (optimisé)
    /// </summary>
    Task<(List<CalendarBo> Items, int TotalCount)> GetPagedSimpleAsync(int skip, int take);
}
