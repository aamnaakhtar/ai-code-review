using System.Text.Json.Serialization;

namespace CodeReview.API.Models;

public class LLMReviewResponse
{
    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("issues")]
    public List<LLMIssue> Issues { get; set; } = new();
}

public class LLMIssue
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("severity")]
    public string Severity { get; set; } = string.Empty;

    [JsonPropertyName("line")]
    public int? Line { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("suggestion")]
    public string Suggestion { get; set; } = string.Empty;
}