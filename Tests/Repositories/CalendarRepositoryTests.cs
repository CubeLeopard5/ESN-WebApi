using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories;

[TestClass]
public class CalendarRepositoryTests
{
    private EsnDevContext _context = null!;
    private CalendarRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EsnDevContext(options);
        _repository = new CalendarRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetAllCalendarsWithDetailsAsync_ShouldReturnAllCalendars()
    {
        // Arrange
        var user = new UserBo { Email = "test@test.com", FirstName = "Test", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var calendar1 = new CalendarBo { Title = "Cal 1", EventDate = DateTime.Now, MainOrganizerId = user.Id };
        var calendar2 = new CalendarBo { Title = "Cal 2", EventDate = DateTime.Now, MainOrganizerId = user.Id };
        await _context.Calendars.AddRangeAsync(calendar1, calendar2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllCalendarsWithDetailsAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task GetCalendarWithDetailsAsync_ShouldReturnCalendar_WhenExists()
    {
        // Arrange
        var user = new UserBo { Email = "test@test.com", FirstName = "Test", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var calendar = new CalendarBo { Title = "Test Calendar", EventDate = DateTime.Now, MainOrganizerId = user.Id };
        await _context.Calendars.AddAsync(calendar);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCalendarWithDetailsAsync(calendar.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Calendar", result.Title);
    }

    [TestMethod]
    public async Task GetCalendarsByEventIdAsync_ShouldReturnCalendarsForEvent()
    {
        // Arrange
        var user = new UserBo { Email = "test@test.com", FirstName = "Test", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var eventBo = new EventBo
        {
            Title = "Event",
            Description = "Desc",
            Location = "Loc",
            StartDate = DateTime.Now,
            EndDate = DateTime.Now.AddHours(2),
            MaxParticipants = 10,
            EventfrogLink = "link",
            UserId = user.Id,
            CreatedAt = DateTime.Now
        };
        await _context.Events.AddAsync(eventBo);
        await _context.SaveChangesAsync();

        var calendar1 = new CalendarBo { Title = "Cal 1", EventDate = DateTime.Now, EventId = eventBo.Id, MainOrganizerId = user.Id };
        var calendar2 = new CalendarBo { Title = "Cal 2", EventDate = DateTime.Now, EventId = eventBo.Id, MainOrganizerId = user.Id };
        var calendar3 = new CalendarBo { Title = "Cal 3", EventDate = DateTime.Now, MainOrganizerId = user.Id };
        await _context.Calendars.AddRangeAsync(calendar1, calendar2, calendar3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCalendarsByEventIdAsync(eventBo.Id);

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var user = new UserBo { Email = "test@test.com", FirstName = "Test", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 15; i++)
        {
            await _context.Calendars.AddAsync(new CalendarBo
            {
                Title = $"Calendar {i}",
                EventDate = DateTime.Now,
                MainOrganizerId = user.Id
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(0, 10);

        // Assert
        Assert.AreEqual(15, totalCount);
        Assert.AreEqual(10, items.Count);
    }

    [TestMethod]
    public async Task GetCalendarSimpleAsync_ShouldReturnCalendar()
    {
        // Arrange
        var user = new UserBo { Email = "test@test.com", FirstName = "Test", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var calendar = new CalendarBo { Title = "Simple Cal", EventDate = DateTime.Now, MainOrganizerId = user.Id };
        await _context.Calendars.AddAsync(calendar);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetCalendarSimpleAsync(calendar.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Simple Cal", result.Title);
    }

    [TestMethod]
    public async Task GetPagedSimpleAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var user = new UserBo { Email = "test@test.com", FirstName = "Test", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        for (int i = 1; i <= 10; i++)
        {
            await _context.Calendars.AddAsync(new CalendarBo
            {
                Title = $"Calendar {i}",
                EventDate = DateTime.Now,
                MainOrganizerId = user.Id
            });
        }
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedSimpleAsync(0, 5);

        // Assert
        Assert.AreEqual(10, totalCount);
        Assert.AreEqual(5, items.Count);
    }
}
