using System.Text.Json;
using Business.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Business.Auth;

/// <inheritdoc />
public class RecaptchaService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<RecaptchaService> logger)
    : IRecaptchaService
{
    private const string VerifyUrl = "https://www.google.com/recaptcha/api/siteverify";
    private const double MinScore = 0.5;

    /// <inheritdoc />
    public async Task<bool> VerifyAsync(string token, string expectedAction)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            logger.LogWarning("RecaptchaService.VerifyAsync - Empty token");
            return false;
        }

        var secretKey = configuration["Recaptcha:SecretKey"];
        if (string.IsNullOrEmpty(secretKey))
        {
            // If no secret key is configured, skip verification (dev mode)
            logger.LogWarning("RecaptchaService.VerifyAsync - No secret key configured, skipping verification");
            return true;
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            var response = await client.PostAsync(VerifyUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            }));

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null || !result.Success)
            {
                logger.LogWarning("RecaptchaService.VerifyAsync - Verification failed: {Errors}",
                    result?.ErrorCodes != null ? string.Join(", ", result.ErrorCodes) : "unknown");
                return false;
            }

            if (!string.IsNullOrEmpty(expectedAction) && result.Action != expectedAction)
            {
                logger.LogWarning("RecaptchaService.VerifyAsync - Action mismatch: expected {Expected}, got {Actual}",
                    expectedAction, result.Action);
                return false;
            }

            if (result.Score < MinScore)
            {
                logger.LogWarning("RecaptchaService.VerifyAsync - Score too low: {Score} (min: {MinScore})",
                    result.Score, MinScore);
                return false;
            }

            logger.LogInformation("RecaptchaService.VerifyAsync - Success, score: {Score}, action: {Action}",
                result.Score, result.Action);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "RecaptchaService.VerifyAsync - Exception during verification");
            return false;
        }
    }

    private class RecaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
        public string? Action { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("challenge_ts")]
        public string? ChallengeTs { get; set; }

        public string? Hostname { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("error-codes")]
        public string[]? ErrorCodes { get; set; }
    }
}
