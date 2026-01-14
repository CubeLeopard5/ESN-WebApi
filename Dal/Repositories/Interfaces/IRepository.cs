using System.Linq.Expressions;
using Dal.Specifications;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface générique pour le pattern Repository
/// </summary>
/// <typeparam name="T">Type d'entité</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Récupère une entité par son ID
    /// </summary>
    Task<T?> GetByIdAsync(int id);

    /// <summary>
    /// Récupère toutes les entités
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Recherche des entités selon un prédicat
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Récupère la première entité correspondant au prédicat
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Compte le nombre d'entités correspondant au prédicat
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Vérifie si une entité existe selon le prédicat
    /// </summary>
    Task<bool> AnyAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Récupère des entités selon une spécification
    /// </summary>
    Task<IEnumerable<T>> FindWithSpecificationAsync(ISpecification<T> specification);

    /// <summary>
    /// Récupère la première entité correspondant à une spécification
    /// </summary>
    Task<T?> FirstOrDefaultWithSpecificationAsync(ISpecification<T> specification);

    /// <summary>
    /// Compte le nombre d'entités correspondant à une spécification
    /// </summary>
    Task<int> CountWithSpecificationAsync(ISpecification<T> specification);

    /// <summary>
    /// Ajoute une nouvelle entité
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Ajoute plusieurs entités
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Met à jour une entité
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Met à jour plusieurs entités
    /// </summary>
    void UpdateRange(IEnumerable<T> entities);

    /// <summary>
    /// Supprime une entité
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Supprime plusieurs entités
    /// </summary>
    void DeleteRange(IEnumerable<T> entities);
}
