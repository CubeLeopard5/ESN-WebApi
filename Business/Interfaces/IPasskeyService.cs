using Dto.Passkey;
using Dto.User;
using Fido2NetLib;

namespace Business.Interfaces;

/// <summary>
/// Service de gestion des passkeys WebAuthn/FIDO2
/// </summary>
public interface IPasskeyService
{
    /// <summary>
    /// Initie l'enregistrement d'une nouvelle passkey pour un utilisateur authentifié
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <returns>Tuple contenant l'identifiant du challenge et les options de création du credential</returns>
    /// <exception cref="KeyNotFoundException">Utilisateur non trouvé</exception>
    /// <remarks>
    /// Le challenge est stocké en cache avec un TTL de 5 minutes (single-use).
    /// Les credentials existants de l'utilisateur sont exclus (excludeCredentials).
    /// </remarks>
    Task<(string ChallengeId, CredentialCreateOptions Options)> BeginRegistrationAsync(int userId);

    /// <summary>
    /// Complète l'enregistrement d'une passkey après vérification de l'attestation
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <param name="dto">Réponse d'attestation du navigateur et nom d'affichage</param>
    /// <returns>La passkey créée</returns>
    /// <exception cref="KeyNotFoundException">Utilisateur non trouvé ou challenge invalide</exception>
    /// <exception cref="Fido2VerificationException">Attestation invalide</exception>
    Task<PasskeyDto> CompleteRegistrationAsync(int userId, PasskeyRegistrationCompleteDto dto);

    /// <summary>
    /// Initie le login par passkey
    /// </summary>
    /// <param name="dto">Email optionnel pour filtrer les credentials autorisés</param>
    /// <returns>Tuple contenant l'identifiant du challenge et les options d'assertion</returns>
    /// <remarks>
    /// Si un email est fourni, retourne les allowCredentials de cet utilisateur.
    /// Si aucun email, lance une cérémonie discoverable (resident key).
    /// </remarks>
    Task<(string ChallengeId, AssertionOptions Options)> BeginLoginAsync(PasskeyLoginBeginDto dto);

    /// <summary>
    /// Complète le login par passkey après vérification de l'assertion
    /// </summary>
    /// <param name="dto">Réponse d'assertion du navigateur et identifiant du challenge</param>
    /// <returns>Token JWT et profil utilisateur</returns>
    /// <exception cref="KeyNotFoundException">Challenge ou credential non trouvé</exception>
    /// <exception cref="Business.Exceptions.ForbiddenAccessException">Compte en attente ou refusé</exception>
    /// <exception cref="Fido2VerificationException">Assertion invalide</exception>
    Task<UserLoginResponseDto> CompleteLoginAsync(PasskeyLoginCompleteDto dto);

    /// <summary>
    /// Récupère toutes les passkeys d'un utilisateur
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur</param>
    /// <returns>Liste des passkeys</returns>
    Task<IEnumerable<PasskeyDto>> GetUserPasskeysAsync(int userId);

    /// <summary>
    /// Met à jour le nom d'affichage d'une passkey
    /// </summary>
    /// <param name="passkeyId">Identifiant de la passkey</param>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire</param>
    /// <param name="dto">Nouveau nom d'affichage</param>
    /// <returns>La passkey mise à jour ou null si non trouvée ou non propriétaire</returns>
    Task<PasskeyDto?> UpdatePasskeyAsync(int passkeyId, int userId, UpdatePasskeyDto dto);

    /// <summary>
    /// Supprime une passkey
    /// </summary>
    /// <param name="passkeyId">Identifiant de la passkey</param>
    /// <param name="userId">Identifiant de l'utilisateur propriétaire</param>
    /// <returns>True si supprimée, false si non trouvée ou non propriétaire</returns>
    Task<bool> DeletePasskeyAsync(int passkeyId, int userId);
}
