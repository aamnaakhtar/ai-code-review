namespace CodeReview.API.DTOs;

public class ReviewIssueDto
{
    public string Type { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public int? Line { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Suggestion { get; set; } = string.Empty;
}

public class ReviewResultDto
{
    public string Id { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public List<ReviewIssueDto> Issues { get; set; } = new();
    public string Summary { get; set; } = string.Empty;
    public string ReviewedAt { get; set; } = string.Empty;
}