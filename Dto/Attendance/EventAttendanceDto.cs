using Dto.User;

namespace Dto.Attendance;

/// <summary>
/// DTO représentant un événement avec la liste des présences
/// </summary>
public class EventAttendanceDto
{
    /// <summary>
    /// ID de l'événement
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Titre de l'événement
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Date de début de l'événement
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Date de fin de l'événement
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Lieu de l'événement
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Organisateur de l'événement
    /// </summary>
    public UserDto? Organizer { get; set; }

    /// <summary>
    /// Liste des inscriptions avec présences
    /// </summary>
    public List<AttendanceDto> Registrations { get; set; } = new();

    /// <summary>
    /// Statistiques de présence
    /// </summary>
    public AttendanceStatsDto Stats { get; set; } = new();
}
