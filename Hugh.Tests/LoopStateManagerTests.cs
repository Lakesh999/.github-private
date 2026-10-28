using Hugh.Models;
using Hugh.Orchestration;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for LoopStateManager.
/// Tests save, load, update, and resume scenarios.
/// </summary>
public class LoopStateManagerTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly LoopStateManager _stateManager;

    public LoopStateManagerTests()
    {
        // Create a unique temp directory for each test run
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"HughTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);

        // Use a null logger for tests
        _stateManager = new LoopStateManager(_testWorkspace, Log.Logger);
    }

    public void Dispose()
    {
        // Clean up test directory
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

    #region CreateRunAsync Tests

    [Fact]
    public async Task CreateRunAsync_CreatesNewState()
    {
        var featureRequest = "Add user authentication";

        var state = await _stateManager.CreateRunAsync(featureRequest);

        Assert.NotNull(state);
        Assert.NotEqual(Guid.Empty, state.RunId);
        Assert.Equal(featureRequest, state.FeatureRequest);
        Assert.Equal(LoopPhase.Vision, state.CurrentPhase);
        Assert.Equal(PhaseStatus.NotStarted, state.Status);
        Assert.Empty(state.CompletedPhases);
    }

    [Fact]
    public async Task CreateRunAsync_SetsCurrentState()
    {
        await _stateManager.CreateRunAsync("Test feature");

        Assert.NotNull(_stateManager.CurrentState);
    }

    [Fact]
    public async Task CreateRunAsync_PersistsToFile()
    {
        await _stateManager.CreateRunAsync("Test feature");

        var stateFilePath = Path.Combine(_testWorkspace, ".orchestrator", "loop-state.json");
        Assert.True(File.Exists(stateFilePath));
    }

    [Fact]
    public async Task CreateRunAsync_SetsStartedAtToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var state = await _stateManager.CreateRunAsync("Test feature");

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(state.StartedAt, before, after);
    }

    #endregion

    #region LoadStateAsync Tests

    [Fact]
    public async Task LoadStateAsync_NoFile_ReturnsNull()
    {
        var result = await _stateManager.LoadStateAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadStateAsync_ExistingFile_ReturnsState()
    {
        var created = await _stateManager.CreateRunAsync("Test feature");

        // Create a new instance to simulate fresh load
        var newManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadStateAsync();

        Assert.NotNull(loaded);
        Assert.Equal(created.RunId, loaded.RunId);
        Assert.Equal(created.FeatureRequest, loaded.FeatureRequest);
    }

    [Fact]
    public async Task LoadStateAsync_ByRunId_ReturnsCorrectState()
    {
        var created = await _stateManager.CreateRunAsync("Test feature");

        var newManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadStateAsync(created.RunId);

        Assert.NotNull(loaded);
        Assert.Equal(created.RunId, loaded.RunId);
    }

    [Fact]
    public async Task LoadStateAsync_ByWrongRunId_ReturnsNull()
    {
        await _stateManager.CreateRunAsync("Test feature");

        var newManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadStateAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    #endregion

    #region UpdatePhaseAsync Tests

    [Fact]
    public async Task UpdatePhaseAsync_UpdatesPhaseAndStatus()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);

        Assert.Equal(LoopPhase.Research, _stateManager.CurrentState!.CurrentPhase);
        Assert.Equal(PhaseStatus.InProgress, _stateManager.CurrentState.Status);
    }

    [Fact]
    public async Task UpdatePhaseAsync_WithCodingSubPhase_SetsCodingSubPhase()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.UpdatePhaseAsync(LoopPhase.Coding, PhaseStatus.InProgress, subPhase: 3);

        Assert.Equal(3, _stateManager.CurrentState!.CurrentSubPhase);
    }

    [Fact]
    public async Task UpdatePhaseAsync_PersistsChanges()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.InProgress);

        var newManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadStateAsync();

        Assert.Equal(LoopPhase.Planning, loaded!.CurrentPhase);
        Assert.Equal(PhaseStatus.InProgress, loaded.Status);
    }

    [Fact]
    public async Task UpdatePhaseAsync_NoState_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress));
    }

    #endregion

    #region MarkPhaseCompleteAsync Tests

    [Fact]
    public async Task MarkPhaseCompleteAsync_AddsToCompletedPhases()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.MarkPhaseCompleteAsync();

        Assert.Contains(LoopPhase.Vision, _stateManager.CurrentState!.CompletedPhases);
    }

    [Fact]
    public async Task MarkPhaseCompleteAsync_SetsStatusToComplete()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.MarkPhaseCompleteAsync();

        Assert.Equal(PhaseStatus.Complete, _stateManager.CurrentState!.Status);
    }

    [Fact]
    public async Task MarkPhaseCompleteAsync_WithDocumentPath_UpdatesDocuments()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.MarkPhaseCompleteAsync("/docs/vision/001-feature.md");

        Assert.Equal("/docs/vision/001-feature.md", _stateManager.CurrentState!.Documents.Vision);
    }

    [Fact]
    public async Task MarkPhaseCompleteAsync_ResearchPhase_UpdatesResearchDocument()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);

        await _stateManager.MarkPhaseCompleteAsync("/docs/research/001-feature.md");

        Assert.Equal("/docs/research/001-feature.md", _stateManager.CurrentState!.Documents.Research);
    }

    [Fact]
    public async Task MarkPhaseCompleteAsync_PlanningPhase_UpdatesPlanDocument()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.InProgress);

        await _stateManager.MarkPhaseCompleteAsync("/docs/plans/001-feature.md");

        Assert.Equal("/docs/plans/001-feature.md", _stateManager.CurrentState!.Documents.Plan);
    }

    [Fact]
    public async Task MarkPhaseCompleteAsync_DoesNotDuplicatePhases()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.MarkPhaseCompleteAsync();
        await _stateManager.MarkPhaseCompleteAsync(); // Mark again

        Assert.Single(_stateManager.CurrentState!.CompletedPhases, p => p == LoopPhase.Vision);
    }

    #endregion

    #region MarkPhaseFailedAsync Tests

    [Fact]
    public async Task MarkPhaseFailedAsync_SetsStatusToFailed()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.MarkPhaseFailedAsync("API timeout occurred");

        Assert.Equal(PhaseStatus.Failed, _stateManager.CurrentState!.Status);
    }

    [Fact]
    public async Task MarkPhaseFailedAsync_SetsFailureReason()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.MarkPhaseFailedAsync("Missing configuration");

        Assert.Equal("Missing configuration", _stateManager.CurrentState!.FailureReason);
    }

    #endregion

    #region MarkPausedAsync Tests

    [Fact]
    public async Task MarkPausedAsync_SetsStatusToPaused()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.MarkPausedAsync();

        Assert.Equal(PhaseStatus.Paused, _stateManager.CurrentState!.Status);
    }

    #endregion

    #region AddDecisionAsync Tests

    [Fact]
    public async Task AddDecisionAsync_AddsDecisionToList()
    {
        await _stateManager.CreateRunAsync("Test feature");
        var decision = new Decision
        {
            Phase = "Vision",
            Question = "What architecture?",
            Options = ["Microservices", "Monolith"],
            Selected = "Microservices",
            Rationale = "Agent recommendation",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        };

        await _stateManager.AddDecisionAsync(decision);

        Assert.Single(_stateManager.CurrentState!.Decisions);
        Assert.Equal("What architecture?", _stateManager.CurrentState.Decisions[0].Question);
    }

    [Fact]
    public async Task AddDecisionAsync_MultipleDecisions_PreservesAll()
    {
        await _stateManager.CreateRunAsync("Test feature");

        for (int i = 1; i <= 3; i++)
        {
            await _stateManager.AddDecisionAsync(new Decision
            {
                Phase = "Vision",
                Question = $"Question {i}",
                Selected = $"Answer {i}",
                Timestamp = DateTime.UtcNow
            });
        }

        Assert.Equal(3, _stateManager.CurrentState!.Decisions.Count);
    }

    #endregion

    #region AdvanceToNextPhaseAsync Tests

    [Fact]
    public async Task AdvanceToNextPhaseAsync_Vision_AdvancesToResearch()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(LoopPhase.Research, _stateManager.CurrentState!.CurrentPhase);
    }

    [Fact]
    public async Task AdvanceToNextPhaseAsync_Research_AdvancesToPlanning()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.Complete);

        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(LoopPhase.Planning, _stateManager.CurrentState!.CurrentPhase);
    }

    [Fact]
    public async Task AdvanceToNextPhaseAsync_Planning_AdvancesToReview()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.Complete);

        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(LoopPhase.Review, _stateManager.CurrentState!.CurrentPhase);
    }

    [Fact]
    public async Task AdvanceToNextPhaseAsync_Review_AdvancesToCoding()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Review, PhaseStatus.Complete);

        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(LoopPhase.Coding, _stateManager.CurrentState!.CurrentPhase);
    }

    [Fact]
    public async Task AdvanceToNextPhaseAsync_Coding_SetsComplete()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Coding, PhaseStatus.Complete);

        await _stateManager.AdvanceToNextPhaseAsync();

        // Coding is the last phase, so status should be Complete
        Assert.Equal(PhaseStatus.Complete, _stateManager.CurrentState!.Status);
    }

    [Fact]
    public async Task AdvanceToNextPhaseAsync_ToCoding_SetsCodingSubPhaseToOne()
    {
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Review, PhaseStatus.Complete);

        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(1, _stateManager.CurrentState!.CurrentSubPhase);
    }

    [Fact]
    public async Task AdvanceToNextPhaseAsync_SetsStatusToNotStarted()
    {
        await _stateManager.CreateRunAsync("Test feature");

        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(PhaseStatus.NotStarted, _stateManager.CurrentState!.Status);
    }

    #endregion

    #region Resume Scenario Tests

    [Fact]
    public async Task ResumeScenario_LoadsExistingStateAndContinues()
    {
        // Simulate a previous session
        var originalState = await _stateManager.CreateRunAsync("Implement OAuth");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Vision, PhaseStatus.InProgress);
        await _stateManager.MarkPhaseCompleteAsync("/docs/vision/001-oauth.md");
        await _stateManager.AdvanceToNextPhaseAsync();

        // Simulate a new process loading the state
        var resumeManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loadedState = await resumeManager.LoadStateAsync(originalState.RunId);

        Assert.NotNull(loadedState);
        Assert.Equal(originalState.RunId, loadedState.RunId);
        Assert.Equal(LoopPhase.Research, loadedState.CurrentPhase);
        Assert.Contains(LoopPhase.Vision, loadedState.CompletedPhases);
        Assert.Equal("/docs/vision/001-oauth.md", loadedState.Documents.Vision);
    }

    [Fact]
    public async Task ResumeScenario_AfterFailure_CanRetry()
    {
        // Simulate a failure
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);
        await _stateManager.MarkPhaseFailedAsync("Network timeout");

        // Simulate resume
        var resumeManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var state = await resumeManager.LoadStateAsync();

        Assert.Equal(PhaseStatus.Failed, state!.Status);
        Assert.Equal("Network timeout", state.FailureReason);

        // Retry the phase
        await resumeManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);

        Assert.Equal(PhaseStatus.InProgress, resumeManager.CurrentState!.Status);
        Assert.Equal(LoopPhase.Research, resumeManager.CurrentState.CurrentPhase);
    }

    [Fact]
    public async Task ResumeScenario_AfterPause_ContinuesWithDecisions()
    {
        // Create and pause
        await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.AddDecisionAsync(new Decision
        {
            Phase = "Vision",
            Question = "Which approach?",
            Selected = "A",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        });
        await _stateManager.MarkPausedAsync();

        // Resume
        var resumeManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var state = await resumeManager.LoadStateAsync();

        Assert.Equal(PhaseStatus.Paused, state!.Status);
        Assert.Single(state.Decisions);
        Assert.Equal("Which approach?", state.Decisions[0].Question);
    }

    #endregion

    #region State Persistence Tests

    [Fact]
    public async Task StatePersistence_SurvivesJsonRoundTrip()
    {
        // Create a complex state
        var created = await _stateManager.CreateRunAsync("Complex feature request with special characters: <>&\"'");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.InProgress, subPhase: 2);
        await _stateManager.AddDecisionAsync(new Decision
        {
            Phase = "Planning",
            Question = "Database choice?",
            Options = ["PostgreSQL", "SQL Server", "MongoDB"],
            Selected = "PostgreSQL",
            Rationale = "Best fit for relational data",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        });

        // Load in a new manager
        var newManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadStateAsync();

        Assert.NotNull(loaded);
        Assert.Equal(created.RunId, loaded.RunId);
        Assert.Equal("Complex feature request with special characters: <>&\"'", loaded.FeatureRequest);
        Assert.Equal(LoopPhase.Planning, loaded.CurrentPhase);
        Assert.Equal(2, loaded.CurrentSubPhase);
        Assert.Single(loaded.Decisions);
        Assert.Equal("PostgreSQL", loaded.Decisions[0].Selected);
    }

    #endregion

    #region CreateRunFromPhaseAsync Tests

    [Fact]
    public async Task CreateRunFromPhaseAsync_AtResearch_SetsVisionComplete()
    {
        var docs = new DocumentPaths { Vision = "/docs/vision/001-vision.md" };

        var state = await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Research, docs);

        Assert.Equal(LoopPhase.Research, state.CurrentPhase);
        Assert.Equal(PhaseStatus.NotStarted, state.Status);
        Assert.Single(state.CompletedPhases);
        Assert.Contains(LoopPhase.Vision, state.CompletedPhases);
        Assert.Equal("/docs/vision/001-vision.md", state.Documents.Vision);
    }

    [Fact]
    public async Task CreateRunFromPhaseAsync_AtPlanning_SetsVisionAndResearchComplete()
    {
        var docs = new DocumentPaths
        {
            Vision = "/docs/vision/001-vision.md",
            Research = "/docs/research/001-research.md"
        };

        var state = await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Planning, docs);

        Assert.Equal(LoopPhase.Planning, state.CurrentPhase);
        Assert.Equal(2, state.CompletedPhases.Count);
        Assert.Contains(LoopPhase.Vision, state.CompletedPhases);
        Assert.Contains(LoopPhase.Research, state.CompletedPhases);
        Assert.Equal("/docs/vision/001-vision.md", state.Documents.Vision);
        Assert.Equal("/docs/research/001-research.md", state.Documents.Research);
    }

    [Fact]
    public async Task CreateRunFromPhaseAsync_AtVision_HasNoCompletedPhases()
    {
        var state = await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Vision);

        Assert.Equal(LoopPhase.Vision, state.CurrentPhase);
        Assert.Empty(state.CompletedPhases);
    }

    [Fact]
    public async Task CreateRunFromPhaseAsync_AtCoding_SetsSubPhaseToOne()
    {
        var state = await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Coding);

        Assert.Equal(LoopPhase.Coding, state.CurrentPhase);
        Assert.Equal(1, state.CurrentSubPhase);
        Assert.Equal(4, state.CompletedPhases.Count);
    }

    [Fact]
    public async Task CreateRunFromPhaseAsync_NullDocuments_UsesEmptyDocuments()
    {
        var state = await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Research);

        Assert.Null(state.Documents.Vision);
        Assert.Null(state.Documents.Research);
        Assert.Null(state.Documents.Plan);
    }

    [Fact]
    public async Task CreateRunFromPhaseAsync_PersistsToFile()
    {
        await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Planning);

        var stateFilePath = Path.Combine(_testWorkspace, ".orchestrator", "loop-state.json");
        Assert.True(File.Exists(stateFilePath));
    }

    [Fact]
    public async Task CreateRunFromPhaseAsync_LoadState_RoundTrips()
    {
        var docs = new DocumentPaths
        {
            Vision = "/docs/vision/001-vision.md",
            Research = "/docs/research/001-research.md"
        };

        var created = await _stateManager.CreateRunFromPhaseAsync("Test feature", LoopPhase.Planning, docs);

        // Load it back
        var loaded = await _stateManager.LoadStateAsync();

        Assert.NotNull(loaded);
        Assert.Equal(created.RunId, loaded!.RunId);
        Assert.Equal(LoopPhase.Planning, loaded.CurrentPhase);
        Assert.Equal(2, loaded.CompletedPhases.Count);
        Assert.Contains(LoopPhase.Vision, loaded.CompletedPhases);
        Assert.Contains(LoopPhase.Research, loaded.CompletedPhases);
        Assert.Equal("/docs/vision/001-vision.md", loaded.Documents.Vision);
        Assert.Equal("/docs/research/001-research.md", loaded.Documents.Research);
    }

    #endregion
}