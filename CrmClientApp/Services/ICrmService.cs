namespace CrmClientApp.Services;

public interface ICrmService
{
    Task<string> GetClientDataAsync(string clientId);
}
