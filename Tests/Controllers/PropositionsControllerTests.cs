using Business.Interfaces;
using Dto;
using Dto.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class PropositionsControllerTests
    {
        private Mock<IPropositionService> _mockPropositionService = null!;
        private Mock<ILogger<PropositionsController>> _mockLogger = null!;
        private PropositionsController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockPropositionService = new Mock<IPropositionService>();
            _mockLogger = new Mock<ILogger<PropositionsController>>();
            _controller = new PropositionsController(_mockPropositionService.Object, _mockLogger.Object);

            SetupAuthenticatedUser(TestUserEmail);
        }

        private void SetupAuthenticatedUser(string email)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("userId", "1")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #region GetPropositions Tests

        [TestMethod]
        public async Task GetPropositions_ReturnsOkResult_WithListOfPropositions()
        {
            // Arrange
            var propositions = new List<PropositionDto>
            {
                new() { Id = 1, Title = "Proposition A", Description = "Description A" },
                new() { Id = 2, Title = "Proposition B", Description = "Description B" }
            };
            var pagedResult = new PagedResult<PropositionDto>(propositions, 2, 1, 20);
            _mockPropositionService.Setup(s => s.GetAllPropositionsAsync(It.IsAny<PaginationParams>(), It.IsAny<string?>())).ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetPropositions(pagination);

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as PagedResult<PropositionDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(2, returned.Items.Count);
            Assert.AreEqual(2, returned.TotalCount);
        }

        [TestMethod]
        public async Task GetPropositions_ExcludesDeletedPropositions()
        {
            // Arrange
            var propositions = new List<PropositionDto>
            {
                new() { Id = 1, Title = "Active", Description = "Active proposition" }
            };
            var pagedResult = new PagedResult<PropositionDto>(propositions, 1, 1, 20);
            _mockPropositionService.Setup(s => s.GetAllPropositionsAsync(It.IsAny<PaginationParams>(), It.IsAny<string?>())).ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetPropositions(pagination);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var returned = okResult.Value as PagedResult<PropositionDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(1, returned.Items.Count);
            Assert.AreEqual(1, returned.TotalCount);
        }

        #endregion

        #region GetProposition Tests

        [TestMethod]
        public async Task GetProposition_ReturnsOkResult_WithValidId()
        {
            // Arrange
            var propositionDto = new PropositionDto { Id = 1, Title = "Test Proposition", Description = "Test Description" };
            _mockPropositionService.Setup(s => s.GetPropositionByIdAsync(1, It.IsAny<string?>())).ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.GetProposition(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Test Proposition", dto.Title);
        }

        [TestMethod]
        public async Task GetProposition_ReturnsNotFound_WhenInvalidId()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.GetPropositionByIdAsync(999, It.IsAny<string?>())).ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.GetProposition(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task GetProposition_ReturnsNotFound_WhenPropositionIsDeleted()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.GetPropositionByIdAsync(1)).ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.GetProposition(1);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region PostProposition Tests

        [TestMethod]
        public async Task PostProposition_ReturnsCreatedAtAction_WithValidDto()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Title = "New Proposition",
                Description = "New Description"
            };
            var createdProposition = new PropositionDto
            {
                Id = 1,
                Title = propositionDto.Title,
                Description = propositionDto.Description
            };
            _mockPropositionService.Setup(s => s.CreatePropositionAsync(propositionDto, TestUserEmail))
                .ReturnsAsync(createdProposition);

            // Act
            var result = await _controller.PostProposition(propositionDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            Assert.AreEqual(nameof(_controller.GetProposition), createdResult.ActionName);
            var dto = createdResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("New Proposition", dto.Title);
        }

        [TestMethod]
        public async Task PostProposition_ReturnsBadRequest_WhenModelStateInvalid()
        {
            // Arrange
            _controller.ModelState.AddModelError("Title", "Required");
            var propositionDto = new PropositionDto();

            // Act
            var result = await _controller.PostProposition(propositionDto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task PostProposition_ReturnsNotFound_WhenUserNotFound()
        {
            // Arrange
            var propositionDto = new PropositionDto { Title = "Test", Description = "Test" };
            _mockPropositionService.Setup(s => s.CreatePropositionAsync(It.IsAny<PropositionDto>(), It.IsAny<string>()))
                .ThrowsAsync(new UnauthorizedAccessException("User not found"));

            // Act
            var result = await _controller.PostProposition(propositionDto);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        #endregion

        #region PutProposition Tests

        [TestMethod]
        public async Task PutProposition_ReturnsOkResult_WithValidDto()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Updated Proposition",
                Description = "Updated Description"
            };
            _mockPropositionService.Setup(s => s.UpdatePropositionAsync(1, propositionDto, It.IsAny<string>()))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.PutProposition(1, propositionDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("Updated Proposition", dto.Title);
        }

        [TestMethod]
        public async Task PutProposition_ReturnsNotFound_WhenPropositionDoesNotExist()
        {
            // Arrange
            var propositionDto = new PropositionDto { Id = 999, Title = "Test", Description = "Test" };
            _mockPropositionService.Setup(s => s.UpdatePropositionAsync(999, propositionDto, It.IsAny<string>()))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.PutProposition(999, propositionDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task PutProposition_ReturnsNotFound_WhenPropositionIsDeleted()
        {
            // Arrange
            var propositionDto = new PropositionDto { Id = 1, Title = "Test", Description = "Test" };
            _mockPropositionService.Setup(s => s.UpdatePropositionAsync(1, propositionDto, It.IsAny<string>()))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.PutProposition(1, propositionDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region DeleteProposition Tests

        [TestMethod]
        public async Task DeleteProposition_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            var deletedProposition = new PropositionDto { Id = 1, Title = "Deleted Proposition", Description = "Deleted Description" };
            _mockPropositionService.Setup(s => s.DeletePropositionAsync(1, It.IsAny<string>()))
                .ReturnsAsync(deletedProposition);

            // Act
            var result = await _controller.DeleteProposition(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task DeleteProposition_ReturnsNotFound_WhenPropositionDoesNotExist()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.DeletePropositionAsync(999, It.IsAny<string>()))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.DeleteProposition(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        [TestMethod]
        public async Task DeleteProposition_PerformsSoftDelete()
        {
            // Arrange
            var deletedProposition = new PropositionDto
            {
                Id = 1,
                Title = "Soft Deleted",
                Description = "Description"
            };
            _mockPropositionService.Setup(s => s.DeletePropositionAsync(1, It.IsAny<string>()))
                .ReturnsAsync(deletedProposition);

            // Act
            var result = await _controller.DeleteProposition(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
            _mockPropositionService.Verify(s => s.DeletePropositionAsync(1, It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region VoteUp Tests

        [TestMethod]
        public async Task VoteUp_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Test Proposition",
                Description = "Test Description",
                VotesUp = 6,
                VotesDown = 2
            };
            _mockPropositionService.Setup(s => s.VoteUpAsync(1, TestUserEmail))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.VoteUp(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(1, dto.Id);
            Assert.AreEqual(6, dto.VotesUp);
            _mockPropositionService.Verify(s => s.VoteUpAsync(1, TestUserEmail), Times.Once);
        }

        [TestMethod]
        public async Task VoteUp_ReturnsNotFound_WhenPropositionDoesNotExist()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.VoteUpAsync(999, TestUserEmail))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.VoteUp(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task VoteUp_ReturnsNotFound_WhenPropositionIsDeleted()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.VoteUpAsync(1, TestUserEmail))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.VoteUp(1);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task VoteUp_ReturnsUnauthorized_WhenUserNotFound()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.VoteUpAsync(1, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException("User not found"));

            // Act
            var result = await _controller.VoteUp(1);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task VoteUp_IncrementsVotesUp_WhenNewVote()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                VotesUp = 6, // Was 5, now 6
                VotesDown = 2
            };
            _mockPropositionService.Setup(s => s.VoteUpAsync(1, TestUserEmail))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.VoteUp(1);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(6, dto.VotesUp);
        }

        [TestMethod]
        public async Task VoteUp_UpdatesCounters_WhenChangingFromDownToUp()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                VotesUp = 6,   // Incremented
                VotesDown = 1  // Decremented
            };
            _mockPropositionService.Setup(s => s.VoteUpAsync(1, TestUserEmail))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.VoteUp(1);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(6, dto.VotesUp);
            Assert.AreEqual(1, dto.VotesDown);
        }

        #endregion

        #region VoteDown Tests

        [TestMethod]
        public async Task VoteDown_ReturnsOkResult_WhenSuccessful()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Test Proposition",
                Description = "Test Description",
                VotesUp = 5,
                VotesDown = 3
            };
            _mockPropositionService.Setup(s => s.VoteDownAsync(1, TestUserEmail))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.VoteDown(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(1, dto.Id);
            Assert.AreEqual(3, dto.VotesDown);
            _mockPropositionService.Verify(s => s.VoteDownAsync(1, TestUserEmail), Times.Once);
        }

        [TestMethod]
        public async Task VoteDown_ReturnsNotFound_WhenPropositionDoesNotExist()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.VoteDownAsync(999, TestUserEmail))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.VoteDown(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task VoteDown_ReturnsNotFound_WhenPropositionIsDeleted()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.VoteDownAsync(1, TestUserEmail))
                .ReturnsAsync((PropositionDto?)null);

            // Act
            var result = await _controller.VoteDown(1);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        [TestMethod]
        public async Task VoteDown_ReturnsUnauthorized_WhenUserNotFound()
        {
            // Arrange
            _mockPropositionService.Setup(s => s.VoteDownAsync(1, TestUserEmail))
                .ThrowsAsync(new UnauthorizedAccessException("User not found"));

            // Act
            var result = await _controller.VoteDown(1);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task VoteDown_IncrementsVotesDown_WhenNewVote()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                VotesUp = 5,
                VotesDown = 3 // Was 2, now 3
            };
            _mockPropositionService.Setup(s => s.VoteDownAsync(1, TestUserEmail))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.VoteDown(1);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(3, dto.VotesDown);
        }

        [TestMethod]
        public async Task VoteDown_UpdatesCounters_WhenChangingFromUpToDown()
        {
            // Arrange
            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = "Test",
                Description = "Test",
                VotesUp = 4,   // Decremented
                VotesDown = 3  // Incremented
            };
            _mockPropositionService.Setup(s => s.VoteDownAsync(1, TestUserEmail))
                .ReturnsAsync(propositionDto);

            // Act
            var result = await _controller.VoteDown(1);

            // Assert
            var okResult = (OkObjectResult)result.Result!;
            var dto = okResult.Value as PropositionDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(4, dto.VotesUp);
            Assert.AreEqual(3, dto.VotesDown);
        }

        #endregion
    }
}
