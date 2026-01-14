using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bo.Models;

public partial class CalendarSubOrganizerBo
{
    [Required]
    public int CalendarId { get; set; }

    [Required]
    public int UserId { get; set; }

    [ForeignKey("CalendarId")]
    public virtual CalendarBo Calendar { get; set; } = null!;

    [ForeignKey("UserId")]
    public virtual UserBo User { get; set; } = null!;
}