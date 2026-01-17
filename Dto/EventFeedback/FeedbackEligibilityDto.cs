namespace Dto.EventFeedback;

/// <summary>
/// DTO pour vérifier l'éligibilité d'un utilisateur à soumettre un feedback
/// </summary>
public class FeedbackEligibilityDto
{
    /// <summary>
    /// Indique si l'utilisateur peut soumettre un feedback
    /// </summary>
    public bool CanSubmit { get; set; }

    /// <summary>
    /// Raison si l'utilisateur ne peut pas soumettre (not_attended, already_submitted, deadline_passed, no_feedback_form)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Indique si l'utilisateur a déjà soumis un feedback
    /// </summary>
    public bool HasSubmitted { get; set; }

    /// <summary>
    /// Schéma du formulaire de feedback (JSON SurveyJS) si éligible
    /// </summary>
    public string? FeedbackFormData { get; set; }

    /// <summary>
    /// Date limite pour soumettre le feedback
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Feedback existant de l'utilisateur (si HasSubmitted = true)
    /// </summary>
    public EventFeedbackDto? ExistingFeedback { get; set; }
}
