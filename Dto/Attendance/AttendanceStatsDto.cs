namespace Dto.Attendance;

/// <summary>
/// DTO représentant les statistiques de présence d'un événement
/// </summary>
public class AttendanceStatsDto
{
    /// <summary>
    /// ID de l'événement
    /// </summary>
    public int EventId { get; set; }

    /// <summary>
    /// Titre de l'événement
    /// </summary>
    public string EventTitle { get; set; } = string.Empty;

    /// <summary>
    /// Date de l'événement
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Nombre total d'inscrits (status = registered)
    /// </summary>
    public int TotalRegistered { get; set; }

    /// <summary>
    /// Nombre total de présences validées
    /// </summary>
    public int TotalValidated { get; set; }

    /// <summary>
    /// Nombre de présents
    /// </summary>
    public int PresentCount { get; set; }

    /// <summary>
    /// Nombre d'absents
    /// </summary>
    public int AbsentCount { get; set; }

    /// <summary>
    /// Nombre d'excusés
    /// </summary>
    public int ExcusedCount { get; set; }

    /// <summary>
    /// Nombre non encore validés
    /// </summary>
    public int NotYetValidatedCount { get; set; }

    /// <summary>
    /// Taux de présence (présents / inscrits * 100)
    /// </summary>
    public decimal AttendanceRate { get; set; }

    /// <summary>
    /// Taux de validation (validés / inscrits * 100)
    /// </summary>
    public decimal ValidationRate { get; set; }
}
