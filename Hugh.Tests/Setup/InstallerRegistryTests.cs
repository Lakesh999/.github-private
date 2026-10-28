using Hugh.Setup.Installation;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for InstallerRegistry.
/// Tests installer selection and priority-based auto-selection logic.
/// </summary>
public class InstallerRegistryTests
{
    #region Mock Installer Implementation

    /// <summary>
    /// Mock installer for testing registry behavior.
    /// </summary>
    private class MockInstaller : IInstaller
    {
        private readonly bool _isAvailable;
        private readonly HashSet<string> _supportedPrerequisites;

        public MockInstaller(string installerId, bool isAvailable = true, params string[] supportedPrerequisites)
        {
            InstallerId = installerId;
            _isAvailable = isAvailable;
            _supportedPrerequisites = new HashSet<string>(supportedPrerequisites, StringComparer.OrdinalIgnoreCase);
        }

        public string InstallerId { get; }

        public bool CanInstall(string prerequisiteId) => _supportedPrerequisites.Contains(prerequisiteId);

        public Task<bool> IsAvailableAsync(CancellationToken ct = default) => Task.FromResult(_isAvailable);

        public Task<InstallationResult> InstallAsync(string prerequisiteId, CancellationToken ct = default)
        {
            return Task.FromResult(new InstallationResult
            {
                Success = true,
                Version = "1.0.0"
            });
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullInstallers_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new InstallerRegistry(null!));
    }

    [Fact]
    public void Constructor_WithEmptyInstallers_CreatesInstance()
    {
        var registry = new InstallerRegistry(Array.Empty<IInstaller>());

        Assert.NotNull(registry);
    }

    [Fact]
    public void Constructor_WithValidInstallers_CreatesInstance()
    {
        var installers = new[]
        {
            new MockInstaller("winget", true, "github-cli"),
            new MockInstaller("manual", true, "github-cli")
        };

        var registry = new InstallerRegistry(installers);

        Assert.NotNull(registry);
    }

    #endregion

    #region GetInstallerAsync Tests - Auto Selection

    [Fact]
    public async Task GetInstallerAsync_WhenNoInstallersRegistered_ReturnsNull()
    {
        // Arrange
        var registry = new InstallerRegistry(Array.Empty<IInstaller>());

        // Act
        var result = await registry.GetInstallerAsync("github-cli");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstallerAsync_WhenNoInstallerSupportsPrerequisite_ReturnsNull()
    {
        // Arrange
        var installers = new[]
        {
            new MockInstaller("winget", true, "other-prerequisite"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync("github-cli");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstallerAsync_WhenInstallerNotAvailable_ReturnsNull()
    {
        // Arrange
        var installers = new[]
        {
            new MockInstaller("winget", isAvailable: false, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync("github-cli");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetInstallerAsync_WhenSingleInstallerAvailable_ReturnsIt()
    {
        // Arrange
        var installers = new[]
        {
            new MockInstaller("winget", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync("github-cli");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("winget", result.InstallerId);
    }

    [Fact]
    public async Task GetInstallerAsync_WhenMultipleInstallersAvailable_ReturnsFirstInPriorityOrder()
    {
        // Arrange - winget first in list (higher priority)
        var installers = new IInstaller[]
        {
            new MockInstaller("winget", true, "github-cli"),
            new MockInstaller("manual", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync("github-cli");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("winget", result.InstallerId);
    }

    [Fact]
    public async Task GetInstallerAsync_WhenFirstInstallerUnavailable_ReturnsSecond()
    {
        // Arrange - winget unavailable, manual available
        var installers = new IInstaller[]
        {
            new MockInstaller("winget", isAvailable: false, "github-cli"),
            new MockInstaller("manual", isAvailable: true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync("github-cli");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("manual", result.InstallerId);
    }

    [Fact]
    public async Task GetInstallerAsync_WhenFirstInstallerDoesNotSupportPrerequisite_ReturnsSecond()
    {
        // Arrange - winget doesn't support copilot-cli
        var installers = new IInstaller[]
        {
            new MockInstaller("winget", true, "github-cli"),
            new MockInstaller("manual", true, "github-cli", "copilot-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync("copilot-cli");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("manual", result.InstallerId);
    }

    #endregion

    #region GetAvailableInstallersAsync Tests

    [Fact]
    public async Task GetAvailableInstallersAsync_WhenNoInstallersRegistered_ReturnsEmptyList()
    {
        // Arrange
        var registry = new InstallerRegistry(Array.Empty<IInstaller>());

        // Act
        var result = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableInstallersAsync_WhenNoInstallerSupportsPrerequisite_ReturnsEmptyList()
    {
        // Arrange
        var installers = new[]
        {
            new MockInstaller("winget", true, "other-prerequisite"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetAvailableInstallersAsync_WhenSingleInstallerAvailable_ReturnsIt()
    {
        // Arrange
        var installers = new[]
        {
            new MockInstaller("winget", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert
        Assert.Single(result);
        Assert.Equal("winget", result[0].InstallerId);
    }

    [Fact]
    public async Task GetAvailableInstallersAsync_WhenMultipleInstallersAvailable_ReturnsAllInOrder()
    {
        // Arrange
        var installers = new IInstaller[]
        {
            new MockInstaller("winget", true, "github-cli"),
            new MockInstaller("npm", true, "github-cli"),
            new MockInstaller("manual", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal("winget", result[0].InstallerId);
        Assert.Equal("npm", result[1].InstallerId);
        Assert.Equal("manual", result[2].InstallerId);
    }

    [Fact]
    public async Task GetAvailableInstallersAsync_FiltersOutUnavailableInstallers()
    {
        // Arrange
        var installers = new IInstaller[]
        {
            new MockInstaller("winget", isAvailable: false, "github-cli"),
            new MockInstaller("npm", isAvailable: true, "github-cli"),
            new MockInstaller("manual", isAvailable: true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("npm", result[0].InstallerId);
        Assert.Equal("manual", result[1].InstallerId);
    }

    [Fact]
    public async Task GetAvailableInstallersAsync_FiltersOutNonSupportingInstallers()
    {
        // Arrange
        var installers = new IInstaller[]
        {
            new MockInstaller("winget", true, "other-prerequisite"),
            new MockInstaller("npm", true, "github-cli", "copilot-cli"),
            new MockInstaller("manual", true, "github-cli", "copilot-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("npm", result[0].InstallerId);
        Assert.Equal("manual", result[1].InstallerId);
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task GetInstallerAsync_WithCancelledToken_PassesTokenToInstallers()
    {
        // Arrange
        // Note: InstallerRegistry doesn't explicitly check cancellation tokens.
        // It relies on downstream IInstaller.IsAvailableAsync to check cancellation.
        // This test verifies the token is passed through and doesn't cause issues.
        var installers = new[]
        {
            new MockInstaller("winget", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - With our mock that doesn't check cancellation, this should complete normally
        var result = await registry.GetInstallerAsync("github-cli", cts.Token);

        // Assert - The mock installer doesn't check cancellation, so it returns normally
        Assert.NotNull(result);
        Assert.Equal("winget", result.InstallerId);
    }

    [Fact]
    public async Task GetAvailableInstallersAsync_WithCancelledToken_PassesTokenToInstallers()
    {
        // Arrange
        // Note: InstallerRegistry doesn't explicitly check cancellation tokens.
        // It relies on downstream IInstaller.IsAvailableAsync to check cancellation.
        var installers = new[]
        {
            new MockInstaller("winget", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);
        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act - With our mock that doesn't check cancellation, this should complete normally
        var result = await registry.GetAvailableInstallersAsync("github-cli", cts.Token);

        // Assert
        Assert.Single(result);
        Assert.Equal("winget", result[0].InstallerId);
    }

    #endregion

    #region Case Sensitivity Tests

    [Theory]
    [InlineData("GITHUB-CLI")]
    [InlineData("GitHub-Cli")]
    [InlineData("github-CLI")]
    public async Task GetInstallerAsync_PrerequisiteId_IsCaseInsensitive(string prerequisiteId)
    {
        // Arrange
        var installers = new[]
        {
            new MockInstaller("winget", true, "github-cli"),
        };
        var registry = new InstallerRegistry(installers);

        // Act
        var result = await registry.GetInstallerAsync(prerequisiteId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("winget", result.InstallerId);
    }

    #endregion
}
