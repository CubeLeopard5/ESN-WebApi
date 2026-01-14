using System.ComponentModel.DataAnnotations;

namespace Dto.EventTemplate;

public class CreateEventFromTemplateDto
{
    [Required]
    public int TemplateId { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? Location { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public int? MaxParticipants { get; set; }

    public string? EventfrogLink { get; set; }

    public string? SurveyJsData { get; set; }
}
