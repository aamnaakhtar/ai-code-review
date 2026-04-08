namespace CodeReview.API.Models.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property — EF uses this to understand the relationship
    public ICollection<ReviewJob> ReviewJobs { get; set; } = new List<ReviewJob>();
}