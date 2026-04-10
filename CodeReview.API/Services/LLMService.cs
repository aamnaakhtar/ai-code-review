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
        // Check if code needs chunking
        if (!TokenChunker.NeedsChunking(code))
        {
            _logger.LogInformation(
                "Code fits in one chunk ({Tokens} tokens) — sending directly",
                TokenChunker.EstimateTokens(code));

            return await ReviewChunkAsync(code, language);
        }

        // Large file — chunk it
        var chunks = TokenChunker.Chunk(code);
        _logger.LogInformation(
            "Code split into {Count} chunks for review", chunks.Count);

        // Review each chunk in parallel for speed
        var chunkTasks = chunks.Select((chunk, index) =>
            ReviewChunkWithContextAsync(chunk, language, index + 1, chunks.Count));

        var chunkResults = await Task.WhenAll(chunkTasks);

        // Merge all chunk results into one response
        return MergeResults(chunkResults, chunks.Count);
    }

    private async Task<LLMReviewResponse> ReviewChunkWithContextAsync(
        CodeChunk chunk, string language, int chunkNumber, int totalChunks)
    {
        _logger.LogInformation(
            "Reviewing chunk {Num}/{Total} (lines {Start}-{End}, ~{Tokens} tokens)",
            chunkNumber, totalChunks, chunk.StartLine, chunk.EndLine, chunk.EstimatedTokens);

        var result = await ReviewChunkAsync(chunk.Code, language);

        // Adjust line numbers — chunk starts at chunk.StartLine, not 1
        foreach (var issue in result.Issues)
        {
            if (issue.Line.HasValue)
                issue.Line = issue.Line.Value + chunk.StartLine - 1;
        }

        return result;
    }

    private async Task<LLMReviewResponse> ReviewChunkAsync(string code, string language)
    {
        var prompt = PromptBuilder.BuildReviewPrompt(code, language);

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                temperature = 0.2,
                maxOutputTokens = 8192
            }
        };

        var json = JsonSerializer.Serialize(requestBody);
        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            _logger.LogInformation("Gemini call attempt {Attempt}/{Max}", attempt, maxRetries);

            var response = await _httpClient.PostAsync(url, content);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                return ParseGeminiResponse(responseJson);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Gemini attempt {Attempt} failed with {Status}: {Body}",
                attempt, response.StatusCode, errorBody);

            if ((int)response.StatusCode == 503 && attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 5));
                continue;
            }

            response.EnsureSuccessStatusCode();
        }

        throw new Exception("Gemini API failed after all retries.");
    }

    private static LLMReviewResponse MergeResults(LLMReviewResponse[] results, int chunkCount)
    {
        var allIssues = results
            .SelectMany(r => r.Issues)
            .ToList();

        // Deduplicate — remove issues with the same type + line number
        // This handles overlap zones where same issue is caught twice
        var deduplicated = allIssues
            .GroupBy(i => new { i.Type, i.Line })
            .Select(g => g.First())
            .OrderBy(i => i.Line ?? int.MaxValue)
            .ToList();

        return new LLMReviewResponse
        {
            Summary = chunkCount > 1
                ? $"Large file reviewed in {chunkCount} chunks. {deduplicated.Count} issues found."
                : results[0].Summary,
            Issues = deduplicated
        };
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

        text = text.Trim();
        if (text.StartsWith("```json")) text = text[7..];
        if (text.StartsWith("```")) text = text[3..];
        if (text.EndsWith("```")) text = text[..^3];
        text = text.Trim();

        var result = JsonSerializer.Deserialize<LLMReviewResponse>(text);
        return result ?? new LLMReviewResponse
        {
            Summary = "Could not parse review response.",
            Issues = new List<LLMIssue>()
        };
    }
}