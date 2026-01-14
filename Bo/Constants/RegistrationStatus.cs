namespace Bo.Constants;

/// <summary>
/// Constantes pour les statuts d'inscription aux événements
/// </summary>
public static class RegistrationStatus
{
    /// <summary>
    /// Inscription confirmée et active
    /// </summary>
    public const string Registered = "registered";

    /// <summary>
    /// Inscription annulée par l'utilisateur
    /// </summary>
    public const string Cancelled = "cancelled";

    /// <summary>
    /// Inscription en attente de validation
    /// </summary>
    public const string Pending = "pending";

    /// <summary>
    /// Inscription approuvée par un administrateur
    /// </summary>
    public const string Approved = "approved";

    /// <summary>
    /// Inscription rejetée
    /// </summary>
    public const string Rejected = "rejected";
}
