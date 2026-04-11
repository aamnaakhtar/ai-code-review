using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeReview.API.Data;
using CodeReview.API.DTOs;
using CodeReview.API.Models.Entities;
using CodeReview.API.Services;

namespace CodeReview.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly ReviewQueue _queue;
    private readonly AppDbContext _db;
    private readonly ILogger<ReviewController> _logger;
    private readonly ReviewCacheService _cache;

    public ReviewController(
        ReviewQueue queue,
        AppDbContext db,
        ReviewCacheService cache,
        ILogger<ReviewController> logger)
    {
        _queue = queue;
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    // Add this endpoint
    [HttpGet("cache/stats")]
    public IActionResult GetCacheStats()
    {
        var stats = _cache.GetStats();
        return Ok(stats);
    }    // POST api/review — submit a job, returns jobId instantly

    [HttpPost]
    public async Task<ActionResult<SubmitReviewResponseDto>> SubmitReview(
        [FromBody] ReviewRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code cannot be empty.");

        // Create the job record in DB
        var job = new ReviewJob
        {
            Code = request.Code,
            Language = request.Language,
            Status = "pending"
        };

        _db.ReviewJobs.Add(job);
        await _db.SaveChangesAsync();

        // Enqueue for background processing
        await _queue.Writer.WriteAsync(job);

        _logger.LogInformation(
            "Job {JobId} queued for {Language}", job.Id, job.Language);

        // Return immediately — don't wait for LLM
        return Ok(new SubmitReviewResponseDto
        {
            JobId = job.Id.ToString(),
            Status = "pending"
        });
    }

    // GET api/review/{jobId} — polling endpoint
    [HttpGet("{jobId}")]
    public async Task<ActionResult<ReviewResultDto>> GetReview(string jobId)
    {
        if (!Guid.TryParse(jobId, out var id))
            return BadRequest("Invalid job ID.");

        var job = await _db.ReviewJobs
            .Include(j => j.Result)
            .ThenInclude(r => r!.Issues)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job == null)
            return NotFound();

        // Still processing — tell frontend to keep polling
        if (job.Status == "pending" || job.Status == "processing")
            return Ok(new ReviewResultDto
            {
                Id = job.Id.ToString(),
                Status = job.Status,
                Issues = new List<ReviewIssueDto>(),
                Summary = "",
                ReviewedAt = ""
            });

        // Failed
        if (job.Status == "error")
            return Ok(new ReviewResultDto
            {
                Id = job.Id.ToString(),
                Status = "error",
                Issues = new List<ReviewIssueDto>(),
                Summary = "Review failed. Please try again.",
                ReviewedAt = ""
            });

        // Done — return the full result
        var result = job.Result!;
        return Ok(new ReviewResultDto
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
        });
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy" });
}