using Microsoft.AspNetCore.Mvc;
using CodeReview.API.DTOs;
using CodeReview.API.Services;

namespace CodeReview.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReviewController : ControllerBase
{
    private readonly IReviewService _reviewService;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(IReviewService reviewService, ILogger<ReviewController> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<ActionResult<ReviewResultDto>> SubmitReview(
        [FromBody] ReviewRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            return BadRequest("Code cannot be empty.");

        _logger.LogInformation(
            "Review requested for language: {Language}", request.Language);

        var result = await _reviewService.ReviewCodeAsync(request);
        return Ok(result);
    }

    [HttpGet("health")]
    public IActionResult Health() => Ok(new { status = "healthy" });
}