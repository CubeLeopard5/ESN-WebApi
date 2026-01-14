using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories;

[TestClass]
public class UserRepositoryTests
{
    private EsnDevContext _context = null!;
    private UserRepository _repository = null!;

    [TestInitialize]
    public void Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EsnDevContext(options);
        _repository = new UserRepository(_context);
    }

    [TestCleanup]
    public void Cleanup()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [TestMethod]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
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
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("test@example.com", result.Email);
        Assert.AreEqual("Test", result.FirstName);
    }

    [TestMethod]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.GetByEmailAsync("nonexistent@example.com");

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetByEmailWithRoleAsync_ShouldReturnUserWithRole_WhenUserExists()
    {
        // Arrange
        var role = new RoleBo
        {
            Name = "User",
            CanCreateEvents = false,
            CanModifyEvents = false,
            CanDeleteEvents = false,
            CanCreateUsers = false,
            CanModifyUsers = false,
            CanDeleteUsers = false
        };
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        var user = new UserBo
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = new DateTime(1990, 1, 1),
            StudentType = StudentType.Local,
            RoleId = role.Id
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailWithRoleAsync("test@example.com");

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Role);
        Assert.AreEqual("User", result.Role.Name);
    }

    [TestMethod]
    public async Task GetUserWithRoleAsync_ShouldReturnUserWithRole_WhenUserExists()
    {
        // Arrange
        var role = new RoleBo
        {
            Name = "User",
            CanCreateEvents = false,
            CanModifyEvents = false,
            CanDeleteEvents = false,
            CanCreateUsers = false,
            CanModifyUsers = false,
            CanDeleteUsers = false
        };
        await _context.Roles.AddAsync(role);
        await _context.SaveChangesAsync();

        var user = new UserBo
        {
            Email = "test@example.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hash",
            BirthDate = new DateTime(1990, 1, 1),
            StudentType = StudentType.Local,
            RoleId = role.Id
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetUserWithRoleAsync(user.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Role);
        Assert.AreEqual("User", result.Role.Name);
    }

    [TestMethod]
    public async Task GetUserWithRoleAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Act
        var result = await _repository.GetUserWithRoleAsync(999);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task GetEsnMembersAsync_ShouldReturnOnlyEsnMembers()
    {
        // Arrange
        var users = new List<UserBo>
        {
            new() { Email = "member1@example.com", FirstName = "Member", LastName = "One", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.EsnMember },
            new() { Email = "member2@example.com", FirstName = "Member", LastName = "Two", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.EsnMember },
            new() { Email = "international@example.com", FirstName = "International", LastName = "Student", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.International },
            new() { Email = "local@example.com", FirstName = "Local", LastName = "Student", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetEsnMembersAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
        Assert.IsTrue(result.All(u => u.StudentType == StudentType.EsnMember));
    }

    [TestMethod]
    public async Task EmailExistsAsync_ShouldReturnTrue_WhenEmailExists()
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
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.EmailExistsAsync("test@example.com");

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EmailExistsAsync_ShouldReturnFalse_WhenEmailDoesNotExist()
    {
        // Act
        var result = await _repository.EmailExistsAsync("nonexistent@example.com");

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldReturnPagedResults()
    {
        // Arrange
        var users = new List<UserBo>();
        for (int i = 1; i <= 15; i++)
        {
            users.Add(new UserBo
            {
                Email = $"user{i}@example.com",
                FirstName = $"User",
                LastName = $"{i}",
                PasswordHash = "hash",
                BirthDate = new DateTime(1990, 1, 1),
                StudentType = StudentType.Local
            });
        }
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(0, 10);

        // Assert
        Assert.AreEqual(15, totalCount);
        Assert.AreEqual(10, items.Count);
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldSkipCorrectly()
    {
        // Arrange
        var users = new List<UserBo>();
        for (int i = 1; i <= 15; i++)
        {
            users.Add(new UserBo
            {
                Email = $"user{i}@example.com",
                FirstName = $"User",
                LastName = $"{i}",
                PasswordHash = "hash",
                BirthDate = new DateTime(1990, 1, 1),
                StudentType = StudentType.Local
            });
        }
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(10, 10);

        // Assert
        Assert.AreEqual(15, totalCount);
        Assert.AreEqual(5, items.Count);
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldReturnEmptyList_WhenSkipExceedsTotalCount()
    {
        // Arrange
        var users = new List<UserBo>();
        for (int i = 1; i <= 5; i++)
        {
            users.Add(new UserBo
            {
                Email = $"user{i}@example.com",
                FirstName = $"User",
                LastName = $"{i}",
                PasswordHash = "hash",
                BirthDate = new DateTime(1990, 1, 1),
                StudentType = StudentType.Local
            });
        }
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(10, 10);

        // Assert
        Assert.AreEqual(5, totalCount);
        Assert.AreEqual(0, items.Count);
    }
}
