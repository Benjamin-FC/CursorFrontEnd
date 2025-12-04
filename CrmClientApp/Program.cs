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
var crmServerBaseUrl = builder.Configuration["ExternalApi:CrmServer:BaseUrl"];
var timeoutSeconds = builder.Configuration.GetValue<int>("ExternalApi:CrmServer:TimeoutSeconds", 30);

if (string.IsNullOrWhiteSpace(crmServerBaseUrl))
{
    throw new InvalidOperationException("ExternalApi:CrmServer:BaseUrl configuration is required");
}

// Validate OAuth configuration
var tokenEndpoint = builder.Configuration["ExternalApi:Token:Endpoint"];
if (string.IsNullOrWhiteSpace(tokenEndpoint))
{
    throw new InvalidOperationException("ExternalApi:Token:Endpoint configuration is required");
}

// Validate environment variables for OAuth
var clientId = Environment.GetEnvironmentVariable("OAUTH_CLIENT_ID");
var clientSecret = Environment.GetEnvironmentVariable("OAUTH_CLIENT_SECRET");

if (string.IsNullOrWhiteSpace(clientId))
{
    throw new InvalidOperationException("OAUTH_CLIENT_ID environment variable is required");
}

if (string.IsNullOrWhiteSpace(clientSecret))
{
    throw new InvalidOperationException("OAUTH_CLIENT_SECRET environment variable is required");
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
