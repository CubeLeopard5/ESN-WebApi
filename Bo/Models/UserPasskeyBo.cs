using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Bo.Models;

/// <summary>
/// Entité représentant une passkey WebAuthn/FIDO2 associée à un utilisateur.
/// Stocke les informations du credential nécessaires à la vérification des assertions.
/// </summary>
[Table("UserPasskeys")]
public class UserPasskeyBo
{
    /// <summary>
    /// Identifiant unique de la passkey
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Identifiant de l'utilisateur propriétaire de cette passkey
    /// </summary>
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

    /// <summary>
    /// Date de création de la passkey
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Date de dernière utilisation de la passkey pour une authentification
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Utilisateur propriétaire de cette passkey
    /// </summary>
    public virtual UserBo User { get; set; } = null!;
}
