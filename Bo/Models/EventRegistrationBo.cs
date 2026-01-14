using System.ComponentModel.DataAnnotations;

namespace Bo.Models;

public partial class EventRegistrationBo
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    [MaxLength(100000)]
    public string SurveyJsData { get; set; } = string.Empty;

    public DateTime? RegisteredAt { get; set; }

    public string Status { get; set; } = null!;

    public virtual EventBo Event { get; set; } = null!;

    public virtual UserBo User { get; set; } = null!;
}
