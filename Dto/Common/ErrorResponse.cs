namespace Dto.Common;

/// <summary>
/// Réponse d'erreur standardisée pour l'API
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Code d'erreur HTTP
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Message d'erreur principal
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Détails supplémentaires de l'erreur (optionnel)
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Timestamp de l'erreur
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Chemin de la requête qui a causé l'erreur
    /// </summary>
    public string? Path { get; set; }

    /// <summary>
    /// Liste des erreurs de validation (optionnel)
    /// </summary>
    public Dictionary<string, string[]>? ValidationErrors { get; set; }

    public ErrorResponse() { }

    public ErrorResponse(int statusCode, string message, string? details = null)
    {
        StatusCode = statusCode;
        Message = message;
        Details = details;
    }
}
