using AutoMapper;
using Bo.Models;
using Business.Calendar;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Calendar;
using Dto.Common;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services
{
    [TestClass]
    public class CalendarServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IMapper> _mockMapper = null!;
        private Mock<ILogger<CalendarService>> _mockLogger = null!;
        private Mock<ICalendarRepository> _mockCalendarRepository = null!;
        private Mock<ICalendarSubOrganizerRepository> _mockCalendarSubOrganizerRepository = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private CalendarService _calendarService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<CalendarService>>();
            _mockCalendarRepository = new Mock<ICalendarRepository>();
            _mockCalendarSubOrganizerRepository = new Mock<ICalendarSubOrganizerRepository>();
            _mockUserRepository = new Mock<IUserRepository>();

            _mockUnitOfWork.Setup(u => u.Calendars).Returns(_mockCalendarRepository.Object);
            _mockUnitOfWork.Setup(u => u.CalendarSubOrganizers).Returns(_mockCalendarSubOrganizerRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            _calendarService = new CalendarService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        #region GetAllCalendars Tests

        [TestMethod]
        public async Task GetAllCalendarsAsync_ReturnsListOfCalendars()
        {
            // Arrange
            var calendarsBo = new List<CalendarBo>
            {
                new CalendarBo
                {
                    Id = 1,
                    Title = "Calendar 1",
                    EventDate = DateTime.Now.AddDays(1)
                },
                new CalendarBo
                {
                    Id = 2,
                    Title = "Calendar 2",
                    EventDate = DateTime.Now.AddDays(2)
                }
            };

            var calendarDtos = new List<CalendarDto>
            {
                new CalendarDto { Id = 1, Title = "Calendar 1" },
                new CalendarDto { Id = 2, Title = "Calendar 2" }
            };

            _mockCalendarRepository.Setup(r => r.GetAllCalendarsWithDetailsAsync())
                .ReturnsAsync(calendarsBo);
            _mockMapper.Setup(m => m.Map<IEnumerable<CalendarDto>>(calendarsBo))
                .Returns(calendarDtos);

            // Act
            var result = await _calendarService.GetAllCalendarsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetCalendarById Tests

        [TestMethod]
        public async Task GetCalendarByIdAsync_ExistingCalendar_ReturnsCalendarDto()
        {
            // Arrange
            var calendarId = 1;
            var calendarBo = new CalendarBo
            {
                Id = calendarId,
                Title = "Test Calendar",
                EventDate = DateTime.Now.AddDays(1)
            };

            var calendarDto = new CalendarDto
            {
                Id = calendarId,
                Title = "Test Calendar",
                EventDate = DateTime.Now.AddDays(1)
            };

            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarId))
                .ReturnsAsync(calendarBo);
            _mockMapper.Setup(m => m.Map<CalendarDto>(calendarBo))
                .Returns(calendarDto);

            // Act
            var result = await _calendarService.GetCalendarByIdAsync(calendarId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(calendarId, result.Id);
            Assert.AreEqual("Test Calendar", result.Title);
        }

        [TestMethod]
        public async Task GetCalendarByIdAsync_NonExistingCalendar_ReturnsNull()
        {
            // Arrange
            var calendarId = 999;
            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarId))
                .ReturnsAsync((CalendarBo?)null);

            // Act
            var result = await _calendarService.GetCalendarByIdAsync(calendarId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetCalendarsByEventId Tests

        [TestMethod]
        public async Task GetCalendarsByEventIdAsync_ReturnsCalendarsForEvent()
        {
            // Arrange
            var eventId = 1;
            var calendarsBo = new List<CalendarBo>
            {
                new CalendarBo { Id = 1, Title = "Calendar 1" },
                new CalendarBo { Id = 2, Title = "Calendar 2" }
            };

            var calendarDtos = new List<CalendarDto>
            {
                new CalendarDto { Id = 1, Title = "Calendar 1" },
                new CalendarDto { Id = 2, Title = "Calendar 2" }
            };

            _mockCalendarRepository.Setup(r => r.GetCalendarsByEventIdAsync(eventId))
                .ReturnsAsync(calendarsBo);
            _mockMapper.Setup(m => m.Map<IEnumerable<CalendarDto>>(calendarsBo))
                .Returns(calendarDtos);

            // Act
            var result = await _calendarService.GetCalendarsByEventIdAsync(eventId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region CreateCalendar Tests

        [TestMethod]
        public async Task CreateCalendarAsync_ValidCalendar_ReturnsCalendarDto()
        {
            // Arrange
            var createDto = new CalendarCreateDto
            {
                Title = "New Calendar",
                EventDate = DateTime.Now.AddDays(1)
            };

            var calendarBo = new CalendarBo
            {
                Id = 1,
                Title = createDto.Title,
                EventDate = createDto.EventDate
            };

            var calendarDto = new CalendarDto
            {
                Id = 1,
                Title = createDto.Title
            };

            _mockMapper.Setup(m => m.Map<CalendarBo>(createDto))
                .Returns(calendarBo);
            _mockCalendarRepository.Setup(r => r.AddAsync(It.IsAny<CalendarBo>()))
                .ReturnsAsync(calendarBo);
            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarBo.Id))
                .ReturnsAsync(calendarBo);
            _mockMapper.Setup(m => m.Map<CalendarDto>(It.IsAny<CalendarBo>()))
                .Returns(calendarDto);

            // Act
            var result = await _calendarService.CreateCalendarAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(createDto.Title, result.Title);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreateCalendarAsync_WithSubOrganizers_AddsSubOrganizers()
        {
            // Arrange
            var createDto = new CalendarCreateDto
            {
                Title = "New Calendar",
                EventDate = DateTime.Now.AddDays(1),
                SubOrganizerIds = new List<int> { 1, 2, 3 }
            };

            var calendarBo = new CalendarBo
            {
                Id = 1,
                Title = createDto.Title,
                EventDate = createDto.EventDate
            };

            var calendarDto = new CalendarDto
            {
                Id = 1,
                Title = createDto.Title
            };

            _mockMapper.Setup(m => m.Map<CalendarBo>(createDto))
                .Returns(calendarBo);
            _mockCalendarRepository.Setup(r => r.AddAsync(It.IsAny<CalendarBo>()))
                .ReturnsAsync(calendarBo);
            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarBo.Id))
                .ReturnsAsync(calendarBo);
            _mockMapper.Setup(m => m.Map<CalendarDto>(It.IsAny<CalendarBo>()))
                .Returns(calendarDto);

            // Act
            var result = await _calendarService.CreateCalendarAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            _mockCalendarSubOrganizerRepository.Verify(r => r.AddAsync(It.IsAny<CalendarSubOrganizerBo>()), Times.Exactly(3));
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2));
        }

        #endregion

        #region UpdateCalendar Tests

        [TestMethod]
        public async Task UpdateCalendarAsync_ValidUpdate_ReturnsUpdatedCalendarDto()
        {
            // Arrange
            var calendarId = 1;
            var userEmail = "organizer@test.com";
            var updateDto = new CalendarUpdateDto
            {
                Title = "Updated Calendar",
            };

            var organizer = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Organizer",
                LastName = "User"
            };

            var existingCalendar = new CalendarBo
            {
                Id = calendarId,
                Title = "Old Calendar",
                EventDate = DateTime.Now.AddDays(1),
                MainOrganizerId = 1
            };

            var updatedCalendarDto = new CalendarDto
            {
                Id = calendarId,
                Title = "Updated Calendar",
            };

            _mockCalendarRepository.Setup(r => r.GetByIdAsync(calendarId))
                .ReturnsAsync(existingCalendar);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(organizer);
            _mockMapper.Setup(m => m.Map(updateDto, existingCalendar))
                .Returns(existingCalendar);
            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarId))
                .ReturnsAsync(existingCalendar);
            _mockMapper.Setup(m => m.Map<CalendarDto>(existingCalendar))
                .Returns(updatedCalendarDto);

            // Act
            var result = await _calendarService.UpdateCalendarAsync(calendarId, updateDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Calendar", result.Title);
            _mockCalendarRepository.Verify(r => r.Update(existingCalendar), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateCalendarAsync_NonExistingCalendar_ReturnsNull()
        {
            // Arrange
            var calendarId = 999;
            var userEmail = "organizer@test.com";
            var updateDto = new CalendarUpdateDto
            {
                Title = "Updated Calendar"
            };

            _mockCalendarRepository.Setup(r => r.GetByIdAsync(calendarId))
                .ReturnsAsync((CalendarBo?)null);

            // Act
            var result = await _calendarService.UpdateCalendarAsync(calendarId, updateDto, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockCalendarRepository.Verify(r => r.Update(It.IsAny<CalendarBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region DeleteCalendar Tests

        [TestMethod]
        public async Task DeleteCalendarAsync_ExistingCalendar_ReturnsDeletedCalendar()
        {
            // Arrange
            var calendarId = 1;
            var userEmail = "organizer@test.com";
            var organizer = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Organizer",
                LastName = "User"
            };

            var calendarBo = new CalendarBo
            {
                Id = calendarId,
                Title = "Test Calendar",
                EventDate = DateTime.Now.AddDays(1),
                MainOrganizerId = 1
            };

            var calendarDto = new CalendarDto
            {
                Id = calendarId,
                Title = "Test Calendar"
            };

            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarId))
                .ReturnsAsync(calendarBo);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(organizer);
            _mockMapper.Setup(m => m.Map<CalendarDto>(calendarBo))
                .Returns(calendarDto);

            // Act
            var result = await _calendarService.DeleteCalendarAsync(calendarId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(calendarId, result.Id);
            _mockCalendarRepository.Verify(r => r.Delete(calendarBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeleteCalendarAsync_NonExistingCalendar_ReturnsNull()
        {
            // Arrange
            var calendarId = 999;
            var userEmail = "organizer@test.com";
            _mockCalendarRepository.Setup(r => r.GetCalendarWithDetailsAsync(calendarId))
                .ReturnsAsync((CalendarBo?)null);

            // Act
            var result = await _calendarService.DeleteCalendarAsync(calendarId, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockCalendarRepository.Verify(r => r.Delete(It.IsAny<CalendarBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion
    }
}
