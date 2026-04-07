using CodeReview.API.DTOs;

namespace CodeReview.API.Services;

public interface IReviewService
{
    Task<ReviewResultDto> ReviewCodeAsync(ReviewRequestDto request);
}