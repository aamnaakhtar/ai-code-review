namespace CodeReview.API.Services;

public class CodeChunk
{
    public string Code { get; set; } = string.Empty;
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public int EstimatedTokens { get; set; }
}

public class TokenChunker
{
    // Gemini's context window is ~30k tokens
    // We use 8000 as our budget per chunk — leaves room for prompt + response
    private const int MaxTokensPerChunk = 8000;

    // Overlap — how many lines to repeat between chunks
    // Ensures functions split at boundaries still get full context
    private const int OverlapLines = 20;

    // Token estimation — 1 token ≈ 4 characters (industry standard approximation)
    public static int EstimateTokens(string text) => text.Length / 4;

    public static bool NeedsChunking(string code)
        => EstimateTokens(code) > MaxTokensPerChunk;

    public static List<CodeChunk> Chunk(string code)
    {
        // If small enough, return as single chunk — no splitting needed
        if (!NeedsChunking(code))
        {
            return new List<CodeChunk>
            {
                new CodeChunk
                {
                    Code = code,
                    StartLine = 1,
                    EndLine = code.Split('\n').Length,
                    EstimatedTokens = EstimateTokens(code)
                }
            };
        }

        var lines = code.Split('\n');
        var chunks = new List<CodeChunk>();
        int startLine = 0; // 0-indexed internally

        while (startLine < lines.Length)
        {
            var chunkLines = new List<string>();
            int currentTokens = 0;
            int endLine = startLine;

            // Keep adding lines until we hit the token budget
            while (endLine < lines.Length)
            {
                var lineTokens = EstimateTokens(lines[endLine] + "\n");

                // If adding this line would exceed budget, stop
                if (currentTokens + lineTokens > MaxTokensPerChunk && chunkLines.Count > 0)
                    break;

                chunkLines.Add(lines[endLine]);
                currentTokens += lineTokens;
                endLine++;
            }

            chunks.Add(new CodeChunk
            {
                Code = string.Join("\n", chunkLines),
                StartLine = startLine + 1,    // 1-indexed for display
                EndLine = endLine,
                EstimatedTokens = currentTokens
            });

            // Move start forward, but step back by OverlapLines
            // This creates the overlap between consecutive chunks
            startLine = endLine - OverlapLines;

            // Safety — if we didn't advance at all, force forward
            // (prevents infinite loop on single very long lines)
            if (startLine <= chunks.Last().StartLine - 1)
                startLine = endLine;
        }

        return chunks;
    }
}