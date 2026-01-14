using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Tests.Specifications;

[TestClass]
public class EventSpecificationsTests
{
    private EsnDevContext _context = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EsnDevContext(options);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task EventsWithDetailsSpecification_ShouldIncludeUserAndRegistrations()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "organizer@example.com",
            FirstName = "Organizer",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var eventBo = new EventBo
        {
            Title = "Test Event",
            Description = "Description",
            Location = "Location",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 50,
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsWithDetailsSpecification();

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.User);
        Assert.AreEqual("organizer@example.com", result.User.Email);
        Assert.IsNotNull(result.EventRegistrations);
    }

    [TestMethod]
    public async Task EventsWithDetailsSpecification_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "user@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.Local
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var events = new List<EventBo>
        {
            new()
            {
                Title = "Event 1",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            },
            new()
            {
                Title = "Event 2",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            },
            new()
            {
                Title = "Event 3",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            }
        };
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsWithDetailsSpecification();

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.AreEqual("Event 3", result.First().Title); // Most recent first
        Assert.AreEqual("Event 1", result.Last().Title); // Oldest last
    }

    [TestMethod]
    public async Task EventByIdWithDetailsSpecification_ShouldReturnSpecificEvent()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "organizer@example.com",
            FirstName = "Organizer",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var events = new List<EventBo>
        {
            new()
            {
                Title = "Event 1",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id
            },
            new()
            {
                Title = "Event 2",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id
            }
        };
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var targetEventId = events[1].Id;
        var specification = new EventSpecifications.EventByIdWithDetailsSpecification(targetEventId);

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(targetEventId, result.Id);
        Assert.AreEqual("Event 2", result.Title);
        Assert.IsNotNull(result.User);
        Assert.IsNotNull(result.EventRegistrations);
    }

    [TestMethod]
    public async Task EventByIdWithDetailsSpecification_ShouldReturnNull_WhenEventDoesNotExist()
    {
        // Arrange
        var specification = new EventSpecifications.EventByIdWithDetailsSpecification(999);

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task EventsByUserEmailSpecification_ShouldReturnOnlyUserEvents()
    {
        // Arrange
        var user1 = new UserBo
        {
            Email = "user1@example.com",
            FirstName = "User",
            LastName = "One",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        var user2 = new UserBo
        {
            Email = "user2@example.com",
            FirstName = "User",
            LastName = "Two",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        await _context.Users.AddRangeAsync(new[] { user1, user2 });
        await _context.SaveChangesAsync();

        var events = new List<EventBo>
        {
            new()
            {
                Title = "User1 Event 1",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user1.Id
            },
            new()
            {
                Title = "User2 Event",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user2.Id
            },
            new()
            {
                Title = "User1 Event 2",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user1.Id
            }
        };
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsByUserEmailSpecification("user1@example.com");

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(e => e.User.Email == "user1@example.com"));
    }

    [TestMethod]
    public async Task EventsByUserEmailSpecification_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "user@example.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var events = new List<EventBo>
        {
            new()
            {
                Title = "Old Event",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new()
            {
                Title = "Recent Event",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow
            }
        };
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsByUserEmailSpecification("user@example.com");

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.AreEqual("Recent Event", result.First().Title);
        Assert.AreEqual("Old Event", result.Last().Title);
    }

    [TestMethod]
    public async Task EventsWithPaginationSpecification_ShouldReturnCorrectPage()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "user@example.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.Local
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var events = new List<EventBo>();
        for (int i = 1; i <= 15; i++)
        {
            events.Add(new EventBo
            {
                Title = $"Event {i}",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsWithPaginationSpecification(1, 10);

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(10, result.Count);
        Assert.AreEqual("Event 1", result.First().Title); // Most recent first
    }

    [TestMethod]
    public async Task EventsWithPaginationSpecification_ShouldReturnSecondPage()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "user@example.com",
            FirstName = "User",
            LastName = "Test",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.Local
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var events = new List<EventBo>();
        for (int i = 1; i <= 15; i++)
        {
            events.Add(new EventBo
            {
                Title = $"Event {i}",
                Description = "Desc",
                Location = "Loc",
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddHours(1),
                MaxParticipants = 50,
                UserId = user.Id,
                CreatedAt = DateTime.UtcNow.AddDays(-i)
            });
        }
        await _context.Events.AddRangeAsync(events);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsWithPaginationSpecification(2, 10);

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(5, result.Count); // Only 5 items on page 2
        Assert.AreEqual("Event 11", result.First().Title); // First item on page 2
    }

    [TestMethod]
    public async Task EventsWithPaginationSpecification_ShouldIncludeUserAndRegistrations()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "organizer@example.com",
            FirstName = "Organizer",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var eventBo = new EventBo
        {
            Title = "Test Event",
            Description = "Description",
            Location = "Location",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 50,
            UserId = user.Id
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsWithPaginationSpecification(1, 10);

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.User);
        Assert.AreEqual("organizer@example.com", result.User.Email);
        Assert.IsNotNull(result.EventRegistrations);
    }

    [TestMethod]
    public async Task EventsWithPaginationSpecification_WithRegistrations_ShouldLoadAll()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "organizer@example.com",
            FirstName = "Organizer",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.EsnMember
        };
        var attendee = new UserBo
        {
            Email = "attendee@example.com",
            FirstName = "Attendee",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = DateTime.Now,
            StudentType = StudentType.International
        };
        await _context.Users.AddRangeAsync(new[] { user, attendee });
        await _context.SaveChangesAsync();

        var eventBo = new EventBo
        {
            Title = "Test Event",
            Description = "Description",
            Location = "Location",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 50,
            UserId = user.Id
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        var registration = new EventRegistrationBo
        {
            EventId = eventBo.Id,
            UserId = attendee.Id,
            Status = RegistrationStatus.Registered
        };
        await _context.EventRegistrations.AddAsync(registration);
        await _context.SaveChangesAsync();

        var specification = new EventSpecifications.EventsWithPaginationSpecification(1, 10);

        // Act
        var query = SpecificationEvaluator<EventBo>.GetQuery(_context.Events, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.EventRegistrations);
        Assert.AreEqual(1, result.EventRegistrations.Count);
    }
}
