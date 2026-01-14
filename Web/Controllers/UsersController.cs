using Bo.Enums;
using Business.Exceptions;
using Dto.Common;
using Dto.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using Web.Extensions;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class UsersController(Business.Interfaces.IUserService userService, ILogger<UsersController> logger) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<string>> Login(UserLoginDto loginDto)
    {
        logger.LogInformation("Login request received for {Email}", loginDto.Email);

        try
        {
            var response = await userService.LoginAsync(loginDto);

            logger.LogInformation("Login successful for {Email}", loginDto.Email);

            return Ok(response);
        }
        catch (ForbiddenAccessException ex)
        {
            logger.LogWarning(ex, "Login - Account not approved");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Login - Invalid credentials");
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(UserLoginResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UserLoginResponseDto>> RefreshToken()
    {
        logger.LogInformation("Token refresh request received");

        try
        {
            // Get the current token from the Authorization header
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                logger.LogWarning("Token refresh - No valid authorization header");
                return Unauthorized("No token provided");
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();

            var response = await userService.RefreshTokenAsync(token);

            logger.LogInformation("Token refresh successful");

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "Token refresh - Unauthorized");
            return Unauthorized(ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "Token refresh - User not found");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token refresh failed with exception");
            return StatusCode(500, "An error occurred while refreshing the token");
        }
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        logger.LogInformation("GetCurrentUser request received");

        try
        {
            var email = User.GetUserEmailOrThrow();
            var userDto = await userService.GetCurrentUserAsync(email);

            if (userDto == null)
            {
                logger.LogInformation("GetCurrentUser - User not found");
                return NotFound();
            }

            logger.LogInformation("GetCurrentUser successful for {Email}", email);

            return Ok(userDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetCurrentUser - Unable to extract email from claims");
            return Unauthorized(ex.Message);
        }
    }

    [HttpGet]
    [Authorize] // Protéger l'accès à la liste des utilisateurs
    [ProducesResponseType(typeof(PagedResult<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<UserDto>>> GetUsers([FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetUsers request received - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        var users = await userService.GetAllUsersAsync(pagination);

        logger.LogInformation("GetUsers successful - Returned {Count} of {TotalCount} users",
            users.Items.Count, users.TotalCount);

        return Ok(users);
    }

    [Authorize]
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        logger.LogInformation("GetUser request received for {Id}", id);

        var user = await userService.GetUserByIdAsync(id);

        if (user == null)
        {
            logger.LogInformation("GetUser - User {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetUser successful for {Id}", id);

        return Ok(user);
    }

    [Authorize]
    [HttpGet("esn-members")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetEsnMembers()
    {
        logger.LogInformation("GetEsnMembers request received");

        var esnMembers = await userService.GetEsnMembersAsync();

        logger.LogInformation("GetEsnMembers successful");

        return Ok(esnMembers);
    }

    [HttpPost]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDto>> PostUser(UserCreateDto createDto)
    {
        logger.LogInformation("PostUser request received for {Email}", createDto.Email);

        try
        {
            var response = await userService.CreateUserAsync(createDto);

            logger.LogInformation("PostUser successful for {Email}", createDto.Email);

            return CreatedAtAction(nameof(GetUser), new { id = response.Id }, response);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "PostUser - Duplicate email: {Email}", createDto.Email);
            return Conflict(new { message = "Unable to create user. Please check your information." });
        }
    }

    [Authorize]
    [HttpPut("Password/{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserDto>> PutPasswordUser(int id, UserPasswordChangeDto passwordDto)
    {
        logger.LogInformation("PutPasswordUser request received for {Id}", id);

        try
        {
            var userDto = await userService.UpdatePasswordAsync(id, passwordDto);

            if (userDto == null)
            {
                logger.LogInformation("PutPasswordUser - User {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("PutPasswordUser successful for {Id}", id);

            return Ok(userDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PutPasswordUser - Incorrect old password for {Id}", id);
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<UserDto>> PutUser(int id, UserUpdateDto userDto)
    {
        logger.LogInformation("PutUser request received for {Id} with {Email}", id, userDto.Email);

        try
        {
            // Vérifier que l'utilisateur modifie son propre profil OU a le rôle Admin
            var currentUserEmail = User.GetUserEmailOrThrow();
            var currentUser = await userService.GetCurrentUserAsync(currentUserEmail);

            if (currentUser != null && currentUser.Id != id && !User.IsInRole("Admin"))
            {
                logger.LogWarning("PutUser - User {CurrentUserId} attempted to modify user {TargetUserId} without Admin role",
                    currentUser.Id, id);
                return Forbid();
            }

            var responseDto = await userService.UpdateUserAsync(id, userDto);

            if (responseDto == null)
            {
                logger.LogInformation("PutUser - User {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("PutUser successful for {Email}", userDto.Email);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PutUser - Unable to extract email from claims");
            return Unauthorized(ex.Message);
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        logger.LogInformation("DeleteUser request received for {Id}", id);

        var user = await userService.DeleteUserAsync(id);

        if (user == null)
        {
            logger.LogInformation("DeleteUser - User {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("DeleteUser successful for {Id}", id);

        return NoContent();
    }

    /// <summary>
    /// Récupère les utilisateurs en attente de validation
    /// </summary>
    /// <returns>Liste des utilisateurs avec statut Pending</returns>
    /// <response code="200">Liste des utilisateurs en attente</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Pas de rôle Admin</response>
    [Authorize(Roles = "Admin")]
    [HttpGet("pending")]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<IEnumerable<UserDto>>> GetPendingUsers()
    {
        logger.LogInformation("GetPendingUsers called");

        var users = await userService.GetUsersByStatusAsync(UserStatus.Pending);

        logger.LogInformation("GetPendingUsers found {Count} pending users", users.Count());

        return Ok(users);
    }

    /// <summary>
    /// Approuve un utilisateur
    /// </summary>
    /// <param name="id">ID de l'utilisateur à approuver</param>
    /// <response code="204">Utilisateur approuvé avec succès</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Pas de rôle Admin</response>
    /// <response code="404">Utilisateur non trouvé</response>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/approve")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveUser(int id)
    {
        logger.LogInformation("ApproveUser called for UserId {UserId}", id);

        try
        {
            await userService.ApproveUserAsync(id);

            logger.LogInformation("ApproveUser completed - User {UserId} approved", id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "ApproveUser - User {UserId} not found", id);
            return NotFound(new { message = $"User {id} not found" });
        }
    }

    /// <summary>
    /// Refuse un utilisateur
    /// </summary>
    /// <param name="id">ID de l'utilisateur à refuser</param>
    /// <param name="dto">Raison du refus (optionnel)</param>
    /// <response code="204">Utilisateur refusé avec succès</response>
    /// <response code="400">Données invalides</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Pas de rôle Admin</response>
    /// <response code="404">Utilisateur non trouvé</response>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/reject")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectUser(int id, [FromBody] RejectUserDto dto)
    {
        logger.LogInformation("RejectUser called for UserId {UserId} with reason: {Reason}",
            id, dto.Reason ?? "No reason provided");

        if (!ModelState.IsValid)
        {
            logger.LogWarning("RejectUser - Invalid model state");
            return BadRequest(ModelState);
        }

        try
        {
            await userService.RejectUserAsync(id, dto.Reason);

            logger.LogInformation("RejectUser completed - User {UserId} rejected", id);

            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            logger.LogWarning(ex, "RejectUser - User {UserId} not found", id);
            return NotFound(new { message = $"User {id} not found" });
        }
    }
}
