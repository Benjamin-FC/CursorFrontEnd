using System.Security.Cryptography;
using System.Text;

namespace CrmClientApp.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenService> _logger;
    private readonly string _userId;
    private readonly string _password;

    public TokenService(IConfiguration configuration, ILogger<TokenService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Read userid and password from environment variables
        _userId = Environment.GetEnvironmentVariable("CRM_USER_ID") 
            ?? throw new InvalidOperationException("CRM_USER_ID environment variable is required");
        _password = Environment.GetEnvironmentVariable("CRM_PASSWORD") 
            ?? throw new InvalidOperationException("CRM_PASSWORD environment variable is required");
    }

    public string GenerateToken()
    {
        try
        {
            // Get token generation config values
            var tokenAlgorithm = _configuration["ExternalApi:Token:Algorithm"] ?? "SHA256";
            var tokenSecret = _configuration["ExternalApi:Token:Secret"] 
                ?? throw new InvalidOperationException("ExternalApi:Token:Secret configuration is required");
            var tokenExpiryMinutes = _configuration.GetValue<int>("ExternalApi:Token:ExpiryMinutes", 60);
            
            // Generate timestamp for token expiry
            var expiryTime = DateTime.UtcNow.AddMinutes(tokenExpiryMinutes);
            var timestamp = ((DateTimeOffset)expiryTime).ToUnixTimeSeconds();
            
            // Create token payload combining userid, password, secret, and timestamp
            var payload = $"{_userId}:{_password}:{tokenSecret}:{timestamp}";
            
            // Generate token based on algorithm
            string token;
            switch (tokenAlgorithm.ToUpperInvariant())
            {
                case "SHA256":
                    token = GenerateSha256Token(payload);
                    break;
                case "SHA512":
                    token = GenerateSha512Token(payload);
                    break;
                case "MD5":
                    token = GenerateMd5Token(payload);
                    break;
                default:
                    _logger.LogWarning("Unknown token algorithm: {Algorithm}, defaulting to SHA256", tokenAlgorithm);
                    token = GenerateSha256Token(payload);
                    break;
            }
            
            // Optionally include timestamp in token format: {hash}:{timestamp}
            var formattedToken = _configuration.GetValue<bool>("ExternalApi:Token:IncludeTimestamp", true)
                ? $"{token}:{timestamp}"
                : token;
            
            _logger.LogDebug("Generated token for user: {UserId}", _userId);
            return formattedToken;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating token");
            throw;
        }
    }

    private string GenerateSha256Token(string payload)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateSha512Token(string payload)
    {
        using var sha512 = SHA512.Create();
        var hashBytes = sha512.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hashBytes);
    }

    private string GenerateMd5Token(string payload)
    {
        using var md5 = MD5.Create();
        var hashBytes = md5.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToBase64String(hashBytes);
    }
}
