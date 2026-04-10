using CodeReview.API.Data;
using CodeReview.API.DTOs;
using CodeReview.API.Models.Entities;

namespace CodeReview.API.Services;

public class ReviewService : IReviewService
{
    private readonly ILLMService _llmService;
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewService> _logger;

    public ReviewService(
        ILLMService llmService,
        AppDbContext db,
        ILogger<ReviewService> logger)
    {
        _llmService = llmService;
        _db = db;
        _logger = logger;
    }

    public async Task<ReviewResultDto> ReviewCodeAsync(ReviewRequestDto request)
    {
        // Save job to DB first
        var job = new ReviewJob
        {
            Code = request.Code,
            Language = request.Language,
            Status = "processing"
        };
        _db.ReviewJobs.Add(job);
        await _db.SaveChangesAsync();

        try
        {
            // Call LLM
            var llmResponse = await _llmService.ReviewCodeAsync(
                request.Code,
                request.Language);

            // Save result to DB
            var result = new ReviewResult
            {
                JobId = job.Id,
                Summary = llmResponse.Summary,
                TotalIssues = llmResponse.Issues.Count,
                Issues = llmResponse.Issues.Select(i => new ReviewIssue
                {
                    Type = i.Type,
                    Severity = i.Severity,
                    Line = i.Line,
                    Message = i.Message,
                    Suggestion = i.Suggestion
                }).ToList()
            };

            _db.ReviewResults.Add(result);
            job.Status = "done";
            await _db.SaveChangesAsync();

            return new ReviewResultDto
            {
                Id = result.Id.ToString(),
                Status = "done",
                Summary = result.Summary,
                ReviewedAt = result.ReviewedAt.ToString("o"),
                Issues = result.Issues.Select(i => new ReviewIssueDto
                {
                    Type = i.Type,
                    Severity = i.Severity,
                    Line = i.Line,
                    Message = i.Message,
                    Suggestion = i.Suggestion
                }).ToList()
            };
        }
        catch (Exception ex)
        {
            job.Status = "error";
            await _db.SaveChangesAsync();
            _logger.LogError(ex, "LLM review failed for job {JobId}", job.Id);
            throw;
        }
    }
}