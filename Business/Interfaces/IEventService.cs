using Bo.Enums;
using Dto.Common;
using Dto.Event;

namespace Business.Interfaces;

/// <summary>
/// Interface de gestion des événements et inscriptions
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Récupère tous les événements sans pagination
    /// </summary>
    /// <returns>Collection de tous les événements</returns>
    /// <remarks>
    /// OBSOLÈTE : Cette méthode peut causer des problèmes de performance et de mémoire.
    /// Utilisez GetAllEventsAsync(PaginationParams) à la place.
    /// </remarks>
    Task<IEnumerable<EventDto>> GetAllEventsAsync();

    /// <summary>
    /// Récupère tous les événements pour l'administration (y compris événements passés)
    /// </summary>
    /// <returns>Collection de tous les événements</returns>
    /// <remarks>
    /// Méthode réservée aux administrateurs.
    /// Retourne TOUS les événements sans filtre de date.
    /// </remarks>
    Task<IEnumerable<EventDto>> GetAllEventsForAdminAsync();

    /// <summary>
    /// Récupère tous les événements avec pagination et filtre temporel
    /// </summary>
    /// <param name="pagination">Paramètres de pagination (numéro de page, taille de page)</param>
    /// <param name="userEmail">Email de l'utilisateur authentifié (optionnel, pour calculer isCurrentUserRegistered)</param>
    /// <param name="timeFilter">Filtre temporel: Future (défaut), Past, ou All</param>
    /// <returns>Résultat paginé contenant les événements et métadonnées de pagination</returns>
    /// <remarks>
    /// Méthode recommandée pour récupérer les événements.
    /// Seuls les événements liés à un calendrier sont retournés.
    /// Pagination par défaut : 10 éléments par page
    /// Pagination maximale : 100 éléments par page
    /// Si userEmail fourni, chaque EventDto aura IsCurrentUserRegistered renseigné
    /// </remarks>
    Task<PagedResult<EventDto>> GetAllEventsAsync(PaginationParams pagination, string? userEmail = null, EventTimeFilter timeFilter = EventTimeFilter.Future);

    /// <summary>
    /// Récupère un événement par son identifiant
    /// </summary>
    /// <param name="id">Identifiant unique de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur authentifié (optionnel, pour calculer isCurrentUserRegistered)</param>
    /// <returns>Événement complet ou null si non trouvé</returns>
    /// <remarks>
    /// Inclut toutes les informations : titre, description, lieu, dates, capacité, formulaire SurveyJS
    /// Si userEmail fourni, EventDto aura IsCurrentUserRegistered renseigné
    /// </remarks>
    Task<EventDto?> GetEventByIdAsync(int id, string? userEmail = null);

    /// <summary>
    /// Crée un nouvel événement
    /// </summary>
    /// <param name="createEventDto">Données de création de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur créateur</param>
    /// <returns>Événement créé</returns>
    /// <remarks>
    /// L'utilisateur créateur devient automatiquement le propriétaire de l'événement.
    /// Champs optionnels : EventfrogLink, MaxParticipants, SurveyJsData
    /// </remarks>
    Task<EventDto> CreateEventAsync(CreateEventDto createEventDto, string userEmail);

    /// <summary>
    /// Met à jour un événement existant
    /// </summary>
    /// <param name="id">Identifiant de l'événement à modifier</param>
    /// <param name="eventDto">Nouvelles données de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la modification</param>
    /// <returns>Événement mis à jour ou null si non trouvé</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est pas le créateur de l'événement</exception>
    /// <remarks>
    /// Seul le créateur de l'événement peut le modifier.
    /// </remarks>
    Task<EventDto?> UpdateEventAsync(int id, EventDto eventDto, string userEmail);

    /// <summary>
    /// Supprime définitivement un événement
    /// </summary>
    /// <param name="id">Identifiant de l'événement à supprimer</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la suppression</param>
    /// <returns>True si supprimé, false si non trouvé</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est pas le créateur de l'événement</exception>
    /// <remarks>
    /// Seul le créateur de l'événement peut le supprimer.
    /// Supprime également toutes les inscriptions associées.
    /// </remarks>
    Task<bool> DeleteEventAsync(int id, string userEmail);

    /// <summary>
    /// Inscrit un utilisateur à un événement
    /// </summary>
    /// <param name="eventId">Identifiant de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur s'inscrivant</param>
    /// <param name="surveyJsData">Réponses au formulaire SurveyJS (optionnel)</param>
    /// <returns>Message de confirmation</returns>
    /// <exception cref="KeyNotFoundException">Événement ou utilisateur non trouvé</exception>
    /// <exception cref="InvalidOperationException">Événement complet ou utilisateur déjà inscrit</exception>
    /// <remarks>
    /// Processus d'inscription :
    /// - Vérification de la disponibilité (capacité maximale)
    /// - Vérification de la non-duplication
    /// - Réactivation si inscription annulée précédemment
    /// - Transaction garantissant l'atomicité
    /// </remarks>
    Task<string> RegisterForEventAsync(int eventId, string userEmail, string? surveyJsData);

    /// <summary>
    /// Désinscrit un utilisateur d'un événement
    /// </summary>
    /// <param name="eventId">Identifiant de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur se désinscrivant</param>
    /// <returns>Message de confirmation</returns>
    /// <exception cref="KeyNotFoundException">Inscription non trouvée</exception>
    /// <remarks>
    /// Effectue un soft delete (annulation) de l'inscription.
    /// Libère une place pour permettre une nouvelle inscription.
    /// </remarks>
    Task<string> UnregisterFromEventAsync(int eventId, string userEmail);

    /// <summary>
    /// Récupère tous les inscrits à un événement
    /// </summary>
    /// <param name="eventId">Identifiant de l'événement</param>
    /// <returns>Événement avec liste complète des inscrits ou null si non trouvé</returns>
    /// <remarks>
    /// Retourne l'événement avec toutes les inscriptions actives et leurs réponses aux formulaires.
    /// N'inclut pas les inscriptions annulées.
    /// </remarks>
    Task<EventWithRegistrationsDto?> GetEventRegistrationsAsync(int eventId);
}
