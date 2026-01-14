using Dto.Common;
using Dto.Event;
using Dto.EventTemplate;

namespace Business.Interfaces;

/// <summary>
/// Interface de gestion des templates d'événements réutilisables
/// </summary>
public interface IEventTemplateService
{
    /// <summary>
    /// Récupère tous les templates d'événements sans pagination
    /// </summary>
    /// <returns>Collection de tous les templates</returns>
    /// <remarks>
    /// OBSOLÈTE : Cette méthode peut causer des problèmes de performance et de mémoire.
    /// Utilisez GetAllTemplatesAsync(PaginationParams) à la place.
    /// </remarks>
    Task<IEnumerable<EventTemplateDto>> GetAllTemplatesAsync();

    /// <summary>
    /// Récupère tous les templates d'événements avec pagination
    /// </summary>
    /// <param name="pagination">Paramètres de pagination (numéro de page, taille de page)</param>
    /// <returns>Résultat paginé contenant les templates et métadonnées de pagination</returns>
    /// <remarks>
    /// Méthode recommandée pour récupérer les templates.
    /// Pagination par défaut : 10 éléments par page
    /// Pagination maximale : 100 éléments par page
    /// </remarks>
    Task<PagedResult<EventTemplateDto>> GetAllTemplatesAsync(PaginationParams pagination);

    /// <summary>
    /// Récupère un template par son identifiant
    /// </summary>
    /// <param name="id">Identifiant unique du template</param>
    /// <returns>Template complet ou null si non trouvé</returns>
    /// <remarks>
    /// Inclut toutes les données : titre, description, formulaire SurveyJS pré-configuré
    /// </remarks>
    Task<EventTemplateDto?> GetTemplateByIdAsync(int id);

    /// <summary>
    /// Crée un nouveau template d'événement
    /// </summary>
    /// <param name="createTemplateDto">Données du template (titre, description, formulaire SurveyJS)</param>
    /// <returns>Template créé</returns>
    /// <remarks>
    /// Permet de créer un modèle d'événement réutilisable.
    /// Utile pour les événements récurrents avec le même formulaire d'inscription.
    /// </remarks>
    Task<EventTemplateDto> CreateTemplateAsync(CreateEventTemplateDto createTemplateDto);

    /// <summary>
    /// Met à jour un template existant
    /// </summary>
    /// <param name="id">Identifiant du template à modifier</param>
    /// <param name="templateDto">Nouvelles données du template</param>
    /// <returns>Template mis à jour ou null si non trouvé</returns>
    /// <remarks>
    /// Permet de modifier le titre, la description ou le formulaire SurveyJS.
    /// </remarks>
    Task<EventTemplateDto?> UpdateTemplateAsync(int id, EventTemplateDto templateDto);

    /// <summary>
    /// Supprime définitivement un template
    /// </summary>
    /// <param name="id">Identifiant du template à supprimer</param>
    /// <returns>True si supprimé, false si non trouvé</returns>
    /// <remarks>
    /// Suppression définitive du template.
    /// N'affecte pas les événements déjà créés à partir de ce template.
    /// </remarks>
    Task<bool> DeleteTemplateAsync(int id);

    /// <summary>
    /// Crée un nouvel événement à partir d'un template
    /// </summary>
    /// <param name="createEventFromTemplateDto">Données spécifiques à l'événement (dates, lieu, capacité)</param>
    /// <param name="userEmail">Email de l'utilisateur créateur</param>
    /// <returns>Événement créé à partir du template</returns>
    /// <exception cref="KeyNotFoundException">Template ou utilisateur non trouvé</exception>
    /// <remarks>
    /// Processus de création :
    /// - Récupère le template existant
    /// - Copie les données du template (titre, description, SurveyJsData)
    /// - Ajoute les données spécifiques fournies (dates, lieu, MaxParticipants)
    /// - Associe l'événement à l'utilisateur créateur
    /// Gain de temps pour événements récurrents
    /// </remarks>
    Task<EventDto> CreateEventFromTemplateAsync(CreateEventFromTemplateDto createEventFromTemplateDto, string userEmail);

    /// <summary>
    /// Sauvegarde un événement existant comme template réutilisable
    /// </summary>
    /// <param name="eventId">Identifiant de l'événement à convertir en template</param>
    /// <returns>Template créé à partir de l'événement</returns>
    /// <exception cref="KeyNotFoundException">Événement non trouvé</exception>
    /// <remarks>
    /// Copie le titre, la description et le formulaire SurveyJS de l'événement.
    /// N'inclut pas les dates, lieu ou capacité (données spécifiques à l'événement).
    /// Permet de réutiliser facilement un événement pour de futures éditions.
    /// </remarks>
    Task<EventTemplateDto> SaveEventAsTemplateAsync(int eventId);
}
