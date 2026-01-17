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
        // Valider le paramètre "id" principal
        if (await TryRejectInvalidIdAsync(context, "id"))
            return;

        // Valider les autres paramètres d'ID (eventId, userId, calendarId, etc.)
        if (await TryRejectInvalidIdParametersAsync(context))
            return;

        await _next(context);
    }

    private async Task<bool> TryRejectInvalidIdAsync(HttpContext context, string parameterName)
    {
        if (!context.Request.RouteValues.TryGetValue(parameterName, out var idValue))
            return false;

        if (!int.TryParse(idValue?.ToString(), out var id))
            return false;

        if (id > 0)
            return false;

        await WriteInvalidIdResponseAsync(context, parameterName, id);
        return true;
    }

    private async Task<bool> TryRejectInvalidIdParametersAsync(HttpContext context)
    {
        foreach (var routeKey in context.Request.RouteValues.Keys)
        {
            if (!IsIdParameter(routeKey))
                continue;

            if (await TryRejectInvalidIdAsync(context, routeKey))
                return true;
        }

        return false;
    }

    private static bool IsIdParameter(string routeKey)
    {
        return routeKey.EndsWith("Id", StringComparison.OrdinalIgnoreCase) && routeKey != "id";
    }

    private async Task WriteInvalidIdResponseAsync(HttpContext context, string parameterName, int id)
    {
        _logger.LogWarning("Invalid {ParameterName} {Id} rejected for path {Path}",
            parameterName, id, context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

        var errorResponse = new
        {
            error = $"Invalid {parameterName}",
            message = $"The {parameterName} must be a positive integer greater than zero.",
            value = id
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse));
    }
}
