namespace Dto.Common;

/// <summary>
/// Résultat paginé générique
/// </summary>
/// <typeparam name="T">Type des éléments</typeparam>
public class PagedResult<T>
{
    /// <summary>
    /// Liste des éléments pour la page courante
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Numéro de page actuel (commence à 1)
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Taille de page
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Nombre total d'éléments
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Nombre total de pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Indique s'il y a une page précédente
    /// </summary>
    public bool HasPrevious => PageNumber > 1;

    /// <summary>
    /// Indique s'il y a une page suivante
    /// </summary>
    public bool HasNext => PageNumber < TotalPages;

    /// <summary>
    /// Constructeur
    /// </summary>
    public PagedResult(List<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageNumber = pageNumber;
        PageSize = pageSize;
    }

    /// <summary>
    /// Constructeur vide
    /// </summary>
    public PagedResult()
    {
    }
}
