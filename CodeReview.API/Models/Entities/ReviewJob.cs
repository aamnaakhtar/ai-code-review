namespace CodeReview.API.Models.Entities;

public class ReviewJob
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign key — links this job to a user
    public Guid UserId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = "pending"; // pending, processing, done, error
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ReviewResult? Result { get; set; }
}