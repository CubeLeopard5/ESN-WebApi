using System.ComponentModel.DataAnnotations;

namespace Dto.EventFeedback;

/// <summary>
/// DTO pour mettre à jour le formulaire de feedback d'un événement (Admin)
/// </summary>
public class UpdateFeedbackFormDto
{
    /// <summary>
    /// Schéma du formulaire de feedback (JSON SurveyJS)
    /// Null pour supprimer le formulaire
    /// </summary>
    [MaxLength(100000, ErrorMessage = "Le schéma du formulaire dépasse la taille maximale autorisée")]
    public string? FeedbackFormData { get; set; }

    /// <summary>
    /// Date limite pour soumettre un feedback
    /// Null pour pas de limite
    /// </summary>
    public DateTime? FeedbackDeadline { get; set; }
}
