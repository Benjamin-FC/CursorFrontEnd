namespace CrmClientApp.Services;

/// <summary>
/// Service interface for retrieving client data from an external CRM server.
/// </summary>
public interface ICrmService
{
    /// <summary>
    /// Retrieves client data from the external CRM server for the specified client ID.
    /// </summary>
    /// <param name="clientId">The unique identifier of the client to retrieve data for.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the client data as a string.</returns>
    /// <exception cref="HttpRequestException">Thrown when the HTTP request to the CRM server fails.</exception>
    Task<string> GetClientDataAsync(string clientId);
}
