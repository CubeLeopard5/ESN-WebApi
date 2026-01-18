using System.ComponentModel.DataAnnotations;

namespace Dto.EventTemplate;

/// <summary>
/// DTO for applying a template to create a new event.
/// The TemplateId is provided in the URL path, not in the body.
/// </summary>
public class ApplyTemplateDto
{
    /// <summary>
    /// Title of the new event
    /// </summary>
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Optional description override (uses template description if null)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Event location
    /// </summary>
    public string? Location { get; set; }

    /// <summary>
    /// Event start date and time
    /// </summary>
    [Required]
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Optional event end date and time
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Maximum number of participants (null = unlimited)
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Optional Eventfrog link
    /// </summary>
    public string? EventfrogLink { get; set; }

    /// <summary>
    /// Optional SurveyJS form data override (uses template form if null)
    /// </summary>
    public string? SurveyJsData { get; set; }

    /// <summary>
    /// Optional organizer notes override (uses template notes if null)
    /// </summary>
    [MaxLength(10000)]
    public string? OrganizerNotes { get; set; }
}
