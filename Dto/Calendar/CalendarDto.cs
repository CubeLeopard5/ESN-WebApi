using Dto.Event;
using Dto.User;
using System.ComponentModel.DataAnnotations;

namespace Dto.Calendar;

public class CalendarDto
{
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    public int? EventId { get; set; }
    public EventDto? Event { get; set; }

    public int? MainOrganizerId { get; set; }
    public UserDto? MainOrganizer { get; set; }

    public int? EventManagerId { get; set; }
    public UserDto? EventManager { get; set; }

    public int? ResponsableComId { get; set; }
    public UserDto? ResponsableCom { get; set; }

    public List<UserDto> SubOrganizers { get; set; } = [];
}