namespace CodeReview.API.DTOs;

public class ReviewHistoryDto
{
    public string JobId { get; set; } = string.Empty;
    public string Language { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedAt { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public int TotalIssues { get; set; }
    public string CodePreview { get; set; } = string.Empty;
}