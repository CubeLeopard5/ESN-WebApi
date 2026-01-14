using System.ComponentModel.DataAnnotations;

namespace Dto.User
{
    public class UserUpdateDto
    {
        [Required]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide.")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty;

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

        [StringLength(50)]
        public string? EsnCardNumber { get; set; }

        [StringLength(255)]
        public string? UniversityName { get; set; }

        [StringLength(100)]
        public string? TransportPass { get; set; }
    }
}
