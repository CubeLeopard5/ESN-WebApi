namespace Dto.Common;

/// <summary>
/// Paramètres de pagination pour les requêtes
/// </summary>
public class PaginationParams
{
    private const int MaxPageSize = 100;
    private int _pageSize = 20;

    /// <summary>
    /// Numéro de page (commence à 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Taille de page (max 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : value;
    }

    /// <summary>
    /// Champ de tri (ex: "createdAt", "votesUp", "score")
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Ordre de tri ("asc" ou "desc", défaut: "desc")
    /// </summary>
    public string? SortOrder { get; set; } = "desc";

    /// <summary>
    /// Calcule le nombre d'éléments à sauter
    /// </summary>
    public int Skip => (PageNumber - 1) * PageSize;
}
