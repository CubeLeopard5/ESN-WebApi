using Bo.Enums;
using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Repository spécialisé pour les inscriptions aux événements
/// </summary>
public interface IEventRegistrationRepository : IRepository<EventRegistrationBo>
{
    /// <summary>
    /// Récupère toutes les inscriptions pour un événement donné
    /// </summary>
    Task<IEnumerable<EventRegistrationBo>> GetByEventIdAsync(int eventId);

    /// <summary>
    /// Récupère toutes les inscriptions d'un utilisateur
    /// </summary>
    Task<IEnumerable<EventRegistrationBo>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Vérifie si un utilisateur est déjà inscrit à un événement
    /// </summary>
    Task<bool> IsUserRegisteredAsync(int eventId, int userId);

    /// <summary>
    /// Récupère l'inscription d'un utilisateur pour un événement spécifique
    /// </summary>
    Task<EventRegistrationBo?> GetByEventAndUserAsync(int eventId, int userId);

    /// <summary>
    /// Récupère toutes les inscriptions d'un utilisateur pour une liste d'événements (optimisation N+1)
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <param name="eventIds">Liste des IDs d'événements</param>
    /// <returns>Liste des inscriptions</returns>
    Task<IEnumerable<EventRegistrationBo>> GetByUserAndEventsAsync(int userId, int[] eventIds);

    /// <summary>
    /// Récupère toutes les inscriptions d'un événement avec les détails de présence
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Inscriptions avec User et AttendanceValidatedBy</returns>
    Task<IEnumerable<EventRegistrationBo>> GetByEventIdWithAttendanceAsync(int eventId);

    /// <summary>
    /// Récupère une inscription par son ID avec tous les détails
    /// </summary>
    /// <param name="registrationId">ID de l'inscription</param>
    /// <returns>Inscription avec navigation properties</returns>
    Task<EventRegistrationBo?> GetByIdWithDetailsAsync(int registrationId);

    /// <summary>
    /// Récupère les statistiques de présence pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Dictionnaire status -> count</returns>
    Task<Dictionary<AttendanceStatus?, int>> GetAttendanceStatsAsync(int eventId);

    /// <summary>
    /// Récupère plusieurs inscriptions par leurs IDs en une seule requête (optimisation N+1)
    /// </summary>
    /// <param name="registrationIds">Liste des IDs d'inscriptions</param>
    /// <returns>Dictionnaire id -> inscription</returns>
    Task<Dictionary<int, EventRegistrationBo>> GetByIdsAsync(IEnumerable<int> registrationIds);

    /// <summary>
    /// Récupère toutes les inscriptions avec leur statut de présence
    /// </summary>
    /// <returns>Liste des inscriptions avec AttendanceStatus</returns>
    Task<IEnumerable<EventRegistrationBo>> GetAllWithAttendanceAsync();

    /// <summary>
    /// Récupère les inscriptions créées après une date donnée
    /// </summary>
    /// <param name="date">Date de début</param>
    /// <returns>Liste des inscriptions créées après la date</returns>
    Task<IEnumerable<EventRegistrationBo>> GetRegistrationsAfterAsync(DateTime date);

    /// <summary>
    /// Calcule le taux de présence moyen sur toutes les inscriptions validées
    /// </summary>
    /// <returns>Taux de présence moyen en pourcentage</returns>
    Task<decimal> GetAverageAttendanceRateAsync();
}
