using System.Text.Json;
using System.Text.Json.Serialization;
using Hugh.Models;

namespace Hugh.Tests.Models;

/// <summary>
/// Unit tests for SolutionState models.
/// Tests serialization round-trip, default values, and enum handling.
/// </summary>
public class SolutionStateTests
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    #region SolutionState Defaults

    [Fact]
    public void SolutionState_DefaultValues_AreCorrect()
    {
        var state = new SolutionState();

        Assert.Equal(Guid.Empty, state.SolutionId);
        Assert.Equal("", state.OriginalRequest);
        Assert.Equal(SolutionMode.SingleFeature, state.Mode);
        Assert.Equal(SolutionPhase.Triage, state.CurrentPhase);
        Assert.NotNull(state.Documents);
        Assert.Empty(state.Releases);
        Assert.Equal(0, state.CurrentReleaseIndex);
        Assert.Equal(PhaseStatus.NotStarted, state.Status);
        Assert.Null(state.FailureReason);
        Assert.Empty(state.Decisions);
    }

    [Fact]
    public void SolutionDocumentPaths_DefaultValues_AreNull()
    {
        var docs = new SolutionDocumentPaths();

        Assert.Null(docs.ClarifiedVision);
        Assert.Null(docs.Roadmap);
    }

    [Fact]
    public void ReleaseDefinition_DefaultValues_AreCorrect()
    {
        var release = new ReleaseDefinition();

        Assert.Equal(0, release.ReleaseNumber);
        Assert.Equal("", release.Name);
        Assert.Equal("", release.Description);
        Assert.Equal("", release.Scope);
        Assert.Equal(Guid.Empty, release.RunId);
        Assert.Equal(ReleaseStatus.Pending, release.Status);
    }

    #endregion

    #region Serialization Round-Trip

    [Fact]
    public void SolutionState_SerializesAndDeserializes_Correctly()
    {
        var original = new SolutionState
        {
            SolutionId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            OriginalRequest = "Build a task management platform",
            Mode = SolutionMode.MultiRelease,
            CurrentPhase = SolutionPhase.Execution,
            Documents = new SolutionDocumentPaths
            {
                ClarifiedVision = "/docs/vision/abc-clarified-vision.md",
                Roadmap = "/docs/roadmap/abc-roadmap.md"
            },
            Releases = new List<ReleaseDefinition>
            {
                new()
                {
                    ReleaseNumber = 1,
                    Name = "MVP — Core Features",
                    Description = "Basic task management",
                    Scope = "- Task CRUD\n- User auth",
                    RunId = Guid.NewGuid(),
                    Status = ReleaseStatus.Complete
                },
                new()
                {
                    ReleaseNumber = 2,
                    Name = "Collaboration",
                    Description = "Team features",
                    Scope = "- Shared boards\n- Comments",
                    RunId = Guid.NewGuid(),
                    Status = ReleaseStatus.InProgress
                }
            },
            CurrentReleaseIndex = 1,
            Status = PhaseStatus.InProgress,
            FailureReason = null,
            Decisions = new List<Decision>
            {
                new()
                {
                    Phase = "Triage",
                    Question = "Classification",
                    Selected = "SOLUTION",
                    Autonomous = true,
                    Timestamp = DateTime.UtcNow
                }
            }
        };

        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SolutionState>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(original.SolutionId, deserialized.SolutionId);
        Assert.Equal(original.OriginalRequest, deserialized.OriginalRequest);
        Assert.Equal(original.Mode, deserialized.Mode);
        Assert.Equal(original.CurrentPhase, deserialized.CurrentPhase);
        Assert.Equal(original.Documents.ClarifiedVision, deserialized.Documents.ClarifiedVision);
        Assert.Equal(original.Documents.Roadmap, deserialized.Documents.Roadmap);
        Assert.Equal(2, deserialized.Releases.Count);
        Assert.Equal("MVP — Core Features", deserialized.Releases[0].Name);
        Assert.Equal(ReleaseStatus.Complete, deserialized.Releases[0].Status);
        Assert.Equal(ReleaseStatus.InProgress, deserialized.Releases[1].Status);
        Assert.Equal(1, deserialized.CurrentReleaseIndex);
        Assert.Single(deserialized.Decisions);
    }

    [Fact]
    public void SolutionState_WithSpecialCharacters_SurvivesRoundTrip()
    {
        var original = new SolutionState
        {
            SolutionId = Guid.NewGuid(),
            OriginalRequest = "Build a platform with <HTML> & \"quotes\" and 'apostrophes'",
            Mode = SolutionMode.MultiRelease,
            CurrentPhase = SolutionPhase.Clarification
        };

        var json = JsonSerializer.Serialize(original, _jsonOptions);
        var deserialized = JsonSerializer.Deserialize<SolutionState>(json, _jsonOptions);

        Assert.NotNull(deserialized);
        Assert.Equal(original.OriginalRequest, deserialized.OriginalRequest);
    }

    #endregion

    #region Enum Serialization

    [Theory]
    [InlineData(SolutionMode.SingleFeature, "singleFeature")]
    [InlineData(SolutionMode.MultiRelease, "multiRelease")]
    public void SolutionMode_SerializesAsCamelCase(SolutionMode mode, string expected)
    {
        var json = JsonSerializer.Serialize(mode, _jsonOptions);
        Assert.Contains(expected, json);
    }

    [Theory]
    [InlineData(SolutionPhase.Triage, "triage")]
    [InlineData(SolutionPhase.Clarification, "clarification")]
    [InlineData(SolutionPhase.Decomposition, "decomposition")]
    [InlineData(SolutionPhase.Execution, "execution")]
    [InlineData(SolutionPhase.Complete, "complete")]
    public void SolutionPhase_SerializesAsCamelCase(SolutionPhase phase, string expected)
    {
        var json = JsonSerializer.Serialize(phase, _jsonOptions);
        Assert.Contains(expected, json);
    }

    [Theory]
    [InlineData(ReleaseStatus.Pending, "pending")]
    [InlineData(ReleaseStatus.InProgress, "inProgress")]
    [InlineData(ReleaseStatus.Complete, "complete")]
    [InlineData(ReleaseStatus.Failed, "failed")]
    [InlineData(ReleaseStatus.Skipped, "skipped")]
    public void ReleaseStatus_SerializesAsCamelCase(ReleaseStatus status, string expected)
    {
        var json = JsonSerializer.Serialize(status, _jsonOptions);
        Assert.Contains(expected, json);
    }

    #endregion

    #region Record With-Expressions

    [Fact]
    public void SolutionState_WithExpression_CreatesModifiedCopy()
    {
        var original = new SolutionState
        {
            SolutionId = Guid.NewGuid(),
            OriginalRequest = "Test",
            Mode = SolutionMode.SingleFeature,
            CurrentPhase = SolutionPhase.Triage
        };

        var modified = original with
        {
            Mode = SolutionMode.MultiRelease,
            CurrentPhase = SolutionPhase.Clarification
        };

        // Original unchanged
        Assert.Equal(SolutionMode.SingleFeature, original.Mode);
        Assert.Equal(SolutionPhase.Triage, original.CurrentPhase);

        // Modified has new values
        Assert.Equal(SolutionMode.MultiRelease, modified.Mode);
        Assert.Equal(SolutionPhase.Clarification, modified.CurrentPhase);

        // Unchanged fields preserved
        Assert.Equal(original.SolutionId, modified.SolutionId);
        Assert.Equal(original.OriginalRequest, modified.OriginalRequest);
    }

    [Fact]
    public void SolutionDocumentPaths_WithExpression_PreservesOtherFields()
    {
        var original = new SolutionDocumentPaths
        {
            ClarifiedVision = "/path/to/vision.md"
        };

        var modified = original with { Roadmap = "/path/to/roadmap.md" };

        Assert.Equal("/path/to/vision.md", modified.ClarifiedVision);
        Assert.Equal("/path/to/roadmap.md", modified.Roadmap);
    }

    [Fact]
    public void ReleaseDefinition_WithExpression_UpdatesStatus()
    {
        var original = new ReleaseDefinition
        {
            ReleaseNumber = 1,
            Name = "MVP",
            Status = ReleaseStatus.Pending
        };

        var modified = original with { Status = ReleaseStatus.Complete };

        Assert.Equal(ReleaseStatus.Pending, original.Status);
        Assert.Equal(ReleaseStatus.Complete, modified.Status);
        Assert.Equal(original.ReleaseNumber, modified.ReleaseNumber);
    }

    #endregion

    #region ReleaseContext

    [Fact]
    public void ReleaseContext_DefaultValues_AreCorrect()
    {
        var ctx = new ReleaseContext();

        Assert.Equal(0, ctx.ReleaseNumber);
        Assert.Equal("", ctx.ReleaseName);
        Assert.Equal("", ctx.ReleaseScope);
        Assert.Equal("", ctx.SolutionVisionPath);
        Assert.Equal("", ctx.RoadmapPath);
        Assert.Empty(ctx.CompletedReleases);
    }

    [Fact]
    public void ReleaseContext_WithValues_SetsCorrectly()
    {
        var ctx = new ReleaseContext
        {
            ReleaseNumber = 2,
            ReleaseName = "Collaboration Features",
            ReleaseScope = "- Shared boards\n- Comments",
            SolutionVisionPath = "/docs/vision/abc.md",
            RoadmapPath = "/docs/roadmap/abc.md",
            CompletedReleases = new List<string> { "Release 1: MVP" }
        };

        Assert.Equal(2, ctx.ReleaseNumber);
        Assert.Equal("Collaboration Features", ctx.ReleaseName);
        Assert.Single(ctx.CompletedReleases);
    }

    #endregion
}
