namespace Dto.Passkey;

/// <summary>
/// DTO de réponse pour une passkey utilisateur
/// </summary>
public class PasskeyDto
{
    /// <summary>
    /// Identifiant unique de la passkey
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Nom d'affichage donné par l'utilisateur
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Date de création de la passkey
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Date de dernière utilisation pour une authentification
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
