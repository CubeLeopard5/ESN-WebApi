namespace Business.Interfaces;

/// <summary>
/// Service de verification Google reCAPTCHA v3
/// </summary>
public interface IRecaptchaService
{
    /// <summary>
    /// Verifie un token reCAPTCHA v3 aupres de l'API Google
    /// </summary>
    /// <param name="token">Token reCAPTCHA fourni par le client</param>
    /// <param name="expectedAction">Action attendue (e.g. "login", "register")</param>
    /// <returns>True si le token est valide et le score suffisant</returns>
    Task<bool> VerifyAsync(string token, string expectedAction);
}
