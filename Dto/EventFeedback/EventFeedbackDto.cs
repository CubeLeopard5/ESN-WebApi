using Dto.User;

namespace Dto.EventFeedback;

/// <summary>
/// DTO représentant un feedback soumis
/// </summary>
public class EventFeedbackDto
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
    /// Nom complet de l'utilisateur
    /// </summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Données du formulaire de feedback (JSON SurveyJS response)
    /// </summary>
    public string FeedbackData { get; set; } = string.Empty;

    /// <summary>
    /// Date de soumission du feedback
    /// </summary>
    public DateTime SubmittedAt { get; set; }

    /// <summary>
    /// Date de dernière modification (null si jamais modifié)
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Informations de l'utilisateur
    /// </summary>
    public UserDto? User { get; set; }
}
