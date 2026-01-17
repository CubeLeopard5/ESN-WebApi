using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Repository spécialisé pour les feedbacks d'événements
/// </summary>
public interface IEventFeedbackRepository : IRepository<EventFeedbackBo>
{
    /// <summary>
    /// Récupère tous les feedbacks d'un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Liste des feedbacks avec les informations utilisateur</returns>
    Task<IEnumerable<EventFeedbackBo>> GetByEventIdAsync(int eventId);

    /// <summary>
    /// Récupère le feedback d'un utilisateur pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Feedback ou null si non trouvé</returns>
    Task<EventFeedbackBo?> GetByEventAndUserAsync(int eventId, int userId);

    /// <summary>
    /// Vérifie si un utilisateur a déjà soumis un feedback pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>True si un feedback existe</returns>
    Task<bool> HasUserSubmittedFeedbackAsync(int eventId, int userId);

    /// <summary>
    /// Compte le nombre de feedbacks pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Nombre de feedbacks</returns>
    Task<int> CountByEventIdAsync(int eventId);

    /// <summary>
    /// Récupère tous les feedbacks d'un utilisateur
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <returns>Liste des feedbacks avec les informations événement</returns>
    Task<IEnumerable<EventFeedbackBo>> GetByUserIdAsync(int userId);
}
