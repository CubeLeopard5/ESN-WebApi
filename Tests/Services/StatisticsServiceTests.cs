using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.Interfaces;
using Business.Statistics;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Statistics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

[TestClass]
public class StatisticsServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<ILogger<StatisticsService>> _mockLogger = null!;
    private Mock<IEventRepository> _mockEventRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IEventRegistrationRepository> _mockEventRegistrationRepository = null!;
    private IStatisticsService _statisticsService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<StatisticsService>>();
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEventRegistrationRepository = new Mock<IEventRegistrationRepository>();

        _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.EventRegistrations).Returns(_mockEventRegistrationRepository.Object);

        _statisticsService = new StatisticsService(
            _mockUnitOfWork.Object,
            _mockLogger.Object
        );
    }

    #region GetGlobalStatsAsync Tests

    [TestMethod]
    public async Task GetGlobalStatsAsync_WithData_ShouldReturnCorrectTotals()
    {
        // Arrange
        _mockEventRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(42);
        _mockUserRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(156);
        _mockEventRegistrationRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(387);
        _mockEventRegistrationRepository.Setup(r => r.GetAverageAttendanceRateAsync())
            .ReturnsAsync(78.5m);

        // Act
        var result = await _statisticsService.GetGlobalStatsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(42, result.TotalEvents);
        Assert.AreEqual(156, result.TotalUsers);
        Assert.AreEqual(387, result.TotalRegistrations);
        Assert.AreEqual(78.5m, result.AverageAttendanceRate);
    }

    [TestMethod]
    public async Task GetGlobalStatsAsync_EmptyDatabase_ShouldReturnZeros()
    {
        // Arrange
        _mockEventRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(0);
        _mockUserRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(0);
        _mockEventRegistrationRepository.Setup(r => r.CountAsync())
            .ReturnsAsync(0);
        _mockEventRegistrationRepository.Setup(r => r.GetAverageAttendanceRateAsync())
            .ReturnsAsync(0m);

        // Act
        var result = await _statisticsService.GetGlobalStatsAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalEvents);
        Assert.AreEqual(0, result.TotalUsers);
        Assert.AreEqual(0, result.TotalRegistrations);
        Assert.AreEqual(0m, result.AverageAttendanceRate);
    }

    #endregion

    #region GetEventsOverTimeAsync Tests

    [TestMethod]
    public async Task GetEventsOverTimeAsync_WithData_ShouldReturnMonthlyBreakdown()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var events = new List<EventBo>
        {
            new() { Id = 1, Title = "Event 1", CreatedAt = now.AddMonths(-1) },
            new() { Id = 2, Title = "Event 2", CreatedAt = now.AddMonths(-1) },
            new() { Id = 3, Title = "Event 3", CreatedAt = now }
        };

        _mockEventRepository.Setup(r => r.GetEventsCreatedAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(events);

        // Act
        var result = await _statisticsService.GetEventsOverTimeAsync(12);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.TotalInPeriod);
        Assert.IsTrue(result.DataPoints.Count > 0);
        Assert.IsTrue(result.DataPoints.Count <= 12);
    }

    [TestMethod]
    public async Task GetEventsOverTimeAsync_EmptyPeriod_ShouldReturnEmptyDataPoints()
    {
        // Arrange
        _mockEventRepository.Setup(r => r.GetEventsCreatedAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventBo>());

        // Act
        var result = await _statisticsService.GetEventsOverTimeAsync(6);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalInPeriod);
        Assert.AreEqual(6, result.DataPoints.Count); // Should still have month labels
    }

    [TestMethod]
    public async Task GetEventsOverTimeAsync_CustomMonths_ShouldRespectParameter()
    {
        // Arrange
        _mockEventRepository.Setup(r => r.GetEventsCreatedAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventBo>());

        // Act
        var result = await _statisticsService.GetEventsOverTimeAsync(3);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(3, result.DataPoints.Count);
    }

    #endregion

    #region GetRegistrationTrendAsync Tests

    [TestMethod]
    public async Task GetRegistrationTrendAsync_WithData_ShouldReturnMonthlyBreakdown()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 1, RegisteredAt = now.AddMonths(-2) },
            new() { Id = 2, RegisteredAt = now.AddMonths(-1) },
            new() { Id = 3, RegisteredAt = now.AddMonths(-1) },
            new() { Id = 4, RegisteredAt = now }
        };

        _mockEventRegistrationRepository.Setup(r => r.GetRegistrationsAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(registrations);

        // Act
        var result = await _statisticsService.GetRegistrationTrendAsync(12);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(4, result.TotalInPeriod);
        Assert.IsTrue(result.DataPoints.Count > 0);
    }

    [TestMethod]
    public async Task GetRegistrationTrendAsync_EmptyPeriod_ShouldReturnZeroTotal()
    {
        // Arrange
        _mockEventRegistrationRepository.Setup(r => r.GetRegistrationsAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventRegistrationBo>());

        // Act
        var result = await _statisticsService.GetRegistrationTrendAsync(6);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalInPeriod);
    }

    #endregion

    #region GetAttendanceBreakdownAsync Tests

    [TestMethod]
    public async Task GetAttendanceBreakdownAsync_MixedStatuses_ShouldCalculateCorrectly()
    {
        // Arrange
        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 1, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 2, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 3, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 4, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 5, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 6, AttendanceStatus = AttendanceStatus.Absent },
            new() { Id = 7, AttendanceStatus = AttendanceStatus.Absent },
            new() { Id = 8, AttendanceStatus = AttendanceStatus.Excused },
            new() { Id = 9, AttendanceStatus = null },
            new() { Id = 10, AttendanceStatus = null }
        };

        _mockEventRegistrationRepository.Setup(r => r.GetAllWithAttendanceAsync())
            .ReturnsAsync(registrations);

        // Act
        var result = await _statisticsService.GetAttendanceBreakdownAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.PresentCount);
        Assert.AreEqual(2, result.AbsentCount);
        Assert.AreEqual(1, result.ExcusedCount);
        Assert.AreEqual(2, result.NotValidatedCount);
        Assert.AreEqual(10, result.TotalCount);
        // Percentages based on validated (8 total validated)
        Assert.AreEqual(62.5m, result.PresentPercentage); // 5/8 * 100
        Assert.AreEqual(25m, result.AbsentPercentage);    // 2/8 * 100
        Assert.AreEqual(12.5m, result.ExcusedPercentage); // 1/8 * 100
    }

    [TestMethod]
    public async Task GetAttendanceBreakdownAsync_AllPresent_ShouldReturn100Percent()
    {
        // Arrange
        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 1, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 2, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 3, AttendanceStatus = AttendanceStatus.Present }
        };

        _mockEventRegistrationRepository.Setup(r => r.GetAllWithAttendanceAsync())
            .ReturnsAsync(registrations);

        // Act
        var result = await _statisticsService.GetAttendanceBreakdownAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.PresentPercentage);
        Assert.AreEqual(0m, result.AbsentPercentage);
        Assert.AreEqual(0m, result.ExcusedPercentage);
    }

    [TestMethod]
    public async Task GetAttendanceBreakdownAsync_NoValidated_ShouldReturnZeroPercentages()
    {
        // Arrange
        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 1, AttendanceStatus = null },
            new() { Id = 2, AttendanceStatus = null }
        };

        _mockEventRegistrationRepository.Setup(r => r.GetAllWithAttendanceAsync())
            .ReturnsAsync(registrations);

        // Act
        var result = await _statisticsService.GetAttendanceBreakdownAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result.PresentPercentage);
        Assert.AreEqual(0m, result.AbsentPercentage);
        Assert.AreEqual(0m, result.ExcusedPercentage);
        Assert.AreEqual(2, result.NotValidatedCount);
    }

    [TestMethod]
    public async Task GetAttendanceBreakdownAsync_EmptyData_ShouldReturnZeros()
    {
        // Arrange
        _mockEventRegistrationRepository.Setup(r => r.GetAllWithAttendanceAsync())
            .ReturnsAsync(new List<EventRegistrationBo>());

        // Act
        var result = await _statisticsService.GetAttendanceBreakdownAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0m, result.PresentPercentage);
    }

    #endregion

    #region GetParticipationTrendAsync Tests

    [TestMethod]
    public async Task GetParticipationTrendAsync_WithData_ShouldCalculateRates()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 1, RegisteredAt = now.AddMonths(-1), AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 2, RegisteredAt = now.AddMonths(-1), AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 3, RegisteredAt = now.AddMonths(-1), AttendanceStatus = AttendanceStatus.Absent },
            new() { Id = 4, RegisteredAt = now.AddMonths(-1), AttendanceStatus = null },
            new() { Id = 5, RegisteredAt = now, AttendanceStatus = AttendanceStatus.Present }
        };

        _mockEventRegistrationRepository.Setup(r => r.GetRegistrationsAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(registrations);

        // Act
        var result = await _statisticsService.GetParticipationTrendAsync(12);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.DataPoints.Count > 0);
        Assert.IsTrue(result.AverageRate >= 0);
    }

    [TestMethod]
    public async Task GetParticipationTrendAsync_EmptyData_ShouldReturnZeroAverage()
    {
        // Arrange
        _mockEventRegistrationRepository.Setup(r => r.GetRegistrationsAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventRegistrationBo>());

        // Act
        var result = await _statisticsService.GetParticipationTrendAsync(6);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0m, result.AverageRate);
    }

    #endregion

    #region GetTopEventsAsync Tests

    [TestMethod]
    public async Task GetTopEventsAsync_WithData_ShouldReturnSortedByRegistrations()
    {
        // Arrange
        var events = new List<EventBo>
        {
            new()
            {
                Id = 1,
                Title = "Small Event",
                StartDate = DateTime.UtcNow,
                MaxParticipants = 50,
                EventRegistrations = new List<EventRegistrationBo>
                {
                    new() { Id = 1, AttendanceStatus = AttendanceStatus.Present },
                    new() { Id = 2, AttendanceStatus = AttendanceStatus.Present }
                }
            },
            new()
            {
                Id = 2,
                Title = "Big Event",
                StartDate = DateTime.UtcNow,
                MaxParticipants = 100,
                EventRegistrations = new List<EventRegistrationBo>
                {
                    new() { Id = 3, AttendanceStatus = AttendanceStatus.Present },
                    new() { Id = 4, AttendanceStatus = AttendanceStatus.Present },
                    new() { Id = 5, AttendanceStatus = AttendanceStatus.Present },
                    new() { Id = 6, AttendanceStatus = AttendanceStatus.Absent },
                    new() { Id = 7, AttendanceStatus = AttendanceStatus.Present }
                }
            }
        };

        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(It.IsAny<int>()))
            .ReturnsAsync(events.OrderByDescending(e => e.EventRegistrations.Count).ToList());

        // Act
        var result = await _statisticsService.GetTopEventsAsync(10);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Big Event", result[0].Title);
        Assert.AreEqual(5, result[0].RegistrationCount);
        Assert.AreEqual("Small Event", result[1].Title);
        Assert.AreEqual(2, result[1].RegistrationCount);
    }

    [TestMethod]
    public async Task GetTopEventsAsync_ShouldCalculateFillRate()
    {
        // Arrange
        var events = new List<EventBo>
        {
            new()
            {
                Id = 1,
                Title = "Half Full Event",
                StartDate = DateTime.UtcNow,
                MaxParticipants = 100,
                EventRegistrations = Enumerable.Range(1, 50)
                    .Select(i => new EventRegistrationBo { Id = i, AttendanceStatus = AttendanceStatus.Present })
                    .ToList()
            }
        };

        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(It.IsAny<int>()))
            .ReturnsAsync(events);

        // Act
        var result = await _statisticsService.GetTopEventsAsync(10);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(50m, result[0].FillRate);
    }

    [TestMethod]
    public async Task GetTopEventsAsync_NoMaxParticipants_FillRateShouldBeNull()
    {
        // Arrange
        var events = new List<EventBo>
        {
            new()
            {
                Id = 1,
                Title = "Unlimited Event",
                StartDate = DateTime.UtcNow,
                MaxParticipants = null,
                EventRegistrations = new List<EventRegistrationBo>
                {
                    new() { Id = 1, AttendanceStatus = AttendanceStatus.Present }
                }
            }
        };

        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(It.IsAny<int>()))
            .ReturnsAsync(events);

        // Act
        var result = await _statisticsService.GetTopEventsAsync(10);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result[0].FillRate);
    }

    [TestMethod]
    public async Task GetTopEventsAsync_ShouldCalculateAttendanceRate()
    {
        // Arrange
        var events = new List<EventBo>
        {
            new()
            {
                Id = 1,
                Title = "Event with Attendance",
                StartDate = DateTime.UtcNow,
                MaxParticipants = 100,
                EventRegistrations = new List<EventRegistrationBo>
                {
                    new() { Id = 1, AttendanceStatus = AttendanceStatus.Present },
                    new() { Id = 2, AttendanceStatus = AttendanceStatus.Present },
                    new() { Id = 3, AttendanceStatus = AttendanceStatus.Absent },
                    new() { Id = 4, AttendanceStatus = AttendanceStatus.Absent }
                }
            }
        };

        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(It.IsAny<int>()))
            .ReturnsAsync(events);

        // Act
        var result = await _statisticsService.GetTopEventsAsync(10);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(50m, result[0].AttendanceRate); // 2 present out of 4 registered
    }

    [TestMethod]
    public async Task GetTopEventsAsync_EmptyData_ShouldReturnEmptyList()
    {
        // Arrange
        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<EventBo>());

        // Act
        var result = await _statisticsService.GetTopEventsAsync(10);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(0, result.Count);
    }

    [TestMethod]
    public async Task GetTopEventsAsync_ShouldRespectCountParameter()
    {
        // Arrange
        var events = Enumerable.Range(1, 20)
            .Select(i => new EventBo
            {
                Id = i,
                Title = $"Event {i}",
                StartDate = DateTime.UtcNow,
                EventRegistrations = Enumerable.Range(1, i)
                    .Select(j => new EventRegistrationBo { Id = j })
                    .ToList()
            })
            .OrderByDescending(e => e.EventRegistrations.Count)
            .Take(5)
            .ToList();

        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(5))
            .ReturnsAsync(events);

        // Act
        var result = await _statisticsService.GetTopEventsAsync(5);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.Count);
    }

    #endregion

    #region GetDashboardStatsAsync Tests

    [TestMethod]
    public async Task GetDashboardStatsAsync_ShouldAggregateAllStats()
    {
        // Arrange
        SetupMocksForDashboard();

        // Act
        var result = await _statisticsService.GetDashboardStatsAsync(12, 10);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.GlobalStats);
        Assert.IsNotNull(result.EventsOverTime);
        Assert.IsNotNull(result.RegistrationTrend);
        Assert.IsNotNull(result.AttendanceBreakdown);
        Assert.IsNotNull(result.ParticipationTrend);
        Assert.IsNotNull(result.TopEvents);
    }

    [TestMethod]
    public async Task GetDashboardStatsAsync_ShouldUseProvidedParameters()
    {
        // Arrange
        SetupMocksForDashboard();

        // Act
        var result = await _statisticsService.GetDashboardStatsAsync(6, 5);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(6, result.EventsOverTime.DataPoints.Count);
        _mockEventRepository.Verify(r => r.GetEventsWithRegistrationCountAsync(5), Times.Once);
    }

    #endregion

    #region Helper Methods

    private void SetupMocksForDashboard()
    {
        // Global stats
        _mockEventRepository.Setup(r => r.CountAsync()).ReturnsAsync(10);
        _mockUserRepository.Setup(r => r.CountAsync()).ReturnsAsync(50);
        _mockEventRegistrationRepository.Setup(r => r.CountAsync()).ReturnsAsync(100);
        _mockEventRegistrationRepository.Setup(r => r.GetAverageAttendanceRateAsync()).ReturnsAsync(75m);

        // Events over time
        _mockEventRepository.Setup(r => r.GetEventsCreatedAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventBo>());

        // Registration trend and participation trend
        _mockEventRegistrationRepository.Setup(r => r.GetRegistrationsAfterAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<EventRegistrationBo>());

        // Attendance breakdown
        _mockEventRegistrationRepository.Setup(r => r.GetAllWithAttendanceAsync())
            .ReturnsAsync(new List<EventRegistrationBo>());

        // Top events
        _mockEventRepository.Setup(r => r.GetEventsWithRegistrationCountAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<EventBo>());
    }

    #endregion
}
