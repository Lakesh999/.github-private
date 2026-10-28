using System.Text.RegularExpressions;

namespace Hugh.Agents;

/// <summary>
/// Extracts document paths from agent responses using multiple regex patterns.
/// Handles various formats agents use to communicate document locations.
/// </summary>
public partial class DocumentPathExtractor
{
    /// <summary>
    /// Pattern 1: Explicit creation statements like "Created vision document at: docs/vision/001.md"
    /// Captures paths containing directory separators after action verbs.
    /// Uses non-greedy match to find the path after any descriptive text.
    /// Supports both forward slashes and backslashes.
    /// </summary>
    [GeneratedRegex(@"(?:created|saved|wrote|generated|updated)\b[^`""'\n]*?[`""'\s]([a-zA-Z0-9_.\\/-]+[/\\][a-zA-Z0-9_.-]+\.md)[`""']?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExplicitCreationRegex();

    /// <summary>
    /// Pattern 2: Markdown bold document references like "**Document:** docs/vision/001.md"
    /// Also matches variations like "**Next Step Document:**", "**Output Document:**", etc.
    /// Used in handoff messages between agents.
    /// </summary>
    [GeneratedRegex(@"\*\*(?:[\w\s]+)?Document(?:\s+Location)?:\*\*\s*[`""']?([^\s`""'\n]+\.md)[`""']?",
        RegexOptions.Compiled)]
    private static partial Regex HandoffDocumentRegex();

    /// <summary>
    /// Pattern 3: Markdown link format like "[001-feature.md](docs/plans/001-feature.md)" or "[Vision](docs/vision/001.md)"
    /// Captures the link target (the URL part in parentheses). Display text does not need to be .md.
    /// </summary>
    [GeneratedRegex(@"\[([^\]]+)\]\(([^)]+\.md)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    /// <summary>
    /// Pattern 4: Backtick-wrapped paths like "created at: `docs/research/001.md`"
    /// Common in code-formatted agent outputs.
    /// </summary>
    [GeneratedRegex(@"(?:created|saved|updated|written)\s+(?:at|to)?:?\s*`([^`]+\.md)`",
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BacktickPathRegex();

    /// <summary>
    /// Extracts the first document path found in the agent response.
    /// Patterns are tried in priority order: ExplicitCreation, Handoff, Backtick, MarkdownLink.
    /// </summary>
    /// <param name="agentResponse">The agent response text to search.</param>
    /// <returns>The first matched document path, or null if none found.</returns>
    public string? ExtractDocumentPath(string agentResponse)
    {
        if (string.IsNullOrWhiteSpace(agentResponse))
        {
            return null;
        }

        // Try patterns in priority order
        // Priority 1: Explicit creation statements (most specific)
        var match = ExplicitCreationRegex().Match(agentResponse);
        if (match.Success)
        {
            return NormalizePath(match.Groups[1].Value);
        }

        // Priority 2: Handoff document markers
        match = HandoffDocumentRegex().Match(agentResponse);
        if (match.Success)
        {
            return NormalizePath(match.Groups[1].Value);
        }

        // Priority 3: Backtick-wrapped paths
        match = BacktickPathRegex().Match(agentResponse);
        if (match.Success)
        {
            return NormalizePath(match.Groups[1].Value);
        }

        // Priority 4: Markdown links (least specific - capture the target URL)
        match = MarkdownLinkRegex().Match(agentResponse);
        if (match.Success)
        {
            return NormalizePath(match.Groups[2].Value);
        }

        return null;
    }

    /// <summary>
    /// Extracts all unique document paths found in the agent response.
    /// </summary>
    /// <param name="agentResponse">The agent response text to search.</param>
    /// <returns>A list of all unique document paths found, in discovery order.</returns>
    public IReadOnlyList<string> ExtractAllDocumentPaths(string agentResponse)
    {
        if (string.IsNullOrWhiteSpace(agentResponse))
        {
            return Array.Empty<string>();
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var orderedPaths = new List<string>();

        void AddPath(string path)
        {
            var normalized = NormalizePath(path);
            if (!string.IsNullOrEmpty(normalized) && paths.Add(normalized))
            {
                orderedPaths.Add(normalized);
            }
        }

        // Collect from all patterns
        foreach (Match match in ExplicitCreationRegex().Matches(agentResponse))
        {
            AddPath(match.Groups[1].Value);
        }

        foreach (Match match in HandoffDocumentRegex().Matches(agentResponse))
        {
            AddPath(match.Groups[1].Value);
        }

        foreach (Match match in BacktickPathRegex().Matches(agentResponse))
        {
            AddPath(match.Groups[1].Value);
        }

        foreach (Match match in MarkdownLinkRegex().Matches(agentResponse))
        {
            // For markdown links, use the target URL (group 2)
            AddPath(match.Groups[2].Value);
        }

        return orderedPaths;
    }

    /// <summary>
    /// Normalizes a document path by stripping quotes, backticks, and normalizing path separators.
    /// </summary>
    /// <param name="path">The raw path to normalize.</param>
    /// <returns>The normalized path with forward slashes.</returns>
    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        // Strip leading/trailing quotes and backticks
        var normalized = path.Trim('`', '"', '\'', ' ');

        // Normalize path separators to forward slashes
        normalized = normalized.Replace('\\', '/');

        // Remove any trailing punctuation that might have been captured
        normalized = normalized.TrimEnd('.', ',', ';', ':', '!', '?');

        return normalized;
    }
}
