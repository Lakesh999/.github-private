using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Handlers;
using Hugh.Logging;
using Hugh.Models;
using Serilog;
using Xunit;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for SdkQuestionHandler — verifies SDK-native question handling behavior.
/// </summary>
public class SdkQuestionHandlerTests : IDisposable
{
    private readonly string _testDir;
    private readonly DecisionLogger _decisionLogger;
    private readonly AutonomousConfig _autonomousConfig;
    private readonly SdkQuestionHandler _handler;

    public SdkQuestionHandlerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"sdk-qh-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _decisionLogger = new DecisionLogger(_testDir, Log.Logger);
        _autonomousConfig = new AutonomousConfig
        {
            Enabled = true,
            AcceptRecommendationsOnly = true
        };
        _handler = new SdkQuestionHandler(_decisionLogger, _autonomousConfig, Log.Logger);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDir))
                Directory.Delete(_testDir, recursive: true);
        }
        catch
        {
            // Ignore cleanup errors in tests
        }
    }

    #region DetectRecommendation Tests

    [Fact]
    public void DetectRecommendation_WithRecommendation_ReturnsLetterAndOption()
    {
        var prompt = "Please choose:\n\n**Recommendation:** A — Use microservices architecture";
        
        var (letter, option) = _handler.DetectRecommendation(prompt);
        
        Assert.Equal("A", letter);
        Assert.Equal("Use microservices architecture", option);
    }

    [Fact]
    public void DetectRecommendation_NoRecommendation_ReturnsNull()
    {
        var prompt = "Please choose from the following options:\nA — Option 1\nB — Option 2";
        
        var (letter, option) = _handler.DetectRecommendation(prompt);
        
        Assert.Null(letter);
        Assert.Null(option);
    }

    [Fact]
    public void DetectRecommendation_MultipleRecommendations_ReturnsFirst()
    {
        var prompt = """
            **Recommendation:** B — Second option
            Some other text
            **Recommendation:** C — Third option
            """;
        
        var (letter, option) = _handler.DetectRecommendation(prompt);
        
        Assert.Equal("B", letter);
        Assert.Equal("Second option", option);
    }

    [Fact]
    public void DetectRecommendation_NullPrompt_ReturnsNull()
    {
        var (letter, option) = _handler.DetectRecommendation(null);
        
        Assert.Null(letter);
        Assert.Null(option);
    }

    [Fact]
    public void DetectRecommendation_EmptyPrompt_ReturnsNull()
    {
        var (letter, option) = _handler.DetectRecommendation("");
        
        Assert.Null(letter);
        Assert.Null(option);
    }

    [Theory]
    [InlineData("**Recommendation:** 1 — First numberic option", "1", "First numberic option")]
    [InlineData("**Recommendation:** E — Last letter option", "E", "Last letter option")]
    [InlineData("**Recommendation:** C – En dash variant", "C", "En dash variant")]
    [InlineData("**Recommendation:** D - Hyphen variant", "D", "Hyphen variant")]
    public void DetectRecommendation_VariousSeparators_ReturnsCorrectly(
        string prompt, string expectedLetter, string expectedOption)
    {
        var (letter, option) = _handler.DetectRecommendation(prompt);
        
        Assert.Equal(expectedLetter, letter);
        Assert.Equal(expectedOption, option);
    }

    #endregion

    #region ShouldAutoAccept Tests

    [Fact]
    public void ShouldAutoAccept_RecommendationPresent_ReturnsTrue()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Which option?\n**Recommendation:** A — Best option"
        };
        
        var result = _handler.ShouldAutoAccept(request);
        
        Assert.True(result);
    }

    [Fact]
    public void ShouldAutoAccept_NoRecommendation_ReturnsFalse()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Which option do you prefer?",
            Choices = new List<string> { "A — Option 1", "B — Option 2" }
        };
        
        var result = _handler.ShouldAutoAccept(request);
        
        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoAccept_AcceptDisabled_ReturnsFalse()
    {
        var config = new AutonomousConfig
        {
            Enabled = true,
            AcceptRecommendationsOnly = false
        };
        var handler = new SdkQuestionHandler(_decisionLogger, config, Log.Logger);
        
        var request = new HughUserInputRequest
        {
            Prompt = "**Recommendation:** A — Best option"
        };
        
        var result = handler.ShouldAutoAccept(request);
        
        Assert.False(result);
    }

    [Fact]
    public void ShouldAutoAccept_RecommendedChoice_ReturnsTrue()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Select an option:",
            Choices = new List<string>
            {
                "A — Regular option",
                "B — Special option (recommended)",
                "C — Another option"
            }
        };
        
        var result = _handler.ShouldAutoAccept(request);
        
        Assert.True(result);
    }

    [Fact]
    public void ShouldAutoAccept_CheckmarkRecommendedChoice_ReturnsTrue()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Select an option:",
            Choices = new List<string>
            {
                "A — Regular option",
                "B — ✓ Special option",
                "C — Another option"
            }
        };
        
        var result = _handler.ShouldAutoAccept(request);
        
        Assert.True(result);
    }

    #endregion

    #region HandleAsync Tests

    [Fact]
    public async Task HandleAsync_WithRecommendation_AutoAccepts()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Choose wisely:\n**Recommendation:** B — Smart choice"
        };
        
        var response = await _handler.HandleAsync(request, "Phase 1", Guid.NewGuid());
        
        Assert.True(response.IsAutomatic);
        Assert.Equal("B", response.Answer);
    }

    [Fact]
    public void HandleAsync_WithRecommendation_AcceptDisabled_DoesNotAutoAccept()
    {
        // Note: This test would require mocking Console.ReadLine which is complex
        // For now, we test a different scenario - that with AcceptRecommendationsOnly = false,
        // no auto-accept happens. This test validates the config check.
        var config = new AutonomousConfig
        {
            Enabled = true,
            AcceptRecommendationsOnly = false
        };
        var handler = new SdkQuestionHandler(_decisionLogger, config, Log.Logger);
        
        var request = new HughUserInputRequest
        {
            Prompt = "**Recommendation:** A — Best option"
        };
        
        // ShouldAutoAccept should be false
        var shouldAutoAccept = handler.ShouldAutoAccept(request);
        Assert.False(shouldAutoAccept);
    }

    [Fact]
    public async Task HandleAsync_ModeSelection_AutoSelectsManual()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "🔄 **Execution Mode Selection**\n\nPlease select:\n1 — Manual\n2 — Automatic"
        };
        
        var response = await _handler.HandleAsync(request, "Mode Selection", Guid.NewGuid());
        
        Assert.True(response.IsAutomatic);
        Assert.Equal("1", response.Answer);
    }

    [Fact]
    public async Task HandleAsync_ModeSelection_PlainText_AutoSelectsManual()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Execution Mode Selection: Choose 1 for Manual or 2 for Automatic"
        };
        
        var response = await _handler.HandleAsync(request, "Phase 1", Guid.NewGuid());
        
        Assert.True(response.IsAutomatic);
        Assert.Equal("1", response.Answer);
    }

    [Fact]
    public async Task HandleAsync_WithChoices_AutoAcceptsRecommended()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Select database:",
            Choices = new List<string>
            {
                "A — PostgreSQL",
                "B — SQL Server (recommended)",
                "C — MySQL"
            }
        };
        
        var response = await _handler.HandleAsync(request, "Phase 2", Guid.NewGuid());
        
        Assert.True(response.IsAutomatic);
        Assert.Equal("B", response.Answer);
    }

    [Fact]
    public async Task HandleAsync_LogsDecisionViaDecisionLogger()
    {
        var runId = Guid.NewGuid();
        var request = new HughUserInputRequest
        {
            Prompt = "**Recommendation:** C — Chosen option"
        };
        
        await _handler.HandleAsync(request, "Test Phase", runId);
        
        // Check that a decisions file was created
        var decisionsFile = Path.Combine(_testDir, ".orchestrator", "decisions.jsonl");
        Assert.True(File.Exists(decisionsFile), "Decisions file should be created");
        
        var content = await File.ReadAllTextAsync(decisionsFile);
        Assert.Contains("C", content);
        Assert.Contains("Chosen option", content);
    }

    #endregion

    #region HughUserInputRequest Tests

    [Fact]
    public void HughUserInputRequest_FromSignalClassification_Basic()
    {
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = "Which database?",
            QuestionOptions = "A — PostgreSQL, B — MySQL"
        };
        
        var request = HughUserInputRequest.FromSignalClassification(classification);
        
        Assert.Equal("Which database?", request.Prompt);
        Assert.NotNull(request.Choices);
        Assert.Equal(2, request.Choices!.Count);
        Assert.Contains("A — PostgreSQL", request.Choices);
        Assert.Contains("B — MySQL", request.Choices);
    }

    [Fact]
    public void HughUserInputRequest_FromSignalClassification_OpenEnded()
    {
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = "What is your project name?",
            QuestionOptions = "open-ended"
        };
        
        var request = HughUserInputRequest.FromSignalClassification(classification);
        
        Assert.Equal("What is your project name?", request.Prompt);
        Assert.Null(request.Choices);
        Assert.True(request.AllowFreeform);
    }

    [Fact]
    public void HughUserInputRequest_FromSignalClassification_NullQuestionText_UsesRawMatch()
    {
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = null,
            RawMatch = "STATUS: needs_user_input"
        };
        
        var request = HughUserInputRequest.FromSignalClassification(classification);
        
        Assert.Equal("STATUS: needs_user_input", request.Prompt);
    }

    [Fact]
    public void HughUserInputRequest_FromSignalClassification_NoQuestionOrMatch_UsesFallback()
    {
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = null,
            RawMatch = null
        };
        
        var request = HughUserInputRequest.FromSignalClassification(classification);
        
        Assert.Equal("Question from agent", request.Prompt);
    }

    #endregion

    #region UserInputResponse Tests

    [Fact]
    public void UserInputResponse_AutomaticResponse_HasCorrectProperties()
    {
        var response = new UserInputResponse
        {
            Answer = "A",
            IsAutomatic = true
        };
        
        Assert.Equal("A", response.Answer);
        Assert.True(response.IsAutomatic);
    }

    [Fact]
    public void UserInputResponse_ManualResponse_HasCorrectProperties()
    {
        var response = new UserInputResponse
        {
            Answer = "Custom answer",
            IsAutomatic = false
        };
        
        Assert.Equal("Custom answer", response.Answer);
        Assert.False(response.IsAutomatic);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task HandleAsync_NullRequest_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _handler.HandleAsync((HughUserInputRequest)null!, "Phase 1", Guid.NewGuid()));
    }

    [Fact]
    public void DetectRecommendation_WhitespaceOnly_ReturnsNull()
    {
        var (letter, option) = _handler.DetectRecommendation("   \n\t  ");
        
        Assert.Null(letter);
        Assert.Null(option);
    }

    [Fact]
    public async Task HandleAsync_ModeSelectionPrioritizedOverRecommendation()
    {
        // If both mode selection and recommendation are present, mode selection wins
        var request = new HughUserInputRequest
        {
            Prompt = "🔄 **Execution Mode Selection**\n\n**Recommendation:** A — Some option"
        };
        
        var response = await _handler.HandleAsync(request, "Phase 1", Guid.NewGuid());
        
        // Should select "1" for mode selection, not "A" for recommendation
        Assert.Equal("1", response.Answer);
        Assert.True(response.IsAutomatic);
    }

    [Fact]
    public void ShouldAutoAccept_BracketRecommended_ReturnsTrue()
    {
        var request = new HughUserInputRequest
        {
            Prompt = "Select:",
            Choices = new List<string>
            {
                "A — Option 1",
                "B — Option 2 [recommended]"
            }
        };
        
        var result = _handler.ShouldAutoAccept(request);
        
        Assert.True(result);
    }

    #endregion
}
