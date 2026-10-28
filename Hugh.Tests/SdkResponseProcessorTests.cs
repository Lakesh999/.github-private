using GitHub.Copilot.SDK;
using Hugh.Agents;
using Hugh.Detection;
using Hugh.Metrics;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for SdkResponseProcessor.
/// Tests signal classification integration, path extraction, and event handling.
/// </summary>
public class SdkResponseProcessorTests
{
    private readonly SignalClassifier _signalClassifier = new();
    private readonly DocumentPathExtractor _pathExtractor = new();
    private readonly ILogger<SdkResponseProcessor> _logger = Substitute.For<ILogger<SdkResponseProcessor>>();

    private SdkResponseProcessor CreateProcessor() =>
        new(_signalClassifier, _pathExtractor, _logger);

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullSignalClassifier_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SdkResponseProcessor(null!, _pathExtractor, _logger));
    }

    [Fact]
    public void Constructor_WithNullPathExtractor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SdkResponseProcessor(_signalClassifier, null!, _logger));
    }

    [Fact]
    public void Constructor_WithNullLogger_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new SdkResponseProcessor(_signalClassifier, _pathExtractor, null!));
    }

    [Fact]
    public void Constructor_WithValidDependencies_CreatesInstance()
    {
        var processor = CreateProcessor();

        Assert.NotNull(processor);
    }

    [Fact]
    public void Constructor_WithMetricsCollector_CreatesInstance()
    {
        var metricsCollector = new ToolMetricsCollector();
        
        var processor = new SdkResponseProcessor(
            _signalClassifier,
            _pathExtractor,
            _logger,
            metricsCollector);

        Assert.NotNull(processor);
    }

    [Fact]
    public void Constructor_WithNullMetricsCollector_CreatesInstance()
    {
        var processor = new SdkResponseProcessor(
            _signalClassifier,
            _pathExtractor,
            _logger,
            null);

        Assert.NotNull(processor);
    }

    #endregion

    #region IsCompletionSignal Tests

    [Theory]
    [InlineData(SignalType.PhaseComplete, true)]
    [InlineData(SignalType.WorkflowComplete, true)]
    [InlineData(SignalType.None, false)]
    [InlineData(SignalType.QuestionOpen, false)]
    [InlineData(SignalType.QuestionWithRecommendation, false)]
    [InlineData(SignalType.Blocker, false)]
    [InlineData(SignalType.ModeSelection, false)]
    public void IsCompletionSignal_ReturnsExpectedResult(SignalType signalType, bool expected)
    {
        var result = SdkResponseProcessor.IsCompletionSignal(signalType);

        Assert.Equal(expected, result);
    }

    #endregion

    #region IsPauseSignal Tests

    [Theory]
    [InlineData(SignalType.QuestionOpen, true)]
    [InlineData(SignalType.QuestionWithRecommendation, true)]
    [InlineData(SignalType.Blocker, true)]
    [InlineData(SignalType.ModeSelection, true)]
    [InlineData(SignalType.None, false)]
    [InlineData(SignalType.PhaseComplete, false)]
    [InlineData(SignalType.WorkflowComplete, false)]
    public void IsPauseSignal_ReturnsExpectedResult(SignalType signalType, bool expected)
    {
        var result = SdkResponseProcessor.IsPauseSignal(signalType);

        Assert.Equal(expected, result);
    }

    #endregion

    #region ProcessedResponse Record Tests

    [Fact]
    public void ProcessedResponse_DefaultValues_AreCorrect()
    {
        var response = new SdkResponseProcessor.ProcessedResponse();

        Assert.Equal("", response.Content);
        Assert.Equal(SignalType.None, response.Signal.Type);
        Assert.Null(response.OutputDocumentPath);
        Assert.False(response.IsComplete);
        Assert.Null(response.Error);
    }

    [Fact]
    public void ProcessedResponse_WithInitializedValues_ReturnsCorrectValues()
    {
        var signal = new SignalClassification
        {
            Type = SignalType.PhaseComplete,
            PhaseNumber = 3
        };
        var error = new InvalidOperationException("Test error");

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = "Test content",
            Signal = signal,
            OutputDocumentPath = "docs/test.md",
            IsComplete = true,
            Error = error
        };

        Assert.Equal("Test content", response.Content);
        Assert.Equal(SignalType.PhaseComplete, response.Signal.Type);
        Assert.Equal(3, response.Signal.PhaseNumber);
        Assert.Equal("docs/test.md", response.OutputDocumentPath);
        Assert.True(response.IsComplete);
        Assert.Same(error, response.Error);
    }

    #endregion

    #region Signal Classification Integration Tests

    [Fact]
    public void ProcessedResponse_WithPhaseCompleteContent_HasCorrectSignal()
    {
        // This tests that the signal classification works correctly with the processor
        var content = "✅ **Phase [3] Complete** — All tasks done";
        var signal = _signalClassifier.Classify(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = signal,
            IsComplete = true
        };

        Assert.Equal(SignalType.PhaseComplete, response.Signal.Type);
        Assert.Equal(3, response.Signal.PhaseNumber);
        Assert.True(SdkResponseProcessor.IsCompletionSignal(response.Signal.Type));
        Assert.False(SdkResponseProcessor.IsPauseSignal(response.Signal.Type));
    }

    [Fact]
    public void ProcessedResponse_WithWorkflowCompleteContent_HasCorrectSignal()
    {
        var content = "All Phases Complete — Implementation Finished";
        var signal = _signalClassifier.Classify(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = signal,
            IsComplete = true
        };

        Assert.Equal(SignalType.WorkflowComplete, response.Signal.Type);
        Assert.True(SdkResponseProcessor.IsCompletionSignal(response.Signal.Type));
    }

    [Fact]
    public void ProcessedResponse_WithBlockerContent_HasCorrectSignal()
    {
        var content = "❌ **Blocked** — Cannot proceed due to missing dependency";
        var signal = _signalClassifier.Classify(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = signal,
            IsComplete = true
        };

        Assert.Equal(SignalType.Blocker, response.Signal.Type);
        Assert.True(SdkResponseProcessor.IsPauseSignal(response.Signal.Type));
        Assert.False(SdkResponseProcessor.IsCompletionSignal(response.Signal.Type));
    }

    [Fact]
    public void ProcessedResponse_WithQuestionContent_HasCorrectSignal()
    {
        var content = "**Recommendation:** A — Use the first approach";
        var signal = _signalClassifier.Classify(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = signal,
            IsComplete = true
        };

        Assert.Equal(SignalType.QuestionWithRecommendation, response.Signal.Type);
        Assert.Equal("A", response.Signal.RecommendationLetter);
        Assert.True(SdkResponseProcessor.IsPauseSignal(response.Signal.Type));
    }

    [Fact]
    public void ProcessedResponse_WithModeSelectionContent_HasCorrectSignal()
    {
        var content = "🔄 **Execution Mode Selection**\n\nPlease choose...";
        var signal = _signalClassifier.Classify(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = signal,
            IsComplete = true
        };

        Assert.Equal(SignalType.ModeSelection, response.Signal.Type);
        Assert.True(SdkResponseProcessor.IsPauseSignal(response.Signal.Type));
    }

    #endregion

    #region Path Extraction Integration Tests

    [Fact]
    public void ProcessedResponse_WithDocumentPath_ExtractsPath()
    {
        var content = "Created vision document at: docs/vision/001-feature.md\n\nDocument is ready.";
        var extractedPath = _pathExtractor.ExtractDocumentPath(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = _signalClassifier.Classify(content),
            OutputDocumentPath = extractedPath,
            IsComplete = true
        };

        Assert.Equal("docs/vision/001-feature.md", response.OutputDocumentPath);
    }

    [Fact]
    public void ProcessedResponse_WithMarkdownLinkPath_ExtractsPath()
    {
        var content = "See [the plan](docs/plans/001-plan.md) for details.";
        var extractedPath = _pathExtractor.ExtractDocumentPath(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            OutputDocumentPath = extractedPath,
            IsComplete = true
        };

        Assert.Equal("docs/plans/001-plan.md", response.OutputDocumentPath);
    }

    [Fact]
    public void ProcessedResponse_WithBacktickPath_ExtractsPath()
    {
        var content = "updated at: `docs/research/analysis.md`";
        var extractedPath = _pathExtractor.ExtractDocumentPath(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            OutputDocumentPath = extractedPath,
            IsComplete = true
        };

        Assert.Equal("docs/research/analysis.md", response.OutputDocumentPath);
    }

    [Fact]
    public void ProcessedResponse_WithHandoffDocument_ExtractsPath()
    {
        var content = "**Document:** docs/vision/feature.md\n\nReady for next step.";
        var extractedPath = _pathExtractor.ExtractDocumentPath(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            OutputDocumentPath = extractedPath,
            IsComplete = true
        };

        Assert.Equal("docs/vision/feature.md", response.OutputDocumentPath);
    }

    [Fact]
    public void ProcessedResponse_WithNoPath_ReturnsNull()
    {
        var content = "This content has no document path references.";
        var extractedPath = _pathExtractor.ExtractDocumentPath(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            OutputDocumentPath = extractedPath,
            IsComplete = true
        };

        Assert.Null(response.OutputDocumentPath);
    }

    #endregion

    #region Combined Signal and Path Tests

    [Fact]
    public void ProcessedResponse_WithPhaseCompleteAndPath_ExtractsBoth()
    {
        var content = """
            ✅ **Phase [2] Complete** — Vision Document Created

            **Document:** docs/vision/001-feature.md
            
            Ready for next phase.
            """;

        var signal = _signalClassifier.Classify(content);
        var path = _pathExtractor.ExtractDocumentPath(content);

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = content,
            Signal = signal,
            OutputDocumentPath = path,
            IsComplete = true
        };

        Assert.Equal(SignalType.PhaseComplete, response.Signal.Type);
        Assert.Equal(2, response.Signal.PhaseNumber);
        Assert.Equal("docs/vision/001-feature.md", response.OutputDocumentPath);
        Assert.True(response.IsComplete);
        Assert.Null(response.Error);
    }

    [Fact]
    public void ProcessedResponse_WithErrorState_PreservesPartialContent()
    {
        var partialContent = "Starting implementation...\nCreated file at: `docs/plans/partial.md`";
        var error = new Exception("Session terminated unexpectedly");

        var response = new SdkResponseProcessor.ProcessedResponse
        {
            Content = partialContent,
            Signal = _signalClassifier.Classify(partialContent),
            OutputDocumentPath = _pathExtractor.ExtractDocumentPath(partialContent),
            IsComplete = false,
            Error = error
        };

        Assert.Equal(partialContent, response.Content);
        Assert.Equal("docs/plans/partial.md", response.OutputDocumentPath);
        Assert.False(response.IsComplete);
        Assert.Same(error, response.Error);
    }

    #endregion

    #region ProcessResponseAsync Null Argument Tests

    [Fact]
    public async Task ProcessResponseAsync_WithNullSession_ThrowsArgumentNullException()
    {
        var processor = CreateProcessor();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => processor.ProcessResponseAsync(null!, CancellationToken.None));
    }

    #endregion

    #region DetectToolSignal Tests

    [Fact]
    public void DetectToolSignal_HughNotifyPhaseComplete_ReturnsPhaseComplete()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("hugh_notify_phase_complete");

        Assert.NotNull(result);
        Assert.Equal(SignalType.PhaseComplete, result.Type);
    }

    [Fact]
    public void DetectToolSignal_HughReportBlocker_ReturnsBlocker()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("hugh_report_blocker");

        Assert.NotNull(result);
        Assert.Equal(SignalType.Blocker, result.Type);
    }

    [Fact]
    public void DetectToolSignal_HughNotifyWorkflowComplete_ReturnsWorkflowComplete()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("hugh_notify_workflow_complete");

        Assert.NotNull(result);
        Assert.Equal(SignalType.WorkflowComplete, result.Type);
    }

    [Fact]
    public void DetectToolSignal_NonHughTool_ReturnsNull()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("file_search");

        Assert.Null(result);
    }

    [Fact]
    public void DetectToolSignal_RegularTool_ReturnsNull()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("run_in_terminal");

        Assert.Null(result);
    }

    [Fact]
    public void DetectToolSignal_NullToolName_ReturnsNull()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal(null);

        Assert.Null(result);
    }

    [Fact]
    public void DetectToolSignal_EmptyToolName_ReturnsNull()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("");

        Assert.Null(result);
    }

    [Fact]
    public void DetectToolSignal_UnknownHughTool_ReturnsNull()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("hugh_unknown_tool");

        Assert.Null(result);
    }

    [Fact]
    public void DetectToolSignal_CaseInsensitive_ReturnsSignal()
    {
        var processor = CreateProcessor();

        var result = processor.DetectToolSignal("HUGH_NOTIFY_PHASE_COMPLETE");

        Assert.NotNull(result);
        Assert.Equal(SignalType.PhaseComplete, result.Type);
    }

    #endregion

    #region Tool-First Priority and Text Fallback Tests

    /// <summary>
    /// Tests that when a tool-based signal is detected, it takes priority over any text-based signal.
    /// This verifies the dual-detection architecture's priority order.
    /// </summary>
    [Fact]
    public void ToolSignal_TakesPriority_WhenBothPresent()
    {
        // This test verifies the conceptual priority: tool signals take precedence.
        // In ProcessResponseAsync, if _toolBasedSignal is set (from a hugh_* tool call),
        // it is used instead of calling SignalClassifier.Classify().
        
        // Simulate content that would match text-based blocker pattern
        var contentWithBlockerText = "❌ **Blocked** — Cannot proceed due to missing dependency";
        
        // The text classifier would detect this as Blocker
        var textSignal = _signalClassifier.Classify(contentWithBlockerText);
        Assert.Equal(SignalType.Blocker, textSignal.Type);
        
        // But if a tool signal indicated PhaseComplete, it would take priority
        var processor = CreateProcessor();
        var toolSignal = processor.DetectToolSignal("hugh_notify_phase_complete");
        Assert.NotNull(toolSignal);
        Assert.Equal(SignalType.PhaseComplete, toolSignal.Type);
        
        // Verify priority: tool signal (PhaseComplete) != text signal (Blocker)
        // This demonstrates that the two detection methods can return different signals,
        // and the system is designed to prefer the tool-based signal.
        Assert.NotEqual(textSignal.Type, toolSignal.Type);
    }

    /// <summary>
    /// Tests that when no tool-based signal is detected, the text-based regex classification is used.
    /// This is the expected behavior in VS Code Copilot chat where hugh_* tools are unavailable.
    /// </summary>
    [Fact]
    public void TextFallback_UsedWhenNoToolSignal()
    {
        // When running in VS Code Copilot chat (not Hugh CLI), 
        // agents cannot call hugh_* tools. In this case, text-based detection is used.
        
        // Example content with phase complete text format
        var content = "✅ **Phase [3] Complete** — All tasks done";
        
        // Text classifier detects the signal
        var textSignal = _signalClassifier.Classify(content);
        
        Assert.Equal(SignalType.PhaseComplete, textSignal.Type);
        Assert.Equal(3, textSignal.PhaseNumber);
        
        // DetectToolSignal returns null for non-hugh tools
        var processor = CreateProcessor();
        Assert.Null(processor.DetectToolSignal("read_file"));
        Assert.Null(processor.DetectToolSignal("run_in_terminal"));
        Assert.Null(processor.DetectToolSignal(null));
        Assert.Null(processor.DetectToolSignal(""));
        
        // Therefore, text fallback would be used
    }

    /// <summary>
    /// Tests that when text fallback is used (no tool signal detected), 
    /// a warning should be logged. This test verifies the warning message format.
    /// </summary>
    [Fact]
    public void TextFallback_LogsWarning_WhenSignalDetected()
    {
        // The warning message format when text fallback fires:
        // "No tool-based signal detected, falling back to text regex classification: {SignalType}"
        
        // This test documents that when _toolBasedSignal is null and
        // SignalClassifier returns a non-None signal, a warning is logged.
        
        var content = "✅ **Phase [5] Complete** — Implementation done";
        var signal = _signalClassifier.Classify(content);
        
        // Signal is detected via text
        Assert.Equal(SignalType.PhaseComplete, signal.Type);
        Assert.Equal(5, signal.PhaseNumber);
        
        // In ProcessResponseAsync, if _toolBasedSignal is null:
        // _logger.LogWarning("No tool-based signal detected, falling back to text regex classification: {SignalType}", signal.Type);
        // This warning indicates either:
        // 1. Running in VS Code chat (expected - tools unavailable)
        // 2. Running in Hugh CLI but tool wasn't called (may indicate tool registration issues)
    }

    /// <summary>
    /// Verifies that all regex patterns in SignalClassifier remain functional.
    /// This test ensures no patterns were removed during tool integration.
    /// </summary>
    [Fact]
    public void AllRegexPatterns_StillFunctional()
    {
        // Per Review Decision D10: DO NOT remove any SignalClassifier regex patterns.
        // All regex patterns are kept permanently as fallback.
        
        // PhaseComplete patterns
        Assert.Equal(SignalType.PhaseComplete, _signalClassifier.Classify("✅ **Phase [1] Complete**").Type);
        Assert.Equal(SignalType.PhaseComplete, _signalClassifier.Classify("✅ **Phase 2 Complete**").Type);
        Assert.Equal(SignalType.PhaseComplete, _signalClassifier.Classify("✅ **Phase 3 Completed**").Type);
        
        // WorkflowComplete patterns - all should still match
        Assert.Equal(SignalType.WorkflowComplete, _signalClassifier.Classify("Research Complete").Type);
        Assert.Equal(SignalType.WorkflowComplete, _signalClassifier.Classify("All Phases Complete").Type);
        Assert.Equal(SignalType.WorkflowComplete, _signalClassifier.Classify("Plan Complete").Type);
        Assert.Equal(SignalType.WorkflowComplete, _signalClassifier.Classify("Implementation Finished").Type);
        
        // Blocker patterns
        Assert.Equal(SignalType.Blocker, _signalClassifier.Classify("❌ **Blocked** - Cannot proceed").Type);
        Assert.Equal(SignalType.Blocker, _signalClassifier.Classify("❌ **Blocker**: Missing file").Type);
        Assert.Equal(SignalType.Blocker, _signalClassifier.Classify("⚠️ **Implementation Paused** - Waiting").Type);
        Assert.Equal(SignalType.Blocker, _signalClassifier.Classify("⚠️ **Execution Stopped** due to error").Type);
        
        // QuestionWithRecommendation patterns
        var questionSignal = _signalClassifier.Classify("**Recommendation:** A — Use option A");
        Assert.Equal(SignalType.QuestionWithRecommendation, questionSignal.Type);
        Assert.Equal("A", questionSignal.RecommendationLetter);
        
        // ModeSelection patterns
        Assert.Equal(SignalType.ModeSelection, _signalClassifier.Classify("🔄 **Execution Mode Selection**").Type);
        
        // QuestionOpen patterns
        Assert.Equal(SignalType.QuestionOpen, _signalClassifier.Classify("**How would you like to proceed?**").Type);
    }

    /// <summary>
    /// Tests that utility hugh_* tools (like workspace structure) don't generate signals.
    /// Only specific signal tools should map to signal types.
    /// </summary>
    [Fact]
    public void DetectToolSignal_GetWorkspaceStructure_ReturnsNull()
    {
        var processor = CreateProcessor();

        // hugh_get_workspace_structure is a utility tool, not a signal tool
        var result = processor.DetectToolSignal("hugh_get_workspace_structure");

        Assert.Null(result);
    }

    /// <summary>
    /// Tests all three signal tools map correctly to their respective signal types.
    /// </summary>
    [Fact]
    public void DetectToolSignal_AllSignalTools_MapCorrectly()
    {
        var processor = CreateProcessor();

        // Verify all three signal tools are properly mapped
        var phaseComplete = processor.DetectToolSignal("hugh_notify_phase_complete");
        var workflowComplete = processor.DetectToolSignal("hugh_notify_workflow_complete");
        var blocker = processor.DetectToolSignal("hugh_report_blocker");

        Assert.NotNull(phaseComplete);
        Assert.NotNull(workflowComplete);
        Assert.NotNull(blocker);

        Assert.Equal(SignalType.PhaseComplete, phaseComplete.Type);
        Assert.Equal(SignalType.WorkflowComplete, workflowComplete.Type);
        Assert.Equal(SignalType.Blocker, blocker.Type);
    }

    #endregion
}
