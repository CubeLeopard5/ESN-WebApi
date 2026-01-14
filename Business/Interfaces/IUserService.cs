using Bo.Enums;
using Dto.Common;
using Dto.User;

namespace Business.Interfaces;

/// <summary>
/// Interface de gestion des utilisateurs, authentification JWT et opérations CRUD
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Authentifie un utilisateur et génère un token JWT
    /// </summary>
    /// <param name="loginDto">Identifiants de connexion (email et mot de passe)</param>
    /// <returns>Token JWT et profil utilisateur complet</returns>
    /// <exception cref="UnauthorizedAccessException">Identifiants invalides ou utilisateur inexistant</exception>
    /// <remarks>
    /// Implémente une protection contre les attaques par timing en effectuant
    /// un hashage factice même si l'utilisateur n'existe pas.
    /// Rate limiting : 5 tentatives par 5 minutes par IP
    /// </remarks>
    Task<UserLoginResponseDto> LoginAsync(UserLoginDto loginDto);

    /// <summary>
    /// Rafraîchit un token JWT expiré et génère un nouveau token
    /// </summary>
    /// <param name="token">Token JWT expiré à rafraîchir</param>
    /// <returns>Nouveau token JWT et profil utilisateur mis à jour</returns>
    /// <exception cref="UnauthorizedAccessException">Token invalide, trop ancien (>7 jours) ou utilisateur inexistant</exception>
    /// <exception cref="KeyNotFoundException">Utilisateur associé au token non trouvé</exception>
    /// <remarks>
    /// Conditions de rafraîchissement :
    /// - Le token ne doit pas avoir plus de 7 jours depuis son émission initiale
    /// - La signature doit être valide même si le token est expiré
    /// - L'utilisateur doit toujours exister dans la base de données
    /// Le nouveau token contient les données utilisateur à jour (rôle, permissions)
    /// </remarks>
    Task<UserLoginResponseDto> RefreshTokenAsync(string token);

    /// <summary>
    /// Récupère le profil de l'utilisateur actuellement authentifié
    /// </summary>
    /// <param name="userEmail">Adresse email de l'utilisateur connecté</param>
    /// <returns>Profil utilisateur complet ou null si non trouvé</returns>
    /// <remarks>
    /// Utilisé pour obtenir les informations du profil de l'utilisateur connecté.
    /// L'email provient généralement du claim JWT dans le contexte HTTP.
    /// </remarks>
    Task<UserDto?> GetCurrentUserAsync(string userEmail);

    /// <summary>
    /// Récupère tous les utilisateurs sans pagination
    /// </summary>
    /// <returns>Collection de tous les utilisateurs</returns>
    /// <remarks>
    /// OBSOLÈTE : Cette méthode peut causer des problèmes de performance et de mémoire.
    /// Utilisez GetAllUsersAsync(PaginationParams) à la place.
    /// </remarks>
    Task<IEnumerable<UserDto>> GetAllUsersAsync();

    /// <summary>
    /// Récupère tous les utilisateurs avec pagination
    /// </summary>
    /// <param name="pagination">Paramètres de pagination (numéro de page, taille de page)</param>
    /// <returns>Résultat paginé contenant les utilisateurs et métadonnées de pagination</returns>
    /// <remarks>
    /// Méthode recommandée pour récupérer les utilisateurs.
    /// Pagination par défaut : 10 éléments par page
    /// Pagination maximale : 100 éléments par page
    /// </remarks>
    Task<PagedResult<UserDto>> GetAllUsersAsync(PaginationParams pagination);

    /// <summary>
    /// Récupère un utilisateur par son identifiant
    /// </summary>
    /// <param name="id">Identifiant unique de l'utilisateur</param>
    /// <returns>Profil utilisateur ou null si non trouvé</returns>
    /// <remarks>
    /// Utilisé pour consulter le profil d'un utilisateur spécifique.
    /// Retourne null si l'utilisateur n'existe pas.
    /// </remarks>
    Task<UserDto?> GetUserByIdAsync(int id);

    /// <summary>
    /// Récupère la liste de tous les membres ESN
    /// </summary>
    /// <returns>Collection des utilisateurs ayant le type "esn_member"</returns>
    /// <remarks>
    /// Filtre les utilisateurs dont le StudentType est "esn_member".
    /// Utilisé pour afficher la liste des membres de l'association.
    /// </remarks>
    Task<IEnumerable<UserDto>> GetEsnMembersAsync();

    /// <summary>
    /// Crée un nouveau compte utilisateur
    /// </summary>
    /// <param name="createDto">Données de création incluant email, mot de passe et informations personnelles</param>
    /// <returns>Profil de l'utilisateur créé</returns>
    /// <exception cref="InvalidOperationException">Un utilisateur avec cet email existe déjà</exception>
    /// <remarks>
    /// Processus de création :
    /// - Vérification de l'unicité de l'email
    /// - Hashage sécurisé du mot de passe avec PBKDF2
    /// - Attribution du rôle par défaut
    /// Rate limiting : 3 créations par heure par IP
    /// </remarks>
    Task<UserDto> CreateUserAsync(UserCreateDto createDto);

    /// <summary>
    /// Change le mot de passe d'un utilisateur
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur</param>
    /// <param name="passwordDto">Ancien et nouveau mot de passe</param>
    /// <returns>Profil utilisateur mis à jour ou null si non trouvé</returns>
    /// <exception cref="UnauthorizedAccessException">L'ancien mot de passe est incorrect</exception>
    /// <remarks>
    /// Sécurité :
    /// - Vérification obligatoire de l'ancien mot de passe
    /// - Nouveau mot de passe hashé avec PBKDF2
    /// - L'utilisateur doit être propriétaire du compte ou Admin
    /// </remarks>
    Task<UserDto?> UpdatePasswordAsync(int id, UserPasswordChangeDto passwordDto);

    /// <summary>
    /// Met à jour les informations d'un utilisateur
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur</param>
    /// <param name="userDto">Nouvelles informations personnelles</param>
    /// <returns>Profil utilisateur mis à jour ou null si non trouvé</returns>
    /// <remarks>
    /// Permet de modifier :
    /// - Prénom, nom
    /// - Date de naissance
    /// - Type d'étudiant
    /// - Numéro ESN Card
    /// - Université et pass transport
    /// Autorisation requise : propriétaire du compte OU rôle Admin
    /// </remarks>
    Task<UserDto?> UpdateUserAsync(int id, UserUpdateDto userDto);

    /// <summary>
    /// Récupère les utilisateurs par statut
    /// </summary>
    /// <param name="status">Statut à filtrer (Pending, Approved, Rejected)</param>
    /// <returns>Liste des utilisateurs ayant ce statut</returns>
    /// <remarks>
    /// Utilisé principalement pour afficher les utilisateurs en attente de validation.
    /// Autorisation requise : rôle Admin uniquement
    /// </remarks>
    Task<IEnumerable<UserDto>> GetUsersByStatusAsync(UserStatus status);

    /// <summary>
    /// Approuve un utilisateur en attente de validation
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur à approuver</param>
    /// <exception cref="KeyNotFoundException">Utilisateur non trouvé</exception>
    /// <remarks>
    /// Change le statut de l'utilisateur à Approved.
    /// L'utilisateur pourra désormais se connecter.
    /// Autorisation requise : rôle Admin uniquement
    /// Log : Information avec ID admin et ID utilisateur
    /// </remarks>
    Task ApproveUserAsync(int userId);

    /// <summary>
    /// Refuse un utilisateur avec une raison optionnelle
    /// </summary>
    /// <param name="userId">Identifiant de l'utilisateur à refuser</param>
    /// <param name="reason">Raison du refus (optionnel, max 500 caractères)</param>
    /// <exception cref="KeyNotFoundException">Utilisateur non trouvé</exception>
    /// <remarks>
    /// Change le statut de l'utilisateur à Rejected.
    /// L'utilisateur ne pourra pas se connecter.
    /// Autorisation requise : rôle Admin uniquement
    /// Log : Information avec ID admin, ID utilisateur et raison
    /// </remarks>
    Task RejectUserAsync(int userId, string? reason = null);

    /// <summary>
    /// Supprime définitivement un compte utilisateur
    /// </summary>
    /// <param name="id">Identifiant de l'utilisateur à supprimer</param>
    /// <returns>Profil de l'utilisateur supprimé ou null si non trouvé</returns>
    /// <remarks>
    /// Suppression en cascade :
    /// - Toutes les inscriptions aux événements
    /// - Tous les votes sur propositions
    /// - Toutes les propositions créées
    /// - Tous les événements créés
    /// Autorisation requise : rôle Admin uniquement
    /// </remarks>
    Task<UserDto?> DeleteUserAsync(int id);
}
