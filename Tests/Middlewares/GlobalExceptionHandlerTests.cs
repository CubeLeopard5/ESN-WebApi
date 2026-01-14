using Dto.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using System.Net;
using System.Text.Json;
using Web.Middlewares;

namespace Tests.Middlewares;

[TestClass]
public class GlobalExceptionHandlerTests
{
    private Mock<ILogger<GlobalExceptionHandler>> _mockLogger = null!;
    private Mock<IHostEnvironment> _mockEnvironment = null!;
    private GlobalExceptionHandler _handler = null!;
    private DefaultHttpContext _httpContext = null!;

    [TestInitialize]
    public void Setup()
    {
        _mockLogger = new Mock<ILogger<GlobalExceptionHandler>>();
        _mockEnvironment = new Mock<IHostEnvironment>();

        // Par défaut, simule un environnement de développement pour voir les messages d'erreur dans les tests
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");

        _handler = new GlobalExceptionHandler(_mockLogger.Object, _mockEnvironment.Object);
        _httpContext = new DefaultHttpContext();
        _httpContext.Response.Body = new MemoryStream();
    }

    [TestMethod]
    public async Task TryHandleAsync_UnauthorizedAccessException_Returns401()
    {
        // Arrange
        var exception = new UnauthorizedAccessException("Not authorized");
        _httpContext.Request.Path = "/api/test";

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual((int)HttpStatusCode.Unauthorized, _httpContext.Response.StatusCode);
        Assert.IsTrue(_httpContext.Response.ContentType?.StartsWith("application/json") ?? false);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(401, errorResponse.StatusCode);
        Assert.AreEqual("Accès non autorisé", errorResponse.Message);
    }

    [TestMethod]
    public async Task TryHandleAsync_KeyNotFoundException_Returns404()
    {
        // Arrange
        var exception = new KeyNotFoundException("Resource not found");
        _httpContext.Request.Path = "/api/test";

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual((int)HttpStatusCode.NotFound, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(404, errorResponse.StatusCode);
        Assert.AreEqual("Ressource non trouvée", errorResponse.Message);
    }

    [TestMethod]
    public async Task TryHandleAsync_InvalidOperationException_Returns400()
    {
        // Arrange
        var exception = new InvalidOperationException("Invalid operation");
        _httpContext.Request.Path = "/api/test";

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(400, errorResponse.StatusCode);
        Assert.AreEqual("Opération invalide", errorResponse.Message);
    }

    [TestMethod]
    public async Task TryHandleAsync_ArgumentException_Returns400()
    {
        // Arrange
        var exception = new ArgumentException("Invalid argument");
        _httpContext.Request.Path = "/api/test";

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual((int)HttpStatusCode.BadRequest, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(400, errorResponse.StatusCode);
        Assert.AreEqual("Argument invalide", errorResponse.Message);
    }

    [TestMethod]
    public async Task TryHandleAsync_GenericException_Returns500()
    {
        // Arrange
        var exception = new Exception("Something went wrong");
        _httpContext.Request.Path = "/api/test";

        // Act
        var result = await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        Assert.IsTrue(result);
        Assert.AreEqual((int)HttpStatusCode.InternalServerError, _httpContext.Response.StatusCode);

        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(500, errorResponse.StatusCode);
        Assert.AreEqual("Une erreur interne s'est produite", errorResponse.Message);
    }

    [TestMethod]
    public async Task TryHandleAsync_ShouldIncludeRequestPath()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _httpContext.Request.Path = "/api/users/123";

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual("/api/users/123", errorResponse.Path);
    }

    [TestMethod]
    public async Task TryHandleAsync_ShouldLogException()
    {
        // Arrange
        var exception = new Exception("Test exception");
        _httpContext.Request.Path = "/api/test";

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                exception,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task TryHandleAsync_InProduction_ShouldHideExceptionDetails()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        _handler = new GlobalExceptionHandler(_mockLogger.Object, _mockEnvironment.Object);

        var exception = new Exception("Sensitive database error: connection to SQL Server failed");
        _httpContext.Request.Path = "/api/test";

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(500, errorResponse.StatusCode);
        // En production, le message sensible ne doit PAS être exposé
        Assert.AreNotEqual("Sensitive database error: connection to SQL Server failed", errorResponse.Details);
        Assert.AreEqual("An error occurred while processing your request. Please contact support if the problem persists.", errorResponse.Details);
    }

    [TestMethod]
    public async Task TryHandleAsync_InDevelopment_ShouldShowExceptionDetails()
    {
        // Arrange
        var exception = new Exception("Detailed error message for debugging");
        _httpContext.Request.Path = "/api/test";

        // Act
        await _handler.TryHandleAsync(_httpContext, exception, CancellationToken.None);

        // Assert
        _httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var responseBody = await new StreamReader(_httpContext.Response.Body).ReadToEndAsync();
        var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseBody, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.IsNotNull(errorResponse);
        Assert.AreEqual(500, errorResponse.StatusCode);
        // En développement, le message détaillé doit être visible
        Assert.AreEqual("Detailed error message for debugging", errorResponse.Details);
    }
}
