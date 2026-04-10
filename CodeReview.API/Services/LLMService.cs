using System.Text;
using System.Text.Json;
using CodeReview.API.Configuration;
using CodeReview.API.Models;
using Microsoft.Extensions.Options;

namespace CodeReview.API.Services;

public interface ILLMService
{
    Task<LLMReviewResponse> ReviewCodeAsync(string code, string language);
}

public class GeminiLLMService : ILLMService
{
    private readonly HttpClient _httpClient;
    private readonly LLMOptions _options;
    private readonly ILogger<GeminiLLMService> _logger;

    public GeminiLLMService(
        HttpClient httpClient,
        IOptions<LLMOptions> options,
        ILogger<GeminiLLMService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<LLMReviewResponse> ReviewCodeAsync(string code, string language)
    {
        var prompt = PromptBuilder.BuildReviewPrompt(code, language);

        var requestBody = new
        {
            contents = new[]
            {
            new
            {
                parts = new[]
                {
                    new { text = prompt }
                }
            }
        },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 8192
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        // Retry up to 3 times on 503
        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogInformation(
                "Calling Gemini API for {Language} — attempt {Attempt}/{Max}",
                language, attempt, maxRetries);

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("Gemini responded successfully on attempt {Attempt}", attempt);
                return ParseGeminiResponse(responseJson);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Gemini attempt {Attempt} failed with {StatusCode}: {Body}",
                attempt, response.StatusCode, errorBody);

            // If it's a 503, wait and retry
            if ((int)response.StatusCode == 503 && attempt < maxRetries)
            {
                var delay = TimeSpan.FromSeconds(attempt * 5); // 5s, 10s, 15s
                _logger.LogInformation("Waiting {Delay}s before retry...", delay.TotalSeconds);
                await Task.Delay(delay);
                continue;
            }

            // For any other error or last attempt, throw
            response.EnsureSuccessStatusCode();
        }

        throw new Exception("Gemini API failed after all retries.");
    }

    private LLMReviewResponse ParseGeminiResponse(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);

        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "{}";

        // Strip markdown fences Gemini sometimes adds
        text = text.Trim();
        if (text.StartsWith("```json"))
            text = text[7..];
        if (text.StartsWith("```"))
            text = text[3..];
        if (text.EndsWith("```"))
            text = text[..^3];
        text = text.Trim();

        _logger.LogInformation("Parsed Gemini response text: {Text}", text);

        var result = JsonSerializer.Deserialize<LLMReviewResponse>(text);
        return result ?? new LLMReviewResponse
        {
            Summary = "Could not parse review response.",
            Issues = new List<LLMIssue>()
        };
    }
}