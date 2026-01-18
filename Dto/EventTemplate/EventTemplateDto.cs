using System.ComponentModel.DataAnnotations;

namespace Dto.EventTemplate;

public class EventTemplateDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; }
    public string SurveyJsData { get; set; } = string.Empty;

    /// <summary>
    /// Notes internes pour les organisateurs
    /// </summary>
    [MaxLength(10000)]
    public string? OrganizerNotes { get; set; }
}
