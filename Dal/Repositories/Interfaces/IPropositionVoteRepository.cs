using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Repository spécialisé pour les votes sur les propositions
/// </summary>
public interface IPropositionVoteRepository : IRepository<PropositionVoteBo>
{
    /// <summary>
    /// Récupère tous les votes pour une proposition donnée
    /// </summary>
    Task<IEnumerable<PropositionVoteBo>> GetByPropositionIdAsync(int propositionId);

    /// <summary>
    /// Récupère tous les votes d'un utilisateur
    /// </summary>
    Task<IEnumerable<PropositionVoteBo>> GetByUserIdAsync(int userId);

    /// <summary>
    /// Récupère le vote d'un utilisateur pour une proposition spécifique
    /// </summary>
    Task<PropositionVoteBo?> GetByPropositionAndUserAsync(int propositionId, int userId);

    /// <summary>
    /// Compte le nombre de votes positifs pour une proposition
    /// </summary>
    Task<int> CountUpVotesAsync(int propositionId);

    /// <summary>
    /// Compte le nombre de votes négatifs pour une proposition
    /// </summary>
    Task<int> CountDownVotesAsync(int propositionId);

    /// <summary>
    /// Récupère les votes d'un utilisateur pour une liste de propositions
    /// </summary>
    /// <param name="userId">ID de l'utilisateur</param>
    /// <param name="propositionIds">Liste des IDs de propositions</param>
    /// <returns>Collection des votes de l'utilisateur pour ces propositions</returns>
    Task<IEnumerable<PropositionVoteBo>> GetUserVotesForPropositionsAsync(int userId, List<int> propositionIds);
}
