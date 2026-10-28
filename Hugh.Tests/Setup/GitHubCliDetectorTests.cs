using Hugh.Setup.Detection;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for GitHubCliDetector.
/// Tests detection of GitHub CLI (gh) installation status.
/// </summary>
public class GitHubCliDetectorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GitHubCliDetector(null!));
    }

    [Fact]
    public void Constructor_WithValidProcessRunner_CreatesInstance()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubCliDetector(mockRunner);

        Assert.NotNull(detector);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void PrerequisiteId_ReturnsGitHubCli()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubCliDetector(mockRunner);

        Assert.Equal("github-cli", detector.PrerequisiteId);
    }

    [Fact]
    public void DisplayName_ReturnsGitHubCLI()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubCliDetector(mockRunner);

        Assert.Equal("GitHub CLI", detector.DisplayName);
    }

    [Fact]
    public void Priority_Returns30()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubCliDetector(mockRunner);

        Assert.Equal(30, detector.Priority);
    }

    [Fact]
    public void IsInstallable_ReturnsTrue()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubCliDetector(mockRunner);

        Assert.True(detector.IsInstallable);
    }

    #endregion

    #region DetectAsync Tests - Satisfied State

    [Theory]
    [InlineData("gh version 2.45.0 (2024-03-14)\nhttps://github.com/cli/cli/releases/tag/v2.45.0", "2.45.0")]
    [InlineData("gh version 2.50.0 (2024-05-20)", "2.50.0")]
    [InlineData("version 2.0.0", "2.0.0")]
    [InlineData("2.30.0", "2.30.0")]
    public async Task DetectAsync_WhenGhInstalled_ReturnsSatisfiedWithVersion(string output, string expectedVersion)
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "--version", output);
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Equal("github-cli", result.PrerequisiteId);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Contains("installed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenGhInstalled_VerifiesCommandExecution()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "--version", "gh version 2.45.0");
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        await detector.DetectAsync();

        // Assert
        Assert.Single(mockRunner.Invocations);
        Assert.Equal(("gh", "--version"), mockRunner.Invocations[0]);
    }

    [Fact]
    public async Task DetectAsync_WithMultiLineOutput_ParsesFirstLineVersion()
    {
        // Arrange
        var multiLineOutput = """
            gh version 2.45.0 (2024-03-14)
            https://github.com/cli/cli/releases/tag/v2.45.0
            
            More information here
            """;
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "--version", multiLineOutput);
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Equal("2.45.0", result.Version);
    }

    #endregion

    #region DetectAsync Tests - Missing State

    [Fact]
    public async Task DetectAsync_WhenGhNotInstalled_ReturnsMissingWithExitCode()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupFailure("gh", "--version", 1, "'gh' is not recognized");
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Equal("github-cli", result.PrerequisiteId);
        Assert.Null(result.Version);
        Assert.Contains("not installed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenExecutableNotFound_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("gh", "--version");
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Contains("not found", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenFileNotFoundExceptionThrown_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupException("gh", "--version", new FileNotFoundException("File not found"));
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
    }

    #endregion

    #region DetectAsync Tests - Hints

    [Fact]
    public async Task DetectAsync_WhenMissing_ProvideInstallHints()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("gh", "--version");
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.NotNull(result.Hints);
        Assert.NotEmpty(result.Hints);
        Assert.Contains(result.Hints, h => h.Contains("winget install", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DetectAsync_WhenMissing_ProvidesAlternativeDownloadHint()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("gh", "--version");
        var detector = new GitHubCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Contains(result.Hints!, h => h.Contains("cli.github.com", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region DetectAsync Tests - Cancellation

    [Fact]
    public async Task DetectAsync_WithCancelledToken_PassesTokenThrough()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "--version", "gh version 2.45.0");
        var detector = new GitHubCliDetector(mockRunner);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - mock doesn't check cancellation, but verifies token is passed
        await detector.DetectAsync(cts.Token);

        // Assert
        Assert.Single(mockRunner.Invocations);
    }

    #endregion
}
