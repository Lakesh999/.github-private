using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Handlers;
using Hugh.Logging;
using Hugh.Models;
using NSubstitute;
using Serilog;
using Xunit;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for QuestionHandler — verifies delegation to SdkQuestionHandler.
/// </summary>
public class QuestionHandlerTests : IDisposable
{
    private readonly string _testDir;
    private readonly SdkQuestionHandler _mockSdkHandler;
    private readonly QuestionHandler _handler;
    private readonly DecisionLogger _decisionLogger;
    private readonly AutonomousConfig _autonomousConfig;

    public QuestionHandlerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"qh-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _decisionLogger = new DecisionLogger(_testDir, Log.Logger);
        _autonomousConfig = new AutonomousConfig
        {
            Enabled = true,
            AcceptRecommendationsOnly = true
        };
        
        // Create a mock SdkQuestionHandler for delegation verification
        _mockSdkHandler = Substitute.ForPartsOf<SdkQuestionHandler>(
            _decisionLogger, _autonomousConfig, Log.Logger);
        
        _handler = new QuestionHandler(_mockSdkHandler, Log.Logger);
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

    #region Delegation Verification Tests

    [Fact]
    public async Task HandleQuestionAsync_DelegatesToSdkQuestionHandler()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            RawMatch = "STATUS: needs_user_input",
            QuestionText = "Which database should we target?"
        };
        
        // Configure mock to return a response
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert - verify delegation occurred
        await _mockSdkHandler.Received(1).HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Is<string>(s => s == "Phase 1"),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleQuestionAsync_ConvertsSignalClassificationToHughUserInputRequest()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = "Which database?",
            QuestionOptions = "A — SQL Server, B — PostgreSQL",
            RawMatch = "STATUS: needs_user_input"
        };
        
        HughUserInputRequest? capturedRequest = null;
        _mockSdkHandler.HandleAsync(
            Arg.Do<HughUserInputRequest>(r => capturedRequest = r),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        await _handler.HandleQuestionAsync(classification, "Phase 2", Guid.NewGuid());

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Which database?", capturedRequest!.Prompt);
        Assert.NotNull(capturedRequest.Choices);
        Assert.Contains("A — SQL Server", capturedRequest.Choices);
        Assert.Contains("B — PostgreSQL", capturedRequest.Choices);
    }

    #endregion

    #region NeedsUserInput Tests

    [Fact]
    public async Task HandleQuestionAsync_NeedsUserInput_ReturnsPauseWithQuestion()
    {
        // Arrange - use real handler to test full flow
        var realHandler = CreateRealQuestionHandler();
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            RawMatch = "STATUS: needs_user_input",
            QuestionText = "Which database should we target?"
        };

        // Note: With real SdkQuestionHandler, open questions trigger console input
        // For this test, we use mock to return empty answer (simulating pause)
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.ShouldPause);
        Assert.Equal("Which database should we target?", result.QuestionText);
    }

    [Fact]
    public async Task HandleQuestionAsync_NeedsUserInput_NullQuestionText_UsesFallback()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            RawMatch = "STATUS: needs_user_input",
            QuestionText = null
        };
        
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.ShouldPause);
        Assert.NotNull(result.QuestionText); // Fallback prompt from FromSignalClassification
    }

    [Fact]
    public async Task HandleQuestionAsync_NeedsUserInput_WithOptions_PassesOptionsToSdkHandler()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            RawMatch = "STATUS: needs_user_input",
            QuestionText = "Which caching strategy?",
            QuestionOptions = "A — Redis, B — Memcached, C — In-memory",
            RecommendationLetter = "A",
            RecommendationOption = "Redis"
        };
        
        HughUserInputRequest? capturedRequest = null;
        _mockSdkHandler.HandleAsync(
            Arg.Do<HughUserInputRequest>(r => capturedRequest = r),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        await _handler.HandleQuestionAsync(classification, "Phase 2", Guid.NewGuid());

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("Which caching strategy?", capturedRequest!.Prompt);
        Assert.NotNull(capturedRequest.Choices);
        Assert.Equal(3, capturedRequest.Choices!.Count);
    }

    #endregion

    #region QuestionOpen Tests

    [Fact]
    public async Task HandleQuestionAsync_QuestionOpen_DelegatesToSdkHandler()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.QuestionOpen,
            RawMatch = "**How would you like to proceed?**"
        };
        
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.ShouldPause);
        await _mockSdkHandler.Received(1).HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    #endregion

    #region QuestionWithRecommendation Tests

    [Fact]
    public async Task HandleQuestionAsync_QuestionWithRecommendation_AutoAcceptsDelegated()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.QuestionWithRecommendation,
            RecommendationLetter = "A",
            RecommendationOption = "Use microservices",
            RawMatch = "**Recommendation:** A — Use microservices"
        };
        
        // SdkQuestionHandler auto-accepts recommendations
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "A", IsAutomatic = true });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.ShouldPause); // Auto-responds
        Assert.Equal("A", result.Response);
        Assert.NotNull(result.DecisionMade);
        Assert.True(result.DecisionMade!.Autonomous);
    }

    #endregion

    #region ModeSelection Tests

    [Fact]
    public async Task HandleQuestionAsync_ModeSelection_AutoSelectsMode1Delegated()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.ModeSelection,
            RawMatch = "🔄 **Execution Mode Selection**"
        };
        
        // SdkQuestionHandler auto-selects mode 1
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "1", IsAutomatic = true });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.ShouldPause); // Auto-responds
        Assert.Equal("1", result.Response);
    }

    #endregion

    #region Non-Question Signal Tests

    [Fact]
    public async Task HandleQuestionAsync_UnknownSignalType_ReturnsNull()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.PhaseComplete,
            PhaseNumber = 3,
            RawMatch = "✅ **Phase [3] Complete**"
        };

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.Null(result);
        // Should not delegate to SdkQuestionHandler for non-question signals
        await _mockSdkHandler.DidNotReceive().HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleQuestionAsync_BlockerSignal_ReturnsNull()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            RawMatch = "❌ **Blocked**"
        };

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region UserInputResponse Conversion Tests

    [Fact]
    public async Task HandleQuestionAsync_AutomaticResponse_ReturnsAutoRespondWithDecision()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.QuestionWithRecommendation,
            RawMatch = "**Recommendation:** B — Option B",
            RecommendationLetter = "B",
            RecommendationOption = "Option B"
        };
        
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "B", IsAutomatic = true });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 3", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.ShouldPause);
        Assert.Equal("B", result.Response);
        Assert.NotNull(result.DecisionMade);
        Assert.Equal("B", result.DecisionMade!.Selected);
        Assert.Equal("Phase 3", result.DecisionMade.Phase);
    }

    [Fact]
    public async Task HandleQuestionAsync_NonAutomaticWithAnswer_ReturnsResponseWithoutPause()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = "Enter database name",
            RawMatch = "STATUS: needs_user_input"
        };
        
        // User provided input via console
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "ProductionDB", IsAutomatic = false });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.False(result!.ShouldPause); // User already answered
        Assert.Equal("ProductionDB", result.Response);
    }

    [Fact]
    public async Task HandleQuestionAsync_EmptyNonAutomaticResponse_ReturnsPauseWithQuestion()
    {
        // Arrange
        var classification = new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            QuestionText = "Which option?",
            RawMatch = "STATUS: needs_user_input"
        };
        
        // Empty response (user cancelled or error)
        _mockSdkHandler.HandleAsync(
            Arg.Any<HughUserInputRequest>(),
            Arg.Any<string>(),
            Arg.Any<Guid>(),
            Arg.Any<CancellationToken>())
            .Returns(new UserInputResponse { Answer = "", IsAutomatic = false });

        // Act
        var result = await _handler.HandleQuestionAsync(classification, "Phase 1", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result!.ShouldPause);
        Assert.Equal("Which option?", result.QuestionText);
    }

    #endregion

    #region Helper Methods

    private QuestionHandler CreateRealQuestionHandler()
    {
        var realSdkHandler = new SdkQuestionHandler(_decisionLogger, _autonomousConfig, Log.Logger);
        return new QuestionHandler(realSdkHandler, Log.Logger);
    }

    #endregion
}
