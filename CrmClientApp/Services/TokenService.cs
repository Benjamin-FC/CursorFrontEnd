using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CrmClientApp.Services;

/// <summary>
/// Service implementation for retrieving and managing OAuth 2.0 access tokens.
/// Implements token caching with automatic refresh before expiration and thread-safe token retrieval.
/// </summary>
public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    
    private CachedToken? _cachedToken;

    /// <summary>
    /// Initializes a new instance of the <see cref="TokenService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration instance for accessing token server settings.</param>
    /// <param name="logger">The logger instance for logging operations.</param>
    /// <param name="httpClientFactory">The HTTP client factory for creating HTTP clients.</param>
    /// <exception cref="InvalidOperationException">Thrown when required OAuth environment variables are not set.</exception>
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

    /// <summary>
    /// Retrieves a valid OAuth access token. Uses cached token if available and valid,
    /// otherwise fetches a new token from the token server. Thread-safe implementation
    /// ensures only one token request is made at a time.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the OAuth access token as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request to the token server fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token response is invalid or required configuration is missing.</exception>
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

    /// <summary>
    /// Fetches a new OAuth token from the token server using client credentials flow.
    /// Supports both form-encoded and Basic Authentication methods.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the OAuth access token as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request to the token server fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token response is invalid or required configuration is missing.</exception>
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

    /// <summary>
    /// Represents a cached OAuth token with its expiration time.
    /// </summary>
    private class CachedToken
    {
        /// <summary>
        /// Gets or sets the OAuth access token.
        /// </summary>
        public string Token { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the UTC date and time when the token expires.
        /// </summary>
        public DateTime ExpiresAt { get; set; }
    }

    /// <summary>
    /// Represents the response from the OAuth token server.
    /// </summary>
    private class TokenResponse
    {
        /// <summary>
        /// Gets or sets the access token.
        /// </summary>
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
        
        /// <summary>
        /// Gets or sets the token type (typically "Bearer").
        /// </summary>
        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }
        
        /// <summary>
        /// Gets or sets the number of seconds until the token expires.
        /// </summary>
        [JsonPropertyName("expires_in")]
        public int? ExpiresIn { get; set; }
        
        /// <summary>
        /// Gets or sets the OAuth scope associated with the token.
        /// </summary>
        [JsonPropertyName("scope")]
        public string? Scope { get; set; }
    }
}
