using AutoMapper;
using Bo.Models;
using Business.EventTemplate;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Event;
using Dto.EventTemplate;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services
{
    [TestClass]
    public class EventTemplateServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IMapper> _mockMapper = null!;
        private Mock<ILogger<EventTemplateService>> _mockLogger = null!;
        private Mock<IEventTemplateRepository> _mockEventTemplateRepository = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IEventRepository> _mockEventRepository = null!;
        private EventTemplateService _eventTemplateService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<EventTemplateService>>();
            _mockEventTemplateRepository = new Mock<IEventTemplateRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEventRepository = new Mock<IEventRepository>();

            _mockUnitOfWork.Setup(u => u.EventTemplates).Returns(_mockEventTemplateRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepository.Object);

            _eventTemplateService = new EventTemplateService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        #region GetAllTemplates Tests

        [TestMethod]
        public async Task GetAllTemplatesAsync_ReturnsListOfTemplates()
        {
            // Arrange
            var templatesBo = new List<EventTemplateBo>
            {
                new EventTemplateBo
                {
                    Id = 1,
                    Title = "Template 1",
                    Description = "Description 1"
                },
                new EventTemplateBo
                {
                    Id = 2,
                    Title = "Template 2",
                    Description = "Description 2"
                }
            };

            var templateDtos = new List<EventTemplateDto>
            {
                new EventTemplateDto { Id = 1, Title = "Template 1" },
                new EventTemplateDto { Id = 2, Title = "Template 2" }
            };

            _mockEventTemplateRepository.Setup(r => r.GetAllTemplatesAsync())
                .ReturnsAsync(templatesBo);
            _mockMapper.Setup(m => m.Map<IEnumerable<EventTemplateDto>>(templatesBo))
                .Returns(templateDtos);

            // Act
            var result = await _eventTemplateService.GetAllTemplatesAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetTemplateById Tests

        [TestMethod]
        public async Task GetTemplateByIdAsync_ExistingTemplate_ReturnsTemplateDto()
        {
            // Arrange
            var templateId = 1;
            var templateBo = new EventTemplateBo
            {
                Id = templateId,
                Title = "Test Template",
                Description = "Test Description"
            };

            var templateDto = new EventTemplateDto
            {
                Id = templateId,
                Title = "Test Template",
                Description = "Test Description"
            };

            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(templateId))
                .ReturnsAsync(templateBo);
            _mockMapper.Setup(m => m.Map<EventTemplateDto>(templateBo))
                .Returns(templateDto);

            // Act
            var result = await _eventTemplateService.GetTemplateByIdAsync(templateId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(templateId, result.Id);
            Assert.AreEqual("Test Template", result.Title);
        }

        [TestMethod]
        public async Task GetTemplateByIdAsync_NonExistingTemplate_ReturnsNull()
        {
            // Arrange
            var templateId = 999;
            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(templateId))
                .ReturnsAsync((EventTemplateBo?)null);

            // Act
            var result = await _eventTemplateService.GetTemplateByIdAsync(templateId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region CreateTemplate Tests

        [TestMethod]
        public async Task CreateTemplateAsync_ValidTemplate_ReturnsTemplateDto()
        {
            // Arrange
            var createDto = new CreateEventTemplateDto
            {
                Title = "New Template",
                Description = "Test Description",
                SurveyJsData = "{}"
            };

            var templateBo = new EventTemplateBo
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description,
                SurveyJsData = createDto.SurveyJsData
            };

            var templateDto = new EventTemplateDto
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description
            };

            _mockMapper.Setup(m => m.Map<EventTemplateBo>(createDto))
                .Returns(templateBo);
            _mockEventTemplateRepository.Setup(r => r.AddAsync(It.IsAny<EventTemplateBo>()))
                .ReturnsAsync(templateBo);
            _mockMapper.Setup(m => m.Map<EventTemplateDto>(It.IsAny<EventTemplateBo>()))
                .Returns(templateDto);

            // Act
            var result = await _eventTemplateService.CreateTemplateAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(createDto.Title, result.Title);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        #endregion

        #region UpdateTemplate Tests

        [TestMethod]
        public async Task UpdateTemplateAsync_ValidUpdate_ReturnsUpdatedTemplateDto()
        {
            // Arrange
            var templateId = 1;
            var updateDto = new EventTemplateDto
            {
                Id = templateId,
                Title = "Updated Template",
                Description = "Updated Description",
                SurveyJsData = "{}"
            };

            var existingTemplate = new EventTemplateBo
            {
                Id = templateId,
                Title = "Old Template",
                Description = "Old Description",
                SurveyJsData = "{}"
            };

            var updatedTemplateDto = new EventTemplateDto
            {
                Id = templateId,
                Title = "Updated Template",
                Description = "Updated Description"
            };

            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(templateId))
                .ReturnsAsync(existingTemplate);
            _mockMapper.Setup(m => m.Map<EventTemplateDto>(existingTemplate))
                .Returns(updatedTemplateDto);

            // Act
            var result = await _eventTemplateService.UpdateTemplateAsync(templateId, updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Template", result.Title);
            _mockEventTemplateRepository.Verify(r => r.Update(existingTemplate), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateTemplateAsync_NonExistingTemplate_ReturnsNull()
        {
            // Arrange
            var templateId = 999;
            var updateDto = new EventTemplateDto
            {
                Id = templateId,
                Title = "Updated Template"
            };

            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(templateId))
                .ReturnsAsync((EventTemplateBo?)null);

            // Act
            var result = await _eventTemplateService.UpdateTemplateAsync(templateId, updateDto);

            // Assert
            Assert.IsNull(result);
            _mockEventTemplateRepository.Verify(r => r.Update(It.IsAny<EventTemplateBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region DeleteTemplate Tests

        [TestMethod]
        public async Task DeleteTemplateAsync_ExistingTemplate_ReturnsTrue()
        {
            // Arrange
            var templateId = 1;
            var templateBo = new EventTemplateBo
            {
                Id = templateId,
                Title = "Test Template",
                Description = "Test Description"
            };

            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(templateId))
                .ReturnsAsync(templateBo);

            // Act
            var result = await _eventTemplateService.DeleteTemplateAsync(templateId);

            // Assert
            Assert.IsTrue(result);
            _mockEventTemplateRepository.Verify(r => r.Delete(templateBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeleteTemplateAsync_NonExistingTemplate_ReturnsFalse()
        {
            // Arrange
            var templateId = 999;
            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(templateId))
                .ReturnsAsync((EventTemplateBo?)null);

            // Act
            var result = await _eventTemplateService.DeleteTemplateAsync(templateId);

            // Assert
            Assert.IsFalse(result);
            _mockEventTemplateRepository.Verify(r => r.Delete(It.IsAny<EventTemplateBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region CreateEventFromTemplate Tests

        [TestMethod]
        public async Task CreateEventFromTemplateAsync_ValidData_ReturnsEventDto()
        {
            // Arrange
            var userEmail = "test@example.com";
            var createDto = new CreateEventFromTemplateDto
            {
                TemplateId = 1,
                Title = "Event from Template",
                Location = "Test Location",
                StartDate = DateTime.Now.AddDays(1)
            };

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            var template = new EventTemplateBo
            {
                Id = 1,
                Title = "Template",
                Description = "Template Description",
                SurveyJsData = "{}"
            };

            var eventDto = new EventDto
            {
                Id = 1,
                Title = createDto.Title
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(createDto.TemplateId))
                .ReturnsAsync(template);
            _mockMapper.Setup(m => m.Map<EventDto>(It.IsAny<EventBo>()))
                .Returns(eventDto);

            // Act
            var result = await _eventTemplateService.CreateEventFromTemplateAsync(createDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(createDto.Title, result.Title);
            _mockEventRepository.Verify(r => r.AddAsync(It.IsAny<EventBo>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreateEventFromTemplateAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userEmail = "notfound@example.com";
            var createDto = new CreateEventFromTemplateDto
            {
                TemplateId = 1,
                Title = "Event from Template",
                StartDate = DateTime.Now.AddDays(1)
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync((UserBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventTemplateService.CreateEventFromTemplateAsync(createDto, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        [TestMethod]
        public async Task CreateEventFromTemplateAsync_TemplateNotFound_ThrowsArgumentException()
        {
            // Arrange
            var userEmail = "test@example.com";
            var createDto = new CreateEventFromTemplateDto
            {
                TemplateId = 999,
                Title = "Event from Template",
                StartDate = DateTime.Now.AddDays(1)
            };

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventTemplateRepository.Setup(r => r.GetByIdAsync(createDto.TemplateId))
                .ReturnsAsync((EventTemplateBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventTemplateService.CreateEventFromTemplateAsync(createDto, userEmail);
            }
            catch (ArgumentException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected ArgumentException was not thrown");
        }

        #endregion

        #region SaveEventAsTemplate Tests

        [TestMethod]
        public async Task SaveEventAsTemplateAsync_ValidEvent_ReturnsTemplateDto()
        {
            // Arrange
            var eventId = 1;
            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                SurveyJsData = "{}",
                UserId = 1,
                StartDate = DateTime.Now.AddDays(1)
            };

            var templateDto = new EventTemplateDto
            {
                Id = 1,
                Title = eventBo.Title,
                Description = eventBo.Description
            };

            _mockEventRepository.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockMapper.Setup(m => m.Map<EventTemplateDto>(It.IsAny<EventTemplateBo>()))
                .Returns(templateDto);

            // Act
            var result = await _eventTemplateService.SaveEventAsTemplateAsync(eventId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(eventBo.Title, result.Title);
            _mockEventTemplateRepository.Verify(r => r.AddAsync(It.IsAny<EventTemplateBo>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task SaveEventAsTemplateAsync_EventNotFound_ThrowsArgumentException()
        {
            // Arrange
            var eventId = 999;
            _mockEventRepository.Setup(r => r.GetByIdAsync(eventId))
                .ReturnsAsync((EventBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventTemplateService.SaveEventAsTemplateAsync(eventId);
            }
            catch (ArgumentException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected ArgumentException was not thrown");
        }

        #endregion
    }
}
