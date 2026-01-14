namespace Bo.Enums;

/// <summary>
/// Filtre pour le statut de suppression des propositions
/// </summary>
public enum DeletedStatus
{
    /// <summary>
    /// Toutes les propositions (supprimées et actives)
    /// </summary>
    All = 0,

    /// <summary>
    /// Uniquement les propositions actives (non supprimées)
    /// </summary>
    Active = 1,

    /// <summary>
    /// Uniquement les propositions supprimées
    /// </summary>
    Deleted = 2
}
