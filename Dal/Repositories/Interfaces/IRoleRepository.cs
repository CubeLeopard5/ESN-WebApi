using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Repository spécialisé pour les rôles utilisateur
/// </summary>
public interface IRoleRepository : IRepository<RoleBo>
{
    /// <summary>
    /// Récupère un rôle par son nom
    /// </summary>
    Task<RoleBo?> GetByNameAsync(string name);

    /// <summary>
    /// Récupère tous les rôles actifs
    /// </summary>
    Task<IEnumerable<RoleBo>> GetActiveRolesAsync();

    /// <summary>
    /// Vérifie si un rôle existe par son nom
    /// </summary>
    Task<bool> ExistsByNameAsync(string name);
}
