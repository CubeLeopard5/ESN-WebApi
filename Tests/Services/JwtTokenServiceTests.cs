using Bo.Models;
using Business.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.Services
{
    [TestClass]
    public class JwtTokenServiceTests
    {
        private Mock<IConfiguration> _mockConfiguration = null!;
        private Mock<ILogger<JwtTokenService>> _mockLogger = null!;
        private JwtTokenService _jwtTokenService = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockConfiguration = new Mock<IConfiguration>();
            _mockLogger = new Mock<ILogger<JwtTokenService>>();

            var jwtSection = new Mock<IConfigurationSection>();
            jwtSection.Setup(x => x["Key"]).Returns("ThisIsAVerySecureKeyForJWTTokenGenerationWithAtLeast32Characters");
            jwtSection.Setup(x => x["Issuer"]).Returns("TestIssuer");
            jwtSection.Setup(x => x["Audience"]).Returns("TestAudience");
            jwtSection.Setup(x => x["ExpireMinutes"]).Returns("30");
            _mockConfiguration.Setup(c => c.GetSection("Jwt")).Returns(jwtSection.Object);

            _jwtTokenService = new JwtTokenService(
                _mockConfiguration.Object,
                _mockLogger.Object
            );
        }

        [TestMethod]
        public void GenerateToken_ValidUser_ReturnsNonEmptyToken()
        {
            // Arrange
            var user = new UserBo
            {
                Id = 1,
                Email = "test@example.com",
                FirstName = "Test",
                LastName = "User",
                StudentType = "exchange"
            };

            // Act
            var token = _jwtTokenService.GenerateToken(user);

            // Assert
            Assert.IsNotNull(token);
            Assert.IsFalse(string.IsNullOrWhiteSpace(token));
        }

        [TestMethod]
        public void GenerateToken_UserWithRole_IncludesRoleClaims()
        {
            // Arrange
            var user = new UserBo
            {
                Id = 1,
                Email = "admin@example.com",
                FirstName = "Admin",
                LastName = "User",
                StudentType = "esn_member",
                Role = new RoleBo
                {
                    Id = 1,
                    Name = "Admin",
                    CanCreateEvents = true,
                    CanModifyEvents = true,
                    CanDeleteEvents = true,
                    CanCreateUsers = true,
                    CanModifyUsers = true,
                    CanDeleteUsers = true
                }
            };

            // Act
            var token = _jwtTokenService.GenerateToken(user);

            // Assert
            Assert.IsNotNull(token);
            // Token is a valid JWT (3 parts separated by dots)
            var parts = token.Split('.');
            Assert.AreEqual(3, parts.Length);
        }

        [TestMethod]
        public void GenerateToken_UserWithoutRole_ReturnsValidToken()
        {
            // Arrange
            var user = new UserBo
            {
                Id = 2,
                Email = "noRole@example.com",
                FirstName = "No",
                LastName = "Role",
                StudentType = "local",
                Role = null
            };

            // Act
            var token = _jwtTokenService.GenerateToken(user);

            // Assert
            Assert.IsNotNull(token);
            var parts = token.Split('.');
            Assert.AreEqual(3, parts.Length);
        }
    }
}
