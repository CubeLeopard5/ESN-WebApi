using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bo.Models;

public partial class CalendarBo
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public DateTime EventDate { get; set; }

    public int? EventId { get; set; }

    public int? MainOrganizerId { get; set; }

    public int? EventManagerId { get; set; }

    public int? ResponsableComId { get; set; }

    [ForeignKey("EventId")]
    public virtual EventBo? Event { get; set; }

    [ForeignKey("MainOrganizerId")]
    public virtual UserBo? MainOrganizer { get; set; }

    [ForeignKey("EventManagerId")]
    public virtual UserBo? EventManager { get; set; }

    [ForeignKey("ResponsableComId")]
    public virtual UserBo? ResponsableCom { get; set; }

    public virtual ICollection<CalendarSubOrganizerBo> CalendarSubOrganizers { get; set; } = [];
}