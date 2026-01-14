using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Web.Middlewares;

/// <summary>
/// Middleware pour valider automatiquement tous les ID de route
/// Rejette les requêtes avec des ID <= 0
/// </summary>
public class ValidateIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ValidateIdMiddleware> _logger;

    public ValidateIdMiddleware(RequestDelegate next, ILogger<ValidateIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Vérifier si la route contient un paramètre "id"
        if (context.Request.RouteValues.TryGetValue("id", out var idValue))
        {
            // Tenter de convertir en int
            if (int.TryParse(idValue?.ToString(), out var id))
            {
                if (id <= 0)
                {
                    _logger.LogWarning("Invalid ID {Id} rejected for path {Path}", id, context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    context.Response.ContentType = "application/json";

                    var errorResponse = new
                    {
                        error = "Invalid ID",
                        message = "The ID must be a positive integer greater than zero.",
                        id = id
                    };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                    return;
                }
            }
        }

        // Vérifier d'autres paramètres d'ID courants (eventId, userId, calendarId, etc.)
        foreach (var routeKey in context.Request.RouteValues.Keys)
        {
            if (routeKey.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && routeKey != "id")
            {
                if (context.Request.RouteValues.TryGetValue(routeKey, out var value))
                {
                    if (int.TryParse(value?.ToString(), out var paramId) && paramId <= 0)
                    {
                        _logger.LogWarning("Invalid {ParameterName} {Id} rejected for path {Path}",
                            routeKey, paramId, context.Request.Path);

                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        context.Response.ContentType = "application/json";

                        var errorResponse = new
                        {
                            error = $"Invalid {routeKey}",
                            message = $"The {routeKey} must be a positive integer greater than zero.",
                            value = paramId
                        };

                        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
                        return;
                    }
                }
            }
        }

        await _next(context);
    }
}
