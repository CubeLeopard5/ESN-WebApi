using System.ComponentModel.DataAnnotations;

namespace Dto.EventTemplate;

public class CreateEventTemplateDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public string SurveyJsData { get; set; } = string.Empty;

    /// <summary>
    /// Notes internes pour les organisateurs
    /// </summary>
    [MaxLength(10000)]
    public string? OrganizerNotes { get; set; }
}
