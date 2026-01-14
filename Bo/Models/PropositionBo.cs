using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bo.Models;

[Table("Propositions")]
public partial class PropositionBo
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(255)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [Required]
    public int UserId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? ModifiedAt { get; set; }

    public DateTime? DeletedAt { get; set; }

    public int VotesUp { get; set; } = 0;

    public int VotesDown { get; set; } = 0;

    public bool IsDeleted { get; set; } = false;

    // Navigation properties
    public virtual UserBo User { get; set; } = null!;
    public virtual ICollection<PropositionVoteBo> Votes { get; set; } = new List<PropositionVoteBo>();
}
