using Bo.Models;
using Dal.Repositories.Interfaces;
using Bo.Constants;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository User
/// </summary>
public class UserRepository(EsnDevContext context)
    : Repository<UserBo>(context), IUserRepository
{
    public async Task<UserBo?> GetByEmailAsync(string email)
    {
        return await _dbSet
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserBo?> GetByEmailWithRoleAsync(string email)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<UserBo?> GetUserWithRoleAsync(int userId)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == userId);
    }

    public async Task<IEnumerable<UserBo>> GetEsnMembersAsync()
    {
        return await _dbSet
            .Where(u => u.StudentType == StudentType.EsnMember)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }

    public async Task<(List<UserBo> Items, int TotalCount)> GetPagedAsync(int skip, int take)
    {
        var totalCount = await _dbSet.CountAsync();

        var items = await _dbSet
            .Include(u => u.Role)
            .OrderByDescending(u => u.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        return (items, totalCount);
    }
}
