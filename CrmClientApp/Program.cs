using CrmClientApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Register HTTP Client Factory for Token Service
builder.Services.AddHttpClient();

// Register Token Service
builder.Services.AddSingleton<ITokenService, TokenService>();

// Register CRM service
var crmServerBaseUrl = Environment.GetEnvironmentVariable("CRM_BASEURL");
var timeoutSeconds = builder.Configuration.GetValue<int>("ExternalApi:CrmServer:TimeoutSeconds", 30);

if (string.IsNullOrWhiteSpace(crmServerBaseUrl))
{
    throw new InvalidOperationException("CRM_BASEURL environment variable is required");
}

// Validate environment variables for OAuth
var requiredEnvVars = new[] { "CRM_TOKEN_URL", "CRM_CLIENT_ID", "CRM_CLIENT_SECRET", "CRM_USERNAME", "CRM_PASSWORD" };
foreach (var envVar in requiredEnvVars)
{
    if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(envVar)))
    {
        throw new InvalidOperationException($"{envVar} environment variable is required");
    }
}

builder.Services.AddHttpClient<ICrmService, CrmService>(client =>
{
    client.BaseAddress = new Uri(crmServerBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(timeoutSeconds);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");

app.UseRouting();

app.UseAuthorization();

app.MapControllers();

// Serve static files from React app (for production)
app.UseStaticFiles();

// Fallback to index.html for SPA routing (only in production)
if (!app.Environment.IsDevelopment())
{
    app.MapFallbackToFile("index.html");
}

app.Run();
