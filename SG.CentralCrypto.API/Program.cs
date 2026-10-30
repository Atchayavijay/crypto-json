var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Enable Swagger
app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

// Root endpoint
app.MapGet("/", () => Results.Ok(new
{
    Status = "Success",
    Message = "Central Crypto API is running",
    Version = "1.0"
}));

app.Run();