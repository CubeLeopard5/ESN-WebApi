namespace Dto.EventFeedback;

/// <summary>
/// DTO pour les statistiques agrégées des feedbacks d'un événement
/// </summary>
public class FeedbackSummaryDto
{
    /// <summary>
    /// Identifiant de l'événement
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Titre de l'événement
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Nombre total de participants ayant assisté (AttendanceStatus = Present)
    /// </summary>
    public int TotalAttendees { get; set; }

    /// <summary>
    /// Nombre de feedbacks soumis
    /// </summary>
    public int TotalFeedbacks { get; set; }

    /// <summary>
    /// Taux de réponse (TotalFeedbacks / TotalAttendees * 100)
    /// </summary>
    public decimal ResponseRate { get; set; }

    /// <summary>
    /// Date limite pour soumettre un feedback
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// Indique si un formulaire de feedback est configuré
    /// </summary>
    public bool HasFeedbackForm { get; set; }
}
