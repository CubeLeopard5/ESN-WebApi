using AutoMapper;
using Bo.Enums;
using Bo.Models;
using Business.Exceptions;
using Business.Interfaces;
using Business.Passkey;
using Dal.Repositories.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Passkey;
using Dto.User;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using System.Runtime.CompilerServices;

namespace Tests.Services
{
    [TestClass]
    public class PasskeyServiceTests
    {
        private Mock<IUnitOfWork> _mockUnitOfWork = null!;
        private Mock<IFido2> _mockFido2 = null!;
        private Mock<IJwtTokenService> _mockJwtTokenService = null!;
        private IMemoryCache _memoryCache = null!;
        private Mock<IMapper> _mockMapper = null!;
        private Mock<ILogger<PasskeyService>> _mockLogger = null!;
        private Mock<IUserRepository> _mockUserRepository = null!;
        private Mock<IPasskeyRepository> _mockPasskeyRepository = null!;
        private PasskeyService _service = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockUnitOfWork = new Mock<IUnitOfWork>();
            _mockFido2 = new Mock<IFido2>();
            _mockJwtTokenService = new Mock<IJwtTokenService>();
            _memoryCache = new MemoryCache(new MemoryCacheOptions());
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<PasskeyService>>();
            _mockUserRepository = new Mock<IUserRepository>();
            _mockPasskeyRepository = new Mock<IPasskeyRepository>();

            _mockUnitOfWork.Setup(u => u.Users).Returns(_mockUserRepository.Object);
            _mockUnitOfWork.Setup(u => u.Passkeys).Returns(_mockPasskeyRepository.Object);

            _service = new PasskeyService(
                _mockUnitOfWork.Object,
                _mockFido2.Object,
                _mockJwtTokenService.Object,
                _memoryCache,
                _mockMapper.Object,
                _mockLogger.Object
            );
        }

        [TestCleanup]
        public void Cleanup()
        {
            _memoryCache.Dispose();
        }

        /// <summary>
        /// Creates a CredentialCreateOptions bypassing required member checks
        /// </summary>
        private static CredentialCreateOptions CreateCredentialCreateOptions()
        {
            return (CredentialCreateOptions)RuntimeHelpers.GetUninitializedObject(typeof(CredentialCreateOptions));
        }

        /// <summary>
        /// Creates an AssertionOptions bypassing required member checks
        /// </summary>
        private static AssertionOptions CreateAssertionOptions()
        {
            return (AssertionOptions)RuntimeHelpers.GetUninitializedObject(typeof(AssertionOptions));
        }

        #region BeginRegistration Tests

        [TestMethod]
        public async Task BeginRegistrationAsync_ValidUser_ReturnsOptions()
        {
            // Arrange
            var user = new UserBo
            {
                Id = 1, Email = "test@example.com", FirstName = "Test", LastName = "User",
                StudentType = "exchange"
            };
            _mockUserRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(user);
            _mockPasskeyRepository.Setup(r => r.GetByUserIdAsync(1))
                .ReturnsAsync(new List<UserPasskeyBo>());

            var expectedOptions = CreateCredentialCreateOptions();
            _mockFido2.Setup(f => f.RequestNewCredential(It.IsAny<RequestNewCredentialParams>()))
                .Returns(expectedOptions);

            // Act
            var (challengeId, options) = await _service.BeginRegistrationAsync(1);

            // Assert
            Assert.IsNotNull(challengeId);
            Assert.IsNotNull(options);
            Assert.AreEqual(expectedOptions, options);
            _mockFido2.Verify(f => f.RequestNewCredential(It.IsAny<RequestNewCredentialParams>()), Times.Once);
        }

        [TestMethod]
        public async Task BeginRegistrationAsync_UserNotFound_ThrowsKeyNotFoundException()
        {
            // Arrange
            _mockUserRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((UserBo?)null);

            // Act & Assert
            var thrown = false;
            try
            {
                await _service.BeginRegistrationAsync(999);
            }
            catch (KeyNotFoundException)
            {
                thrown = true;
            }
            Assert.IsTrue(thrown, "Expected KeyNotFoundException was not thrown");
        }

        #endregion

        #region CompleteRegistration Tests

        [TestMethod]
        public async Task CompleteRegistrationAsync_ValidAttestation_CreatesPasskey()
        {
            // Arrange
            var challengeId = Guid.NewGuid().ToString();
            var options = CreateCredentialCreateOptions();
            _memoryCache.Set($"passkey:reg:{challengeId}", options, TimeSpan.FromMinutes(5));

            var dto = new PasskeyRegistrationCompleteDto
            {
                ChallengeId = challengeId,
                AttestationResponse = "{}",
                DisplayName = "My Passkey"
            };

            var credentialResult = (RegisteredPublicKeyCredential)RuntimeHelpers.GetUninitializedObject(typeof(RegisteredPublicKeyCredential));
            // Set fields via reflection since they have required/init setters
            typeof(RegisteredPublicKeyCredential).GetProperty("Id")!.SetValue(credentialResult, new byte[] { 10, 20, 30 });
            typeof(RegisteredPublicKeyCredential).GetProperty("PublicKey")!.SetValue(credentialResult, new byte[] { 40, 50, 60 });
            typeof(RegisteredPublicKeyCredential).GetProperty("SignCount")!.SetValue(credentialResult, 0u);
            typeof(RegisteredPublicKeyCredential).GetProperty("AaGuid")!.SetValue(credentialResult, Guid.Empty);
            typeof(RegisteredPublicKeyCredential).GetProperty("Type")!.SetValue(credentialResult, PublicKeyCredentialType.PublicKey);

            _mockFido2.Setup(f => f.MakeNewCredentialAsync(
                It.IsAny<MakeNewCredentialParams>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(credentialResult);

            var expectedDto = new PasskeyDto { Id = 1, DisplayName = "My Passkey" };
            _mockMapper.Setup(m => m.Map<PasskeyDto>(It.IsAny<UserPasskeyBo>())).Returns(expectedDto);
            _mockPasskeyRepository.Setup(r => r.AddAsync(It.IsAny<UserPasskeyBo>()))
                .ReturnsAsync((UserPasskeyBo p) => p);

            // Act
            var result = await _service.CompleteRegistrationAsync(1, dto);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("My Passkey", result.DisplayName);
            _mockPasskeyRepository.Verify(r => r.AddAsync(It.IsAny<UserPasskeyBo>()), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task CompleteRegistrationAsync_ExpiredChallenge_ThrowsKeyNotFoundException()
        {
            // Arrange - no challenge in cache
            var dto = new PasskeyRegistrationCompleteDto
            {
                ChallengeId = "nonexistent",
                AttestationResponse = "{}",
                DisplayName = "Test"
            };

            // Act & Assert
            var thrown = false;
            try
            {
                await _service.CompleteRegistrationAsync(1, dto);
            }
            catch (KeyNotFoundException)
            {
                thrown = true;
            }
            Assert.IsTrue(thrown, "Expected KeyNotFoundException was not thrown");
        }

        #endregion

        #region BeginLogin Tests

        [TestMethod]
        public async Task BeginLoginAsync_WithEmail_ReturnsOptionsWithAllowCredentials()
        {
            // Arrange
            var user = new UserBo { Id = 1, Email = "test@example.com" };
            _mockUserRepository.Setup(r => r.GetByEmailAsync("test@example.com")).ReturnsAsync(user);

            var passkeys = new List<UserPasskeyBo>
            {
                new() { CredentialId = "ABCD" }
            };
            _mockPasskeyRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(passkeys);

            var expectedOptions = CreateAssertionOptions();
            _mockFido2.Setup(f => f.GetAssertionOptions(It.IsAny<GetAssertionOptionsParams>()))
                .Returns(expectedOptions);

            // Act
            var (challengeId, options) = await _service.BeginLoginAsync(new PasskeyLoginBeginDto { Email = "test@example.com" });

            // Assert
            Assert.IsNotNull(challengeId);
            Assert.AreEqual(expectedOptions, options);
        }

        [TestMethod]
        public async Task BeginLoginAsync_WithoutEmail_ReturnsDiscoverableOptions()
        {
            // Arrange
            var expectedOptions = CreateAssertionOptions();
            _mockFido2.Setup(f => f.GetAssertionOptions(It.IsAny<GetAssertionOptionsParams>()))
                .Returns(expectedOptions);

            // Act
            var (challengeId, options) = await _service.BeginLoginAsync(new PasskeyLoginBeginDto { Email = null });

            // Assert
            Assert.IsNotNull(challengeId);
            Assert.AreEqual(expectedOptions, options);
            _mockUserRepository.Verify(r => r.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        }

        #endregion

        #region CompleteLogin Tests

        [TestMethod]
        public async Task CompleteLoginAsync_ExpiredChallenge_ThrowsKeyNotFoundException()
        {
            // Arrange
            var dto = new PasskeyLoginCompleteDto
            {
                ChallengeId = "nonexistent",
                AssertionResponse = "{}"
            };

            // Act & Assert
            var thrown = false;
            try
            {
                await _service.CompleteLoginAsync(dto);
            }
            catch (KeyNotFoundException)
            {
                thrown = true;
            }
            Assert.IsTrue(thrown, "Expected KeyNotFoundException was not thrown");
        }

        #endregion

        #region GetUserPasskeys Tests

        [TestMethod]
        public async Task GetUserPasskeysAsync_ReturnsPasskeyList()
        {
            // Arrange
            var passkeys = new List<UserPasskeyBo>
            {
                new() { Id = 1, UserId = 1, DisplayName = "Key 1", CredentialId = "a" },
                new() { Id = 2, UserId = 1, DisplayName = "Key 2", CredentialId = "b" }
            };
            _mockPasskeyRepository.Setup(r => r.GetByUserIdAsync(1)).ReturnsAsync(passkeys);

            var dtos = new List<PasskeyDto>
            {
                new() { Id = 1, DisplayName = "Key 1" },
                new() { Id = 2, DisplayName = "Key 2" }
            };
            _mockMapper.Setup(m => m.Map<IEnumerable<PasskeyDto>>(passkeys)).Returns(dtos);

            // Act
            var result = await _service.GetUserPasskeysAsync(1);

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(2, result.Count());
        }

        #endregion

        #region UpdatePasskey Tests

        [TestMethod]
        public async Task UpdatePasskeyAsync_ValidOwner_Updates()
        {
            // Arrange
            var passkey = new UserPasskeyBo
            {
                Id = 1, UserId = 1, DisplayName = "Old Name", CredentialId = "abc"
            };
            _mockPasskeyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(passkey);

            var expectedDto = new PasskeyDto { Id = 1, DisplayName = "New Name" };
            _mockMapper.Setup(m => m.Map<PasskeyDto>(It.IsAny<UserPasskeyBo>())).Returns(expectedDto);

            // Act
            var result = await _service.UpdatePasskeyAsync(1, 1, new UpdatePasskeyDto { DisplayName = "New Name" });

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("New Name", result.DisplayName);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task UpdatePasskeyAsync_WrongOwner_ReturnsNull()
        {
            // Arrange
            var passkey = new UserPasskeyBo
            {
                Id = 1, UserId = 2, DisplayName = "Name", CredentialId = "abc"
            };
            _mockPasskeyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(passkey);

            // Act
            var result = await _service.UpdatePasskeyAsync(1, 1, new UpdatePasskeyDto { DisplayName = "New" });

            // Assert
            Assert.IsNull(result);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Never);
        }

        [TestMethod]
        public async Task UpdatePasskeyAsync_NotFound_ReturnsNull()
        {
            // Arrange
            _mockPasskeyRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((UserPasskeyBo?)null);

            // Act
            var result = await _service.UpdatePasskeyAsync(999, 1, new UpdatePasskeyDto { DisplayName = "New" });

            // Assert
            Assert.IsNull(result);
        }

        #endregion

        #region DeletePasskey Tests

        [TestMethod]
        public async Task DeletePasskeyAsync_ValidOwner_ReturnsTrue()
        {
            // Arrange
            var passkey = new UserPasskeyBo { Id = 1, UserId = 1, CredentialId = "abc" };
            _mockPasskeyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(passkey);

            // Act
            var result = await _service.DeletePasskeyAsync(1, 1);

            // Assert
            Assert.IsTrue(result);
            _mockPasskeyRepository.Verify(r => r.Delete(passkey), Times.Once);
            _mockUnitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [TestMethod]
        public async Task DeletePasskeyAsync_WrongOwner_ReturnsFalse()
        {
            // Arrange
            var passkey = new UserPasskeyBo { Id = 1, UserId = 2, CredentialId = "abc" };
            _mockPasskeyRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(passkey);

            // Act
            var result = await _service.DeletePasskeyAsync(1, 1);

            // Assert
            Assert.IsFalse(result);
            _mockPasskeyRepository.Verify(r => r.Delete(It.IsAny<UserPasskeyBo>()), Times.Never);
        }

        [TestMethod]
        public async Task DeletePasskeyAsync_NotFound_ReturnsFalse()
        {
            // Arrange
            _mockPasskeyRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((UserPasskeyBo?)null);

            // Act
            var result = await _service.DeletePasskeyAsync(999, 1);

            // Assert
            Assert.IsFalse(result);
        }

        #endregion
    }
}
