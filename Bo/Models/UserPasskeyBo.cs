using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bo.Models;

[Table("UserPasskeys")]
public class UserPasskeyBo
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    /// <summary>
    /// Identifiant unique du credential WebAuthn (base64url encoded)
    /// </summary>
    [Required]
    [StringLength(512)]
    public string CredentialId { get; set; } = string.Empty;

    /// <summary>
    /// Clé publique du credential (format COSE)
    /// </summary>
    [Required]
    public byte[] PublicKey { get; set; } = [];

    /// <summary>
    /// Compteur de signatures pour la détection de clonage
    /// </summary>
    [Required]
    public uint SignCount { get; set; }

    /// <summary>
    /// AAGUID de l'authenticator
    /// </summary>
    public Guid AaGuid { get; set; }

    /// <summary>
    /// Type de credential (e.g. "public-key")
    /// </summary>
    [StringLength(32)]
    public string CredentialType { get; set; } = "public-key";

    /// <summary>
    /// Nom d'affichage donné par l'utilisateur
    /// </summary>
    [StringLength(255)]
    public string? DisplayName { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastUsedAt { get; set; }

    // Navigation
    public virtual UserBo User { get; set; } = null!;
}
