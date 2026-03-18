using Bo.Enums;
using Business.Interfaces;
using Dto.Attendance;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class AttendanceControllerTests
    {
        private Mock<IAttendanceService> _mockAttendanceService = null!;
        private Mock<ILogger<AttendanceController>> _mockLogger = null!;
        private AttendanceController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockAttendanceService = new Mock<IAttendanceService>();
            _mockLogger = new Mock<ILogger<AttendanceController>>();
            _controller = new AttendanceController(_mockAttendanceService.Object, _mockLogger.Object);

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

        #region GetEventAttendance Tests

        [TestMethod]
        public async Task GetEventAttendance_WhenEventExists_ReturnsOkWithAttendance()
        {
            // Arrange
            var eventId = 1;
            var attendanceDto = new EventAttendanceDto { Id = eventId };
            _mockAttendanceService.Setup(s => s.GetEventAttendanceAsync(eventId))
                .ReturnsAsync(attendanceDto);

            // Act
            var result = await _controller.GetEventAttendance(eventId);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(attendanceDto, okResult.Value);
        }

        [TestMethod]
        public async Task GetEventAttendance_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAttendanceService.Setup(s => s.GetEventAttendanceAsync(99))
                .ReturnsAsync((EventAttendanceDto?)null);

            // Act
            var result = await _controller.GetEventAttendance(99);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region GetAttendanceStats Tests

        [TestMethod]
        public async Task GetAttendanceStats_WhenEventExists_ReturnsOkWithStats()
        {
            // Arrange
            var eventId = 1;
            var statsDto = new AttendanceStatsDto { EventId = eventId };
            _mockAttendanceService.Setup(s => s.GetAttendanceStatsAsync(eventId))
                .ReturnsAsync(statsDto);

            // Act
            var result = await _controller.GetAttendanceStats(eventId);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(statsDto, okResult.Value);
        }

        [TestMethod]
        public async Task GetAttendanceStats_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAttendanceService.Setup(s => s.GetAttendanceStatsAsync(99))
                .ReturnsAsync((AttendanceStatsDto?)null);

            // Act
            var result = await _controller.GetAttendanceStats(99);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region ValidateAttendance Tests

        [TestMethod]
        public async Task ValidateAttendance_WhenValid_ReturnsOkWithResult()
        {
            // Arrange
            var eventId = 1;
            var registrationId = 10;
            var dto = new ValidateAttendanceDto { Status = AttendanceStatus.Present };
            var attendanceDto = new AttendanceDto { Id = registrationId };
            _mockAttendanceService.Setup(s => s.ValidateAttendanceAsync(
                    eventId, registrationId, AttendanceStatus.Present, TestUserEmail))
                .ReturnsAsync(attendanceDto);

            // Act
            var result = await _controller.ValidateAttendance(eventId, registrationId, dto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task ValidateAttendance_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var dto = new ValidateAttendanceDto { Status = AttendanceStatus.Present };
            _mockAttendanceService.Setup(s => s.ValidateAttendanceAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AttendanceStatus>(), TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException("Not authorized"));

            // Act
            var result = await _controller.ValidateAttendance(1, 10, dto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ForbidResult>(result.Result);
        }

        [TestMethod]
        public async Task ValidateAttendance_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new ValidateAttendanceDto { Status = AttendanceStatus.Present };
            _mockAttendanceService.Setup(s => s.ValidateAttendanceAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AttendanceStatus>(), TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException("Registration not found"));

            // Act
            var result = await _controller.ValidateAttendance(1, 10, dto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task ValidateAttendance_WhenInvalidOperation_ReturnsBadRequest()
        {
            // Arrange
            var dto = new ValidateAttendanceDto { Status = AttendanceStatus.Present };
            _mockAttendanceService.Setup(s => s.ValidateAttendanceAsync(
                    It.IsAny<int>(), It.IsAny<int>(), It.IsAny<AttendanceStatus>(), TestUserEmail))
                .ThrowsAsync(new InvalidOperationException("Invalid registration"));

            // Act
            var result = await _controller.ValidateAttendance(1, 10, dto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region BulkValidateAttendance Tests

        [TestMethod]
        public async Task BulkValidateAttendance_WhenValid_ReturnsOk()
        {
            // Arrange
            var eventId = 1;
            var dto = new BulkValidateAttendanceDto
            {
                Attendances = new List<BulkAttendanceItemDto>
                {
                    new() { RegistrationId = 1, Status = AttendanceStatus.Present },
                    new() { RegistrationId = 2, Status = AttendanceStatus.Absent }
                }
            };
            _mockAttendanceService.Setup(s => s.BulkValidateAttendanceAsync(eventId, dto, TestUserEmail))
                .ReturnsAsync(2);

            // Act
            var result = await _controller.BulkValidateAttendance(eventId, dto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
        }

        [TestMethod]
        public async Task BulkValidateAttendance_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var dto = new BulkValidateAttendanceDto
            {
                Attendances = new List<BulkAttendanceItemDto>()
            };
            _mockAttendanceService.Setup(s => s.BulkValidateAttendanceAsync(
                    It.IsAny<int>(), dto, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.BulkValidateAttendance(1, dto);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(result);
        }

        #endregion

        #region ResetAttendance Tests

        [TestMethod]
        public async Task ResetAttendance_WhenSuccess_ReturnsNoContent()
        {
            // Arrange
            _mockAttendanceService.Setup(s => s.ResetAttendanceAsync(1, 10, TestUserEmail))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.ResetAttendance(1, 10);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task ResetAttendance_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockAttendanceService.Setup(s => s.ResetAttendanceAsync(1, 99, TestUserEmail))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.ResetAttendance(1, 99);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        [TestMethod]
        public async Task ResetAttendance_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            _mockAttendanceService.Setup(s => s.ResetAttendanceAsync(
                    It.IsAny<int>(), It.IsAny<int>(), TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.ResetAttendance(1, 10);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(result);
        }

        #endregion
    }
}
