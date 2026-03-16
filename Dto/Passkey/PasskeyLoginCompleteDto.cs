using System.ComponentModel.DataAnnotations;

namespace Dto.Passkey;

/// <summary>
/// DTO pour compléter le login par passkey (assertion response du navigateur)
/// </summary>
public class PasskeyLoginCompleteDto
{
    /// <summary>
    /// Identifiant du challenge (GUID retourné par /login/begin)
    /// </summary>
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>
    /// Réponse d'assertion sérialisée en JSON depuis navigator.credentials.get()
    /// </summary>
    [Required]
    public string AssertionResponse { get; set; } = string.Empty;
}
