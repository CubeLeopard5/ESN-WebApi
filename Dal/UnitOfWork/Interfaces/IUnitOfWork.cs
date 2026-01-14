using Dal.Repositories.Interfaces;

namespace Dal.UnitOfWork.Interfaces;

/// <summary>
/// Interface du pattern Unit of Work
/// Coordonne les modifications de plusieurs repositories et garantit la cohérence transactionnelle
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Tous les repositories sont maintenant spécialisés pour garantir la cohérence architecturale
    IEventRepository Events { get; }
    IEventTemplateRepository EventTemplates { get; }
    IUserRepository Users { get; }
    ICalendarRepository Calendars { get; }
    IPropositionRepository Propositions { get; }
    IEventRegistrationRepository EventRegistrations { get; }
    ICalendarSubOrganizerRepository CalendarSubOrganizers { get; }
    IRoleRepository Roles { get; }
    IPropositionVoteRepository PropositionVotes { get; }

    /// <summary>
    /// Sauvegarde tous les changements en une seule transaction
    /// </summary>
    Task<int> SaveChangesAsync();

    /// <summary>
    /// Démarre une transaction explicite
    /// </summary>
    Task BeginTransactionAsync();

    /// <summary>
    /// Valide la transaction en cours
    /// </summary>
    Task CommitTransactionAsync();

    /// <summary>
    /// Annule la transaction en cours
    /// </summary>
    Task RollbackTransactionAsync();
}
