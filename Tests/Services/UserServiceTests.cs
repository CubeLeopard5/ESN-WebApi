using AutoMapper;
using Bo.Constants;
using Bo.Enums;
using Bo.Models;
using Business.User;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services
{
    [TestClass]
    public class UserServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IMapper> _mockMapper = null!;
        private Mock<ILogger<UserService>> _mockLogger = null!;
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private UserService _userService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<UserService>>();
            _mockConfiguration = new Mock<IConfiguration>();
            _mockUserRepository = new Mock<IUserRepository>();

            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);

            // Setup JWT configuration
            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Key"]).Returns("ThisIsAVerySecureKeyForJWTTokenGenerationWithAtLeast32Characters");
            jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
            jwtSection.Setup(x => x["ExpiresInMinutes"]).Returns("60");
            _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);

            _userService = new UserService(
                _mockUnitOfWork.Object,
                _mockMapper.Object,
                _mockLogger.Object,
                _mockConfiguration.Object
            );
        }

        #region Login Tests

        [TestMethod]
        public async Task LoginAsync_ValidCredentials_ReturnsUserDto()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "test@example.com",
                Password = "Password123"
            };

            var userBo = new UserBo
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = new PasswordHasher<UserBo>().HashPassword(null!, "Password123"),
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International,
                Status = UserStatus.Approved // L'utilisateur doit être approuvé pour se connecter
            };

            var userDto = new UserDto
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockUserRepository.Setup(r => r.GetByEmailWithRoleAsync(loginDto.Email))
                .ReturnsAsync(userBo);
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<UserBo>()))
                .Returns(userDto);

            // Act
            var result = await _userService.LoginAsync(loginDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.User);
            Assert.AreEqual(userDto.Email, result.User.Email);
            Assert.IsNotNull(result.Token);
        }

        [TestMethod]
        public async Task LoginAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "notfound@example.com",
                Password = "Password123"
            };

            _mockUserRepository.Setup(r => r.GetByEmailWithRoleAsync(loginDto.Email))
                .ReturnsAsync((UserBo?)null);

            // Act & Assert - Après correction timing attack, on lance UnauthorizedAccessException
            var exceptionThrown = false;
            try
            {
                await _userService.LoginAsync(loginDto);
            }
            catch (UnauthorizedAccessException ex)
            {
                Assert.AreEqual("Invalid credentials", ex.Message);
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        [TestMethod]
        public async Task LoginAsync_InvalidPassword_ThrowsUnauthorizedAccessException()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "test@example.com",
                Password = "WrongPassword"
            };

            var userBo = new UserBo
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = new PasswordHasher<UserBo>().HashPassword(null!, "CorrectPassword"),
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            _mockUserRepository.Setup(r => r.GetByEmailWithRoleAsync(loginDto.Email))
                .ReturnsAsync(userBo);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _userService.LoginAsync(loginDto);
            }
            catch (UnauthorizedAccessException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected UnauthorizedAccessException was not thrown");
        }

        #endregion

        #region CreateUser Tests

        [TestMethod]
        public async Task CreateUserAsync_ValidUser_ReturnsUserDto()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Email = "newuser@example.com",
                Password = "Password123",
                FirstName = "New",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-20),
                StudentType = Bo.Constants.StudentType.International
            };

            var userBo = new UserBo
            {
                Id = 1,
                Email = createDto.Email,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                BirthDate = createDto.BirthDate,
                StudentType = createDto.StudentType
            };

            var userDto = new UserDto
            {
                Id = 1,
                Email = createDto.Email,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(createDto.Email))
                .ReturnsAsync((UserBo?)null);
            _mockMapper.Setup(m => m.Map<UserBo>(createDto))
                .Returns(userBo);
            _mockUserRepository.Setup(r => r.AddAsync(It.IsAny<UserBo>()))
                .ReturnsAsync(userBo);
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<UserBo>()))
                .Returns(userDto);

            // Act
            var result = await _userService.CreateUserAsync(createDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(createDto.Email, result.Email);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CreateUserAsync_DuplicateEmail_ThrowsInvalidOperationException()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Email = "existing@example.com",
                Password = "Password123",
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-20),
                StudentType = Bo.Constants.StudentType.International
            };

            var existingUser = new UserBo
            {
                Id = 1,
                Email = createDto.Email,
                FirstName = "Existing",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            _mockUserRepository.Setup(r => r.GetByEmailAsync(createDto.Email))
                .ReturnsAsync(existingUser);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _userService.CreateUserAsync(createDto);
            }
            catch (InvalidOperationException)
            {
                exceptionThrown = true;
            }
            Assert.IsTrue(exceptionThrown, "Expected InvalidOperationException was not thrown");
        }

        #endregion

        #region GetUserById Tests

        [TestMethod]
        public async Task GetUserByIdAsync_ExistingUser_ReturnsUserDto()
        {
            // Arrange
            var userId = 1;
            var userBo = new UserBo
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            var userDto = new UserDto
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(userBo);
            _mockMapper.Setup(m => m.Map<UserDto>(userBo))
                .Returns(userDto);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.Id);
            Assert.AreEqual(userDto.Email, result.Email);
        }

        [TestMethod]
        public async Task GetUserByIdAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var userId = 999;
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((UserBo?)null);

            // Act
            var result = await _userService.GetUserByIdAsync(userId);

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region GetAllUsers Tests

        [TestMethod]
        public async Task GetAllUsersAsync_ReturnsListOfUsers()
        {
            // Arrange
            var usersBo = new List<UserBo>
            {
                new UserBo
                {
                    Id = 1,
                    Email = "user1@example.com",
                    FirstName = "User",
                    LastName = "One",
                    BirthDate = DateTime.Now.AddYears(-25),
                    StudentType = Bo.Constants.StudentType.International
                },
                new UserBo
                {
                    Id = 2,
                    Email = "user2@example.com",
                    FirstName = "User",
                    LastName = "Two",
                    BirthDate = DateTime.Now.AddYears(-22),
                    StudentType = Bo.Constants.StudentType.Local
                }
            };

            var userDtos = new List<UserDto>
            {
                new UserDto { Id = 1, Email = "user1@example.com", FirstName = "User", LastName = "One" },
                new UserDto { Id = 2, Email = "user2@example.com", FirstName = "User", LastName = "Two" }
            };

            _mockUserRepository.Setup(r => r.GetAllAsync())
                .ReturnsAsync(usersBo);
            _mockMapper.Setup(m => m.Map<IEnumerable<UserDto>>(usersBo))
                .Returns(userDtos);

            // Act
            var result = await _userService.GetAllUsersAsync();

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region UpdateUser Tests

        [TestMethod]
        public async Task UpdateUserAsync_ValidUpdate_ReturnsUpdatedUserDto()
        {
            // Arrange
            var userId = 1;
            var updateDto = new UserUpdateDto
            {
                FirstName = "Updated",
                LastName = "User",
                PhoneNumber = "1234567890"
            };

            var existingUser = new UserBo
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Old",
                LastName = "Name",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            var updatedUserDto = new UserDto
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Updated",
                LastName = "User"
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(existingUser);
            _mockMapper.Setup(m => m.Map(updateDto, existingUser))
                .Returns(existingUser);
            _mockMapper.Setup(m => m.Map<UserDto>(existingUser))
                .Returns(updatedUserDto);

            // Act
            var result = await _userService.UpdateUserAsync(userId, updateDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("Updated", result.FirstName);
            _mockUserRepository.Verify(r => r.Update(existingUser), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdateUserAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var userId = 999;
            var updateDto = new UserUpdateDto
            {
                FirstName = "Updated",
                LastName = "User"
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((UserBo?)null);

            // Act
            var result = await _userService.UpdateUserAsync(userId, updateDto);

            // Assert
            Assert.IsNull(result);
            _mockUserRepository.Verify(r => r.Update(It.IsAny<UserBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region DeleteUser Tests

        [TestMethod]
        public async Task DeleteUserAsync_ExistingUser_ReturnsDeletedUser()
        {
            // Arrange
            var userId = 1;
            var userBo = new UserBo
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International
            };

            var userDto = new UserDto
            {
                Id = userId,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync(userBo);
            _mockMapper.Setup(m => m.Map<UserDto>(userBo))
                .Returns(userDto);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(userId, result.Id);
            _mockUserRepository.Verify(r => r.Delete(userBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeleteUserAsync_NonExistingUser_ReturnsNull()
        {
            // Arrange
            var userId = 999;
            _mockUserRepository.Setup(r => r.GetByIdAsync(userId))
                .ReturnsAsync((UserBo?)null);

            // Act
            var result = await _userService.DeleteUserAsync(userId);

            // Assert
            Assert.IsNull(result);
            _mockUserRepository.Verify(r => r.Delete(It.IsAny<UserBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion

        #region User Status Tests

        [TestMethod]
        public async Task LoginAsync_WhenUserStatusPending_ThrowsForbiddenAccessException()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "pending@test.com",
                Password = "Password123"
            };

            var userBo = new UserBo
            {
                Id = 1,
                Email = "pending@test.com",
                PasswordHash = new PasswordHasher<UserBo>().HashPassword(null!, "Password123"),
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International,
                Status = UserStatus.Pending
            };

            _mockUserRepository.Setup(r => r.GetByEmailWithRoleAsync(loginDto.Email))
                .ReturnsAsync(userBo);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _userService.LoginAsync(loginDto);
            }
            catch (Business.Exceptions.ForbiddenAccessException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.Contains("attente"));
            }

            Assert.IsTrue(exceptionThrown, "Expected ForbiddenAccessException was not thrown");
        }

        [TestMethod]
        public async Task LoginAsync_WhenUserStatusRejected_ThrowsForbiddenAccessException()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "rejected@test.com",
                Password = "Password123"
            };

            var userBo = new UserBo
            {
                Id = 1,
                Email = "rejected@test.com",
                PasswordHash = new PasswordHasher<UserBo>().HashPassword(null!, "Password123"),
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International,
                Status = UserStatus.Rejected
            };

            _mockUserRepository.Setup(r => r.GetByEmailWithRoleAsync(loginDto.Email))
                .ReturnsAsync(userBo);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _userService.LoginAsync(loginDto);
            }
            catch (Business.Exceptions.ForbiddenAccessException ex)
            {
                exceptionThrown = true;
                Assert.IsTrue(ex.Message.Contains("refusé"));
            }

            Assert.IsTrue(exceptionThrown, "Expected ForbiddenAccessException was not thrown");
        }

        [TestMethod]
        public async Task LoginAsync_WhenUserStatusApproved_ReturnsToken()
        {
            // Arrange
            var loginDto = new UserLoginDto
            {
                Email = "approved@test.com",
                Password = "Password123"
            };

            var userBo = new UserBo
            {
                Id = 1,
                Email = "approved@test.com",
                PasswordHash = new PasswordHasher<UserBo>().HashPassword(null!, "Password123"),
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.Now.AddYears(-25),
                StudentType = Bo.Constants.StudentType.International,
                Status = UserStatus.Approved
            };

            var userDto = new UserDto
            {
                Id = 1,
                Email = "approved@test.com",
                FirstName = "Test",
                LastName = "User"
            };

            _mockUserRepository.Setup(r => r.GetByEmailWithRoleAsync(loginDto.Email))
                .ReturnsAsync(userBo);
            _mockMapper.Setup(m => m.Map<UserDto>(It.IsAny<UserBo>()))
                .Returns(userDto);

            // Act
            var result = await _userService.LoginAsync(loginDto);

            // Assert
            Assert.IsNotNull(result);
            Assert.IsNotNull(result.Token);
        }

        [TestMethod]
        public async Task GetUsersByStatusAsync_WhenCalled_ReturnsFilteredUsers()
        {
            // Arrange
            var users = new List<UserBo>
            {
                new() { Id = 1, Email = "user1@test.com", Status = UserStatus.Pending },
                new() { Id = 2, Email = "user2@test.com", Status = UserStatus.Pending },
                new() { Id = 3, Email = "user3@test.com", Status = UserStatus.Approved }
            };

            var pendingUsersDto = new List<UserDto>
            {
                new() { Id = 1, Email = "user1@test.com" },
                new() { Id = 2, Email = "user2@test.com" }
            };

            _mockUserRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(users);
            _mockMapper.Setup(m => m.Map<IEnumerable<UserDto>>(It.IsAny<IEnumerable<UserBo>>()))
                .Returns(pendingUsersDto);

            // Act
            var result = await _userService.GetUsersByStatusAsync(UserStatus.Pending);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task ApproveUserAsync_WhenUserExists_UpdatesStatusToApproved()
        {
            // Arrange
            var userBo = new UserBo
            {
                Id = 1,
                Email = "test@test.com",
                Status = UserStatus.Pending
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(userBo);

            // Act
            await _userService.ApproveUserAsync(1);

            // Assert
            Assert.AreEqual(UserStatus.Approved, userBo.Status);
            _mockUserRepository.Verify(r => r.Update(userBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task ApproveUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((UserBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _userService.ApproveUserAsync(999);
            }
            catch (KeyNotFoundException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "Expected KeyNotFoundException was not thrown");
            _mockUserRepository.Verify(r => r.Update(It.IsAny<UserBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task RejectUserAsync_WhenUserExists_UpdatesStatusToRejected()
        {
            // Arrange
            var userBo = new UserBo
            {
                Id = 1,
                Email = "test@test.com",
                Status = UserStatus.Pending
            };

            _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(userBo);

            // Act
            await _userService.RejectUserAsync(1, "Test reason");

            // Assert
            Assert.AreEqual(UserStatus.Rejected, userBo.Status);
            _mockUserRepository.Verify(r => r.Update(userBo), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task RejectUserAsync_WhenUserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((UserBo?)null);

            // Act & Assert
            var exceptionThrown = false;
            try
            {
                await _userService.RejectUserAsync(999, "Test reason");
            }
            catch (KeyNotFoundException)
            {
                exceptionThrown = true;
            }

            Assert.IsTrue(exceptionThrown, "Expected KeyNotFoundException was not thrown");
            _mockUserRepository.Verify(r => r.Update(It.IsAny<UserBo>()), Times.Never);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        #endregion
    }
}
