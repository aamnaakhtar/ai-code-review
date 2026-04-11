using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using CodeReview.API.Models;

namespace CodeReview.API.Services;

public class ReviewCacheService
{
    // Thread-safe dictionary — multiple background workers can read/write safely
    private readonly ConcurrentDictionary<string, CachedReview> _cache = new();

    // How long a cached result stays valid
    private readonly TimeSpan _cacheDuration = TimeSpan.FromHours(24);

    private readonly ILogger<ReviewCacheService> _logger;

    public ReviewCacheService(ILogger<ReviewCacheService> logger)
    {
        _logger = logger;
    }

    // Generate a deterministic cache key from code + language
    public string GenerateCacheKey(string code, string language)
    {
        // Normalize — trim whitespace so formatting differences don't create misses
        var normalized = $"{language}:{code.Trim()}";
        var bytes = Encoding.UTF8.GetBytes(normalized);

        // SHA256 hash — produces a fixed 64-char hex string regardless of input size
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public bool TryGet(string cacheKey, out LLMReviewResponse? result)
    {
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            // Check if still valid
            if (DateTime.UtcNow - cached.CachedAt < _cacheDuration)
            {
                _logger.LogInformation(
                    "Cache HIT for key {Key} — skipping LLM call", cacheKey[..8]);
                result = cached.Response;
                return true;
            }

            // Expired — remove it
            _cache.TryRemove(cacheKey, out _);
            _logger.LogInformation("Cache EXPIRED for key {Key}", cacheKey[..8]);
        }

        result = null;
        return false;
    }

    public void Set(string cacheKey, LLMReviewResponse response)
    {
        _cache[cacheKey] = new CachedReview
        {
            Response = response,
            CachedAt = DateTime.UtcNow
        };

        _logger.LogInformation(
            "Cache SET for key {Key} — {Count} total entries",
            cacheKey[..8], _cache.Count);
    }

    // Stats endpoint — useful for monitoring and showing interviewers
    public CacheStats GetStats()
    {
        var now = DateTime.UtcNow;
        var valid = _cache.Values.Count(c => now - c.CachedAt < _cacheDuration);
        return new CacheStats
        {
            TotalEntries = _cache.Count,
            ValidEntries = valid,
            ExpiredEntries = _cache.Count - valid
        };
    }
}

public class CachedReview
{
    public LLMReviewResponse Response { get; set; } = new();
    public DateTime CachedAt { get; set; }
}

public class CacheStats
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
}