namespace CrmClientApp.Services;

public interface ITokenService
{
    Task<string> GetTokenAsync();
}
