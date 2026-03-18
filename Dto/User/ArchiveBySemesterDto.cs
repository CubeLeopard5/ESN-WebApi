using System.ComponentModel.DataAnnotations;

namespace Dto.User;

/// <summary>
/// DTO for bulk archiving students by semester
/// </summary>
public class ArchiveBySemesterDto
{
    /// <summary>
    /// Semester to archive, e.g. "autumn-2025"
    /// </summary>
    [Required]
    [RegularExpression(@"^(autumn|spring|both)-\d{4}$", ErrorMessage = "Semester must be in format 'autumn-YYYY', 'spring-YYYY', or 'both-YYYY'.")]
    public string Semester { get; set; } = string.Empty;

    /// <summary>
    /// Optional student type filter ("exchange", "local", or null for all non-ESN members)
    /// </summary>
    [RegularExpression("exchange|local", ErrorMessage = "StudentType must be 'exchange' or 'local'.")]
    public string? StudentType { get; set; }
}
