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

app.Run();