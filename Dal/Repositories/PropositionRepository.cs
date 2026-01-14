using Bo.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository Proposition
/// </summary>
public class PropositionRepository(EsnDevContext context)
    : Repository<PropositionBo>(context), Interfaces.IPropositionRepository
{
    public async Task<IEnumerable<PropositionBo>> GetActivePropositionsWithDetailsAsync()
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .Include(p => p.User)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<IEnumerable<PropositionBo>> GetAllPropositionsWithDetailsAsync()
    {
        return await GetActivePropositionsWithDetailsAsync();
    }

    public async Task<PropositionBo?> GetActivePropositionWithDetailsAsync(int propositionId)
    {
        return await _dbSet
            .Where(p => !p.IsDeleted)
            .Include(p => p.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propositionId);
    }

    public async Task<PropositionBo?> GetPropositionWithDetailsAsync(int propositionId)
    {
        return await GetActivePropositionWithDetailsAsync(propositionId);
    }

    public Task SoftDeleteAsync(PropositionBo proposition)
    {
        proposition.IsDeleted = true;
        proposition.DeletedAt = DateTime.UtcNow;
        Update(proposition);
        return Task.CompletedTask;
    }

    public async Task<(List<PropositionBo> Items, int TotalCount)> GetPagedAsync(int skip, int take)
    {
        var totalCount = await _dbSet.Where(p => !p.IsDeleted).CountAsync();

        var items = await _dbSet
            .Where(p => !p.IsDeleted)
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }

    /// <inheritdoc />
    public async Task<(List<PropositionBo> Items, int TotalCount)> GetPagedWithFilterAsync(int skip, int take, Bo.Enums.DeletedStatus deletedStatus)
    {
        var query = _dbSet.AsQueryable();

        // Apply filter based on status
        query = deletedStatus switch
        {
            Bo.Enums.DeletedStatus.Active => query.Where(p => !p.IsDeleted),
            Bo.Enums.DeletedStatus.Deleted => query.Where(p => p.IsDeleted),
            Bo.Enums.DeletedStatus.All => query, // No filter
            _ => query.Where(p => !p.IsDeleted) // Default to Active
        };

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(p => p.User)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }
}
