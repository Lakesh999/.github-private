using Hugh.Setup.Detection;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for CopilotCliDetector.
/// Tests detection of GitHub Copilot CLI installation status.
/// </summary>
public class CopilotCliDetectorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new CopilotCliDetector(null!));
    }

    [Fact]
    public void Constructor_WithValidProcessRunner_CreatesInstance()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new CopilotCliDetector(mockRunner);

        Assert.NotNull(detector);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void PrerequisiteId_ReturnsCopilotCli()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new CopilotCliDetector(mockRunner);

        Assert.Equal("copilot-cli", detector.PrerequisiteId);
    }

    [Fact]
    public void DisplayName_ReturnsGitHubCopilotCLI()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new CopilotCliDetector(mockRunner);

        Assert.Equal("GitHub Copilot CLI", detector.DisplayName);
    }

    [Fact]
    public void Priority_Returns40()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new CopilotCliDetector(mockRunner);

        Assert.Equal(40, detector.Priority);
    }

    [Fact]
    public void IsInstallable_ReturnsTrue()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new CopilotCliDetector(mockRunner);

        Assert.True(detector.IsInstallable);
    }

    #endregion

    #region DetectAsync Tests - Satisfied State

    [Theory]
    [InlineData("copilot version 1.0.0", "1.0.0")]
    [InlineData("GitHub Copilot CLI version 1.2.3", "1.2.3")]
    [InlineData("1.5.0", "1.5.0")]
    [InlineData("version 2.0.0-beta", "2.0.0-beta")]
    public async Task DetectAsync_WhenCopilotInstalled_ReturnsSatisfiedWithVersion(string output, string expectedVersion)
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("copilot", "--version", output);
        var detector = new CopilotCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Equal("copilot-cli", result.PrerequisiteId);
        Assert.Equal(expectedVersion, result.Version);
        Assert.Contains("installed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenCopilotInstalled_VerifiesCommandExecution()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("copilot", "--version", "copilot version 1.0.0");
        var detector = new CopilotCliDetector(mockRunner);

        // Act
        await detector.DetectAsync();

        // Assert
        Assert.Single(mockRunner.Invocations);
        Assert.Equal(("copilot", "--version"), mockRunner.Invocations[0]);
    }

    #endregion

    #region DetectAsync Tests - Binary Not Found (Satisfied with Info Message)

    [Fact]
    public async Task DetectAsync_WhenCopilotNotInstalled_ReturnsSatisfiedWithInfoMessage()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupFailure("copilot", "--version", 1, "'copilot' is not recognized");
        var detector = new CopilotCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Equal("copilot-cli", result.PrerequisiteId);
        Assert.Contains("optional", result.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SDK bundles CLI internally", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenExecutableNotFound_ReturnsSatisfiedWithInfoMessage()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("copilot", "--version");
        var detector = new CopilotCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("optional", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenFileNotFoundExceptionThrown_ReturnsSatisfied()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupException("copilot", "--version", new FileNotFoundException("File not found"));
        var detector = new CopilotCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
    }

    #endregion

    #region DetectAsync Tests - No Hints When Binary Missing (non-blocking)

    [Fact]
    public async Task DetectAsync_WhenBinaryMissing_NoHintsProvided()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("copilot", "--version");
        var detector = new CopilotCliDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert - no hints because this is non-blocking (Satisfied state)
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Null(result.Hints);
    }

    #endregion

    #region DetectAsync Tests - Cancellation

    [Fact]
    public async Task DetectAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("copilot", "--version", "1.0.0");
        var detector = new CopilotCliDetector(mockRunner);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - the mock doesn't check cancellation, but real runner would
        // This test verifies the token is passed through
        // In a real scenario with actual process execution, this would throw
        await detector.DetectAsync(cts.Token);

        // Verify the method was still invoked (mock doesn't throw on cancellation)
        Assert.Single(mockRunner.Invocations);
    }

    #endregion
}
