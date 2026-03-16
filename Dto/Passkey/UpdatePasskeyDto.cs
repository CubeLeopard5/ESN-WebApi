using System.ComponentModel.DataAnnotations;

namespace Dto.Passkey;

/// <summary>
/// DTO pour mettre à jour une passkey (renommer)
/// </summary>
public class UpdatePasskeyDto
{
    /// <summary>
    /// Nouveau nom d'affichage pour la passkey
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;
}
