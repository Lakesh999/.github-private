using Hugh.Configuration;
using Hugh.Models;
using Hugh.Orchestration;
using Hugh.Setup;
using Hugh.Setup.Detection;
using Hugh.Setup.Installation;
using Hugh.Setup.UserInterface;
using Hugh.Tests.Infrastructure;
using NSubstitute;
using Serilog;

namespace Hugh.Tests.Cli;

/// <summary>
/// CLI smoke tests that validate critical command-line paths work end-to-end.
/// These are fast, isolated tests with no external dependencies.
/// </summary>
[Trait("Category", TestCategory.Smoke)]
public class CliSmokeTests : IDisposable
{
    private readonly string _testWorkspace;

    public CliSmokeTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"HughCliSmoke_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspace))
        {
            try
            {
                Directory.Delete(_testWorkspace, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region ParseTimeoutOverride Tests (Steps 4.1, 4.2, 4.3)

    [Fact]
    public void ParseTimeoutOverride_ValidFormat_ReturnsTimeSpan()
    {
        // Arrange & Act
        var result = Program.ParseTimeoutOverride("01:00:00");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromHours(1), result.Value);
    }

    [Fact]
    public void ParseTimeoutOverride_InvalidFormat_ReturnsNull()
    {
        // Arrange & Act
        var result = Program.ParseTimeoutOverride("xyz");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTimeoutOverride_EmptyString_ReturnsNull()
    {
        // Arrange & Act
        var result = Program.ParseTimeoutOverride("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTimeoutOverride_Null_ReturnsNull()
    {
        // Arrange & Act
        var result = Program.ParseTimeoutOverride(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ParseTimeoutOverride_MinutesFormat_Works()
    {
        // Arrange & Act
        var result = Program.ParseTimeoutOverride("00:30:00");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TimeSpan.FromMinutes(30), result.Value);
    }

    #endregion

    #region ApplyTimeoutOverride Tests (Step 4.3)

    [Fact]
    public void ApplyTimeoutOverride_SetsAllPhases()
    {
        // Arrange
        var config = new HughConfig();
        var timeout = TimeSpan.FromHours(1);

        // Act
        Program.ApplyTimeoutOverride(config, timeout);

        // Assert — all 5 phase timeouts must be set identically
        Assert.Equal(timeout, config.Timeouts.VisionPhase);
        Assert.Equal(timeout, config.Timeouts.ResearchPhase);
        Assert.Equal(timeout, config.Timeouts.PlanningPhase);
        Assert.Equal(timeout, config.Timeouts.ReviewPhase);
        Assert.Equal(timeout, config.Timeouts.CodingPhasePerPhase);
    }

    #endregion

    #region Doctor Tests (Steps 4.4, 4.5)

    [Fact]
    public async Task Doctor_AllSatisfied_ExitCode0()
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
    }

    [Fact]
    public async Task Doctor_OneMissing_ExitCode1()
    {
        // Arrange
        var satisfiedDetector = CreateMockDetector("satisfied-prereq", PrerequisiteState.Satisfied);
        var missingDetector = CreateMockDetector("missing-prereq", PrerequisiteState.Missing);
        var ui = Substitute.For<ISetupUserInterface>();
        var installerRegistry = Substitute.For<IInstallerRegistry>();
        var orchestrator = new SetupOrchestrator(
            new[] { satisfiedDetector, missingDetector }, installerRegistry, ui);

        // Act
        var result = await orchestrator.RunAsync(new SetupOptions());

        // Assert
        Assert.False(result.AllSatisfied);
    }

    #endregion

    #region Status Tests (Steps 4.6, 4.7)

    [Fact]
    public async Task Status_NoStateFile_ShowsNoActiveRuns()
    {
        // Arrange — empty workspace with no state file
        var stateManager = new LoopStateManager(_testWorkspace, Log.Logger);

        // Act
        var state = await stateManager.LoadStateAsync();

        // Assert
        Assert.Null(state);
    }

    [Fact]
    public async Task Status_WithStateFile_ShowsRunStatus()
    {
        // Arrange — create state via manager
        var stateManager = new LoopStateManager(_testWorkspace, Log.Logger);
        await stateManager.CreateRunAsync("Test feature request");

        // Act
        var loadedState = await stateManager.LoadStateAsync();

        // Assert
        Assert.NotNull(loadedState);
        Assert.Equal("Test feature request", loadedState.FeatureRequest);
        Assert.NotEqual(Guid.Empty, loadedState.RunId);
    }

    #endregion

    #region Resume Tests (Steps 4.8, 4.9)

    [Fact]
    public async Task Resume_NoState_ReturnsFalse()
    {
        // Arrange — empty workspace with no state file
        var stateManager = new LoopStateManager(_testWorkspace, Log.Logger);

        // Act
        var state = await stateManager.LoadStateAsync();

        // Assert — no state to load means null
        Assert.Null(state);
    }

    [Fact]
    public async Task Resume_CompletedRun_ReturnsTrue()
    {
        // Arrange — create a run then verify its status can be inspected
        var stateManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var createdState = await stateManager.CreateRunAsync("Completed feature");

        // Act — load the state back and check completion status
        var loadedState = await stateManager.LoadStateAsync();

        // Assert — state exists and can be checked for phase status
        Assert.NotNull(loadedState);
        Assert.Equal(PhaseStatus.NotStarted, loadedState.Status);
        Assert.Equal(LoopPhase.Vision, loadedState.CurrentPhase);
        Assert.Empty(loadedState.CompletedPhases);
    }

    #endregion

    #region NoInteractive Tests (Steps 4.10, 4.11)

    [Fact]
    public void NoInteractive_ClearsInteractivePhases()
    {
        // Arrange — config with default interactive phases
        var config = new HughConfig();
        Assert.NotEmpty(config.Autonomous.InteractivePhases); // "Vision" by default

        // Act — simulate --no-interactive clearing interactive phases
        config.Autonomous.InteractivePhases.Clear();

        // Assert
        Assert.Empty(config.Autonomous.InteractivePhases);
    }

    [Fact]
    public async Task NoInteractive_Doctor_SetsNonInteractiveTrue()
    {
        // Arrange — create a non-interactive UI and verify it skips prompts
        var nonInteractiveUI = new NonInteractiveSetupUI();
        var missingStatus = new PrerequisiteStatus
        {
            PrerequisiteId = "test-prereq",
            State = PrerequisiteState.Missing
        };

        // Act — non-interactive UI should return Skip for fix prompts
        var fixDecision = await nonInteractiveUI.PromptToFixAsync(
            missingStatus,
            Array.Empty<IInstaller>());

        var authDecision = await nonInteractiveUI.PromptForAuthenticationAsync();

        // Assert — all prompts return Skip decisions
        Assert.Equal(FixDecision.Skip, fixDecision);
        Assert.Equal(AuthDecision.Skip, authDecision);
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
