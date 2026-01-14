using Dal;
using Microsoft.AspNetCore.Mvc;

namespace Web.Controllers;

[Route("api/[controller]")]
[ApiController]
public class HealthController(EsnDevContext context, ILogger<HealthController> logger) : ControllerBase
{
    /// <summary>
    /// Health check endpoint to verify API and database connectivity
    /// </summary>
    /// <returns>Health status information</returns>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<ActionResult<object>> GetHealth()
    {
        logger.LogInformation("Health check requested");

        try
        {
            // Check database connectivity
            var canConnect = await context.Database.CanConnectAsync();

            var health = new
            {
                status = canConnect ? "Healthy" : "Unhealthy",
                checks = new
                {
                    database = canConnect ? "Healthy" : "Unhealthy"
                }
            };

            if (!canConnect)
            {
                logger.LogWarning("Health check failed - database unreachable");
                return StatusCode(StatusCodes.Status503ServiceUnavailable, health);
            }

            logger.LogInformation("Health check passed");
            return Ok(health);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed with exception");

            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                status = "Unhealthy",
                checks = new
                {
                    database = "Unhealthy"
                }
            });
        }
    }
}
