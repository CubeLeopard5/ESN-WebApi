using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Web.Extensions;

/// <summary>
/// Extensions pour ClaimsPrincipal pour simplifier l'extraction de l'email utilisateur
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Récupère l'email de l'utilisateur depuis les claims
    /// </summary>
    /// <param name="user">Le ClaimsPrincipal</param>
    /// <returns>L'email de l'utilisateur ou null si non trouvé</returns>
    public static string? GetUserEmail(this ClaimsPrincipal user)
    {
        return user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
    }

    /// <summary>
    /// Récupère l'email de l'utilisateur depuis les claims ou lève une exception
    /// </summary>
    /// <param name="user">Le ClaimsPrincipal</param>
    /// <returns>L'email de l'utilisateur</returns>
    /// <exception cref="UnauthorizedAccessException">Si l'email n'est pas trouvé dans les claims</exception>
    public static string GetUserEmailOrThrow(this ClaimsPrincipal user)
    {
        var email = user.GetUserEmail();
        if (string.IsNullOrEmpty(email))
            throw new UnauthorizedAccessException("User email not found in claims");
        return email;
    }
}
