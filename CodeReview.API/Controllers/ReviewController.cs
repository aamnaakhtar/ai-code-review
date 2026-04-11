using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CodeReview.API.Data;
using CodeReview.API.DTOs;
using CodeReview.API.Models.Entities;
using CodeReview.API.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace CodeReview.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
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

        // Extract userId from the JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = userIdClaim != null ? Guid.Parse(userIdClaim) : null;

        var job = new ReviewJob
        {
            Code = request.Code,
            Language = request.Language,
            Status = "pending",
            UserId = userId  // now linked to real user
        };

        _db.ReviewJobs.Add(job);
        await _db.SaveChangesAsync();

        await _queue.Writer.WriteAsync(job);

        _logger.LogInformation(
            "Job {JobId} queued for {Language} by user {UserId}",
            job.Id, job.Language, userId);

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

    // GET api/review/history — fetch recent reviews for display
    [HttpGet("history")]
    public async Task<ActionResult<List<ReviewHistoryDto>>> GetHistory()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid? userId = userIdClaim != null ? Guid.Parse(userIdClaim) : null;

        var query = _db.ReviewJobs
            .Include(j => j.Result)
            .Where(j => j.Status == "done" && j.Result != null);

        // Filter by user if authenticated
        if (userId.HasValue)
            query = query.Where(j => j.UserId == userId);

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Take(10)
            .Select(j => new ReviewHistoryDto
            {
                JobId = j.Id.ToString(),
                Language = j.Language,
                Status = j.Status,
                CreatedAt = j.CreatedAt.ToString("o"),
                Summary = j.Result!.Summary,
                TotalIssues = j.Result!.TotalIssues,
                CodePreview = j.Code.Length > 60
                    ? j.Code.Substring(0, 60) + "..."
                    : j.Code
            })
            .ToListAsync();

        return Ok(jobs);
    }
}