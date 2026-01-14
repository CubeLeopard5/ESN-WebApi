using Serilog;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Web.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        var userEmail = context.User?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? "Anonymous";

        var path = context.Request.Path;
        var method = context.Request.Method;
        var ip = context.Connection.RemoteIpAddress?.ToString();

        Log.Information("➡️ {Method} {Path} called by {User} from {IP} at {Time}",
            method, path, userEmail, ip, DateTime.UtcNow);

        await next(context);

        Log.Information("⬅️ {Method} {Path} completed with status {StatusCode}",
            method, path, context.Response.StatusCode);
    }
}
