using CodeReview.API.Data;
using CodeReview.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace CodeReview.API.Services;

public class ReviewWorker : BackgroundService
{
    private readonly ReviewQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReviewWorker> _logger;

    public ReviewWorker(
        ReviewQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<ReviewWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Review worker started");

        // Keep reading from the queue until the app shuts down
        await foreach (var job in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await ProcessJobAsync(job, stoppingToken);
        }
    }

    private async Task ProcessJobAsync(ReviewJob job, CancellationToken ct)
    {
        _logger.LogInformation("Processing job {JobId} for {Language}",
            job.Id, job.Language);

        // Background services are singletons but DbContext is scoped
        // So we must create a new scope to get a fresh DbContext
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var llm = scope.ServiceProvider.GetRequiredService<ILLMService>();

        try
        {
            // Mark as processing
            job.Status = "processing";
            db.ReviewJobs.Update(job);
            await db.SaveChangesAsync(ct);

            // Call the LLM
            var llmResponse = await llm.ReviewCodeAsync(job.Code, job.Language);

            // Save result
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

            db.ReviewResults.Add(result);
            job.Status = "done";
            db.ReviewJobs.Update(job);
            await db.SaveChangesAsync(ct);

            _logger.LogInformation("Job {JobId} completed successfully", job.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Job {JobId} failed", job.Id);

            // Use a fresh scope since the previous one may be corrupted
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