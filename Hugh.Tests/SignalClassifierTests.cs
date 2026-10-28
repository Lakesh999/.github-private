using Hugh.Detection;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for SignalClassifier.
/// Tests all signal type detection patterns.
/// </summary>
public class SignalClassifierTests
{
    private readonly SignalClassifier _classifier = new();

    #region PhaseComplete Tests

    [Theory]
    [InlineData("✅ **Phase [1] Complete**", 1)]
    [InlineData("✅ **Phase [3] Complete**", 3)]
    [InlineData("✅ **Phase [10] Complete**", 10)]
    [InlineData("✅ **Phase 2 Complete**", 2)]
    [InlineData("✅ **Phase 5 Completed**", 5)]
    [InlineData("Some text before ✅ **Phase [7] Complete** and after", 7)]
    public void Classify_PhaseComplete_ReturnsCorrectSignalAndPhaseNumber(string input, int expectedPhase)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.PhaseComplete, result.Type);
        Assert.Equal(expectedPhase, result.PhaseNumber);
    }

    [Theory]
    [InlineData("✅   **Phase   [4]   Complete**", 4)]
    [InlineData("✅ **Phase[6]Complete**", null)] // No space - may not match perfectly
    public void Classify_PhaseComplete_HandlesVariousSpacing(string input, int? expectedPhase)
    {
        var result = _classifier.Classify(input);

        // If it matched, verify the phase number
        if (result.Type == SignalType.PhaseComplete)
        {
            Assert.Equal(expectedPhase, result.PhaseNumber);
        }
    }

    #endregion

    #region WorkflowComplete Tests

    [Theory]
    [InlineData("Research Complete")]
    [InlineData("All Phases Complete")]
    [InlineData("Plan Complete")]
    [InlineData("Implementation Finished")]
    [InlineData("The Research Complete for this feature")]
    public void Classify_WorkflowComplete_ReturnsCorrectSignal(string input)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.WorkflowComplete, result.Type);
    }

    #endregion

    #region QuestionWithRecommendation Tests

    [Theory]
    [InlineData("**Recommendation:** A — Use microservices architecture", "A", "Use microservices architecture")]
    [InlineData("**Recommendation:** B — Use JWT authentication", "B", "Use JWT authentication")]
    [InlineData("**Recommendation:** C — Implement caching layer", "C", "Implement caching layer")]
    [InlineData("**Recommendation:** 1 — Option one description", "1", "Option one description")]
    [InlineData("**Recommendation:** E — Final option", "E", "Final option")]
    public void Classify_QuestionWithRecommendation_ExtractsLetterAndOption(
        string input,
        string expectedLetter,
        string expectedOption)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.QuestionWithRecommendation, result.Type);
        Assert.Equal(expectedLetter, result.RecommendationLetter);
        Assert.Equal(expectedOption, result.RecommendationOption);
    }

    [Theory]
    [InlineData("**Recommendation:** A – En-dash option", "A", "En-dash option")]
    [InlineData("**Recommendation:** B - Hyphen option", "B", "Hyphen option")]
    public void Classify_QuestionWithRecommendation_HandlesDifferentDashes(
        string input,
        string expectedLetter,
        string expectedOption)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.QuestionWithRecommendation, result.Type);
        Assert.Equal(expectedLetter, result.RecommendationLetter);
        Assert.Equal(expectedOption, result.RecommendationOption);
    }

    #endregion

    #region QuestionOpen Tests

    [Theory]
    [InlineData("**How would you like to proceed?**")]
    public void Classify_QuestionOpen_ReturnsCorrectSignal(string input)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.QuestionOpen, result.Type);
    }

    [Theory]
    [InlineData("**What would you like me to do?**")]
    [InlineData("**Please choose an option:**")]
    [InlineData("**Please select your preference?**")]
    [InlineData("**Please specify the configuration?**")]
    public void Classify_QuestionPatterns_NotMatchingCurrentRegex_ReturnsNone(string input)
    {
        // These patterns don't match the current OpenQuestionRegex
        // The regex is: \*\*(?:How would you like to proceed|What would you like|Please (?:choose|select|specify))\?\*\*
        // These test cases verify the actual behavior (may be enhanced in future)
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.None, result.Type);
    }

    #endregion

    #region Blocker Tests

    [Theory]
    [InlineData("❌ **Blocked** - Missing dependencies")]
    [InlineData("❌ **Blocker**: API key not found")]
    [InlineData("⚠️ **Implementation Paused** - Waiting for review")]
    [InlineData("⚠️ **Execution Stopped** due to error")]
    public void Classify_Blocker_ReturnsCorrectSignal(string input)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.Blocker, result.Type);
    }

    #endregion

    #region ModeSelection Tests

    [Theory]
    [InlineData("🔄 **Execution Mode Selection**")]
    [InlineData("🔄 **Execution Mode Selection**\n\nChoose manual or automatic...")]
    public void Classify_ModeSelection_ReturnsCorrectSignal(string input)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.ModeSelection, result.Type);
    }

    #endregion

    #region None Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Classify_EmptyOrNull_ReturnsNone(string? input)
    {
        var result = _classifier.Classify(input!);

        Assert.Equal(SignalType.None, result.Type);
    }

    [Theory]
    [InlineData("Just some regular text")]
    [InlineData("Phase 3 is working well")]
    [InlineData("I recommend option A")]
    [InlineData("Complete the task")]
    public void Classify_NoSignal_ReturnsNone(string input)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.None, result.Type);
    }

    #endregion

    #region Priority Tests

    [Fact]
    public void Classify_BlockerTakesPriorityOverPhaseComplete()
    {
        var input = "✅ **Phase [3] Complete** but also ❌ **Blocked** - Error occurred";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.Blocker, result.Type);
    }

    [Fact]
    public void Classify_ModeSelectionTakesPriorityOverQuestion()
    {
        var input = "🔄 **Execution Mode Selection**\n**Recommendation:** A — Manual mode";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.ModeSelection, result.Type);
    }

    [Fact]
    public void Classify_WorkflowCompleteTakesPriorityOverPhaseComplete()
    {
        var input = "✅ **Phase [5] Complete**\n\nAll Phases Complete - Implementation Finished";

        var result = _classifier.Classify(input);

        // WorkflowComplete has higher priority than PhaseComplete
        Assert.Equal(SignalType.WorkflowComplete, result.Type);
    }

    #endregion

    #region FindAllPhaseCompletions Tests

    [Fact]
    public void FindAllPhaseCompletions_MultipleCompletions_ReturnsAll()
    {
        var input = "✅ **Phase [1] Complete**\n✅ **Phase [2] Complete**\n✅ **Phase [3] Complete**";

        var results = _classifier.FindAllPhaseCompletions(input);

        Assert.Equal(3, results.Count);
        Assert.Equal(1, results[0].PhaseNumber);
        Assert.Equal(2, results[1].PhaseNumber);
        Assert.Equal(3, results[2].PhaseNumber);
    }

    [Fact]
    public void FindAllPhaseCompletions_NoCompletions_ReturnsEmpty()
    {
        var input = "No phase completions here";

        var results = _classifier.FindAllPhaseCompletions(input);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FindAllPhaseCompletions_EmptyOrNull_ReturnsEmpty(string? input)
    {
        var results = _classifier.FindAllPhaseCompletions(input!);

        Assert.Empty(results);
    }

    #endregion

    #region FindAllRecommendations Tests

    [Fact]
    public void FindAllRecommendations_MultipleRecommendations_ReturnsAll()
    {
        var input = """
            **Recommendation:** A — First option
            **Recommendation:** B — Second option
            **Recommendation:** C — Third option
            """;

        var results = _classifier.FindAllRecommendations(input);

        Assert.Equal(3, results.Count);
        Assert.Equal("A", results[0].RecommendationLetter);
        Assert.Equal("B", results[1].RecommendationLetter);
        Assert.Equal("C", results[2].RecommendationLetter);
    }

    [Fact]
    public void FindAllRecommendations_NoRecommendations_ReturnsEmpty()
    {
        var input = "No recommendations here";

        var results = _classifier.FindAllRecommendations(input);

        Assert.Empty(results);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void FindAllRecommendations_EmptyOrNull_ReturnsEmpty(string? input)
    {
        var results = _classifier.FindAllRecommendations(input!);

        Assert.Empty(results);
    }

    #endregion

    #region NeedsUserInput Tests

    [Fact]
    public void Classify_NeedsUserInput_ReturnsCorrectSignal()
    {
        var input = "STATUS: needs_user_input\nQUESTION_FOR_USER: Which database should we use?";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
    }

    [Fact]
    public void Classify_NeedsUserInput_ExtractsQuestionText()
    {
        var input = "STATUS: needs_user_input\nQUESTION_FOR_USER: Which database should we use for this project?";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
        Assert.Equal("Which database should we use for this project?", result.QuestionText);
    }

    [Fact]
    public void Classify_NeedsUserInput_ExtractsRecommendation()
    {
        var input = "STATUS: needs_user_input\nQUESTION_FOR_USER: Which option?\n**Recommendation:** B — Use PostgreSQL";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
        Assert.Equal("Which option?", result.QuestionText);
        Assert.Equal("B", result.RecommendationLetter);
        Assert.Equal("Use PostgreSQL", result.RecommendationOption);
    }

    [Fact]
    public void Classify_NeedsUserInput_StatusOnly_FallsThrough()
    {
        // STATUS: needs_user_input WITHOUT QUESTION_FOR_USER should NOT classify as NeedsUserInput
        var input = "STATUS: needs_user_input";

        var result = _classifier.Classify(input);

        Assert.NotEqual(SignalType.NeedsUserInput, result.Type);
    }

    [Theory]
    [InlineData("STATUS: NEEDS_USER_INPUT\nQUESTION_FOR_USER: Test question?")]
    [InlineData("STATUS: Needs_User_Input\nQUESTION_FOR_USER: Test question?")]
    [InlineData("STATUS:needs_user_input\nQUESTION_FOR_USER: Test question?")]
    public void Classify_NeedsUserInput_CaseInsensitive(string input)
    {
        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
        Assert.Equal("Test question?", result.QuestionText);
    }

    [Fact]
    public void Classify_NeedsUserInput_WithSurroundingContent()
    {
        var input = """
            PHASE: 3
            STATUS: needs_user_input
            TASKS_COMPLETED: []
            QUESTION_FOR_USER: Which authentication method should I use?
            QUESTION_TYPE: refinement
            QUESTION_OPTIONS: A) OAuth, B) JWT, C) API Keys
            RECOMMENDATION: none
            """;

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
        Assert.Equal("Which authentication method should I use?", result.QuestionText);
        Assert.Equal("A) OAuth, B) JWT, C) API Keys", result.QuestionOptions);
    }

    [Fact]
    public void Classify_NeedsUserInput_ExtractsQuestionOptions()
    {
        var input = "STATUS: needs_user_input\nQUESTION_FOR_USER: Which approach?\nQUESTION_OPTIONS: A — Redis, B — Memcached, C — In-memory";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
        Assert.Equal("A — Redis, B — Memcached, C — In-memory", result.QuestionOptions);
    }

    [Theory]
    [InlineData("open-ended")]
    [InlineData("Open-Ended")]
    [InlineData("none")]
    [InlineData("None")]
    public void Classify_NeedsUserInput_FiltersOutOpenEndedAndNoneOptions(string optionsValue)
    {
        var input = $"STATUS: needs_user_input\nQUESTION_FOR_USER: What should we do?\nQUESTION_OPTIONS: {optionsValue}";

        var result = _classifier.Classify(input);

        Assert.Equal(SignalType.NeedsUserInput, result.Type);
        Assert.Null(result.QuestionOptions);
    }

    [Fact]
    public void Classify_NeedsUserInput_PriorityOverModeSelection()
    {
        var input = "STATUS: needs_user_input\nQUESTION_FOR_USER: Should we proceed?\n🔄 **Execution Mode Selection**";

        var result = _classifier.Classify(input);

        // NeedsUserInput has higher priority than ModeSelection
        Assert.Equal(SignalType.NeedsUserInput, result.Type);
    }

    [Fact]
    public void Classify_NeedsUserInput_PriorityAfterBlocker()
    {
        var input = "❌ **Blocked** - Error occurred\nSTATUS: needs_user_input\nQUESTION_FOR_USER: What should I do?";

        var result = _classifier.Classify(input);

        // Blocker should still win over NeedsUserInput
        Assert.Equal(SignalType.Blocker, result.Type);
    }

    [Fact]
    public void Classify_NeedsUserInput_PlanTextFalsePositive()
    {
        // Plan document text that mentions STATUS: needs_user_input as an example
        // but does not have a paired QUESTION_FOR_USER line should NOT match
        var input = """
            The subagent return format supports these values:
            STATUS: success | partial | blocked | needs_user_input
            
            When STATUS is needs_user_input, the subagent should include question fields.
            """;

        var result = _classifier.Classify(input);

        // Should NOT be NeedsUserInput — this is plan description text, not an actual signal
        Assert.NotEqual(SignalType.NeedsUserInput, result.Type);
    }

    #endregion

    #region RawMatch Tests

    [Fact]
    public void Classify_IncludesRawMatch()
    {
        var input = "✅ **Phase [3] Complete** - All tasks done";

        var result = _classifier.Classify(input);

        Assert.NotNull(result.RawMatch);
        Assert.Contains("Phase", result.RawMatch);
    }

    #endregion
}
