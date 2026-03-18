using System.ComponentModel.DataAnnotations;

namespace Dto.User;

/// <summary>
/// DTO for removing ESN member status
/// </summary>
public class RemoveEsnMemberDto
{
    /// <summary>
    /// New student type to assign ("local" or "exchange")
    /// </summary>
    [Required]
    [RegularExpression("local|exchange", ErrorMessage = "Must be 'local' or 'exchange'.")]
    public string NewStudentType { get; set; } = string.Empty;
}
