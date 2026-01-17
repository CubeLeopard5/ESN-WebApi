using Dal.Repositories;
using Dal.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace Dal.UnitOfWork;

/// <summary>
/// Implémentation du pattern Unit of Work
/// Tous les repositories sont maintenant spécialisés pour une architecture cohérente
/// </summary>
public class UnitOfWork(EsnDevContext context) : Interfaces.IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    // Lazy initialization des repositories spécialisés
    public IEventRepository Events =>
        field ??= new EventRepository(context);

    public IEventTemplateRepository EventTemplates =>
        field ??= new EventTemplateRepository(context);

    public IUserRepository Users =>
        field ??= new UserRepository(context);

    public ICalendarRepository Calendars =>
        field ??= new CalendarRepository(context);

    public IPropositionRepository Propositions =>
        field ??= new PropositionRepository(context);

    public IEventRegistrationRepository EventRegistrations =>
        field ??= new EventRegistrationRepository(context);

    public ICalendarSubOrganizerRepository CalendarSubOrganizers =>
        field ??= new CalendarSubOrganizerRepository(context);

    public IRoleRepository Roles =>
        field ??= new RoleRepository(context);

    public IPropositionVoteRepository PropositionVotes =>
        field ??= new PropositionVoteRepository(context);

    public IEventFeedbackRepository EventFeedbacks =>
        field ??= new EventFeedbackRepository(context);

    public async Task<int> SaveChangesAsync()
    {
        return await context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        // Ne pas disposer le context car il est géré par le conteneur DI (Scoped)
        GC.SuppressFinalize(this);
    }
}
