using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface pour le repository Passkey avec méthodes spécifiques WebAuthn
/// </summary>
public interface IPasskeyRepository : IRepository<UserPasskeyBo>
{
    /// <summary>
    /// Récupère toutes les passkeys d'un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <returns>Liste des passkeys de l'utilisateur</returns>
    Task<IEnumerable<UserPasskeyBo>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Récupère une passkey par son CredentialId WebAuthn
    /// </summary>
    /// <param name="credentialId">Identifiant du credential (base64url)</param>
    /// <returns>La passkey ou null si non trouvée</returns>
    Task<UserPasskeyBo?> GetByCredentialIdAsync(string credentialId);

    /// <summary>
    /// Vérifie si un CredentialId existe déjà
    /// </summary>
    /// <param name="credentialId">Identifiant du credential à vérifier</param>
    /// <returns>True si le credential existe déjà</returns>
    Task<bool> CredentialIdExistsAsync(string credentialId);
}
