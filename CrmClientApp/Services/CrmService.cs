using System.Text;

namespace CrmClientApp.Services;

/// <summary>
/// Service implementation for retrieving client data from an external CRM server.
/// Handles OAuth token retrieval and HTTP requests to the CRM API.
/// </summary>
public class CrmService : ICrmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrmService> _logger;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="CrmService"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client configured with the CRM server base URL.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="tokenService">The token service for retrieving OAuth tokens.</param>
    /// <param name="configuration">The configuration instance for accessing app settings.</param>
    public CrmService(
        HttpClient httpClient, 
        ILogger<CrmService> logger,
        ITokenService tokenService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _tokenService = tokenService;
        _configuration = configuration;
    }

    /// <summary>
    /// Retrieves client data from the external CRM server for the specified client ID.
    /// Automatically retrieves and includes an OAuth token in the request headers.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client to retrieve data for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the client data as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request to the CRM server fails or returns a non-success status code.</exception>
    public async Task<string> GetClientDataAsync(string clientId)
    {
        try
        {
            _logger.LogInformation("Calling GetClientData for client ID: {ClientId}", clientId);
            
            // Get OAuth token dynamically
            var token = await _tokenService.GetTokenAsync();
            
            // Get token header name from config (default to Authorization)
            var tokenHeaderName = _configuration["ExternalApi:Token:HeaderName"] ?? "Authorization";
            var tokenHeaderFormat = _configuration["ExternalApi:Token:HeaderFormat"] ?? "Bearer {0}";
            
            // Create request message
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/GetClientData?id={clientId}");
            
            // Add token to header
            var tokenHeaderValue = string.Format(tokenHeaderFormat, token);
            request.Headers.Add(tokenHeaderName, tokenHeaderValue);
            
            var response = await _httpClient.SendAsync(request);
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully retrieved client data for ID: {ClientId}", clientId);
            
            return content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling CRM server for client ID: {ClientId}", clientId);
            throw;
        }
    }
}
