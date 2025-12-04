namespace CrmClientApp.Services;

/// <summary>
/// Service interface for retrieving and managing OAuth 2.0 access tokens.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Retrieves a valid OAuth access token. Returns a cached token if available and not expired,
    /// otherwise fetches a new token from the token server.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains the OAuth access token as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request to the token server fails.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the token response is invalid or required configuration is missing.</exception>
    Task<string> GetTokenAsync();
}
