using System.Text.RegularExpressions;
using Hugh.Models;
using Hugh.Orchestration;
using Serilog;

namespace Hugh.Tests.Orchestration;

/// <summary>
/// Unit tests for SolutionOrchestrator.
/// Tests triage classification regex, state management delegation,
/// and HasExistingSolution routing.
/// Note: Full integration tests with CopilotService require the GitHub Copilot SDK.
/// </summary>
public partial class SolutionOrchestratorTests : IDisposable
{
    private readonly string _testWorkspace;

    // Mirror the regex from SolutionOrchestrator for testing
    [GeneratedRegex(
        @"\*\*Classification:\*\*\s*(FEATURE|SOLUTION)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex TriageClassificationRegex();

    public SolutionOrchestratorTests()
    {
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"HughSolutionOrcTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testWorkspace))
        {
            try { Directory.Delete(_testWorkspace, recursive: true); }
            catch { /* cleanup best-effort */ }
        }
    }

    #region Triage Classification Regex Tests

    [Fact]
    public void TriageRegex_FeatureClassification_Matches()
    {
        var response = "**Classification:** FEATURE\n**Reasoning:** Simple single-feature request.";

        var match = TriageClassificationRegex().Match(response);

        Assert.True(match.Success);
        Assert.Equal("FEATURE", match.Groups[1].Value);
    }

    [Fact]
    public void TriageRegex_SolutionClassification_Matches()
    {
        var response = "**Classification:** SOLUTION\n**Reasoning:** Multi-component system.";

        var match = TriageClassificationRegex().Match(response);

        Assert.True(match.Success);
        Assert.Equal("SOLUTION", match.Groups[1].Value);
    }

    [Fact]
    public void TriageRegex_CaseInsensitive_Matches()
    {
        var response = "**Classification:** feature\n**Reasoning:** Simple request.";

        var match = TriageClassificationRegex().Match(response);

        Assert.True(match.Success);
        Assert.Equal("feature", match.Groups[1].Value);
    }

    [Fact]
    public void TriageRegex_MixedCase_Matches()
    {
        var response = "**Classification:** Solution\n**Reasoning:** Multi-release.";

        var match = TriageClassificationRegex().Match(response);

        Assert.True(match.Success);
        Assert.Equal("Solution", match.Groups[1].Value);
    }

    [Fact]
    public void TriageRegex_WithExtraSpaces_Matches()
    {
        var response = "**Classification:**   FEATURE  \n**Reasoning:** Simple.";

        var match = TriageClassificationRegex().Match(response);

        Assert.True(match.Success);
        Assert.Equal("FEATURE", match.Groups[1].Value);
    }

    [Fact]
    public void TriageRegex_NoClassification_DoesNotMatch()
    {
        var response = "I think this is a simple feature request.\nIt should be handled normally.";

        var match = TriageClassificationRegex().Match(response);

        Assert.False(match.Success);
    }

    [Fact]
    public void TriageRegex_InvalidClassification_DoesNotMatch()
    {
        var response = "**Classification:** UNKNOWN\n**Reasoning:** Cannot determine.";

        var match = TriageClassificationRegex().Match(response);

        Assert.False(match.Success);
    }

    [Fact]
    public void TriageRegex_EmbeddedInLongerResponse_Matches()
    {
        var response = """
            Based on my analysis of the user's request, I have determined the following:

            The request involves building a comprehensive task management platform with multiple
            distinct features including authentication, boards, real-time collaboration, and more.

            **Classification:** SOLUTION
            **Reasoning:** The request describes a multi-component system that would benefit from
            being broken down into multiple releases, starting with an MVP and building incrementally.
            **Confidence:** High
            """;

        var match = TriageClassificationRegex().Match(response);

        Assert.True(match.Success);
        Assert.Equal("SOLUTION", match.Groups[1].Value);
    }

    #endregion

    #region HasExistingSolution Tests

    [Fact]
    public void HasExistingSolution_NoStateFile_ReturnsFalse()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);

        Assert.False(manager.HasExistingSolution());
    }

    [Fact]
    public async Task HasExistingSolution_WithStateFile_ReturnsTrue()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);
        await manager.CreateSolutionAsync("Build something");

        Assert.True(manager.HasExistingSolution());
    }

    #endregion

    #region Triage Classification Logic Tests

    [Theory]
    [InlineData("SOLUTION", SolutionMode.MultiRelease)]
    [InlineData("solution", SolutionMode.MultiRelease)]
    [InlineData("Solution", SolutionMode.MultiRelease)]
    [InlineData("FEATURE", SolutionMode.SingleFeature)]
    [InlineData("feature", SolutionMode.SingleFeature)]
    [InlineData("Feature", SolutionMode.SingleFeature)]
    public void ClassificationToMode_MapsCorrectly(string classification, SolutionMode expectedMode)
    {
        // Simulate the classification-to-mode logic from SolutionOrchestrator.RunTriageAsync
        var mode = classification.ToUpperInvariant() == "SOLUTION"
            ? SolutionMode.MultiRelease
            : SolutionMode.SingleFeature;

        Assert.Equal(expectedMode, mode);
    }

    #endregion

    #region State Flow Tests

    [Fact]
    public async Task SolutionWorkflow_TriageToExecution_PhasesProgressCorrectly()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);

        // Triage
        await manager.CreateSolutionAsync("Build a platform");
        await manager.UpdatePhaseAsync(SolutionPhase.Triage, PhaseStatus.InProgress);
        Assert.Equal(SolutionPhase.Triage, manager.CurrentState!.CurrentPhase);

        // Classified as multi-release
        await manager.SetModeAsync(SolutionMode.MultiRelease);
        Assert.Equal(SolutionMode.MultiRelease, manager.CurrentState!.Mode);

        // Clarification
        await manager.UpdatePhaseAsync(SolutionPhase.Clarification, PhaseStatus.InProgress);
        Assert.Equal(SolutionPhase.Clarification, manager.CurrentState!.CurrentPhase);

        await manager.UpdateDocumentsAsync(new SolutionDocumentPaths
        {
            ClarifiedVision = "/docs/vision/abc.md"
        });

        // Decomposition
        await manager.UpdatePhaseAsync(SolutionPhase.Decomposition, PhaseStatus.InProgress);
        Assert.Equal(SolutionPhase.Decomposition, manager.CurrentState!.CurrentPhase);

        await manager.UpdateDocumentsAsync(manager.CurrentState.Documents with
        {
            Roadmap = "/docs/roadmap/abc.md"
        });

        await manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 2, Name = "Extended", Status = ReleaseStatus.Pending }
        });

        // Execution
        await manager.UpdatePhaseAsync(SolutionPhase.Execution, PhaseStatus.InProgress);
        Assert.Equal(SolutionPhase.Execution, manager.CurrentState!.CurrentPhase);

        // Complete releases
        await manager.UpdateReleaseStatusAsync(0, ReleaseStatus.Complete);
        await manager.UpdateReleaseStatusAsync(1, ReleaseStatus.Complete);

        // Mark complete
        await manager.MarkCompleteAsync();
        Assert.Equal(SolutionPhase.Complete, manager.CurrentState!.CurrentPhase);
        Assert.Equal(PhaseStatus.Complete, manager.CurrentState.Status);
    }

    [Fact]
    public async Task SolutionWorkflow_FailureMidRelease_SetsCorrectState()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);

        await manager.CreateSolutionAsync("Build a platform");
        await manager.SetModeAsync(SolutionMode.MultiRelease);
        await manager.SetReleasesAsync(new List<ReleaseDefinition>
        {
            new() { ReleaseNumber = 1, Name = "MVP", Status = ReleaseStatus.Pending },
            new() { ReleaseNumber = 2, Name = "Extended", Status = ReleaseStatus.Pending }
        });

        await manager.UpdateReleaseStatusAsync(0, ReleaseStatus.Complete);
        await manager.UpdateReleaseStatusAsync(1, ReleaseStatus.InProgress);

        // Simulate failure during release 2
        await manager.UpdateReleaseStatusAsync(1, ReleaseStatus.Failed);
        await manager.MarkFailedAsync("Release 2 coding phase failed");

        Assert.Equal(PhaseStatus.Failed, manager.CurrentState!.Status);
        Assert.Equal("Release 2 coding phase failed", manager.CurrentState.FailureReason);
        Assert.Equal(ReleaseStatus.Complete, manager.CurrentState.Releases[0].Status);
        Assert.Equal(ReleaseStatus.Failed, manager.CurrentState.Releases[1].Status);
    }

    [Fact]
    public async Task SolutionWorkflow_SingleFeature_StaysInSingleFeatureMode()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);

        await manager.CreateSolutionAsync("Add a login page");
        await manager.SetModeAsync(SolutionMode.SingleFeature);

        Assert.Equal(SolutionMode.SingleFeature, manager.CurrentState!.Mode);
        Assert.Empty(manager.CurrentState.Releases);
    }

    #endregion

    #region Per-Release LoopOrchestrator Integration Tests

    [Fact]
    public async Task PerReleaseExecution_CreatesIsolatedStateManagers()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);
        await manager.CreateSolutionAsync("Test");

        var release1 = manager.CreateReleaseStateManager(1);
        var release2 = manager.CreateReleaseStateManager(2);

        var state1 = await release1.CreateRunAsync("Release 1: MVP");
        var state2 = await release2.CreateRunAsync("Release 2: Extended");

        // Modify release 1 independently
        await release1.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);
        await release1.MarkPhaseCompleteAsync("/docs/research/r1.md");

        // Release 2 should be unaffected
        Assert.Equal(LoopPhase.Vision, release2.CurrentState!.CurrentPhase);

        // Release 1 should have progressed
        Assert.Contains(LoopPhase.Research, release1.CurrentState!.CompletedPhases);
    }

    [Fact]
    public void PerReleaseExecution_StateDirectoriesAreIsolated()
    {
        var manager = new SolutionStateManager(_testWorkspace, Log.Logger);

        var dir1 = manager.GetReleaseStateDirectory(1);
        var dir2 = manager.GetReleaseStateDirectory(2);

        Assert.NotEqual(dir1, dir2);
        Assert.Contains("release-1", dir1);
        Assert.Contains("release-2", dir2);
        Assert.True(Directory.Exists(dir1));
        Assert.True(Directory.Exists(dir2));
    }

    #endregion
}
