using System.Linq.Expressions;
using Dal.Repositories.Interfaces;
using Dal.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation générique du pattern Repository
/// </summary>
/// <typeparam name="T">Type d'entité</typeparam>
public class Repository<T>(EsnDevContext context) : IRepository<T>
    where T : class
{
    protected readonly EsnDevContext _context = context;
    protected readonly DbSet<T> _dbSet = context.Set<T>();

    public virtual async Task<T?> GetByIdAsync(int id)
    {
        return await _dbSet.FindAsync(id);
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }

    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.FirstOrDefaultAsync(predicate);
    }

    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        return predicate == null
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }

    public virtual async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate)
    {
        return await _dbSet.AnyAsync(predicate);
    }

    public virtual async Task<IEnumerable<T>> FindWithSpecificationAsync(ISpecification<T> specification)
    {
        var query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.ToListAsync();
    }

    public virtual async Task<T?> FirstOrDefaultWithSpecificationAsync(ISpecification<T> specification)
    {
        var query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.FirstOrDefaultAsync();
    }

    public virtual async Task<int> CountWithSpecificationAsync(ISpecification<T> specification)
    {
        var query = SpecificationEvaluator<T>.GetQuery(_dbSet.AsQueryable(), specification);
        return await query.CountAsync();
    }

    public virtual async Task<T> AddAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        await _dbSet.AddRangeAsync(entities);
    }

    public virtual void Update(T entity)
    {
        _dbSet.Update(entity);
    }

    public virtual void UpdateRange(IEnumerable<T> entities)
    {
        _dbSet.UpdateRange(entities);
    }

    public virtual void Delete(T entity)
    {
        _dbSet.Remove(entity);
    }

    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        _dbSet.RemoveRange(entities);
    }
}
