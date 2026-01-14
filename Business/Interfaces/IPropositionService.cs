using Bo.Enums;
using Dto;
using Dto.Common;
using Dto.Proposition;

namespace Business.Interfaces;

/// <summary>
/// Interface de gestion des propositions d'activités et système de vote
/// </summary>
public interface IPropositionService
{
    /// <summary>
    /// Récupère toutes les propositions actives sans pagination
    /// </summary>
    /// <returns>Collection de toutes les propositions non supprimées</returns>
    /// <remarks>
    /// OBSOLÈTE : Cette méthode peut causer des problèmes de performance et de mémoire.
    /// Utilisez GetAllPropositionsAsync(PaginationParams) à la place.
    /// Exclut automatiquement les propositions supprimées (soft delete).
    /// </remarks>
    Task<IEnumerable<PropositionDto>> GetAllPropositionsAsync();

    /// <summary>
    /// Récupère toutes les propositions actives avec pagination
    /// </summary>
    /// <param name="pagination">Paramètres de pagination (numéro de page, taille de page)</param>
    /// <param name="userEmail">Email de l'utilisateur (optionnel, pour inclure le vote personnel)</param>
    /// <returns>Résultat paginé contenant les propositions et métadonnées de pagination</returns>
    /// <remarks>
    /// Méthode recommandée pour récupérer les propositions.
    /// Pagination par défaut : 10 éléments par page
    /// Pagination maximale : 100 éléments par page
    /// Exclut automatiquement les propositions supprimées (soft delete).
    /// Si userEmail est fourni, le champ UserVoteType sera rempli avec le vote de l'utilisateur.
    /// </remarks>
    Task<PagedResult<PropositionDto>> GetAllPropositionsAsync(PaginationParams pagination, string? userEmail = null);

    /// <summary>
    /// Récupère une proposition par son identifiant
    /// </summary>
    /// <param name="id">Identifiant unique de la proposition</param>
    /// <param name="userEmail">Email de l'utilisateur (optionnel, pour inclure le vote personnel)</param>
    /// <returns>Proposition complète avec compteurs de votes ou null si non trouvée</returns>
    /// <remarks>
    /// Inclut les compteurs VotesUp et VotesDown dénormalisés pour la performance.
    /// Si userEmail est fourni, le champ UserVoteType sera rempli avec le vote de l'utilisateur.
    /// </remarks>
    Task<PropositionDto?> GetPropositionByIdAsync(int id, string? userEmail = null);

    /// <summary>
    /// Crée une nouvelle proposition d'activité
    /// </summary>
    /// <param name="propositionDto">Données de la proposition (titre, description)</param>
    /// <param name="userEmail">Email de l'utilisateur créateur</param>
    /// <returns>Proposition créée avec compteurs initialisés à 0</returns>
    /// <remarks>
    /// L'utilisateur créateur devient automatiquement l'auteur de la proposition.
    /// Les compteurs VotesUp et VotesDown sont initialisés à 0.
    /// </remarks>
    Task<PropositionDto> CreatePropositionAsync(PropositionDto propositionDto, string userEmail);

    /// <summary>
    /// Met à jour une proposition existante
    /// </summary>
    /// <param name="id">Identifiant de la proposition à modifier</param>
    /// <param name="propositionDto">Nouvelles données de la proposition</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la modification</param>
    /// <returns>Proposition mise à jour ou null si non trouvée</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est pas l'auteur de la proposition</exception>
    /// <remarks>
    /// Seul l'auteur de la proposition peut la modifier.
    /// Met à jour la date de modification.
    /// </remarks>
    Task<PropositionDto?> UpdatePropositionAsync(int id, PropositionDto propositionDto, string userEmail);

    /// <summary>
    /// Supprime une proposition (soft delete)
    /// </summary>
    /// <param name="id">Identifiant de la proposition à supprimer</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la suppression</param>
    /// <returns>Proposition supprimée ou null si non trouvée</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est pas l'auteur de la proposition</exception>
    /// <remarks>
    /// Seul l'auteur de la proposition peut la supprimer.
    /// Effectue un soft delete pour préservation historique.
    /// La proposition n'apparaîtra plus dans les listes mais reste en base.
    /// </remarks>
    Task<PropositionDto?> DeletePropositionAsync(int id, string userEmail);

    /// <summary>
    /// Vote positivement pour une proposition
    /// </summary>
    /// <param name="id">Identifiant de la proposition</param>
    /// <param name="userEmail">Email de l'utilisateur votant</param>
    /// <returns>Proposition mise à jour avec nouveaux compteurs de votes</returns>
    /// <exception cref="KeyNotFoundException">Proposition ou utilisateur non trouvé</exception>
    /// <remarks>
    /// Comportement du vote :
    /// - Si pas encore voté : ajoute un vote Up
    /// - Si déjà voté Up : pas de changement
    /// - Si déjà voté Down : change le vote en Up
    /// Rate limiting : 30 votes par minute par IP
    /// Recalcul automatique des compteurs VotesUp et VotesDown
    /// </remarks>
    Task<PropositionDto?> VoteUpAsync(int id, string userEmail);

    /// <summary>
    /// Vote négativement pour une proposition
    /// </summary>
    /// <param name="id">Identifiant de la proposition</param>
    /// <param name="userEmail">Email de l'utilisateur votant</param>
    /// <returns>Proposition mise à jour avec nouveaux compteurs de votes</returns>
    /// <exception cref="KeyNotFoundException">Proposition ou utilisateur non trouvé</exception>
    /// <remarks>
    /// Comportement du vote :
    /// - Si pas encore voté : ajoute un vote Down
    /// - Si déjà voté Down : pas de changement
    /// - Si déjà voté Up : change le vote en Down
    /// Rate limiting : 30 votes par minute par IP
    /// Recalcul automatique des compteurs VotesUp et VotesDown
    /// </remarks>
    Task<PropositionDto?> VoteDownAsync(int id, string userEmail);

    /// <summary>
    /// Récupère toutes les propositions avec filtre pour l'administration
    /// </summary>
    /// <param name="pagination">Paramètres de pagination (numéro de page, taille de page)</param>
    /// <param name="filter">Paramètres de filtrage (statut de suppression)</param>
    /// <param name="userEmail">Email de l'utilisateur (optionnel, pour inclure le vote personnel)</param>
    /// <returns>Résultat paginé avec filtre appliqué</returns>
    /// <remarks>
    /// Permet de récupérer les propositions selon leur statut de suppression :
    /// - Active : uniquement les propositions non supprimées (IsDeleted = false)
    /// - Deleted : uniquement les propositions supprimées (IsDeleted = true)
    /// - All : toutes les propositions (actives et supprimées)
    /// Accessible uniquement aux membres ESN et administrateurs.
    /// Si userEmail est fourni, le champ UserVoteType sera rempli avec le vote de l'utilisateur.
    /// </remarks>
    Task<PagedResult<PropositionDto>> GetAllPropositionsForAdminAsync(
        PaginationParams pagination,
        PropositionFilterDto filter,
        string? userEmail = null);

    /// <summary>
    /// Supprime une proposition en tant qu'administrateur/ESN member (soft delete)
    /// </summary>
    /// <param name="id">Identifiant de la proposition à supprimer</param>
    /// <param name="userEmail">Email de l'utilisateur effectuant la suppression</param>
    /// <returns>Proposition supprimée ou null si non trouvée</returns>
    /// <exception cref="UnauthorizedAccessException">L'utilisateur n'est ni membre ESN ni administrateur</exception>
    /// <remarks>
    /// Accessible uniquement aux membres ESN (StudentType = "esn_member") et administrateurs.
    /// Effectue un soft delete pour préservation historique.
    /// La proposition n'apparaîtra plus dans les listes publiques mais reste en base.
    /// Peut supprimer n'importe quelle proposition, même celles d'autres utilisateurs.
    /// </remarks>
    Task<PropositionDto?> DeletePropositionAsAdminAsync(int id, string userEmail);
}
