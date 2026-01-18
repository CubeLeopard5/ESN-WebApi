using Bo.Models;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository EventTemplate
/// </summary>
public class EventTemplateRepository(EsnDevContext context)
    : Repository<EventTemplateBo>(context), Interfaces.IEventTemplateRepository
{
    public async Task<IEnumerable<EventTemplateBo>> GetAllTemplatesWithUserAsync()
    {
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<EventTemplateBo>> GetAllTemplatesAsync()
    {
        return await GetAllTemplatesWithUserAsync();
    }

    public async Task<EventTemplateBo?> GetTemplateWithUserAsync(int templateId)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(et => et.Id == templateId);
    }

    public async Task<IEnumerable<EventTemplateBo>> GetTemplatesByUserEmailAsync(string userEmail)
    {
        // EventTemplate n'a pas de navigation User, retourner tous les templates
        return await _dbSet.AsNoTracking().ToListAsync();
    }

    public async Task<(List<EventTemplateBo> Items, int TotalCount)> GetPagedAsync(int skip, int take)
    {
        var totalCount = await _dbSet.CountAsync();

        var items = await _dbSet
            .OrderByDescending(et => et.Id)
            .Skip(skip)
            .Take(take)
            .AsNoTracking()
            .ToListAsync();

        return (items, totalCount);
    }
}
