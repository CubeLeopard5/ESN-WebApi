using Business.Interfaces;
using Dto.Statistics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class StatisticsControllerTests
    {
        private Mock<IStatisticsService> _mockStatisticsService = null!;
        private Mock<ILogger<StatisticsController>> _mockLogger = null!;
        private StatisticsController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockStatisticsService = new Mock<IStatisticsService>();
            _mockLogger = new Mock<ILogger<StatisticsController>>();
            _controller = new StatisticsController(_mockStatisticsService.Object, _mockLogger.Object);

            SetupAuthenticatedUser(TestUserEmail);
        }

        private void SetupAuthenticatedUser(string email)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Role, "Admin"),
                new Claim("userId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        private void SetupVerifyAccessSuccess()
        {
            _mockStatisticsService.Setup(s => s.VerifyAccessAsync(TestUserEmail))
                .Returns(Task.CompletedTask);
        }

        private void SetupVerifyAccessForbidden()
        {
            _mockStatisticsService.Setup(s => s.VerifyAccessAsync(TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException("Access denied"));
        }

        #region GetGlobalStats Tests

        [TestMethod]
        public async Task GetGlobalStats_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var stats = new GlobalStatsDto { TotalEvents = 10, TotalUsers = 50, TotalRegistrations = 200 };
            _mockStatisticsService.Setup(s => s.GetGlobalStatsAsync()).ReturnsAsync(stats);

            // Act
            var result = await _controller.GetGlobalStats();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(stats, okResult.Value);
        }

        [TestMethod]
        public async Task GetGlobalStats_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetGlobalStats();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetEventsOverTime Tests

        [TestMethod]
        public async Task GetEventsOverTime_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var data = new EventsOverTimeDto { TotalInPeriod = 15 };
            _mockStatisticsService.Setup(s => s.GetEventsOverTimeAsync(12)).ReturnsAsync(data);

            // Act
            var result = await _controller.GetEventsOverTime(12);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetEventsOverTime_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetEventsOverTime();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetRegistrationTrend Tests

        [TestMethod]
        public async Task GetRegistrationTrend_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var data = new RegistrationTrendDto { TotalInPeriod = 100 };
            _mockStatisticsService.Setup(s => s.GetRegistrationTrendAsync(12)).ReturnsAsync(data);

            // Act
            var result = await _controller.GetRegistrationTrend(12);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetRegistrationTrend_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetRegistrationTrend();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetAttendanceBreakdown Tests

        [TestMethod]
        public async Task GetAttendanceBreakdown_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var data = new AttendanceBreakdownDto { PresentCount = 80, AbsentCount = 10, ExcusedCount = 5 };
            _mockStatisticsService.Setup(s => s.GetAttendanceBreakdownAsync()).ReturnsAsync(data);

            // Act
            var result = await _controller.GetAttendanceBreakdown();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetAttendanceBreakdown_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetAttendanceBreakdown();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetParticipationTrend Tests

        [TestMethod]
        public async Task GetParticipationTrend_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var data = new ParticipationRateTrendDto { AverageRate = 75.5m };
            _mockStatisticsService.Setup(s => s.GetParticipationTrendAsync(12)).ReturnsAsync(data);

            // Act
            var result = await _controller.GetParticipationTrend(12);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetParticipationTrend_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetParticipationTrend();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetTopEvents Tests

        [TestMethod]
        public async Task GetTopEvents_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var data = new List<TopEventDto>
            {
                new() { EventId = 1, Title = "Top Event", RegistrationCount = 50 }
            };
            _mockStatisticsService.Setup(s => s.GetTopEventsAsync(10)).ReturnsAsync(data);

            // Act
            var result = await _controller.GetTopEvents(10);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returnedData = okResult.Value as List<TopEventDto>;
            Assert.IsNotNull(returnedData);
            Assert.AreEqual(1, returnedData.Count);
        }

        [TestMethod]
        public async Task GetTopEvents_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetTopEvents();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetDashboardStats Tests

        [TestMethod]
        public async Task GetDashboardStats_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            SetupVerifyAccessSuccess();
            var data = new DashboardStatsDto();
            _mockStatisticsService.Setup(s => s.GetDashboardStatsAsync(12, 10)).ReturnsAsync(data);

            // Act
            var result = await _controller.GetDashboardStats(12, 10);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetDashboardStats_WhenUnauthorized_Returns403()
        {
            // Arrange
            SetupVerifyAccessForbidden();

            // Act
            var result = await _controller.GetDashboardStats();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion
    }
}
