using System.ComponentModel.DataAnnotations;

namespace Bo.Models;

/// <summary>
/// Entité représentant un feedback soumis par un participant après un événement
/// </summary>
public class EventFeedbackBo
{
    /// <summary>
    /// Identifiant unique du feedback
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Identifiant de l'événement concerné
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Identifiant de l'utilisateur ayant soumis le feedback
    /// </summary>
    public int UserId { get; set; }

    /// <summary>
    /// Données du formulaire de feedback (JSON SurveyJS response)
    /// </summary>
    [MaxLength(100000)]
    public string FeedbackData { get; set; } = string.Empty;

    /// <summary>
    /// Date de soumission du feedback
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Date de dernière modification du feedback (null si jamais modifié)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Événement concerné
    /// </summary>
    public virtual EventBo Event { get; set; } = null!;

    /// <summary>
    /// Utilisateur ayant soumis le feedback
    /// </summary>
    public virtual UserBo User { get; set; } = null!;
}
