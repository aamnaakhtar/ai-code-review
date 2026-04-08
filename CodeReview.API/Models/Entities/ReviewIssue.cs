namespace CodeReview.API.Models.Entities;

public class ReviewIssue
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key — links issue to its result
    public Guid ResultId { get; set; }
    public string Type { get; set; } = string.Empty;      // bug, performance, style
    public string Severity { get; set; } = string.Empty;  // high, medium, low
    public int? Line { get; set; }                         // nullable
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;

    // Navigation property
    public ReviewResult Result { get; set; } = null!;
}