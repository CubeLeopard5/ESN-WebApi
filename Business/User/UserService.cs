using AutoMapper;
using Bo.Enums;
using Business.Exceptions;
using Business.Interfaces;
using Dal.UnitOfWork.Interfaces;
using Dto.Common;
using Dto.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Caching.Memory;

namespace Business.User;

/// <summary>
/// Service de gestion des utilisateurs, authentification JWT et opérations CRUD
/// </summary>
public class UserService(
    IUnitOfWork unitOfWork,
    IMapper mapper,
    ILogger<UserService> logger,
    IConfiguration configuration,
    IJwtTokenService jwtTokenService,
    Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache)
    : IUserService
{
    /// <inheritdoc />
    public async Task<UserLoginResponseDto> LoginAsync(UserLoginDto loginDto)
    {
        logger.LogInformation("UserService.LoginAsync called for {Email}", loginDto.Email);

        var user = await unitOfWork.Users.GetByEmailWithRoleAsync(loginDto.Email);
        var hasher = new PasswordHasher<Bo.Models.UserBo>();

        // Si l'utilisateur n'existe pas, effectuer quand même une vérification de hash
        // pour maintenir un timing constant et éviter l'énumération d'utilisateurs
        if (user == null)
        {
            var dummyUser = new Bo.Models.UserBo();
            var dummyHash = hasher.HashPassword(dummyUser, "dummy_password_for_timing_protection");
            hasher.VerifyHashedPassword(dummyUser, dummyHash, loginDto.Password);

            logger.LogWarning("UserService.LoginAsync - Invalid credentials");
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Check account lockout
        if (user.LockoutEndTime.HasValue && user.LockoutEndTime > DateTime.UtcNow)
        {
            var remainingMinutes = (int)Math.Ceiling((user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes);
            logger.LogWarning("UserService.LoginAsync - Account locked for User {UserId}, {Minutes} min remaining",
                user.Id, remainingMinutes);
            throw new ForbiddenAccessException($"Account temporarily locked. Try again in {remainingMinutes} minute(s).");
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            // Increment failed attempts and apply lockout if threshold reached
            user.FailedLoginAttempts++;
            user.LockoutEndTime = GetLockoutDuration(user.FailedLoginAttempts);
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();

            if (user.LockoutEndTime.HasValue)
            {
                var lockMinutes = (int)Math.Ceiling((user.LockoutEndTime.Value - DateTime.UtcNow).TotalMinutes);
                logger.LogWarning("UserService.LoginAsync - Account locked after {Attempts} failed attempts for User {UserId}",
                    user.FailedLoginAttempts, user.Id);
                throw new ForbiddenAccessException($"Too many failed attempts. Account locked for {lockMinutes} minute(s).");
            }

            logger.LogWarning("UserService.LoginAsync - Invalid credentials (attempt {Attempts}) for User {UserId}",
                user.FailedLoginAttempts, user.Id);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        // Reset failed attempts on successful login
        if (user.FailedLoginAttempts > 0)
        {
            user.FailedLoginAttempts = 0;
            user.LockoutEndTime = null;
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();
        }

        // Vérifier le statut du compte
        if (user.Status == UserStatus.Pending)
        {
            logger.LogWarning("UserService.LoginAsync - User {UserId} attempted login with Pending status", user.Id);
            throw new ForbiddenAccessException("Your account is pending approval by an administrator.");
        }

        if (user.Status == UserStatus.Rejected)
        {
            logger.LogWarning("UserService.LoginAsync - User {UserId} attempted login with Rejected status", user.Id);
            throw new ForbiddenAccessException("Your account has been rejected. Please contact the administrator.");
        }

        var token = jwtTokenService.GenerateToken(user);

        logger.LogInformation("UserService.LoginAsync completed successfully for {Email}", loginDto.Email);

        return new UserLoginResponseDto
        {
            Token = token,
            User = mapper.Map<UserDto>(user)
        };
    }

    /// <inheritdoc />
    public async Task<UserLoginResponseDto> RefreshTokenAsync(string token)
    {
        logger.LogInformation("UserService.RefreshTokenAsync called");

        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtConfig = configuration.GetSection("Jwt");
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? jwtConfig["Key"]!;
        var key = Encoding.UTF8.GetBytes(jwtKey);

        try
        {
            // Validate token (allowing expired tokens)
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtConfig["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtConfig["Audience"],
                ValidateLifetime = false, // Allow expired tokens for refresh
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            // Vérifier que le token n'est pas trop ancien (max 7 jours après émission)
            var jwtToken = validatedToken as JwtSecurityToken;
            if (jwtToken != null)
            {
                var issuedAt = jwtToken.IssuedAt;
                var maxRefreshDuration = TimeSpan.FromDays(7);

                if (DateTime.UtcNow - issuedAt > maxRefreshDuration)
                {
                    logger.LogWarning("UserService.RefreshTokenAsync - Token too old to refresh (issued at {IssuedAt})", issuedAt);
                    throw new UnauthorizedAccessException("Token has expired beyond refresh period");
                }
            }

            // Extract email from claims
            var email = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                logger.LogWarning("UserService.RefreshTokenAsync - No email claim found in token");
                throw new UnauthorizedAccessException("Invalid token");
            }

            // Get user from database
            var user = await unitOfWork.Users.GetByEmailWithRoleAsync(email);

            if (user == null)
            {
                logger.LogWarning("UserService.RefreshTokenAsync - User not found for email {Email}", email);
                throw new KeyNotFoundException($"User not found: {email}");
            }

            // Generate new token
            var newToken = jwtTokenService.GenerateToken(user);

            logger.LogInformation("UserService.RefreshTokenAsync completed successfully for {Email}", user.Email);

            return new UserLoginResponseDto
            {
                Token = newToken,
                User = mapper.Map<UserDto>(user)
            };
        }
        catch (SecurityTokenException ex)
        {
            logger.LogWarning(ex, "UserService.RefreshTokenAsync - Invalid token");
            throw new UnauthorizedAccessException("Invalid token", ex);
        }
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetCurrentUserAsync(string userEmail)
    {
        logger.LogInformation("UserService.GetCurrentUserAsync called for {Email}", userEmail);

        var user = await unitOfWork.Users.GetByEmailAsync(userEmail);
        if (user == null)
        {
            logger.LogWarning("UserService.GetCurrentUserAsync - User not found for {Email}", userEmail);
            return null;
        }

        logger.LogInformation("UserService.GetCurrentUserAsync completed for {Email}", userEmail);

        return mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    [Obsolete("Use GetAllUsersAsync(PaginationParams pagination) instead for better performance and memory management")]
    public async Task<IEnumerable<UserDto>> GetAllUsersAsync()
    {
        logger.LogInformation("UserService.GetAllUsersAsync called (non-paginated - deprecated)");

        var users = await unitOfWork.Users.GetAllAsync();

        logger.LogInformation("UserService.GetAllUsersAsync completed, returning {Count} users", users.Count());

        return mapper.Map<IEnumerable<UserDto>>(users);
    }

    /// <inheritdoc />
    public async Task<PagedResult<UserDto>> GetAllUsersAsync(PaginationParams pagination)
    {
        logger.LogInformation("UserService.GetAllUsersAsync (paginated) called - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var (items, totalCount) = await unitOfWork.Users.GetPagedAsync(
            pagination.Skip,
            pagination.PageSize);

        var dtos = mapper.Map<List<UserDto>>(items);

        logger.LogInformation("UserService.GetAllUsersAsync (paginated) completed - Returned {Count} of {TotalCount}",
            dtos.Count, totalCount);

        return new PagedResult<UserDto>(dtos, totalCount, pagination.PageNumber, pagination.PageSize);
    }

    /// <inheritdoc />
    public async Task<UserDto?> GetUserByIdAsync(int id)
    {
        logger.LogInformation("UserService.GetUserByIdAsync called for UserId {Id}", id);

        var user = await unitOfWork.Users.GetByIdAsync(id);

        if (user == null)
        {
            logger.LogWarning("UserService.GetUserByIdAsync - User {Id} not found", id);
            return null;
        }

        logger.LogInformation("UserService.GetUserByIdAsync completed for UserId {Id}", id);

        return mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetEsnMembersAsync()
    {
        logger.LogInformation("UserService.GetEsnMembersAsync called");

        var esnMembers = await unitOfWork.Users.GetEsnMembersAsync();

        logger.LogInformation("UserService.GetEsnMembersAsync completed, found {Count} ESN members", esnMembers.Count());

        return mapper.Map<IEnumerable<UserDto>>(esnMembers);
    }

    /// <inheritdoc />
    public async Task<UserDto> CreateUserAsync(UserCreateDto createDto)
    {
        logger.LogInformation("UserService.CreateUserAsync called for {Email}", createDto.Email);

        // Per-email registration rate limit: max 2 attempts per hour per email
        var emailKey = $"reg:email:{createDto.Email.ToLowerInvariant()}";
        var attempts = memoryCache.GetOrCreate(emailKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
            return 0;
        });
        if (attempts >= 2)
        {
            logger.LogWarning("UserService.CreateUserAsync - Too many registration attempts for {Email}", createDto.Email);
            throw new InvalidOperationException("Too many registration attempts for this email. Please try again later.");
        }
        memoryCache.Set(emailKey, attempts + 1, TimeSpan.FromHours(1));

        var existingUser = await unitOfWork.Users.GetByEmailAsync(createDto.Email);

        if (existingUser != null)
        {
            logger.LogWarning("UserService.CreateUserAsync - Duplicate email: {Email}", createDto.Email);
            throw new InvalidOperationException($"A user with email {createDto.Email} already exists.");
        }

        var user = mapper.Map<Bo.Models.UserBo>(createDto);

        var hasher = new PasswordHasher<Bo.Models.UserBo>();
        user.PasswordHash = hasher.HashPassword(user, createDto.Password);

        await unitOfWork.Users.AddAsync(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("UserService.CreateUserAsync completed for {Email}", user.Email);

        return mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDto?> UpdatePasswordAsync(int id, UserPasswordChangeDto passwordDto)
    {
        logger.LogInformation("UserService.UpdatePasswordAsync called for UserId {Id}", id);

        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            logger.LogWarning("UserService.UpdatePasswordAsync - User {Id} not found", id);
            return null;
        }

        var hasher = new PasswordHasher<Bo.Models.UserBo>();

        // Verify the old password
        var verificationResult = hasher.VerifyHashedPassword(user, user.PasswordHash, passwordDto.OldPassword);
        if (verificationResult == PasswordVerificationResult.Failed)
        {
            logger.LogWarning("UserService.UpdatePasswordAsync - Incorrect old password for UserId {Id}", id);
            throw new UnauthorizedAccessException("Current password is incorrect");
        }

        // Hash and set the new password
        var hashed = hasher.HashPassword(user, passwordDto.NewPassword);
        user.PasswordHash = hashed;

        try
        {
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("UserService.UpdatePasswordAsync - Password updated successfully for UserId {Id}", id);
        }
        catch (Exception ex)
        {
            if (!await unitOfWork.Users.AnyAsync(e => e.Id == id))
            {
                logger.LogWarning("UserService.UpdatePasswordAsync - Concurrency failure for UserId {Id}, record not found", id);
                return null;
            }
            logger.LogError(ex, "UserService.UpdatePasswordAsync - Concurrency exception for UserId {Id}", id);
            throw;
        }

        logger.LogInformation("UserService.UpdatePasswordAsync completed for {Email}", user.Email);

        return mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDto?> UpdateUserAsync(int id, UserUpdateDto userDto)
    {
        logger.LogInformation("UserService.UpdateUserAsync called for UserId {Id} with {Email}", id, userDto.Email);

        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            logger.LogWarning("UserService.UpdateUserAsync - User {Id} not found", id);
            return null;
        }

        mapper.Map(userDto, user);

        try
        {
            unitOfWork.Users.Update(user);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("UserService.UpdateUserAsync - User {Id} updated successfully", id);
        }
        catch (Exception ex)
        {
            if (!await unitOfWork.Users.AnyAsync(e => e.Id == id))
            {
                logger.LogWarning("UserService.UpdateUserAsync - Concurrency failure for UserId {Id}, record not found", id);
                return null;
            }
            logger.LogError(ex, "UserService.UpdateUserAsync - Concurrency exception for UserId {Id}", id);
            throw;
        }

        logger.LogInformation("UserService.UpdateUserAsync completed for {Email}", user.Email);

        return mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDto?> DeleteUserAsync(int id)
    {
        logger.LogInformation("UserService.DeleteUserAsync called for UserId {Id}", id);

        var user = await unitOfWork.Users.GetByIdAsync(id);
        if (user == null)
        {
            logger.LogWarning("UserService.DeleteUserAsync - User {Id} not found", id);
            return null;
        }

        unitOfWork.Users.Delete(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("UserService.DeleteUserAsync completed for {Email}", user.Email);

        return mapper.Map<UserDto>(user);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<UserDto>> GetUsersByStatusAsync(UserStatus status)
    {
        logger.LogInformation("UserService.GetUsersByStatusAsync called for Status {Status}", status);

        var users = await unitOfWork.Users.GetAllAsync();
        var filteredUsers = users.Where(u => u.Status == status);

        logger.LogInformation("UserService.GetUsersByStatusAsync found {Count} users with status {Status}",
            filteredUsers.Count(), status);

        return mapper.Map<IEnumerable<UserDto>>(filteredUsers);
    }

    /// <inheritdoc />
    public async Task ApproveUserAsync(int userId)
    {
        logger.LogInformation("UserService.ApproveUserAsync called for UserId {UserId}", userId);

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("UserService.ApproveUserAsync - User {UserId} not found", userId);
            throw new KeyNotFoundException($"User {userId} not found");
        }

        user.Status = UserStatus.Approved;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("UserService.ApproveUserAsync completed - User {UserId} approved", userId);
    }

    /// <inheritdoc />
    public async Task RejectUserAsync(int userId, string? reason = null)
    {
        logger.LogInformation("UserService.RejectUserAsync called for UserId {UserId} with reason: {Reason}",
            userId, reason ?? "No reason provided");

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("UserService.RejectUserAsync - User {UserId} not found", userId);
            throw new KeyNotFoundException($"User {userId} not found");
        }

        user.Status = UserStatus.Rejected;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("UserService.RejectUserAsync completed - User {UserId} rejected. Reason: {Reason}",
            userId, reason ?? "No reason provided");
    }

    /// <inheritdoc />
    public async Task RevokeUserAsync(int userId)
    {
        logger.LogInformation("UserService.RevokeUserAsync called for UserId {UserId}", userId);

        var user = await unitOfWork.Users.GetByIdAsync(userId);
        if (user == null)
        {
            logger.LogWarning("UserService.RevokeUserAsync - User {UserId} not found", userId);
            throw new KeyNotFoundException($"User {userId} not found");
        }

        user.Status = UserStatus.Pending;
        unitOfWork.Users.Update(user);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("UserService.RevokeUserAsync completed - User {UserId} status set to Pending", userId);
    }

    /// <summary>
    /// Returns the lockout end time based on the number of failed attempts.
    /// Implements exponential backoff: 3 fails = 5min, 4 = 15min, 5+ = 1 hour.
    /// </summary>
    private static DateTime? GetLockoutDuration(int failedAttempts)
    {
        return failedAttempts switch
        {
            >= 5 => DateTime.UtcNow.AddHours(1),
            4 => DateTime.UtcNow.AddMinutes(15),
            3 => DateTime.UtcNow.AddMinutes(5),
            _ => null
        };
    }

}
