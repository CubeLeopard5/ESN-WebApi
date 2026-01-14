namespace Business.Exceptions;

/// <summary>
/// Exception levée quand un utilisateur est authentifié mais n'a pas l'autorisation d'accéder à une ressource
/// Se traduit en code HTTP 403 Forbidden
/// </summary>
public class ForbiddenAccessException : Exception
{
    public ForbiddenAccessException() : base()
    {
    }

    public ForbiddenAccessException(string message) : base(message)
    {
    }

    public ForbiddenAccessException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
