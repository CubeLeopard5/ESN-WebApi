using Business.Interfaces;
using Dto;
using Dto.Common;
using Dto.Proposition;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;
using Web.Middlewares;

namespace Web.Controllers;

/// <summary>
/// Controller d'administration pour la gestion des propositions
/// </summary>
/// <remarks>
/// Endpoints réservés aux membres ESN et administrateurs
/// </remarks>
[Route("api/admin/propositions")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class PropositionAdminController(
    IPropositionService propositionService,
    ILogger<PropositionAdminController> logger) : ControllerBase
{
    /// <summary>
    /// Liste toutes les propositions avec filtrage par statut de suppression
    /// </summary>
    /// <param name="pagination">Paramètres de pagination</param>
    /// <param name="filter">Filtre sur le statut de suppression (Active, Deleted, All)</param>
    /// <returns>Résultat paginé contenant les propositions filtrées</returns>
    /// <response code="200">Liste des propositions avec métadonnées de pagination</response>
    /// <response code="401">Utilisateur non authentifié</response>
    /// <response code="403">Utilisateur non autorisé (ni membre ESN ni administrateur)</response>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<PropositionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<PagedResult<PropositionDto>>> GetAllPropositions(
        [FromQuery] PaginationParams pagination,
        [FromQuery] PropositionFilterDto filter)
    {
        logger.LogInformation(
            "GetAllPropositions (Admin) request received - Page {PageNumber}, Size {PageSize}, Filter {Status}",
            pagination.PageNumber, pagination.PageSize, filter.Status);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var propositions = await propositionService.GetAllPropositionsForAdminAsync(pagination, filter, email);

            logger.LogInformation("GetAllPropositions (Admin) successful - Returned {Count} of {TotalCount} propositions",
                propositions.Items.Count, propositions.TotalCount);

            return Ok(propositions);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetAllPropositions (Admin) - Unauthorized access attempt by {Email}",
                User.Identity?.Name ?? "Unknown");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Supprime une proposition (soft delete) en tant qu'administrateur/ESN member
    /// </summary>
    /// <param name="id">Identifiant de la proposition à supprimer</param>
    /// <returns>Pas de contenu si succès</returns>
    /// <response code="204">Proposition supprimée avec succès</response>
    /// <response code="401">Utilisateur non authentifié</response>
    /// <response code="403">Utilisateur non autorisé (ni membre ESN ni administrateur)</response>
    /// <response code="404">Proposition non trouvée</response>
    /// <remarks>
    /// Accessible uniquement aux membres ESN (StudentType = "esn_member") et administrateurs.
    /// Effectue un soft delete : la proposition est marquée comme supprimée mais reste en base.
    /// </remarks>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProposition(int id)
    {
        logger.LogInformation("DeleteProposition (Admin) request received for {Id}", id);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var proposition = await propositionService.DeletePropositionAsAdminAsync(id, email);

            if (proposition == null)
            {
                logger.LogInformation("DeleteProposition (Admin) - Proposition {Id} not found", id);
                return NotFound(new { message = $"Proposition with ID {id} not found" });
            }

            logger.LogInformation("DeleteProposition (Admin) successful for {Id} by {Email}", id, email);

            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "DeleteProposition (Admin) - Unauthorized access attempt for {Id} by {Email}",
                id, User.Identity?.Name ?? "Unknown");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
