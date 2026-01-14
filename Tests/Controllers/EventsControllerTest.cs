using Business.Interfaces;
using Dto.Common;
using Dto.Event;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class EventsControllerTests
    {
        private Mock<IEventService> _mockEventService = null!;
        private Mock<IEventTemplateService> _mockEventTemplateService = null!;
        private Mock<ILogger<EventsController>> _mockLogger = null!;
        private EventsController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockEventService = new Mock<IEventService>();
            _mockEventTemplateService = new Mock<IEventTemplateService>();
            _mockLogger = new Mock<ILogger<EventsController>>();
            _controller = new EventsController(_mockEventService.Object, _mockEventTemplateService.Object, _mockLogger.Object);

            // Mock authenticated user
            SetupAuthenticatedUser(TestUserEmail);
        }

        private void SetupAuthenticatedUser(string email)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim("userId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #region GetEvents Tests

        [TestMethod]
        public async Task GetEvents_ReturnsOkResult_WithListOfEvents()
        {
            // Arrange
            var events = new List<EventDto>
            {
                new() { Id = 1, Title = "Event A", StartDate = DateTime.UtcNow },
                new() { Id = 2, Title = "Event B", StartDate = DateTime.UtcNow.AddDays(1) }
            };
            var pagedResult = new PagedResult<EventDto>(events, 2, 1, 20);
            _mockEventService.Setup(s => s.GetAllEventsAsync(It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetEvents(pagination);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as PagedResult<EventDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(2, returned.Items.Count);
            Assert.AreEqual(2, returned.TotalCount);
        }

        #endregion

        #region GetEvent Tests

        [TestMethod]
        public async Task GetEvent_ReturnsOkResult_WithValidId()
        {
            // Arrange
            var eventDto = new EventDto { Id = 1, Title = "Test Event", StartDate = DateTime.UtcNow };
            _mockEventService.Setup(s => s.GetEventByIdAsync(1)).ReturnsAsync(eventDto);

            // Act
            var result = await _controller.GetEvent(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as EventDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Test Event", dto.Title);
        }

        [TestMethod]
        public async Task GetEvent_ReturnsNotFound_WhenInvalidId()
        {
            // Arrange
            _mockEventService.Setup(s => s.GetEventByIdAsync(999)).ReturnsAsync((EventDto?)null);

            // Act
            var result = await _controller.GetEvent(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region PostEvent Tests

        [TestMethod]
        public async Task PostEvent_ReturnsCreatedAtAction_WithValidDto()
        {
            // Arrange
            var createDto = new CreateEventDto
            {
                Title = "New Event",
                StartDate = DateTime.UtcNow.AddDays(1)
            };
            var createdEvent = new EventDto
            {
                Id = 1,
                Title = "New Event",
                StartDate = createDto.StartDate
            };
            _mockEventService.Setup(s => s.CreateEventAsync(createDto, TestUserEmail))
                .ReturnsAsync(createdEvent);

            // Act
            var result = await _controller.PostEvent(createDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual(nameof(_controller.GetEvent), createdResult.ActionName);
            var dto = createdResult.Value as EventDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("New Event", dto.Title);
        }

        [TestMethod]
        public async Task PostEvent_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var createDto = new CreateEventDto();

            // Act
            var result = await _controller.PostEvent(createDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task PostEvent_ReturnsUnauthorized_WhenUserNotFound()
        {
            // Arrange
            var createDto = new CreateEventDto { Title = "Test", StartDate = DateTime.UtcNow };
            _mockEventService.Setup(s => s.CreateEventAsync(It.IsAny<CreateEventDto>(), It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedAccessException("User not found"));

            // Act
            var result = await _controller.PostEvent(createDto);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        #endregion

        #region PutEvent Tests

        [TestMethod]
        public async Task PutEvent_ReturnsOkResult_WithValidDto()
        {
            // Arrange
            var eventDto = new EventDto
            {
                Id = 1,
                Title = "Updated Event",
                StartDate = DateTime.UtcNow
            };
            _mockEventService.Setup(s => s.UpdateEventAsync(1, eventDto, TestUserEmail))
                .ReturnsAsync(eventDto);

            // Act
            var result = await _controller.PutEvent(1, eventDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as EventDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Updated Event", dto.Title);
        }

        [TestMethod]
        public async Task PutEvent_ReturnsNotFound_WhenEventDoesNotExist()
        {
            // Arrange
            var eventDto = new EventDto { Id = 999, Title = "Test", StartDate = DateTime.UtcNow };
            _mockEventService.Setup(s => s.UpdateEventAsync(999, eventDto, TestUserEmail))
                .ReturnsAsync((EventDto?)null);

            // Act
            var result = await _controller.PutEvent(999, eventDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task PutEvent_ReturnsForbid_WhenUnauthorized()
        {
            // Arrange
            var eventDto = new EventDto { Id = 1, Title = "Test", StartDate = DateTime.UtcNow };
            _mockEventService.Setup(s => s.UpdateEventAsync(1, eventDto, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.PutEvent(1, eventDto);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        #endregion

        #region DeleteEvent Tests

        [TestMethod]
        public async Task DeleteEvent_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            _mockEventService.Setup(s => s.DeleteEventAsync(1, TestUserEmail))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteEvent(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task DeleteEvent_ReturnsNotFound_WhenEventDoesNotExist()
        {
            // Arrange
            _mockEventService.Setup(s => s.DeleteEventAsync(999, TestUserEmail))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteEvent(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        [TestMethod]
        public async Task DeleteEvent_ReturnsForbid_WhenUnauthorized()
        {
            // Arrange
            _mockEventService.Setup(s => s.DeleteEventAsync(1, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException());

            // Act
            var result = await _controller.DeleteEvent(1);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result);
        }

        #endregion

        #region RegisterForEvent Tests

        [TestMethod]
        public async Task RegisterForEvent_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var registerDto = new RegisterEventDto { SurveyJsData = "{}" };
            _mockEventService.Setup(s => s.RegisterForEventAsync(1, TestUserEmail, registerDto.SurveyJsData))
                .ReturnsAsync("Successfully registered for event");

            // Act
            var result = await _controller.RegisterForEvent(1, registerDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
        }

        [TestMethod]
        public async Task RegisterForEvent_ReturnsNotFound_WhenEventDoesNotExist()
        {
            // Arrange
            var registerDto = new RegisterEventDto { SurveyJsData = "{}" };
            _mockEventService.Setup(s => s.RegisterForEventAsync(999, TestUserEmail, registerDto.SurveyJsData))
                .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.RegisterForEvent(999, registerDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        [TestMethod]
        public async Task RegisterForEvent_ReturnsBadRequest_WhenAlreadyRegistered()
        {
            // Arrange
            var registerDto = new RegisterEventDto { SurveyJsData = "{}" };
            _mockEventService.Setup(s => s.RegisterForEventAsync(1, TestUserEmail, registerDto.SurveyJsData))
                .ThrowsAsync(new InvalidOperationException("Already registered"));

            // Act
            var result = await _controller.RegisterForEvent(1, registerDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result);
        }

        #endregion

        #region UnregisterFromEvent Tests

        [TestMethod]
        public async Task UnregisterFromEvent_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            _mockEventService.Setup(s => s.UnregisterFromEventAsync(1, TestUserEmail))
                .ReturnsAsync("Successfully unregistered from event");

            // Act
            var result = await _controller.UnregisterFromEvent(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
        }

        [TestMethod]
        public async Task UnregisterFromEvent_ReturnsNotFound_WhenNoRegistrationFound()
        {
            // Arrange
            _mockEventService.Setup(s => s.UnregisterFromEventAsync(1, TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException());

            // Act
            var result = await _controller.UnregisterFromEvent(1);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        #endregion

        #region GetEventRegistrations Tests

        [TestMethod]
        public async Task GetEventRegistrations_ReturnsOk_WithRegistrations()
        {
            // Arrange
            var eventWithRegistrations = new EventWithRegistrationsDto
            {
                Id = 1,
                Title = "Test Event",
                Registrations = new List<EventRegistrationDto>()
            };
            _mockEventService.Setup(s => s.GetEventRegistrationsAsync(1))
                .ReturnsAsync(eventWithRegistrations);

            // Act
            var result = await _controller.GetEventRegistrations(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as EventWithRegistrationsDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Test Event", dto.Title);
        }

        [TestMethod]
        public async Task GetEventRegistrations_ReturnsNotFound_WhenEventDoesNotExist()
        {
            // Arrange
            _mockEventService.Setup(s => s.GetEventRegistrationsAsync(999))
                .ReturnsAsync((EventWithRegistrationsDto?)null);

            // Act
            var result = await _controller.GetEventRegistrations(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion
    }
}
