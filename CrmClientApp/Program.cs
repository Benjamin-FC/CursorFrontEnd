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

// Register Token Service
builder.Services.AddSingleton<ITokenService, TokenService>();

// Register CRM service
var crmServerBaseUrl = builder.Configuration["ExternalApi:CrmServer:BaseUrl"];
var timeoutSeconds = builder.Configuration.GetValue<int>("ExternalApi:CrmServer:TimeoutSeconds", 30);

if (string.IsNullOrWhiteSpace(crmServerBaseUrl))
{
    throw new InvalidOperationException("ExternalApi:CrmServer:BaseUrl configuration is required");
}

// Validate environment variables for token generation
var userId = Environment.GetEnvironmentVariable("CRM_USER_ID");
var password = Environment.GetEnvironmentVariable("CRM_PASSWORD");

if (string.IsNullOrWhiteSpace(userId))
{
    throw new InvalidOperationException("CRM_USER_ID environment variable is required");
}

if (string.IsNullOrWhiteSpace(password))
{
    throw new InvalidOperationException("CRM_PASSWORD environment variable is required");
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
