namespace CodeReview.API.Models.Entities;

public class ReviewResult
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key — links result back to the job
    public Guid JobId { get; set; }
    public string Summary { get; set; } = string.Empty;
    public int TotalIssues { get; set; }
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ReviewJob Job { get; set; } = null!;
    public ICollection<ReviewIssue> Issues { get; set; } = new List<ReviewIssue>();
}