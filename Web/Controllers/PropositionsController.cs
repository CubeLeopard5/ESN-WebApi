using Dto;
using Dto.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Middlewares;
using Microsoft.AspNetCore.RateLimiting;
using Web.Extensions;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class PropositionsController(Business.Interfaces.IPropositionService propositionService, ILogger<PropositionsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PropositionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<PropositionDto>>> GetPropositions([FromQuery] PaginationParams pagination)
    {
        logger.LogInformation("GetPropositions request received - Page {PageNumber}, Size {PageSize}",
            pagination.PageNumber, pagination.PageSize);

        // Get user email if authenticated
        string? userEmail = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                userEmail = User.GetUserEmailOrThrow();
            }
            catch
            {
                // User not authenticated properly, continue without email
            }
        }

        var propositions = await propositionService.GetAllPropositionsAsync(pagination, userEmail);

        logger.LogInformation("GetPropositions successful - Returned {Count} of {TotalCount} propositions",
            propositions.Items.Count, propositions.TotalCount);

        return Ok(propositions);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(PropositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PropositionDto>> GetProposition(int id)
    {
        logger.LogInformation("GetProposition request received for {Id}", id);

        // Get user email if authenticated
        string? userEmail = null;
        if (User.Identity?.IsAuthenticated == true)
        {
            try
            {
                userEmail = User.GetUserEmailOrThrow();
            }
            catch
            {
                // User not authenticated properly, continue without email
            }
        }

        var proposition = await propositionService.GetPropositionByIdAsync(id, userEmail);

        if (proposition == null)
        {
            logger.LogInformation("GetProposition - Proposition {Id} not found", id);
            return NotFound();
        }

        logger.LogInformation("GetProposition successful for {Id}", id);

        return Ok(proposition);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(PropositionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PropositionDto>> PostProposition(PropositionDto propositionDto)
    {
        logger.LogInformation("PostProposition request received with Title {Title}", propositionDto.Title);

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var email = User.GetUserEmailOrThrow();
            var responseDto = await propositionService.CreatePropositionAsync(propositionDto, email);

            logger.LogInformation("PostProposition successful for {Title}", responseDto.Title);

            return CreatedAtAction(nameof(GetProposition), new { id = responseDto.Id }, responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PostProposition - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(PropositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PropositionDto>> PutProposition(int id, PropositionDto propositionDto)
    {
        logger.LogInformation("PutProposition request received for {Id} with Title {Title}", id, propositionDto.Title);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var responseDto = await propositionService.UpdatePropositionAsync(id, propositionDto, email);

            if (responseDto == null)
            {
                logger.LogInformation("PutProposition - Proposition {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("PutProposition successful for {Id}", id);

            return Ok(responseDto);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "PutProposition - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> DeleteProposition(int id)
    {
        logger.LogInformation("DeleteProposition request received for {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var proposition = await propositionService.DeletePropositionAsync(id, email);

            if (proposition == null)
            {
                logger.LogInformation("DeleteProposition - Proposition {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("DeleteProposition successful for {Id}", id);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "DeleteProposition - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{id}/vote-up")]
    [EnableRateLimiting("voting")]
    [ProducesResponseType(typeof(PropositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PropositionDto>> VoteUp(int id)
    {
        logger.LogInformation("VoteUp request received for Proposition {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var proposition = await propositionService.VoteUpAsync(id, email);

            if (proposition == null)
            {
                logger.LogInformation("VoteUp - Proposition {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("VoteUp successful for Proposition {Id}", id);

            return Ok(proposition);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "VoteUp - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }

    [Authorize]
    [HttpPost("{id}/vote-down")]
    [EnableRateLimiting("voting")]
    [ProducesResponseType(typeof(PropositionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PropositionDto>> VoteDown(int id)
    {
        logger.LogInformation("VoteDown request received for Proposition {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var proposition = await propositionService.VoteDownAsync(id, email);

            if (proposition == null)
            {
                logger.LogInformation("VoteDown - Proposition {Id} not found", id);
                return NotFound();
            }

            logger.LogInformation("VoteDown successful for Proposition {Id}", id);

            return Ok(proposition);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "VoteDown - Unauthorized");
            return Unauthorized(ex.Message);
        }
    }
}
