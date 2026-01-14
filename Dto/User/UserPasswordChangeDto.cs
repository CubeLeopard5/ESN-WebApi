using System.ComponentModel.DataAnnotations;

namespace Dto.User
{
    public class UserPasswordChangeDto
    {
        [Required]
        [StringLength(255)]
        public string OldPassword { get; set; } = string.Empty;

        [Required]
        [StringLength(255)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
