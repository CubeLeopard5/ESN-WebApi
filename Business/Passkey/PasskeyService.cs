using AutoMapper;
using Bo.Enums;
using Bo.Models;
using Business.Exceptions;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Passkey;
using Dto.User;
using Fido2NetLib;
using Fido2NetLib.Objects;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace Business.Passkey;

/// <summary>
/// Service de gestion des passkeys WebAuthn/FIDO2
/// </summary>
public class PasskeyService(
    IUnitOfWork unitOfWork,
    IFido2 fido2,
    IJwtTokenService jwtTokenService,
    IMemoryCache memoryCache,
    IMapper mapper,
    ILogger<PasskeyService> logger)
    : IPasskeyService
{
    private static readonly TimeSpan ChallengeTtl = TimeSpan.FromMinutes(5);

    /// <inheritdoc />
    public async Task<(string ChallengeId, CredentialCreateOptions Options)> BeginRegistrationAsync(int userId)
    {
        logger.LogInformation("PasskeyService.BeginRegistrationAsync called for UserId {UserId}", userId);

        var user = await unitOfWork.Users.GetByIdAsync(userId)
            ?? throw new KeyNotFoundException($"User {userId} not found");

        // Build Fido2User
        var fido2User = new Fido2User
        {
            Id = Encoding.UTF8.GetBytes(user.Id.ToString()),
            Name = user.Email,
            DisplayName = $"{user.FirstName} {user.LastName}"
        };

        // Get existing credentials to exclude
        var existingPasskeys = await unitOfWork.Passkeys.GetByUserIdAsync(userId);
        var excludeCredentials = existingPasskeys
            .Select(p => new PublicKeyCredentialDescriptor(Base64UrlDecode(p.CredentialId)))
            .ToList();

        var options = fido2.RequestNewCredential(new RequestNewCredentialParams
        {
            User = fido2User,
            ExcludeCredentials = excludeCredentials,
            AuthenticatorSelection = new AuthenticatorSelection
            {
                ResidentKey = ResidentKeyRequirement.Preferred,
                UserVerification = UserVerificationRequirement.Preferred
            },
            AttestationPreference = AttestationConveyancePreference.None
        });

        // Store options in cache (single-use challenge)
        var challengeId = Guid.NewGuid().ToString();
        memoryCache.Set($"passkey:reg:{challengeId}", options, ChallengeTtl);

        logger.LogInformation("PasskeyService.BeginRegistrationAsync completed for UserId {UserId}, ChallengeId {ChallengeId}",
            userId, challengeId);

        return (challengeId, options);
    }

    /// <inheritdoc />
    public async Task<PasskeyDto> CompleteRegistrationAsync(int userId, PasskeyRegistrationCompleteDto dto)
    {
        logger.LogInformation("PasskeyService.CompleteRegistrationAsync called for UserId {UserId}", userId);

        // Retrieve and remove challenge from cache (single-use)
        var cacheKey = $"passkey:reg:{dto.ChallengeId}";
        if (!memoryCache.TryGetValue(cacheKey, out CredentialCreateOptions? options) || options == null)
        {
            throw new KeyNotFoundException("Challenge not found or expired");
        }
        memoryCache.Remove(cacheKey);

        // Parse attestation response
        var attestationResponse = JsonSerializer.Deserialize<AuthenticatorAttestationRawResponse>(dto.AttestationResponse)
            ?? throw new ArgumentException("Invalid attestation response");

        // Verify attestation
        var credential = await fido2.MakeNewCredentialAsync(new MakeNewCredentialParams
        {
            AttestationResponse = attestationResponse,
            OriginalOptions = options,
            IsCredentialIdUniqueToUserCallback = async (args, ct) =>
            {
                var exists = await unitOfWork.Passkeys.CredentialIdExistsAsync(Base64UrlEncode(args.CredentialId));
                return !exists;
            }
        });

        // Store credential
        var passkey = new UserPasskeyBo
        {
            UserId = userId,
            CredentialId = Base64UrlEncode(credential.Id),
            PublicKey = credential.PublicKey,
            SignCount = credential.SignCount,
            AaGuid = credential.AaGuid,
            CredentialType = credential.Type.ToString(),
            DisplayName = dto.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        await unitOfWork.Passkeys.AddAsync(passkey);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("PasskeyService.CompleteRegistrationAsync completed - Passkey {PasskeyId} created for UserId {UserId}",
            passkey.Id, userId);

        return mapper.Map<PasskeyDto>(passkey);
    }

    /// <inheritdoc />
    public async Task<(string ChallengeId, AssertionOptions Options)> BeginLoginAsync(PasskeyLoginBeginDto dto)
    {
        logger.LogInformation("PasskeyService.BeginLoginAsync called with Email {Email}", dto.Email ?? "(discoverable)");

        var allowCredentials = new List<PublicKeyCredentialDescriptor>();

        if (!string.IsNullOrEmpty(dto.Email))
        {
            var user = await unitOfWork.Users.GetByEmailAsync(dto.Email);
            if (user != null)
            {
                var passkeys = await unitOfWork.Passkeys.GetByUserIdAsync(user.Id);
                allowCredentials = passkeys
                    .Select(p => new PublicKeyCredentialDescriptor(Base64UrlDecode(p.CredentialId)))
                    .ToList();
            }
            // If user not found, still return options (don't reveal if email exists)
        }

        var options = fido2.GetAssertionOptions(new GetAssertionOptionsParams
        {
            AllowedCredentials = allowCredentials,
            UserVerification = UserVerificationRequirement.Preferred
        });

        var challengeId = Guid.NewGuid().ToString();
        memoryCache.Set($"passkey:login:{challengeId}", options, ChallengeTtl);

        logger.LogInformation("PasskeyService.BeginLoginAsync completed, ChallengeId {ChallengeId}", challengeId);

        return (challengeId, options);
    }

    /// <inheritdoc />
    public async Task<UserLoginResponseDto> CompleteLoginAsync(PasskeyLoginCompleteDto dto)
    {
        logger.LogInformation("PasskeyService.CompleteLoginAsync called");

        // Retrieve and remove challenge from cache (single-use)
        var cacheKey = $"passkey:login:{dto.ChallengeId}";
        if (!memoryCache.TryGetValue(cacheKey, out AssertionOptions? options) || options == null)
        {
            throw new KeyNotFoundException("Challenge not found or expired");
        }
        memoryCache.Remove(cacheKey);

        // Parse assertion response
        var assertionResponse = JsonSerializer.Deserialize<AuthenticatorAssertionRawResponse>(dto.AssertionResponse)
            ?? throw new ArgumentException("Invalid assertion response");

        // Find the credential (assertionResponse.Id is already base64url encoded)
        var passkey = await unitOfWork.Passkeys.GetByCredentialIdAsync(assertionResponse.Id)
            ?? throw new KeyNotFoundException("Credential not found");

        // Verify assertion
        var result = await fido2.MakeAssertionAsync(new MakeAssertionParams
        {
            AssertionResponse = assertionResponse,
            OriginalOptions = options,
            StoredPublicKey = passkey.PublicKey,
            StoredSignatureCounter = passkey.SignCount,
            IsUserHandleOwnerOfCredentialIdCallback = async (args, ct) =>
            {
                var storedPasskey = await unitOfWork.Passkeys.GetByCredentialIdAsync(Base64UrlEncode(args.CredentialId));
                if (storedPasskey == null) return false;
                var userIdFromHandle = Encoding.UTF8.GetString(args.UserHandle);
                return storedPasskey.UserId.ToString() == userIdFromHandle;
            }
        });

        // Update sign count and last used
        passkey.SignCount = result.SignCount;
        passkey.LastUsedAt = DateTime.UtcNow;
        unitOfWork.Passkeys.Update(passkey);
        await unitOfWork.SaveChangesAsync();

        // Check user status (same rules as password login)
        var user = passkey.User;
        if (user.Status == UserStatus.Pending)
        {
            logger.LogWarning("PasskeyService.CompleteLoginAsync - User {UserId} attempted login with Pending status", user.Id);
            throw new ForbiddenAccessException("Your account is pending approval by an administrator.");
        }

        if (user.Status == UserStatus.Rejected)
        {
            logger.LogWarning("PasskeyService.CompleteLoginAsync - User {UserId} attempted login with Rejected status", user.Id);
            throw new ForbiddenAccessException("Your account has been rejected. Please contact the administrator.");
        }

        if (user.Status == UserStatus.Archived)
        {
            logger.LogWarning("PasskeyService.CompleteLoginAsync - User {UserId} attempted login with Archived status", user.Id);
            throw new ForbiddenAccessException("Your account has been archived.");
        }

        // Generate JWT
        var token = jwtTokenService.GenerateToken(user);

        logger.LogInformation("PasskeyService.CompleteLoginAsync completed for UserId {UserId}", user.Id);

        return new UserLoginResponseDto
        {
            Token = token,
            User = mapper.Map<UserDto>(user)
        };
    }

    /// <inheritdoc />
    public async Task<IEnumerable<PasskeyDto>> GetUserPasskeysAsync(int userId)
    {
        logger.LogInformation("PasskeyService.GetUserPasskeysAsync called for UserId {UserId}", userId);

        var passkeys = await unitOfWork.Passkeys.GetByUserIdAsync(userId);
        return mapper.Map<IEnumerable<PasskeyDto>>(passkeys);
    }

    /// <inheritdoc />
    public async Task<PasskeyDto?> UpdatePasskeyAsync(int passkeyId, int userId, UpdatePasskeyDto dto)
    {
        logger.LogInformation("PasskeyService.UpdatePasskeyAsync called for PasskeyId {PasskeyId}, UserId {UserId}",
            passkeyId, userId);

        var passkey = await unitOfWork.Passkeys.GetByIdAsync(passkeyId);
        if (passkey == null || passkey.UserId != userId)
        {
            return null;
        }

        passkey.DisplayName = dto.DisplayName;
        unitOfWork.Passkeys.Update(passkey);
        await unitOfWork.SaveChangesAsync();

        return mapper.Map<PasskeyDto>(passkey);
    }

    /// <inheritdoc />
    public async Task<bool> DeletePasskeyAsync(int passkeyId, int userId)
    {
        logger.LogInformation("PasskeyService.DeletePasskeyAsync called for PasskeyId {PasskeyId}, UserId {UserId}",
            passkeyId, userId);

        var passkey = await unitOfWork.Passkeys.GetByIdAsync(passkeyId);
        if (passkey == null || passkey.UserId != userId)
        {
            return false;
        }

        unitOfWork.Passkeys.Delete(passkey);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("PasskeyService.DeletePasskeyAsync completed - Passkey {PasskeyId} deleted", passkeyId);

        return true;
    }

    private static string Base64UrlEncode(byte[] data)
    {
        return Convert.ToBase64String(data)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string base64Url)
    {
        var base64 = base64Url.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
