using Business.Interfaces;
using Dto.Attendance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;

namespace Web.Controllers;

/// <summary>
/// Controller pour la gestion des présences aux événements
/// </summary>
[Route("api/events/{eventId}/attendance")]
[ApiController]
[Authorize]
public class AttendanceController : ControllerBase
{
    private readonly IAttendanceService _attendanceService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(IAttendanceService attendanceService, ILogger<AttendanceController> logger)
    {
        _attendanceService = attendanceService;
        _logger = logger;
    }

    /// <summary>
    /// Récupère toutes les inscriptions d'un événement avec les statuts de présence
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Événement avec inscriptions et statistiques</returns>
    /// <response code="200">Retourne l'événement avec les présences</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpGet]
    [ProducesResponseType(typeof(EventAttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<EventAttendanceDto>> GetEventAttendance(int eventId)
    {
        _logger.LogInformation("GetEventAttendance request for EventId {EventId}", eventId);

        var result = await _attendanceService.GetEventAttendanceAsync(eventId);
        if (result == null)
            return NotFound($"Event {eventId} not found");

        return Ok(result);
    }

    /// <summary>
    /// Récupère les statistiques de présence d'un événement
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <returns>Statistiques de présence</returns>
    /// <response code="200">Retourne les statistiques</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="404">Événement non trouvé</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(AttendanceStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceStatsDto>> GetAttendanceStats(int eventId)
    {
        _logger.LogInformation("GetAttendanceStats request for EventId {EventId}", eventId);

        var stats = await _attendanceService.GetAttendanceStatsAsync(eventId);
        if (stats == null)
            return NotFound($"Event {eventId} not found");

        return Ok(stats);
    }

    /// <summary>
    /// Valide la présence d'un participant
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="registrationId">ID de l'inscription</param>
    /// <param name="dto">Statut de présence à attribuer</param>
    /// <returns>Inscription mise à jour</returns>
    /// <response code="200">Présence validée avec succès</response>
    /// <response code="400">Données invalides ou inscription non valide</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Non autorisé (doit être esn_member ou Admin)</response>
    /// <response code="404">Inscription non trouvée</response>
    [HttpPut("{registrationId}")]
    [ProducesResponseType(typeof(AttendanceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AttendanceDto>> ValidateAttendance(
        int eventId,
        int registrationId,
        [FromBody] ValidateAttendanceDto dto)
    {
        _logger.LogInformation("ValidateAttendance request for EventId {EventId}, RegistrationId {RegistrationId}",
            eventId, registrationId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _attendanceService.ValidateAttendanceAsync(eventId, registrationId, dto.Status, email);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "ValidateAttendance - Forbidden");
            return Forbid();
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
    /// Valide la présence de plusieurs participants en une fois
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="dto">Liste des validations à effectuer</param>
    /// <returns>Nombre d'inscriptions mises à jour</returns>
    /// <response code="200">Validations effectuées avec succès</response>
    /// <response code="400">Données invalides</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Non autorisé (doit être esn_member ou Admin)</response>
    [HttpPut]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult> BulkValidateAttendance(
        int eventId,
        [FromBody] BulkValidateAttendanceDto dto)
    {
        _logger.LogInformation("BulkValidateAttendance request for EventId {EventId}, {Count} attendances",
            eventId, dto.Attendances.Count);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var count = await _attendanceService.BulkValidateAttendanceAsync(eventId, dto, email);
            return Ok(new { message = $"Successfully validated {count} attendances", count });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }

    /// <summary>
    /// Réinitialise la présence d'un participant
    /// </summary>
    /// <param name="eventId">ID de l'événement</param>
    /// <param name="registrationId">ID de l'inscription</param>
    /// <returns>NoContent si réussi</returns>
    /// <response code="204">Présence réinitialisée</response>
    /// <response code="401">Non authentifié</response>
    /// <response code="403">Non autorisé (doit être esn_member ou Admin)</response>
    /// <response code="404">Inscription non trouvée</response>
    [HttpDelete("{registrationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> ResetAttendance(int eventId, int registrationId)
    {
        _logger.LogInformation("ResetAttendance request for EventId {EventId}, RegistrationId {RegistrationId}",
            eventId, registrationId);

        try
        {
            var email = User.GetUserEmailOrThrow();
            var result = await _attendanceService.ResetAttendanceAsync(eventId, registrationId, email);

            if (!result)
                return NotFound($"Registration {registrationId} not found for event {eventId}");

            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
    }
}
