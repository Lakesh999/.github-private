using Hugh.Setup;
using Hugh.Setup.Detection;
using Hugh.Setup.Installation;
using Hugh.Setup.UserInterface;
using NSubstitute;

namespace Hugh.Tests.Setup;

/// <summary>
/// Unit tests for SetupOrchestrator.
/// Tests the workflow of detection → prompt → install → verify cycle.
/// </summary>
public class SetupOrchestratorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullDetectors_ThrowsArgumentNullException()
    {
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var ui = Substitute.For<ISetupUserInterface>();

        Assert.Throws<ArgumentNullException>(() =>
            new SetupOrchestrator(null!, installerRegistry, ui));
    }

    [Fact]
    public void Constructor_WithNullInstallerRegistry_ThrowsArgumentNullException()
    {
        var detectors = Array.Empty<IPrerequisiteDetector>();
        var ui = Substitute.For<ISetupUserInterface>();

        Assert.Throws<ArgumentNullException>(() =>
            new SetupOrchestrator(detectors, null!, ui));
    }

    [Fact]
    public void Constructor_WithNullUserInterface_ThrowsArgumentNullException()
    {
        var detectors = Array.Empty<IPrerequisiteDetector>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();

        Assert.Throws<ArgumentNullException>(() =>
            new SetupOrchestrator(detectors, installerRegistry, null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var detectors = Array.Empty<IPrerequisiteDetector>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var ui = Substitute.For<ISetupUserInterface>();

        var orchestrator = new SetupOrchestrator(detectors, installerRegistry, ui);

        Assert.NotNull(orchestrator);
    }

    #endregion

    #region RunAsync Tests - Basic Detection

    [Fact]
    public async Task RunAsync_WithNoDetectors_ReturnsAllSatisfied()
    {
        // Arrange
        var detectors = Array.Empty<IPrerequisiteDetector>();
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(detectors, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions());

        // Assert
        Assert.True(result.AllSatisfied);
        Assert.Empty(result.Statuses);
    }

    [Fact]
    public async Task RunAsync_WithSingleSatisfiedDetector_ReturnsAllSatisfied()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Satisfied);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions());

        // Assert
        Assert.True(result.AllSatisfied);
        Assert.Single(result.Statuses);
        Assert.Equal(PrerequisiteState.Satisfied, result.Statuses[0].State);
    }

    [Fact]
    public async Task RunAsync_WithMissingDetector_ReturnsNotAllSatisfied()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Missing);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions());

        // Assert
        Assert.False(result.AllSatisfied);
        Assert.Single(result.Statuses);
        Assert.Equal(PrerequisiteState.Missing, result.Statuses[0].State);
    }

    [Fact]
    public async Task RunAsync_DisplaysPrerequisiteReport()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Satisfied);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions());

        // Assert
        ui.Received(1).ShowPrerequisiteReport(Arg.Any<IReadOnlyList<PrerequisiteStatus>>());
    }

    #endregion

    #region RunAsync Tests - Priority Ordering

    [Fact]
    public async Task RunAsync_DetectorsRunInPriorityOrder()
    {
        // Arrange
        var callOrder = new List<string>();

        var detector1 = CreateMockDetector("high-priority", PrerequisiteState.Satisfied, priority: 10);
        detector1.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add("high-priority");
                return Task.FromResult(new PrerequisiteStatus
                {
                    PrerequisiteId = "high-priority",
                    State = PrerequisiteState.Satisfied
                });
            });

        var detector2 = CreateMockDetector("low-priority", PrerequisiteState.Satisfied, priority: 50);
        detector2.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                callOrder.Add("low-priority");
                return Task.FromResult(new PrerequisiteStatus
                {
                    PrerequisiteId = "low-priority",
                    State = PrerequisiteState.Satisfied
                });
            });

        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();

        // Pass detector2 first to verify sorting happens
        var orchestrator = new SetupOrchestrator(new[] { detector2, detector1 }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions());

        // Assert
        Assert.Equal(new[] { "high-priority", "low-priority" }, callOrder);
    }

    #endregion

    #region RunAsync Tests - Fix Mode Disabled

    [Fact]
    public async Task RunAsync_WhenFixDisabled_DoesNotPromptToFix()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Missing);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions { Fix = false });

        // Assert
        await ui.DidNotReceive().PromptToFixAsync(
            Arg.Any<PrerequisiteStatus>(),
            Arg.Any<IReadOnlyList<IInstaller>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenFixDisabledAndAllSatisfied_ReturnsTrue()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Satisfied);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = false });

        // Assert
        Assert.True(result.AllSatisfied);
        Assert.Null(result.InstallationResults);
    }

    #endregion

    #region RunAsync Tests - Fix Mode Enabled

    [Fact]
    public async Task RunAsync_WhenFixEnabledAndAllSatisfied_DoesNotAttempInstall()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Satisfied);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        Assert.True(result.AllSatisfied);
        await installerRegistry.DidNotReceive().GetAvailableInstallersAsync(
            Arg.Any<string>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenFixEnabledAndMissing_QueriesInstallerRegistry()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Missing);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IInstaller>());
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        await installerRegistry.Received(1).GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenNoInstallersAvailable_ShowsManualInstructions()
    {
        // Arrange
        var status = new PrerequisiteStatus
        {
            PrerequisiteId = "test-prereq",
            State = PrerequisiteState.Missing,
            Hints = new[] { "Install manually from source" }
        };
        var detector = Substitute.For<IPrerequisiteDetector>();
        detector.PrerequisiteId.Returns("test-prereq");
        detector.Priority.Returns(10);
        detector.DetectAsync(Arg.Any<CancellationToken>()).Returns(status);

        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(Array.Empty<IInstaller>());

        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        ui.Received(1).ShowManualInstructions("test-prereq", Arg.Is<string[]>(h => h.Contains("Install manually from source")));
    }

    [Fact]
    public async Task RunAsync_WhenUserChoosesSkip_ContinuesToNextDetector()
    {
        // Arrange
        var detector1 = CreateMockDetector("prereq-1", PrerequisiteState.Missing);
        var detector2 = CreateMockDetector("prereq-2", PrerequisiteState.Satisfied, priority: 20);

        var ui = Substitute.For<ISetupUserInterface>();
        ui.PromptToFixAsync(Arg.Any<PrerequisiteStatus>(), Arg.Any<IReadOnlyList<IInstaller>>(), Arg.Any<CancellationToken>())
            .Returns(FixDecision.Skip);

        var installer = Substitute.For<IInstaller>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("prereq-1", Arg.Any<CancellationToken>())
            .Returns(new[] { installer });

        var orchestrator = new SetupOrchestrator(new[] { detector1, detector2 }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        Assert.False(result.AllSatisfied);
        Assert.Equal(2, result.Statuses.Count);
    }

    [Fact]
    public async Task RunAsync_WhenUserChoosesAbort_StopsImmediately()
    {
        // Arrange
        var detector1 = CreateMockDetector("prereq-1", PrerequisiteState.Missing, priority: 10);
        var detector2 = CreateMockDetector("prereq-2", PrerequisiteState.Missing, priority: 20);

        var ui = Substitute.For<ISetupUserInterface>();
        ui.PromptToFixAsync(Arg.Any<PrerequisiteStatus>(), Arg.Any<IReadOnlyList<IInstaller>>(), Arg.Any<CancellationToken>())
            .Returns(FixDecision.Abort);

        var installer = Substitute.For<IInstaller>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new[] { installer });

        var orchestrator = new SetupOrchestrator(new[] { detector1, detector2 }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        Assert.False(result.AllSatisfied);
        // Only prereq-1 should have been processed
        await installerRegistry.Received(1).GetAvailableInstallersAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RunAsync_WhenUserChoosesInstall_InstallsAndVerifies()
    {
        // Arrange
        var callCount = 0;
        var detector = Substitute.For<IPrerequisiteDetector>();
        detector.PrerequisiteId.Returns("test-prereq");
        detector.Priority.Returns(10);
        detector.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                // First call: Missing, Second call: Satisfied (after install)
                return Task.FromResult(new PrerequisiteStatus
                {
                    PrerequisiteId = "test-prereq",
                    State = callCount == 1 ? PrerequisiteState.Missing : PrerequisiteState.Satisfied
                });
            });

        var ui = Substitute.For<ISetupUserInterface>();
        ui.PromptToFixAsync(Arg.Any<PrerequisiteStatus>(), Arg.Any<IReadOnlyList<IInstaller>>(), Arg.Any<CancellationToken>())
            .Returns(FixDecision.Install);

        var installer = Substitute.For<IInstaller>();
        installer.InstallerId.Returns("test-installer");
        installer.InstallAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new InstallationResult { Success = true, Version = "1.0.0" });

        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new[] { installer });

        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        Assert.True(result.AllSatisfied);
        Assert.NotNull(result.InstallationResults);
        Assert.Single(result.InstallationResults);
        Assert.True(result.InstallationResults[0].Success);

        // Verify install was called
        await installer.Received(1).InstallAsync("test-prereq", Arg.Any<CancellationToken>());

        // Verify re-detection happened
        Assert.Equal(2, callCount);
    }

    #endregion

    #region RunAsync Tests - NonInteractive Mode

    [Fact]
    public async Task RunAsync_WhenNonInteractive_AutoInstallsWithoutPrompt()
    {
        // Arrange
        var callCount = 0;
        var detector = Substitute.For<IPrerequisiteDetector>();
        detector.PrerequisiteId.Returns("test-prereq");
        detector.Priority.Returns(10);
        detector.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(new PrerequisiteStatus
                {
                    PrerequisiteId = "test-prereq",
                    State = callCount == 1 ? PrerequisiteState.Missing : PrerequisiteState.Satisfied
                });
            });

        var ui = Substitute.For<ISetupUserInterface>();

        var installer = Substitute.For<IInstaller>();
        installer.InstallerId.Returns("test-installer");
        installer.InstallAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new InstallationResult { Success = true });

        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new[] { installer });

        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions { Fix = true, NonInteractive = true });

        // Assert - PromptToFixAsync should NOT be called in non-interactive mode
        await ui.DidNotReceive().PromptToFixAsync(
            Arg.Any<PrerequisiteStatus>(),
            Arg.Any<IReadOnlyList<IInstaller>>(),
            Arg.Any<CancellationToken>());

        // But installer should still be called
        await installer.Received(1).InstallAsync("test-prereq", Arg.Any<CancellationToken>());
    }

    #endregion

    #region RunAsync Tests - Installation Result Display

    [Fact]
    public async Task RunAsync_WhenInstallSucceeds_ShowsInstallationResult()
    {
        // Arrange
        var callCount = 0;
        var detector = Substitute.For<IPrerequisiteDetector>();
        detector.PrerequisiteId.Returns("test-prereq");
        detector.Priority.Returns(10);
        detector.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                callCount++;
                return Task.FromResult(new PrerequisiteStatus
                {
                    PrerequisiteId = "test-prereq",
                    State = callCount == 1 ? PrerequisiteState.Missing : PrerequisiteState.Satisfied
                });
            });

        var ui = Substitute.For<ISetupUserInterface>();
        ui.PromptToFixAsync(Arg.Any<PrerequisiteStatus>(), Arg.Any<IReadOnlyList<IInstaller>>(), Arg.Any<CancellationToken>())
            .Returns(FixDecision.Install);

        var installer = Substitute.For<IInstaller>();
        installer.InstallerId.Returns("test-installer");
        installer.InstallAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new InstallationResult { Success = true, Version = "2.0.0" });

        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new[] { installer });

        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        ui.Received(1).ShowInstallationProgress("test-prereq", Arg.Any<string>());
        ui.Received(1).ShowInstallationResult("test-prereq", Arg.Any<InstallationResult>());
    }

    [Fact]
    public async Task RunAsync_WhenInstallFails_StillShowsResult()
    {
        // Arrange
        var detector = Substitute.For<IPrerequisiteDetector>();
        detector.PrerequisiteId.Returns("test-prereq");
        detector.Priority.Returns(10);
        detector.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(new PrerequisiteStatus
            {
                PrerequisiteId = "test-prereq",
                State = PrerequisiteState.Missing
            });

        var ui = Substitute.For<ISetupUserInterface>();
        ui.PromptToFixAsync(Arg.Any<PrerequisiteStatus>(), Arg.Any<IReadOnlyList<IInstaller>>(), Arg.Any<CancellationToken>())
            .Returns(FixDecision.Install);

        var installer = Substitute.For<IInstaller>();
        installer.InstallerId.Returns("test-installer");
        installer.InstallAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new InstallationResult { Success = false, ErrorMessage = "Installation failed" });

        var installerRegistry = Substitute.For<IInstallerRegistry>();
        installerRegistry.GetAvailableInstallersAsync("test-prereq", Arg.Any<CancellationToken>())
            .Returns(new[] { installer });

        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        // Act
        await orchestrator.RunAsync(new SetupOptions { Fix = true });

        // Assert
        ui.Received().ShowInstallationResult("test-prereq", Arg.Is<InstallationResult>(r => !r.Success));
    }

    #endregion

    #region RunAsync Tests - Cancellation

    [Fact]
    public async Task RunAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var detector = CreateMockDetector("test-prereq", PrerequisiteState.Satisfied);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(new[] { detector }, installerRegistry, ui);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await orchestrator.RunAsync(new SetupOptions(), cts.Token));
    }

    #endregion

    #region Helper Methods

    private static IPrerequisiteDetector CreateMockDetector(
        string prerequisiteId,
        PrerequisiteState state,
        int priority = 10,
        string? version = null)
    {
        var detector = Substitute.For<IPrerequisiteDetector>();
        detector.PrerequisiteId.Returns(prerequisiteId);
        detector.Priority.Returns(priority);
        detector.DetectAsync(Arg.Any<CancellationToken>())
            .Returns(new PrerequisiteStatus
            {
                PrerequisiteId = prerequisiteId,
                State = state,
                Version = version
            });

        return detector;
    }

    #endregion
}
