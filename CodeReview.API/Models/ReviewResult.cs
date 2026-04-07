namespace CodeReview.API.Models;

public class ReviewIssue
{
    public string Type { get; set; } = string.Empty;      // bug, performance, style
    public string Severity { get; set; } = string.Empty;  // high, medium, low
    public int? Line { get; set; }                        // nullable — not every issue has a line
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

public class ReviewResult
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Status { get; set; } = "done";
    public List<ReviewIssue> Issues { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;
}