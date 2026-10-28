using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Models;
using Hugh.Orchestration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace Hugh.Tests.Orchestration;

/// <summary>
/// Tests for per-sub-phase build verification logic in
/// LoopOrchestrator.ExecuteCodingPhasesAsync.
/// Uses a testable subclass that overrides RunShellCommandAsync and
/// RunRepairPhaseAsync to control build/repair outcomes without
/// invoking real shell processes.
/// </summary>
public class SubPhaseBuildVerificationTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly LoopStateManager _stateManager;
    private readonly PlanPhaseParser _planPhaseParser;
    private readonly string _planPath;
    private readonly string _agentsDir;

    /// <summary>
    /// Plan content with a single incomplete phase for predictable loop behavior.
    /// After the mock CopilotService executes the coding sub-phase,
    /// it rewrites the plan so FindNextIncompletePhase returns null.
    /// </summary>
    private const string IncompletePlanContent = """
        # Test Plan

        ## Progress Summary

        | Phase | Description | Status | Tasks |
        |-------|-------------|--------|-------|
        | 1 | Setup | ⏳ Not Started | 0/1 |
        """;

    private const string CompletePlanContent = """
        # Test Plan

        ## Progress Summary

        | Phase | Description | Status | Tasks |
        |-------|-------------|--------|-------|
        | 1 | Setup | ✅ Complete | 1/1 |
        """;

    public SubPhaseBuildVerificationTests()
    {
        _testWorkspace = Path.Combine(
            Path.GetTempPath(), $"SubPhaseBuildVerificationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);

        // Create required subdirectories
        _agentsDir = Path.Combine(_testWorkspace, "agents");
        Directory.CreateDirectory(_agentsDir);
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "vision"));
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "research"));
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "plans"));

        // Create the plan file
        _planPath = Path.Combine(_testWorkspace, "docs", "plans", "test-plan.md");
        File.WriteAllText(_planPath, IncompletePlanContent);

        _stateManager = new LoopStateManager(_testWorkspace, Log.Logger);
        _planPhaseParser = new PlanPhaseParser();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspace))
        {
            try { Directory.Delete(_testWorkspace, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    #region Helper Methods

    /// <summary>
    /// Creates a BuildVerificationTestOrchestrator wired with mocked dependencies.
    /// The mock ICopilotService marks the plan complete on the first call so the
    /// coding sub-phase loop exits after one iteration.
    /// </summary>
    private async Task<BuildVerificationTestOrchestrator> CreateOrchestratorAsync(
        HughConfig config,
        Func<int, (bool Success, string Output)> shellResultProvider,
        Func<int, PhaseResult>? repairResultProvider = null)
    {
        // Initialize state manager
        await _stateManager.CreateRunAsync("test feature request");

        var options = Options.Create(config);

        // Mock ICopilotService: on first call, mark plan complete and return success
        var mockCopilot = Substitute.For<ICopilotService>();
        mockCopilot
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                // Mark the plan as complete so the while loop exits
                File.WriteAllText(_planPath, CompletePlanContent);
                return new PhaseResult
                {
                    Success = true,
                    Duration = TimeSpan.FromSeconds(1)
                };
            });

        // Mock IAgentRegistry
        var mockRegistry = Substitute.For<IAgentRegistry>();
        mockRegistry.GetAgent(LoopPhase.Coding).Returns(new AgentDefinition
        {
            Name = "pch-coder",
            BuiltInPromptName = "coding",
            Phase = LoopPhase.Coding,
            DefaultTimeout = TimeSpan.FromMinutes(10)
        });

        // Real PhaseExecutor with mocked Copilot service
        var phaseExecutor = new PhaseExecutor(
            mockCopilot, mockRegistry, _stateManager,
            options, NullLogger<PhaseExecutor>.Instance, new CancellationTokenSource());

        return new BuildVerificationTestOrchestrator(
            phaseExecutor,
            _stateManager,
            _planPhaseParser,
            options,
            NullLogger<LoopOrchestrator>.Instance,
            shellResultProvider,
            repairResultProvider);
    }

    /// <summary>
    /// Creates a LoopState positioned at the Coding phase with all prior phases complete.
    /// </summary>
    private LoopState CreateCodingState() => new()
    {
        RunId = Guid.NewGuid(),
        StartedAt = DateTime.UtcNow,
        FeatureRequest = "test feature",
        CurrentPhase = LoopPhase.Coding,
        CompletedPhases = new List<LoopPhase>
        {
            LoopPhase.Vision,
            LoopPhase.Research,
            LoopPhase.Planning,
            LoopPhase.Review
        },
        Documents = new DocumentPaths { Plan = _planPath },
        Status = PhaseStatus.NotStarted
    };

    #endregion

    #region Tests

    [Fact]
    public async Task BuildVerification_SkippedWhenVerifyBuildPerSubPhaseDisabled()
    {
        // Arrange: VerifyBuildPerSubPhase = false
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Coding = new CodingConfig { Granularity = "phase" },
            Verification = new VerificationConfig
            {
                Enabled = true,
                VerifyBuildPerSubPhase = false,
                BuildCommand = "dotnet build"
            }
        };

        var orchestrator = await CreateOrchestratorAsync(
            config,
            shellResultProvider: _ => (true, "Build succeeded"));

        var state = CreateCodingState();

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: build verification never called during sub-phase
        // (post-coding verification may still call RunShellCommandAsync,
        //  so we check that sub-phase count is 0)
        Assert.True(result);
        Assert.Equal(0, orchestrator.SubPhaseBuildCallCount);
    }

    [Fact]
    public async Task BuildVerification_RunsAndPassesWhenEnabled()
    {
        // Arrange: all verification flags enabled, build succeeds
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Coding = new CodingConfig { Granularity = "phase" },
            Verification = new VerificationConfig
            {
                Enabled = true,
                VerifyBuildPerSubPhase = true,
                BuildCommand = "dotnet build"
            }
        };

        var orchestrator = await CreateOrchestratorAsync(
            config,
            shellResultProvider: _ => (true, "Build succeeded"));

        var state = CreateCodingState();

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: sub-phase build verification ran and passed, overall success
        Assert.True(result);
        Assert.Equal(1, orchestrator.SubPhaseBuildCallCount);
        Assert.Equal(0, orchestrator.RepairCallCount);
    }

    [Fact]
    public async Task BuildVerification_TriggersRepairOnBuildFailure()
    {
        // Arrange: build fails, repair succeeds
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Coding = new CodingConfig { Granularity = "phase" },
            Verification = new VerificationConfig
            {
                Enabled = true,
                VerifyBuildPerSubPhase = true,
                BuildCommand = "dotnet build"
            }
        };

        var orchestrator = await CreateOrchestratorAsync(
            config,
            shellResultProvider: _ => (false, "error CS1234: Something broke"),
            repairResultProvider: _ => new PhaseResult
            {
                Success = true,
                Duration = TimeSpan.FromSeconds(2)
            });

        var state = CreateCodingState();

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: build failed, repair was triggered and succeeded
        Assert.True(result);
        Assert.Equal(1, orchestrator.SubPhaseBuildCallCount);
        Assert.Equal(1, orchestrator.RepairCallCount);
    }

    [Fact]
    public async Task BuildVerification_RepairFailureReturnsErrorResult()
    {
        // Arrange: build fails, repair also fails
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Coding = new CodingConfig { Granularity = "phase" },
            Verification = new VerificationConfig
            {
                Enabled = true,
                VerifyBuildPerSubPhase = true,
                BuildCommand = "dotnet build"
            }
        };

        var orchestrator = await CreateOrchestratorAsync(
            config,
            shellResultProvider: _ => (false, "error CS1234: Something broke"),
            repairResultProvider: _ => new PhaseResult
            {
                Success = false,
                ErrorMessage = "Repair agent could not fix the build errors",
                Duration = TimeSpan.FromSeconds(3)
            });

        var state = CreateCodingState();

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: overall failure because repair failed
        Assert.False(result);
        Assert.Equal(1, orchestrator.SubPhaseBuildCallCount);
        Assert.Equal(1, orchestrator.RepairCallCount);
    }

    [Fact]
    public async Task BuildVerification_SkippedWhenBuildCommandEmpty()
    {
        // Arrange: BuildCommand is empty (no build tool configured)
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Coding = new CodingConfig { Granularity = "phase" },
            Verification = new VerificationConfig
            {
                Enabled = true,
                VerifyBuildPerSubPhase = true,
                BuildCommand = ""
            }
        };

        var orchestrator = await CreateOrchestratorAsync(
            config,
            shellResultProvider: _ => (true, "Build succeeded"));

        var state = CreateCodingState();

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: no build verification when BuildCommand is empty
        Assert.True(result);
        Assert.Equal(0, orchestrator.SubPhaseBuildCallCount);
    }

    [Fact]
    public async Task BuildVerification_SkippedWhenVerificationDisabled()
    {
        // Arrange: Verification.Enabled = false
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Coding = new CodingConfig { Granularity = "phase" },
            Verification = new VerificationConfig
            {
                Enabled = false,
                VerifyBuildPerSubPhase = true,
                BuildCommand = "dotnet build"
            }
        };

        var orchestrator = await CreateOrchestratorAsync(
            config,
            shellResultProvider: _ => (true, "Build succeeded"));

        var state = CreateCodingState();

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: no build verification when Verification.Enabled is false
        Assert.True(result);
        Assert.Equal(0, orchestrator.SubPhaseBuildCallCount);
    }

    #endregion

    #region Testable Subclass

    /// <summary>
    /// Test subclass of LoopOrchestrator that overrides RunShellCommandAsync,
    /// RunRepairPhaseAsync, and RunPostCodingVerificationAsync for controlled
    /// testing of per-sub-phase build verification without real shell execution.
    /// Post-coding verification is disabled (no-op) to isolate sub-phase behavior.
    /// </summary>
    private sealed class BuildVerificationTestOrchestrator : LoopOrchestrator
    {
        private readonly Func<int, (bool Success, string Output)> _shellResultProvider;
        private readonly Func<int, PhaseResult>? _repairResultProvider;
        private int _shellCallCount;
        private int _repairCallCount;

        /// <summary>
        /// Number of times RunShellCommandAsync was called during per-sub-phase
        /// build verification (post-coding verification is overridden as no-op).
        /// </summary>
        public int SubPhaseBuildCallCount => _shellCallCount;

        /// <summary>
        /// Total number of times RunRepairPhaseAsync was called.
        /// </summary>
        public int RepairCallCount => _repairCallCount;

        public BuildVerificationTestOrchestrator(
            PhaseExecutor phaseExecutor,
            LoopStateManager stateManager,
            PlanPhaseParser planPhaseParser,
            IOptions<HughConfig> options,
            ILogger<LoopOrchestrator> logger,
            Func<int, (bool Success, string Output)> shellResultProvider,
            Func<int, PhaseResult>? repairResultProvider = null)
            : base(phaseExecutor, stateManager, planPhaseParser, new ResearchPhaseParser(), new PlanCreationPhaseParser(), new TaskParser(), options, logger)
        {
            _shellResultProvider = shellResultProvider;
            _repairResultProvider = repairResultProvider;
        }

        protected override Task<(bool Success, string Output)> RunShellCommandAsync(
            string command, CancellationToken ct)
        {
            _shellCallCount++;
            return Task.FromResult(_shellResultProvider(_shellCallCount));
        }

        protected override Task<PhaseResult> RunRepairPhaseAsync(
            string planPath, string failureType, string errorOutput, CancellationToken ct)
        {
            _repairCallCount++;
            var result = _repairResultProvider?.Invoke(_repairCallCount)
                ?? new PhaseResult { Success = true, Duration = TimeSpan.FromSeconds(1) };
            return Task.FromResult(result);
        }

        /// <summary>
        /// No-op override to isolate per-sub-phase build verification from
        /// the post-coding verification cycle.
        /// </summary>
        protected override Task<PhaseResult?> RunPostCodingVerificationAsync(
            string planPath, TimeSpan accumulatedDuration, CancellationToken ct)
        {
            return Task.FromResult<PhaseResult?>(null);
        }
    }

    #endregion
}
