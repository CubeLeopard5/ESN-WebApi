namespace Web.Middlewares;

/// <summary>
/// Middleware pour ajouter les headers de sécurité HTTP recommandés
/// Protège contre XSS, clickjacking, MIME sniffing, etc.
/// </summary>
public class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: Empêche le navigateur de deviner le MIME type
        // Protège contre les attaques basées sur le MIME sniffing
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");

        // X-Frame-Options: Empêche l'affichage de la page dans une iframe
        // Protège contre le clickjacking
        context.Response.Headers.Append("X-Frame-Options", "DENY");

        // X-XSS-Protection: Active la protection XSS du navigateur (legacy, mais utile pour anciens navigateurs)
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");

        // Referrer-Policy: Contrôle les informations de référence envoyées
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");

        // Content-Security-Policy: Définit les sources de contenu autorisées
        // Note: Adapté pour une API (pas de scripts/styles inline)
        context.Response.Headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'");

        // Permissions-Policy: Contrôle les fonctionnalités du navigateur
        context.Response.Headers.Append("Permissions-Policy", "geolocation=(), microphone=(), camera=()");

        // Strict-Transport-Security: Force HTTPS (seulement en production)
        // max-age=31536000 = 1 an, includeSubDomains applique à tous les sous-domaines
        if (!context.Request.IsHttps && context.RequestServices.GetRequiredService<IHostEnvironment>().IsProduction())
        {
            // En production, rediriger vers HTTPS devrait être géré en amont
            // Ce header indique aux navigateurs d'utiliser uniquement HTTPS pour les futures requêtes
        }
        else if (context.Request.IsHttps)
        {
            context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
        }

        await next(context);
    }
}

/// <summary>
/// Extensions pour enregistrer le middleware de headers de sécurité
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
{
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<SecurityHeadersMiddleware>();
    }
}
