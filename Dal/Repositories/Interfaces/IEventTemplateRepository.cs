using Bo.Models;

namespace Dal.Repositories.Interfaces;

/// <summary>
/// Interface pour le repository EventTemplate avec méthodes spécifiques
/// </summary>
public interface IEventTemplateRepository : IRepository<EventTemplateBo>
{
    /// <summary>
    /// Récupère tous les templates (sans includes - simple)
    /// </summary>
    Task<IEnumerable<EventTemplateBo>> GetAllTemplatesAsync();

    /// <summary>
    /// Récupère les templates paginés (sans includes - simple)
    /// </summary>
    Task<(List<EventTemplateBo> Items, int TotalCount)> GetPagedAsync(int skip, int take);
}
