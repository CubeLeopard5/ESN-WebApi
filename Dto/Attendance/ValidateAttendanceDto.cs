using System.ComponentModel.DataAnnotations;
using Bo.Enums;

namespace Dto.Attendance;

/// <summary>
/// DTO pour valider la présence d'un participant
/// </summary>
public class ValidateAttendanceDto
{
    /// <summary>
    /// Statut de présence à attribuer
    /// </summary>
    [Required]
    public AttendanceStatus Status { get; set; }
}
