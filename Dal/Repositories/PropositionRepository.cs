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

    /// <inheritdoc />
    public async Task<(List<PropositionBo> Items, int TotalCount)> GetPagedAsync(int skip, int take, string? sortBy = null, string? sortOrder = "desc")
    {
        var totalCount = await _dbSet.Where(p => !p.IsDeleted).CountAsync();

        var query = _dbSet
            .Where(p => !p.IsDeleted)
            .Include(p => p.User);

        var orderedQuery = ApplySorting(query, sortBy, sortOrder);

        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }

    /// <summary>
    /// Applique le tri dynamique sur la requête
    /// </summary>
    private static IOrderedQueryable<PropositionBo> ApplySorting(IQueryable<PropositionBo> query, string? sortBy, string? sortOrder)
    {
        var isDescending = string.Equals(sortOrder, "desc", StringComparison.OrdinalIgnoreCase);

        return (sortBy?.ToLowerInvariant(), isDescending) switch
        {
            ("votesup", true) => query.OrderByDescending(p => p.VotesUp),
            ("votesup", false) => query.OrderBy(p => p.VotesUp),
            ("score", true) => query.OrderByDescending(p => p.VotesUp - p.VotesDown),
            ("score", false) => query.OrderBy(p => p.VotesUp - p.VotesDown),
            ("createdat", false) => query.OrderBy(p => p.CreatedAt),
            // Default: createdAt DESC (most recent first)
            _ => query.OrderByDescending(p => p.CreatedAt)
        };
    }

    /// <inheritdoc />
    public async Task<(List<PropositionBo> Items, int TotalCount)> GetPagedWithFilterAsync(int skip, int take, Bo.Enums.DeletedStatus deletedStatus, string? sortBy = null, string? sortOrder = "desc")
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

        var queryWithIncludes = query.Include(p => p.User);
        var orderedQuery = ApplySorting(queryWithIncludes, sortBy, sortOrder);

        var items = await orderedQuery
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }
}
