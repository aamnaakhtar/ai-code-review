using CodeReview.API.Data;
using CodeReview.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeReview.API.Services;

public class ReviewWorker : BackgroundService
{
    private readonly ReviewQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ReviewCacheService _cache;
    private readonly ILogger<ReviewWorker> _logger;

    public ReviewWorker(
        ReviewQueue queue,
        IServiceScopeFactory scopeFactory,
        ReviewCacheService cache,
        ILogger<ReviewWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Review worker started");

        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(ReviewJob job, CancellationToken ct)
    {
        _logger.LogInformation("Processing job {JobId}", job.Id);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var llm = scope.ServiceProvider.GetRequiredService<ILLMService>();

        try
        {
            job.Status = "processing";
            db.ReviewJobs.Update(job);
            await db.SaveChangesAsync(ct);

            // Step 1 — check cache BEFORE calling LLM
            var cacheKey = _cache.GenerateCacheKey(job.Code, job.Language);
            if (!_cache.TryGet(cacheKey, out var llmResponse))
            {
                // Cache miss — call the real LLM
                _logger.LogInformation("Cache MISS — calling Gemini for job {JobId}", job.Id);
                llmResponse = await llm.ReviewCodeAsync(job.Code, job.Language);

                // Store in cache for future identical submissions
                _cache.Set(cacheKey, llmResponse!);
            }
            else
            {
                _logger.LogInformation("Cache HIT — skipping Gemini for job {JobId}", job.Id);
            }

            // Save result to DB regardless of cache hit/miss
            var result = new ReviewResult
            {
                JobId = job.Id,
                Summary = llmResponse!.Summary,
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

            db.ReviewResults.Add(result);
            job.Status = "done";
            db.ReviewJobs.Update(job);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);

            using var errorScope = _scopeFactory.CreateScope();
            var errorDb = errorScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var failedJob = await errorDb.ReviewJobs.FindAsync(job.Id);
            if (failedJob != null)
            {
                failedJob.Status = "error";
                await errorDb.SaveChangesAsync();
            }
        }
    }
}