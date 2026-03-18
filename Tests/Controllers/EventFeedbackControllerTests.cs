using Business.Interfaces;
using Dto.EventFeedback;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class EventFeedbackControllerTests
    {
        private Mock<IEventFeedbackService> _mockFeedbackService = null!;
        private Mock<ILogger<EventFeedbackController>> _mockLogger = null!;
        private EventFeedbackController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockFeedbackService = new Mock<IEventFeedbackService>();
            _mockLogger = new Mock<ILogger<EventFeedbackController>>();
            _controller = new EventFeedbackController(_mockFeedbackService.Object, _mockLogger.Object);

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

        #region CheckEligibility Tests

        [TestMethod]
        public async Task CheckEligibility_WhenEventExists_ReturnsOk()
        {
            // Arrange
            var eventId = 1;
            var eligibility = new FeedbackEligibilityDto { CanSubmit = true };
            _mockFeedbackService.Setup(s => s.CheckEligibilityAsync(eventId, TestUserEmail))
                .ReturnsAsync(eligibility);

            // Act
            var result = await _controller.CheckEligibility(eventId);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            Assert.AreEqual(eligibility, okResult.Value);
        }

        [TestMethod]
        public async Task CheckEligibility_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.CheckEligibilityAsync(99, TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.CheckEligibility(99);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region SubmitFeedback Tests

        [TestMethod]
        public async Task SubmitFeedback_WhenValid_ReturnsCreated()
        {
            // Arrange
            var eventId = 1;
            var submitDto = new SubmitFeedbackDto { FeedbackData = "{\"q1\": \"good\"}" };
            var feedbackDto = new EventFeedbackDto { Id = 1, EventId = eventId };
            _mockFeedbackService.Setup(s => s.SubmitFeedbackAsync(eventId, TestUserEmail, submitDto))
                .ReturnsAsync(feedbackDto);

            // Act
            var result = await _controller.SubmitFeedback(eventId, submitDto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual(feedbackDto, createdResult.Value);
        }

        [TestMethod]
        public async Task SubmitFeedback_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            var submitDto = new SubmitFeedbackDto { FeedbackData = "{}" };
            _mockFeedbackService.Setup(s => s.SubmitFeedbackAsync(99, TestUserEmail, submitDto))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.SubmitFeedback(99, submitDto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task SubmitFeedback_WhenNotEligible_ReturnsBadRequest()
        {
            // Arrange
            var submitDto = new SubmitFeedbackDto { FeedbackData = "{}" };
            _mockFeedbackService.Setup(s => s.SubmitFeedbackAsync(1, TestUserEmail, submitDto))
                .ThrowsAsync(new InvalidOperationException("Not eligible"));

            // Act
            var result = await _controller.SubmitFeedback(1, submitDto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region GetMyFeedback Tests

        [TestMethod]
        public async Task GetMyFeedback_WhenFeedbackExists_ReturnsOk()
        {
            // Arrange
            var eventId = 1;
            var feedbackDto = new EventFeedbackDto { Id = 1, EventId = eventId };
            _mockFeedbackService.Setup(s => s.GetUserFeedbackAsync(eventId, TestUserEmail))
                .ReturnsAsync(feedbackDto);

            // Act
            var result = await _controller.GetMyFeedback(eventId);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetMyFeedback_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.GetUserFeedbackAsync(1, TestUserEmail))
                .ReturnsAsync((EventFeedbackDto?)null);

            // Act
            var result = await _controller.GetMyFeedback(1);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetMyFeedback_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.GetUserFeedbackAsync(99, TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.GetMyFeedback(99);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region UpdateFeedback Tests

        [TestMethod]
        public async Task UpdateFeedback_WhenValid_ReturnsOk()
        {
            // Arrange
            var eventId = 1;
            var submitDto = new SubmitFeedbackDto { FeedbackData = "{\"q1\": \"updated\"}" };
            var feedbackDto = new EventFeedbackDto { Id = 1, EventId = eventId };
            _mockFeedbackService.Setup(s => s.UpdateFeedbackAsync(eventId, TestUserEmail, submitDto))
                .ReturnsAsync(feedbackDto);

            // Act
            var result = await _controller.UpdateFeedback(eventId, submitDto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task UpdateFeedback_WhenNotFound_ReturnsNotFound()
        {
            // Arrange
            var submitDto = new SubmitFeedbackDto { FeedbackData = "{}" };
            _mockFeedbackService.Setup(s => s.UpdateFeedbackAsync(1, TestUserEmail, submitDto))
                .ThrowsAsync(new KeyNotFoundException("Feedback not found"));

            // Act
            var result = await _controller.UpdateFeedback(1, submitDto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task UpdateFeedback_WhenDeadlinePassed_ReturnsBadRequest()
        {
            // Arrange
            var submitDto = new SubmitFeedbackDto { FeedbackData = "{}" };
            _mockFeedbackService.Setup(s => s.UpdateFeedbackAsync(1, TestUserEmail, submitDto))
                .ThrowsAsync(new InvalidOperationException("Deadline passed"));

            // Act
            var result = await _controller.UpdateFeedback(1, submitDto);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region GetAllFeedbacks Tests

        [TestMethod]
        public async Task GetAllFeedbacks_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            var eventId = 1;
            var feedbacks = new List<EventFeedbackDto>
            {
                new() { Id = 1, EventId = eventId },
                new() { Id = 2, EventId = eventId }
            };
            _mockFeedbackService.Setup(s => s.GetAllFeedbacksAsync(eventId, TestUserEmail))
                .ReturnsAsync(feedbacks);

            // Act
            var result = await _controller.GetAllFeedbacks(eventId);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetAllFeedbacks_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.GetAllFeedbacksAsync(1, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.GetAllFeedbacks(1);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ForbidResult>(result.Result);
        }

        [TestMethod]
        public async Task GetAllFeedbacks_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.GetAllFeedbacksAsync(99, TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.GetAllFeedbacks(99);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region GetFeedbackSummary Tests

        [TestMethod]
        public async Task GetFeedbackSummary_WhenAuthorized_ReturnsOk()
        {
            // Arrange
            var eventId = 1;
            var summary = new FeedbackSummaryDto { EventId = eventId };
            _mockFeedbackService.Setup(s => s.GetFeedbackSummaryAsync(eventId, TestUserEmail))
                .ReturnsAsync(summary);

            // Act
            var result = await _controller.GetFeedbackSummary(eventId);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetFeedbackSummary_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.GetFeedbackSummaryAsync(1, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.GetFeedbackSummary(1);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ForbidResult>(result.Result);
        }

        [TestMethod]
        public async Task GetFeedbackSummary_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockFeedbackService.Setup(s => s.GetFeedbackSummaryAsync(99, TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.GetFeedbackSummary(99);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion

        #region UpdateFeedbackForm Tests

        [TestMethod]
        public async Task UpdateFeedbackForm_WhenAuthorized_ReturnsNoContent()
        {
            // Arrange
            var eventId = 1;
            var dto = new UpdateFeedbackFormDto { FeedbackFormData = "{}" };
            _mockFeedbackService.Setup(s => s.UpdateFeedbackFormAsync(eventId, TestUserEmail, dto))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.UpdateFeedbackForm(eventId, dto);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task UpdateFeedbackForm_WhenUnauthorized_ReturnsForbid()
        {
            // Arrange
            var dto = new UpdateFeedbackFormDto { FeedbackFormData = "{}" };
            _mockFeedbackService.Setup(s => s.UpdateFeedbackFormAsync(1, TestUserEmail, dto))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.UpdateFeedbackForm(1, dto);

            // Assert
            Assert.IsInstanceOfType<ForbidResult>(result);
        }

        [TestMethod]
        public async Task UpdateFeedbackForm_WhenEventNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new UpdateFeedbackFormDto { FeedbackFormData = "{}" };
            _mockFeedbackService.Setup(s => s.UpdateFeedbackFormAsync(99, TestUserEmail, dto))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.UpdateFeedbackForm(99, dto);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        #endregion
    }
}
