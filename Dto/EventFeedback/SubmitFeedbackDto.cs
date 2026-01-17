using System.ComponentModel.DataAnnotations;

namespace Dto.EventFeedback;

/// <summary>
/// DTO pour soumettre un feedback
/// </summary>
public class SubmitFeedbackDto
{
    /// <summary>
    /// Données du formulaire de feedback (JSON SurveyJS response)
    /// </summary>
    [Required(ErrorMessage = "Les données du feedback sont requises")]
    [MaxLength(100000, ErrorMessage = "Les données du feedback dépassent la taille maximale autorisée")]
    public string FeedbackData { get; set; } = string.Empty;
}
