using Bo.Models;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Dal.Repositories;

/// <summary>
/// Implémentation du repository pour les rôles utilisateur
/// </summary>
public class RoleRepository(EsnDevContext context) : Repository<RoleBo>(context), IRoleRepository
{
    public async Task<RoleBo?> GetByNameAsync(string name)
    {
        return await context.Roles
            .FirstOrDefaultAsync(r => r.Name == name);
    }

    public async Task<IEnumerable<RoleBo>> GetActiveRolesAsync()
    {
        // Tous les rôles sont considérés comme actifs dans le modèle actuel
        // Cette méthode peut être étendue si un champ IsActive est ajouté
        return await context.Roles.ToListAsync();
    }

    public async Task<bool> ExistsByNameAsync(string name)
    {
        return await context.Roles
            .AnyAsync(r => r.Name == name);
    }
}
