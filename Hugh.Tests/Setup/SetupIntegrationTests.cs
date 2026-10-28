using Hugh.Setup;
using Hugh.Setup.Detection;
using Hugh.Setup.Installation;
using Hugh.Setup.UserInterface;

namespace Hugh.Tests.Setup;

/// <summary>
/// Integration tests for the Setup module.
/// These tests use real implementations to verify end-to-end behavior.
/// Note: These tests depend on the actual system state and may have different
/// results based on what's installed on the test machine.
/// </summary>
[Trait("Category", "Integration")]
public class SetupIntegrationTests
{
    /// <summary>
    /// Real process runner for integration tests.
    /// </summary>
    private readonly ProcessRunner _processRunner = new();

    #region Detector Integration Tests

    [Fact]
    public async Task GitHubCliDetector_DetectsActualGhInstallation()
    {
        // Arrange
        var detector = new GitHubCliDetector(_processRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert - either satisfied or missing, but should not throw
        Assert.NotNull(result);
        Assert.Equal("github-cli", result.PrerequisiteId);
        Assert.True(
            result.State == PrerequisiteState.Satisfied || result.State == PrerequisiteState.Missing,
            $"Unexpected state: {result.State}");

        // If satisfied, should have a version
        if (result.State == PrerequisiteState.Satisfied)
        {
            Assert.NotNull(result.Version);
            Assert.NotEmpty(result.Version);
        }
    }

    [Fact]
    public async Task CopilotCliDetector_DetectsActualCopilotInstallation()
    {
        // Arrange
        var detector = new CopilotCliDetector(_processRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert - either satisfied or missing, but should not throw
        Assert.NotNull(result);
        Assert.Equal("copilot-cli", result.PrerequisiteId);
        Assert.True(
            result.State == PrerequisiteState.Satisfied || result.State == PrerequisiteState.Missing,
            $"Unexpected state: {result.State}");
    }

    [Fact]
    public async Task GitHubAuthDetector_DetectsActualAuthStatus()
    {
        // Arrange
        var detector = new GitHubAuthDetector(_processRunner);

        // Act
        var result = await detector.DetectAsync();

        // Assert - any valid state is acceptable
        Assert.NotNull(result);
        Assert.Equal("github-auth", result.PrerequisiteId);
        Assert.True(
            result.State == PrerequisiteState.Satisfied ||
            result.State == PrerequisiteState.Missing ||
            result.State == PrerequisiteState.Misconfigured,
            $"Unexpected state: {result.State}");
    }

    #endregion

    #region End-to-End Orchestrator Tests

    [Fact]
    public async Task SetupOrchestrator_RunsAllDetectors_WithNonInteractiveUI()
    {
        // Arrange
        var detectors = new IPrerequisiteDetector[]
        {
            new GitHubCliDetector(_processRunner),
            new CopilotCliDetector(_processRunner),
            new GitHubAuthDetector(_processRunner)
        };

        var installerRegistry = new InstallerRegistry(Array.Empty<IInstaller>());
        var ui = new NonInteractiveSetupUI();

        var orchestrator = new SetupOrchestrator(detectors, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = false });

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Statuses.Count);

        // Verify each detector ran
        Assert.Contains(result.Statuses, s => s.PrerequisiteId == "github-cli");
        Assert.Contains(result.Statuses, s => s.PrerequisiteId == "copilot-cli");
        Assert.Contains(result.Statuses, s => s.PrerequisiteId == "github-auth");
    }

    [Fact]
    public async Task SetupOrchestrator_OrdersByPriority()
    {
        // Arrange
        var detectors = new IPrerequisiteDetector[]
        {
            new GitHubAuthDetector(_processRunner),   // Priority 50
            new GitHubCliDetector(_processRunner),    // Priority 30
            new CopilotCliDetector(_processRunner)    // Priority 40
        };

        var installerRegistry = new InstallerRegistry(Array.Empty<IInstaller>());
        var ui = new NonInteractiveSetupUI();

        var orchestrator = new SetupOrchestrator(detectors, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions());

        // Assert - statuses should be in priority order (30, 40, 50)
        Assert.Equal("github-cli", result.Statuses[0].PrerequisiteId);    // Priority 30
        Assert.Equal("copilot-cli", result.Statuses[1].PrerequisiteId);   // Priority 40
        Assert.Equal("github-auth", result.Statuses[2].PrerequisiteId);   // Priority 50
    }

    [Fact]
    public async Task SetupOrchestrator_WithFixEnabled_NonInteractive_SkipsAllPrompts()
    {
        // Arrange
        var detectors = new IPrerequisiteDetector[]
        {
            new GitHubCliDetector(_processRunner)
        };

        // Empty installer list so nothing actually gets installed
        var installerRegistry = new InstallerRegistry(Array.Empty<IInstaller>());
        var ui = new NonInteractiveSetupUI();

        var orchestrator = new SetupOrchestrator(detectors, installerRegistry, ui);

        // Act - Fix=true, NonInteractive=true should not throw or hang
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = true, NonInteractive = true });

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Statuses);
        // No installation results since no installers were available
        Assert.Null(result.InstallationResults);
    }

    #endregion

    #region InstallerRegistry Integration Tests

    [Fact]
    public async Task InstallerRegistry_WithRealInstallers_SelectsByAvailability()
    {
        // Arrange
        var manualInstaller = new ManualInstaller();
        var wingetInstaller = new WingetInstaller(_processRunner);

        // Winget first (higher priority)
        var registry = new InstallerRegistry(new IInstaller[] { wingetInstaller, manualInstaller });

        // Act
        var installer = await registry.GetInstallerAsync("github-cli");

        // Assert - should return winget if available, otherwise manual
        Assert.NotNull(installer);
        // The actual installer returned depends on system state
    }

    [Fact]
    public async Task InstallerRegistry_GetAvailableInstallers_ReturnsAllAvailable()
    {
        // Arrange
        var manualInstaller = new ManualInstaller();
        var registry = new InstallerRegistry(new IInstaller[] { manualInstaller });

        // Act
        var installers = await registry.GetAvailableInstallersAsync("github-cli");

        // Assert - manual installer should always be available
        Assert.NotEmpty(installers);
        Assert.Contains(installers, i => i.InstallerId == "manual");
    }

    #endregion

    #region NonInteractiveSetupUI Tests

    [Fact]
    public async Task NonInteractiveSetupUI_PromptToFix_AlwaysReturnsSkip()
    {
        // Arrange
        var ui = new NonInteractiveSetupUI();
        var status = new PrerequisiteStatus
        {
            PrerequisiteId = "test",
            State = PrerequisiteState.Missing
        };

        // Act
        var decision = await ui.PromptToFixAsync(status, Array.Empty<IInstaller>());

        // Assert
        Assert.Equal(FixDecision.Skip, decision);
    }

    [Fact]
    public async Task NonInteractiveSetupUI_PromptForAuthentication_AlwaysReturnsSkip()
    {
        // Arrange
        var ui = new NonInteractiveSetupUI();

        // Act
        var decision = await ui.PromptForAuthenticationAsync();

        // Assert
        Assert.Equal(AuthDecision.Skip, decision);
    }

    [Fact]
    public void NonInteractiveSetupUI_ShowPrerequisiteReport_DoesNotThrow()
    {
        // Arrange
        var ui = new NonInteractiveSetupUI();
        var statuses = new List<PrerequisiteStatus>
        {
            new() { PrerequisiteId = "test1", State = PrerequisiteState.Satisfied, Version = "1.0.0" },
            new() { PrerequisiteId = "test2", State = PrerequisiteState.Missing, Hints = new[] { "Install it" } },
            new() { PrerequisiteId = "test3", State = PrerequisiteState.Misconfigured, Message = "Config error" }
        };

        // Act & Assert - should not throw
        var exception = Record.Exception(() => ui.ShowPrerequisiteReport(statuses));
        Assert.Null(exception);
    }

    [Fact]
    public void NonInteractiveSetupUI_ShowInstallationProgress_DoesNotThrow()
    {
        // Arrange
        var ui = new NonInteractiveSetupUI();

        // Act & Assert
        var exception = Record.Exception(() => ui.ShowInstallationProgress("test", "Installing..."));
        Assert.Null(exception);
    }

    [Fact]
    public void NonInteractiveSetupUI_ShowInstallationResult_DoesNotThrow()
    {
        // Arrange
        var ui = new NonInteractiveSetupUI();

        // Act & Assert - success case
        var exception1 = Record.Exception(() => ui.ShowInstallationResult("test", new InstallationResult
        {
            Success = true,
            Version = "1.0.0"
        }));
        Assert.Null(exception1);

        // Act & Assert - failure case
        var exception2 = Record.Exception(() => ui.ShowInstallationResult("test", new InstallationResult
        {
            Success = false,
            ErrorMessage = "Failed"
        }));
        Assert.Null(exception2);
    }

    #endregion

    #region Cancellation Integration Tests

    [Fact]
    public async Task SetupOrchestrator_Cancellation_StopsEarly()
    {
        // Arrange
        var detectors = new IPrerequisiteDetector[]
        {
            new GitHubCliDetector(_processRunner),
            new CopilotCliDetector(_processRunner),
            new GitHubAuthDetector(_processRunner)
        };

        var installerRegistry = new InstallerRegistry(Array.Empty<IInstaller>());
        var ui = new NonInteractiveSetupUI();

        var orchestrator = new SetupOrchestrator(detectors, installerRegistry, ui);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(1)); // Cancel almost immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
            await orchestrator.RunAsync(new SetupOptions(), cts.Token));
    }

    #endregion
}
