namespace Dto.Passkey;

/// <summary>
/// DTO pour initier le login par passkey
/// </summary>
public class PasskeyLoginBeginDto
{
    /// <summary>
    /// Email de l'utilisateur (optionnel pour le login discoverable)
    /// </summary>
    public string? Email { get; set; }
}
