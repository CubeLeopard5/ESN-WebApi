using Bo.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dto.Proposition;

/// <summary>
/// Paramètres de filtrage pour les propositions (administration)
/// </summary>
public class PropositionFilterDto
{
    /// <summary>
    /// Filtre par statut de suppression
    /// </summary>
    /// <remarks>
    /// All = toutes les propositions,
    /// Active = uniquement les actives,
    /// Deleted = uniquement les supprimées
    /// </remarks>
    [EnumDataType(typeof(DeletedStatus))]
    public DeletedStatus Status { get; set; } = DeletedStatus.Active;
}
