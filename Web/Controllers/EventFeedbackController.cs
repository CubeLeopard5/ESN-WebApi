using Business.Interfaces;
using Dto.EventFeedback;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;

namespace Web.Controllers;

/// <summary>
/// Controller pour la gestion des feedbacks d'événements
/// </summary>
[Route("api/events/{eventId}/feedback")]
[ApiController]
[Authorize]
public class EventFeedbackController : ControllerBase
{
    private readonly IEventFeedbackService _feedbackService;
    private readonly ILogger<EventFeedbackController> _logger;

    public EventFeedbackController(IEventFeedbackService feedbackService, ILogger<EventFeedbackController> logger)
    {
        _feedbackService = feedbackService;
        _logger = logger;
    }

    /// <summary>
    /// Vérifie l'éligibilité de l'utilisateur à soumettre un feedback
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Informations d'éligibilité incluant le formulaire si éligible</returns>
    /// <response code="200">Retourne les informations d'éligibilité</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpGet("eligibility")]
    [ProducesResponseType(typeof(FeedbackEligibilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackEligibilityDto>> CheckEligibility(int eventId)
    {
        _logger.LogInformation("CheckEligibility request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _feedbackService.CheckEligibilityAsync(eventId, email);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Soumet un feedback pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="dto">Données du feedback</param>
    /// <returns>Feedback créé</returns>
    /// <response code="201">Feedback créé avec succès</response>
    /// <response code="400">Utilisateur non éligible ou données invalides</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpPost]
    [ProducesResponseType(typeof(EventFeedbackDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventFeedbackDto>> SubmitFeedback(int eventId, [FromBody] SubmitFeedbackDto dto)
    {
        _logger.LogInformation("SubmitFeedback request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _feedbackService.SubmitFeedbackAsync(eventId, email, dto);
            return CreatedAtAction(nameof(GetMyFeedback), new { eventId }, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Récupère le feedback de l'utilisateur connecté pour un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Feedback de l'utilisateur ou null</returns>
    /// <response code="200">Retourne le feedback</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="404">Événement ou feedback non trouvé</response>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(EventFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventFeedbackDto>> GetMyFeedback(int eventId)
    {
        _logger.LogInformation("GetMyFeedback request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _feedbackService.GetUserFeedbackAsync(eventId, email);

            if (result == null)
                return NotFound("Feedback not found");

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Met à jour le feedback de l'utilisateur connecté
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="dto">Nouvelles données du feedback</param>
    /// <returns>Feedback mis à jour</returns>
    /// <response code="200">Feedback mis à jour avec succès</response>
    /// <response code="400">Deadline passée ou données invalides</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="404">Événement ou feedback non trouvé</response>
    [HttpPut]
    [ProducesResponseType(typeof(EventFeedbackDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventFeedbackDto>> UpdateFeedback(int eventId, [FromBody] SubmitFeedbackDto dto)
    {
        _logger.LogInformation("UpdateFeedback request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _feedbackService.UpdateFeedbackAsync(eventId, email, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Récupère tous les feedbacks d'un événement (Admin/ESN Member)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Liste des feedbacks</returns>
    /// <response code="200">Retourne la liste des feedbacks</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Non autorisé (doit être esn_member ou Admin)</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpGet("all")]
    [ProducesResponseType(typeof(IEnumerable<EventFeedbackDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<EventFeedbackDto>>> GetAllFeedbacks(int eventId)
    {
        _logger.LogInformation("GetAllFeedbacks request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _feedbackService.GetAllFeedbacksAsync(eventId, email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Récupère les statistiques de feedback d'un événement (Admin/ESN Member)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Statistiques agrégées</returns>
    /// <response code="200">Retourne les statistiques</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Non autorisé (doit être esn_member ou Admin)</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(FeedbackSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackSummaryDto>> GetFeedbackSummary(int eventId)
    {
        _logger.LogInformation("GetFeedbackSummary request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _feedbackService.GetFeedbackSummaryAsync(eventId, email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    /// <summary>
    /// Met à jour le formulaire de feedback d'un événement (Admin/ESN Member)
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="dto">Nouveau formulaire et deadline</param>
    /// <returns>NoContent si succès</returns>
    /// <response code="204">Formulaire mis à jour avec succès</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Non autorisé (doit être esn_member ou Admin)</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpPut("form")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateFeedbackForm(int eventId, [FromBody] UpdateFeedbackFormDto dto)
    {
        _logger.LogInformation("UpdateFeedbackForm request for EventId {EventId}", eventId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            await _feedbackService.UpdateFeedbackFormAsync(eventId, email, dto);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
