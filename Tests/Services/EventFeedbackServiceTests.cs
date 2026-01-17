using AutoMapper;
using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.EventFeedback;
using Business.Interfaces;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.EventFeedback;
using Dto.User;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Tests.Services;

[TestClass]
public class EventFeedbackServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<EventFeedbackService>> _mockLogger = null!;
    private Mock<IEventRepository> _mockEventRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IEventRegistrationRepository> _mockEventRegistrationRepository = null!;
    private Mock<IEventFeedbackRepository> _mockEventFeedbackRepository = null!;
    private IEventFeedbackService _feedbackService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<EventFeedbackService>>();
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEventRegistrationRepository = new Mock<IEventRegistrationRepository>();
        _mockEventFeedbackRepository = new Mock<IEventFeedbackRepository>();

        _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.EventRegistrations).Returns(_mockEventRegistrationRepository.Object);
        _mockUnitOfWork.Setup(u => u.EventFeedbacks).Returns(_mockEventFeedbackRepository.Object);

        _feedbackService = new EventFeedbackService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region CheckEligibilityAsync Tests

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenUserAttended_ReturnsCanSubmit()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(7)
        };

        var user = new UserBo { Id = userId, Email = userEmail, FirstName = "Test", LastName = "User" };

        var registration = new EventRegistrationBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Present,
            Status = RegistrationStatus.Registered
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(registration);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync((EventFeedbackBo?)null);

        // Act
        var result = await _feedbackService.CheckEligibilityAsync(eventId, userEmail);

        // Assert
        Assert.IsTrue(result.CanSubmit);
        Assert.IsFalse(result.HasSubmitted);
        Assert.IsNull(result.Reason);
        Assert.IsNotNull(result.FeedbackFormData);
    }

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenUserNotAttended_ReturnsCannotSubmit()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}"
        };

        var user = new UserBo { Id = userId, Email = userEmail };

        var registration = new EventRegistrationBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Absent,
            Status = RegistrationStatus.Registered
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(registration);

        // Act
        var result = await _feedbackService.CheckEligibilityAsync(eventId, userEmail);

        // Assert
        Assert.IsFalse(result.CanSubmit);
        Assert.AreEqual("not_attended", result.Reason);
    }

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenNoRegistration_ReturnsCannotSubmit()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}"
        };

        var user = new UserBo { Id = userId, Email = userEmail };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync((EventRegistrationBo?)null);

        // Act
        var result = await _feedbackService.CheckEligibilityAsync(eventId, userEmail);

        // Assert
        Assert.IsFalse(result.CanSubmit);
        Assert.AreEqual("not_attended", result.Reason);
    }

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenAlreadySubmitted_ReturnsHasSubmitted()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(7)
        };

        var user = new UserBo { Id = userId, Email = userEmail, FirstName = "Test", LastName = "User" };

        var registration = new EventRegistrationBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Present,
            Status = RegistrationStatus.Registered
        };

        var existingFeedback = new EventFeedbackBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            FeedbackData = "{}",
            SubmittedAt = DateTime.UtcNow.AddHours(-1),
            User = user
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(registration);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(existingFeedback);

        // Act
        var result = await _feedbackService.CheckEligibilityAsync(eventId, userEmail);

        // Assert
        Assert.IsTrue(result.HasSubmitted);
        Assert.IsTrue(result.CanSubmit);
        Assert.IsNotNull(result.ExistingFeedback);
    }

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenDeadlinePassed_ReturnsCannotSubmit()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(-1)
        };

        var user = new UserBo { Id = userId, Email = userEmail };

        var registration = new EventRegistrationBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Present,
            Status = RegistrationStatus.Registered
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(registration);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync((EventFeedbackBo?)null);

        // Act
        var result = await _feedbackService.CheckEligibilityAsync(eventId, userEmail);

        // Assert
        Assert.IsFalse(result.CanSubmit);
        Assert.AreEqual("deadline_passed", result.Reason);
    }

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenNoFeedbackForm_ReturnsCannotSubmit()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = null
        };

        var user = new UserBo { Id = userId, Email = userEmail };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);

        // Act
        var result = await _feedbackService.CheckEligibilityAsync(eventId, userEmail);

        // Assert
        Assert.IsFalse(result.CanSubmit);
        Assert.AreEqual("no_feedback_form", result.Reason);
    }

    [TestMethod]
    public async Task CheckEligibilityAsync_WhenEventNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var eventId = 999;
        var userEmail = "user@example.com";
        var exceptionThrown = false;

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((EventBo?)null);

        // Act
        try
        {
            await _feedbackService.CheckEligibilityAsync(eventId, userEmail);
        }
        catch (KeyNotFoundException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    #endregion

    #region SubmitFeedbackAsync Tests

    [TestMethod]
    public async Task SubmitFeedbackAsync_WhenEligible_CreatesFeedback()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;
        var dto = new SubmitFeedbackDto { FeedbackData = "{\"rating\": 5}" };

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(7)
        };

        var user = new UserBo { Id = userId, Email = userEmail, FirstName = "Test", LastName = "User" };

        var registration = new EventRegistrationBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Present,
            Status = RegistrationStatus.Registered
        };

        var savedFeedback = new EventFeedbackBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            FeedbackData = dto.FeedbackData,
            SubmittedAt = DateTime.UtcNow,
            User = user
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(registration);
        _mockEventFeedbackRepository.Setup(r => r.AddAsync(It.IsAny<EventFeedbackBo>())).ReturnsAsync(savedFeedback);

        _mockEventFeedbackRepository.SetupSequence(r => r.GetByEventAndUserAsync(eventId, userId))
            .ReturnsAsync((EventFeedbackBo?)null)
            .ReturnsAsync(savedFeedback);

        // Act
        var result = await _feedbackService.SubmitFeedbackAsync(eventId, userEmail, dto);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(dto.FeedbackData, result.FeedbackData);
        _mockEventFeedbackRepository.Verify(r => r.AddAsync(It.IsAny<EventFeedbackBo>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task SubmitFeedbackAsync_WhenNotEligible_ThrowsInvalidOperationException()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;
        var dto = new SubmitFeedbackDto { FeedbackData = "{}" };
        var exceptionThrown = false;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = null
        };

        var user = new UserBo { Id = userId, Email = userEmail };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);

        // Act
        try
        {
            await _feedbackService.SubmitFeedbackAsync(eventId, userEmail, dto);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    [TestMethod]
    public async Task SubmitFeedbackAsync_WhenAlreadySubmitted_ThrowsInvalidOperationException()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;
        var dto = new SubmitFeedbackDto { FeedbackData = "{}" };
        var exceptionThrown = false;
        var exceptionMessage = string.Empty;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(7)
        };

        var user = new UserBo { Id = userId, Email = userEmail, FirstName = "Test", LastName = "User" };

        var registration = new EventRegistrationBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            AttendanceStatus = AttendanceStatus.Present,
            Status = RegistrationStatus.Registered
        };

        var existingFeedback = new EventFeedbackBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            FeedbackData = "{}",
            SubmittedAt = DateTime.UtcNow,
            User = user
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(registration);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId)).ReturnsAsync(existingFeedback);

        // Act
        try
        {
            await _feedbackService.SubmitFeedbackAsync(eventId, userEmail, dto);
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            exceptionMessage = ex.Message;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
        StringAssert.Contains(exceptionMessage, "already submitted");
    }

    #endregion

    #region UpdateFeedbackAsync Tests

    [TestMethod]
    public async Task UpdateFeedbackAsync_WhenBeforeDeadline_UpdatesFeedback()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;
        var dto = new SubmitFeedbackDto { FeedbackData = "{\"rating\": 4}" };

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(7)
        };

        var user = new UserBo { Id = userId, Email = userEmail, FirstName = "Test", LastName = "User" };

        var existingFeedback = new EventFeedbackBo
        {
            Id = 1,
            EventId = eventId,
            UserId = userId,
            FeedbackData = "{\"rating\": 5}",
            SubmittedAt = DateTime.UtcNow.AddHours(-1),
            User = user
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockEventFeedbackRepository.Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<EventFeedbackBo, bool>>>()))
            .ReturnsAsync(existingFeedback);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventAndUserAsync(eventId, userId))
            .ReturnsAsync(existingFeedback);

        // Act
        var result = await _feedbackService.UpdateFeedbackAsync(eventId, userEmail, dto);

        // Assert
        Assert.IsNotNull(result);
        _mockEventFeedbackRepository.Verify(r => r.Update(It.IsAny<EventFeedbackBo>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateFeedbackAsync_WhenDeadlinePassed_ThrowsInvalidOperationException()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var userId = 10;
        var dto = new SubmitFeedbackDto { FeedbackData = "{}" };
        var exceptionThrown = false;
        var exceptionMessage = string.Empty;

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(-1)
        };

        var user = new UserBo { Id = userId, Email = userEmail };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);

        // Act
        try
        {
            await _feedbackService.UpdateFeedbackAsync(eventId, userEmail, dto);
        }
        catch (InvalidOperationException ex)
        {
            exceptionThrown = true;
            exceptionMessage = ex.Message;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
        StringAssert.Contains(exceptionMessage, "deadline passed");
    }

    #endregion

    #region GetAllFeedbacksAsync Tests

    [TestMethod]
    public async Task GetAllFeedbacksAsync_WhenAdmin_ReturnsAllFeedbacks()
    {
        // Arrange
        var eventId = 1;
        var adminEmail = "admin@example.com";

        var admin = new UserBo
        {
            Id = 1,
            Email = adminEmail,
            Role = new RoleBo { Name = "Admin" }
        };

        var eventBo = new EventBo { Id = eventId, Title = "Test Event" };

        var feedbacks = new List<EventFeedbackBo>
        {
            new() { Id = 1, EventId = eventId, UserId = 10, FeedbackData = "{}", SubmittedAt = DateTime.UtcNow, User = new UserBo { Id = 10, FirstName = "User", LastName = "1" } },
            new() { Id = 2, EventId = eventId, UserId = 11, FeedbackData = "{}", SubmittedAt = DateTime.UtcNow, User = new UserBo { Id = 11, FirstName = "User", LastName = "2" } }
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(adminEmail)).ReturnsAsync(admin);
        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(feedbacks);

        // Act
        var result = await _feedbackService.GetAllFeedbacksAsync(eventId, adminEmail);

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task GetAllFeedbacksAsync_WhenEsnMember_ReturnsAllFeedbacks()
    {
        // Arrange
        var eventId = 1;
        var esnEmail = "esn@example.com";

        var esnMember = new UserBo
        {
            Id = 1,
            Email = esnEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var eventBo = new EventBo { Id = eventId, Title = "Test Event" };

        var feedbacks = new List<EventFeedbackBo>();

        _mockUserRepository.Setup(r => r.GetByEmailAsync(esnEmail)).ReturnsAsync(esnMember);
        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockEventFeedbackRepository.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(feedbacks);

        // Act
        var result = await _feedbackService.GetAllFeedbacksAsync(eventId, esnEmail);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public async Task GetAllFeedbacksAsync_WhenNotAuthorized_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var eventId = 1;
        var userEmail = "user@example.com";
        var exceptionThrown = false;

        var regularUser = new UserBo
        {
            Id = 1,
            Email = userEmail,
            StudentType = Bo.Constants.StudentType.International
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(regularUser);

        // Act
        try
        {
            await _feedbackService.GetAllFeedbacksAsync(eventId, userEmail);
        }
        catch (UnauthorizedAccessException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    #endregion

    #region GetFeedbackSummaryAsync Tests

    [TestMethod]
    public async Task GetFeedbackSummaryAsync_ReturnsCorrectStatistics()
    {
        // Arrange
        var eventId = 1;
        var adminEmail = "admin@example.com";

        var admin = new UserBo
        {
            Id = 1,
            Email = adminEmail,
            Role = new RoleBo { Name = "Admin" }
        };

        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            FeedbackFormData = "{\"elements\":[]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(7)
        };

        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 1, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 2, AttendanceStatus = AttendanceStatus.Present },
            new() { Id = 3, AttendanceStatus = AttendanceStatus.Absent },
            new() { Id = 4, AttendanceStatus = AttendanceStatus.Present }
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(adminEmail)).ReturnsAsync(admin);
        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventIdWithAttendanceAsync(eventId)).ReturnsAsync(registrations);
        _mockEventFeedbackRepository.Setup(r => r.CountByEventIdAsync(eventId)).ReturnsAsync(2);

        // Act
        var result = await _feedbackService.GetFeedbackSummaryAsync(eventId, adminEmail);

        // Assert
        Assert.AreEqual(eventId, result.EventId);
        Assert.AreEqual("Test Event", result.EventTitle);
        Assert.AreEqual(3, result.TotalAttendees);
        Assert.AreEqual(2, result.TotalFeedbacks);
        Assert.AreEqual(66.67m, result.ResponseRate);
        Assert.IsTrue(result.HasFeedbackForm);
    }

    #endregion

    #region UpdateFeedbackFormAsync Tests

    [TestMethod]
    public async Task UpdateFeedbackFormAsync_WhenAdmin_UpdatesForm()
    {
        // Arrange
        var eventId = 1;
        var adminEmail = "admin@example.com";
        var dto = new UpdateFeedbackFormDto
        {
            FeedbackFormData = "{\"elements\":[{\"type\":\"rating\"}]}",
            FeedbackDeadline = DateTime.UtcNow.AddDays(14)
        };

        var admin = new UserBo
        {
            Id = 1,
            Email = adminEmail,
            Role = new RoleBo { Name = "Admin" }
        };

        var eventBo = new EventBo { Id = eventId, Title = "Test Event" };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(adminEmail)).ReturnsAsync(admin);
        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(eventBo);

        // Act
        var result = await _feedbackService.UpdateFeedbackFormAsync(eventId, adminEmail, dto);

        // Assert
        Assert.IsTrue(result);
        _mockEventRepository.Verify(r => r.Update(It.Is<EventBo>(e =>
            e.FeedbackFormData == dto.FeedbackFormData &&
            e.FeedbackDeadline == dto.FeedbackDeadline
        )), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task UpdateFeedbackFormAsync_WhenEventNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var eventId = 999;
        var adminEmail = "admin@example.com";
        var dto = new UpdateFeedbackFormDto();
        var exceptionThrown = false;

        var admin = new UserBo
        {
            Id = 1,
            Email = adminEmail,
            Role = new RoleBo { Name = "Admin" }
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(adminEmail)).ReturnsAsync(admin);
        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync((EventBo?)null);

        // Act
        try
        {
            await _feedbackService.UpdateFeedbackFormAsync(eventId, adminEmail, dto);
        }
        catch (KeyNotFoundException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    #endregion
}
