using System.ComponentModel.DataAnnotations;

namespace Dto.Passkey;

/// <summary>
/// DTO pour compléter l'enregistrement d'une passkey (attestation response du navigateur)
/// </summary>
public class PasskeyRegistrationCompleteDto
{
    /// <summary>
    /// Identifiant du challenge (GUID retourné par /register/begin)
    /// </summary>
    [Required]
    public string ChallengeId { get; set; } = string.Empty;

    /// <summary>
    /// Réponse d'attestation sérialisée en JSON depuis navigator.credentials.create()
    /// </summary>
    [Required]
    public string AttestationResponse { get; set; } = string.Empty;

    /// <summary>
    /// Nom d'affichage donné par l'utilisateur à cette passkey
    /// </summary>
    [MaxLength(255)]
    public string? DisplayName { get; set; }
}
