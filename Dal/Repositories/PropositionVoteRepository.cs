using Bo.Constants;
using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository pour les votes sur les propositions
/// </summary>
public class PropositionVoteRepository(EsnDevContext context) : Repository<PropositionVoteBo>(context), IPropositionVoteRepository
{
    public async Task<IEnumerable<PropositionVoteBo>> GetByPropositionIdAsync(int propositionId)
    {
        return await context.PropositionVotes
            .Where(pv => pv.PropositionId == propositionId)
            .Include(pv => pv.User)
            .ToListAsync();
    }

    public async Task<IEnumerable<PropositionVoteBo>> GetByUserIdAsync(int userId)
    {
        return await context.PropositionVotes
            .Where(pv => pv.UserId == userId)
            .Include(pv => pv.Proposition)
            .ToListAsync();
    }

    public async Task<PropositionVoteBo?> GetByPropositionAndUserAsync(int propositionId, int userId)
    {
        return await context.PropositionVotes
            .FirstOrDefaultAsync(pv => pv.PropositionId == propositionId && pv.UserId == userId);
    }

    public async Task<int> CountUpVotesAsync(int propositionId)
    {
        return await context.PropositionVotes
            .CountAsync(pv => pv.PropositionId == propositionId && pv.VoteType == VoteType.Up);
    }

    public async Task<int> CountDownVotesAsync(int propositionId)
    {
        return await context.PropositionVotes
            .CountAsync(pv => pv.PropositionId == propositionId && pv.VoteType == VoteType.Down);
    }

    public async Task<IEnumerable<PropositionVoteBo>> GetUserVotesForPropositionsAsync(int userId, List<int> propositionIds)
    {
        return await context.PropositionVotes
            .Where(pv => pv.UserId == userId && propositionIds.Contains(pv.PropositionId))
            .AsNoTracking()
            .ToListAsync();
    }
}
