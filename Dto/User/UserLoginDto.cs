using System.ComponentModel.DataAnnotations;

namespace Dto.User
{
    public class UserLoginDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string Password { get; set; } = string.Empty;
    }

}
