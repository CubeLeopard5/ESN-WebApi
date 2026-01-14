using AutoMapper;
using Bo.Constants;
using Bo.Models;
using Business.Proposition;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services
{
    [TestClass]
    public class PropositionServiceTests
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

        #region GetAllPropositions Tests

        [TestMethod]
        public async Task GetAllPropositionsAsync_ReturnsListOfPropositions()
        {
            // Arrange
            var propositionsBo = new List<PropositionBo>
            {
                new PropositionBo
                {
                    Id = 1,
                    Title = "Proposition 1",
                    Description = "Description 1",
                    IsDeleted = false
                },
                new PropositionBo
                {
                    Id = 2,
                    Title = "Proposition 2",
                    Description = "Description 2",
                    IsDeleted = false
                }
            };

            var propositionDtos = new List<PropositionDto>
            {
                new PropositionDto { Id = 1, Title = "Proposition 1" },
                new PropositionDto { Id = 2, Title = "Proposition 2" }
            };

            _mockPropositionRepository.Setup(r => r.GetAllPropositionsWithDetailsAsync())
                .ReturnsAsync(propositionsBo);
            _mockMapper.Setup(m => m.Map<IEnumerable<PropositionDto>>(propositionsBo))
                .Returns(propositionDtos);

            // Act
            var result = await _propositionService.GetAllPropositionsAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region GetPropositionById Tests

        [TestMethod]
        public async Task GetPropositionByIdAsync_ExistingProposition_ReturnsPropositionDto()
        {
            // Arrange
            var propositionId = 1;
            var propositionBo = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                IsDeleted = false
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description"
            };

            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId))
                .ReturnsAsync(propositionBo);
            _mockMapper.Setup(m => m.Map<PropositionDto>(propositionBo))
                .Returns(propositionDto);

            // Act
            var result = await _propositionService.GetPropositionByIdAsync(propositionId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(propositionId, result.Id);
            Assert.AreEqual("Test Proposition", result.Title);
        }

        [TestMethod]
        public async Task GetPropositionByIdAsync_NonExistingProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 999;
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId))
                .ReturnsAsync((PropositionBo?)null);

            // Act
            var result = await _propositionService.GetPropositionByIdAsync(propositionId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region CreateProposition Tests

        [TestMethod]
        public async Task CreatePropositionAsync_ValidProposition_ReturnsPropositionDto()
        {
            // Arrange
            var userEmail = "test@example.com";
            var createDto = new PropositionDto
            {
                Title = "New Proposition",
                Description = "Test Description"
            };

            var user = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.International
            };

            var propositionBo = new PropositionBo
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description,
                UserId = user.Id,
                IsDeleted = false
            };

            var propositionDto = new PropositionDto
            {
                Id = 1,
                Title = createDto.Title,
                Description = createDto.Description
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(user);
            _mockMapper.Setup(m => m.Map<PropositionBo>(createDto))
                .Returns(propositionBo);
            _mockPropositionRepository.Setup(r => r.AddAsync(It.IsAny<PropositionBo>()))
                .ReturnsAsync(propositionBo);
            _mockMapper.Setup(m => m.Map<PropositionDto>(It.IsAny<PropositionBo>()))
                .Returns(propositionDto);

            // Act
            var result = await _propositionService.CreatePropositionAsync(createDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(createDto.Title, result.Title);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreatePropositionAsync_UserNotFound_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var userEmail = "notfound@example.com";
            var createDto = new PropositionDto
            {
                Title = "New Proposition",
                Description = "Test Description"
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync((UserBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _propositionService.CreatePropositionAsync(createDto, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        #endregion

        #region UpdateProposition Tests

        [TestMethod]
        public async Task UpdatePropositionAsync_ValidUpdate_ReturnsUpdatedPropositionDto()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "owner@test.com";
            var updateDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Updated Proposition",
                Description = "Updated Description"
            };

            var owner = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Owner",
                LastName = "User"
            };

            var existingProposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Old Proposition",
                Description = "Old Description",
                IsDeleted = false,
                UserId = 1
            };

            var updatedPropositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Updated Proposition",
                Description = "Updated Description"
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(existingProposition);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(owner);
            _mockMapper.Setup(m => m.Map(updateDto, existingProposition))
                .Returns(existingProposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(existingProposition))
                .Returns(updatedPropositionDto);

            // Act
            var result = await _propositionService.UpdatePropositionAsync(propositionId, updateDto, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated Proposition", result.Title);
            _mockPropositionRepository.Verify(r => r.Update(existingProposition), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePropositionAsync_NonExistingProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 999;
            var userEmail = "owner@test.com";
            var updateDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Updated Proposition"
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync((PropositionBo?)null);

            // Act
            var result = await _propositionService.UpdatePropositionAsync(propositionId, updateDto, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockPropositionRepository.Verify(r => r.Update(It.IsAny<PropositionBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task UpdatePropositionAsync_DeletedProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "owner@test.com";
            var updateDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Updated Proposition"
            };

            var deletedProposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Deleted Proposition",
                IsDeleted = true,
                UserId = 1
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(deletedProposition);

            // Act
            var result = await _propositionService.UpdatePropositionAsync(propositionId, updateDto, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockPropositionRepository.Verify(r => r.Update(It.IsAny<PropositionBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region DeleteProposition Tests

        [TestMethod]
        public async Task DeletePropositionAsync_ExistingProposition_SoftDeletesProposition()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "owner@test.com";
            var owner = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Owner",
                LastName = "User"
            };

            var propositionBo = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                IsDeleted = false,
                UserId = 1
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition"
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(propositionBo);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(owner);
            _mockMapper.Setup(m => m.Map<PropositionDto>(propositionBo))
                .Returns(propositionDto);

            // Act
            var result = await _propositionService.DeletePropositionAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(propositionId, result.Id);
            Assert.IsTrue(propositionBo.IsDeleted);
            Assert.IsNotNull(propositionBo.DeletedAt);
            _mockPropositionRepository.Verify(r => r.Update(propositionBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeletePropositionAsync_NonExistingProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 999;
            var userEmail = "owner@test.com";
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync((PropositionBo?)null);

            // Act
            var result = await _propositionService.DeletePropositionAsync(propositionId, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockPropositionRepository.Verify(r => r.Update(It.IsAny<PropositionBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task DeletePropositionAsync_AlreadyDeletedProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "owner@test.com";
            var deletedProposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Deleted Proposition",
                IsDeleted = true,
                UserId = 1
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(deletedProposition);

            // Act
            var result = await _propositionService.DeletePropositionAsync(propositionId, userEmail);

            // Assert
            Assert.IsNull(result);
            _mockPropositionRepository.Verify(r => r.Update(It.IsAny<PropositionBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region DeleteProposition Authorization Tests

        [TestMethod]
        public async Task DeletePropositionAsync_WhenUserIsOwner_ShouldDeleteSuccessfully()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "owner@test.com";
            var owner = new UserBo
            {
                Id = 1,
                Email = userEmail,
                FirstName = "Owner",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.Local // Regular user, NOT ESN member
            };

            var propositionBo = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                IsDeleted = false,
                UserId = owner.Id // Owner
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition"
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(propositionBo);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(owner);
            _mockMapper.Setup(m => m.Map<PropositionDto>(propositionBo))
                .Returns(propositionDto);

            // Act
            var result = await _propositionService.DeletePropositionAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(propositionBo.IsDeleted);
            Assert.IsNotNull(propositionBo.DeletedAt);
            _mockPropositionRepository.Verify(r => r.Update(propositionBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeletePropositionAsync_WhenUserIsEsnMember_ShouldDeleteSuccessfully()
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
                StudentType = StudentType.EsnMember // ESN Member
            };

            var propositionBo = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                IsDeleted = false,
                UserId = 1 // Different owner
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
            var result = await _propositionService.DeletePropositionAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(propositionBo.IsDeleted);
            Assert.IsNotNull(propositionBo.DeletedAt);
            _mockPropositionRepository.Verify(r => r.Update(propositionBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeletePropositionAsync_WhenUserIsAdmin_ShouldDeleteSuccessfully()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "admin@test.com";
            var admin = new UserBo
            {
                Id = 3,
                Email = userEmail,
                FirstName = "Admin",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = StudentType.Local,
                Role = new RoleBo { Id = 1, Name = UserRole.Admin } // Admin role
            };

            var propositionBo = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                IsDeleted = false,
                UserId = 1 // Different owner
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition"
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(propositionBo);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(admin);
            _mockMapper.Setup(m => m.Map<PropositionDto>(propositionBo))
                .Returns(propositionDto);

            // Act
            var result = await _propositionService.DeletePropositionAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsTrue(propositionBo.IsDeleted);
            Assert.IsNotNull(propositionBo.DeletedAt);
            _mockPropositionRepository.Verify(r => r.Update(propositionBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeletePropositionAsync_WhenUserIsNeitherOwnerNorEsnMemberNorAdmin_ShouldThrowUnauthorizedException()
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
                StudentType = StudentType.Local // NOT ESN member
                // No Admin role
            };

            var propositionBo = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                IsDeleted = false,
                UserId = 1 // Different owner
            };

            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId))
                .ReturnsAsync(propositionBo);
            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail))
                .ReturnsAsync(regularUser);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _propositionService.DeletePropositionAsync(propositionId, userEmail);
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

        #region VoteUp Tests

        [TestMethod]
        public async Task VoteUpAsync_NewVote_AddsVoteAndIncrementsCounter()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                VotesUp = 5,
                VotesDown = 2,
                IsDeleted = false,
                UserId = 2
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 6,
                VotesDown = 2
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
            _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
                .ReturnsAsync((PropositionVoteBo?)null);
            _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(6);
            _mockPropositionVoteRepository.Setup(r => r.CountDownVotesAsync(propositionId)).ReturnsAsync(2);
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId)).ReturnsAsync(proposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(proposition)).Returns(propositionDto);

            // Act
            var result = await _propositionService.VoteUpAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.VotesUp);
            Assert.AreEqual(2, result.VotesDown);
            _mockPropositionVoteRepository.Verify(r => r.AddAsync(It.Is<PropositionVoteBo>(v =>
                v.PropositionId == propositionId &&
                v.UserId == user.Id &&
                v.VoteType == Bo.Models.VoteType.Up
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for vote, once for proposition
        }

        [TestMethod]
        public async Task VoteUpAsync_ChangeFromDownToUp_UpdatesVoteAndCounters()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 5,
                VotesDown = 3,
                IsDeleted = false,
                UserId = 2
            };

            var existingVote = new PropositionVoteBo
            {
                Id = 10,
                PropositionId = propositionId,
                UserId = user.Id,
                VoteType = Bo.Models.VoteType.Down,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 6,
                VotesDown = 2
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
            _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
                .ReturnsAsync(existingVote);
            _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(6);
            _mockPropositionVoteRepository.Setup(r => r.CountDownVotesAsync(propositionId)).ReturnsAsync(2);
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId)).ReturnsAsync(proposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(proposition)).Returns(propositionDto);

            // Act
            var result = await _propositionService.VoteUpAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(6, result.VotesUp);
            Assert.AreEqual(2, result.VotesDown);
            Assert.AreEqual(Bo.Models.VoteType.Up, existingVote.VoteType);
            Assert.IsNotNull(existingVote.UpdatedAt);
            _mockPropositionVoteRepository.Verify(r => r.Update(existingVote), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for vote, once for proposition
        }

        [TestMethod]
        public async Task VoteUpAsync_AlreadyVotedUp_RemovesVote()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 5,
                VotesDown = 2,
                IsDeleted = false,
                UserId = 2
            };

            var existingVote = new PropositionVoteBo
            {
                Id = 10,
                PropositionId = propositionId,
                UserId = user.Id,
                VoteType = Bo.Models.VoteType.Up,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 4, // Decremented from 5 to 4
                VotesDown = 2
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
            _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
                .ReturnsAsync(existingVote);
            _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(4); // After deletion
            _mockPropositionVoteRepository.Setup(r => r.CountDownVotesAsync(propositionId)).ReturnsAsync(2);
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId)).ReturnsAsync(proposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(proposition)).Returns(propositionDto);

            // Act
            var result = await _propositionService.VoteUpAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.VotesUp); // Vote removed, counter decremented
            Assert.AreEqual(2, result.VotesDown);
            _mockPropositionVoteRepository.Verify(r => r.Delete(existingVote), Times.Once); // Vote deleted
            _mockPropositionVoteRepository.Verify(r => r.AddAsync(It.IsAny<PropositionVoteBo>()), Times.Never);
            _mockPropositionVoteRepository.Verify(r => r.Update(It.IsAny<PropositionVoteBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for vote, once for proposition
        }

        [TestMethod]
        public async Task VoteUpAsync_UserNotFound_ThrowsUnauthorizedException()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "nonexistent@example.com";

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync((UserBo?)null);

            // Act
            var exceptionThrown = false;
            try
            {
                await _propositionService.VoteUpAsync(propositionId, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        [TestMethod]
        public async Task VoteUpAsync_PropositionNotFound_ReturnsNull()
        {
            // Arrange
            var propositionId = 999;
            var userEmail = "test@example.com";
            var user = new UserBo { Id = 1, Email = userEmail };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync((PropositionBo?)null);

            // Act
            var result = await _propositionService.VoteUpAsync(propositionId, userEmail);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task VoteUpAsync_DeletedProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";
            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Deleted Proposition",
                IsDeleted = true
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);

            // Act
            var result = await _propositionService.VoteUpAsync(propositionId, userEmail);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region VoteDown Tests

        [TestMethod]
        public async Task VoteDownAsync_NewVote_AddsVoteAndIncrementsCounter()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                Description = "Test Description",
                VotesUp = 5,
                VotesDown = 2,
                IsDeleted = false,
                UserId = 2
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 5,
                VotesDown = 3
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
            _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
                .ReturnsAsync((PropositionVoteBo?)null);
            _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(5);
            _mockPropositionVoteRepository.Setup(r => r.CountDownVotesAsync(propositionId)).ReturnsAsync(3);
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId)).ReturnsAsync(proposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(proposition)).Returns(propositionDto);

            // Act
            var result = await _propositionService.VoteDownAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.VotesUp);
            Assert.AreEqual(3, result.VotesDown);
            _mockPropositionVoteRepository.Verify(r => r.AddAsync(It.Is<PropositionVoteBo>(v =>
                v.PropositionId == propositionId &&
                v.UserId == user.Id &&
                v.VoteType == Bo.Models.VoteType.Down
            )), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for vote, once for proposition
        }

        [TestMethod]
        public async Task VoteDownAsync_ChangeFromUpToDown_UpdatesVoteAndCounters()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 6,
                VotesDown = 2,
                IsDeleted = false,
                UserId = 2
            };

            var existingVote = new PropositionVoteBo
            {
                Id = 10,
                PropositionId = propositionId,
                UserId = user.Id,
                VoteType = Bo.Models.VoteType.Up,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 5,
                VotesDown = 3
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
            _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
                .ReturnsAsync(existingVote);
            _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(5);
            _mockPropositionVoteRepository.Setup(r => r.CountDownVotesAsync(propositionId)).ReturnsAsync(3);
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId)).ReturnsAsync(proposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(proposition)).Returns(propositionDto);

            // Act
            var result = await _propositionService.VoteDownAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.VotesUp);
            Assert.AreEqual(3, result.VotesDown);
            Assert.AreEqual(Bo.Models.VoteType.Down, existingVote.VoteType);
            Assert.IsNotNull(existingVote.UpdatedAt);
            _mockPropositionVoteRepository.Verify(r => r.Update(existingVote), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for vote, once for proposition
        }

        [TestMethod]
        public async Task VoteDownAsync_AlreadyVotedDown_RemovesVote()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";

            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 5,
                VotesDown = 2,
                IsDeleted = false,
                UserId = 2
            };

            var existingVote = new PropositionVoteBo
            {
                Id = 10,
                PropositionId = propositionId,
                UserId = user.Id,
                VoteType = Bo.Models.VoteType.Down,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            };

            var propositionDto = new PropositionDto
            {
                Id = propositionId,
                Title = "Test Proposition",
                VotesUp = 5,
                VotesDown = 1 // Decremented from 2 to 1
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);
            _mockPropositionVoteRepository.Setup(r => r.GetByPropositionAndUserAsync(propositionId, user.Id))
                .ReturnsAsync(existingVote);
            _mockPropositionVoteRepository.Setup(r => r.CountUpVotesAsync(propositionId)).ReturnsAsync(5);
            _mockPropositionVoteRepository.Setup(r => r.CountDownVotesAsync(propositionId)).ReturnsAsync(1); // After deletion
            _mockPropositionRepository.Setup(r => r.GetPropositionWithDetailsAsync(propositionId)).ReturnsAsync(proposition);
            _mockMapper.Setup(m => m.Map<PropositionDto>(proposition)).Returns(propositionDto);

            // Act
            var result = await _propositionService.VoteDownAsync(propositionId, userEmail);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(5, result.VotesUp);
            Assert.AreEqual(1, result.VotesDown); // Vote removed, counter decremented
            _mockPropositionVoteRepository.Verify(r => r.Delete(existingVote), Times.Once); // Vote deleted
            _mockPropositionVoteRepository.Verify(r => r.AddAsync(It.IsAny<PropositionVoteBo>()), Times.Never);
            _mockPropositionVoteRepository.Verify(r => r.Update(It.IsAny<PropositionVoteBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Exactly(2)); // Once for vote, once for proposition
        }

        [TestMethod]
        public async Task VoteDownAsync_UserNotFound_ThrowsUnauthorizedException()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "nonexistent@example.com";

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync((UserBo?)null);

            // Act
            var exceptionThrown = false;
            try
            {
                await _propositionService.VoteDownAsync(propositionId, userEmail);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }

            // Assert
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        [TestMethod]
        public async Task VoteDownAsync_PropositionNotFound_ReturnsNull()
        {
            // Arrange
            var propositionId = 999;
            var userEmail = "test@example.com";
            var user = new UserBo { Id = 1, Email = userEmail };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync((PropositionBo?)null);

            // Act
            var result = await _propositionService.VoteDownAsync(propositionId, userEmail);

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task VoteDownAsync_DeletedProposition_ReturnsNull()
        {
            // Arrange
            var propositionId = 1;
            var userEmail = "test@example.com";
            var user = new UserBo { Id = 1, Email = userEmail };
            var proposition = new PropositionBo
            {
                Id = propositionId,
                Title = "Deleted Proposition",
                IsDeleted = true
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(userEmail)).ReturnsAsync(user);
            _mockPropositionRepository.Setup(r => r.GetByIdAsync(propositionId)).ReturnsAsync(proposition);

            // Act
            var result = await _propositionService.VoteDownAsync(propositionId, userEmail);

            // Assert
            Assert.IsNull(result);
        }

        #endregion
    }
}
