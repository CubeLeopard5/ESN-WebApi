using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface pour le repository User avec méthodes spécifiques
/// </summary>
public interface IUserRepository : IRepository<UserBo>
{
    /// <summary>
    /// Récupère un utilisateur par son email (sans includes)
    /// </summary>
    Task<UserBo?> GetByEmailAsync(string email);

    /// <summary>
    /// Récupère un utilisateur par son email avec son rôle
    /// </summary>
    Task<UserBo?> GetByEmailWithRoleAsync(string email);

    /// <summary>
    /// Récupère un utilisateur avec son rôle
    /// </summary>
    Task<UserBo?> GetUserWithRoleAsync(int userId);

    /// <summary>
    /// Récupère tous les membres ESN (sans includes)
    /// </summary>
    Task<IEnumerable<UserBo>> GetEsnMembersAsync();

    /// <summary>
    /// Vérifie si un email existe déjà
    /// </summary>
    Task<bool> EmailExistsAsync(string email);

    /// <summary>
    /// Récupère les utilisateurs paginés avec rôle
    /// </summary>
    Task<(List<UserBo> Items, int TotalCount)> GetPagedAsync(int skip, int take);
}
