using Hugh.Agents;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for DocumentPathExtractor.
/// Tests all regex patterns for extracting document paths from agent responses.
/// </summary>
public class DocumentPathExtractorTests
{
    private readonly DocumentPathExtractor _extractor = new();

    #region ExplicitCreation Pattern Tests

    [Theory]
    [InlineData("Created vision document at: docs/vision/001.md", "docs/vision/001.md")]
    [InlineData("Saved to docs/plans/feature.md", "docs/plans/feature.md")]
    [InlineData("Wrote docs/research/analysis.md", "docs/research/analysis.md")]
    [InlineData("Generated document at docs/vision/new-feature.md", "docs/vision/new-feature.md")]
    [InlineData("Updated docs/plans/001-update.md", "docs/plans/001-update.md")]
    [InlineData("created vision: docs/vision/test.md", "docs/vision/test.md")]
    [InlineData("CREATED AT: docs/plans/UPPER.md", "docs/plans/UPPER.md")]
    public void ExtractDocumentPath_ExplicitCreation_ReturnsPath(string input, string expectedPath)
    {
        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    [Theory]
    [InlineData("Created document at: `docs/vision/001.md`", "docs/vision/001.md")]
    [InlineData("Saved to \"docs/plans/feature.md\"", "docs/plans/feature.md")]
    [InlineData("Wrote 'docs/research/analysis.md'", "docs/research/analysis.md")]
    public void ExtractDocumentPath_ExplicitCreation_HandlesQuotes(string input, string expectedPath)
    {
        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region HandoffDocument Pattern Tests

    [Theory]
    [InlineData("**Document:** docs/vision/001.md", "docs/vision/001.md")]
    [InlineData("**Document Location:** docs/plans/feature.md", "docs/plans/feature.md")]
    [InlineData("**Document:** `docs/research/001.md`", "docs/research/001.md")]
    [InlineData("Some text before **Document:** docs/vision/test.md and after", "docs/vision/test.md")]
    public void ExtractDocumentPath_HandoffDocument_ReturnsPath(string input, string expectedPath)
    {
        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region BacktickPath Pattern Tests

    [Theory]
    [InlineData("created at: `docs/research/001.md`", "docs/research/001.md")]
    [InlineData("saved to: `docs/plans/feature.md`", "docs/plans/feature.md")]
    [InlineData("updated at: `docs/vision/001.md`", "docs/vision/001.md")]
    [InlineData("written to: `docs/research/analysis.md`", "docs/research/analysis.md")]
    public void ExtractDocumentPath_BacktickPath_ReturnsPath(string input, string expectedPath)
    {
        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region MarkdownLink Pattern Tests

    [Theory]
    [InlineData("[001-feature.md](docs/plans/001-feature.md)", "docs/plans/001-feature.md")]
    [InlineData("[vision.md](docs/vision/001-vision.md)", "docs/vision/001-vision.md")]
    [InlineData("See [my-doc.md](path/to/my-doc.md) for details", "path/to/my-doc.md")]
    [InlineData("[README.md](docs/README.md) - the main doc", "docs/README.md")]
    public void ExtractDocumentPath_MarkdownLink_ReturnsLinkTarget(string input, string expectedPath)
    {
        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region Priority Order Tests

    [Fact]
    public void ExtractDocumentPath_MultiplePatterns_ReturnsHighestPriority()
    {
        // ExplicitCreation has highest priority
        var input = """
            **Document:** docs/handoff.md
            Created vision document at: docs/vision/priority.md
            [link.md](docs/link.md)
            """;

        var result = _extractor.ExtractDocumentPath(input);

        // ExplicitCreation pattern should win
        Assert.Equal("docs/vision/priority.md", result);
    }

    [Fact]
    public void ExtractDocumentPath_NoExplicitCreation_ReturnsHandoff()
    {
        var input = """
            **Document:** docs/handoff.md
            [link.md](docs/link.md)
            """;

        var result = _extractor.ExtractDocumentPath(input);

        // Handoff pattern should win when no ExplicitCreation
        Assert.Equal("docs/handoff.md", result);
    }

    #endregion

    #region ExtractAllDocumentPaths Tests

    [Fact]
    public void ExtractAllDocumentPaths_MultiplePathsFromDifferentPatterns_ReturnsAll()
    {
        var input = """
            Created vision document at: docs/vision/001.md
            **Document:** docs/handoff/002.md
            See [feature.md](docs/plans/003.md) for more
            Also updated at: `docs/research/004.md`
            """;

        var result = _extractor.ExtractAllDocumentPaths(input);

        Assert.Contains("docs/vision/001.md", result);
        Assert.Contains("docs/handoff/002.md", result);
        Assert.Contains("docs/plans/003.md", result);
        Assert.Contains("docs/research/004.md", result);
        Assert.Equal(4, result.Count);
    }

    [Fact]
    public void ExtractAllDocumentPaths_DuplicatePaths_ReturnsUnique()
    {
        var input = """
            Created at: docs/vision/001.md
            Saved to docs/vision/001.md
            **Document:** docs/vision/001.md
            """;

        var result = _extractor.ExtractAllDocumentPaths(input);

        Assert.Single(result);
        Assert.Equal("docs/vision/001.md", result[0]);
    }

    [Fact]
    public void ExtractAllDocumentPaths_PreservesDiscoveryOrder()
    {
        var input = """
            Created docs/first.md
            Created docs/second.md
            Created docs/third.md
            """;

        var result = _extractor.ExtractAllDocumentPaths(input);

        Assert.Equal(3, result.Count);
        Assert.Equal("docs/first.md", result[0]);
        Assert.Equal("docs/second.md", result[1]);
        Assert.Equal("docs/third.md", result[2]);
    }

    [Fact]
    public void ExtractAllDocumentPaths_CaseInsensitiveDuplicates_ReturnsUnique()
    {
        var input = """
            Created docs/VISION/001.md
            Updated docs/vision/001.md
            """;

        var result = _extractor.ExtractAllDocumentPaths(input);

        // Should deduplicate case-insensitively
        Assert.Single(result);
    }

    #endregion

    #region NormalizePath Tests

    [Theory]
    [InlineData("docs\\vision\\001.md", "docs/vision/001.md")]
    [InlineData("docs/plans/feature.md", "docs/plans/feature.md")]
    public void ExtractDocumentPath_NormalizesPathSeparators(string pathInInput, string expectedPath)
    {
        var input = $"Created at: {pathInInput}";

        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    [Theory]
    [InlineData("`docs/vision/001.md`", "docs/vision/001.md")]
    [InlineData("\"docs/plans/feature.md\"", "docs/plans/feature.md")]
    [InlineData("'docs/research/001.md'", "docs/research/001.md")]
    public void ExtractDocumentPath_StripsQuotesAndBackticks(string pathInInput, string expectedPath)
    {
        var input = $"**Document:** {pathInInput}";

        var result = _extractor.ExtractDocumentPath(input);

        Assert.Equal(expectedPath, result);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractDocumentPath_NullOrEmpty_ReturnsNull(string? input)
    {
        var result = _extractor.ExtractDocumentPath(input!);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ExtractAllDocumentPaths_NullOrEmpty_ReturnsEmptyList(string? input)
    {
        var result = _extractor.ExtractAllDocumentPaths(input!);

        Assert.Empty(result);
    }

    [Theory]
    [InlineData("No document paths here")]
    [InlineData("Just some text without any .md files")]
    [InlineData("Created something else entirely")]
    public void ExtractDocumentPath_NoMatches_ReturnsNull(string input)
    {
        var result = _extractor.ExtractDocumentPath(input);

        Assert.Null(result);
    }

    [Theory]
    [InlineData("No document paths here")]
    [InlineData("Just some text without any .md files")]
    public void ExtractAllDocumentPaths_NoMatches_ReturnsEmptyList(string input)
    {
        var result = _extractor.ExtractAllDocumentPaths(input);

        Assert.Empty(result);
    }

    [Fact]
    public void ExtractDocumentPath_RealWorldAgentResponse_ExtractsCorrectly()
    {
        var agentResponse = """
            ✅ **Vision Document Created**

            I've created the vision document based on our discussion.

            **Document:** `docs/vision/001-new-feature.md`

            The document captures:
            - Problem statement
            - Target users
            - Success criteria

            Ready to hand off to @pch-researcher for research phase.
            """;

        var result = _extractor.ExtractDocumentPath(agentResponse);

        Assert.Equal("docs/vision/001-new-feature.md", result);
    }

    [Fact]
    public void ExtractAllDocumentPaths_RealWorldWithMultipleDocs_ExtractsAll()
    {
        var agentResponse = """
            ## Research Complete

            Created research document at: docs/research/001-api-design.md

            Related documents reviewed:
            - [Vision](docs/vision/001-feature.md)
            - [Prior Research](docs/research/000-background.md)

            **Next Step Document:** docs/plans/001-implementation.md
            """;

        var result = _extractor.ExtractAllDocumentPaths(agentResponse);

        Assert.Contains("docs/research/001-api-design.md", result);
        Assert.Contains("docs/vision/001-feature.md", result);
        Assert.Contains("docs/research/000-background.md", result);
        Assert.Contains("docs/plans/001-implementation.md", result);
    }

    #endregion
}
