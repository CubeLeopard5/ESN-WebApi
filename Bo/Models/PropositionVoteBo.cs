namespace Bo.Models;

public class PropositionVoteBo
{
    public int Id { get; set; }
    public int PropositionId { get; set; }
    public int UserId { get; set; }
    public VoteType VoteType { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public PropositionBo Proposition { get; set; } = null!;
    public UserBo User { get; set; } = null!;
}

public enum VoteType
{
    Up = 1,
    Down = 2
}
