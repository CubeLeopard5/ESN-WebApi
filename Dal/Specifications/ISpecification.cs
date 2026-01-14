using System.Linq.Expressions;

namespace Dal.Specifications;

/// <summary>
/// Interface pour le pattern Specification
/// Encapsule la logique de requête de manière réutilisable
/// </summary>
/// <typeparam name="T">Type d'entité</typeparam>
public interface ISpecification<T>
{
    /// <summary>
    /// Critère de filtre principal
    /// </summary>
    Expression<Func<T, bool>>? Criteria { get; }

    /// <summary>
    /// Includes pour le chargement eager
    /// </summary>
    List<Expression<Func<T, object>>> Includes { get; }

    /// <summary>
    /// Includes sous forme de chaînes (pour ThenInclude)
    /// </summary>
    List<string> IncludeStrings { get; }

    /// <summary>
    /// Expression de tri
    /// </summary>
    Expression<Func<T, object>>? OrderBy { get; }

    /// <summary>
    /// Expression de tri descendant
    /// </summary>
    Expression<Func<T, object>>? OrderByDescending { get; }

    /// <summary>
    /// Nombre d'éléments à ignorer (pagination)
    /// </summary>
    int Skip { get; }

    /// <summary>
    /// Nombre d'éléments à prendre (pagination)
    /// </summary>
    int Take { get; }

    /// <summary>
    /// Active la pagination
    /// </summary>
    bool IsPagingEnabled { get; }
}
