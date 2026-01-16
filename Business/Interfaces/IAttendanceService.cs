using Bo.Enums;
using Dto.Attendance;

namespace Business.Interfaces;

/// <summary>
/// Service de gestion des présences aux événements
/// </summary>
public interface IAttendanceService
{
    /// <summary>
    /// Récupère un événement avec toutes les inscriptions et leurs statuts de présence
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Événement avec inscriptions et statistiques, ou null si non trouvé</returns>
    Task<EventAttendanceDto?> GetEventAttendanceAsync(int eventId);

    /// <summary>
    /// Récupère les statistiques de présence d'un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Statistiques de présence, ou null si événement non trouvé</returns>
    Task<AttendanceStatsDto?> GetAttendanceStatsAsync(int eventId);

    /// <summary>
    /// Valide la présence d'un participant
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="registrationId">ID de l'inscription</param>
    /// <param name="status">Statut de présence à attribuer</param>
    /// <param name="validatorEmail">Email de l'utilisateur qui valide</param>
    /// <returns>Inscription mise à jour</returns>
    /// <exception cref="KeyNotFoundException">Inscription non trouvée</exception>
    /// <exception cref="UnauthorizedAccessException">Utilisateur non autorisé</exception>
    /// <exception cref="InvalidOperationException">Inscription non valide pour validation</exception>
    Task<AttendanceDto> ValidateAttendanceAsync(int eventId, int registrationId, AttendanceStatus status, string validatorEmail);

    /// <summary>
    /// Valide la présence de plusieurs participants en une fois
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="dto">Liste des validations à effectuer</param>
    /// <param name="validatorEmail">Email de l'utilisateur qui valide</param>
    /// <returns>Nombre d'inscriptions mises à jour</returns>
    /// <exception cref="UnauthorizedAccessException">Utilisateur non autorisé</exception>
    Task<int> BulkValidateAttendanceAsync(int eventId, BulkValidateAttendanceDto dto, string validatorEmail);

    /// <summary>
    /// Réinitialise la présence d'un participant (remet à null)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="registrationId">ID de l'inscription</param>
    /// <param name="validatorEmail">Email de l'utilisateur qui effectue le reset</param>
    /// <returns>True si réinitialisé, false si inscription non trouvée</returns>
    /// <exception cref="UnauthorizedAccessException">Utilisateur non autorisé</exception>
    Task<bool> ResetAttendanceAsync(int eventId, int registrationId, string validatorEmail);
}
