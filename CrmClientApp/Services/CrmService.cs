using System.Text;

namespace CrmClientApp.Services;

public class CrmService : ICrmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CrmService> _logger;

    public CrmService(HttpClient httpClient, ILogger<CrmService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetClientDataAsync(string clientId)
    {
        try
        {
            _logger.LogInformation("Calling GetClientData for client ID: {ClientId}", clientId);
            
            // Assuming the endpoint is /api/GetClientData?id={clientId}
            // Adjust the endpoint path based on the actual CRM server API structure
            var response = await _httpClient.GetAsync($"/api/GetClientData?id={clientId}");
            
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
