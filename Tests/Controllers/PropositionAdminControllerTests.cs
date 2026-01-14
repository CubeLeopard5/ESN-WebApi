using Business.Interfaces;
using Bo.Enums;
using Dto;
using Dto.Common;
using Dto.Proposition;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers;

[TestClass]
public class PropositionAdminControllerTests
{
    private Mock<IPropositionService> _mockPropositionService = null!;
    private Mock<ILogger<PropositionAdminController>> _mockLogger = null!;
    private PropositionAdminController _controller = null!;
    private const string TestAdminEmail = "admin@example.com";

    [TestInitialize]
    public void Setup()
    {
        _mockPropositionService = new Mock<IPropositionService>();
        _mockLogger = new Mock<ILogger<PropositionAdminController>>();
        _controller = new PropositionAdminController(_mockPropositionService.Object, _mockLogger.Object);

        SetupAuthenticatedUser(TestAdminEmail);
    }

    private void SetupAuthenticatedUser(string email)
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, email),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("userId", "1")
        }, "mock"));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    #region GetAllPropositions Tests

    [TestMethod]
    public async Task GetAllPropositions_WithActiveFilter_ReturnsOk()
    {
        // Arrange
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PropositionFilterDto { Status = DeletedStatus.Active };
        var propositions = new List<PropositionDto>
        {
            new() { Id = 1, Title = "Active Proposition 1" },
            new() { Id = 2, Title = "Active Proposition 2" }
        };
        var pagedResult = new PagedResult<PropositionDto>(propositions, 2, 1, 10);

        _mockPropositionService
            .Setup(s => s.GetAllPropositionsForAdminAsync(
                It.IsAny<PaginationParams>(),
                It.IsAny<PropositionFilterDto>(),
                It.IsAny<string?>()))
            .ReturnsAsync(pagedResult);

        // Act
        var result = await _controller.GetAllPropositions(pagination, filter);

        // Assert
        Assert.IsNotNull(result.Result);
        Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        var okResult = (OkObjectResult)result.Result;
        var returned = okResult.Value as PagedResult<PropositionDto>;
        Assert.IsNotNull(returned);
        Assert.AreEqual(2, returned.Items.Count);
        Assert.AreEqual(2, returned.TotalCount);
        _mockPropositionService.Verify(s => s.GetAllPropositionsForAdminAsync(
            It.IsAny<PaginationParams>(),
            It.IsAny<PropositionFilterDto>(),
            TestAdminEmail), Times.Once);
    }

    [TestMethod]
    public async Task GetAllPropositions_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var pagination = new PaginationParams { PageNumber = 1, PageSize = 10 };
        var filter = new PropositionFilterDto { Status = DeletedStatus.Active };

        _mockPropositionService
            .Setup(s => s.GetAllPropositionsForAdminAsync(
                It.IsAny<PaginationParams>(),
                It.IsAny<PropositionFilterDto>(),
                It.IsAny<string?>()))
            .ThrowsAsync(new UnauthorizedAccessException("Access denied"));

        // Act
        var result = await _controller.GetAllPropositions(pagination, filter);

        // Assert
        Assert.IsNotNull(result.Result);
        Assert.IsInstanceOfType<ObjectResult>(result.Result);
        var objectResult = (ObjectResult)result.Result;
        Assert.AreEqual(403, objectResult.StatusCode);
    }

    #endregion

    #region DeleteProposition Tests

    [TestMethod]
    public async Task DeleteProposition_WhenAuthorized_ReturnsNoContent()
    {
        // Arrange
        var propositionId = 1;
        var deletedProposition = new PropositionDto { Id = propositionId, Title = "Deleted Proposition" };

        _mockPropositionService
            .Setup(s => s.DeletePropositionAsAdminAsync(propositionId, TestAdminEmail))
            .ReturnsAsync(deletedProposition);

        // Act
        var result = await _controller.DeleteProposition(propositionId);

        // Assert
        Assert.IsInstanceOfType<NoContentResult>(result);
        _mockPropositionService.Verify(s => s.DeletePropositionAsAdminAsync(propositionId, TestAdminEmail), Times.Once);
    }

    [TestMethod]
    public async Task DeleteProposition_WhenNotFound_ReturnsNotFound()
    {
        // Arrange
        var propositionId = 999;

        _mockPropositionService
            .Setup(s => s.DeletePropositionAsAdminAsync(propositionId, TestAdminEmail))
            .ReturnsAsync((PropositionDto?)null);

        // Act
        var result = await _controller.DeleteProposition(propositionId);

        // Assert
        Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        var notFoundResult = (NotFoundObjectResult)result;
        Assert.IsNotNull(notFoundResult.Value);
    }

    [TestMethod]
    public async Task DeleteProposition_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var propositionId = 1;

        _mockPropositionService
            .Setup(s => s.DeletePropositionAsAdminAsync(propositionId, TestAdminEmail))
            .ThrowsAsync(new UnauthorizedAccessException("You don't have permission"));

        // Act
        var result = await _controller.DeleteProposition(propositionId);

        // Assert
        Assert.IsInstanceOfType<ObjectResult>(result);
        var objectResult = (ObjectResult)result;
        Assert.AreEqual(403, objectResult.StatusCode);
    }

    #endregion
}
