using AutoMapper;
using Bo.Constants;
using Bo.Models;
using Business.Event;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Event;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services
{
    [TestClass]
    public class EventServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IMapper> _mockMapper = null!;
        private Mock<ILogger<EventService>> _mockLogger = null!;
        private Mock<IEventRepository> _mockEventRepository = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IEventRegistrationRepository> _mockEventRegistrationRepository = null!;
        private Mock<ICalendarRepository> _mockCalendarRepository = null!;
        private EventService _eventService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<EventService>>();
            _mockEventRepository = new Mock<IEventRepository>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEventRegistrationRepository = new Mock<IEventRegistrationRepository>();
            _mockCalendarRepository = new Mock<ICalendarRepository>();

            _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepository.Object);
            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.EventRegistrations).Returns(_mockEventRegistrationRepository.Object);
            _mockUnitOfWork.Setup(u => u.Calendars).Returns(_mockCalendarRepository.Object);

            _eventService = new EventService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        #region GetAllEvents Tests

        [TestMethod]
        public async Task GetAllEventsAsync_ReturnsListOfEvents()
        {
            // Arrange
            var eventsBo = new List<EventBo>
            {
                new EventBo
                {
                    Id = 1,
                    Title = "Event 1",
                    StartDate = DateTime.Now.AddDays(1),
                    UserId = 1,
                    EventRegistrations = new List<EventRegistrationBo>()
                },
                new EventBo
                {
                    Id = 2,
                    Title = "Event 2",
                    StartDate = DateTime.Now.AddDays(2),
                    UserId = 1,
                    EventRegistrations = new List<EventRegistrationBo>()
                }
            };

            var eventDtos = new List<EventDto>
            {
                new EventDto { Id = 1, Title = "Event 1" },
                new EventDto { Id = 2, Title = "Event 2" }
            };

            _mockEventRepository.Setup(r => r.GetAllEventsWithDetailsAsync())
                .ReturnsAsync(eventsBo);
            _mockMapper.Setup(m => m.Map<EventDto>(It.IsAny<EventBo>()))
                .Returns<EventBo>(e => new EventDto { Id = e.Id, Title = e.Title });

            // Act
            var result = await _eventService.GetAllEventsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetEventById Tests

        [TestMethod]
        public async Task GetEventByIdAsync_ExistingEvent_ReturnsEventDto()
        {
            // Arrange
            var eventId = 1;
            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            var eventDto = new EventDto
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description"
            };

            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockMapper.Setup(m => m.Map<EventDto>(eventBo))
                .Returns(eventDto);

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(eventId, result.Id);
            Assert.AreEqual("Test Event", result.Title);
        }

        [TestMethod]
        public async Task GetEventByIdAsync_NonExistingEvent_ReturnsNull()
        {
            // Arrange
            var eventId = 999;
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync((EventBo?)null);

            // Act
            var result = await _eventService.GetEventByIdAsync(eventId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region CreateEvent Tests

        [TestMethod]
        public async Task CreateEventAsync_ValidEvent_ReturnsEventDto()
        {
            // Arrange
            var userEmail = "test@example.com";
            var createDto = new CreateEventDto
            {
                Title = "New Event",
                Description = "Test Description",
                Location = "Test Location",
                StartDate = DateTime.Now.AddDays(1),
                MaxParticipants = 50
            };

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var eventBo = new EventBo
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description,
                Location = createDto.Location,
                StartDate = createDto.StartDate,
                MaxParticipants = createDto.MaxParticipants,
                UserId = user.Id,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            var eventDto = new EventDto
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<EventBo>(createDto))
                .Returns(eventBo);
            _mockEventRepository.Setup(r => r.AddAsync(It.IsAny<EventBo>()))
                .ReturnsAsync(eventBo);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventBo.Id))
                .ReturnsAsync(eventBo);
            _mockMapper.Setup(m => m.Map<EventDto>(It.IsAny<EventBo>()))
                .Returns(eventDto);

            // Act
            var result = await _eventService.CreateEventAsync(createDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(createDto.Title, result.Title);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreateEventAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userEmail = "notfound@example.com";
            var createDto = new CreateEventDto
            {
                Title = "New Event",
                StartDate = DateTime.Now.AddDays(1)
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync((UserBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventService.CreateEventAsync(createDto, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        #endregion

        #region UpdateEvent Tests

        [TestMethod]
        public async Task UpdateEventAsync_ValidUpdate_ReturnsUpdatedEventDto()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "test@example.com";
            var updateDto = new EventDto
            {
                Id = eventId,
                Title = "Updated Event",
                Description = "Updated Description"
            };

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var existingEvent = new EventBo
            {
                Id = eventId,
                Title = "Old Event",
                Description = "Old Description",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            var updatedEventDto = new EventDto
            {
                Id = eventId,
                Title = "Updated Event",
                Description = "Updated Description"
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(existingEvent);
            _mockCalendarRepository.Setup(r => r.GetCalendarByEventIdAsync(eventId))
                .ReturnsAsync((CalendarBo?)null);
            _mockMapper.Setup(m => m.Map<EventDto>(existingEvent))
                .Returns(updatedEventDto);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Event", result.Title);
            _mockEventRepository.Verify(r => r.Update(existingEvent), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateEventAsync_NonExistingEvent_ReturnsNull()
        {
            // Arrange
            var eventId = 999;
            var userEmail = "test@example.com";
            var updateDto = new EventDto
            {
                Id = eventId,
                Title = "Updated Event"
            };

            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync((EventBo?)null);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockEventRepository.Verify(r => r.Update(It.IsAny<EventBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task UpdateEventAsync_EsnMemberUpdatingAnyEvent_ReturnsUpdatedEventDto()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "esnmember@example.com";
            var updateDto = new EventDto
            {
                Id = eventId,
                Title = "Updated by ESN Member",
                Description = "Updated Description"
            };

            var esnMember = new UserBo
            {
                Id = 2, // Different user ID
                Email = userEmail,
                FirstName = "ESN",
                LastName = "Member",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.EsnMember // ESN member can edit any event
            };

            var existingEvent = new EventBo
            {
                Id = eventId,
                Title = "Old Event",
                Description = "Old Description",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1, // Created by different user
                EventRegistrations = new List<EventRegistrationBo>()
            };

            var updatedEventDto = new EventDto
            {
                Id = eventId,
                Title = "Updated by ESN Member",
                Description = "Updated Description"
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(esnMember);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(existingEvent);
            _mockCalendarRepository.Setup(r => r.GetCalendarByEventIdAsync(eventId))
                .ReturnsAsync((CalendarBo?)null);
            _mockMapper.Setup(m => m.Map<EventDto>(existingEvent))
                .Returns(updatedEventDto);

            // Act
            var result = await _eventService.UpdateEventAsync(eventId, updateDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated by ESN Member", result.Title);
            _mockEventRepository.Verify(r => r.Update(existingEvent), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateEventAsync_NonOwnerNonEsnMember_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "otheruser@example.com";
            var updateDto = new EventDto
            {
                Id = eventId,
                Title = "Unauthorized Update"
            };

            var otherUser = new UserBo
            {
                Id = 2, // Different user ID
                Email = userEmail,
                FirstName = "Other",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International // Not esn_member
            };

            var existingEvent = new EventBo
            {
                Id = eventId,
                Title = "Old Event",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1, // Created by different user
                EventRegistrations = new List<EventRegistrationBo>()
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(otherUser);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(existingEvent);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventService.UpdateEventAsync(eventId, updateDto, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
            _mockEventRepository.Verify(r => r.Update(It.IsAny<EventBo>()), Times.Never);
        }

        #endregion

        #region DeleteEvent Tests

        [TestMethod]
        public async Task DeleteEventAsync_ExistingEvent_ReturnsTrue()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "test@example.com";
            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1,
                User = user
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId, userEmail);

            // Assert
            Assert.IsTrue(result);
            _mockEventRepository.Verify(r => r.Delete(eventBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeleteEventAsync_NonExistingEvent_ReturnsFalse()
        {
            // Arrange
            var eventId = 999;
            var userEmail = "test@example.com";
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync((EventBo?)null);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId, userEmail);

            // Assert
            Assert.IsFalse(result);
            _mockEventRepository.Verify(r => r.Delete(It.IsAny<EventBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task DeleteEventAsync_EsnMemberDeletingAnyEvent_ReturnsTrue()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "esnmember@example.com";
            var esnMember = new UserBo
            {
                Id = 2, // Different user ID
                Email = userEmail,
                FirstName = "ESN",
                LastName = "Member",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.EsnMember // ESN member can delete any event
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1, // Created by different user
                User = new UserBo { Id = 1, Email = "creator@example.com" }
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(esnMember);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);

            // Act
            var result = await _eventService.DeleteEventAsync(eventId, userEmail);

            // Assert
            Assert.IsTrue(result);
            _mockEventRepository.Verify(r => r.Delete(eventBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeleteEventAsync_NonOwnerNonEsnMember_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "otheruser@example.com";
            var otherUser = new UserBo
            {
                Id = 2, // Different user ID
                Email = userEmail,
                FirstName = "Other",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International // Not esn_member
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.Now.AddDays(1),
                UserId = 1, // Created by different user
                User = new UserBo { Id = 1, Email = "creator@example.com" }
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(otherUser);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventService.DeleteEventAsync(eventId, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
            _mockEventRepository.Verify(r => r.Delete(It.IsAny<EventBo>()), Times.Never);
        }

        #endregion

        #region RegisterForEvent Tests

        [TestMethod]
        public async Task RegisterForEventAsync_ValidRegistration_ReturnsSuccess()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "test@example.com";
            var surveyData = "{}";

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(-1), // Registration started yesterday
                EndDate = DateTime.UtcNow.AddDays(7), // Registration ends in 7 days
                MaxParticipants = 50,
                UserId = 2,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockEventRepository.Setup(r => r.GetRegistrationAsync(eventId, user.Id))
                .ReturnsAsync((EventRegistrationBo?)null);
            _mockEventRepository.Setup(r => r.GetRegisteredCountAsync(eventId))
                .ReturnsAsync(10);

            // Act
            var result = await _eventService.RegisterForEventAsync(eventId, userEmail, surveyData);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Successfully registered for event", result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task RegisterForEventAsync_EventNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var eventId = 999;
            var userEmail = "test@example.com";

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync((EventBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventService.RegisterForEventAsync(eventId, userEmail, "{}");
            }
            catch (KeyNotFoundException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected KeyNotFoundException was not thrown");
        }

        [TestMethod]
        public async Task RegisterForEventAsync_AlreadyRegistered_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(-1),
                EndDate = DateTime.UtcNow.AddDays(7),
                UserId = 2,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            var existingRegistration = new EventRegistrationBo
            {
                Id = 1,
                EventId = eventId,
                UserId = user.Id,
                Status = RegistrationStatus.Registered
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockEventRepository.Setup(r => r.GetRegistrationAsync(eventId, user.Id))
                .ReturnsAsync(existingRegistration);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _eventService.RegisterForEventAsync(eventId, userEmail, "{}");
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
        }

        [TestMethod]
        public async Task RegisterForEventAsync_BeforeRegistrationPeriod_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "test@example.com";
            var surveyData = "{}";

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(7), // Registration starts in 7 days
                EndDate = DateTime.UtcNow.AddDays(14),
                MaxParticipants = 50,
                UserId = 2,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);

            // Act & Assert
            var exceptionThrown = false;
            var exceptionMessage = "";
            try
            {
                await _eventService.RegisterForEventAsync(eventId, userEmail, surveyData);
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                exceptionMessage = ex.Message;
            }
            Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
            Assert.AreEqual("Registration period has not started yet", exceptionMessage);
        }

        [TestMethod]
        public async Task RegisterForEventAsync_AfterRegistrationPeriod_ThrowsInvalidOperationException()
        {
            // Arrange
            var eventId = 1;
            var userEmail = "test@example.com";
            var surveyData = "{}";

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(-14), // Registration started 14 days ago
                EndDate = DateTime.UtcNow.AddDays(-7), // Registration ended 7 days ago
                MaxParticipants = 50,
                UserId = 2,
                EventRegistrations = new List<EventRegistrationBo>()
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);

            // Act & Assert
            var exceptionThrown = false;
            var exceptionMessage = "";
            try
            {
                await _eventService.RegisterForEventAsync(eventId, userEmail, surveyData);
            }
            catch (InvalidOperationException ex)
            {
                exceptionThrown = true;
                exceptionMessage = ex.Message;
            }
            Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
            Assert.AreEqual("Registration period has ended", exceptionMessage);
        }

        #endregion

        #region GetEventRegistrations Tests

        [TestMethod]
        public async Task GetEventRegistrationsAsync_ExistingEvent_ReturnsEventWithRegistrations()
        {
            // Arrange
            var eventId = 1;
            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                Location = "Test Location",
                StartDate = DateTime.UtcNow.AddDays(7),
                EndDate = DateTime.UtcNow.AddDays(8),
                MaxParticipants = 50,
                UserId = 1,
                User = new UserBo
                {
                    Id = 1,
                    Email = "organizer@example.com",
                    FirstName = "John",
                    LastName = "Doe"
                },
                EventRegistrations = new List<EventRegistrationBo>
                {
                    new EventRegistrationBo
                    {
                        Id = 1,
                        EventId = eventId,
                        UserId = 2,
                        RegisteredAt = DateTime.UtcNow.AddDays(-2),
                        Status = RegistrationStatus.Registered,
                        SurveyJsData = "{\"question1\":\"answer1\"}",
                        User = new UserBo
                        {
                            Id = 2,
                            Email = "user1@example.com",
                            FirstName = "Jane",
                            LastName = "Smith"
                        }
                    },
                    new EventRegistrationBo
                    {
                        Id = 2,
                        EventId = eventId,
                        UserId = 3,
                        RegisteredAt = DateTime.UtcNow.AddDays(-1),
                        Status = RegistrationStatus.Registered,
                        SurveyJsData = "{\"question1\":\"answer2\"}",
                        User = new UserBo
                        {
                            Id = 3,
                            Email = "user2@example.com",
                            FirstName = "Bob",
                            LastName = "Johnson"
                        }
                    }
                }
            };

            var eventWithRegistrationsDto = new EventWithRegistrationsDto
            {
                Id = eventId,
                Title = "Test Event",
                Description = "Test Description",
                Location = "Test Location",
                StartDate = eventBo.StartDate,
                EndDate = eventBo.EndDate,
                MaxParticipants = 50,
                TotalRegistered = 2,
                Registrations = new List<EventRegistrationDto>
                {
                    new EventRegistrationDto
                    {
                        Id = 1,
                        RegisteredAt = eventBo.EventRegistrations.ElementAt(0).RegisteredAt ?? DateTime.UtcNow,
                        Status = "Registered",
                        SurveyJsData = "{\"question1\":\"answer1\"}"
                    },
                    new EventRegistrationDto
                    {
                        Id = 2,
                        RegisteredAt = eventBo.EventRegistrations.ElementAt(1).RegisteredAt ?? DateTime.UtcNow,
                        Status = "Registered",
                        SurveyJsData = "{\"question1\":\"answer2\"}"
                    }
                }
            };

            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockMapper.Setup(m => m.Map<EventWithRegistrationsDto>(eventBo))
                .Returns(eventWithRegistrationsDto);

            // Act
            var result = await _eventService.GetEventRegistrationsAsync(eventId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(eventId, result.Id);
            Assert.AreEqual("Test Event", result.Title);
            Assert.AreEqual(2, result.TotalRegistered);
            Assert.AreEqual(2, result.Registrations.Count);
            Assert.AreEqual("user1@example.com", eventBo.EventRegistrations.ElementAt(0).User.Email);
            _mockEventRepository.Verify(r => r.GetEventWithDetailsAsync(eventId), Times.Once);
        }

        [TestMethod]
        public async Task GetEventRegistrationsAsync_NonExistingEvent_ReturnsNull()
        {
            // Arrange
            var eventId = 999;
            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync((EventBo?)null);

            // Act
            var result = await _eventService.GetEventRegistrationsAsync(eventId);

            // Assert
            Assert.IsNull(result);
            _mockEventRepository.Verify(r => r.GetEventWithDetailsAsync(eventId), Times.Once);
        }

        [TestMethod]
        public async Task GetEventRegistrationsAsync_EventWithNoRegistrations_ReturnsEventWithEmptyList()
        {
            // Arrange
            var eventId = 1;
            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Event Without Registrations",
                Description = "No one registered yet",
                Location = "Test Location",
                StartDate = DateTime.UtcNow.AddDays(7),
                MaxParticipants = 50,
                UserId = 1,
                User = new UserBo
                {
                    Id = 1,
                    Email = "organizer@example.com",
                    FirstName = "John",
                    LastName = "Doe"
                },
                EventRegistrations = new List<EventRegistrationBo>()
            };

            var eventWithRegistrationsDto = new EventWithRegistrationsDto
            {
                Id = eventId,
                Title = "Event Without Registrations",
                Description = "No one registered yet",
                Location = "Test Location",
                StartDate = eventBo.StartDate,
                MaxParticipants = 50,
                TotalRegistered = 0,
                Registrations = new List<EventRegistrationDto>()
            };

            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockMapper.Setup(m => m.Map<EventWithRegistrationsDto>(eventBo))
                .Returns(eventWithRegistrationsDto);

            // Act
            var result = await _eventService.GetEventRegistrationsAsync(eventId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(eventId, result.Id);
            Assert.AreEqual("Event Without Registrations", result.Title);
            Assert.AreEqual(0, result.TotalRegistered);
            Assert.AreEqual(0, result.Registrations.Count);
            _mockEventRepository.Verify(r => r.GetEventWithDetailsAsync(eventId), Times.Once);
        }

        [TestMethod]
        public async Task GetEventRegistrationsAsync_EventWithCancelledRegistrations_IncludesCancelledStatus()
        {
            // Arrange
            var eventId = 1;
            var eventBo = new EventBo
            {
                Id = eventId,
                Title = "Test Event",
                StartDate = DateTime.UtcNow.AddDays(7),
                MaxParticipants = 50,
                UserId = 1,
                User = new UserBo { Id = 1, Email = "organizer@example.com", FirstName = "John", LastName = "Doe" },
                EventRegistrations = new List<EventRegistrationBo>
                {
                    new EventRegistrationBo
                    {
                        Id = 1,
                        EventId = eventId,
                        UserId = 2,
                        RegisteredAt = DateTime.UtcNow.AddDays(-2),
                        Status = RegistrationStatus.Registered,
                        SurveyJsData = "{}",
                        User = new UserBo { Id = 2, Email = "user1@example.com", FirstName = "Jane", LastName = "Smith" }
                    },
                    new EventRegistrationBo
                    {
                        Id = 2,
                        EventId = eventId,
                        UserId = 3,
                        RegisteredAt = DateTime.UtcNow.AddDays(-1),
                        Status = RegistrationStatus.Cancelled,
                        SurveyJsData = "{}",
                        User = new UserBo { Id = 3, Email = "user2@example.com", FirstName = "Bob", LastName = "Johnson" }
                    }
                }
            };

            var eventWithRegistrationsDto = new EventWithRegistrationsDto
            {
                Id = eventId,
                Title = "Test Event",
                TotalRegistered = 1, // Only count registered, not cancelled
                Registrations = new List<EventRegistrationDto>
                {
                    new EventRegistrationDto { Id = 1, Status = "Registered", SurveyJsData = "{}" },
                    new EventRegistrationDto { Id = 2, Status = "Cancelled", SurveyJsData = "{}" }
                }
            };

            _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
                .ReturnsAsync(eventBo);
            _mockMapper.Setup(m => m.Map<EventWithRegistrationsDto>(eventBo))
                .Returns(eventWithRegistrationsDto);

            // Act
            var result = await _eventService.GetEventRegistrationsAsync(eventId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Registrations.Count);
            Assert.AreEqual(1, result.Registrations.Count(r => r.Status == "Registered"));
            Assert.AreEqual(1, result.Registrations.Count(r => r.Status == "Cancelled"));
            Assert.AreEqual(1, result.TotalRegistered);
            _mockEventRepository.Verify(r => r.GetEventWithDetailsAsync(eventId), Times.Once);
        }

        #endregion
    }
}
