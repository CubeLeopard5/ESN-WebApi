namespace Dto.Passkey;

/// <summary>
/// DTO de réponse pour une passkey utilisateur
/// </summary>
public class PasskeyDto
{
    public int Id { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}
