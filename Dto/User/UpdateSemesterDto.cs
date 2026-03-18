using System.ComponentModel.DataAnnotations;

namespace Dto.User;

/// <summary>
/// DTO for updating the semester of an exchange student
/// </summary>
public class UpdateSemesterDto
{
    /// <summary>
    /// Semester value in format "season-year", e.g. "autumn-2025", "spring-2026", "both-2025", or null to clear
    /// </summary>
    [RegularExpression(@"^(autumn|spring|both)-\d{4}$", ErrorMessage = "Semester must be in format 'autumn-YYYY', 'spring-YYYY', or 'both-YYYY'.")]
    [StringLength(15)]
    public string? Semester { get; set; }
}
