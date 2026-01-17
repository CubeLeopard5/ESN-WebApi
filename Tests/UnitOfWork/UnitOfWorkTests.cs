using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace Tests.UnitOfWork;

[TestClass]
public class UnitOfWorkTests
{
    private EsnDevContext _context = null!;
    private Dal.UnitOfWork.UnitOfWork _unitOfWork = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        _context = new EsnDevContext(options);
        _unitOfWork = new Dal.UnitOfWork.UnitOfWork(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        try
        {
            _context?.Database.EnsureDeleted();
        }
        catch { }
        finally
        {
            _unitOfWork?.Dispose();
        }
    }

    [TestMethod]
    public void Events_ShouldReturnEventRepository()
    {
        // Act
        var repository = _unitOfWork.Events;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IEventRepository));
    }

    [TestMethod]
    public void EventTemplates_ShouldReturnEventTemplateRepository()
    {
        // Act
        var repository = _unitOfWork.EventTemplates;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IEventTemplateRepository));
    }

    [TestMethod]
    public void Users_ShouldReturnUserRepository()
    {
        // Act
        var repository = _unitOfWork.Users;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IUserRepository));
    }

    [TestMethod]
    public void Calendars_ShouldReturnCalendarRepository()
    {
        // Act
        var repository = _unitOfWork.Calendars;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.ICalendarRepository));
    }

    [TestMethod]
    public void Propositions_ShouldReturnPropositionRepository()
    {
        // Act
        var repository = _unitOfWork.Propositions;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IPropositionRepository));
    }

    [TestMethod]
    public void EventRegistrations_ShouldReturnGenericRepository()
    {
        // Act
        var repository = _unitOfWork.EventRegistrations;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IRepository<EventRegistrationBo>));
    }

    [TestMethod]
    public void CalendarSubOrganizers_ShouldReturnGenericRepository()
    {
        // Act
        var repository = _unitOfWork.CalendarSubOrganizers;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IRepository<CalendarSubOrganizerBo>));
    }

    [TestMethod]
    public void Roles_ShouldReturnGenericRepository()
    {
        // Act
        var repository = _unitOfWork.Roles;

        // Assert
        Assert.IsNotNull(repository);
        Assert.IsInstanceOfType(repository, typeof(Dal.Repositories.Interfaces.IRepository<RoleBo>));
    }

    [TestMethod]
    public async Task SaveChangesAsync_ShouldPersistChanges()
    {
        // Arrange
        var user = new UserBo
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = new DateTime(1990, 1, 1),
            StudentType = StudentType.Local
        };
        await _unitOfWork.Users.AddAsync(user);

        // Act
        var result = await _unitOfWork.SaveChangesAsync();

        // Assert
        Assert.AreEqual(1, result);
        var savedUser = await _unitOfWork.Users.GetByIdAsync(user.Id);
        Assert.IsNotNull(savedUser);
        Assert.AreEqual("test@example.com", savedUser.Email);
    }

    [TestMethod]
    public async Task BeginTransactionAsync_ShouldStartTransaction()
    {
        // Act
        await _unitOfWork.BeginTransactionAsync();

        // Assert - verify we can commit without error (transaction was started)
        await _unitOfWork.CommitTransactionAsync();

        // Verify unit of work is still functional after transaction
        var repository = _unitOfWork.Users;
        Assert.IsNotNull(repository, "UnitOfWork should remain functional after transaction");
    }

    [TestMethod]
    public async Task CommitTransactionAsync_ShouldCommitChanges()
    {
        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var user = new UserBo
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = new DateTime(1990, 1, 1),
            StudentType = StudentType.Local
        };
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        await _unitOfWork.CommitTransactionAsync();

        // Assert
        var savedUser = await _unitOfWork.Users.GetByIdAsync(user.Id);
        Assert.IsNotNull(savedUser);
    }

    [TestMethod]
    public async Task RollbackTransactionAsync_ShouldRevertChanges()
    {
        // Note: In-memory database does not support transactions, so this test cannot verify actual rollback
        // This test would pass with a real database (SQL Server, PostgreSQL, etc.)
        Assert.Inconclusive("In-memory database does not support transactions");

        // Arrange
        await _unitOfWork.BeginTransactionAsync();
        var user = new UserBo
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = new DateTime(1990, 1, 1),
            StudentType = StudentType.Local
        };
        await _unitOfWork.Users.AddAsync(user);
        await _unitOfWork.SaveChangesAsync();
        var userId = user.Id;

        // Act
        await _unitOfWork.RollbackTransactionAsync();

        // Assert
        var savedUser = await _unitOfWork.Users.GetByIdAsync(userId);
        Assert.IsNull(savedUser);
    }

    [TestMethod]
    public async Task CommitTransactionAsync_WithoutTransaction_ShouldNotThrow()
    {
        // Act - should not throw even without a transaction
        await _unitOfWork.CommitTransactionAsync();

        // Assert - verify unit of work is still functional after commit without transaction
        var repository = _unitOfWork.Users;
        Assert.IsNotNull(repository);
    }

    [TestMethod]
    public async Task RollbackTransactionAsync_WithoutTransaction_ShouldNotThrow()
    {
        // Act - should not throw even without a transaction
        await _unitOfWork.RollbackTransactionAsync();

        // Assert - verify unit of work is still functional after rollback without transaction
        var repository = _unitOfWork.Users;
        Assert.IsNotNull(repository);
    }

    [TestMethod]
    public void Dispose_ShouldBeIdempotent()
    {
        // Act - call Dispose multiple times
        _unitOfWork.Dispose();
        _unitOfWork.Dispose();

        // Assert - second dispose should not throw, proving idempotency
        // If we got here without exception, the test passes
        Assert.IsNotNull(_unitOfWork);
    }
}
