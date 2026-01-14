using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Web.Middlewares;

namespace Tests.Middlewares;

[TestClass]
public class RequestLoggingMiddlewareTests
{
    private DefaultHttpContext _httpContext = null!;
    private bool _nextDelegateCalled;

    [TestInitialize]
    public void Setup()
    {
        _httpContext = new DefaultHttpContext();
        _httpContext.Request.Path = "/api/test";
        _httpContext.Request.Method = "GET";
        _httpContext.Response.StatusCode = 200;
        _nextDelegateCalled = false;
    }

    [TestMethod]
    public async Task Invoke_ShouldCallNextDelegate()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(context =>
        {
            _nextDelegateCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        Assert.IsTrue(_nextDelegateCalled);
    }

    [TestMethod]
    public async Task Invoke_WithAuthenticatedUser_ShouldNotThrow()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "test@example.com"),
            new(ClaimTypes.NameIdentifier, "test@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        // Act & Assert - should not throw
        await middleware.Invoke(_httpContext);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_WithAnonymousUser_ShouldNotThrow()
    {
        // Arrange
        _httpContext.User = new ClaimsPrincipal();

        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        // Act & Assert - should not throw
        await middleware.Invoke(_httpContext);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_WithNoUser_ShouldNotThrow()
    {
        // Arrange
        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        // Act & Assert - should not throw
        await middleware.Invoke(_httpContext);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_WithNameIdentifierClaim_ShouldNotThrow()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, "user@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        // Act & Assert - should not throw
        await middleware.Invoke(_httpContext);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_WithSubClaim_ShouldNotThrow()
    {
        // Arrange
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "admin@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        _httpContext.User = new ClaimsPrincipal(identity);

        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        // Act & Assert - should not throw
        await middleware.Invoke(_httpContext);
        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_ShouldExecuteNextAndLogResponse()
    {
        // Arrange
        var responseStatusCode = 201;
        var middleware = new RequestLoggingMiddleware(context =>
        {
            context.Response.StatusCode = responseStatusCode;
            return Task.CompletedTask;
        });

        // Act
        await middleware.Invoke(_httpContext);

        // Assert
        Assert.AreEqual(responseStatusCode, _httpContext.Response.StatusCode);
    }

    [TestMethod]
    public async Task Invoke_WithDifferentHttpMethods_ShouldNotThrow()
    {
        // Arrange
        var methods = new[] { "GET", "POST", "PUT", "DELETE", "PATCH" };
        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        foreach (var method in methods)
        {
            _httpContext.Request.Method = method;

            // Act & Assert - should not throw
            await middleware.Invoke(_httpContext);
        }

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_WithDifferentPaths_ShouldNotThrow()
    {
        // Arrange
        var paths = new[] { "/api/users", "/api/events/123", "/health", "/" };
        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        foreach (var path in paths)
        {
            _httpContext.Request.Path = path;

            // Act & Assert - should not throw
            await middleware.Invoke(_httpContext);
        }

        Assert.IsTrue(true);
    }

    [TestMethod]
    public async Task Invoke_WithRemoteIpAddress_ShouldNotThrow()
    {
        // Arrange
        _httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("127.0.0.1");
        var middleware = new RequestLoggingMiddleware(context => Task.CompletedTask);

        // Act & Assert - should not throw
        await middleware.Invoke(_httpContext);
        Assert.IsTrue(true);
    }
}
