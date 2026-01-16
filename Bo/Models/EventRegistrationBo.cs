using System.ComponentModel.DataAnnotations;
using Bo.Enums;

namespace Bo.Models;

public partial class EventRegistrationBo
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    [MaxLength(100000)]
    public string SurveyJsData { get; set; } = string.Empty;

    public DateTime? RegisteredAt { get; set; }

    public string Status { get; set; } = null!;

    /// <summary>
    /// Statut de présence (null si non encore validé)
    /// </summary>
    public AttendanceStatus? AttendanceStatus { get; set; }

    /// <summary>
    /// Date/heure de validation de la présence
    /// </summary>
    public DateTime? AttendanceValidatedAt { get; set; }

    /// <summary>
    /// ID de l'utilisateur ayant validé la présence
    /// </summary>
    public int? AttendanceValidatedById { get; set; }

    public virtual EventBo Event { get; set; } = null!;

    public virtual UserBo User { get; set; } = null!;

    /// <summary>
    /// Utilisateur ayant validé la présence
    /// </summary>
    public virtual UserBo? AttendanceValidatedBy { get; set; }
}
