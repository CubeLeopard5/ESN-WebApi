using Business.Exceptions;
using Business.Interfaces;
using Dto.Passkey;
using Dto.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Web.Extensions;
using Web.Middlewares;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class PasskeysController(IPasskeyService passkeyService, IUserService userService, ILogger<PasskeysController> logger)
    : ControllerBase
{
    /// <summary>
    /// Initie l'enregistrement d'une nouvelle passkey
    /// </summary>
    [Authorize]
    [HttpPost("register/begin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> BeginRegistration()
    {
        logger.LogInformation("BeginRegistration request received");

        try
        {
            var userId = GetCurrentUserId();
            var (challengeId, options) = await passkeyService.BeginRegistrationAsync(userId);

            return Ok(new { challengeId, options });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "BeginRegistration - User not found");
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Complète l'enregistrement d'une passkey
    /// </summary>
    [Authorize]
    [HttpPost("register/complete")]
    [ProducesResponseType(typeof(PasskeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PasskeyDto>> CompleteRegistration(PasskeyRegistrationCompleteDto dto)
    {
        logger.LogInformation("CompleteRegistration request received");

        try
        {
            var userId = GetCurrentUserId();
            var passkey = await passkeyService.CompleteRegistrationAsync(userId, dto);

            return Ok(passkey);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "CompleteRegistration - Challenge or user not found");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is Fido2NetLib.Fido2VerificationException or ArgumentException)
        {
            logger.LogWarning(ex, "CompleteRegistration - Verification failed");
            return BadRequest(new { message = "Passkey registration failed" });
        }
    }

    /// <summary>
    /// Initie le login par passkey
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login/begin")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> BeginLogin(PasskeyLoginBeginDto dto)
    {
        logger.LogInformation("BeginLogin request received");

        var (challengeId, options) = await passkeyService.BeginLoginAsync(dto);
        return Ok(new { challengeId, options });
    }

    /// <summary>
    /// Complète le login par passkey
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login/complete")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserLoginResponseDto>> CompleteLogin(PasskeyLoginCompleteDto dto)
    {
        logger.LogInformation("CompleteLogin request received");

        try
        {
            var response = await passkeyService.CompleteLoginAsync(dto);
            return Ok(response);
        }
        catch (ForbiddenAccessException ex)
        {
            logger.LogWarning(ex, "CompleteLogin - Account not approved");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "CompleteLogin - Challenge or credential not found");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex) when (ex is Fido2NetLib.Fido2VerificationException or ArgumentException)
        {
            logger.LogWarning(ex, "CompleteLogin - Verification failed");
            return BadRequest(new { message = "Passkey authentication failed" });
        }
    }

    /// <summary>
    /// Liste les passkeys de l'utilisateur connecté
    /// </summary>
    [Authorize]
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PasskeyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IEnumerable<PasskeyDto>>> GetPasskeys()
    {
        var userId = GetCurrentUserId();
        var passkeys = await passkeyService.GetUserPasskeysAsync(userId);
        return Ok(passkeys);
    }

    /// <summary>
    /// Renomme une passkey
    /// </summary>
    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PasskeyDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PasskeyDto>> UpdatePasskey(int id, UpdatePasskeyDto dto)
    {
        var userId = GetCurrentUserId();
        var passkey = await passkeyService.UpdatePasskeyAsync(id, userId, dto);

        if (passkey == null)
            return NotFound();

        return Ok(passkey);
    }

    /// <summary>
    /// Supprime une passkey
    /// </summary>
    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeletePasskey(int id)
    {
        var userId = GetCurrentUserId();
        var deleted = await passkeyService.DeletePasskeyAsync(id, userId);

        if (!deleted)
            return NotFound();

        return NoContent();
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("User ID not found in claims");
        return userId;
    }
}
