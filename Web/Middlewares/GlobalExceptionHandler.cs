using Dto.Common;
using Microsoft.AspNetCore.Diagnostics;
using System.Net;

namespace Web.Middlewares;

/// <summary>
/// Middleware global pour la gestion centralisée des exceptions
/// </summary>
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        logger.LogError(exception, "Une exception non gérée s'est produite: {Message}", exception.Message);

        var errorResponse = exception switch
        {
            UnauthorizedAccessException unauthorizedException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Unauthorized,
                Message = "Accès non autorisé",
                Details = GetSafeDetails(unauthorizedException),
                Path = httpContext.Request.Path
            },
            KeyNotFoundException keyNotFoundException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.NotFound,
                Message = "Ressource non trouvée",
                Details = GetSafeDetails(keyNotFoundException),
                Path = httpContext.Request.Path
            },
            InvalidOperationException invalidOperationException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Opération invalide",
                Details = GetSafeDetails(invalidOperationException),
                Path = httpContext.Request.Path
            },
            ArgumentException argumentException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = "Argument invalide",
                Details = GetSafeDetails(argumentException),
                Path = httpContext.Request.Path
            },
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = "Une erreur interne s'est produite",
                Details = GetSafeDetails(exception),
                Path = httpContext.Request.Path
            }
        };

        httpContext.Response.StatusCode = errorResponse.StatusCode;
        httpContext.Response.ContentType = "application/json";

        await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

        return true;
    }

    /// <summary>
    /// Retourne les détails de l'exception seulement en développement.
    /// En production, retourne un message générique pour éviter la fuite d'informations sensibles.
    /// </summary>
    private string GetSafeDetails(Exception exception)
    {
        return environment.IsDevelopment()
            ? exception.Message
            : "An error occurred while processing your request. Please contact support if the problem persists.";
    }
}
