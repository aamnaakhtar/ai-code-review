namespace CodeReview.API.Configuration;

public class LLMOptions
{
    public string Provider { get; set; } = "gemini";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
}