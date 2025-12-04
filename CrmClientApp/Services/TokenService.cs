using System.Text;
using System.Text.Json;

namespace CrmClientApp.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private CachedToken? _cachedToken;

    public TokenService(
        IConfiguration configuration, 
        ILogger<TokenService> logger,
        IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();
        
        // Read client ID and secret from environment variables
        _clientId = Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID") 
            ?? throw new InvalidOperationException("OAUTH_CLIENT_ID environment variable is required");
        _clientSecret = Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET") 
            ?? throw new InvalidOperationException("OAUTH_CLIENT_SECRET environment variable is required");
    }

    public async Task<string> GetTokenAsync()
    {
        // Check if we have a valid cached token
        if (_cachedToken != null && _cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
        {
            _logger.LogDebug("Using cached OAuth token");
            return _cachedToken.Token;
        }

        // Use semaphore to ensure only one token request at a time
        await _semaphore.WaitAsync();
        try
        {
            // Double-check after acquiring lock (another thread might have refreshed it)
            if (_cachedToken != null && _cachedToken.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogDebug("Using cached OAuth token (after lock)");
                return _cachedToken.Token;
            }

            // Fetch new token from token server
            return await FetchTokenFromServerAsync();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    private async Task<string> FetchTokenFromServerAsync()
    {
        try
        {
            var tokenEndpoint = _configuration["ExternalApi:Token:Endpoint"] 
                ?? throw new InvalidOperationException("ExternalApi:Token:Endpoint configuration is required");
            var scope = _configuration["ExternalApi:Token:Scope"] ?? "";
            var grantType = _configuration["ExternalApi:Token:GrantType"] ?? "client_credentials";

            _logger.LogInformation("Fetching OAuth token from token server");

            // Prepare OAuth token request
            var requestBody = new List<KeyValuePair<string, string>>
            {
                new("grant_type", grantType),
                new("client_id", _clientId),
                new("client_secret", _clientSecret)
            };

            if (!string.IsNullOrWhiteSpace(scope))
            {
                requestBody.Add(new KeyValuePair<string, string>("scope", scope));
            }

            var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
            {
                Content = new FormUrlEncodedContent(requestBody)
            };

            // Add basic auth header if configured
            var useBasicAuth = _configuration.GetValue<bool>("ExternalApi:Token:UseBasicAuth", false);
            if (useBasicAuth)
            {
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);
            }

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (tokenResponse == null || string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
            {
                throw new InvalidOperationException("Invalid token response from token server");
            }

            // Cache the token
            var expiresIn = tokenResponse.ExpiresIn ?? 3600; // Default to 1 hour if not specified
            _cachedToken = new CachedToken
            {
                Token = tokenResponse.AccessToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn - 60) // Refresh 1 minute before expiry
            };

            _logger.LogInformation("Successfully obtained OAuth token, expires in {ExpiresIn} seconds", expiresIn);
            return _cachedToken.Token;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error fetching OAuth token from token server");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching OAuth token");
            throw;
        }
    }

    private class CachedToken
    {
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }

    private class TokenResponse
    {
        public string? AccessToken { get; set; }
        public string? TokenType { get; set; }
        public int? ExpiresIn { get; set; }
        public string? Scope { get; set; }
    }
}
