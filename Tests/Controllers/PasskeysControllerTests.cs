using Business.Exceptions;
using Business.Interfaces;
using Dto.Passkey;
using Dto.User;
using Fido2NetLib;
using Microsoft.AspNetCore.Http;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class PasskeysControllerTests
    {
        private Mock<IPasskeyService> _mockPasskeyService = null!;
        private Mock<IUserService> _mockUserService = null!;
        private Mock<ILogger<PasskeysController>> _mockLogger = null!;
        private PasskeysController _controller = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockPasskeyService = new Mock<IPasskeyService>();
            _mockUserService = new Mock<IUserService>();
            _mockLogger = new Mock<ILogger<PasskeysController>>();
            _controller = new PasskeysController(
                _mockPasskeyService.Object,
                _mockUserService.Object,
                _mockLogger.Object);

            SetupAuthenticatedUser(1);
        }

        private void SetupAuthenticatedUser(int userId)
        {
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test@example.com"),
                new Claim("userId", userId.ToString()),
                new Claim(ClaimTypes.Role, "User")
            }, "mock"));

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = user }
            };
        }

        #region BeginRegistration Tests

        [TestMethod]
        public async Task BeginRegistration_ReturnsOk_WithChallengeAndOptions()
        {
            // Arrange
            var options = (CredentialCreateOptions)RuntimeHelpers.GetUninitializedObject(typeof(CredentialCreateOptions));
            _mockPasskeyService.Setup(s => s.BeginRegistrationAsync(1))
                .ReturnsAsync(("challenge-id", options));

            // Act
            var result = await _controller.BeginRegistration();

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
        }

        [TestMethod]
        public async Task BeginRegistration_UserNotFound_ReturnsNotFound()
        {
            // Arrange
            _mockPasskeyService.Setup(s => s.BeginRegistrationAsync(1))
                .ThrowsAsync(new KeyNotFoundException("User not found"));

            // Act
            var result = await _controller.BeginRegistration();

            // Assert
            Assert.IsInstanceOfType<NotFoundObjectResult>(result);
        }

        #endregion

        #region CompleteRegistration Tests

        [TestMethod]
        public async Task CompleteRegistration_ValidAttestation_ReturnsOk()
        {
            // Arrange
            var dto = new PasskeyRegistrationCompleteDto
            {
                ChallengeId = "challenge-id",
                AttestationResponse = "{}",
                DisplayName = "My Key"
            };
            var passkeyDto = new PasskeyDto { Id = 1, DisplayName = "My Key" };
            _mockPasskeyService.Setup(s => s.CompleteRegistrationAsync(1, dto))
                .ReturnsAsync(passkeyDto);

            // Act
            var result = await _controller.CompleteRegistration(dto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task CompleteRegistration_ExpiredChallenge_ReturnsBadRequest()
        {
            // Arrange
            var dto = new PasskeyRegistrationCompleteDto
            {
                ChallengeId = "expired",
                AttestationResponse = "{}",
                DisplayName = "Test"
            };
            _mockPasskeyService.Setup(s => s.CompleteRegistrationAsync(1, dto))
                .ThrowsAsync(new KeyNotFoundException("Challenge not found"));

            // Act
            var result = await _controller.CompleteRegistration(dto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region BeginLogin Tests

        [TestMethod]
        public async Task BeginLogin_ReturnsOk()
        {
            // Arrange
            var dto = new PasskeyLoginBeginDto { Email = "test@example.com" };
            var options = (AssertionOptions)RuntimeHelpers.GetUninitializedObject(typeof(AssertionOptions));
            _mockPasskeyService.Setup(s => s.BeginLoginAsync(dto))
                .ReturnsAsync(("challenge-id", options));

            // Act
            var result = await _controller.BeginLogin(dto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result);
        }

        #endregion

        #region CompleteLogin Tests

        [TestMethod]
        public async Task CompleteLogin_ValidAssertion_ReturnsOk()
        {
            // Arrange
            var dto = new PasskeyLoginCompleteDto
            {
                ChallengeId = "challenge-id",
                AssertionResponse = "{}"
            };
            var response = new UserLoginResponseDto
            {
                Token = "jwt-token",
                User = new UserDto { Id = 1, Email = "test@example.com" }
            };
            _mockPasskeyService.Setup(s => s.CompleteLoginAsync(dto))
                .ReturnsAsync(response);

            // Act
            var result = await _controller.CompleteLogin(dto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var loginResponse = okResult.Value as UserLoginResponseDto;
            Assert.IsNotNull(loginResponse);
            Assert.AreEqual("jwt-token", loginResponse.Token);
        }

        [TestMethod]
        public async Task CompleteLogin_UserPending_ReturnsForbidden()
        {
            // Arrange
            var dto = new PasskeyLoginCompleteDto
            {
                ChallengeId = "challenge-id",
                AssertionResponse = "{}"
            };
            _mockPasskeyService.Setup(s => s.CompleteLoginAsync(dto))
                .ThrowsAsync(new ForbiddenAccessException("Your account is pending approval by an administrator."));

            // Act
            var result = await _controller.CompleteLogin(dto);

            // Assert
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = result.Result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(StatusCodes.Status403Forbidden, objectResult.StatusCode);
        }

        [TestMethod]
        public async Task CompleteLogin_InvalidAssertion_ReturnsBadRequest()
        {
            // Arrange
            var dto = new PasskeyLoginCompleteDto
            {
                ChallengeId = "challenge-id",
                AssertionResponse = "{}"
            };
            _mockPasskeyService.Setup(s => s.CompleteLoginAsync(dto))
                .ThrowsAsync(new Fido2VerificationException("Invalid assertion"));

            // Act
            var result = await _controller.CompleteLogin(dto);

            // Assert
            Assert.IsInstanceOfType<BadRequestObjectResult>(result.Result);
        }

        #endregion

        #region GetPasskeys Tests

        [TestMethod]
        public async Task GetPasskeys_ReturnsOk_WithPasskeyList()
        {
            // Arrange
            var passkeys = new List<PasskeyDto>
            {
                new() { Id = 1, DisplayName = "Key 1" },
                new() { Id = 2, DisplayName = "Key 2" }
            };
            _mockPasskeyService.Setup(s => s.GetUserPasskeysAsync(1)).ReturnsAsync(passkeys);

            // Act
            var result = await _controller.GetPasskeys();

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
            var okResult = (OkObjectResult)result.Result;
            var returned = okResult.Value as IEnumerable<PasskeyDto>;
            Assert.IsNotNull(returned);
            Assert.AreEqual(2, returned.Count());
        }

        #endregion

        #region UpdatePasskey Tests

        [TestMethod]
        public async Task UpdatePasskey_ValidOwner_ReturnsOk()
        {
            // Arrange
            var dto = new UpdatePasskeyDto { DisplayName = "New Name" };
            var passkeyDto = new PasskeyDto { Id = 1, DisplayName = "New Name" };
            _mockPasskeyService.Setup(s => s.UpdatePasskeyAsync(1, 1, dto)).ReturnsAsync(passkeyDto);

            // Act
            var result = await _controller.UpdatePasskey(1, dto);

            // Assert
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task UpdatePasskey_NotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new UpdatePasskeyDto { DisplayName = "New Name" };
            _mockPasskeyService.Setup(s => s.UpdatePasskeyAsync(999, 1, dto)).ReturnsAsync((PasskeyDto?)null);

            // Act
            var result = await _controller.UpdatePasskey(999, dto);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result.Result);
        }

        #endregion

        #region DeletePasskey Tests

        [TestMethod]
        public async Task DeletePasskey_ValidOwner_ReturnsNoContent()
        {
            // Arrange
            _mockPasskeyService.Setup(s => s.DeletePasskeyAsync(1, 1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeletePasskey(1);

            // Assert
            Assert.IsInstanceOfType<NoContentResult>(result);
        }

        [TestMethod]
        public async Task DeletePasskey_NotFound_ReturnsNotFound()
        {
            // Arrange
            _mockPasskeyService.Setup(s => s.DeletePasskeyAsync(999, 1)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeletePasskey(999);

            // Assert
            Assert.IsInstanceOfType<NotFoundResult>(result);
        }

        #endregion
    }
}
