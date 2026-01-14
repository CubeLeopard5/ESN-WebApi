using Bo.Constants;
using Bo.Enums;
using Business.Exceptions;
using Business.Interfaces;
using Dto.Common;
using Dto.User;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class UsersControllerTests
    {
        private Mock<IUserService> _mockUserService = null!;
        private Mock<ILogger<UsersController>> _mockLogger = null!;
        private UsersController _controller = null!;
        private const string TestUserEmail = "test@example.com";

        [TestInitialize]
        public void Setup()
        {
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<UsersController>>();
            _controller = new UsersController(_mockUserService.Object, _mockLogger.Object);

            SetupAuthenticatedUser(TestUserEmail);
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

        #region Login Tests

        [TestMethod]
        public async Task Login_ReturnsOk_WithValidCredentials()
        {
            // Arrange
            var loginDto = new UserLoginDto { Email = TestUserEmail, Password = "password" };
            var response = new UserLoginResponseDto
            {
                Token = "jwt-token",
                User = new UserDto { Id = 1, Email = TestUserEmail, StudentType = Bo.Constants.StudentType.EsnMember }
            };
            _mockUserService.Setup(s => s.LoginAsync(loginDto)).ReturnsAsync(response);

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var loginResponse = okResult.Value as UserLoginResponseDto;
            Assert.IsNotNull(loginResponse);
            Assert.AreEqual("jwt-token", loginResponse.Token);
        }

        [TestMethod]
        public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
        {
            // Arrange - Après correction timing attack, on retourne toujours Unauthorized au lieu de NotFound
            var loginDto = new UserLoginDto { Email = "nonexistent@test.com", Password = "password" };
            _mockUserService.Setup(s => s.LoginAsync(loginDto))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid credentials"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task Login_ReturnsUnauthorized_WithInvalidPassword()
        {
            // Arrange
            var loginDto = new UserLoginDto { Email = TestUserEmail, Password = "wrong" };
            _mockUserService.Setup(s => s.LoginAsync(loginDto))
                .ThrowsAsync(new UnauthorizedAccessException("Invalid email or password"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.IsInstanceOfType<UnauthorizedObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task Login_ReturnsForbidden_WhenUserStatusIsPending()
        {
            // Arrange
            var loginDto = new UserLoginDto { Email = TestUserEmail, Password = "password" };
            _mockUserService.Setup(s => s.LoginAsync(loginDto))
                .ThrowsAsync(new ForbiddenAccessException("Votre compte est en attente de validation par un administrateur"));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        [TestMethod]
        public async Task Login_ReturnsForbidden_WhenUserStatusIsRejected()
        {
            // Arrange
            var loginDto = new UserLoginDto { Email = TestUserEmail, Password = "password" };
            _mockUserService.Setup(s => s.LoginAsync(loginDto))
                .ThrowsAsync(new ForbiddenAccessException("Votre compte a été refusé. Contactez l'administrateur."));

            // Act
            var result = await _controller.Login(loginDto);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        #endregion

        #region GetCurrentUser Tests

        [TestMethod]
        public async Task GetCurrentUser_ReturnsOk_WithAuthenticatedUser()
        {
            // Arrange
            var userDto = new UserDto { Id = 1, Email = TestUserEmail, StudentType = Bo.Constants.StudentType.EsnMember };
            _mockUserService.Setup(s => s.GetCurrentUserAsync(TestUserEmail)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetCurrentUser();

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as UserDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(TestUserEmail, dto.Email);
        }

        #endregion

        #region GetUsers Tests

        [TestMethod]
        public async Task GetUsers_ReturnsOk_WithListOfUsers()
        {
            // Arrange
            var users = new List<UserDto>
            {
                new() { Id = 1, Email = "user1@test.com", StudentType = Bo.Constants.StudentType.EsnMember },
                new() { Id = 2, Email = "user2@test.com", StudentType = "exchange_student" }
            };
            var pagedResult = new PagedResult<UserDto>(users, 2, 1, 20);
            _mockUserService.Setup(s => s.GetAllUsersAsync(It.IsAny<PaginationParams>())).ReturnsAsync(pagedResult);

            // Act
            var pagination = new PaginationParams();
            var result = await _controller.GetUsers(pagination);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as PagedResult<UserDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(2, returned.Items.Count);
            Assert.AreEqual(2, returned.TotalCount);
        }

        #endregion

        #region GetUser Tests

        [TestMethod]
        public async Task GetUser_ReturnsOk_WithValidId()
        {
            // Arrange
            var userDto = new UserDto { Id = 1, Email = TestUserEmail, StudentType = Bo.Constants.StudentType.EsnMember };
            _mockUserService.Setup(s => s.GetUserByIdAsync(1)).ReturnsAsync(userDto);

            // Act
            var result = await _controller.GetUser(1);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as UserDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual(TestUserEmail, dto.Email);
        }

        [TestMethod]
        public async Task GetUser_ReturnsNotFound_WhenInvalidId()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUserByIdAsync(999)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.GetUser(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region PostUser Tests

        [TestMethod]
        public async Task PostUser_ReturnsCreatedAtAction_WithValidDto()
        {
            // Arrange
            var createDto = new UserCreateDto
            {
                Email = "newuser@test.com",
                FirstName = "New",
                LastName = "User",
                Password = "password",
                StudentType = "exchange_student"
            };
            var createdUser = new UserDto
            {
                Id = 1,
                Email = createDto.Email,
                FirstName = createDto.FirstName,
                LastName = createDto.LastName,
                StudentType = createDto.StudentType
            };
            _mockUserService.Setup(s => s.CreateUserAsync(createDto)).ReturnsAsync(createdUser);

            // Act
            var result = await _controller.PostUser(createDto);

            // Assert
            Assert.IsInstanceOfType<CreatedAtActionResult>(result.Result);
            var createdResult = (CreatedAtActionResult)result.Result;
            var dto = createdResult.Value as UserDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("newuser@test.com", dto.Email);
        }

        [TestMethod]
        public async Task PostUser_ReturnsConflict_WhenEmailAlreadyExists()
        {
            // Arrange
            var createDto = new UserCreateDto { Email = "existing@test.com", Password = "password", StudentType = "exchange_student" };
            _mockUserService.Setup(s => s.CreateUserAsync(createDto))
                .ThrowsAsync(new InvalidOperationException("A user with email existing@test.com already exists."));

            // Act
            var result = await _controller.PostUser(createDto);

            // Assert
            Assert.IsInstanceOfType<ConflictObjectResult>(result.Result);
        }

        #endregion

        #region PutUser Tests

        [TestMethod]
        public async Task PutUser_ReturnsOk_WithValidDto()
        {
            // Arrange
            var updateDto = new UserUpdateDto
            {
                Email = "updated@test.com",
                FirstName = "Updated",
                LastName = "User",
                BirthDate = DateTime.UtcNow.AddYears(-25)
            };
            var updatedUser = new UserDto
            {
                Id = 1,
                Email = updateDto.Email,
                FirstName = updateDto.FirstName,
                LastName = updateDto.LastName,
                StudentType = Bo.Constants.StudentType.EsnMember
            };
            _mockUserService.Setup(s => s.UpdateUserAsync(1, updateDto)).ReturnsAsync(updatedUser);

            // Act
            var result = await _controller.PutUser(1, updateDto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var dto = okResult.Value as UserDto;
            Assert.IsNotNull(dto);
            Assert.AreEqual("updated@test.com", dto.Email);
        }

        [TestMethod]
        public async Task PutUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var updateDto = new UserUpdateDto { Email = "test@test.com", FirstName = "Test", LastName = "User", BirthDate = DateTime.UtcNow };
            _mockUserService.Setup(s => s.UpdateUserAsync(999, updateDto))
                .ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.PutUser(999, updateDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region DeleteUser Tests

        [TestMethod]
        public async Task DeleteUser_ReturnsOk_WhenSuccessful()
        {
            // Arrange
            var deletedUser = new UserDto { Id = 1, Email = TestUserEmail, StudentType = Bo.Constants.StudentType.EsnMember };
            _mockUserService.Setup(s => s.DeleteUserAsync(1)).ReturnsAsync(deletedUser);

            // Act
            var result = await _controller.DeleteUser(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task DeleteUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _mockUserService.Setup(s => s.DeleteUserAsync(999)).ReturnsAsync((UserDto?)null);

            // Act
            var result = await _controller.DeleteUser(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        #endregion

        #region GetPendingUsers Tests

        [TestMethod]
        public async Task GetPendingUsers_ReturnsOk_WithListOfPendingUsers()
        {
            // Arrange
            var pendingUsers = new List<UserDto>
            {
                new() { Id = 1, Email = "pending1@test.com", Status = UserStatus.Pending, StudentType = Bo.Constants.StudentType.EsnMember },
                new() { Id = 2, Email = "pending2@test.com", Status = UserStatus.Pending, StudentType = "exchange_student" }
            };
            _mockUserService.Setup(s => s.GetUsersByStatusAsync(UserStatus.Pending))
                .ReturnsAsync(pendingUsers);

            // Act
            var result = await _controller.GetPendingUsers();

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returnedUsers = okResult.Value as IEnumerable<UserDto>;
            Assert.IsNotNull(returnedUsers);
            Assert.AreEqual(2, returnedUsers.Count());
            Assert.IsTrue(returnedUsers.All(u => u.Status == UserStatus.Pending));
        }

        [TestMethod]
        public async Task GetPendingUsers_ReturnsEmptyList_WhenNoPendingUsers()
        {
            // Arrange
            _mockUserService.Setup(s => s.GetUsersByStatusAsync(UserStatus.Pending))
                .ReturnsAsync(new List<UserDto>());

            // Act
            var result = await _controller.GetPendingUsers();

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returnedUsers = okResult.Value as IEnumerable<UserDto>;
            Assert.IsNotNull(returnedUsers);
            Assert.AreEqual(0, returnedUsers.Count());
        }

        #endregion

        #region ApproveUser Tests

        [TestMethod]
        public async Task ApproveUser_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            _mockUserService.Setup(s => s.ApproveUserAsync(1)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.ApproveUser(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
            _mockUserService.Verify(s => s.ApproveUserAsync(1), Times.Once);
        }

        [TestMethod]
        public async Task ApproveUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            _mockUserService.Setup(s => s.ApproveUserAsync(999))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            // Act
            var result = await _controller.ApproveUser(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        #endregion

        #region RejectUser Tests

        [TestMethod]
        public async Task RejectUser_ReturnsNoContent_WhenSuccessful()
        {
            // Arrange
            var rejectDto = new RejectUserDto { Reason = "Test reason" };
            _mockUserService.Setup(s => s.RejectUserAsync(1, rejectDto.Reason)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RejectUser(1, rejectDto);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
            _mockUserService.Verify(s => s.RejectUserAsync(1, rejectDto.Reason), Times.Once);
        }

        [TestMethod]
        public async Task RejectUser_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var rejectDto = new RejectUserDto { Reason = "Test reason" };
            _mockUserService.Setup(s => s.RejectUserAsync(999, rejectDto.Reason))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            // Act
            var result = await _controller.RejectUser(999, rejectDto);

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        [TestMethod]
        public async Task RejectUser_ReturnsNoContent_WhenReasonIsNull()
        {
            // Arrange
            var rejectDto = new RejectUserDto { Reason = null };
            _mockUserService.Setup(s => s.RejectUserAsync(1, null)).Returns(Task.CompletedTask);

            // Act
            var result = await _controller.RejectUser(1, rejectDto);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
            _mockUserService.Verify(s => s.RejectUserAsync(1, null), Times.Once);
        }

        #endregion
    }
}
