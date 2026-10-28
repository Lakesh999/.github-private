using Hugh.Detection;
using Hugh.Models;
using Hugh.Orchestration;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for ProgressReporter.
/// Tests markdown rendering, sub-phase detail, directory creation, and error handling.
/// </summary>
public class ProgressReporterTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly string _docsPath;
    private readonly ResearchPhaseParser _researchPhaseParser;
    private readonly PlanPhaseParser _planPhaseParser;
    private readonly ProgressReporter _reporter;

    public ProgressReporterTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"HughTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);

        _docsPath = Path.Combine(_testWorkspace, "docs");
        _researchPhaseParser = new ResearchPhaseParser();
        _planPhaseParser = new PlanPhaseParser();
        _reporter = new ProgressReporter(_docsPath, _researchPhaseParser, _planPhaseParser, Log.Logger);
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

    #region RenderProgressMarkdown Tests

    [Fact]
    public void RenderProgressMarkdown_EmptyState_RendersHeaderAndTable()
    {
        var state = CreateDefaultState();

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.Contains("# Hugh Progress Report", markdown);
        Assert.Contains("**Run ID:**", markdown);
        Assert.Contains("**Feature:** Add authentication", markdown);
        Assert.Contains("**Current Phase:** Vision", markdown);
        Assert.Contains("**Status:**", markdown);
        Assert.Contains("## Phase Overview", markdown);
        Assert.Contains("| Phase | Status | Document |", markdown);
        Assert.Contains("| Vision |", markdown);
        Assert.Contains("| Research |", markdown);
        Assert.Contains("| Planning |", markdown);
        Assert.Contains("| Review |", markdown);
        Assert.Contains("| Coding |", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_CompletedPhases_ShowsCorrectStatus()
    {
        var state = CreateDefaultState() with
        {
            CurrentPhase = LoopPhase.Planning,
            Status = PhaseStatus.InProgress,
            CompletedPhases = new List<LoopPhase> { LoopPhase.Vision, LoopPhase.Research },
            Documents = new DocumentPaths
            {
                Vision = "docs/vision/001-feature.md",
                Research = "docs/research/001-feature.md"
            }
        };

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.Contains("| Vision | ✅ Complete |", markdown);
        Assert.Contains("| Research | ✅ Complete |", markdown);
        Assert.Contains("| Planning | 🔄 In Progress |", markdown);
        Assert.Contains("| Review | ⏳ Not Started |", markdown);
        Assert.Contains("| Coding | ⏳ Not Started |", markdown);
        Assert.Contains("`docs/vision/001-feature.md`", markdown);
        Assert.Contains("`docs/research/001-feature.md`", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_FailedPhase_ShowsFailureDetails()
    {
        var state = CreateDefaultState() with
        {
            CurrentPhase = LoopPhase.Research,
            Status = PhaseStatus.Failed,
            FailureReason = "API rate limit exceeded",
            CompletedPhases = new List<LoopPhase> { LoopPhase.Vision }
        };

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.Contains("| Research | ❌ Failed |", markdown);
        Assert.Contains("## Failure Details", markdown);
        Assert.Contains("**Reason:** API rate limit exceeded", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_WithDecisions_RendersDecisionTable()
    {
        var state = CreateDefaultState() with
        {
            Decisions = new List<Decision>
            {
                new()
                {
                    Phase = "Vision",
                    Question = "Which auth provider?",
                    Selected = "OAuth 2.0",
                    Rationale = "Industry standard"
                },
                new()
                {
                    Phase = "Planning",
                    Question = "Database choice?",
                    Selected = "PostgreSQL",
                    Rationale = "Existing infrastructure"
                }
            }
        };

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.Contains("## Decisions", markdown);
        Assert.Contains("| 1 | Vision | Which auth provider? | OAuth 2.0 | Industry standard |", markdown);
        Assert.Contains("| 2 | Planning | Database choice? | PostgreSQL | Existing infrastructure |", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_WithSubPhase_ShowsSubPhaseNumber()
    {
        var state = CreateDefaultState() with
        {
            CurrentPhase = LoopPhase.Coding,
            Status = PhaseStatus.InProgress,
            CurrentSubPhase = 3
        };

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.Contains("**Current Sub-Phase:** 3", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_NoDecisions_OmitsDecisionSection()
    {
        var state = CreateDefaultState();

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.DoesNotContain("## Decisions", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_NoFailure_OmitsFailureSection()
    {
        var state = CreateDefaultState() with
        {
            Status = PhaseStatus.InProgress
        };

        var markdown = _reporter.RenderProgressMarkdown(state);

        Assert.DoesNotContain("## Failure Details", markdown);
    }

    #endregion

    #region GenerateProgressDocumentAsync Tests

    [Fact]
    public async Task GenerateProgressDocumentAsync_CreatesDirectoryAndFile()
    {
        var state = CreateDefaultState();

        await _reporter.GenerateProgressDocumentAsync(state);

        Assert.True(File.Exists(_reporter.ProgressDocPath));
        var content = await File.ReadAllTextAsync(_reporter.ProgressDocPath);
        Assert.Contains("# Hugh Progress Report", content);
    }

    [Fact]
    public async Task GenerateProgressDocumentAsync_UpdatesExistingDocument()
    {
        var state1 = CreateDefaultState() with
        {
            Status = PhaseStatus.InProgress,
            CurrentPhase = LoopPhase.Vision
        };

        await _reporter.GenerateProgressDocumentAsync(state1);

        var state2 = state1 with
        {
            CurrentPhase = LoopPhase.Research,
            CompletedPhases = new List<LoopPhase> { LoopPhase.Vision }
        };

        await _reporter.GenerateProgressDocumentAsync(state2);

        var content = await File.ReadAllTextAsync(_reporter.ProgressDocPath);
        Assert.Contains("| Vision | ✅ Complete |", content);
        Assert.Contains("**Current Phase:** Research", content);
    }

    [Fact]
    public async Task GenerateProgressDocumentAsync_DirectoryAlreadyExists_Succeeds()
    {
        // Pre-create the directory
        Directory.CreateDirectory(Path.GetDirectoryName(_reporter.ProgressDocPath)!);

        var state = CreateDefaultState();
        await _reporter.GenerateProgressDocumentAsync(state);

        Assert.True(File.Exists(_reporter.ProgressDocPath));
    }

    #endregion

    #region RenderResearchProgress Tests

    [Fact]
    public void RenderResearchProgress_WithResearchDoc_RendersSubPhaseTable()
    {
        var researchContent = @"
## Research Phases

| # | Phase | Status | Scope | Date |
|---|-------|--------|-------|------|
| 1 | Architecture Review | ✅ Complete | Review current arch | 2026-02-27 |
| 2 | API Analysis | 🔄 In Progress | Analyze endpoints | — |
| 3 | Security Audit | ⏳ Not Started | Security review | — |
";
        var researchPath = Path.Combine(_testWorkspace, "research.md");
        File.WriteAllText(researchPath, researchContent);

        var result = _reporter.RenderResearchProgress(researchPath);

        Assert.Contains("| 1 | Architecture Review | ✅ |", result);
        Assert.Contains("| 2 | API Analysis | 🔄 |", result);
        Assert.Contains("| 3 | Security Audit | ⏳ |", result);
    }

    [Fact]
    public void RenderResearchProgress_NullPath_ReturnsEmpty()
    {
        var result = _reporter.RenderResearchProgress(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderResearchProgress_NonexistentFile_ReturnsEmpty()
    {
        var result = _reporter.RenderResearchProgress("/nonexistent/file.md");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderResearchProgress_FileWithNoPhases_ReturnsEmpty()
    {
        var researchPath = Path.Combine(_testWorkspace, "empty-research.md");
        File.WriteAllText(researchPath, "# Research Document\n\nNo phases here.");

        var result = _reporter.RenderResearchProgress(researchPath);

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region RenderPlanProgress Tests

    [Fact]
    public void RenderPlanProgress_WithPlanDoc_RendersSubPhaseTable()
    {
        var planContent = @"
## Progress Summary

| Phase | Name | Status | Tasks |
|-------|------|--------|-------|
| 1 | Setup | ✅ Complete | 3/3 |
| 2 | Core Logic | 🔄 In Progress | 2/5 |
| 3 | Testing | ⏳ Not Started | 0/4 |
";
        var planPath = Path.Combine(_testWorkspace, "plan.md");
        File.WriteAllText(planPath, planContent);

        var result = _reporter.RenderPlanProgress(planPath);

        Assert.Contains("| 1 | Setup | ✅ | 3/3 |", result);
        Assert.Contains("| 2 | Core Logic | 🔄 | 2/5 |", result);
        Assert.Contains("| 3 | Testing | ⏳ | 0/4 |", result);
    }

    [Fact]
    public void RenderPlanProgress_NullPath_ReturnsEmpty()
    {
        var result = _reporter.RenderPlanProgress(null);

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderPlanProgress_NonexistentFile_ReturnsEmpty()
    {
        var result = _reporter.RenderPlanProgress("/nonexistent/plan.md");

        Assert.Equal(string.Empty, result);
    }

    [Fact]
    public void RenderPlanProgress_FileWithNoPhases_ReturnsEmpty()
    {
        var planPath = Path.Combine(_testWorkspace, "empty-plan.md");
        File.WriteAllText(planPath, "# Plan Document\n\nNo phases here.");

        var result = _reporter.RenderPlanProgress(planPath);

        Assert.Equal(string.Empty, result);
    }

    #endregion

    #region ProgressDocPath Tests

    [Fact]
    public void ProgressDocPath_DefaultFileName_ReturnsCorrectPath()
    {
        var expectedPath = Path.Combine(_docsPath, "progress", "progress.md");

        Assert.Equal(expectedPath, _reporter.ProgressDocPath);
    }

    [Fact]
    public void ProgressDocPath_CustomFileName_ReturnsCorrectPath()
    {
        var customReporter = new ProgressReporter(
            _docsPath, _researchPhaseParser, _planPhaseParser,
            Log.Logger, "release-1-progress.md");

        var expectedPath = Path.Combine(_docsPath, "progress", "release-1-progress.md");

        Assert.Equal(expectedPath, customReporter.ProgressDocPath);
    }

    #endregion

    #region IProgressReporter Hook Tests

    [Fact]
    public async Task OnPhaseUpdatedAsync_DoesNotThrow()
    {
        await _reporter.OnPhaseUpdatedAsync(LoopPhase.Vision, PhaseStatus.InProgress, null);
    }

    [Fact]
    public async Task OnPhaseCompletedAsync_DoesNotThrow()
    {
        await _reporter.OnPhaseCompletedAsync(LoopPhase.Vision, "docs/vision/001.md");
    }

    [Fact]
    public async Task OnPhaseFailedAsync_DoesNotThrow()
    {
        await _reporter.OnPhaseFailedAsync(LoopPhase.Research, "Connection timeout");
    }

    #endregion

    #region LoopStateManager Integration Tests

    [Fact]
    public async Task LoopStateManager_WithProgressReporter_GeneratesProgressDoc()
    {
        var stateManager = new LoopStateManager(
            _testWorkspace, Log.Logger, progressReporter: _reporter);

        await stateManager.CreateRunAsync("Test feature");

        // The progress document should be generated after CreateRunAsync -> SaveStateAsync
        Assert.True(File.Exists(_reporter.ProgressDocPath));
        var content = await File.ReadAllTextAsync(_reporter.ProgressDocPath);
        Assert.Contains("# Hugh Progress Report", content);
        Assert.Contains("Test feature", content);
    }

    [Fact]
    public async Task LoopStateManager_WithNullReporter_DoesNotThrow()
    {
        var stateManager = new LoopStateManager(
            _testWorkspace, Log.Logger, progressReporter: null);

        var state = await stateManager.CreateRunAsync("Test feature");

        Assert.NotNull(state);
        Assert.Equal("Test feature", state.FeatureRequest);
    }

    [Fact]
    public async Task LoopStateManager_PhaseComplete_UpdatesProgressDoc()
    {
        var stateManager = new LoopStateManager(
            _testWorkspace, Log.Logger, progressReporter: _reporter);

        await stateManager.CreateRunAsync("Test feature");
        await stateManager.UpdatePhaseAsync(LoopPhase.Vision, PhaseStatus.InProgress);
        await stateManager.MarkPhaseCompleteAsync("docs/vision/001.md");

        var content = await File.ReadAllTextAsync(_reporter.ProgressDocPath);
        Assert.Contains("| Vision | ✅ Complete |", content);
    }

    [Fact]
    public async Task LoopStateManager_PhaseFailed_UpdatesProgressDoc()
    {
        var stateManager = new LoopStateManager(
            _testWorkspace, Log.Logger, progressReporter: _reporter);

        await stateManager.CreateRunAsync("Test feature");
        await stateManager.UpdatePhaseAsync(LoopPhase.Vision, PhaseStatus.InProgress);
        await stateManager.MarkPhaseFailedAsync("LLM timeout error");

        var content = await File.ReadAllTextAsync(_reporter.ProgressDocPath);
        Assert.Contains("## Failure Details", content);
        Assert.Contains("LLM timeout error", content);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RenderProgressMarkdown_DecisionWithPipeCharacter_EscapesProperly()
    {
        var state = CreateDefaultState() with
        {
            Decisions = new List<Decision>
            {
                new()
                {
                    Phase = "Vision",
                    Question = "Option A | Option B?",
                    Selected = "A",
                    Rationale = "Better | cleaner"
                }
            }
        };

        var markdown = _reporter.RenderProgressMarkdown(state);

        // Pipe characters should be escaped in table cells
        Assert.Contains(@"Option A \| Option B?", markdown);
        Assert.Contains(@"Better \| cleaner", markdown);
    }

    [Fact]
    public void RenderProgressMarkdown_DocumentPathsWithNoDocuments_ShowsDash()
    {
        var state = CreateDefaultState();

        var markdown = _reporter.RenderProgressMarkdown(state);

        // All document paths should show "—" when no documents exist
        Assert.Contains("| Vision | ⏳ Not Started | — |", markdown);
    }

    #endregion

    #region Helpers

    private static LoopState CreateDefaultState()
    {
        return new LoopState
        {
            RunId = Guid.Parse("12345678-1234-1234-1234-123456789abc"),
            StartedAt = new DateTime(2026, 2, 27, 10, 0, 0, DateTimeKind.Utc),
            FeatureRequest = "Add authentication",
            CurrentPhase = LoopPhase.Vision,
            CurrentSubPhase = null,
            CompletedPhases = new List<LoopPhase>(),
            Documents = new DocumentPaths(),
            Decisions = new List<Decision>(),
            Status = PhaseStatus.NotStarted,
            FailureReason = null
        };
    }

    #endregion
}
