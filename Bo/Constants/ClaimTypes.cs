namespace Bo.Constants;

/// <summary>
/// Constantes pour les types de claims JWT
/// </summary>
public static class CustomClaimTypes
{
    /// <summary>
    /// Claim pour l'email de l'utilisateur
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// Claim pour le rôle de l'utilisateur
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// Claim pour l'ID de l'utilisateur
    /// </summary>
    public const string UserId = "userId";

    /// <summary>
    /// Claim pour le nom de l'utilisateur
    /// </summary>
    public const string Name = "name";
}
