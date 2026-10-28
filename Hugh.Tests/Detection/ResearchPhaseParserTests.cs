using Hugh.Detection;

namespace Hugh.Tests.Detection;

/// <summary>
/// Unit tests for ResearchPhaseParser.
/// Tests parsing of research document phase tables, status detection, and scope extraction.
/// </summary>
public class ResearchPhaseParserTests
{
    private readonly ResearchPhaseParser _parser = new();

    #region ParsePhases

    [Fact]
    public void ParsePhases_WithValidResearchTable_ReturnsAllPhases()
    {
        var content = """
            # Research Document

            ## Research Phases

            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Architecture Review | ✅ Complete | Review architecture | 2026-02-20 |
            | 2 | API Surface Analysis | 🔄 In Progress | Analyze API endpoints | 2026-02-21 |
            | 3 | Data Model Study | ⏳ Not Started | Study data models | — |
            | 4 | Security Assessment | ⏳ Not Started | Assess security concerns | — |
            | 5 | Performance Baseline | ❌ Blocked | Measure current performance | — |
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(5, phases.Count);

        Assert.Equal(1, phases[0].Number);
        Assert.Equal("Architecture Review", phases[0].Name);
        Assert.Equal("✅", phases[0].StatusEmoji);
        Assert.Equal(PhaseProgressStatus.Complete, phases[0].Status);

        Assert.Equal(2, phases[1].Number);
        Assert.Equal("API Surface Analysis", phases[1].Name);
        Assert.Equal("🔄", phases[1].StatusEmoji);
        Assert.Equal(PhaseProgressStatus.InProgress, phases[1].Status);

        Assert.Equal(3, phases[2].Number);
        Assert.Equal("Data Model Study", phases[2].Name);
        Assert.Equal("⏳", phases[2].StatusEmoji);
        Assert.Equal(PhaseProgressStatus.NotStarted, phases[2].Status);

        Assert.Equal(4, phases[3].Number);
        Assert.Equal("Security Assessment", phases[3].Name);
        Assert.Equal(PhaseProgressStatus.NotStarted, phases[3].Status);

        Assert.Equal(5, phases[4].Number);
        Assert.Equal("Performance Baseline", phases[4].Name);
        Assert.Equal("❌", phases[4].StatusEmoji);
        Assert.Equal(PhaseProgressStatus.Blocked, phases[4].Status);
    }

    [Fact]
    public void ParsePhases_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(_parser.ParsePhases(""));
        Assert.Empty(_parser.ParsePhases("   "));
        Assert.Empty(_parser.ParsePhases(null!));
    }

    [Fact]
    public void ParsePhases_ExtractsScopeText()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Setup | ✅ Complete | Initialize project structure and dependencies | 2026-02-20 |
            | 2 | Core Logic | ⏳ Not Started | Implement main business rules and validation | — |
            """;

        var phases = _parser.ParsePhases(content);

        Assert.Equal(2, phases.Count);
        Assert.Equal("Initialize project structure and dependencies", phases[0].Scope);
        Assert.Equal("Implement main business rules and validation", phases[1].Scope);
    }

    #endregion

    #region FindNextIncompletePhase

    [Fact]
    public void FindNextIncompletePhase_WithMixedStatuses_ReturnsFirstIncomplete()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ✅ Complete | Scope 2 | 2026-02-21 |
            | 3 | Phase Three | ⏳ Not Started | Scope 3 | — |
            | 4 | Phase Four | ⏳ Not Started | Scope 4 | — |
            | 5 | Phase Five | ⏳ Not Started | Scope 5 | — |
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.NotNull(next);
        Assert.Equal(3, next.Number);
        Assert.Equal("Phase Three", next.Name);
        Assert.Equal(PhaseProgressStatus.NotStarted, next.Status);
    }

    [Fact]
    public void FindNextIncompletePhase_AllComplete_ReturnsNull()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ✅ Complete | Scope 2 | 2026-02-21 |
            | 3 | Phase Three | ✅ Complete | Scope 3 | 2026-02-22 |
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.Null(next);
    }

    [Fact]
    public void FindNextIncompletePhase_InProgressPhase_ReturnsInProgress()
    {
        // A 🔄 In Progress phase should be returned before ⏳ Not Started phases
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ⏳ Not Started | Scope 2 | — |
            | 3 | Phase Three | 🔄 In Progress | Scope 3 | 2026-02-21 |
            | 4 | Phase Four | ⏳ Not Started | Scope 4 | — |
            """;

        var next = _parser.FindNextIncompletePhase(content);

        Assert.NotNull(next);
        Assert.Equal(3, next.Number);
        Assert.Equal("Phase Three", next.Name);
        Assert.Equal(PhaseProgressStatus.InProgress, next.Status);
    }

    #endregion

    #region HasIncompletePhases

    [Fact]
    public void HasIncompletePhases_WithNotStartedPhases_ReturnsTrue()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ⏳ Not Started | Scope 2 | — |
            """;

        Assert.True(_parser.HasIncompletePhases(content));
    }

    [Fact]
    public void HasIncompletePhases_WithInProgressPhases_ReturnsTrue()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | 🔄 In Progress | Scope 2 | 2026-02-21 |
            """;

        Assert.True(_parser.HasIncompletePhases(content));
    }

    [Fact]
    public void HasIncompletePhases_AllComplete_ReturnsFalse()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ✅ Complete | Scope 2 | 2026-02-21 |
            | 3 | Phase Three | ✅ Complete | Scope 3 | 2026-02-22 |
            """;

        Assert.False(_parser.HasIncompletePhases(content));
    }

    [Fact]
    public void HasIncompletePhases_EmptyContent_ReturnsFalse()
    {
        Assert.False(_parser.HasIncompletePhases(""));
        Assert.False(_parser.HasIncompletePhases("   "));
    }

    #endregion

    #region GetIncompletePhaseNumbers

    [Fact]
    public void GetIncompletePhaseNumbers_ReturnsMixedIncompletePhases()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ⏳ Not Started | Scope 2 | — |
            | 3 | Phase Three | 🔄 In Progress | Scope 3 | 2026-02-21 |
            | 4 | Phase Four | ✅ Complete | Scope 4 | 2026-02-22 |
            | 5 | Phase Five | ⏳ Not Started | Scope 5 | — |
            """;

        var incomplete = _parser.GetIncompletePhaseNumbers(content);

        Assert.Equal(3, incomplete.Count);
        Assert.Contains(2, incomplete);
        Assert.Contains(3, incomplete);
        Assert.Contains(5, incomplete);
    }

    [Fact]
    public void GetIncompletePhaseNumbers_AllComplete_ReturnsEmpty()
    {
        var content = """
            | Phase | Name | Status | Scope | Session |
            |-------|------|--------|-------|---------|
            | 1 | Phase One | ✅ Complete | Scope 1 | 2026-02-20 |
            | 2 | Phase Two | ✅ Complete | Scope 2 | 2026-02-21 |
            """;

        Assert.Empty(_parser.GetIncompletePhaseNumbers(content));
    }

    [Fact]
    public void GetIncompletePhaseNumbers_EmptyContent_ReturnsEmpty()
    {
        Assert.Empty(_parser.GetIncompletePhaseNumbers(""));
    }

    #endregion
}
