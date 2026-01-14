using Bo.Models;

namespace Dal.Specifications;

/// <summary>
/// Spécifications pour les événements
/// </summary>
public static class EventSpecifications
{
    /// <summary>
    /// Spécification pour récupérer les événements avec tous leurs détails
    /// </summary>
    public class EventsWithDetailsSpecification : BaseSpecification<EventBo>
    {
        public EventsWithDetailsSpecification() : base()
        {
            AddInclude(e => e.User);
            AddInclude(e => e.EventRegistrations);
            ApplyOrderByDescending(e => e.CreatedAt);
        }
    }

    /// <summary>
    /// Spécification pour récupérer un événement spécifique avec ses détails
    /// </summary>
    public class EventByIdWithDetailsSpecification : BaseSpecification<EventBo>
    {
        public EventByIdWithDetailsSpecification(int eventId)
            : base(e => e.Id == eventId)
        {
            AddInclude(e => e.User);
            AddInclude(e => e.EventRegistrations);
        }
    }

    /// <summary>
    /// Spécification pour récupérer les événements créés par un utilisateur
    /// </summary>
    public class EventsByUserEmailSpecification : BaseSpecification<EventBo>
    {
        public EventsByUserEmailSpecification(string userEmail)
            : base(e => e.User.Email == userEmail)
        {
            AddInclude(e => e.User);
            ApplyOrderByDescending(e => e.CreatedAt);
        }
    }

    /// <summary>
    /// Spécification pour récupérer les événements avec pagination
    /// </summary>
    public class EventsWithPaginationSpecification : BaseSpecification<EventBo>
    {
        public EventsWithPaginationSpecification(int pageNumber, int pageSize)
            : base()
        {
            AddInclude(e => e.User);
            AddInclude(e => e.EventRegistrations);
            ApplyOrderByDescending(e => e.CreatedAt);
            ApplyPaging((pageNumber - 1) * pageSize, pageSize);
        }
    }
}
