using System.Text;

namespace CrmClientApp.Services;

public class CrmService : ICrmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrmService> _logger;
    private readonly ITokenService _tokenService;
    private readonly IConfiguration _configuration;

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

    public async Task<string> GetClientDataAsync(string clientId)
    {
        try
        {
            _logger.LogInformation("Calling GetClientData for client ID: {ClientId}", clientId);
            
            // Generate token dynamically
            var token = _tokenService.GenerateToken();
            
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
