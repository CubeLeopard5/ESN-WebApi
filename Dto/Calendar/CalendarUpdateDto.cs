using System.ComponentModel.DataAnnotations;

namespace Dto.Calendar;

public class CalendarUpdateDto
{
    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    public int? EventId { get; set; }

    public int? MainOrganizerId { get; set; }

    public int? EventManagerId { get; set; }

    public int? ResponsableComId { get; set; }

    public List<int>? SubOrganizerIds { get; set; }
}