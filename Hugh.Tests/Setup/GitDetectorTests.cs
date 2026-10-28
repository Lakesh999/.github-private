using Hugh.Setup.Detection;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for GitDetector.
/// Tests detection of Git installation status.
/// </summary>
public class GitDetectorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new GitDetector(null!));
    }

    [Fact]
    public void Constructor_WithValidProcessRunner_CreatesInstance()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitDetector(mockRunner);

        Assert.NotNull(detector);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void PrerequisiteId_ReturnsGit()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitDetector(mockRunner);

        Assert.Equal("git", detector.PrerequisiteId);
    }

    [Fact]
    public void DisplayName_ReturnsGit()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitDetector(mockRunner);

        Assert.Equal("Git", detector.DisplayName);
    }

    [Fact]
    public void Priority_Returns25()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitDetector(mockRunner);

        Assert.Equal(25, detector.Priority);
    }

    [Fact]
    public void IsInstallable_ReturnsTrue()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new GitDetector(mockRunner);

        Assert.True(detector.IsInstallable);
    }

    #endregion

    #region DetectAsync Tests - Satisfied State

    [Theory]
    [InlineData("git version 2.43.0.windows.1", "2.43.0.windows.1")]
    [InlineData("git version 2.39.2", "2.39.2")]
    [InlineData("git version 2.45.0", "2.45.0")]
    public async Task DetectAsync_WhenGitInstalled_ReturnsSatisfiedWithVersion(string output, string expectedVersion)
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("git", "--version", output);
        var detector = new GitDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Equal("git", result.PrerequisiteId);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Contains("installed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenGitInstalled_VerifiesCommandExecution()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("git", "--version", "git version 2.43.0");
        var detector = new GitDetector(mockRunner);

        // Act
        await detector.DetectAsync();

        // Assert
        Assert.Single(mockRunner.Invocations);
        Assert.Equal(("git", "--version"), mockRunner.Invocations[0]);
    }

    #endregion

    #region DetectAsync Tests - Missing State

    [Fact]
    public async Task DetectAsync_WhenGitNotInstalled_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("git", "--version");
        var detector = new GitDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Equal("git", result.PrerequisiteId);
        Assert.NotNull(result.Hints);
        Assert.Contains(result.Hints, h => h.Contains("winget install Git.Git"));
    }

    [Fact]
    public async Task DetectAsync_WhenCommandFails_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupFailure("git", "--version", exitCode: 1);
        var detector = new GitDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
    }

    [Fact]
    public async Task DetectAsync_WhenFileNotFound_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupException("git", "--version", new FileNotFoundException("git not found"));
        var detector = new GitDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Contains(result.Hints!, h => h.Contains("git-scm.com"));
    }

    #endregion
}
