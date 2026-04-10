using CodeReview.API.Configuration;
using CodeReview.API.Data;
using CodeReview.API.Middleware;
using CodeReview.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Bind LLM config section to typed options class
builder.Services.Configure<LLMOptions>(
    builder.Configuration.GetSection("LLM"));

// Register HttpClient for Gemini calls
builder.Services.AddHttpClient<GeminiLLMService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(120);
});
// Register services
builder.Services.AddScoped<ILLMService, GeminiLLMService>();

// Register DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration
        .GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Queue — singleton because it lives for entire app lifetime
builder.Services.AddSingleton<ReviewQueue>();

// Background worker — automatically started by .NET
builder.Services.AddHostedService<ReviewWorker>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseAuthorization();
app.MapControllers();

app.Run();