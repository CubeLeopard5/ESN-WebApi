using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories;

[TestClass]
public class EventRepositoryTests
{
    private EsnDevContext _context = null!;
    private EventRepository _repository = null!;
    private UserBo _testUser = null!;

    [TestInitialize]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EsnDevContext(options);
        _repository = new EventRepository(_context);

        // Create a test user
        _testUser = new UserBo
        {
            Email = "testuser@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = new DateTime(1990, 1, 1),
            StudentType = StudentType.Local
        };
        await _context.Users.AddAsync(_testUser);
        await _context.SaveChangesAsync();
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetAllEventsWithDetailsAsync_ShouldReturnEventsWithDetails()
    {
        // Arrange
        var event1 = new EventBo
        {
            Title = "Event 1",
            Description = "Description 1",
            Location = "Location 1",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = _testUser.Id,
            CreatedAt = DateTime.Now.AddDays(-1)
        };
        var event2 = new EventBo
        {
            Title = "Event 2",
            Description = "Description 2",
            Location = "Location 2",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 20,
            EventfrogLink = "link",
            UserId = _testUser.Id,
            CreatedAt = DateTime.Now
        };
        await _context.Events.AddRangeAsync(event1, event2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllEventsWithDetailsAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsNotNull(result.First().User);
    }

    [TestMethod]
    public async Task GetEventsPagedAsync_ShouldReturnCorrectPageSize()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await _context.Events.AddAsync(new EventBo
            {
                Title = $"Event {i}",
                Description = "Description",
                Location = "Location",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(2),
                MaxParticipants = 10,
                EventfrogLink = "link",
                UserId = _testUser.Id,
                CreatedAt = DateTime.Now
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var (events, totalCount) = await _repository.GetEventsPagedAsync(0, 10);

        // Assert
        Assert.AreEqual(15, totalCount);
        Assert.AreEqual(10, events.Count);
    }

    [TestMethod]
    public async Task GetEventWithDetailsAsync_ShouldReturnEventWithDetails()
    {
        // Arrange
        var eventBo = new EventBo
        {
            Title = "Test Event",
            Description = "Description",
            Location = "Location",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = _testUser.Id,
            CreatedAt = DateTime.Now
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventWithDetailsAsync(eventBo.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.User);
        Assert.AreEqual("Test Event", result.Title);
    }

    [TestMethod]
    public async Task GetEventsByUserEmailAsync_ShouldReturnUserEvents()
    {
        // Arrange
        var anotherUser = new UserBo
        {
            Email = "another@example.com",
            FirstName = "Another",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.Local
        };
        await _context.Users.AddAsync(anotherUser);
        await _context.SaveChangesAsync();

        await _context.Events.AddAsync(new EventBo
        {
            Title = "Event 1",
            Description = "Desc",
            Location = "Loc",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = _testUser.Id,
            CreatedAt = DateTime.Now
        });
        await _context.Events.AddAsync(new EventBo
        {
            Title = "Event 2",
            Description = "Desc",
            Location = "Loc",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = anotherUser.Id,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEventsByUserEmailAsync(_testUser.Email);

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Event 1", result.First().Title);
    }

    [TestMethod]
    public async Task GetRegistrationAsync_ShouldReturnRegistration_WhenExists()
    {
        // Arrange
        var eventBo = new EventBo
        {
            Title = "Test Event",
            Description = "Desc",
            Location = "Loc",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = _testUser.Id,
            CreatedAt = DateTime.Now
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        var registration = new EventRegistrationBo
        {
            EventId = eventBo.Id,
            UserId = _testUser.Id,
            Status = RegistrationStatus.Registered,
            RegisteredAt = DateTime.Now
        };
        await _context.EventRegistrations.AddAsync(registration);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRegistrationAsync(eventBo.Id, _testUser.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(RegistrationStatus.Registered, result.Status);
    }

    [TestMethod]
    public async Task GetRegistrationAsync_ShouldReturnNull_WhenDoesNotExist()
    {
        // Act
        var result = await _repository.GetRegistrationAsync(999, 999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetRegisteredCountAsync_ShouldReturnCorrectCount()
    {
        // Arrange
        var eventBo = new EventBo
        {
            Title = "Test Event",
            Description = "Desc",
            Location = "Loc",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = _testUser.Id,
            CreatedAt = DateTime.Now
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        await _context.EventRegistrations.AddRangeAsync(
            new EventRegistrationBo { EventId = eventBo.Id, UserId = _testUser.Id, Status = RegistrationStatus.Registered, RegisteredAt = DateTime.Now },
            new EventRegistrationBo { EventId = eventBo.Id, UserId = _testUser.Id + 1, Status = RegistrationStatus.Registered, RegisteredAt = DateTime.Now },
            new EventRegistrationBo { EventId = eventBo.Id, UserId = _testUser.Id + 2, Status = RegistrationStatus.Cancelled, RegisteredAt = DateTime.Now }
        );
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetRegisteredCountAsync(eventBo.Id);

        // Assert
        Assert.AreEqual(2, result);
    }
}
