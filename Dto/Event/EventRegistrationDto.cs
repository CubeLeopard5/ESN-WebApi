using Dto.User;
using System.ComponentModel.DataAnnotations;

namespace Dto.Event
{
    public class EventRegistrationDto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime RegisteredAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = string.Empty;

        [Required]
        public string SurveyJsData { get; set; } = string.Empty;

        [Required]
        public UserDto User { get; set; } = null!;
    }
}
