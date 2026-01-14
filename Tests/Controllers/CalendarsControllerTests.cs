using Business.Interfaces;
using Dto.Calendar;
using Dto.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class CalendarsControllerTests
    {
        private Mock<ICalendarService> _mockCalendarService = null!;
        private Mock<ILogger<CalendarsController>> _mockLogger = null!;
        private CalendarsController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockCalendarService = new Mock<ICalendarService>();
            _mockLogger = new Mock<ILogger<CalendarsController>>();
            _controller = new CalendarsController(_mockCalendarService.Object, _mockLogger.Object);

            // Mock authenticated user
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

        #region GetCalendars Tests

        [TestMethod]
        public async Task GetCalendars_ReturnsOkResult_WithListOfCalendars()
        {
            // Arrange
            var calendars = new List<CalendarDto>
            {
                new() { Id = 1, Title = "Calendar A", EventDate = DateTime.UtcNow },
                new() { Id = 2, Title = "Calendar B", EventDate = DateTime.UtcNow.AddDays(1) }
            };
            var pagedResult = new PagedResult<CalendarDto>(calendars, 2, 1, 20);
            _mockCalendarService.Setup(s => s.GetAllCalendarsAsync(It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetCalendars(pagination);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as PagedResult<CalendarDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(2, returned.Items.Count);
            Assert.AreEqual(2, returned.TotalCount);
        }

        [TestMethod]
        public async Task GetCalendars_ReturnsEmptyList_WhenNoCalendars()
        {
            // Arrange
            var pagedResult = new PagedResult<CalendarDto>(new List<CalendarDto>(), 0, 1, 20);
            _mockCalendarService.Setup(s => s.GetAllCalendarsAsync(It.IsAny<PaginationParams>()))
                .ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetCalendars(pagination);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var returned = okResult.Value as PagedResult<CalendarDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(0, returned.Items.Count);
            Assert.AreEqual(0, returned.TotalCount);
        }

        #endregion

        #region GetCalendar Tests

        [TestMethod]
        public async Task GetCalendar_ReturnsOkResult_WithValidId()
        {
            // Arrange
            var calendarDto = new CalendarDto { Id = 1, Title = "Test Calendar", EventDate = DateTime.UtcNow };
            _mockCalendarService.Setup(s => s.GetCalendarByIdAsync(1)).ReturnsAsync(calendarDto);

            // Act
            var result = await _controller.GetCalendar(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as CalendarDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Test Calendar", dto.Title);
        }

        [TestMethod]
        public async Task GetCalendar_ReturnsNotFound_WhenInvalidId()
        {
            // Arrange
            _mockCalendarService.Setup(s => s.GetCalendarByIdAsync(999)).ReturnsAsync((CalendarDto?)null);

            // Act
            var result = await _controller.GetCalendar(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region GetCalendarsByEvent Tests

        [TestMethod]
        public async Task GetCalendarsByEvent_ReturnsOkResult_WithCalendarsForEvent()
        {
            // Arrange
            var calendars = new List<CalendarDto>
            {
                new() { Id = 1, Title = "Calendar 1", EventDate = DateTime.UtcNow, EventId = 5 },
                new() { Id = 2, Title = "Calendar 2", EventDate = DateTime.UtcNow.AddDays(1), EventId = 5 }
            };
            _mockCalendarService.Setup(s => s.GetCalendarsByEventIdAsync(5)).ReturnsAsync(calendars);

            // Act
            var result = await _controller.GetCalendarsByEvent(5);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as IEnumerable<CalendarDto>;
            Assert.AreEqual(2, returned?.Count());
            Assert.IsTrue(returned?.All(c => c.EventId == 5));
        }

        [TestMethod]
        public async Task GetCalendarsByEvent_ReturnsEmptyList_WhenNoCalendarsForEvent()
        {
            // Arrange
            _mockCalendarService.Setup(s => s.GetCalendarsByEventIdAsync(999))
                .ReturnsAsync(new List<CalendarDto>());

            // Act
            var result = await _controller.GetCalendarsByEvent(999);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var returned = okResult.Value as IEnumerable<CalendarDto>;
            Assert.AreEqual(0, returned?.Count());
        }

        #endregion

        #region PostCalendar Tests

        [TestMethod]
        public async Task PostCalendar_ReturnsCreatedAtAction_WithValidDto()
        {
            // Arrange
            var createDto = new CalendarCreateDto
            {
                Title = "New Calendar",
                EventDate = DateTime.UtcNow.AddDays(7)
            };
            var createdCalendar = new CalendarDto
            {
                Id = 1,
                Title = "New Calendar",
                EventDate = createDto.EventDate
            };
            _mockCalendarService.Setup(s => s.CreateCalendarAsync(createDto))
                .ReturnsAsync(createdCalendar);

            // Act
            var result = await _controller.PostCalendar(createDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual(nameof(_controller.GetCalendar), createdResult.ActionName);
            var dto = createdResult.Value as CalendarDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("New Calendar", dto.Title);
        }

        [TestMethod]
        public async Task PostCalendar_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var createDto = new CalendarCreateDto();

            // Act
            var result = await _controller.PostCalendar(createDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task PostCalendar_CreatesCalendarWithSubOrganizers()
        {
            // Arrange
            var createDto = new CalendarCreateDto
            {
                Title = "Calendar with Sub-Organizers",
                EventDate = DateTime.UtcNow,
                SubOrganizerIds = new List<int> { 2, 3, 4 }
            };
            var createdCalendar = new CalendarDto
            {
                Id = 1,
                Title = "Calendar with Sub-Organizers",
                EventDate = createDto.EventDate,
                SubOrganizers = new List<Dto.User.UserDto>()
            };
            _mockCalendarService.Setup(s => s.CreateCalendarAsync(createDto))
                .ReturnsAsync(createdCalendar);

            // Act
            var result = await _controller.PostCalendar(createDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            _mockCalendarService.Verify(s => s.CreateCalendarAsync(
                It.Is<CalendarCreateDto>(dto => dto.SubOrganizerIds!.Count == 3)), Times.Once);
        }

        #endregion

        #region PutCalendar Tests

        [TestMethod]
        public async Task PutCalendar_ReturnsOkResult_WithValidDto()
        {
            // Arrange
            var updateDto = new CalendarUpdateDto
            {
                Title = "Updated Calendar",
                EventDate = DateTime.UtcNow
            };
            var updatedCalendar = new CalendarDto
            {
                Id = 1,
                Title = "Updated Calendar",
                EventDate = updateDto.EventDate
            };
            _mockCalendarService.Setup(s => s.UpdateCalendarAsync(1, updateDto, It.IsAny<string>()))
                .ReturnsAsync(updatedCalendar);

            // Act
            var result = await _controller.PutCalendar(1, updateDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as CalendarDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Updated Calendar", dto.Title);
        }

        [TestMethod]
        public async Task PutCalendar_ReturnsNotFound_WhenCalendarDoesNotExist()
        {
            // Arrange
            var updateDto = new CalendarUpdateDto { Title = "Test", EventDate = DateTime.UtcNow };
            _mockCalendarService.Setup(s => s.UpdateCalendarAsync(999, updateDto, It.IsAny<string>()))
                .ReturnsAsync((CalendarDto?)null);

            // Act
            var result = await _controller.PutCalendar(999, updateDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task PutCalendar_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var updateDto = new CalendarUpdateDto();

            // Act
            var result = await _controller.PutCalendar(1, updateDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task PutCalendar_UpdatesSubOrganizers()
        {
            // Arrange
            var updateDto = new CalendarUpdateDto
            {
                Title = "Updated Calendar",
                EventDate = DateTime.UtcNow,
                SubOrganizerIds = new List<int> { 5, 6 }
            };
            var updatedCalendar = new CalendarDto
            {
                Id = 1,
                Title = "Updated Calendar",
                EventDate = updateDto.EventDate
            };
            _mockCalendarService.Setup(s => s.UpdateCalendarAsync(1, updateDto, It.IsAny<string>()))
                .ReturnsAsync(updatedCalendar);

            // Act
            var result = await _controller.PutCalendar(1, updateDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            _mockCalendarService.Verify(s => s.UpdateCalendarAsync(1,
                It.Is<CalendarUpdateDto>(dto => dto.SubOrganizerIds!.Count == 2), It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region DeleteCalendar Tests

        [TestMethod]
        public async Task DeleteCalendar_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            var deletedCalendar = new CalendarDto
            {
                Id = 1,
                Title = "Deleted Calendar",
                EventDate = DateTime.UtcNow
            };
            _mockCalendarService.Setup(s => s.DeleteCalendarAsync(1, It.IsAny<string>()))
                .ReturnsAsync(deletedCalendar);

            // Act
            var result = await _controller.DeleteCalendar(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task DeleteCalendar_ReturnsNotFound_WhenCalendarDoesNotExist()
        {
            // Arrange
            _mockCalendarService.Setup(s => s.DeleteCalendarAsync(999, It.IsAny<string>()))
                .ReturnsAsync((CalendarDto?)null);

            // Act
            var result = await _controller.DeleteCalendar(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        #endregion
    }
}
