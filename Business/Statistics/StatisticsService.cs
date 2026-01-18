using Bo.Enums;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Statistics;
using Microsoft.Extensions.Logging;
using System.Globalization;

namespace Business.Statistics;

/// <summary>
/// Service de gestion des statistiques pour le tableau de bord administrateur
/// </summary>
public class StatisticsService : IStatisticsService
{
    private const int MinMonths = 1;
    private const int MaxMonths = 120; // 10 years maximum
    private const int MinTopEventsCount = 1;
    private const int MaxTopEventsCount = 100;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StatisticsService> _logger;

    public StatisticsService(IUnitOfWork unitOfWork, ILogger<StatisticsService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    private static int ClampMonths(int months) => Math.Clamp(months, MinMonths, MaxMonths);
    private static int ClampTopEventsCount(int count) => Math.Clamp(count, MinTopEventsCount, MaxTopEventsCount);

    /// <inheritdoc />
    public async Task VerifyAccessAsync(string userEmail)
    {
        var user = await _unitOfWork.Users.GetByEmailWithRoleAsync(userEmail);

        if (user == null)
        {
            _logger.LogWarning("VerifyAccessAsync - User not found: {Email}", userEmail);
            throw new UnauthorizedAccessException("User not found");
        }

        var isAdmin = user.Role?.Name == "Admin";
        var isEsnMember = user.StudentType == Bo.Constants.StudentType.EsnMember;

        if (!isAdmin && !isEsnMember)
        {
            _logger.LogWarning("VerifyAccessAsync - User {Email} is not Admin or ESN Member", userEmail);
            throw new UnauthorizedAccessException("Access denied. Only administrators and ESN members can access statistics.");
        }

        _logger.LogDebug("VerifyAccessAsync - User {Email} authorized (Admin: {IsAdmin}, ESN: {IsEsn})",
            userEmail, isAdmin, isEsnMember);
    }

    /// <inheritdoc />
    public async Task<GlobalStatsDto> GetGlobalStatsAsync()
    {
        _logger.LogDebug("Fetching global statistics");

        var totalEvents = await _unitOfWork.Events.CountAsync();
        var totalUsers = await _unitOfWork.Users.CountAsync();
        var totalRegistrations = await _unitOfWork.EventRegistrations.CountAsync();
        var averageAttendanceRate = await _unitOfWork.EventRegistrations.GetAverageAttendanceRateAsync();

        return new GlobalStatsDto
        {
            TotalEvents = totalEvents,
            TotalUsers = totalUsers,
            TotalRegistrations = totalRegistrations,
            AverageAttendanceRate = averageAttendanceRate
        };
    }

    /// <inheritdoc />
    public async Task<EventsOverTimeDto> GetEventsOverTimeAsync(int months = 12)
    {
        months = ClampMonths(months);
        _logger.LogDebug("Fetching events over time for {Months} months", months);

        var startDate = DateTime.UtcNow.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var events = await _unitOfWork.Events.GetEventsCreatedAfterAsync(startDate);
        var eventsList = events.ToList();

        var dataPoints = GenerateMonthlyDataPoints(months, eventsList, e => e.CreatedAt ?? DateTime.UtcNow);

        return new EventsOverTimeDto
        {
            DataPoints = dataPoints,
            TotalInPeriod = eventsList.Count
        };
    }

    /// <inheritdoc />
    public async Task<RegistrationTrendDto> GetRegistrationTrendAsync(int months = 12)
    {
        months = ClampMonths(months);
        _logger.LogDebug("Fetching registration trend for {Months} months", months);

        var startDate = DateTime.UtcNow.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var registrations = await _unitOfWork.EventRegistrations.GetRegistrationsAfterAsync(startDate);
        var registrationsList = registrations.ToList();

        var dataPoints = GenerateMonthlyDataPoints(months, registrationsList, r => r.RegisteredAt ?? DateTime.UtcNow);

        return new RegistrationTrendDto
        {
            DataPoints = dataPoints,
            TotalInPeriod = registrationsList.Count
        };
    }

    /// <inheritdoc />
    public async Task<AttendanceBreakdownDto> GetAttendanceBreakdownAsync()
    {
        _logger.LogDebug("Fetching attendance breakdown");

        var registrations = await _unitOfWork.EventRegistrations.GetAllWithAttendanceAsync();
        var registrationsList = registrations.ToList();

        var presentCount = registrationsList.Count(r => r.AttendanceStatus == AttendanceStatus.Present);
        var absentCount = registrationsList.Count(r => r.AttendanceStatus == AttendanceStatus.Absent);
        var excusedCount = registrationsList.Count(r => r.AttendanceStatus == AttendanceStatus.Excused);
        var notValidatedCount = registrationsList.Count(r => r.AttendanceStatus == null);
        var totalCount = registrationsList.Count;
        var validatedCount = presentCount + absentCount + excusedCount;

        return new AttendanceBreakdownDto
        {
            PresentCount = presentCount,
            AbsentCount = absentCount,
            ExcusedCount = excusedCount,
            NotValidatedCount = notValidatedCount,
            TotalCount = totalCount,
            PresentPercentage = validatedCount > 0 ? Math.Round((decimal)presentCount / validatedCount * 100, 2) : 0,
            AbsentPercentage = validatedCount > 0 ? Math.Round((decimal)absentCount / validatedCount * 100, 2) : 0,
            ExcusedPercentage = validatedCount > 0 ? Math.Round((decimal)excusedCount / validatedCount * 100, 2) : 0
        };
    }

    /// <inheritdoc />
    public async Task<ParticipationRateTrendDto> GetParticipationTrendAsync(int months = 12)
    {
        var safeMonthCount = ClampMonths(months);
        _logger.LogDebug("Fetching participation trend for {Months} months", safeMonthCount);

        var startDate = DateTime.UtcNow.AddMonths(-safeMonthCount + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var registrations = await _unitOfWork.EventRegistrations.GetRegistrationsAfterAsync(startDate);
        var registrationsList = registrations.ToList();

        var dataPoints = new List<ParticipationRatePointDto>();
        var currentDate = startDate;

        for (var i = 0; i < safeMonthCount; i++)
        {
            var monthRegistrations = registrationsList
                .Where(r => r.RegisteredAt?.Year == currentDate.Year && r.RegisteredAt?.Month == currentDate.Month)
                .ToList();

            var registeredCount = monthRegistrations.Count;
            var attendedCount = monthRegistrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);
            var rate = registeredCount > 0 ? Math.Round((decimal)attendedCount / registeredCount * 100, 2) : 0;

            dataPoints.Add(new ParticipationRatePointDto
            {
                Label = currentDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Year = currentDate.Year,
                Month = currentDate.Month,
                RegisteredCount = registeredCount,
                AttendedCount = attendedCount,
                ParticipationRate = rate
            });

            currentDate = currentDate.AddMonths(1);
        }

        var totalRegistered = dataPoints.Sum(d => d.RegisteredCount);
        var totalAttended = dataPoints.Sum(d => d.AttendedCount);
        var averageRate = totalRegistered > 0 ? Math.Round((decimal)totalAttended / totalRegistered * 100, 2) : 0;

        return new ParticipationRateTrendDto
        {
            DataPoints = dataPoints,
            AverageRate = averageRate
        };
    }

    /// <inheritdoc />
    public async Task<List<TopEventDto>> GetTopEventsAsync(int count = 10)
    {
        count = ClampTopEventsCount(count);
        _logger.LogDebug("Fetching top {Count} events", count);

        var events = await _unitOfWork.Events.GetEventsWithRegistrationCountAsync(count);

        return events.Select(e =>
        {
            var registrations = e.EventRegistrations.ToList();
            var registrationCount = registrations.Count;
            var presentCount = registrations.Count(r => r.AttendanceStatus == AttendanceStatus.Present);
            var attendanceRate = registrationCount > 0 ? Math.Round((decimal)presentCount / registrationCount * 100, 2) : 0;
            decimal? fillRate = e.MaxParticipants.HasValue && e.MaxParticipants > 0
                ? Math.Round((decimal)registrationCount / e.MaxParticipants.Value * 100, 2)
                : null;

            return new TopEventDto
            {
                EventId = e.Id,
                Title = e.Title,
                StartDate = e.StartDate,
                RegistrationCount = registrationCount,
                MaxParticipants = e.MaxParticipants,
                FillRate = fillRate,
                AttendanceRate = attendanceRate
            };
        }).ToList();
    }

    /// <inheritdoc />
    public async Task<DashboardStatsDto> GetDashboardStatsAsync(int months = 12, int topEventsCount = 10)
    {
        months = ClampMonths(months);
        topEventsCount = ClampTopEventsCount(topEventsCount);
        _logger.LogDebug("Fetching complete dashboard statistics");

        var globalStats = await GetGlobalStatsAsync();
        var eventsOverTime = await GetEventsOverTimeAsync(months);
        var registrationTrend = await GetRegistrationTrendAsync(months);
        var attendanceBreakdown = await GetAttendanceBreakdownAsync();
        var participationTrend = await GetParticipationTrendAsync(months);
        var topEvents = await GetTopEventsAsync(topEventsCount);

        return new DashboardStatsDto
        {
            GlobalStats = globalStats,
            EventsOverTime = eventsOverTime,
            RegistrationTrend = registrationTrend,
            AttendanceBreakdown = attendanceBreakdown,
            ParticipationTrend = participationTrend,
            TopEvents = topEvents
        };
    }

    private static List<TimeSeriesDataPointDto> GenerateMonthlyDataPoints<T>(
        int months,
        List<T> items,
        Func<T, DateTime> dateSelector)
    {
        var dataPoints = new List<TimeSeriesDataPointDto>();
        var startDate = DateTime.UtcNow.AddMonths(-months + 1);
        startDate = new DateTime(startDate.Year, startDate.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        for (var i = 0; i < months; i++)
        {
            var currentDate = startDate.AddMonths(i);
            var count = items.Count(item =>
            {
                var date = dateSelector(item);
                return date.Year == currentDate.Year && date.Month == currentDate.Month;
            });

            dataPoints.Add(new TimeSeriesDataPointDto
            {
                Label = currentDate.ToString("MMM yyyy", CultureInfo.InvariantCulture),
                Year = currentDate.Year,
                Month = currentDate.Month,
                Value = count
            });
        }

        return dataPoints;
    }
}
