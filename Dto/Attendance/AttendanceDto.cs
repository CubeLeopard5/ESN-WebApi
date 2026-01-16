using Bo.Enums;
using Dto.User;

namespace Dto.Attendance;

/// <summary>
/// DTO représentant une inscription avec informations de présence
/// </summary>
public class AttendanceDto
{
    /// <summary>
    /// ID de l'inscription
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Date d'inscription
    /// </summary>
    public DateTime RegisteredAt { get; set; }

    /// <summary>
    /// Statut de l'inscription (registered, cancelled, etc.)
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Données du formulaire SurveyJS
    /// </summary>
    public string SurveyJsData { get; set; } = string.Empty;

    /// <summary>
    /// Utilisateur inscrit
    /// </summary>
    public UserDto User { get; set; } = null!;

    /// <summary>
    /// Statut de présence (null si non encore validé)
    /// </summary>
    public AttendanceStatus? AttendanceStatus { get; set; }

    /// <summary>
    /// Date/heure de validation de la présence
    /// </summary>
    public DateTime? AttendanceValidatedAt { get; set; }

    /// <summary>
    /// Utilisateur ayant validé la présence
    /// </summary>
    public UserDto? AttendanceValidatedBy { get; set; }
}
