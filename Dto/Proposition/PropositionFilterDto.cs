using Bo.Enums;
using System.ComponentModel.DataAnnotations;

namespace Dto.Proposition;

/// <summary>
/// Paramètres de filtrage pour les propositions (administration)
/// </summary>
public class PropositionFilterDto
{
    /// <summary>
    /// Filtre par statut (Active = non archivées, Deleted = archivées, All = toutes)
    /// </summary>
    [EnumDataType(typeof(DeletedStatus))]
    public DeletedStatus Status { get; set; } = DeletedStatus.Active;
}
