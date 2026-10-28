using Hugh.Setup.Detection;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for DotNetSdkDetector.
/// Tests detection of .NET SDK installation status.
/// </summary>
public class DotNetSdkDetectorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullProcessRunner_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new DotNetSdkDetector(null!));
    }

    [Fact]
    public void Constructor_WithValidProcessRunner_CreatesInstance()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new DotNetSdkDetector(mockRunner);

        Assert.NotNull(detector);
    }

    #endregion

    #region Properties Tests

    [Fact]
    public void PrerequisiteId_ReturnsDotNetSdk()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new DotNetSdkDetector(mockRunner);

        Assert.Equal("dotnet-sdk", detector.PrerequisiteId);
    }

    [Fact]
    public void DisplayName_ReturnsDotNetSdk()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new DotNetSdkDetector(mockRunner);

        Assert.Equal(".NET SDK", detector.DisplayName);
    }

    [Fact]
    public void Priority_Returns35()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new DotNetSdkDetector(mockRunner);

        Assert.Equal(35, detector.Priority);
    }

    [Fact]
    public void IsInstallable_ReturnsTrue()
    {
        var mockRunner = new MockProcessRunner();
        var detector = new DotNetSdkDetector(mockRunner);

        Assert.True(detector.IsInstallable);
    }

    #endregion

    #region DetectAsync Tests - Satisfied State

    [Theory]
    [InlineData("8.0.400")]
    [InlineData("9.0.100")]
    [InlineData("8.0.301")]
    public async Task DetectAsync_WhenDotNetInstalled_ReturnsSatisfiedWithVersion(string version)
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("dotnet", "--version", version);
        var detector = new DotNetSdkDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Equal("dotnet-sdk", result.PrerequisiteId);
        Assert.Equal(version, result.Version);
        Assert.Contains("installed", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenDotNetInstalled_TrimsWhitespace()
    {
        // dotnet --version may include trailing newline
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("dotnet", "--version", "8.0.400\n");
        var detector = new DotNetSdkDetector(mockRunner);

        var result = await detector.DetectAsync();

        Assert.Equal("8.0.400", result.Version);
    }

    [Fact]
    public async Task DetectAsync_WhenDotNetInstalled_VerifiesCommandExecution()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupSuccess("dotnet", "--version", "8.0.400");
        var detector = new DotNetSdkDetector(mockRunner);

        // Act
        await detector.DetectAsync();

        // Assert
        Assert.Single(mockRunner.Invocations);
        Assert.Equal(("dotnet", "--version"), mockRunner.Invocations[0]);
    }

    #endregion

    #region DetectAsync Tests - Missing State

    [Fact]
    public async Task DetectAsync_WhenDotNetNotInstalled_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupNotFound("dotnet", "--version");
        var detector = new DotNetSdkDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Equal("dotnet-sdk", result.PrerequisiteId);
        Assert.NotNull(result.Hints);
        Assert.Contains(result.Hints, h => h.Contains("winget install Microsoft.DotNet.SDK.8"));
    }

    [Fact]
    public async Task DetectAsync_WhenCommandFails_ReturnsMissing()
    {
        // Arrange
        var mockRunner = new MockProcessRunner()
            .SetupFailure("dotnet", "--version", exitCode: 1);
        var detector = new DotNetSdkDetector(mockRunner);

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
            .SetupException("dotnet", "--version", new FileNotFoundException("dotnet not found"));
        var detector = new DotNetSdkDetector(mockRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert
        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.Contains(result.Hints!, h => h.Contains("dotnet.microsoft.com"));
    }

    #endregion
}
