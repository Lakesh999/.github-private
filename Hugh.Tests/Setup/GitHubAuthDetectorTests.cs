using Hugh.Setup.Detection;
using GitHub.Copilot.SDK;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for GitHubAuthDetector.
/// Tests detection of GitHub authentication via environment variables and gh auth status.
/// </summary>
public class GitHubAuthDetectorTests : IDisposable
{
    private readonly string? _originalGhToken;
    private readonly string? _originalGitHubToken;

    public GitHubAuthDetectorTests()
    {
        // Save original environment variable values
        _originalGhToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        _originalGitHubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        // Clear tokens before each test
        Environment.SetEnvironmentVariable("GH_TOKEN", null);
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", null);
    }

    public void Dispose()
    {
        // Restore original environment variable values
        Environment.SetEnvironmentVariable("GH_TOKEN", _originalGhToken);
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", _originalGitHubToken);
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GitHubAuthDetector(null!));
    }

    [Fact]
    public void Constructor_WithValidProcessRunner_CreatesInstance()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        Assert.NotNull(detector);
    }

    [Fact]
    public void Constructor_WithClientFactory_CreatesInstance()
    {
        var mockRunner = new MockProcessRunner();
        Func<CopilotClient> factory = () => new CopilotClient(new CopilotClientOptions());
        var detector = new GitHubAuthDetector(mockRunner, factory);

        Assert.NotNull(detector);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void PrerequisiteId_ReturnsGitHubAuth()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        Assert.Equal("github-auth", detector.PrerequisiteId);
    }

    [Fact]
    public void DisplayName_ReturnsGitHubAuthentication()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        Assert.Equal("GitHub Authentication", detector.DisplayName);
    }

    [Fact]
    public void Priority_Returns50()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        Assert.Equal(50, detector.Priority);
    }

    [Fact]
    public void IsInstallable_ReturnsTrue()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        Assert.True(detector.IsInstallable);
    }

    #endregion

    #region DetectAsync Tests - GH_TOKEN Environment Variable

    [Fact]
    public async Task DetectAsync_WhenGhTokenSet_ReturnsSatisfied()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GH_TOKEN", "ghp_test_token_12345");
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("GH_TOKEN", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenGhTokenSet_DoesNotRunGhAuthStatus()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GH_TOKEN", "ghp_test_token_12345");
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        await detector.DetectAsync();

        // Assert
        Assert.Empty(mockRunner.Invocations);
    }

    #endregion

    #region DetectAsync Tests - GITHUB_TOKEN Environment Variable

    [Fact]
    public async Task DetectAsync_WhenGitHubTokenSet_ReturnsSatisfied()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "ghp_test_token_67890");
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("GITHUB_TOKEN", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenBothTokensSet_PrefersGhToken()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GH_TOKEN", "ghp_gh_token");
        Environment.SetEnvironmentVariable("GITHUB_TOKEN", "ghp_github_token");
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("GH_TOKEN", result.Message);
    }

    #endregion

    #region DetectAsync Tests - gh auth status Command

    [Theory]
    [InlineData("Logged in to github.com as octocat (oauth_token)", "octocat")]
    [InlineData("Logged in to github.com as test-user", "test-user")]
    [InlineData("Logged in to github.com account myuser (@myuser)", "myuser")]
    public async Task DetectAsync_WhenGhAuthStatusSucceeds_ReturnsSatisfiedWithUsername(string output, string expectedUsername)
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "auth status", output);
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains(expectedUsername, result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenGhAuthStatusSucceeds_VerifiesCommandExecution()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "auth status", "Logged in to github.com as octocat");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        await detector.DetectAsync();

        // Assert
        Assert.Single(mockRunner.Invocations);
        Assert.Equal(("gh", "auth status"), mockRunner.Invocations[0]);
    }

    [Fact]
    public async Task DetectAsync_WhenGhAuthStatusSucceedsWithoutUsername_ReturnsGenericMessage()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "auth status", "Authentication successful");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("Authenticated to GitHub", result.Message);
    }

    #endregion

    #region DetectAsync Tests - Misconfigured State

    [Fact]
    public async Task DetectAsync_WhenGhAuthStatusFails_ReturnsMisconfigured()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupFailure("gh", "auth status", 1, "You are not logged into any GitHub hosts");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Misconfigured, result.State);
        Assert.Contains("not authenticated", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenMisconfigured_ProvidesAuthHints()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupFailure("gh", "auth status", 1, "Not authenticated");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.NotNull(result.Hints);
        Assert.Contains(result.Hints, h => h.Contains("gh auth login", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Hints, h => h.Contains("GH_TOKEN", StringComparison.OrdinalIgnoreCase));
    }

    #endregion

    #region DetectAsync Tests - Missing State (gh CLI not installed)

    [Fact]
    public async Task DetectAsync_WhenGhCliNotInstalled_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("gh", "auth status");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Contains("GitHub CLI is not installed", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenGhCliNotInstalled_ProvidesInstallHints()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("gh", "auth status");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.NotNull(result.Hints);
        Assert.Contains(result.Hints, h => h.Contains("Install GitHub CLI", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task DetectAsync_WhenFileNotFoundException_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupException("gh", "auth status", new FileNotFoundException("gh not found"));
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
    }

    #endregion

    #region DetectAsync Tests - Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task DetectAsync_WithEmptyOrWhitespaceGhToken_FallsBackToGhAuthStatus(string? tokenValue)
    {
        // Arrange
        Environment.SetEnvironmentVariable("GH_TOKEN", tokenValue);
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "auth status", "Logged in to github.com as octocat");
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Single(mockRunner.Invocations); // Fallback to gh auth status was called
    }

    [Fact]
    public async Task DetectAsync_WhenOutputCombinesStdoutAndStderr_ParsesUsername()
    {
        // Arrange - gh auth status sometimes writes to stderr
        var mockRunner = new MockProcessRunner()
            .SetupResult("gh", "auth status", new ProcessResult
            {
                ExitCode = 0,
                StandardOutput = "",
                StandardError = "Logged in to github.com as stderr-user"
            });
        var detector = new GitHubAuthDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("stderr-user", result.Message);
    }

    #endregion

    #region SDK-Based Auth Detection (Client Factory)

    [Fact]
    public async Task DetectAsync_WhenClientFactoryThrows_FallsBackToShell()
    {
        // Arrange - Factory throws to simulate SDK unavailable
        Func<CopilotClient> failingFactory = () => throw new InvalidOperationException("SDK not available");
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "auth status", "Logged in to github.com as shell-user");
        var detector = new GitHubAuthDetector(mockRunner, failingFactory);

        // Act
        var result = await detector.DetectAsync();

        // Assert - falls through to shell-based check
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("shell-user", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WhenNoClientFactory_UsesShellDirectly()
    {
        // Arrange - No factory provided (null)
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("gh", "auth status", "Logged in to github.com as direct-user");
        var detector = new GitHubAuthDetector(mockRunner, clientFactory: null);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("direct-user", result.Message);
        Assert.Single(mockRunner.Invocations);
    }

    [Fact]
    public async Task DetectAsync_WhenEnvTokenSet_SkipsSdkAndShell()
    {
        // Arrange - env token takes priority over everything
        Environment.SetEnvironmentVariable("GH_TOKEN", "ghp_test_token");
        Func<CopilotClient> factory = () => throw new InvalidOperationException("Should not be called");
        var mockRunner = new MockProcessRunner();
        var detector = new GitHubAuthDetector(mockRunner, factory);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("GH_TOKEN", result.Message);
        Assert.Empty(mockRunner.Invocations);
    }

    #endregion
}
