using System.ComponentModel.DataAnnotations;

namespace Dto.User
{
    /// <summary>
    /// DTO pour refuser un utilisateur avec une raison optionnelle
    /// </summary>
    public class RejectUserDto
    {
        /// <summary>
        /// Raison du refus (optionnel)
        /// </summary>
        [StringLength(500, ErrorMessage = "La raison ne peut pas dépasser 500 caractères.")]
        public string? Reason { get; set; }
    }
}
