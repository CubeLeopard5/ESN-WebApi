using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository Passkey
/// </summary>
public class PasskeyRepository(EsnDevContext context)
    : Repository<UserPasskeyBo>(context), IPasskeyRepository
{
    /// <inheritdoc />
    public async Task<IEnumerable<UserPasskeyBo>> GetByUserIdAsync(int userId)
    {
        return await _dbSet
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .AsNoTracking()
            .ToListAsync();
    }

    /// <inheritdoc />
    public async Task<UserPasskeyBo?> GetByCredentialIdAsync(string credentialId)
    {
        return await _dbSet
            .Include(p => p.User)
                .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(p => p.CredentialId == credentialId);
    }

    /// <inheritdoc />
    public async Task<bool> CredentialIdExistsAsync(string credentialId)
    {
        return await _dbSet.AnyAsync(p => p.CredentialId == credentialId);
    }
}
