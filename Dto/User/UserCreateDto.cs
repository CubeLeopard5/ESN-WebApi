using System.ComponentModel.DataAnnotations;

namespace Dto.User
{
    public class UserCreateDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        public DateTime BirthDate { get; set; }

        [Required]
        [Phone(ErrorMessage = "Le numéro de téléphone n'est pas valide.")]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }

        [Required]
        [RegularExpression("exchange|local|esn_member", ErrorMessage = "Le type d'étudiant doit être 'exchange', 'local' ou 'esn_member'.")]
        [StringLength(50)]
        public string StudentType { get; set; } = string.Empty;
    }
}
