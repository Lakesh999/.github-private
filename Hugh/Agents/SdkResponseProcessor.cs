using System.Collections.Concurrent;
using System.Text;
using GitHub.Copilot.SDK;
using Hugh.Detection;
using Hugh.Metrics;
using Hugh.Models;
using Hugh.Tools;
using Microsoft.Extensions.Logging;

namespace Hugh.Agents;

/// <summary>
/// Processes responses from the GitHub Copilot SDK, accumulating content
/// from events and classifying signals for orchestration control.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Dual-Detection Signal Architecture</strong>
/// </para>
/// <para>
/// This processor implements a permanent dual-detection architecture for signal classification:
/// </para>
/// <list type="number">
/// <item>
/// <description>
/// <strong>Tool-Based Detection (Primary):</strong> When running in Hugh CLI, agents can call
/// structured <c>hugh_*</c> tools (e.g., <c>hugh_notify_phase_complete</c>, <c>hugh_report_blocker</c>)
/// to signal workflow events. These tool calls are detected via SDK events and provide explicit,
/// unambiguous signals.
/// </description>
/// </item>
/// <item>
/// <description>
/// <strong>Text Regex Detection (Fallback):</strong> When <c>hugh_*</c> tools are unavailable
/// (e.g., in VS Code Copilot chat), agents use text-based signal formats that are detected via
/// regex patterns in <see cref="SignalClassifier"/>. This maintains backward compatibility and
/// ensures signals work across all environments.
/// </description>
/// </item>
/// </list>
/// <para>
/// <strong>Why Both Detection Methods Are Permanent:</strong>
/// </para>
/// <list type="bullet">
/// <item><description>Agent prompts are shared between VS Code Copilot chat and Hugh CLI</description></item>
/// <item><description>VS Code agents do NOT have access to <c>hugh_*</c> custom tools</description></item>
/// <item><description>Text-based formats remain the only fallback for non-Hugh environments</description></item>
/// <item><description>All <see cref="SignalClassifier"/> regex patterns are preserved permanently</description></item>
/// </list>
/// <para>
/// <strong>Priority Order:</strong> Tool-based signals take priority over text regex when both are present.
/// A warning is logged when text fallback is used, which may indicate tool registration issues in Hugh CLI.
/// </para>
/// </remarks>
public class SdkResponseProcessor
{
    private readonly SignalClassifier _signalClassifier;
    private readonly DocumentPathExtractor _pathExtractor;
    private readonly ILogger<SdkResponseProcessor> _logger;
    private readonly ToolMetricsCollector? _metricsCollector;
    private readonly HughWorkflowTools? _workflowTools;
    
    /// <summary>
    /// Tracks ToolCallId to ToolName mappings for correlating start/complete events.
    /// </summary>
    private readonly ConcurrentDictionary<string, string> _toolCallIdToName = new();
    
    /// <summary>
    /// Tracks the most recent tool-based signal detected from hugh_* tool calls.
    /// This is reset at the start of each ProcessResponseAsync call.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When agents call <c>hugh_notify_phase_complete</c>, <c>hugh_report_blocker</c>, or
    /// <c>hugh_notify_workflow_complete</c> tools, the corresponding signal is stored here.
    /// </para>
    /// <para>
    /// In <see cref="ProcessResponseAsync"/>, if this field is set (tool signal detected),
    /// it takes priority over any text-based regex classification from <see cref="SignalClassifier"/>.
    /// This ensures explicit tool calls are honored over potentially ambiguous text patterns.
    /// </para>
    /// </remarks>
    private SignalClassification? _toolBasedSignal;

    /// <summary>
    /// Initializes a new instance of the SdkResponseProcessor.
    /// </summary>
    /// <param name="signalClassifier">The signal classifier for detecting completion signals.</param>
    /// <param name="pathExtractor">The document path extractor for finding output paths.</param>
    /// <param name="logger">The logger instance.</param>
    public SdkResponseProcessor(
        SignalClassifier signalClassifier,
        DocumentPathExtractor pathExtractor,
        ILogger<SdkResponseProcessor> logger)
        : this(signalClassifier, pathExtractor, logger, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the SdkResponseProcessor with metrics collection.
    /// </summary>
    /// <param name="signalClassifier">The signal classifier for detecting completion signals.</param>
    /// <param name="pathExtractor">The document path extractor for finding output paths.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metricsCollector">Optional tool metrics collector for execution tracking.</param>
    public SdkResponseProcessor(
        SignalClassifier signalClassifier,
        DocumentPathExtractor pathExtractor,
        ILogger<SdkResponseProcessor> logger,
        ToolMetricsCollector? metricsCollector)
        : this(signalClassifier, pathExtractor, logger, metricsCollector, null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the SdkResponseProcessor with metrics collection
    /// and workflow tools for blocker metadata enrichment.
    /// </summary>
    /// <param name="signalClassifier">The signal classifier for detecting completion signals.</param>
    /// <param name="pathExtractor">The document path extractor for finding output paths.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="metricsCollector">Optional tool metrics collector for execution tracking.</param>
    /// <param name="workflowTools">Optional workflow tools instance for reading blocker metadata.</param>
    public SdkResponseProcessor(
        SignalClassifier signalClassifier,
        DocumentPathExtractor pathExtractor,
        ILogger<SdkResponseProcessor> logger,
        ToolMetricsCollector? metricsCollector,
        HughWorkflowTools? workflowTools)
    {
        _signalClassifier = signalClassifier ?? throw new ArgumentNullException(nameof(signalClassifier));
        _pathExtractor = pathExtractor ?? throw new ArgumentNullException(nameof(pathExtractor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _metricsCollector = metricsCollector;
        _workflowTools = workflowTools;
    }

    /// <summary>
    /// Result of processing a Copilot session response.
    /// Contains the accumulated content, detected signal, and metadata.
    /// </summary>
    public record ProcessedResponse
    {
        /// <summary>
        /// The accumulated content from all message delta events.
        /// </summary>
        public string Content { get; init; } = "";

        /// <summary>
        /// The signal classification detected in the response content.
        /// </summary>
        public SignalClassification Signal { get; init; } = new() { Type = SignalType.None };

        /// <summary>
        /// The output document path extracted from the response, if any.
        /// </summary>
        public string? OutputDocumentPath { get; init; }

        /// <summary>
        /// Whether the session completed successfully (SessionIdleEvent received).
        /// </summary>
        public bool IsComplete { get; init; }

        /// <summary>
        /// Any error that occurred during processing (from SessionErrorEvent).
        /// </summary>
        public Exception? Error { get; init; }

        /// <summary>
        /// Tool execution metrics collected during the session.
        /// </summary>
        public ToolMetricsSummary? ToolMetrics { get; init; }

        /// <summary>
        /// Whether context compaction occurred during this session.
        /// Indicates the context window was nearing capacity and was compressed.
        /// </summary>
        public bool CompactionOccurred { get; init; }

        /// <summary>
        /// Token usage metrics captured from AssistantUsageEvent during this session.
        /// </summary>
        public UsageMetrics? Usage { get; init; }

        /// <summary>
        /// Subagent lifecycle metrics captured during this session.
        /// </summary>
        public SubagentMetrics? Subagents { get; init; }

        /// <summary>
        /// The session ID assigned by the Copilot SDK, captured for resume capability.
        /// </summary>
        public string? SessionId { get; init; }
    }

    /// <summary>
    /// Processes the response from a Copilot session by subscribing to its events
    /// and accumulating the content until completion or error.
    /// </summary>
    /// <param name="session">The Copilot session to process responses from.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A ProcessedResponse containing the accumulated content and metadata.</returns>
    public async Task<ProcessedResponse> ProcessResponseAsync(
        CopilotSession session,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(session);

        // Reset tool-based signal for new processing session
        _toolBasedSignal = null;

        var responseBuilder = new StringBuilder();
        var completionSource = new TaskCompletionSource();
        Exception? sessionError = null;
        var compactionOccurred = false;

        // Subagent lifecycle counters
        var subagentsStarted = 0;
        var subagentsCompleted = 0;
        var subagentsFailed = 0;

        // Usage metrics accumulator
        UsageMetrics? usageMetrics = null;

        _logger.LogDebug("Starting response processing for Copilot session");

        // Register cancellation
        ct.Register(() =>
        {
            _logger.LogDebug("Response processing cancelled");
            completionSource.TrySetCanceled(ct);
        });

        // Subscribe to session events
        session.On(evt =>
        {
            // Handle known event types in switch statement
            switch (evt)
            {
                case AssistantMessageDeltaEvent delta:
                    responseBuilder.Append(delta.Data.DeltaContent);
                    _logger.LogTrace("Received message delta: {Length} chars", delta.Data.DeltaContent?.Length ?? 0);
                    break;

                case SessionIdleEvent:
                    _logger.LogDebug("Session idle - response complete");
                    completionSource.TrySetResult();
                    break;

                case SessionErrorEvent error:
                    _logger.LogError("Session error: {Message}", error.Data.Message);
                    sessionError = new Exception(error.Data.Message);
                    completionSource.TrySetException(sessionError);
                    break;

                case ToolExecutionStartEvent toolStart:
                    HandleToolStart(toolStart);
                    break;

                case ToolExecutionCompleteEvent toolComplete:
                    HandleToolComplete(toolComplete);
                    break;

                case AssistantMessageEvent msg:
                    responseBuilder.Append(msg.Data.Content);
                    _logger.LogDebug("Received complete assistant message: {Length} chars", msg.Data.Content?.Length ?? 0);
                    break;

                case SessionCompactionStartEvent:
                    _logger.LogInformation("Context compaction started — context window nearing capacity");
                    compactionOccurred = true;
                    break;

                case SessionCompactionCompleteEvent:
                    _logger.LogInformation("Context compaction complete — context freed");
                    compactionOccurred = true;
                    break;

                case SubagentStartedEvent:
                    subagentsStarted++;
                    _logger.LogInformation("Subagent started (total started: {Count})", subagentsStarted);
                    break;

                case SubagentCompletedEvent:
                    subagentsCompleted++;
                    _logger.LogInformation("Subagent completed (total completed: {Count})", subagentsCompleted);
                    break;

                case SubagentFailedEvent:
                    subagentsFailed++;
                    _logger.LogWarning("Subagent failed (total failed: {Count})", subagentsFailed);
                    break;

                case AssistantUsageEvent usage:
                    usageMetrics = new UsageMetrics
                    {
                        InputTokens = (int)(usage.Data.InputTokens ?? 0),
                        OutputTokens = (int)(usage.Data.OutputTokens ?? 0),
                        ReasoningTokens = (int)(usage.Data.ReasoningTokens ?? 0),
                        Duration = TimeSpan.FromSeconds(usage.Data.Duration ?? 0),
                        Model = usage.Data.Model
                    };
                    _logger.LogInformation(
                        "Usage: input={Input}, output={Output}, reasoning={Reasoning}, model={Model}",
                        usage.Data.InputTokens, usage.Data.OutputTokens, usage.Data.ReasoningTokens, usage.Data.Model);
                    break;

                default:
                    break;
            }
        });

        try
        {
            // Wait for the session to complete or error
            await completionSource.Task;

            var content = responseBuilder.ToString();
            _logger.LogDebug("Response processing complete. Content length: {Length}", content.Length);

            // Tool-based signal takes priority over text-based classification.
            // This is the core of the dual-detection architecture:
            // - Hugh CLI agents call hugh_* tools (primary, unambiguous signals)
            // - VS Code agents use text formats (fallback, detected via regex)
            // Both paths remain permanently active for cross-environment compatibility.
            SignalClassification signal;
            if (_toolBasedSignal != null)
            {
                signal = _toolBasedSignal;
                _logger.LogInformation("Using tool-based signal: {SignalType}", signal.Type);
            }
            else
            {
                signal = _signalClassifier.Classify(content);
                if (signal.Type != SignalType.None)
                {
                    // Warning: Text fallback means no hugh_* tool was called.
                    // In Hugh CLI, this may indicate tool registration or detection issues.
                    // In VS Code chat, this is expected (tools unavailable).
                    _logger.LogWarning("No tool-based signal detected, falling back to text regex classification: {SignalType}", signal.Type);
                }
            }

            // Extract any output document path
            var outputPath = _pathExtractor.ExtractDocumentPath(content);
            if (!string.IsNullOrEmpty(outputPath))
            {
                _logger.LogInformation("Extracted output document path: {Path}", outputPath);
            }

            return new ProcessedResponse
            {
                Content = content,
                Signal = signal,
                OutputDocumentPath = outputPath,
                IsComplete = true,
                Error = null,
                ToolMetrics = _metricsCollector?.GetMetrics(),
                CompactionOccurred = compactionOccurred,
                Usage = usageMetrics,
                Subagents = (subagentsStarted > 0 || subagentsCompleted > 0 || subagentsFailed > 0)
                    ? new SubagentMetrics { Started = subagentsStarted, Completed = subagentsCompleted, Failed = subagentsFailed }
                    : null
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning("Response processing was cancelled");
            var content = responseBuilder.ToString();

            return new ProcessedResponse
            {
                Content = content,
                Signal = _signalClassifier.Classify(content),
                OutputDocumentPath = _pathExtractor.ExtractDocumentPath(content),
                IsComplete = false,
                Error = new OperationCanceledException("Response processing was cancelled", ct),
                ToolMetrics = _metricsCollector?.GetMetrics(),
                CompactionOccurred = compactionOccurred,
                Usage = usageMetrics,
                Subagents = (subagentsStarted > 0 || subagentsCompleted > 0 || subagentsFailed > 0)
                    ? new SubagentMetrics { Started = subagentsStarted, Completed = subagentsCompleted, Failed = subagentsFailed }
                    : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during response processing");
            var content = responseBuilder.ToString();

            return new ProcessedResponse
            {
                Content = content,
                Signal = _signalClassifier.Classify(content),
                OutputDocumentPath = _pathExtractor.ExtractDocumentPath(content),
                IsComplete = false,
                Error = sessionError ?? ex,
                ToolMetrics = _metricsCollector?.GetMetrics(),
                CompactionOccurred = compactionOccurred,
                Usage = usageMetrics,
                Subagents = (subagentsStarted > 0 || subagentsCompleted > 0 || subagentsFailed > 0)
                    ? new SubagentMetrics { Started = subagentsStarted, Completed = subagentsCompleted, Failed = subagentsFailed }
                    : null
            };
        }
    }

    /// <summary>
    /// Handles ToolExecutionStartEvent from the SDK.
    /// </summary>
    private void HandleToolStart(ToolExecutionStartEvent toolStart)
    {
        try
        {
            var toolName = toolStart.Data?.ToolName;
            var toolCallId = toolStart.Data?.ToolCallId;
            
            if (!string.IsNullOrEmpty(toolName))
            {
                // Track the mapping for later correlation with complete event
                if (!string.IsNullOrEmpty(toolCallId))
                {
                    _toolCallIdToName[toolCallId] = toolName;
                }
                
                _metricsCollector?.OnToolStart(toolName, toolCallId);
                _logger.LogDebug("Tool execution started: {ToolName} (CallId: {CallId})", toolName, toolCallId);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - metrics collection is optional
            _logger.LogWarning(ex, "Error handling tool start event");
        }
    }

    /// <summary>
    /// Handles ToolExecutionCompleteEvent from the SDK.
    /// </summary>
    private void HandleToolComplete(ToolExecutionCompleteEvent toolComplete)
    {
        try
        {
            var toolCallId = toolComplete.Data?.ToolCallId;
            
            // Look up the tool name from the start event
            string? toolName = null;
            if (!string.IsNullOrEmpty(toolCallId))
            {
                _toolCallIdToName.TryRemove(toolCallId, out toolName);
            }
            
            if (!string.IsNullOrEmpty(toolName))
            {
                var success = toolComplete.Data?.Success ?? false;
                var errorMessage = toolComplete.Data?.Error?.Message;
                
                _metricsCollector?.OnToolComplete(toolName, success, errorMessage, toolCallId);
                _logger.LogDebug(
                    "Tool execution complete: {ToolName}, Success: {Success}", 
                    toolName, success);
                
                // Detect tool-based signals from hugh_* tools
                var toolSignal = DetectToolSignal(toolName);
                if (toolSignal != null)
                {
                    // Enrich blocker signals with tool-provided metadata
                    if (toolSignal.Type == SignalType.Blocker && _workflowTools != null)
                    {
                        toolSignal = toolSignal with
                        {
                            IsSkippable = _workflowTools.LastBlockerIsSkippable,
                            RawMatch = _workflowTools.LastBlockerReason
                        };
                    }

                    _toolBasedSignal = toolSignal;
                    _logger.LogDebug("Detected tool-based signal from {ToolName}: {SignalType}", toolName, toolSignal.Type);
                }
            }
            else
            {
                // Log when we can't correlate
                _logger.LogTrace("Tool complete event without matching start: {CallId}", toolCallId);
            }
        }
        catch (Exception ex)
        {
            // Log but don't fail - metrics collection is optional
            _logger.LogWarning(ex, "Error handling tool complete event");
        }
    }

    /// <summary>
    /// Detects a signal classification based on a hugh_* tool name.
    /// </summary>
    /// <param name="toolName">The name of the tool that was executed.</param>
    /// <returns>A SignalClassification if the tool is a recognized signal tool, null otherwise.</returns>
    /// <remarks>
    /// <para>
    /// This method maps Hugh structured communication tools to signal types:
    /// </para>
    /// <list type="bullet">
    /// <item><description><c>hugh_notify_phase_complete</c> → <see cref="SignalType.PhaseComplete"/></description></item>
    /// <item><description><c>hugh_notify_workflow_complete</c> → <see cref="SignalType.WorkflowComplete"/></description></item>
    /// <item><description><c>hugh_report_blocker</c> → <see cref="SignalType.Blocker"/></description></item>
    /// </list>
    /// <para>
    /// Other <c>hugh_*</c> tools (e.g., <c>hugh_get_workspace_structure</c>) return null as they
    /// are utility tools, not workflow signals.
    /// </para>
    /// </remarks>
    internal SignalClassification? DetectToolSignal(string? toolName)
    {
        if (string.IsNullOrEmpty(toolName) || !toolName.StartsWith("hugh_", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return toolName.ToLowerInvariant() switch
        {
            "hugh_notify_phase_complete" => new SignalClassification { Type = SignalType.PhaseComplete, Source = SignalSource.ToolCall },
            "hugh_notify_workflow_complete" => new SignalClassification { Type = SignalType.WorkflowComplete, Source = SignalSource.ToolCall },
            "hugh_report_blocker" => new SignalClassification { Type = SignalType.Blocker, Source = SignalSource.ToolCall },
            _ => null
        };
    }


    /// <summary>
    /// Determines if the signal indicates the workflow or phase is complete.
    /// </summary>
    /// <param name="signalType">The signal type to check.</param>
    /// <returns>True if the signal indicates completion, false otherwise.</returns>
    public static bool IsCompletionSignal(SignalType signalType)
    {
        return signalType is SignalType.PhaseComplete or SignalType.WorkflowComplete;
    }

    /// <summary>
    /// Determines if the signal indicates execution should pause for user input.
    /// </summary>
    /// <param name="signalType">The signal type to check.</param>
    /// <returns>True if execution should pause, false otherwise.</returns>
    public static bool IsPauseSignal(SignalType signalType)
    {
        return signalType is SignalType.QuestionOpen
            or SignalType.QuestionWithRecommendation
            or SignalType.Blocker
            or SignalType.ModeSelection
            or SignalType.NeedsUserInput;
    }
}
