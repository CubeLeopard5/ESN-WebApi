using Dto.Calendar;
using Dto.Common;

namespace Business.Interfaces;

/// <summary>
/// Interface de gestion des calendriers et organisation d'événements
/// </summary>
public interface ICalendarService
{
    /// <summary>
    /// Récupère tous les calendriers sans pagination
    /// </summary>
    /// <returns>Collection de tous les calendriers</returns>
    /// <remarks>
    /// OBSOLÈTE : Cette méthode peut causer des problèmes de performance et de mémoire.
    /// Utilisez GetAllCalendarsAsync(PaginationParams) à la place.
    /// </remarks>
    Task<IEnumerable<CalendarDto>> GetAllCalendarsAsync();

    /// <summary>
    /// Récupère tous les calendriers avec pagination
    /// </summary>
    /// <param name="pagination">Paramètres de pagination (numéro de page, taille de page)</param>
    /// <returns>Résultat paginé contenant les calendriers et métadonnées de pagination</returns>
    /// <remarks>
    /// Méthode recommandée pour récupérer les calendriers.
    /// Pagination par défaut : 10 éléments par page
    /// Pagination maximale : 100 éléments par page
    /// </remarks>
    Task<PagedResult<CalendarDto>> GetAllCalendarsAsync(PaginationParams pagination);

    /// <summary>
    /// Récupère un calendrier par son identifiant
    /// </summary>
    /// <param name="id">Identifiant unique du calendrier</param>
    /// <returns>Calendrier complet avec tous les organisateurs ou null si non trouvé</returns>
    /// <remarks>
    /// Inclut : organisateur principal, sous-organisateurs, event manager, responsable communication
    /// </remarks>
    Task<CalendarDto?> GetCalendarByIdAsync(int id);

    /// <summary>
    /// Récupère tous les calendriers associés à un événement
    /// </summary>
    /// <param name="eventId">Identifiant de l'événement</param>
    /// <returns>Collection des calendriers pour cet événement</returns>
    /// <remarks>
    /// Permet de voir toutes les planifications d'un même événement.
    /// </remarks>
    Task<IEnumerable<CalendarDto>> GetCalendarsByEventIdAsync(int eventId);

    /// <summary>
    /// Récupère les calendriers disponibles pour liaison (sans événement)
    /// </summary>
    /// <returns>Collection des calendriers sans EventId</returns>
    /// <remarks>
    /// Utilisé lors de la création d'un événement pour sélectionner un calendrier à lier.
    /// Retourne uniquement les calendriers qui n'ont pas encore d'événement associé.
    /// </remarks>
    Task<IEnumerable<CalendarDto>> GetAvailableCalendarsAsync();

    /// <summary>
    /// Crée un nouveau calendrier avec assignation des responsables
    /// </summary>
    /// <param name="createDto">Données de création incluant événement et organisateurs</param>
    /// <returns>Calendrier créé</returns>
    /// <remarks>
    /// Processus de création :
    /// - Association avec un événement existant
    /// - Désignation d'un organisateur principal (obligatoire)
    /// - Ajout de sous-organisateurs multiples (optionnel)
    /// - Désignation d'un Event Manager (optionnel)
    /// - Désignation d'un Responsable Communication (optionnel)
    /// Transaction garantissant la cohérence des assignations
    /// </remarks>
    Task<CalendarDto> CreateCalendarAsync(CalendarCreateDto createDto);

    /// <summary>
    /// Met à jour un calendrier existant
    /// </summary>
    /// <param name="id">Identifiant du calendrier à modifier</param>
    /// <param name="updateDto">Nouvelles données du calendrier</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la modification</param>
    /// <returns>Calendrier mis à jour ou null si non trouvé</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est pas l'organisateur principal</exception>
    /// <remarks>
    /// Seul l'organisateur principal peut modifier le calendrier.
    /// Transaction garantissant la mise à jour atomique des sous-organisateurs.
    /// </remarks>
    Task<CalendarDto?> UpdateCalendarAsync(int id, CalendarUpdateDto updateDto, string userEmail);

    /// <summary>
    /// Supprime définitivement un calendrier
    /// </summary>
    /// <param name="id">Identifiant du calendrier à supprimer</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la suppression</param>
    /// <returns>Calendrier supprimé ou null si non trouvé</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est pas l'organisateur principal</exception>
    /// <remarks>
    /// Seul l'organisateur principal peut supprimer le calendrier.
    /// Supprime également toutes les relations avec les sous-organisateurs.
    /// </remarks>
    Task<CalendarDto?> DeleteCalendarAsync(int id, string userEmail);
}
