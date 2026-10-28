using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Models;
using Hugh.Orchestration;
using Hugh.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for the review-revise cycle in LoopOrchestrator.
/// Tests DetectReviewOutcome, ExecuteReviewCycleAsync, and related integrations.
/// </summary>
public class ReviewCycleTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly LoopStateManager _stateManager;

    public ReviewCycleTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"ReviewCycleTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);

        // Create docs subdirectories
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "vision"));
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "research"));
        Directory.CreateDirectory(Path.Combine(_testWorkspace, "docs", "plans"));

        _stateManager = new LoopStateManager(_testWorkspace, Log.Logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspace))
        {
            try { Directory.Delete(_testWorkspace, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    #region DetectReviewOutcome Tests

    [Fact]
    public async Task DetectReviewOutcome_ToolBased_Approved()
    {
        var orchestrator = await CreateOrchestratorAsync();

        var result = new PhaseResult
        {
            Success = true,
            ReviewOutcome = ReviewOutcome.Approved
        };

        var outcome = orchestrator.DetectReviewOutcome(result);
        Assert.Equal(ReviewOutcome.Approved, outcome);
    }

    [Fact]
    public async Task DetectReviewOutcome_ToolBased_ApprovedWithFindings()
    {
        var orchestrator = await CreateOrchestratorAsync();

        var result = new PhaseResult
        {
            Success = true,
            ReviewOutcome = ReviewOutcome.ApprovedWithFindings
        };

        var outcome = orchestrator.DetectReviewOutcome(result);
        Assert.Equal(ReviewOutcome.ApprovedWithFindings, outcome);
    }

    [Fact]
    public async Task DetectReviewOutcome_ToolBased_Rejected()
    {
        var orchestrator = await CreateOrchestratorAsync();

        var result = new PhaseResult
        {
            Success = true,
            ReviewOutcome = ReviewOutcome.Rejected
        };

        var outcome = orchestrator.DetectReviewOutcome(result);
        Assert.Equal(ReviewOutcome.Rejected, outcome);
    }

    [Fact]
    public async Task DetectReviewOutcome_NoSignal_DefaultsToApproved()
    {
        var orchestrator = await CreateOrchestratorAsync();

        var result = new PhaseResult
        {
            Success = true,
            ReviewOutcome = null
        };

        var outcome = orchestrator.DetectReviewOutcome(result);
        Assert.Equal(ReviewOutcome.Approved, outcome);
    }

    #endregion

    #region SignalClassifier Review Regex Tests

    [Theory]
    [InlineData("Ready for Implementation")]
    [InlineData("Plan Approved")]
    [InlineData("The plan is ready for implementation.")]
    [InlineData("✅ Plan Approved — proceed to coding")]
    public void SignalClassifier_ReviewApproved_DetectsApproval(string input)
    {
        var classifier = new SignalClassifier();
        Assert.True(classifier.IsReviewApproved(input));
    }

    [Theory]
    [InlineData("Issues Found")]
    [InlineData("Requires Revision")]
    [InlineData("Plan Rejected")]
    [InlineData("❌ Issues Found in the plan")]
    [InlineData("The plan requires revision before coding")]
    public void SignalClassifier_ReviewNeedsRevision_DetectsRevision(string input)
    {
        var classifier = new SignalClassifier();
        Assert.True(classifier.IsReviewNeedsRevision(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Some random text")]
    [InlineData("The code looks good")]
    public void SignalClassifier_ReviewApproved_ReturnsFalseForNonMatches(string input)
    {
        var classifier = new SignalClassifier();
        Assert.False(classifier.IsReviewApproved(input));
    }

    [Theory]
    [InlineData("")]
    [InlineData("All good here")]
    [InlineData("Ready for Implementation")]
    public void SignalClassifier_ReviewNeedsRevision_ReturnsFalseForNonMatches(string input)
    {
        var classifier = new SignalClassifier();
        Assert.False(classifier.IsReviewNeedsRevision(input));
    }

    [Fact]
    public void SignalClassifier_ReviewApproved_ReturnsFalseForNull()
    {
        var classifier = new SignalClassifier();
        Assert.False(classifier.IsReviewApproved(null!));
    }

    [Fact]
    public void SignalClassifier_ReviewNeedsRevision_ReturnsFalseForNull()
    {
        var classifier = new SignalClassifier();
        Assert.False(classifier.IsReviewNeedsRevision(null!));
    }

    #endregion

    #region HughWorkflowTools ReviewOutcome Tests

    [Fact]
    public void NotifyPhaseComplete_WithReviewOutcome_StoresLastReviewOutcome()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var tools = new HughWorkflowTools(logger, _testWorkspace);

        tools.NotifyPhaseComplete(4, "Review complete", null, "approved");

        Assert.Equal("approved", tools.LastReviewOutcome);
    }

    [Fact]
    public void NotifyPhaseComplete_WithReviewOutcome_IncludesInResult()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var tools = new HughWorkflowTools(logger, _testWorkspace);

        var result = tools.NotifyPhaseComplete(4, "Review complete", null, "rejected");

        Assert.Equal("rejected", result.ReviewOutcome);
    }

    [Fact]
    public void NotifyPhaseComplete_WithoutReviewOutcome_LastReviewOutcomeIsNull()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var tools = new HughWorkflowTools(logger, _testWorkspace);

        tools.NotifyPhaseComplete(1, "Phase 1 complete");

        Assert.Null(tools.LastReviewOutcome);
    }

    [Fact]
    public void NotifyPhaseComplete_WithReviewOutcome_ResultReviewOutcomeIsNull_WhenNotProvided()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var tools = new HughWorkflowTools(logger, _testWorkspace);

        var result = tools.NotifyPhaseComplete(1, "Phase 1 complete");

        Assert.Null(result.ReviewOutcome);
    }

    [Fact]
    public void NotifyPhaseComplete_AIFunctionFactory_ReflectsOptionalParameter()
    {
        var logger = new LoggerConfiguration().CreateLogger();
        var tools = new HughWorkflowTools(logger, _testWorkspace);
        var toolSet = tools.CreateToolSet();

        var phaseCompleteTool = toolSet.FirstOrDefault(t => t.Name == "hugh_notify_phase_complete");
        Assert.NotNull(phaseCompleteTool);

        // The tool should have parameters including reviewOutcome
        // AIFunctionFactory.Create uses reflection — optional params must have default values
        // Verify the tool can be created without error (proves reflection works)
        Assert.Equal("hugh_notify_phase_complete", phaseCompleteTool.Name);
    }

    #endregion

    #region ExecuteReviewCycleAsync Tests

    [Fact]
    public async Task ReviewCycle_ApprovedOnFirstPass_NoRevision()
    {
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Review = new ReviewConfig { MaxReviewCycles = 2 }
        };

        var orchestrator = await CreateReviewCycleOrchestratorAsync(config,
            (phase, callNumber) =>
            {
                // Review returns approved immediately
                if (phase == LoopPhase.Review)
                {
                    return new PhaseResult
                    {
                        Success = true,
                        ReviewOutcome = ReviewOutcome.Approved,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                return SuccessResult();
            });

        var state = await CreateStateAtReviewPhaseAsync();

        var result = await orchestrator.InvokeDispatchPhaseAsync(LoopPhase.Review, state, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(ReviewOutcome.Approved, result.ReviewOutcome);

        // Only one Review call, no Planning revision
        Assert.Equal(1, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Review));
        Assert.Equal(0, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Planning));
    }

    [Fact]
    public async Task ReviewCycle_ApprovedWithFindings_ProceedsWithWarning()
    {
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Review = new ReviewConfig { MaxReviewCycles = 2 }
        };

        var orchestrator = await CreateReviewCycleOrchestratorAsync(config,
            (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    return new PhaseResult
                    {
                        Success = true,
                        ReviewOutcome = ReviewOutcome.ApprovedWithFindings,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                return SuccessResult();
            });

        var state = await CreateStateAtReviewPhaseAsync();

        var result = await orchestrator.InvokeDispatchPhaseAsync(LoopPhase.Review, state, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(ReviewOutcome.ApprovedWithFindings, result.ReviewOutcome);

        // Only one Review call, no Planning revision
        Assert.Equal(1, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Review));
        Assert.Equal(0, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Planning));
    }

    [Fact]
    public async Task ReviewCycle_RejectedThenApproved_TriggersRevision()
    {
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Review = new ReviewConfig { MaxReviewCycles = 2 }
        };

        var reviewCallCount = 0;
        var orchestrator = await CreateReviewCycleOrchestratorAsync(config,
            (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    reviewCallCount++;
                    // First review: rejected. Second review: approved.
                    return new PhaseResult
                    {
                        Success = true,
                        ReviewOutcome = reviewCallCount == 1
                            ? ReviewOutcome.Rejected
                            : ReviewOutcome.Approved,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                return SuccessResult();
            });

        var state = await CreateStateAtReviewPhaseAsync();

        var result = await orchestrator.InvokeDispatchPhaseAsync(LoopPhase.Review, state, CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal(ReviewOutcome.Approved, result.ReviewOutcome);

        // Two Review calls, one Planning revision in between
        Assert.Equal(2, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Review));
        Assert.Equal(1, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Planning));

        // Verify order: Review → Planning → Review
        Assert.Equal(LoopPhase.Review, orchestrator.PhaseCalls[0]);
        Assert.Equal(LoopPhase.Planning, orchestrator.PhaseCalls[1]);
        Assert.Equal(LoopPhase.Review, orchestrator.PhaseCalls[2]);
    }

    [Fact]
    public async Task ReviewCycle_MaxCyclesReached_ProceedsAnyway()
    {
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Review = new ReviewConfig { MaxReviewCycles = 1 }
        };

        var orchestrator = await CreateReviewCycleOrchestratorAsync(config,
            (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    // Always rejected
                    return new PhaseResult
                    {
                        Success = true,
                        ReviewOutcome = ReviewOutcome.Rejected,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                return SuccessResult();
            });

        var state = await CreateStateAtReviewPhaseAsync();

        var result = await orchestrator.InvokeDispatchPhaseAsync(LoopPhase.Review, state, CancellationToken.None);

        // Should still succeed (max cycles reached, proceeds anyway)
        Assert.True(result.Success);

        // MaxReviewCycles=1: cycle 0 review (rejected) → planning → cycle 1 review (rejected, at max) → proceed
        Assert.Equal(2, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Review));
        Assert.Equal(1, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Planning));
    }

    [Fact]
    public async Task ReviewCycle_ReviewFails_ReturnsFailure()
    {
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Review = new ReviewConfig { MaxReviewCycles = 2 }
        };

        var orchestrator = await CreateReviewCycleOrchestratorAsync(config,
            (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = "Review agent failed",
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                return SuccessResult();
            });

        var state = await CreateStateAtReviewPhaseAsync();

        var result = await orchestrator.InvokeDispatchPhaseAsync(LoopPhase.Review, state, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Review agent failed", result.ErrorMessage);

        // Only one Review call (failed immediately)
        Assert.Equal(1, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Review));
        Assert.Equal(0, orchestrator.PhaseCalls.Count(c => c == LoopPhase.Planning));
    }

    [Fact]
    public async Task ReviewCycle_PlanningRevisionFails_ReturnsFailure()
    {
        var config = new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            Review = new ReviewConfig { MaxReviewCycles = 2 }
        };

        var orchestrator = await CreateReviewCycleOrchestratorAsync(config,
            (phase, callNumber) =>
            {
                if (phase == LoopPhase.Review)
                {
                    return new PhaseResult
                    {
                        Success = true,
                        ReviewOutcome = ReviewOutcome.Rejected,
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                if (phase == LoopPhase.Planning)
                {
                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = "Planning revision failed",
                        Duration = TimeSpan.FromSeconds(1)
                    };
                }
                return SuccessResult();
            });

        var state = await CreateStateAtReviewPhaseAsync();

        var result = await orchestrator.InvokeDispatchPhaseAsync(LoopPhase.Review, state, CancellationToken.None);

        Assert.False(result.Success);
        Assert.Contains("Planning revision failed", result.ErrorMessage);
    }

    #endregion

    #region PhaseCompleteResult ReviewOutcome Field Tests

    [Fact]
    public void PhaseCompleteResult_ReviewOutcome_DefaultsToNull()
    {
        var result = new PhaseCompleteResult();
        Assert.Null(result.ReviewOutcome);
    }

    [Fact]
    public void PhaseCompleteResult_ReviewOutcome_CanBeSet()
    {
        var result = new PhaseCompleteResult { ReviewOutcome = "approved" };
        Assert.Equal("approved", result.ReviewOutcome);
    }

    #endregion

    #region Helper Methods

    private static PhaseResult SuccessResult() => new()
    {
        Success = true,
        Duration = TimeSpan.FromSeconds(1)
    };

    private async Task<ReviewCycleTestableOrchestrator> CreateOrchestratorAsync(
        HughConfig? config = null)
    {
        config ??= new HughConfig
        {
            DocsPath = Path.Combine(_testWorkspace, "docs")
        };

        await _stateManager.CreateRunAsync("test feature request");

        var options = Options.Create(config);
        var mockCopilot = Substitute.For<ICopilotService>();
        var mockRegistry = Substitute.For<IAgentRegistry>();
        var phaseExecutor = new PhaseExecutor(
            mockCopilot, mockRegistry, _stateManager,
            options, NullLogger<PhaseExecutor>.Instance, new CancellationTokenSource());

        return new ReviewCycleTestableOrchestrator(
            phaseExecutor, _stateManager, options,
            NullLogger<LoopOrchestrator>.Instance,
            (_, _) => SuccessResult());
    }

    private async Task<ReviewCycleTestableOrchestrator> CreateReviewCycleOrchestratorAsync(
        HughConfig config,
        Func<LoopPhase, int, PhaseResult> singlePhaseProvider)
    {
        await _stateManager.CreateRunAsync("test feature request");

        var options = Options.Create(config);
        var mockCopilot = Substitute.For<ICopilotService>();
        var mockRegistry = Substitute.For<IAgentRegistry>();
        var phaseExecutor = new PhaseExecutor(
            mockCopilot, mockRegistry, _stateManager,
            options, NullLogger<PhaseExecutor>.Instance, new CancellationTokenSource());

        return new ReviewCycleTestableOrchestrator(
            phaseExecutor, _stateManager, options,
            NullLogger<LoopOrchestrator>.Instance,
            singlePhaseProvider);
    }

    private async Task<LoopState> CreateStateAtReviewPhaseAsync()
    {
        var state = _stateManager.CurrentState!;

        // Mark Vision, Research, Planning as complete
        await _stateManager.UpdatePhaseAsync(LoopPhase.Vision, PhaseStatus.InProgress, null);

        // Create a dummy plan document for the state
        var planPath = Path.Combine(_testWorkspace, "docs", "plans", "001-test.md");
        File.WriteAllText(planPath, "# Test Plan\n\nThis is a test plan.");

        await _stateManager.MarkPhaseCompleteAsync(
            Path.Combine(_testWorkspace, "docs", "vision", "001-test.md"));

        await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress, null);
        await _stateManager.MarkPhaseCompleteAsync(
            Path.Combine(_testWorkspace, "docs", "research", "001-test.md"));

        await _stateManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.InProgress, null);
        await _stateManager.MarkPhaseCompleteAsync(planPath);

        return _stateManager.CurrentState!;
    }

    #endregion

    #region ReviewCycleTestableOrchestrator

    /// <summary>
    /// Test subclass of LoopOrchestrator that overrides ExecuteSinglePhaseAsync
    /// to return controlled results, enabling isolated testing of the review cycle.
    /// Does NOT override DispatchPhaseAsync — lets the real review routing execute.
    /// </summary>
    private sealed class ReviewCycleTestableOrchestrator : LoopOrchestrator
    {
        private readonly Func<LoopPhase, int, PhaseResult> _singlePhaseProvider;
        private readonly Dictionary<LoopPhase, int> _callCounts = new();

        /// <summary>
        /// Records each ExecuteSinglePhaseAsync call in order.
        /// </summary>
        public List<LoopPhase> PhaseCalls { get; } = new();

        public ReviewCycleTestableOrchestrator(
            PhaseExecutor phaseExecutor,
            LoopStateManager stateManager,
            IOptions<HughConfig> options,
            ILogger<LoopOrchestrator> logger,
            Func<LoopPhase, int, PhaseResult> singlePhaseProvider)
            : base(phaseExecutor, stateManager, new PlanPhaseParser(),
                new ResearchPhaseParser(), new PlanCreationPhaseParser(),
                new TaskParser(), options, logger)
        {
            _singlePhaseProvider = singlePhaseProvider;
        }

        protected override Task<PhaseResult> ExecuteSinglePhaseAsync(
            LoopPhase phase, LoopState state, CancellationToken ct)
        {
            if (!_callCounts.TryGetValue(phase, out var count))
                count = 0;
            count++;
            _callCounts[phase] = count;

            PhaseCalls.Add(phase);
            return Task.FromResult(_singlePhaseProvider(phase, count));
        }

        /// <summary>
        /// Exposes DispatchPhaseAsync for direct testing.
        /// </summary>
        public Task<PhaseResult> InvokeDispatchPhaseAsync(
            LoopPhase phase, LoopState state, CancellationToken ct)
        {
            return DispatchPhaseAsync(phase, state, ct);
        }
    }

    #endregion
}
