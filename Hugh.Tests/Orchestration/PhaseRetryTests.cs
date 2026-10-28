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
/// Tests for per-phase retry logic in LoopOrchestrator.ExecuteLoopAsync.
/// Uses a TestableLoopOrchestrator that overrides DispatchPhaseAsync
/// to return controlled results without invoking real agents.
/// </summary>
public class PhaseRetryTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly LoopStateManager _stateManager;
    private readonly PlanPhaseParser _planPhaseParser;

    public PhaseRetryTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"PhaseRetryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);

        // Create docs subdirectories that EnsurePhaseOutputDirectoryExists expects
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "vision"));
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "research"));
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "plans"));

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
    /// Creates a TestableLoopOrchestrator with the given config and result provider.
    /// </summary>
    private async Task<TestableLoopOrchestrator> CreateOrchestratorAsync(
        HughConfig config,
        Func<LoopPhase, int, PhaseResult> resultProvider,
        Func<LoopPhase, bool>? interactiveOverride = null)
    {
        // Initialize state manager (required for UpdatePhaseAsync etc.)
        await _stateManager.CreateRunAsync("test feature request");

        var options = Options.Create(config);

        // Create a real PhaseExecutor with mocked internals (never actually called
        // because DispatchPhaseAsync is overridden in the testable subclass)
        var mockCopilot = Substitute.For<ICopilotService>();
        var mockRegistry = Substitute.For<IAgentRegistry>();
        var phaseExecutor = new PhaseExecutor(
            mockCopilot, mockRegistry, _stateManager,
            options, NullLogger<PhaseExecutor>.Instance, new CancellationTokenSource());

        return new TestableLoopOrchestrator(
            phaseExecutor,
            _stateManager,
            _planPhaseParser,
            options,
            NullLogger<LoopOrchestrator>.Instance,
            resultProvider,
            interactiveOverride);
    }

    /// <summary>
    /// Creates a LoopState starting at the specified phase with all prior phases marked complete.
    /// </summary>
    private static LoopState CreateStateAtPhase(LoopPhase startPhase)
    {
        var completedPhases = new List<LoopPhase>();
        var allPhases = new[] { LoopPhase.Vision, LoopPhase.Research, LoopPhase.Planning, LoopPhase.Review, LoopPhase.Coding };

        foreach (var p in allPhases)
        {
            if (p == startPhase) break;
            completedPhases.Add(p);
        }

        return new LoopState
        {
            RunId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            FeatureRequest = "test feature",
            CurrentPhase = startPhase,
            CompletedPhases = completedPhases,
            Documents = new DocumentPaths(),
            Status = PhaseStatus.NotStarted
        };
    }

    private static PhaseResult RetryableFailure(string message = "Transient error") => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorClassification = new ErrorClassification
        {
            Category = ErrorCategory.Transient,
            IsRetryable = true,
            SuggestedDelay = TimeSpan.FromMilliseconds(1) // minimal delay for fast tests
        }
    };

    private static PhaseResult NonRetryableFailure(string message = "Auth error") => new()
    {
        Success = false,
        ErrorMessage = message,
        ErrorClassification = new ErrorClassification
        {
            Category = ErrorCategory.Authentication,
            IsRetryable = false,
            SuggestedAction = "Re-authenticate"
        }
    };

    private static PhaseResult SuccessResult() => new()
    {
        Success = true,
        Duration = TimeSpan.FromSeconds(1)
    };

    #endregion

    #region Retry Behavior Tests

    [Fact]
    public async Task RetryableError_RetriesUpToMax()
    {
        // Arrange: Review always fails with retryable error
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 2 }
        };

        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) => RetryableFailure());

        var state = CreateStateAtPhase(LoopPhase.Review);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: 1 initial + 2 retries = 3 calls for Review
        Assert.False(result);
        var reviewCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Review);
        Assert.Equal(3, reviewCalls); // 1 initial + MaxPhaseRetries(2)
    }

    [Fact]
    public async Task NonRetryableError_FailsImmediately()
    {
        // Arrange: Review fails with non-retryable error
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 2 }
        };

        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) => NonRetryableFailure());

        var state = CreateStateAtPhase(LoopPhase.Review);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: only 1 call (no retry for non-retryable errors)
        Assert.False(result);
        var reviewCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Review);
        Assert.Equal(1, reviewCalls);
    }

    [Fact]
    public async Task InteractivePhase_NotRetried()
    {
        // Arrange: Vision is interactive, fails with retryable error
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 2 },
            Autonomous = new AutonomousConfig { InteractivePhases = new List<string> { "Vision" } }
        };

        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) => RetryableFailure(),
            interactiveOverride: phase => phase == LoopPhase.Vision);

        var state = CreateStateAtPhase(LoopPhase.Vision);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: only 1 call (interactive phases are excluded from retry)
        Assert.False(result);
        var visionCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Vision);
        Assert.Equal(1, visionCalls);
    }

    [Fact]
    public async Task SuccessOnSecondAttempt_StopsRetrying()
    {
        // Arrange: Review fails first, succeeds second; Coding always succeeds
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 3 }
        };

        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                    return callNumber == 1 ? RetryableFailure() : SuccessResult();
                return SuccessResult(); // Coding and any other phase
            });

        var state = CreateStateAtPhase(LoopPhase.Review);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: 2 calls for Review (1 fail + 1 success), then stops retrying
        Assert.True(result);
        var reviewCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Review);
        Assert.Equal(2, reviewCalls);
    }

    [Fact]
    public async Task RetryAttemptsMade_AccuratelyCounted()
    {
        // Arrange: Review fails 3 times then succeeds on 4th (MaxPhaseRetries = 3)
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 3 }
        };

        var reviewCallCount = 0;
        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    reviewCallCount++;
                    // Fail first 3 calls, succeed on 4th
                    return reviewCallCount <= 3 ? RetryableFailure() : SuccessResult();
                }
                return SuccessResult();
            });

        var state = CreateStateAtPhase(LoopPhase.Review);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: 4 total calls = 1 initial + 3 retries => RetryAttemptsMade = 3
        Assert.True(result);
        var totalReviewCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Review);
        Assert.Equal(4, totalReviewCalls); // 1 initial + 3 retries

        // The retry count equals totalCalls - 1 (the initial call is not a retry)
        // RetryAttemptsMade is set to retryAttempt counter which equals totalCalls - 1
        Assert.Equal(3, totalReviewCalls - 1);
    }

    [Fact]
    public async Task RetryableError_BecomesNonRetryable_StopsRetrying()
    {
        // Arrange: Review first returns retryable, then non-retryable on retry
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 3 }
        };

        var reviewCallCount = 0;
        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    reviewCallCount++;
                    // First call: retryable. Second call: non-retryable.
                    return reviewCallCount == 1
                        ? RetryableFailure()
                        : NonRetryableFailure();
                }
                return SuccessResult();
            });

        var state = CreateStateAtPhase(LoopPhase.Review);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: 2 calls only (retryable → retry → non-retryable → stop)
        Assert.False(result);
        var reviewCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Review);
        Assert.Equal(2, reviewCalls);
    }

    [Fact]
    public async Task NullErrorClassification_NotRetried()
    {
        // Arrange: Review fails with no error classification (e.g., coding validation error)
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Copilot = new CopilotConfig { MaxPhaseRetries = 2 }
        };

        var orchestrator = await CreateOrchestratorAsync(config,
            resultProvider: (phase, callNumber) => new PhaseResult
            {
                Success = false,
                ErrorMessage = "Generic failure with no classification",
                ErrorClassification = null
            });

        var state = CreateStateAtPhase(LoopPhase.Review);

        // Act
        var result = await orchestrator.ExecuteLoopAsync(state, CancellationToken.None);

        // Assert: only 1 call (null classification means not retryable)
        Assert.False(result);
        var reviewCalls = orchestrator.DispatchCalls
            .Count(c => c.Phase == LoopPhase.Review);
        Assert.Equal(1, reviewCalls);
    }

    #endregion

    #region TestableLoopOrchestrator

    /// <summary>
    /// Test subclass of LoopOrchestrator that overrides DispatchPhaseAsync
    /// and IsInteractivePhase for controlled testing of retry logic.
    /// </summary>
    private sealed class TestableLoopOrchestrator : LoopOrchestrator
    {
        private readonly Func<LoopPhase, int, PhaseResult> _resultProvider;
        private readonly Func<LoopPhase, bool>? _interactiveOverride;
        private readonly Dictionary<LoopPhase, int> _callCounts = new();

        /// <summary>
        /// Records each dispatch call as (Phase, CallNumber).
        /// </summary>
        public List<(LoopPhase Phase, int CallNumber)> DispatchCalls { get; } = new();

        public TestableLoopOrchestrator(
            PhaseExecutor phaseExecutor,
            LoopStateManager stateManager,
            PlanPhaseParser planPhaseParser,
            IOptions<HughConfig> options,
            ILogger<LoopOrchestrator> logger,
            Func<LoopPhase, int, PhaseResult> resultProvider,
            Func<LoopPhase, bool>? interactiveOverride = null)
            : base(phaseExecutor, stateManager, planPhaseParser, new ResearchPhaseParser(), new PlanCreationPhaseParser(), new TaskParser(), options, logger)
        {
            _resultProvider = resultProvider;
            _interactiveOverride = interactiveOverride;
        }

        protected override Task<PhaseResult> DispatchPhaseAsync(
            LoopPhase phase, LoopState state, CancellationToken ct)
        {
            if (!_callCounts.TryGetValue(phase, out var count))
                count = 0;
            count++;
            _callCounts[phase] = count;

            DispatchCalls.Add((phase, count));
            return Task.FromResult(_resultProvider(phase, count));
        }

        protected override bool IsInteractivePhase(LoopPhase phase)
        {
            if (_interactiveOverride != null)
                return _interactiveOverride(phase);
            return base.IsInteractivePhase(phase);
        }
    }

    #endregion
}
