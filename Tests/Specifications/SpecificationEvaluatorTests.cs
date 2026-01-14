using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.Specifications;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Tests.Specifications;

[TestClass]
public class SpecificationEvaluatorTests
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
    public async Task GetQuery_WithCriteria_ShouldFilterResults()
    {
        // Arrange
        var users = new List<UserBo>
        {
            new() { Email = "test1@example.com", FirstName = "Test", LastName = "One", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local },
            new() { Email = "test2@example.com", FirstName = "Test", LastName = "Two", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.EsnMember },
            new() { Email = "test3@example.com", FirstName = "Test", LastName = "Three", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>(u => u.StudentType == StudentType.Local);

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.All(u => u.StudentType == StudentType.Local));
    }

    [TestMethod]
    public async Task GetQuery_WithOrderBy_ShouldSortResults()
    {
        // Arrange
        var users = new List<UserBo>
        {
            new() { Email = "c@example.com", FirstName = "C", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local },
            new() { Email = "a@example.com", FirstName = "A", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local },
            new() { Email = "b@example.com", FirstName = "B", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>();
        specification.ApplyOrderBy(u => u.FirstName);

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual("A", result.First().FirstName);
        Assert.AreEqual("C", result.Last().FirstName);
    }

    [TestMethod]
    public async Task GetQuery_WithOrderByDescending_ShouldSortResultsDescending()
    {
        // Arrange
        var users = new List<UserBo>
        {
            new() { Email = "a@example.com", FirstName = "A", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local },
            new() { Email = "c@example.com", FirstName = "C", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local },
            new() { Email = "b@example.com", FirstName = "B", LastName = "User", PasswordHash = "hash", BirthDate = DateTime.Now, StudentType = StudentType.Local }
        };
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>();
        specification.ApplyOrderByDescending(u => u.FirstName);

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual("C", result.First().FirstName);
        Assert.AreEqual("A", result.Last().FirstName);
    }

    [TestMethod]
    public async Task GetQuery_WithPaging_ShouldReturnPagedResults()
    {
        // Arrange
        var users = new List<UserBo>();
        for (int i = 1; i <= 20; i++)
        {
            users.Add(new UserBo
            {
                Email = $"user{i}@example.com",
                FirstName = $"User{i}",
                LastName = "Test",
                PasswordHash = "hash",
                BirthDate = DateTime.Now,
                StudentType = StudentType.Local
            });
        }
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>();
        specification.ApplyPaging(5, 10);

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(10, result.Count);
    }

    [TestMethod]
    public async Task GetQuery_WithInclude_ShouldLoadRelatedData()
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
            BirthDate = DateTime.Now,
            StudentType = StudentType.Local,
            RoleId = role.Id
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>();
        specification.AddInclude(u => u.Role);

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Role);
        Assert.AreEqual("User", result.Role.Name);
    }

    [TestMethod]
    public async Task GetQuery_WithIncludeString_ShouldLoadRelatedData()
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
            BirthDate = DateTime.Now,
            StudentType = StudentType.Local,
            RoleId = role.Id
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>();
        specification.AddIncludeString("Role");

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.FirstOrDefaultAsync();

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Role);
    }

    [TestMethod]
    public async Task GetQuery_WithMultipleConditions_ShouldApplyAll()
    {
        // Arrange
        var users = new List<UserBo>();
        for (int i = 1; i <= 20; i++)
        {
            users.Add(new UserBo
            {
                Email = $"user{i}@example.com",
                FirstName = $"User",
                LastName = $"{i}",
                PasswordHash = "hash",
                BirthDate = DateTime.Now,
                StudentType = i % 2 == 0 ? StudentType.Local : StudentType.EsnMember
            });
        }
        await _context.Users.AddRangeAsync(users);
        await _context.SaveChangesAsync();

        var specification = new TestSpecification<UserBo>(u => u.StudentType == StudentType.Local);
        specification.ApplyOrderByDescending(u => u.LastName);
        specification.ApplyPaging(2, 3);

        // Act
        var query = SpecificationEvaluator<UserBo>.GetQuery(_context.Users, specification);
        var result = await query.ToListAsync();

        // Assert
        Assert.AreEqual(3, result.Count);
        Assert.IsTrue(result.All(u => u.StudentType == StudentType.Local));
    }
}

// Test implementation of ISpecification
public class TestSpecification<T> : ISpecification<T> where T : class
{
    public TestSpecification()
    {
        Includes = new List<Expression<Func<T, object>>>();
        IncludeStrings = new List<string>();
    }

    public TestSpecification(Expression<Func<T, bool>> criteria) : this()
    {
        Criteria = criteria;
    }

    public Expression<Func<T, bool>>? Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; }
    public List<string> IncludeStrings { get; }
    public Expression<Func<T, object>>? OrderBy { get; private set; }
    public Expression<Func<T, object>>? OrderByDescending { get; private set; }
    public int Skip { get; private set; }
    public int Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }

    public void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    public void AddIncludeString(string includeString)
    {
        IncludeStrings.Add(includeString);
    }

    public void ApplyOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy = orderByExpression;
    }

    public void ApplyOrderByDescending(Expression<Func<T, object>> orderByDescExpression)
    {
        OrderByDescending = orderByDescExpression;
    }

    public void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }
}
