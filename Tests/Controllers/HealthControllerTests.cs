using Dal;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using Moq;
using Web.Controllers;

namespace Tests.Controllers
{
    [TestClass]
    public class HealthControllerTests
    {
        private Mock<ILogger<HealthController>> _mockLogger = null!;

        [TestInitialize]
        public void Setup()
        {
            _mockLogger = new Mock<ILogger<HealthController>>();
        }

        [TestMethod]
        public async Task GetHealth_WhenDatabaseConnected_ReturnsOkHealthy()
        {
            // Arrange - Use InMemory database which always connects
            var options = new DbContextOptionsBuilder<EsnDevContext>()
                .UseInMemoryDatabase(databaseName: "HealthTest_Healthy")
                .Options;
            using var context = new EsnDevContext(options);
            var controller = new HealthController(context, _mockLogger.Object);

            // Act
            var result = await controller.GetHealth();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<OkObjectResult>(result.Result);
        }

        [TestMethod]
        public async Task GetHealth_WhenDatabaseThrows_Returns503()
        {
            // Arrange - Use a mock that simulates database failure
            var mockDatabase = new Mock<DatabaseFacade>(Mock.Of<DbContext>());
            mockDatabase.Setup(d => d.CanConnectAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Connection failed"));

            var options = new DbContextOptionsBuilder<EsnDevContext>()
                .UseInMemoryDatabase(databaseName: "HealthTest_Unhealthy_" + Guid.NewGuid())
                .Options;

            var mockContext = new Mock<EsnDevContext>(options);
            mockContext.Setup(c => c.Database).Returns(mockDatabase.Object);

            var controller = new HealthController(mockContext.Object, _mockLogger.Object);

            // Act
            var result = await controller.GetHealth();

            // Assert
            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType<ObjectResult>(result.Result);
            var objectResult = (ObjectResult)result.Result;
            Assert.AreEqual(StatusCodes.Status503ServiceUnavailable, objectResult.StatusCode);
        }
    }
}
