using Hugh.Models;
using Hugh.Orchestration;
using Serilog;

namespace Hugh.Tests.Orchestration;

/// <summary>
/// Unit tests for SolutionStateManager.
/// Tests create, load, update, resume, and per-release isolation scenarios.
/// </summary>
public class SolutionStateManagerTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly SolutionStateManager _manager;

    public SolutionStateManagerTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"HughSolutionTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);
        _manager = new SolutionStateManager(_testWorkspace, Log.Logger);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspace))
        {
            try { Directory.Delete(_testWorkspace, recursive: true); }
            catch { /* cleanup best-effort */ }
        }
    }

    #region CreateSolutionAsync Tests

    [Fact]
    public async Task CreateSolutionAsync_CreatesNewState()
    {
        var state = await _manager.CreateSolutionAsync("Build a platform");

        Assert.NotNull(state);
        Assert.NotEqual(Guid.Empty, state.SolutionId);
        Assert.Equal("Build a platform", state.OriginalRequest);
        Assert.Equal(SolutionMode.MultiRelease, state.Mode);
        Assert.Equal(SolutionPhase.Triage, state.CurrentPhase);
        Assert.Equal(PhaseStatus.NotStarted, state.Status);
        Assert.Empty(state.Releases);
        Assert.Empty(state.Decisions);
    }

    [Fact]
    public async Task CreateSolutionAsync_SetsCurrentState()
    {
        await _manager.CreateSolutionAsync("Test");

        Assert.NotNull(_manager.CurrentState);
    }

    [Fact]
    public async Task CreateSolutionAsync_PersistsToFile()
    {
        await _manager.CreateSolutionAsync("Test");

        var stateFilePath = Path.Combine(_testWorkspace, ".orchestrator", "solution-state.json");
        Assert.True(File.Exists(stateFilePath));
    }

    [Fact]
    public async Task CreateSolutionAsync_SetsStartedAtToUtcNow()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);

        var state = await _manager.CreateSolutionAsync("Test");

        var after = DateTime.UtcNow.AddSeconds(1);
        Assert.InRange(state.StartedAt, before, after);
    }

    #endregion

    #region LoadSolutionAsync Tests

    [Fact]
    public async Task LoadSolutionAsync_NoFile_ReturnsNull()
    {
        var result = await _manager.LoadSolutionAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task LoadSolutionAsync_ExistingFile_ReturnsState()
    {
        var created = await _manager.CreateSolutionAsync("Build a platform");

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.NotNull(loaded);
        Assert.Equal(created.SolutionId, loaded.SolutionId);
        Assert.Equal(created.OriginalRequest, loaded.OriginalRequest);
    }

    [Fact]
    public async Task LoadSolutionAsync_BySolutionId_ReturnsCorrectState()
    {
        var created = await _manager.CreateSolutionAsync("Test");

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync(created.SolutionId);

        Assert.NotNull(loaded);
        Assert.Equal(created.SolutionId, loaded.SolutionId);
    }

    [Fact]
    public async Task LoadSolutionAsync_ByWrongSolutionId_ReturnsNull()
    {
        await _manager.CreateSolutionAsync("Test");

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync(Guid.NewGuid());

        Assert.Null(loaded);
    }

    #endregion

    #region UpdatePhaseAsync Tests

    [Fact]
    public async Task UpdatePhaseAsync_UpdatesPhaseAndStatus()
    {
        await _manager.CreateSolutionAsync("Test");

        await _manager.UpdatePhaseAsync(SolutionPhase.Clarification, PhaseStatus.InProgress);

        Assert.Equal(SolutionPhase.Clarification, _manager.CurrentState!.CurrentPhase);
        Assert.Equal(PhaseStatus.InProgress, _manager.CurrentState.Status);
    }

    [Fact]
    public async Task UpdatePhaseAsync_PersistsChanges()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.UpdatePhaseAsync(SolutionPhase.Decomposition, PhaseStatus.InProgress);

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.Equal(SolutionPhase.Decomposition, loaded!.CurrentPhase);
        Assert.Equal(PhaseStatus.InProgress, loaded.Status);
    }

    [Fact]
    public async Task UpdatePhaseAsync_NoState_ThrowsInvalidOperationException()
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _manager.UpdatePhaseAsync(SolutionPhase.Clarification, PhaseStatus.InProgress));
    }

    #endregion

    #region SetModeAsync Tests

    [Fact]
    public async Task SetModeAsync_UpdatesMode()
    {
        await _manager.CreateSolutionAsync("Test");

        await _manager.SetModeAsync(SolutionMode.SingleFeature);

        Assert.Equal(SolutionMode.SingleFeature, _manager.CurrentState!.Mode);
    }

    [Fact]
    public async Task SetModeAsync_PersistsChanges()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.SetModeAsync(SolutionMode.MultiRelease);

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.Equal(SolutionMode.MultiRelease, loaded!.Mode);
    }

    #endregion

    #region UpdateDocumentsAsync Tests

    [Fact]
    public async Task UpdateDocumentsAsync_UpdatesDocumentPaths()
    {
        await _manager.CreateSolutionAsync("Test");
        var docs = new SolutionDocumentPaths
        {
            ClarifiedVision = "/docs/vision/abc.md",
            Roadmap = "/docs/roadmap/abc.md"
        };

        await _manager.UpdateDocumentsAsync(docs);

        Assert.Equal("/docs/vision/abc.md", _manager.CurrentState!.Documents.ClarifiedVision);
        Assert.Equal("/docs/roadmap/abc.md", _manager.CurrentState.Documents.Roadmap);
    }

    [Fact]
    public async Task UpdateDocumentsAsync_WithExpression_PreservesExistingPaths()
    {
        await _manager.CreateSolutionAsync("Test");
        var docs1 = new SolutionDocumentPaths { ClarifiedVision = "/docs/vision/abc.md" };
        await _manager.UpdateDocumentsAsync(docs1);

        var docs2 = _manager.CurrentState!.Documents with { Roadmap = "/docs/roadmap/abc.md" };
        await _manager.UpdateDocumentsAsync(docs2);

        Assert.Equal("/docs/vision/abc.md", _manager.CurrentState!.Documents.ClarifiedVision);
        Assert.Equal("/docs/roadmap/abc.md", _manager.CurrentState.Documents.Roadmap);
    }

    #endregion

    #region SetReleasesAsync Tests

    [Fact]
    public async Task SetReleasesAsync_PopulatesReleases()
    {
        await _manager.CreateSolutionAsync("Test");
        var releases = new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 2, Name = "Features", Status = ReleaseStatus.Pending }
        };

        await _manager.SetReleasesAsync(releases);

        Assert.Equal(2, _manager.CurrentState!.Releases.Count);
        Assert.Equal("MVP", _manager.CurrentState.Releases[0].Name);
        Assert.Equal("Features", _manager.CurrentState.Releases[1].Name);
    }

    [Fact]
    public async Task SetReleasesAsync_PersistsChanges()
    {
        await _manager.CreateSolutionAsync("Test");
        var releases = new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending }
        };
        await _manager.SetReleasesAsync(releases);

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.Single(loaded!.Releases);
        Assert.Equal("MVP", loaded.Releases[0].Name);
    }

    #endregion

    #region UpdateReleaseStatusAsync Tests

    [Fact]
    public async Task UpdateReleaseStatusAsync_UpdatesReleaseStatus()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 2, Name = "Features", Status = ReleaseStatus.Pending }
        });

        await _manager.UpdateReleaseStatusAsync(0, ReleaseStatus.InProgress);

        Assert.Equal(ReleaseStatus.InProgress, _manager.CurrentState!.Releases[0].Status);
        Assert.Equal(ReleaseStatus.Pending, _manager.CurrentState.Releases[1].Status);
    }

    [Fact]
    public async Task UpdateReleaseStatusAsync_UpdatesCurrentReleaseIndex()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 2, Name = "Features", Status = ReleaseStatus.Pending }
        });

        await _manager.UpdateReleaseStatusAsync(1, ReleaseStatus.InProgress);

        Assert.Equal(1, _manager.CurrentState!.CurrentReleaseIndex);
    }

    [Fact]
    public async Task UpdateReleaseStatusAsync_InvalidIndex_ThrowsArgumentOutOfRangeException()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending }
        });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _manager.UpdateReleaseStatusAsync(5, ReleaseStatus.InProgress));
    }

    [Fact]
    public async Task UpdateReleaseStatusAsync_NegativeIndex_ThrowsArgumentOutOfRangeException()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending }
        });

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _manager.UpdateReleaseStatusAsync(-1, ReleaseStatus.InProgress));
    }

    #endregion

    #region MarkCompleteAsync Tests

    [Fact]
    public async Task MarkCompleteAsync_SetsPhaseAndStatusToComplete()
    {
        await _manager.CreateSolutionAsync("Test");

        await _manager.MarkCompleteAsync();

        Assert.Equal(SolutionPhase.Complete, _manager.CurrentState!.CurrentPhase);
        Assert.Equal(PhaseStatus.Complete, _manager.CurrentState.Status);
    }

    #endregion

    #region MarkFailedAsync Tests

    [Fact]
    public async Task MarkFailedAsync_SetsStatusToFailed()
    {
        await _manager.CreateSolutionAsync("Test");

        await _manager.MarkFailedAsync("Something went wrong");

        Assert.Equal(PhaseStatus.Failed, _manager.CurrentState!.Status);
    }

    [Fact]
    public async Task MarkFailedAsync_SetsFailureReason()
    {
        await _manager.CreateSolutionAsync("Test");

        await _manager.MarkFailedAsync("Decomposition produced no releases");

        Assert.Equal("Decomposition produced no releases", _manager.CurrentState!.FailureReason);
    }

    #endregion

    #region AddDecisionAsync Tests

    [Fact]
    public async Task AddDecisionAsync_AddsDecisionToList()
    {
        await _manager.CreateSolutionAsync("Test");
        var decision = new Decision
        {
            Phase = "Triage",
            Question = "Classification",
            Selected = "SOLUTION",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        };

        await _manager.AddDecisionAsync(decision);

        Assert.Single(_manager.CurrentState!.Decisions);
        Assert.Equal("Classification", _manager.CurrentState.Decisions[0].Question);
    }

    [Fact]
    public async Task AddDecisionAsync_MultipleDecisions_PreservesAll()
    {
        await _manager.CreateSolutionAsync("Test");

        for (var i = 1; i <= 3; i++)
        {
            await _manager.AddDecisionAsync(new Decision
            {
                Phase = "Clarification",
                Question = $"Question {i}",
                Selected = $"Answer {i}",
                Timestamp = DateTime.UtcNow
            });
        }

        Assert.Equal(3, _manager.CurrentState!.Decisions.Count);
    }

    #endregion

    #region HasExistingSolution Tests

    [Fact]
    public void HasExistingSolution_NoStateFile_ReturnsFalse()
    {
        Assert.False(_manager.HasExistingSolution());
    }

    [Fact]
    public async Task HasExistingSolution_WithStateFile_ReturnsTrue()
    {
        await _manager.CreateSolutionAsync("Test");

        Assert.True(_manager.HasExistingSolution());
    }

    #endregion

    #region Per-Release State Isolation Tests

    [Fact]
    public void CreateReleaseStateManager_CreatesManagerWithCorrectPath()
    {
        var releaseManager = _manager.CreateReleaseStateManager(1);

        Assert.NotNull(releaseManager);
    }

    [Fact]
    public async Task CreateReleaseStateManager_IsolatesPerReleaseState()
    {
        // Create two release state managers
        var release1Manager = _manager.CreateReleaseStateManager(1);
        var release2Manager = _manager.CreateReleaseStateManager(2);

        // Create runs in each
        var state1 = await release1Manager.CreateRunAsync("Release 1 feature");
        var state2 = await release2Manager.CreateRunAsync("Release 2 feature");

        // Verify they have different RunIds and are isolated
        Assert.NotEqual(state1.RunId, state2.RunId);
        Assert.Equal("Release 1 feature", state1.FeatureRequest);
        Assert.Equal("Release 2 feature", state2.FeatureRequest);
    }

    [Fact]
    public async Task CreateReleaseStateManager_PersistsInCorrectDirectory()
    {
        var releaseManager = _manager.CreateReleaseStateManager(3);
        await releaseManager.CreateRunAsync("Release 3 work");

        var expectedPath = Path.Combine(
            _testWorkspace, ".orchestrator", "releases", "release-3", "loop-state.json");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task CreateReleaseStateManager_ReleasesDoNotInterfere()
    {
        var release1 = _manager.CreateReleaseStateManager(1);
        var release2 = _manager.CreateReleaseStateManager(2);

        await release1.CreateRunAsync("Release 1");
        await release1.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);

        await release2.CreateRunAsync("Release 2");

        // Release 1 should still be at Research
        var loaded1 = new LoopStateManager(
            _testWorkspace, Log.Logger, ".orchestrator/releases/release-1");
        var state1 = await loaded1.LoadStateAsync();
        Assert.Equal(LoopPhase.Research, state1!.CurrentPhase);

        // Release 2 should be at Vision (default)
        var loaded2 = new LoopStateManager(
            _testWorkspace, Log.Logger, ".orchestrator/releases/release-2");
        var state2 = await loaded2.LoadStateAsync();
        Assert.Equal(LoopPhase.Vision, state2!.CurrentPhase);
    }

    #endregion

    #region GetReleaseStateDirectory Tests

    [Fact]
    public void GetReleaseStateDirectory_CreatesDirectory()
    {
        var dir = _manager.GetReleaseStateDirectory(1);

        Assert.True(Directory.Exists(dir));
    }

    [Fact]
    public void GetReleaseStateDirectory_ReturnsCorrectPath()
    {
        var dir = _manager.GetReleaseStateDirectory(2);

        var expected = Path.Combine(_testWorkspace, ".orchestrator", "releases", "release-2");
        Assert.Equal(expected, dir);
    }

    #endregion

    #region Resume Scenario Tests

    [Fact]
    public async Task ResumeScenario_LoadsExistingStateAndContinues()
    {
        // Simulate a previous session through clarification
        await _manager.CreateSolutionAsync("Build a platform");
        await _manager.UpdatePhaseAsync(SolutionPhase.Triage, PhaseStatus.InProgress);
        await _manager.SetModeAsync(SolutionMode.MultiRelease);
        await _manager.UpdatePhaseAsync(SolutionPhase.Clarification, PhaseStatus.InProgress);
        await _manager.UpdateDocumentsAsync(new SolutionDocumentPaths
        {
            ClarifiedVision = "/docs/vision/abc.md"
        });
        await _manager.UpdatePhaseAsync(SolutionPhase.Decomposition, PhaseStatus.InProgress);

        // Simulate resume from a fresh process
        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.NotNull(loaded);
        Assert.Equal(SolutionMode.MultiRelease, loaded.Mode);
        Assert.Equal(SolutionPhase.Decomposition, loaded.CurrentPhase);
        Assert.Equal("/docs/vision/abc.md", loaded.Documents.ClarifiedVision);
    }

    [Fact]
    public async Task ResumeScenario_MidRelease_PreservesReleaseProgress()
    {
        await _manager.CreateSolutionAsync("Build a platform");
        await _manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 2, Name = "Features", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 3, Name = "Polish", Status = ReleaseStatus.Pending }
        });

        // Complete release 1, start release 2
        await _manager.UpdateReleaseStatusAsync(0, ReleaseStatus.Complete);
        await _manager.UpdateReleaseStatusAsync(1, ReleaseStatus.InProgress);

        // Resume
        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.Equal(ReleaseStatus.Complete, loaded!.Releases[0].Status);
        Assert.Equal(ReleaseStatus.InProgress, loaded.Releases[1].Status);
        Assert.Equal(ReleaseStatus.Pending, loaded.Releases[2].Status);
        Assert.Equal(1, loaded.CurrentReleaseIndex);
    }

    [Fact]
    public async Task ResumeScenario_AfterFailure_PreservesFailureReason()
    {
        await _manager.CreateSolutionAsync("Test");
        await _manager.MarkFailedAsync("Release 2 coding failed");

        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.Equal(PhaseStatus.Failed, loaded!.Status);
        Assert.Equal("Release 2 coding failed", loaded.FailureReason);
    }

    #endregion

    #region Full State Persistence Round-Trip

    [Fact]
    public async Task StatePersistence_ComplexState_SurvivesJsonRoundTrip()
    {
        // Build up a complex state
        var state = await _manager.CreateSolutionAsync(
            "Build a task management platform with user auth, boards, and real-time collab");
        await _manager.SetModeAsync(SolutionMode.MultiRelease);
        await _manager.UpdateDocumentsAsync(new SolutionDocumentPaths
        {
            ClarifiedVision = "/docs/vision/abc-clarified.md",
            Roadmap = "/docs/roadmap/abc-roadmap.md"
        });
        await _manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new()
            {
                ReleaseNumber = 1,
                Name = "MVP — Core Task Management",
                Description = "Basic task CRUD with auth",
                Scope = "- Task creation\n- User login\n- Task list view",
                RunId = Guid.NewGuid(),
                Status = ReleaseStatus.Complete
            },
            new()
            {
                ReleaseNumber = 2,
                Name = "Collaboration",
                Description = "Team features",
                Scope = "- Shared boards\n- Comments\n- Invitations",
                RunId = Guid.NewGuid(),
                Status = ReleaseStatus.InProgress
            }
        });
        await _manager.AddDecisionAsync(new Decision
        {
            Phase = "Triage",
            Question = "Classification",
            Selected = "SOLUTION",
            Rationale = "Multiple features requested",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        });

        // Load in a new manager
        var newManager = new SolutionStateManager(_testWorkspace, Log.Logger);
        var loaded = await newManager.LoadSolutionAsync();

        Assert.NotNull(loaded);
        Assert.Equal(state.SolutionId, loaded.SolutionId);
        Assert.Equal(SolutionMode.MultiRelease, loaded.Mode);
        Assert.Equal("/docs/vision/abc-clarified.md", loaded.Documents.ClarifiedVision);
        Assert.Equal("/docs/roadmap/abc-roadmap.md", loaded.Documents.Roadmap);
        Assert.Equal(2, loaded.Releases.Count);
        Assert.Equal("MVP — Core Task Management", loaded.Releases[0].Name);
        Assert.Equal(ReleaseStatus.Complete, loaded.Releases[0].Status);
        Assert.Equal(ReleaseStatus.InProgress, loaded.Releases[1].Status);
        Assert.Single(loaded.Decisions);
        Assert.Equal("SOLUTION", loaded.Decisions[0].Selected);
    }

    #endregion
}
