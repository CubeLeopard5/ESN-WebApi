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
}
