using AutoMapper;
using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.Attendance;
using Business.Interfaces;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Attendance;
using Dto.User;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

[TestClass]
public class AttendanceServiceTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<AttendanceService>> _mockLogger = null!;
    private Mock<IEventRepository> _mockEventRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IEventRegistrationRepository> _mockEventRegistrationRepository = null!;
    private IAttendanceService _attendanceService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<AttendanceService>>();
        _mockEventRepository = new Mock<IEventRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockEventRegistrationRepository = new Mock<IEventRegistrationRepository>();

        _mockUnitOfWork.Setup(u => u.Events).Returns(_mockEventRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.EventRegistrations).Returns(_mockEventRegistrationRepository.Object);

        _attendanceService = new AttendanceService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region ValidateAttendanceAsync Tests

    [TestMethod]
    public async Task ValidateAttendanceAsync_ValidRequest_ShouldUpdateRegistration()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 10;
        var validatorEmail = "esn@example.com";
        var status = AttendanceStatus.Present;

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            FirstName = "ESN",
            LastName = "Member",
            BirthDate = DateTime.Now.AddYears(-25),
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var registration = new EventRegistrationBo
        {
            Id = registrationId,
            EventId = eventId,
            UserId = 2,
            Status = RegistrationStatus.Registered,
            RegisteredAt = DateTime.UtcNow.AddDays(-1),
            User = new UserBo { Id = 2, Email = "user@example.com", FirstName = "Test", LastName = "User" }
        };

        var attendanceDto = new AttendanceDto
        {
            Id = registrationId,
            AttendanceStatus = status,
            AttendanceValidatedAt = DateTime.UtcNow
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        _mockEventRegistrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registrationId))
            .ReturnsAsync(registration);
        _mockMapper.Setup(m => m.Map<AttendanceDto>(It.IsAny<EventRegistrationBo>()))
            .Returns(attendanceDto);

        // Act
        var result = await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, status, validatorEmail);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(status, result.AttendanceStatus);
        _mockEventRegistrationRepository.Verify(r => r.Update(It.Is<EventRegistrationBo>(
            er => er.AttendanceStatus == status && er.AttendanceValidatedById == validator.Id
        )), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task ValidateAttendanceAsync_NonEsnMember_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 10;
        var validatorEmail = "user@example.com";
        var status = AttendanceStatus.Present;
        var exceptionThrown = false;

        var nonEsnUser = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            FirstName = "Regular",
            LastName = "User",
            BirthDate = DateTime.Now.AddYears(-25),
            StudentType = Bo.Constants.StudentType.International // Not ESN member
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(nonEsnUser);

        // Act
        try
        {
            await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, status, validatorEmail);
        }
        catch (UnauthorizedAccessException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    [TestMethod]
    public async Task ValidateAttendanceAsync_ValidatorNotFound_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 10;
        var validatorEmail = "notfound@example.com";
        var status = AttendanceStatus.Present;
        var exceptionThrown = false;

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync((UserBo?)null);

        // Act
        try
        {
            await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, status, validatorEmail);
        }
        catch (UnauthorizedAccessException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    [TestMethod]
    public async Task ValidateAttendanceAsync_RegistrationNotFound_ShouldThrowKeyNotFound()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 999;
        var validatorEmail = "esn@example.com";
        var status = AttendanceStatus.Present;
        var exceptionThrown = false;

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        _mockEventRegistrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registrationId))
            .ReturnsAsync((EventRegistrationBo?)null);

        // Act
        try
        {
            await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, status, validatorEmail);
        }
        catch (KeyNotFoundException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    [TestMethod]
    public async Task ValidateAttendanceAsync_WrongEventId_ShouldThrowKeyNotFound()
    {
        // Arrange
        var eventId = 1;
        var wrongEventId = 999;
        var registrationId = 10;
        var validatorEmail = "esn@example.com";
        var status = AttendanceStatus.Present;
        var exceptionThrown = false;

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var registration = new EventRegistrationBo
        {
            Id = registrationId,
            EventId = wrongEventId, // Different event
            UserId = 2,
            Status = RegistrationStatus.Registered
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        _mockEventRegistrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registrationId))
            .ReturnsAsync(registration);

        // Act
        try
        {
            await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, status, validatorEmail);
        }
        catch (KeyNotFoundException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    [TestMethod]
    public async Task ValidateAttendanceAsync_CancelledRegistration_ShouldThrowInvalidOperation()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 10;
        var validatorEmail = "esn@example.com";
        var status = AttendanceStatus.Present;
        var exceptionThrown = false;

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var registration = new EventRegistrationBo
        {
            Id = registrationId,
            EventId = eventId,
            UserId = 2,
            Status = RegistrationStatus.Cancelled // Cancelled registration
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        _mockEventRegistrationRepository.Setup(r => r.GetByIdWithDetailsAsync(registrationId))
            .ReturnsAsync(registration);

        // Act
        try
        {
            await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, status, validatorEmail);
        }
        catch (InvalidOperationException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    #endregion

    #region BulkValidateAttendanceAsync Tests

    [TestMethod]
    public async Task BulkValidateAttendanceAsync_ValidRequest_ShouldUpdateAllRegistrations()
    {
        // Arrange
        var eventId = 1;
        var validatorEmail = "esn@example.com";
        var dto = new BulkValidateAttendanceDto
        {
            Attendances = new List<BulkAttendanceItemDto>
            {
                new() { RegistrationId = 10, Status = AttendanceStatus.Present },
                new() { RegistrationId = 11, Status = AttendanceStatus.Absent },
                new() { RegistrationId = 12, Status = AttendanceStatus.Excused }
            }
        };

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var registrations = new List<EventRegistrationBo>
        {
            new() { Id = 10, EventId = eventId, Status = RegistrationStatus.Registered },
            new() { Id = 11, EventId = eventId, Status = RegistrationStatus.Registered },
            new() { Id = 12, EventId = eventId, Status = RegistrationStatus.Registered }
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);

        // Setup GetByIdsAsync to return all registrations in one call
        _mockEventRegistrationRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(registrations.ToDictionary(r => r.Id, r => r));

        // Act
        var result = await _attendanceService.BulkValidateAttendanceAsync(eventId, dto, validatorEmail);

        // Assert
        Assert.AreEqual(3, result);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitTransactionAsync(), Times.Once);
        // Verify GetByIdsAsync was called once (not N times)
        _mockEventRegistrationRepository.Verify(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()), Times.Once);
    }

    [TestMethod]
    public async Task BulkValidateAttendanceAsync_PartiallyValidRegistrations_ShouldUpdateOnlyValid()
    {
        // Arrange
        var eventId = 1;
        var validatorEmail = "esn@example.com";
        var dto = new BulkValidateAttendanceDto
        {
            Attendances = new List<BulkAttendanceItemDto>
            {
                new() { RegistrationId = 10, Status = AttendanceStatus.Present },
                new() { RegistrationId = 999, Status = AttendanceStatus.Absent } // Non-existent
            }
        };

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var registration = new EventRegistrationBo
        {
            Id = 10,
            EventId = eventId,
            Status = RegistrationStatus.Registered
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        // Only registration 10 exists, 999 doesn't
        _mockEventRegistrationRepository.Setup(r => r.GetByIdsAsync(It.IsAny<IEnumerable<int>>()))
            .ReturnsAsync(new Dictionary<int, EventRegistrationBo> { { 10, registration } });

        // Act
        var result = await _attendanceService.BulkValidateAttendanceAsync(eventId, dto, validatorEmail);

        // Assert
        Assert.AreEqual(1, result); // Only 1 updated
    }

    [TestMethod]
    public async Task BulkValidateAttendanceAsync_NonEsnMember_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = 1;
        var validatorEmail = "user@example.com";
        var exceptionThrown = false;
        var dto = new BulkValidateAttendanceDto
        {
            Attendances = new List<BulkAttendanceItemDto>
            {
                new() { RegistrationId = 10, Status = AttendanceStatus.Present }
            }
        };

        var nonEsnUser = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.International
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(nonEsnUser);

        // Act
        try
        {
            await _attendanceService.BulkValidateAttendanceAsync(eventId, dto, validatorEmail);
        }
        catch (UnauthorizedAccessException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    #endregion

    #region GetEventAttendanceAsync Tests

    [TestMethod]
    public async Task GetEventAttendanceAsync_ExistingEvent_ShouldReturnEventWithAttendance()
    {
        // Arrange
        var eventId = 1;
        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = DateTime.UtcNow,
            UserId = 1,
            User = new UserBo { Id = 1, Email = "organizer@example.com", FirstName = "Org", LastName = "User" }
        };

        var registrations = new List<EventRegistrationBo>
        {
            new()
            {
                Id = 10,
                EventId = eventId,
                UserId = 2,
                Status = RegistrationStatus.Registered,
                AttendanceStatus = AttendanceStatus.Present,
                User = new UserBo { Id = 2, Email = "user1@example.com", FirstName = "User", LastName = "One" }
            },
            new()
            {
                Id = 11,
                EventId = eventId,
                UserId = 3,
                Status = RegistrationStatus.Registered,
                AttendanceStatus = null, // Not yet validated
                User = new UserBo { Id = 3, Email = "user2@example.com", FirstName = "User", LastName = "Two" }
            }
        };

        _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
            .ReturnsAsync(eventBo);
        _mockEventRegistrationRepository.Setup(r => r.GetByEventIdWithAttendanceAsync(eventId))
            .ReturnsAsync(registrations);
        _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<UserBo>()))
            .Returns<UserBo>(u => new UserDto { Id = u.Id, Email = u.Email });
        _mockMapper.Setup(m => m.Map<List<AttendanceDto>>(It.IsAny<IEnumerable<EventRegistrationBo>>()))
            .Returns(new List<AttendanceDto>
            {
                new() { Id = 10, AttendanceStatus = AttendanceStatus.Present },
                new() { Id = 11, AttendanceStatus = null }
            });

        // Act
        var result = await _attendanceService.GetEventAttendanceAsync(eventId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(eventId, result.Id);
        Assert.AreEqual("Test Event", result.Title);
        Assert.AreEqual(2, result.Registrations.Count);
        Assert.AreEqual(2, result.Stats.TotalRegistered);
        Assert.AreEqual(1, result.Stats.PresentCount);
        Assert.AreEqual(1, result.Stats.NotYetValidatedCount);
    }

    [TestMethod]
    public async Task GetEventAttendanceAsync_NonExistingEvent_ShouldReturnNull()
    {
        // Arrange
        var eventId = 999;
        _mockEventRepository.Setup(r => r.GetEventWithDetailsAsync(eventId))
            .ReturnsAsync((EventBo?)null);

        // Act
        var result = await _attendanceService.GetEventAttendanceAsync(eventId);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region GetAttendanceStatsAsync Tests

    [TestMethod]
    public async Task GetAttendanceStatsAsync_MixedAttendance_ShouldCalculateCorrectly()
    {
        // Arrange
        var eventId = 1;
        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = DateTime.UtcNow
        };

        // Create a mock dictionary through Callback to simulate EF Core behavior
        // EF Core's ToDictionaryAsync can handle null keys, but regular Dictionary can't
        // We test the iteration approach by creating a dictionary that mimics the structure
        var statsDict = new Dictionary<AttendanceStatus?, int>();
        // Add non-null entries normally
        statsDict.Add(AttendanceStatus.Present, 5);
        statsDict.Add(AttendanceStatus.Absent, 2);
        statsDict.Add(AttendanceStatus.Excused, 1);
        // For the null key, we need to test separately - for now, test only validated entries
        // The null handling is tested in GetEventAttendanceAsync tests

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync(eventBo);
        _mockEventRegistrationRepository.Setup(r => r.GetAttendanceStatsAsync(eventId))
            .ReturnsAsync(statsDict);

        // Act
        var result = await _attendanceService.GetAttendanceStatsAsync(eventId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(eventId, result.EventId);
        Assert.AreEqual(8, result.TotalRegistered); // 5 + 2 + 1 (no null entries)
        Assert.AreEqual(8, result.TotalValidated); // All validated
        Assert.AreEqual(5, result.PresentCount);
        Assert.AreEqual(2, result.AbsentCount);
        Assert.AreEqual(1, result.ExcusedCount);
        Assert.AreEqual(0, result.NotYetValidatedCount);
        Assert.AreEqual(62.5m, result.AttendanceRate); // 5/8 * 100
        Assert.AreEqual(100m, result.ValidationRate); // 8/8 * 100
    }

    [TestMethod]
    public async Task GetAttendanceStatsAsync_AllPresent_ShouldReturn100PercentRate()
    {
        // Arrange
        var eventId = 1;
        var eventBo = new EventBo
        {
            Id = eventId,
            Title = "Test Event",
            StartDate = DateTime.UtcNow
        };

        var statsDict = new Dictionary<AttendanceStatus?, int>
        {
            { AttendanceStatus.Present, 10 }
        };

        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync(eventBo);
        _mockEventRegistrationRepository.Setup(r => r.GetAttendanceStatsAsync(eventId))
            .ReturnsAsync(statsDict);

        // Act
        var result = await _attendanceService.GetAttendanceStatsAsync(eventId);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(100m, result.AttendanceRate);
        Assert.AreEqual(100m, result.ValidationRate);
    }

    [TestMethod]
    public async Task GetAttendanceStatsAsync_NonExistingEvent_ShouldReturnNull()
    {
        // Arrange
        var eventId = 999;
        _mockEventRepository.Setup(r => r.GetByIdAsync(eventId))
            .ReturnsAsync((EventBo?)null);

        // Act
        var result = await _attendanceService.GetAttendanceStatsAsync(eventId);

        // Assert
        Assert.IsNull(result);
    }

    #endregion

    #region ResetAttendanceAsync Tests

    [TestMethod]
    public async Task ResetAttendanceAsync_ValidRequest_ShouldResetToNull()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 10;
        var validatorEmail = "esn@example.com";

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var registration = new EventRegistrationBo
        {
            Id = registrationId,
            EventId = eventId,
            UserId = 2,
            Status = RegistrationStatus.Registered,
            AttendanceStatus = AttendanceStatus.Present,
            AttendanceValidatedAt = DateTime.UtcNow,
            AttendanceValidatedById = 1
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        _mockEventRegistrationRepository.Setup(r => r.GetByIdAsync(registrationId))
            .ReturnsAsync(registration);

        // Act
        var result = await _attendanceService.ResetAttendanceAsync(eventId, registrationId, validatorEmail);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNull(registration.AttendanceStatus);
        Assert.IsNull(registration.AttendanceValidatedAt);
        Assert.IsNull(registration.AttendanceValidatedById);
        _mockEventRegistrationRepository.Verify(r => r.Update(registration), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task ResetAttendanceAsync_RegistrationNotFound_ShouldReturnFalse()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 999;
        var validatorEmail = "esn@example.com";

        var validator = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(validator);
        _mockEventRegistrationRepository.Setup(r => r.GetByIdAsync(registrationId))
            .ReturnsAsync((EventRegistrationBo?)null);

        // Act
        var result = await _attendanceService.ResetAttendanceAsync(eventId, registrationId, validatorEmail);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task ResetAttendanceAsync_NonEsnMember_ShouldThrowUnauthorized()
    {
        // Arrange
        var eventId = 1;
        var registrationId = 10;
        var validatorEmail = "user@example.com";
        var exceptionThrown = false;

        var nonEsnUser = new UserBo
        {
            Id = 1,
            Email = validatorEmail,
            StudentType = Bo.Constants.StudentType.International
        };

        _mockUserRepository.Setup(r => r.GetByEmailAsync(validatorEmail))
            .ReturnsAsync(nonEsnUser);

        // Act
        try
        {
            await _attendanceService.ResetAttendanceAsync(eventId, registrationId, validatorEmail);
        }
        catch (UnauthorizedAccessException)
        {
            exceptionThrown = true;
        }

        // Assert
        Assert.IsTrue(exceptionThrown);
    }

    #endregion
}
