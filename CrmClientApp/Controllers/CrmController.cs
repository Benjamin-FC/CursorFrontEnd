using CrmClientApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace CrmClientApp.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CrmController : ControllerBase
{
    private readonly ICrmService _crmService;
    private readonly ILogger<CrmController> _logger;

    public CrmController(ICrmService crmService, ILogger<CrmController> logger)
    {
        _crmService = crmService;
        _logger = logger;
    }

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
