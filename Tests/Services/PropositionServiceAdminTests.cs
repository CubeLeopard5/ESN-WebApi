using AutoMapper;
using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.Proposition;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto;
using Dto.Common;
using Dto.Proposition;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services;

[TestClass]
public class PropositionServiceAdminTests
{
    private Mock<IUnitOfWork> _mockUnitOfWork = null!;
    private Mock<IMapper> _mockMapper = null!;
    private Mock<ILogger<PropositionService>> _mockLogger = null!;
    private Mock<IPropositionRepository> _mockPropositionRepository = null!;
    private Mock<IUserRepository> _mockUserRepository = null!;
    private Mock<IPropositionVoteRepository> _mockPropositionVoteRepository = null!;
    private PropositionService _propositionService = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<PropositionService>>();
        _mockPropositionRepository = new Mock<IPropositionRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockPropositionVoteRepository = new Mock<IPropositionVoteRepository>();

        _mockUnitOfWork.Setup(u => u.Propositions).Returns(_mockPropositionRepository.Object);
        _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
        _mockUnitOfWork.Setup(u => u.PropositionVotes).Returns(_mockPropositionVoteRepository.Object);

        _propositionService = new PropositionService(
            _mockUnitOfWork.Object,
            _mockMapper.Object,
            _mockLogger.Object
        );
    }

    #region GetAllPropositionsForAdminAsync Tests

    [TestMethod]
    public async Task GetAllPropositionsForAdminAsync_WithActiveFilter_ReturnsOnlyActivePropositions()
    {
        // Arrange
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PropositionFilterDto { Status = DeletedStatus.Active };
        var userEmail = "admin@test.com";

        var propositionsBo = new List<PropositionBo>
        {
            new PropositionBo
            {
                Id = 1,
                Title = "Active Proposition 1",
                Description = "Description 1",
                IsDeleted = false,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            },
            new PropositionBo
            {
                Id = 2,
                Title = "Active Proposition 2",
                Description = "Description 2",
                IsDeleted = false,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        var propositionDtos = new List<PropositionDto>
        {
            new PropositionDto { Id = 1, Title = "Active Proposition 1" },
            new PropositionDto { Id = 2, Title = "Active Proposition 2" }
        };

        var user = new UserBo { Id = 1, Email = userEmail };

        _mockPropositionRepository.Setup(r => r.GetPagedWithFilterAsync(0, 10, DeletedStatus.Active))
            .ReturnsAsync((propositionsBo, 2));
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((PropositionVoteBo?)null);
        _mockMapper.Setup(m => m.Map<IEnumerable<PropositionDto>>(propositionsBo))
            .Returns(propositionDtos);

        // Act
        var result = await _propositionService.GetAllPropositionsForAdminAsync(pagination, filter, userEmail);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
        Assert.AreEqual("Active Proposition 1", result.Items.First().Title);
        _mockPropositionRepository.Verify(r => r.GetPagedWithFilterAsync(0, 10, DeletedStatus.Active), Times.Once);
    }

    [TestMethod]
    public async Task GetAllPropositionsForAdminAsync_WithDeletedFilter_ReturnsOnlyDeletedPropositions()
    {
        // Arrange
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PropositionFilterDto { Status = DeletedStatus.Deleted };
        var userEmail = "admin@test.com";

        var propositionsBo = new List<PropositionBo>
        {
            new PropositionBo
            {
                Id = 1,
                Title = "Deleted Proposition 1",
                Description = "Description 1",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            },
            new PropositionBo
            {
                Id = 2,
                Title = "Deleted Proposition 2",
                Description = "Description 2",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        var propositionDtos = new List<PropositionDto>
        {
            new PropositionDto { Id = 1, Title = "Deleted Proposition 1" },
            new PropositionDto { Id = 2, Title = "Deleted Proposition 2" }
        };

        var user = new UserBo { Id = 1, Email = userEmail };

        _mockPropositionRepository.Setup(r => r.GetPagedWithFilterAsync(0, 10, DeletedStatus.Deleted))
            .ReturnsAsync((propositionsBo, 2));
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((PropositionVoteBo?)null);
        _mockMapper.Setup(m => m.Map<IEnumerable<PropositionDto>>(propositionsBo))
            .Returns(propositionDtos);

        // Act
        var result = await _propositionService.GetAllPropositionsForAdminAsync(pagination, filter, userEmail);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
        Assert.AreEqual("Deleted Proposition 1", result.Items.First().Title);
        _mockPropositionRepository.Verify(r => r.GetPagedWithFilterAsync(0, 10, DeletedStatus.Deleted), Times.Once);
    }

    [TestMethod]
    public async Task GetAllPropositionsForAdminAsync_WithAllFilter_ReturnsAllPropositions()
    {
        // Arrange
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PropositionFilterDto { Status = DeletedStatus.All };
        var userEmail = "admin@test.com";

        var propositionsBo = new List<PropositionBo>
        {
            new PropositionBo
            {
                Id = 1,
                Title = "Active Proposition",
                Description = "Description 1",
                IsDeleted = false,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            },
            new PropositionBo
            {
                Id = 2,
                Title = "Deleted Proposition",
                Description = "Description 2",
                IsDeleted = true,
                DeletedAt = DateTime.UtcNow,
                UserId = 1,
                CreatedAt = DateTime.UtcNow
            }
        };

        var propositionDtos = new List<PropositionDto>
        {
            new PropositionDto { Id = 1, Title = "Active Proposition" },
            new PropositionDto { Id = 2, Title = "Deleted Proposition" }
        };

        var user = new UserBo { Id = 1, Email = userEmail };

        _mockPropositionRepository.Setup(r => r.GetPagedWithFilterAsync(0, 10, DeletedStatus.All))
            .ReturnsAsync((propositionsBo, 2));
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
        _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync((PropositionVoteBo?)null);
        _mockMapper.Setup(m => m.Map<IEnumerable<PropositionDto>>(propositionsBo))
            .Returns(propositionDtos);

        // Act
        var result = await _propositionService.GetAllPropositionsForAdminAsync(pagination, filter, userEmail);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count());
        Assert.IsTrue(result.Items.Any(p => p.Title == "Active Proposition"));
        Assert.IsTrue(result.Items.Any(p => p.Title == "Deleted Proposition"));
        _mockPropositionRepository.Verify(r => r.GetPagedWithFilterAsync(0, 10, DeletedStatus.All), Times.Once);
    }

    #endregion

    #region DeletePropositionAsAdminAsync Tests

    [TestMethod]
    public async Task DeletePropositionAsAdminAsync_WhenEsnMember_ShouldDeleteSuccessfully()
    {
        // Arrange
        var propositionId = 1;
        var userEmail = "esnmember@test.com";
        var esnMember = new UserBo
        {
            Id = 2,
            Email = userEmail,
            FirstName = "ESN",
            LastName = "Member",
            BirthDate = DateTime.Now.AddYears(-25),
            StudentType = Bo.Constants.StudentType.EsnMember
        };

        var propositionBo = new PropositionBo
        {
            Id = propositionId,
            Title = "Test Proposition",
            Description = "Test Description",
            IsDeleted = false,
            UserId = 1, // Different owner
            CreatedAt = DateTime.UtcNow
        };

        var propositionDto = new PropositionDto
        {
            Id = propositionId,
            Title = "Test Proposition"
        };

        _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
            .ReturnsAsync(propositionBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
            .ReturnsAsync(esnMember);
        _mockMapper.Setup(m => m.Map<PropositionDto>(propositionBo))
            .Returns(propositionDto);

        // Act
        var result = await _propositionService.DeletePropositionAsAdminAsync(propositionId, userEmail);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(propositionBo.IsDeleted);
        Assert.IsNotNull(propositionBo.DeletedAt);
        _mockPropositionRepository.Verify(r => r.Update(propositionBo), Times.Once);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [TestMethod]
    public async Task DeletePropositionAsAdminAsync_WhenRegularUser_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var propositionId = 1;
        var userEmail = "regularuser@test.com";
        var regularUser = new UserBo
        {
            Id = 2,
            Email = userEmail,
            FirstName = "Regular",
            LastName = "User",
            BirthDate = DateTime.Now.AddYears(-25),
            StudentType = Bo.Constants.StudentType.Local // NOT ESN member, NOT Admin
        };

        var propositionBo = new PropositionBo
        {
            Id = propositionId,
            Title = "Test Proposition",
            Description = "Test Description",
            IsDeleted = false,
            UserId = 1, // Different owner
            CreatedAt = DateTime.UtcNow
        };

        _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
            .ReturnsAsync(propositionBo);
        _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
            .ReturnsAsync(regularUser);

        // Act & Assert
        var exceptionThrown = false;
        try
        {
            await _propositionService.DeletePropositionAsAdminAsync(propositionId, userEmail);
        }
        catch (UnauthorizedAccessException)
        {
            exceptionThrown = true;
        }
        Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");

        // Verify no update or save was called
        _mockPropositionRepository.Verify(r => r.Update(It.IsAny<PropositionBo>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    #endregion
}
