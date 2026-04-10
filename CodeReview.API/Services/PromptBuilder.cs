namespace CodeReview.API.Services;

public static class PromptBuilder
{
    public static string BuildReviewPrompt(string code, string language)
    {
        return $$"""
You are an expert code reviewer. Review the following {{language}} code.

IMPORTANT RULES:
- Respond ONLY with valid JSON — no markdown, no text outside the JSON
- Keep each "message" under 20 words
- Keep each "suggestion" under 15 words  
- Return a maximum of 5 issues total
- Focus on the most critical issues only

Respond with exactly this JSON structure:
{
    "summary": "One sentence summary under 20 words",
    "issues": [
        {
            "type": "bug|performance|style|security",
            "severity": "high|medium|low",
            "line": null,
            "message": "Short description under 20 words",
            "suggestion": "Specific fix under 15 words"
        }
    ]
}

Code to review:
```{{language}}
{{code}}
```
""";
    }
}