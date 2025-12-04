using CrmClientApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmClientApp.Controllers;

/// <summary>
/// API controller for retrieving client data from the external CRM server.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CrmController : ControllerBase
{
    private readonly ICrmService _crmService;
    private readonly ILogger<CrmController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrmController"/> class.
    /// </summary>
    /// <param name="crmService">The CRM service for retrieving client data.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    public CrmController(ICrmService crmService, ILogger<CrmController> logger)
    {
        _crmService = crmService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves client data from the external CRM server for the specified client ID.
    /// </summary>
    /// <param name="id">The unique identifier of the client to retrieve data for. Required.</param>
    /// <returns>
    /// An IActionResult containing:
    /// - 200 OK with client data if successful
    /// - 400 Bad Request if the client ID is missing or invalid
    /// - 503 Service Unavailable if the CRM server is unreachable
    /// - 500 Internal Server Error for unexpected errors
    /// </returns>
    /// <remarks>
    /// This endpoint requires OAuth authentication. The OAuth token is automatically
    /// retrieved and included in the request to the external CRM server.
    /// </remarks>
    [HttpGet("GetClientData")]
    public async Task<IActionResult> GetClientData([FromQuery] string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(new { error = "Client ID is required" });
        }

        try
        {
            var clientData = await _crmService.GetClientDataAsync(id);
            return Ok(new { data = clientData });
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to retrieve client data for ID: {Id}", id);
            return StatusCode(503, new { error = "Unable to connect to CRM server", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error retrieving client data for ID: {Id}", id);
            return StatusCode(500, new { error = "An error occurred while retrieving client data", message = ex.Message });
        }
    }
}
