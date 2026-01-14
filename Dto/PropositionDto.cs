using Dto.User;
using System.ComponentModel.DataAnnotations;

namespace Dto
{
    public class PropositionDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Description { get; set; } = string.Empty;

        [Required]
        public DateTime CreatedAt { get; set; }

        [Required]
        public int UserId { get; set; }

        public UserDto? User { get; set; } = null;

        public int VotesUp { get; set; } = 0;

        public int VotesDown { get; set; } = 0;

        /// <summary>
        /// Vote de l'utilisateur actuel (null si pas de vote, 1 pour Up, -1 pour Down)
        /// </summary>
        /// <remarks>
        /// Valeurs: null = pas de vote, 1 = Up, -1 = Down
        /// Attention: En base de données, VoteType.Down = 2, mais le DTO expose -1 pour compatibilité frontend
        /// </remarks>
        public int? UserVoteType { get; set; } = null;
    }
}
