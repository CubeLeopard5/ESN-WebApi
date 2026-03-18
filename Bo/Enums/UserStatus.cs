namespace Bo.Enums;

/// <summary>
/// Statut d'un compte utilisateur
/// </summary>
public enum UserStatus
{
    /// <summary>
    /// Compte en attente de validation par un administrateur
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Compte approuvé, l'utilisateur peut se connecter
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Compte refusé, l'utilisateur ne peut pas se connecter
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Compte archivé, l'utilisateur ne peut plus se connecter
    /// </summary>
    Archived = 3
}
