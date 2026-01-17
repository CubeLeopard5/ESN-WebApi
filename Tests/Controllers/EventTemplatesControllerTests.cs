using Business.Interfaces;
using Dto.Common;
using Dto.Event;
using Dto.EventTemplate;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class EventTemplatesControllerTests
    {
        private Mock<IEventTemplateService> _mockTemplateService = null!;
        private Mock<ILogger<EventTemplatesController>> _mockLogger = null!;
        private EventTemplatesController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockTemplateService = new Mock<IEventTemplateService>();
            _mockLogger = new Mock<ILogger<EventTemplatesController>>();
            _controller = new EventTemplatesController(_mockTemplateService.Object, _mockLogger.Object);

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

        private void SetupUnauthenticatedUser()
        {
            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal() }
            };
        }

        #region GetAllTemplates Tests

        [TestMethod]
        public async Task GetAllTemplates_ReturnsOkResult_WithPagedTemplates()
        {
            // Arrange
            var templates = new List<EventTemplateDto>
            {
                new() { Id = 1, Title = "Template A", Description = "Description A" },
                new() { Id = 2, Title = "Template B", Description = "Description B" }
            };
            var pagedResult = new PagedResult<EventTemplateDto>(templates, 2, 1, 20);
            _mockTemplateService.Setup(s => s.GetAllTemplatesAsync(It.IsAny<PaginationParams>()))
                .ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetAllTemplates(pagination);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as PagedResult<EventTemplateDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(2, returned.Items.Count);
            Assert.AreEqual(2, returned.TotalCount);
        }

        [TestMethod]
        public async Task GetAllTemplates_ReturnsEmptyList_WhenNoTemplates()
        {
            // Arrange
            var pagedResult = new PagedResult<EventTemplateDto>(new List<EventTemplateDto>(), 0, 1, 20);
            _mockTemplateService.Setup(s => s.GetAllTemplatesAsync(It.IsAny<PaginationParams>()))
                .ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetAllTemplates(pagination);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var returned = okResult.Value as PagedResult<EventTemplateDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(0, returned.Items.Count);
            Assert.AreEqual(0, returned.TotalCount);
        }

        #endregion

        #region GetTemplateById Tests

        [TestMethod]
        public async Task GetTemplateById_ReturnsOkResult_WhenTemplateExists()
        {
            // Arrange
            var templateDto = new EventTemplateDto
            {
                Id = 1,
                Title = "Test Template",
                Description = "Test Description"
            };
            _mockTemplateService.Setup(s => s.GetTemplateByIdAsync(1)).ReturnsAsync(templateDto);

            // Act
            var result = await _controller.GetTemplateById(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as EventTemplateDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Test Template", dto.Title);
        }

        [TestMethod]
        public async Task GetTemplateById_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            _mockTemplateService.Setup(s => s.GetTemplateByIdAsync(999)).ReturnsAsync((EventTemplateDto?)null);

            // Act
            var result = await _controller.GetTemplateById(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region CreateTemplate Tests

        [TestMethod]
        public async Task CreateTemplate_ReturnsCreatedAtAction_WithValidDto()
        {
            // Arrange
            var createDto = new CreateEventTemplateDto
            {
                Title = "New Template",
                Description = "Test Description",
                SurveyJsData = "{}"
            };
            var createdTemplate = new EventTemplateDto
            {
                Id = 1,
                Title = "New Template",
                Description = "Test Description"
            };
            _mockTemplateService.Setup(s => s.CreateTemplateAsync(createDto))
                .ReturnsAsync(createdTemplate);

            // Act
            var result = await _controller.CreateTemplate(createDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual(nameof(_controller.GetTemplateById), createdResult.ActionName);
            var dto = createdResult.Value as EventTemplateDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("New Template", dto.Title);
        }

        [TestMethod]
        public async Task CreateTemplate_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var createDto = new CreateEventTemplateDto();

            // Act
            var result = await _controller.CreateTemplate(createDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task CreateTemplate_ReturnsServerError_WhenExceptionOccurs()
        {
            // Arrange
            var createDto = new CreateEventTemplateDto
            {
                Title = "New Template",
                Description = "Test Description",
                SurveyJsData = "{}"
            };
            _mockTemplateService.Setup(s => s.CreateTemplateAsync(createDto))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.CreateTemplate(createDto);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(500, objectResult.StatusCode);
        }

        #endregion

        #region UpdateTemplate Tests

        [TestMethod]
        public async Task UpdateTemplate_ReturnsOkResult_WithValidUpdate()
        {
            // Arrange
            var templateDto = new EventTemplateDto
            {
                Id = 1,
                Title = "Updated Template",
                Description = "Updated Description"
            };
            _mockTemplateService.Setup(s => s.UpdateTemplateAsync(1, templateDto))
                .ReturnsAsync(templateDto);

            // Act
            var result = await _controller.UpdateTemplate(1, templateDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as EventTemplateDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Updated Template", dto.Title);
        }

        [TestMethod]
        public async Task UpdateTemplate_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            var templateDto = new EventTemplateDto
            {
                Id = 999,
                Title = "Updated Template"
            };
            _mockTemplateService.Setup(s => s.UpdateTemplateAsync(999, templateDto))
                .ReturnsAsync((EventTemplateDto?)null);

            // Act
            var result = await _controller.UpdateTemplate(999, templateDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task UpdateTemplate_ReturnsServerError_WhenExceptionOccurs()
        {
            // Arrange
            var templateDto = new EventTemplateDto
            {
                Id = 1,
                Title = "Updated Template"
            };
            _mockTemplateService.Setup(s => s.UpdateTemplateAsync(1, templateDto))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.UpdateTemplate(1, templateDto);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(500, objectResult.StatusCode);
        }

        #endregion

        #region DeleteTemplate Tests

        [TestMethod]
        public async Task DeleteTemplate_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            _mockTemplateService.Setup(s => s.DeleteTemplateAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteTemplate(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task DeleteTemplate_ReturnsNotFound_WhenTemplateDoesNotExist()
        {
            // Arrange
            _mockTemplateService.Setup(s => s.DeleteTemplateAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteTemplate(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        [TestMethod]
        public async Task DeleteTemplate_ReturnsServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockTemplateService.Setup(s => s.DeleteTemplateAsync(1))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _controller.DeleteTemplate(1);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result);
            var objectResult = (ObjectResult)result;
            Assert.AreEqual(500, objectResult.StatusCode);
        }

        #endregion

        #region ApplyTemplate Tests

        [TestMethod]
        public async Task ApplyTemplate_ReturnsCreatedAtAction_WithValidData()
        {
            // Arrange
            var applyDto = new ApplyTemplateDto
            {
                Title = "Event from Template",
                StartDate = DateTime.Now.AddDays(1),
                Location = "Test Location"
            };
            var eventDto = new EventDto
            {
                Id = 1,
                Title = "Event from Template"
            };
            _mockTemplateService.Setup(s => s.CreateEventFromTemplateAsync(
                It.IsAny<CreateEventFromTemplateDto>(), TestUserEmail))
                .ReturnsAsync(eventDto);

            // Act
            var result = await _controller.ApplyTemplate(1, applyDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual("GetEvent", createdResult.ActionName);
            Assert.AreEqual("Events", createdResult.ControllerName);
            var dto = createdResult.Value as EventDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Event from Template", dto.Title);
        }

        [TestMethod]
        public async Task ApplyTemplate_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var applyDto = new ApplyTemplateDto();

            // Act
            var result = await _controller.ApplyTemplate(1, applyDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task ApplyTemplate_ReturnsUnauthorized_WhenUserNotAuthenticated()
        {
            // Arrange
            SetupUnauthenticatedUser();
            var applyDto = new ApplyTemplateDto
            {
                Title = "Event from Template",
                StartDate = DateTime.Now.AddDays(1)
            };

            // Act
            var result = await _controller.ApplyTemplate(1, applyDto);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task ApplyTemplate_ReturnsNotFound_WhenTemplateNotFound()
        {
            // Arrange
            var applyDto = new ApplyTemplateDto
            {
                Title = "Event from Template",
                StartDate = DateTime.Now.AddDays(1)
            };
            _mockTemplateService.Setup(s => s.CreateEventFromTemplateAsync(
                It.IsAny<CreateEventFromTemplateDto>(), TestUserEmail))
                .ThrowsAsync(new KeyNotFoundException("Template not found"));

            // Act
            var result = await _controller.ApplyTemplate(999, applyDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task ApplyTemplate_ReturnsBadRequest_WhenArgumentException()
        {
            // Arrange
            var applyDto = new ApplyTemplateDto
            {
                Title = "Event from Template",
                StartDate = DateTime.Now.AddDays(1)
            };
            _mockTemplateService.Setup(s => s.CreateEventFromTemplateAsync(
                It.IsAny<CreateEventFromTemplateDto>(), TestUserEmail))
                .ThrowsAsync(new ArgumentException("Invalid template"));

            // Act
            var result = await _controller.ApplyTemplate(1, applyDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region SaveEventAsTemplate Tests

        [TestMethod]
        public async Task SaveEventAsTemplate_ReturnsCreatedAtAction_WhenSuccessful()
        {
            // Arrange
            var templateDto = new EventTemplateDto
            {
                Id = 1,
                Title = "Template from Event",
                Description = "Test Description"
            };
            _mockTemplateService.Setup(s => s.SaveEventAsTemplateAsync(1))
                .ReturnsAsync(templateDto);

            // Act
            var result = await _controller.SaveEventAsTemplate(1);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual(nameof(_controller.GetTemplateById), createdResult.ActionName);
            var dto = createdResult.Value as EventTemplateDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Template from Event", dto.Title);
        }

        [TestMethod]
        public async Task SaveEventAsTemplate_ReturnsNotFound_WhenEventNotFound_KeyNotFoundException()
        {
            // Arrange
            _mockTemplateService.Setup(s => s.SaveEventAsTemplateAsync(999))
                .ThrowsAsync(new KeyNotFoundException("Event not found"));

            // Act
            var result = await _controller.SaveEventAsTemplate(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task SaveEventAsTemplate_ReturnsNotFound_WhenEventNotFound_ArgumentException()
        {
            // Arrange
            _mockTemplateService.Setup(s => s.SaveEventAsTemplateAsync(999))
                .ThrowsAsync(new ArgumentException("Event not found"));

            // Act
            var result = await _controller.SaveEventAsTemplate(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result.Result);
        }

        #endregion
    }
}
