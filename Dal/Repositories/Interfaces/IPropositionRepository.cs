using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface pour le repository Proposition avec méthodes spécifiques
/// </summary>
public interface IPropositionRepository : IRepository<PropositionBo>
{
    /// <summary>
    /// Récupère toutes les propositions non supprimées avec User
    /// </summary>
    Task<IEnumerable<PropositionBo>> GetActivePropositionsWithDetailsAsync();

    /// <summary>
    /// Alias pour GetActivePropositionsWithDetailsAsync
    /// </summary>
    Task<IEnumerable<PropositionBo>> GetAllPropositionsWithDetailsAsync();

    /// <summary>
    /// Récupère une proposition non supprimée par ID avec User
    /// </summary>
    Task<PropositionBo?> GetActivePropositionWithDetailsAsync(int propositionId);

    /// <summary>
    /// Alias pour GetActivePropositionWithDetailsAsync
    /// </summary>
    Task<PropositionBo?> GetPropositionWithDetailsAsync(int propositionId);

    /// <summary>
    /// Soft delete d'une proposition
    /// </summary>
    Task SoftDeleteAsync(PropositionBo proposition);

    /// <summary>
    /// Récupère les propositions non supprimées paginées avec User
    /// </summary>
    Task<(List<PropositionBo> Items, int TotalCount)> GetPagedAsync(int skip, int take);

    /// <summary>
    /// Récupère les propositions paginées avec filtre sur le statut de suppression
    /// </summary>
    /// <param name="skip">Nombre d'éléments à sauter</param>
    /// <param name="take">Nombre d'éléments à prendre</param>
    /// <param name="deletedStatus">Filtre sur le statut de suppression (All, Active, Deleted)</param>
    /// <returns>Tuple contenant la liste de propositions avec User et le nombre total</returns>
    /// <remarks>
    /// Permet de récupérer les propositions selon leur statut de suppression :
    /// - Active : uniquement les propositions non supprimées (IsDeleted = false)
    /// - Deleted : uniquement les propositions supprimées (IsDeleted = true)
    /// - All : toutes les propositions
    /// </remarks>
    Task<(List<PropositionBo> Items, int TotalCount)> GetPagedWithFilterAsync(int skip, int take, Bo.Enums.DeletedStatus deletedStatus);
}
