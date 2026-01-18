using Dto.Statistics;

namespace Business.Interfaces;

/// <summary>
/// Service de gestion des statistiques pour le tableau de bord administrateur
/// </summary>
public interface IStatisticsService
{
    /// <summary>
    /// Vérifie si l'utilisateur a le droit d'accéder aux statistiques (Admin ou ESN Member)
    /// </summary>
    /// <param name="userEmail">Email de l'utilisateur</param>
    /// <returns>True si autorisé, False sinon</returns>
    /// <exception cref="UnauthorizedAccessException">Si l'utilisateur n'est pas autorisé</exception>
    Task VerifyAccessAsync(string userEmail);

    /// <summary>
    /// Récupère les statistiques globales (totaux et moyennes)
    /// </summary>
    /// <returns>Statistiques globales incluant total events, users, registrations et taux de présence moyen</returns>
    Task<GlobalStatsDto> GetGlobalStatsAsync();

    /// <summary>
    /// Récupère le nombre d'événements créés par mois sur une période donnée
    /// </summary>
    /// <param name="months">Nombre de mois à inclure (défaut: 12)</param>
    /// <returns>Données des événements par mois avec total sur la période</returns>
    Task<EventsOverTimeDto> GetEventsOverTimeAsync(int months = 12);

    /// <summary>
    /// Récupère la tendance des inscriptions par mois sur une période donnée
    /// </summary>
    /// <param name="months">Nombre de mois à inclure (défaut: 12)</param>
    /// <returns>Données des inscriptions par mois avec total sur la période</returns>
    Task<RegistrationTrendDto> GetRegistrationTrendAsync(int months = 12);

    /// <summary>
    /// Récupère la répartition des statuts de présence (présent, absent, excusé, non validé)
    /// </summary>
    /// <returns>Répartition des présences avec pourcentages</returns>
    Task<AttendanceBreakdownDto> GetAttendanceBreakdownAsync();

    /// <summary>
    /// Récupère l'évolution du taux de participation (inscrits vs présents) par mois
    /// </summary>
    /// <param name="months">Nombre de mois à inclure (défaut: 12)</param>
    /// <returns>Données de participation par mois avec taux moyen sur la période</returns>
    Task<ParticipationRateTrendDto> GetParticipationTrendAsync(int months = 12);

    /// <summary>
    /// Récupère les événements les plus populaires triés par nombre d'inscriptions
    /// </summary>
    /// <param name="count">Nombre d'événements à retourner (défaut: 10)</param>
    /// <returns>Liste des top événements avec leurs statistiques</returns>
    Task<List<TopEventDto>> GetTopEventsAsync(int count = 10);

    /// <summary>
    /// Récupère toutes les statistiques du tableau de bord en un seul appel
    /// </summary>
    /// <param name="months">Nombre de mois pour les données temporelles (défaut: 12)</param>
    /// <param name="topEventsCount">Nombre de top événements à inclure (défaut: 10)</param>
    /// <returns>Toutes les statistiques agrégées pour le tableau de bord</returns>
    Task<DashboardStatsDto> GetDashboardStatsAsync(int months = 12, int topEventsCount = 10);
}
