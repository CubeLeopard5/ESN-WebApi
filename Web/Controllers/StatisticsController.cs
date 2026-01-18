using Business.Interfaces;
using Dto.Statistics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Web.Extensions;
using Web.Middlewares;

namespace Web.Controllers;

/// <summary>
/// Controller for statistics dashboard endpoints
/// </summary>
/// <remarks>
/// All endpoints require Admin role or ESN Member status
/// </remarks>
[Route("api/statistics")]
[ApiController]
[Authorize]
[ServiceFilter(typeof(RequestLoggingActionFilter))]
public class StatisticsController(
    IStatisticsService statisticsService,
    ILogger<StatisticsController> logger) : ControllerBase
{
    /// <summary>
    /// Get global statistics (totals and averages)
    /// </summary>
    /// <returns>Global statistics including total events, users, registrations and average attendance rate</returns>
    /// <response code="200">Global statistics returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("global")]
    [ProducesResponseType(typeof(GlobalStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<GlobalStatsDto>> GetGlobalStats()
    {
        logger.LogInformation("GetGlobalStats request received");

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var stats = await statisticsService.GetGlobalStatsAsync();

            logger.LogInformation("GetGlobalStats successful - Events: {Events}, Users: {Users}, Registrations: {Registrations}",
                stats.TotalEvents, stats.TotalUsers, stats.TotalRegistrations);

            return Ok(stats);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetGlobalStats - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get events created over time (monthly breakdown)
    /// </summary>
    /// <param name="months">Number of months to include (default: 12)</param>
    /// <returns>Events per month data</returns>
    /// <response code="200">Events over time data returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("events-over-time")]
    [ProducesResponseType(typeof(EventsOverTimeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<EventsOverTimeDto>> GetEventsOverTime([FromQuery] int months = 12)
    {
        logger.LogInformation("GetEventsOverTime request received for {Months} months", months);

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var data = await statisticsService.GetEventsOverTimeAsync(months);

            logger.LogInformation("GetEventsOverTime successful - {Total} events in period", data.TotalInPeriod);

            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetEventsOverTime - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get registration trend over time (monthly breakdown)
    /// </summary>
    /// <param name="months">Number of months to include (default: 12)</param>
    /// <returns>Registrations per month data</returns>
    /// <response code="200">Registration trend data returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("registration-trend")]
    [ProducesResponseType(typeof(RegistrationTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<RegistrationTrendDto>> GetRegistrationTrend([FromQuery] int months = 12)
    {
        logger.LogInformation("GetRegistrationTrend request received for {Months} months", months);

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var data = await statisticsService.GetRegistrationTrendAsync(months);

            logger.LogInformation("GetRegistrationTrend successful - {Total} registrations in period", data.TotalInPeriod);

            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetRegistrationTrend - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get attendance status breakdown (present, absent, excused, not validated)
    /// </summary>
    /// <returns>Attendance breakdown with counts and percentages</returns>
    /// <response code="200">Attendance breakdown data returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("attendance-breakdown")]
    [ProducesResponseType(typeof(AttendanceBreakdownDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<AttendanceBreakdownDto>> GetAttendanceBreakdown()
    {
        logger.LogInformation("GetAttendanceBreakdown request received");

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var data = await statisticsService.GetAttendanceBreakdownAsync();

            logger.LogInformation("GetAttendanceBreakdown successful - Present: {Present}, Absent: {Absent}, Excused: {Excused}",
                data.PresentCount, data.AbsentCount, data.ExcusedCount);

            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetAttendanceBreakdown - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get participation rate trend over time (registered vs attended monthly)
    /// </summary>
    /// <param name="months">Number of months to include (default: 12)</param>
    /// <returns>Participation rate per month data</returns>
    /// <response code="200">Participation trend data returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("participation-trend")]
    [ProducesResponseType(typeof(ParticipationRateTrendDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ParticipationRateTrendDto>> GetParticipationTrend([FromQuery] int months = 12)
    {
        logger.LogInformation("GetParticipationTrend request received for {Months} months", months);

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var data = await statisticsService.GetParticipationTrendAsync(months);

            logger.LogInformation("GetParticipationTrend successful - Average rate: {Rate}%", data.AverageRate);

            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetParticipationTrend - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get top events by registration count
    /// </summary>
    /// <param name="count">Number of events to return (default: 10)</param>
    /// <returns>Top events sorted by registration count</returns>
    /// <response code="200">Top events data returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("top-events")]
    [ProducesResponseType(typeof(List<TopEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<List<TopEventDto>>> GetTopEvents([FromQuery] int count = 10)
    {
        logger.LogInformation("GetTopEvents request received for {Count} events", count);

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var data = await statisticsService.GetTopEventsAsync(count);

            logger.LogInformation("GetTopEvents successful - Returned {Count} events", data.Count);

            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetTopEvents - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get complete dashboard statistics in a single call
    /// </summary>
    /// <param name="months">Number of months for time series data (default: 12)</param>
    /// <param name="topEventsCount">Number of top events to include (default: 10)</param>
    /// <returns>All dashboard statistics aggregated</returns>
    /// <response code="200">Dashboard statistics returned successfully</response>
    /// <response code="401">User not authenticated</response>
    /// <response code="403">User is not an administrator or ESN member</response>
    [HttpGet("dashboard")]
    [ProducesResponseType(typeof(DashboardStatsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DashboardStatsDto>> GetDashboardStats(
        [FromQuery] int months = 12,
        [FromQuery] int topEventsCount = 10)
    {
        logger.LogInformation("GetDashboardStats request received for {Months} months, top {Count} events", months, topEventsCount);

        try
        {
            var email = User.GetUserEmailOrThrow();
            await statisticsService.VerifyAccessAsync(email);

            var data = await statisticsService.GetDashboardStatsAsync(months, topEventsCount);

            logger.LogInformation("GetDashboardStats successful");

            return Ok(data);
        }
        catch (UnauthorizedAccessException ex)
        {
            logger.LogWarning(ex, "GetDashboardStats - Forbidden");
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }
}
