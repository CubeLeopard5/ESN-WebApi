using Bo.Constants;
using Bo.Models;
using Dal;
using Dal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories;

[TestClass]
public class PropositionRepositoryTests
{
    private EsnDevContext _context = null!;
    private PropositionRepository _repository = null!;
    private UserBo _testUser = null!;

    [TestInitialize]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<EsnDevContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new EsnDevContext(options);
        _repository = new PropositionRepository(_context);

        _testUser = new UserBo
        {
            Email = "test@example.com",
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
    public async Task GetActivePropositionsWithDetailsAsync_ShouldReturnOnlyActivePropositions()
    {
        // Arrange
        var activeProp = new PropositionBo
        {
            Title = "Active Prop",
            Description = "Active Description",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        var deletedProp = new PropositionBo
        {
            Title = "Deleted Prop",
            Description = "Deleted Description",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };
        await _context.Propositions.AddRangeAsync(activeProp, deletedProp);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActivePropositionsWithDetailsAsync();

        // Assert
        Assert.AreEqual(1, result.Count());
        Assert.AreEqual("Active Prop", result.First().Title);
    }

    [TestMethod]
    public async Task GetAllPropositionsWithDetailsAsync_ShouldReturnActivePropositions()
    {
        // Arrange
        var prop1 = new PropositionBo
        {
            Title = "Prop 1",
            Description = "Description 1",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        var prop2 = new PropositionBo
        {
            Title = "Prop 2",
            Description = "Description 2",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        await _context.Propositions.AddRangeAsync(prop1, prop2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetAllPropositionsWithDetailsAsync();

        // Assert
        Assert.AreEqual(2, result.Count());
    }

    [TestMethod]
    public async Task GetActivePropositionWithDetailsAsync_ShouldReturnProposition_WhenActive()
    {
        // Arrange
        var proposition = new PropositionBo
        {
            Title = "Test Prop",
            Description = "Description",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        await _context.Propositions.AddAsync(proposition);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActivePropositionWithDetailsAsync(proposition.Id);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("Test Prop", result.Title);
        Assert.IsNotNull(result.User);
    }

    [TestMethod]
    public async Task GetActivePropositionWithDetailsAsync_ShouldReturnNull_WhenDeleted()
    {
        // Arrange
        var proposition = new PropositionBo
        {
            Title = "Deleted Prop",
            Description = "Description",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        };
        await _context.Propositions.AddAsync(proposition);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetActivePropositionWithDetailsAsync(proposition.Id);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SoftDeleteAsync_ShouldMarkPropositionAsDeleted()
    {
        // Arrange
        var proposition = new PropositionBo
        {
            Title = "Test Prop",
            Description = "Description",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        await _context.Propositions.AddAsync(proposition);
        await _context.SaveChangesAsync();

        // Act
        await _repository.SoftDeleteAsync(proposition);
        await _context.SaveChangesAsync();

        // Assert
        var deleted = await _context.Propositions.FindAsync(proposition.Id);
        Assert.IsNotNull(deleted);
        Assert.IsTrue(deleted.IsDeleted);
        Assert.IsNotNull(deleted.DeletedAt);
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldReturnPagedActivePropositions()
    {
        // Arrange
        for (int i = 1; i <= 15; i++)
        {
            await _context.Propositions.AddAsync(new PropositionBo
            {
                Title = $"Prop {i}",
                Description = "Description",
                UserId = _testUser.Id,
                IsDeleted = false,
                CreatedAt = DateTime.Now.AddDays(-i)
            });
        }
        // Add deleted propositions
        await _context.Propositions.AddAsync(new PropositionBo
        {
            Title = "Deleted",
            Description = "Desc",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now,
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedAsync(0, 10);

        // Assert
        Assert.AreEqual(15, totalCount);
        Assert.AreEqual(10, items.Count);
        Assert.IsTrue(items.All(p => !p.IsDeleted));
    }

    [TestMethod]
    public async Task GetPagedAsync_ShouldOrderByCreatedAtDescending()
    {
        // Arrange
        var prop1 = new PropositionBo
        {
            Title = "Oldest",
            Description = "Desc",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now.AddDays(-2)
        };
        var prop2 = new PropositionBo
        {
            Title = "Newest",
            Description = "Desc",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        await _context.Propositions.AddRangeAsync(prop1, prop2);
        await _context.SaveChangesAsync();

        // Act
        var (items, _) = await _repository.GetPagedAsync(0, 10);

        // Assert
        Assert.AreEqual("Newest", items.First().Title);
        Assert.AreEqual("Oldest", items.Last().Title);
    }

    #region GetPagedWithFilterAsync Tests

    [TestMethod]
    public async Task GetPagedWithFilterAsync_FilterActive_ReturnsOnlyActivePropositions()
    {
        // Arrange
        var activeProposition1 = new PropositionBo
        {
            Title = "Active 1",
            Description = "Test active 1",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now.AddDays(-1)
        };
        var activeProposition2 = new PropositionBo
        {
            Title = "Active 2",
            Description = "Test active 2",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        var deletedProposition = new PropositionBo
        {
            Title = "Deleted",
            Description = "Test deleted",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now,
            CreatedAt = DateTime.Now.AddDays(-2)
        };

        await _context.Propositions.AddRangeAsync(activeProposition1, activeProposition2, deletedProposition);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedWithFilterAsync(0, 10, Bo.Enums.DeletedStatus.Active);

        // Assert
        Assert.AreEqual(2, totalCount);
        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.All(p => !p.IsDeleted));
        Assert.IsTrue(items.Any(p => p.Title == "Active 1"));
        Assert.IsTrue(items.Any(p => p.Title == "Active 2"));
    }

    [TestMethod]
    public async Task GetPagedWithFilterAsync_FilterDeleted_ReturnsOnlyDeletedPropositions()
    {
        // Arrange
        var activeProposition = new PropositionBo
        {
            Title = "Active",
            Description = "Test active",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        var deletedProposition1 = new PropositionBo
        {
            Title = "Deleted 1",
            Description = "Test deleted 1",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now.AddDays(-1),
            CreatedAt = DateTime.Now.AddDays(-2)
        };
        var deletedProposition2 = new PropositionBo
        {
            Title = "Deleted 2",
            Description = "Test deleted 2",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now,
            CreatedAt = DateTime.Now.AddDays(-1)
        };

        await _context.Propositions.AddRangeAsync(activeProposition, deletedProposition1, deletedProposition2);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedWithFilterAsync(0, 10, Bo.Enums.DeletedStatus.Deleted);

        // Assert
        Assert.AreEqual(2, totalCount);
        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.All(p => p.IsDeleted));
        Assert.IsTrue(items.Any(p => p.Title == "Deleted 1"));
        Assert.IsTrue(items.Any(p => p.Title == "Deleted 2"));
    }

    [TestMethod]
    public async Task GetPagedWithFilterAsync_FilterAll_ReturnsAllPropositions()
    {
        // Arrange
        var activeProposition = new PropositionBo
        {
            Title = "Active",
            Description = "Test active",
            UserId = _testUser.Id,
            IsDeleted = false,
            CreatedAt = DateTime.Now
        };
        var deletedProposition = new PropositionBo
        {
            Title = "Deleted",
            Description = "Test deleted",
            UserId = _testUser.Id,
            IsDeleted = true,
            DeletedAt = DateTime.Now,
            CreatedAt = DateTime.Now.AddDays(-1)
        };

        await _context.Propositions.AddRangeAsync(activeProposition, deletedProposition);
        await _context.SaveChangesAsync();

        // Act
        var (items, totalCount) = await _repository.GetPagedWithFilterAsync(0, 10, Bo.Enums.DeletedStatus.All);

        // Assert
        Assert.AreEqual(2, totalCount);
        Assert.AreEqual(2, items.Count);
        Assert.IsTrue(items.Any(p => p.Title == "Active" && !p.IsDeleted));
        Assert.IsTrue(items.Any(p => p.Title == "Deleted" && p.IsDeleted));
    }

    #endregion
}
