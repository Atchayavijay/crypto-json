var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var apiBaseUrl = builder.Configuration["CryptoApi:BaseUrl"]
    ?? throw new InvalidOperationException(
        "CryptoApi:BaseUrl is missing.");

builder.Services.AddHttpClient("CryptoApi", client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

app.UseHttpsRedirection();

app.MapControllers();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    Status = "Success",
    Message = "Central Crypto IAPI is running"
}));

app.Run();