using System.ComponentModel.DataAnnotations;

namespace Dto.Attendance;

/// <summary>
/// DTO pour valider la présence de plusieurs participants en une fois
/// </summary>
public class BulkValidateAttendanceDto
{
    /// <summary>
    /// Liste des validations à effectuer
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one attendance must be provided")]
    public List<BulkAttendanceItemDto> Attendances { get; set; } = new();
}

/// <summary>
/// Item de validation en masse
/// </summary>
public class BulkAttendanceItemDto
{
    /// <summary>
    /// ID de l'inscription
    /// </summary>
    [Required]
    public int RegistrationId { get; set; }

    /// <summary>
    /// Statut de présence
    /// </summary>
    [Required]
    public Bo.Enums.AttendanceStatus Status { get; set; }
}
