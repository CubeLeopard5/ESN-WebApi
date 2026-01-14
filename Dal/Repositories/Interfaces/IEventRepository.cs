using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface pour le repository Event avec méthodes spécifiques
/// </summary>
public interface IEventRepository : IRepository<EventBo>
{
    /// <summary>
    /// Récupère tous les événements avec leurs inscriptions et utilisateur créateur
    /// </summary>
    Task<IEnumerable<EventBo>> GetAllEventsWithDetailsAsync();

    /// <summary>
    /// Récupère tous les événements pour admin (y compris événements passés)
    /// </summary>
    Task<IEnumerable<EventBo>> GetAllEventsForAdminAsync();

    /// <summary>
    /// Récupère les événements paginés avec projection optimisée (évite N+1)
    /// </summary>
    Task<(List<EventBo> Events, int TotalCount)> GetEventsPagedAsync(int skip, int take);

    /// <summary>
    /// Récupère un événement par ID avec tous ses détails
    /// </summary>
    Task<EventBo?> GetEventWithDetailsAsync(int eventId);

    /// <summary>
    /// Récupère les événements créés par un utilisateur
    /// </summary>
    Task<IEnumerable<EventBo>> GetEventsByUserEmailAsync(string userEmail);

    /// <summary>
    /// Récupère une inscription spécifique
    /// </summary>
    Task<EventRegistrationBo?> GetRegistrationAsync(int eventId, int userId);

    /// <summary>
    /// Compte le nombre d'inscrits pour un événement (status = registered)
    /// </summary>
    Task<int> GetRegisteredCountAsync(int eventId);
}
