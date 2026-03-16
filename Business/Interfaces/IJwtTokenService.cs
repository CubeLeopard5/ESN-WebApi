using Bo.Models;

namespace Business.Interfaces;

/// <summary>
/// Service dédié à la génération et validation des tokens JWT
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Génère un token JWT pour un utilisateur authentifié
    /// </summary>
    /// <param name="user">Utilisateur pour lequel générer le token (doit inclure le rôle)</param>
    /// <returns>Token JWT signé et encodé</returns>
    /// <remarks>
    /// Le token contient les claims suivants :
    /// - sub : Email de l'utilisateur
    /// - userId : Identifiant unique
    /// - name : Prénom et nom
    /// - studentType : Type d'étudiant
    /// - role : Rôle (Admin, User)
    /// - Permissions : CanCreateEvents, CanModifyEvents, etc.
    ///
    /// Configuration :
    /// - Algorithme : HMAC-SHA256
    /// - Durée : 30 minutes (configurable via Jwt:ExpireMinutes)
    /// - Issuer et Audience : configurés dans appsettings.json
    /// </remarks>
    string GenerateToken(UserBo user);
}
