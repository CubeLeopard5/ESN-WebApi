using Dto.EventFeedback;

namespace Business.Interfaces;

/// <summary>
/// Service de gestion des feedbacks d'événements
/// </summary>
public interface IEventFeedbackService
{
    /// <summary>
    /// Vérifie l'éligibilité d'un utilisateur à soumettre un feedback pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur</param>
    /// <returns>Informations d'éligibilité incluant le formulaire si éligible</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    Task<FeedbackEligibilityDto> CheckEligibilityAsync(int eventId, string userEmail);

    /// <summary>
    /// Soumet un feedback pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur</param>
    /// <param name="dto">Données du feedback</param>
    /// <returns>Feedback créé</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    /// <exception cref="InvalidOperationException">Utilisateur non éligible à soumettre un feedback</exception>
    Task<EventFeedbackDto> SubmitFeedbackAsync(int eventId, string userEmail, SubmitFeedbackDto dto);

    /// <summary>
    /// Récupère le feedback d'un utilisateur pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur</param>
    /// <returns>Feedback ou null si non trouvé</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    Task<EventFeedbackDto?> GetUserFeedbackAsync(int eventId, string userEmail);

    /// <summary>
    /// Met à jour le feedback d'un utilisateur pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="userEmail">Email de l'utilisateur</param>
    /// <param name="dto">Nouvelles données du feedback</param>
    /// <returns>Feedback mis à jour</returns>
    /// <exception cref="KeyNotFoundException">Événement ou feedback non trouvé</exception>
    /// <exception cref="InvalidOperationException">Deadline passée, modification non autorisée</exception>
    Task<EventFeedbackDto> UpdateFeedbackAsync(int eventId, string userEmail, SubmitFeedbackDto dto);

    /// <summary>
    /// Récupère tous les feedbacks d'un événement (Admin/ESN Member)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="adminEmail">Email de l'administrateur</param>
    /// <returns>Liste des feedbacks</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    /// <exception cref="UnauthorizedAccessException">Utilisateur non autorisé</exception>
    Task<IEnumerable<EventFeedbackDto>> GetAllFeedbacksAsync(int eventId, string adminEmail);

    /// <summary>
    /// Récupère les statistiques de feedback d'un événement (Admin/ESN Member)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="adminEmail">Email de l'administrateur</param>
    /// <returns>Statistiques agrégées</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    /// <exception cref="UnauthorizedAccessException">Utilisateur non autorisé</exception>
    Task<FeedbackSummaryDto> GetFeedbackSummaryAsync(int eventId, string adminEmail);

    /// <summary>
    /// Met à jour le formulaire de feedback d'un événement (Admin/ESN Member)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="adminEmail">Email de l'administrateur</param>
    /// <param name="dto">Nouveau formulaire et deadline</param>
    /// <returns>True si mis à jour avec succès</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    /// <exception cref="UnauthorizedAccessException">Utilisateur non autorisé</exception>
    Task<bool> UpdateFeedbackFormAsync(int eventId, string adminEmail, UpdateFeedbackFormDto dto);
}
