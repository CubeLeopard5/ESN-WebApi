using System.ComponentModel.DataAnnotations;

namespace Bo.Models;

public partial class EventTemplateBo
{
    public int Id { get; set; }
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }

    [MaxLength(100000)]
    public string SurveyJsData { get; set; } = null!;
}
