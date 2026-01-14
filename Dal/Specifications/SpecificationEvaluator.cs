using Microsoft.EntityFrameworkCore;

namespace Dal.Specifications;

/// <summary>
/// Évaluateur de spécifications
/// Applique les spécifications aux requêtes IQueryable
/// </summary>
public static class SpecificationEvaluator<T> where T : class
{
    /// <summary>
    /// Applique une spécification à une requête
    /// </summary>
    public static IQueryable<T> GetQuery(IQueryable<T> inputQuery, ISpecification<T> specification)
    {
        var query = inputQuery;

        // Applique le critère de filtre
        if (specification.Criteria != null)
        {
            query = query.Where(specification.Criteria);
        }

        // Applique les includes
        query = specification.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Applique les includes sous forme de chaînes
        query = specification.IncludeStrings.Aggregate(query, (current, include) => current.Include(include));

        // Applique le tri
        if (specification.OrderBy != null)
        {
            query = query.OrderBy(specification.OrderBy);
        }
        else if (specification.OrderByDescending != null)
        {
            query = query.OrderByDescending(specification.OrderByDescending);
        }

        // Applique la pagination
        if (specification.IsPagingEnabled)
        {
            query = query.Skip(specification.Skip).Take(specification.Take);
        }

        return query;
    }
}
