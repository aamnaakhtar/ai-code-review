using CodeReview.API.DTOs;

namespace CodeReview.API.Services;

public class ReviewService : IReviewService
{
    public async Task<ReviewResultDto> ReviewCodeAsync(ReviewRequestDto request)
    {
        // Day 4 — this will call the real LLM API
        // For now we return a mock so the controller works end-to-end
        await Task.Delay(500); // simulates async work

        return new ReviewResultDto
        {
            Id = Guid.NewGuid().ToString(),
            Status = "done",
            Summary = $"Reviewed {request.Language} code. Found 2 issues.",
            ReviewedAt = DateTime.UtcNow.ToString("o"),
            Issues = new List<ReviewIssueDto>
            {
                new ReviewIssueDto
                {
                    Type = "bug",
                    Severity = "high",
                    Line = 4,
                    Message = "Off-by-one error in loop condition.",
                    Suggestion = "Change i <= items.length to i < items.length"
                },
                new ReviewIssueDto
                {
                    Type = "style",
                    Severity = "low",
                    Message = "Missing type annotations.",
                    Suggestion = "Add explicit types to function parameters"
                }
            }
        };
    }
}