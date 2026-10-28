using System.Diagnostics;
using GitHub.Copilot.SDK;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Handlers;
using Hugh.Hooks;
using Hugh.Logging;
using Hugh.Metrics;
using Hugh.Models;
using Hugh.Tools;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Serilog;
using Serilog.Extensions.Logging;
using ILogger = Serilog.ILogger;
using SdkSessionConfig = GitHub.Copilot.SDK.SessionConfig;
using SdkUserInputResponse = GitHub.Copilot.SDK.UserInputResponse;

namespace Hugh.Agents;

/// <summary>
/// Implementation of ICopilotService that wraps the GitHub Copilot SDK.
/// Provides session management, retry logic, and signal handling for agent execution.
/// </summary>
public class CopilotService : ICopilotService, IAsyncDisposable
{
    private readonly HughConfig _config;
    private readonly ILogger _logger;
    private readonly QuestionHandler _questionHandler;
    private readonly SdkQuestionHandler _sdkQuestionHandler;
    private readonly BlockerHandler _blockerHandler;
    private readonly SignalClassifier _signalClassifier;
    private readonly DocumentPathExtractor _pathExtractor;
    private readonly SessionErrorClassifier _errorClassifier;
    private readonly ILoggerFactory _loggerFactory;
    private readonly string _workspacePath;

    private CopilotClient? _client;
    private bool _clientStarted;
    private bool _disposed;
    
    /// <summary>
    /// The workflow tools instance created for the most recent session.
    /// Used to read blocker metadata (IsSkippable, Reason) after tool-based signal detection.
    /// </summary>
    private HughWorkflowTools? _lastSessionWorkflowTools;
    
    /// <summary>
    /// The current run ID, set during ExecutePhaseAsync and accessible to SDK callbacks.
    /// </summary>
    private Guid _currentRunId;
    
    /// <summary>
    /// The current agent name, set during ExecutePhaseAsync for SDK callback context.
    /// </summary>
    private string _currentAgentName = string.Empty;

    /// <summary>
    /// Callback invoked when agent requests user input (via text-based signal detection).
    /// Returns the response to send, or null to pause execution.
    /// </summary>
    public Func<SignalClassification, string, Guid, CancellationToken, Task<QuestionResponse?>>? OnQuestionDetected { get; set; }

    public CopilotService(
        HughConfig config,
        DecisionLogger decisionLogger,
        ILogger? logger = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? Log.Logger;
        _sdkQuestionHandler = new SdkQuestionHandler(decisionLogger, config.Autonomous, _logger);
        _questionHandler = new QuestionHandler(_sdkQuestionHandler, _logger);
        _blockerHandler = new BlockerHandler(_logger);
        _signalClassifier = new SignalClassifier();
        _pathExtractor = new DocumentPathExtractor();
        _errorClassifier = new SessionErrorClassifier();
        _workspacePath = Directory.GetCurrentDirectory();

        // Create logger factory with Serilog provider
        _loggerFactory = new SerilogLoggerFactory(_logger);

        // Wire up default question handling via QuestionHandler
        OnQuestionDetected = _questionHandler.HandleQuestionAsync;
    }

    /// <summary>
    /// Creates a CopilotService without decision logging (for testing).
    /// </summary>
    public CopilotService(HughConfig config, ILogger? logger = null)
        : this(config, new DecisionLogger(Directory.GetCurrentDirectory(), logger), logger)
    {
    }

    /// <summary>
    /// Ensures the CopilotClient is initialized and started.
    /// Uses lazy initialization to defer client creation until first use.
    /// </summary>
    internal async Task EnsureClientStartedAsync(CancellationToken ct)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(CopilotService), "Cannot start client after disposal.");

        if (_clientStarted) return;

        _logger.Debug("Initializing CopilotClient...");
        try
        {
            _client = new CopilotClient(new CopilotClientOptions
            {
                Cwd = _workspacePath,
                Logger = _loggerFactory.CreateLogger("CopilotSdk"),
                UseLoggedInUser = true
            });
            await _client.StartAsync();
            _clientStarted = true;
            _logger.Information("CopilotClient initialized and started");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Error(ex, "Failed to start CopilotClient");

            var message = ex.Message.ToLowerInvariant();
            if (message.Contains("copilot") || message.Contains("process") || message.Contains("not found") || message.Contains("no such file"))
            {
                throw new InvalidOperationException(
                    "Failed to start the Copilot CLI process. The standalone 'copilot' binary may not be installed. " +
                    "Install it with: winget install GitHub.Copilot — then verify with: copilot --version — " +
                    "or run: hugh doctor --fix",
                    ex);
            }
            if (message.Contains("auth") || message.Contains("401") || message.Contains("token") || message.Contains("unauthorized"))
            {
                throw new InvalidOperationException(
                    "Copilot authentication failed. Ensure GitHub CLI is authenticated: gh auth login — " +
                    "then verify with: gh auth status — " +
                    "or set GH_TOKEN environment variable with 'repo' and 'read:org' scopes.",
                    ex);
            }
            if (message.Contains("timeout") || message.Contains("timed out"))
            {
                throw new InvalidOperationException(
                    "Copilot CLI timed out during startup. Check your internet connection and try again.",
                    ex);
            }

            throw new InvalidOperationException(
                $"Failed to initialize the Copilot SDK: {ex.Message}. " +
                "Try: 1) Run 'hugh doctor' to check prerequisites, " +
                "2) Verify 'copilot --version' works, " +
                "3) Run 'gh auth status' to check authentication.",
                ex);
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        if (_client != null)
        {
            _logger.Debug("Disposing CopilotClient...");
            try
            {
                await _client.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
            }
            catch (TimeoutException)
            {
                _logger.Warning("CopilotClient.StopAsync timed out after 5s, forcing stop");
                await _client.ForceStopAsync();
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error during CopilotClient stop, forcing stop");
                try { await _client.ForceStopAsync(); } catch { /* best effort */ }
            }
            await _client.DisposeAsync();
            _client = null;
        }

        _loggerFactory.Dispose();
        _clientStarted = false;
        _disposed = true;

        GC.SuppressFinalize(this);
    }

    /// <inheritdoc />
    public async Task<PhaseResult> ExecutePhaseAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        string prompt,
        IEnumerable<string> documentPaths,
        CancellationToken ct,
        string? lastSessionId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var decisions = new List<Decision>();
        var runId = Guid.NewGuid();
        
        // Store run context for SDK callbacks
        _currentRunId = runId;
        _currentAgentName = agentDefinition.Name;

        _logger.Information("Executing phase with agent {AgentName}", agentDefinition.Name);
        _logger.Debug("Agent prompt length: {Length}", agentPrompt.Length);
        _logger.Debug("User prompt: {Prompt}", prompt);
        _logger.Debug("Document paths: {DocumentPaths}", string.Join(", ", documentPaths));

        try
        {
            // Ensure client is started
            await EnsureClientStartedAsync(ct);

            // Create session with agent prompt (or resume if session ID available)
            await using var session = await ResumeOrCreateSessionAsync(
                agentDefinition, agentPrompt, lastSessionId, ct);
            var sessionId = session.SessionId;

            // Create response processor with metrics collection and workflow tools
            var metricsCollector = new ToolMetricsCollector(_logger);
            var responseProcessor = new SdkResponseProcessor(
                _signalClassifier,
                _pathExtractor,
                _loggerFactory.CreateLogger<SdkResponseProcessor>(),
                metricsCollector,
                _lastSessionWorkflowTools);

            // Create SDK attachments from document paths
            var attachments = CreateAttachments(documentPaths);

            // Send the prompt with attachments and retry logic
            await SendWithRetryAsync(session, prompt, ct, attachments);

            // Process the response
            var processedResponse = await responseProcessor.ProcessResponseAsync(session, ct);

            // Log tool metrics summary
            var metrics = processedResponse.ToolMetrics;
            if (metrics != null && metrics.TotalExecutions > 0)
            {
                _logger.Information(
                    "Tool metrics: {Total} executions, {Success} success, {Errors} errors",
                    metrics.TotalExecutions, metrics.SuccessCount, metrics.ErrorCount);
            }

            stopwatch.Stop();

            if (processedResponse.Error != null)
            {
                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = processedResponse.Error.Message,
                    DecisionsMade = decisions,
                    Duration = stopwatch.Elapsed,
                    CompactionOccurred = processedResponse.CompactionOccurred,
                    ToolMetrics = processedResponse.ToolMetrics,
                    ErrorClassification = _errorClassifier.Classify(processedResponse.Error)
                };
            }

            // Handle any signals detected in the response
            if (SdkResponseProcessor.IsPauseSignal(processedResponse.Signal.Type))
            {
                var questionResponse = await HandleSignalAsync(
                    processedResponse.Signal,
                    agentDefinition.Name,
                    runId,
                    ct);

                if (questionResponse?.DecisionMade != null)
                {
                    decisions.Add(questionResponse.DecisionMade);
                }

                if (questionResponse?.ShouldPause == true)
                {
                    // NeedsUserInput: re-invocation loop (max 3 attempts)
                    if (questionResponse.QuestionText != null)
                    {
                        // Guard: skip interactive input in autonomous mode or non-interactive environments
                        if (_config.Autonomous.Enabled || Console.IsInputRedirected)
                        {
                            _logger.Warning(
                                "Skipping interactive input — autonomous mode: {Autonomous}, stdin redirected: {Redirected}. " +
                                "Question: {Question}",
                                _config.Autonomous.Enabled, Console.IsInputRedirected, questionResponse.QuestionText);

                            return new PhaseResult
                            {
                                Success = false,
                                ErrorMessage = $"Phase paused — user input required: {questionResponse.QuestionText}",
                                DecisionsMade = decisions,
                                Duration = stopwatch.Elapsed,
                                CompactionOccurred = processedResponse.CompactionOccurred,
                                ToolMetrics = processedResponse.ToolMetrics
                            };
                        }

                        const int maxUserInputAttempts = 3;
                        for (var attempt = 0; attempt < maxUserInputAttempts; attempt++)
                        {
                            var userAnswer = Console.ReadLine()?.Trim();
                            if (string.IsNullOrEmpty(userAnswer))
                            {
                                _logger.Warning("Empty user answer received, pausing execution");
                                break;
                            }

                            _logger.Information(
                                "User answered (attempt {Attempt}/{Max}): {Answer}",
                                attempt + 1, maxUserInputAttempts, userAnswer);

                            // Send follow-up to the SAME session with the user's answer
                            var followUpPrompt =
                                $"The user was asked: {questionResponse.QuestionText}\n" +
                                $"The user answered: {userAnswer}\n\n" +
                                "Continue the current phase with this context.";

                            await SendWithRetryAsync(session, followUpPrompt, ct);

                            processedResponse = await responseProcessor.ProcessResponseAsync(session, ct);

                            if (processedResponse.Error != null)
                            {
                                return new PhaseResult
                                {
                                    Success = false,
                                    ErrorMessage = processedResponse.Error.Message,
                                    DecisionsMade = decisions,
                                    Duration = stopwatch.Elapsed,
                                    CompactionOccurred = processedResponse.CompactionOccurred,
                                    ToolMetrics = processedResponse.ToolMetrics,
                                    ErrorClassification = _errorClassifier.Classify(processedResponse.Error)
                                };
                            }

                            // Check if we got another NeedsUserInput
                            if (SdkResponseProcessor.IsPauseSignal(processedResponse.Signal.Type))
                            {
                                questionResponse = await HandleSignalAsync(
                                    processedResponse.Signal,
                                    agentDefinition.Name,
                                    runId,
                                    ct);

                                if (questionResponse?.DecisionMade != null)
                                {
                                    decisions.Add(questionResponse.DecisionMade);
                                }

                                if (questionResponse?.ShouldPause == true && questionResponse.QuestionText != null)
                                {
                                    // Another NeedsUserInput — loop continues
                                    continue;
                                }

                                if (questionResponse?.ShouldPause == true)
                                {
                                    // Non-NeedsUserInput pause (e.g., blocker)
                                    return new PhaseResult
                                    {
                                        Success = false,
                                        ErrorMessage = "Execution paused for user input",
                                        OutputDocumentPath = processedResponse.OutputDocumentPath,
                                        DecisionsMade = decisions,
                                        Duration = stopwatch.Elapsed,
                                        CompactionOccurred = processedResponse.CompactionOccurred,
                                        ToolMetrics = processedResponse.ToolMetrics
                                    };
                                }
                            }

                            // Response is not a pause signal — break out of loop
                            break;
                        }
                    }
                    else
                    {
                        // Regular pause (no question text) — preserve existing behavior
                        return new PhaseResult
                        {
                            Success = false,
                            ErrorMessage = "Execution paused for user input",
                            OutputDocumentPath = processedResponse.OutputDocumentPath,
                            DecisionsMade = decisions,
                            Duration = stopwatch.Elapsed,
                            CompactionOccurred = processedResponse.CompactionOccurred,
                            ToolMetrics = processedResponse.ToolMetrics
                        };
                    }
                }
            }

            _logger.Information(
                "Phase execution completed for {AgentName} in {Duration}",
                agentDefinition.Name,
                stopwatch.Elapsed);

            // Check if a review outcome was signaled via workflow tools
            var reviewOutcome = ParseReviewOutcome(_lastSessionWorkflowTools?.LastReviewOutcome);

            return new PhaseResult
            {
                Success = true,
                OutputDocumentPath = processedResponse.OutputDocumentPath,
                ErrorMessage = null,
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed,
                CompactionOccurred = processedResponse.CompactionOccurred,
                ToolMetrics = processedResponse.ToolMetrics,
                ReviewOutcome = reviewOutcome,
                Usage = processedResponse.Usage,
                Subagents = processedResponse.Subagents,
                SessionId = sessionId
            };
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.Warning("Agent execution was cancelled for {AgentName}", agentDefinition.Name);

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = "Operation was cancelled",
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed,
                ErrorClassification = _errorClassifier.Classify(ex)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Agent execution failed for {AgentName}", agentDefinition.Name);

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed,
                ErrorClassification = _errorClassifier.Classify(ex)
            };
        }
    }

    /// <inheritdoc />
    public async Task<PhaseResult> ExecuteInteractivePhaseAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        string prompt,
        IEnumerable<string> documentPaths,
        string expectedOutputPath,
        int maxTurns,
        CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();
        var decisions = new List<Decision>();
        var runId = Guid.NewGuid();

        _currentRunId = runId;
        _currentAgentName = agentDefinition.Name;

        _logger.Information(
            "Executing interactive phase with agent {AgentName} (max {MaxTurns} turns)",
            agentDefinition.Name, maxTurns);

        try
        {
            await EnsureClientStartedAsync(ct);
            await using var session = await CreateSessionAsync(agentDefinition, agentPrompt, ct);

            var metricsCollector = new ToolMetricsCollector(_logger);
            var responseProcessor = new SdkResponseProcessor(
                _signalClassifier,
                _pathExtractor,
                _loggerFactory.CreateLogger<SdkResponseProcessor>(),
                metricsCollector);

            // Create SDK attachments and send initial prompt
            var attachments = CreateAttachments(documentPaths);
            await SendWithRetryAsync(session, prompt, ct, attachments);

            // Track consecutive empty answers for progressive escalation
            var consecutiveEmptyAnswers = 0;

            // Multi-turn Q&A loop (mirrors SolutionOrchestrator.RunClarificationAsync pattern)
            for (var turn = 0; turn < maxTurns; turn++)
            {
                ct.ThrowIfCancellationRequested();

                var processedResponse = await responseProcessor.ProcessResponseAsync(session, ct);

                if (processedResponse.Error != null)
                {
                    stopwatch.Stop();
                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = processedResponse.Error.Message,
                        DecisionsMade = decisions,
                        Duration = stopwatch.Elapsed,
                        CompactionOccurred = processedResponse.CompactionOccurred,
                        ToolMetrics = processedResponse.ToolMetrics,
                        ErrorClassification = _errorClassifier.Classify(processedResponse.Error)
                    };
                }

                // Check for completion signals
                if (processedResponse.Signal.Type == SignalType.WorkflowComplete ||
                    processedResponse.Signal.Type == SignalType.PhaseComplete)
                {
                    _logger.Information(
                        "Interactive phase complete after {Turns} turns", turn + 1);

                    stopwatch.Stop();
                    var outputPath = processedResponse.OutputDocumentPath ?? expectedOutputPath;
                    return new PhaseResult
                    {
                        Success = true,
                        OutputDocumentPath = File.Exists(outputPath) ? outputPath :
                                             File.Exists(expectedOutputPath) ? expectedOutputPath : null,
                        DecisionsMade = decisions,
                        Duration = stopwatch.Elapsed,
                        CompactionOccurred = processedResponse.CompactionOccurred,
                        ToolMetrics = processedResponse.ToolMetrics
                    };
                }

                // Handle auto-accept signals (recommendations, mode selection)
                // before prompting the user
                if (SdkResponseProcessor.IsPauseSignal(processedResponse.Signal.Type))
                {
                    var signalResponse = await HandleSignalAsync(
                        processedResponse.Signal, agentDefinition.Name, runId, ct);

                    if (signalResponse?.DecisionMade != null)
                    {
                        decisions.Add(signalResponse.DecisionMade);
                    }

                    // If auto-accepted (ShouldPause == false with a response), send it back
                    // to the session and continue the loop
                    if (signalResponse?.ShouldPause == false && signalResponse.Response != null)
                    {
                        await SendWithRetryAsync(session, signalResponse.Response, ct);
                        continue;
                    }
                }

                // Display agent's content/question to the user
                if (!string.IsNullOrWhiteSpace(processedResponse.Content))
                {
                    Console.WriteLine();
                    Console.WriteLine(processedResponse.Content.Trim());
                    Console.WriteLine();
                }
                else
                {
                    // No text content was captured — the SDK may have delivered the agent's
                    // response via an event type we don't handle yet, or the agent produced
                    // only tool calls with no visible text. Log for diagnostics.
                    _logger.Warning(
                        "No text content captured from agent response on turn {Turn}. " +
                        "Signal: {SignalType}. The agent may have communicated via " +
                        "an unhandled SDK event type.",
                        turn + 1, processedResponse.Signal.Type);

                    // Display signal question text as fallback if available
                    if (!string.IsNullOrWhiteSpace(processedResponse.Signal.QuestionText))
                    {
                        Console.WriteLine();
                        Console.WriteLine(processedResponse.Signal.QuestionText.Trim());
                        Console.WriteLine();
                    }
                }

                // Read user's answer
                Console.Write("  Your answer: ");
                var userAnswer = Console.ReadLine()?.Trim();

                if (string.IsNullOrEmpty(userAnswer))
                {
                    consecutiveEmptyAnswers++;
                    _logger.Information(
                        "User provided empty answer on turn {Turn} (consecutive: {Count})",
                        turn + 1, consecutiveEmptyAnswers);

                    if (consecutiveEmptyAnswers == 1)
                    {
                        // First empty answer: accept the recommendation or ask to finalize
                        userAnswer = "Accept the recommendation. If you asked a question with options, " +
                                     "use the recommended option. If there is no recommendation, " +
                                     "make reasonable choices and continue.";
                        Console.WriteLine("  (Accepting recommendation...)");
                    }
                    else
                    {
                        // Second+ consecutive empty answer: force finalization
                        userAnswer = "The user is done providing input. Finalize and create the output document now. " +
                                     "Fill in any remaining sections with reasonable defaults. " +
                                     "Do NOT ask any more questions. Complete the document immediately.";
                        Console.WriteLine("  (Finalizing...)");
                    }
                }
                else if (IsPhaseTransitionRequest(userAnswer))
                {
                    // User wants to move on to the next phase — treat as finalization
                    consecutiveEmptyAnswers = 0;
                    _logger.Information(
                        "User requested phase transition on turn {Turn}: {Answer}",
                        turn + 1, userAnswer);
                    userAnswer = "The user wants to proceed to the next workflow phase. " +
                                 "Finalize and create the output document now with reasonable defaults " +
                                 "for any remaining sections. Do NOT suggest invoking other agents — " +
                                 "the orchestrator will handle the transition automatically. " +
                                 "Complete the document immediately.";
                    Console.WriteLine("  (Finalizing for next phase...)");
                }
                else
                {
                    // User provided a real answer — reset empty counter
                    consecutiveEmptyAnswers = 0;
                    Console.WriteLine("  Processing...");
                }

                // Send the answer back to the SAME session
                await SendWithRetryAsync(session, userAnswer, ct);
            }

            // Hit max turns -- check if output was produced anyway
            _logger.Warning(
                "Interactive phase hit max turns ({MaxTurns}), checking for output",
                maxTurns);

            stopwatch.Stop();

            if (File.Exists(expectedOutputPath))
            {
                return new PhaseResult
                {
                    Success = true,
                    OutputDocumentPath = expectedOutputPath,
                    DecisionsMade = decisions,
                    Duration = stopwatch.Elapsed
                };
            }

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Interactive phase exceeded {maxTurns} turns without producing output",
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException ex)
        {
            stopwatch.Stop();
            _logger.Warning("Interactive phase cancelled for {AgentName}", agentDefinition.Name);
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = "Operation was cancelled",
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed,
                ErrorClassification = _errorClassifier.Classify(ex)
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.Error(ex, "Interactive phase failed for {AgentName}", agentDefinition.Name);
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                DecisionsMade = decisions,
                Duration = stopwatch.Elapsed,
                ErrorClassification = _errorClassifier.Classify(ex)
            };
        }
    }

    /// <summary>
    /// Creates a Copilot session configured with the agent prompt.
    /// </summary>
    internal async Task<CopilotSession> CreateSessionAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        CancellationToken ct)
    {
        if (_client == null)
        {
            throw new InvalidOperationException("CopilotClient not initialized. Call EnsureClientStartedAsync first.");
        }

        _logger.Debug("Creating session for agent {AgentName}", agentDefinition.Name);

        // Create workflow tools for agent signaling (phase complete, blockers, etc.)
        var workflowTools = new HughWorkflowTools(_logger, _workspacePath);
        _lastSessionWorkflowTools = workflowTools;

        // Create file system hooks for directory auto-creation and path monitoring
        var fileSystemHooks = new FileSystemHooks(_logger, _workspacePath, agentDefinition.Name);

        var sessionConfig = new SdkSessionConfig
        {
            Model = _config.Copilot.Model,
            SystemMessage = new SystemMessageConfig
            {
                Mode = SystemMessageMode.Append,
                Content = agentPrompt
            },
            Hooks = fileSystemHooks.CreateHooks(),
            Tools = workflowTools.CreateToolSet(),
            OnPermissionRequest = PermissionHandler.ApproveAll,
            OnUserInputRequest = async (request, invocation) =>
            {
                var hughRequest = HughUserInputRequest.FromSdkRequest(request);
                var response = await _sdkQuestionHandler.HandleAsync(hughRequest, _currentAgentName, _currentRunId);
                return new SdkUserInputResponse
                {
                    Answer = response.Answer,
                    WasFreeform = !response.IsAutomatic
                };
            },
            InfiniteSessions = new InfiniteSessionConfig
            {
                Enabled = true,
                BackgroundCompactionThreshold = _config.Copilot.CompactionThreshold,
                BufferExhaustionThreshold = _config.Copilot.BufferExhaustionThreshold
            }
        };

        var session = await _client.CreateSessionAsync(sessionConfig);

        _logger.Debug("Session created with model {Model} and file system hooks", _config.Copilot.Model);
        return session;
    }

    /// <summary>
    /// Attempts to resume an existing session by ID, falling back to creating a new session
    /// if resume fails. Used by the --resume path when a previous session ID is available.
    /// </summary>
    internal async Task<CopilotSession> ResumeOrCreateSessionAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        string? lastSessionId,
        CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(lastSessionId) && _client != null)
        {
            try
            {
                _logger.Information("Attempting to resume session {SessionId}", lastSessionId);

                var workflowTools = new HughWorkflowTools(_logger, _workspacePath);
                _lastSessionWorkflowTools = workflowTools;

                var fileSystemHooks = new FileSystemHooks(_logger, _workspacePath, agentDefinition.Name);

                var resumeConfig = new ResumeSessionConfig
                {
                    Model = _config.Copilot.Model,
                    SystemMessage = new SystemMessageConfig
                    {
                        Mode = SystemMessageMode.Append,
                        Content = agentPrompt
                    },
                    Hooks = fileSystemHooks.CreateHooks(),
                    Tools = workflowTools.CreateToolSet(),
                    OnPermissionRequest = PermissionHandler.ApproveAll,
                    OnUserInputRequest = async (request, invocation) =>
                    {
                        var hughRequest = HughUserInputRequest.FromSdkRequest(request);
                        var response = await _sdkQuestionHandler.HandleAsync(hughRequest, _currentAgentName, _currentRunId);
                        return new SdkUserInputResponse
                        {
                            Answer = response.Answer,
                            WasFreeform = !response.IsAutomatic
                        };
                    },
                    InfiniteSessions = new InfiniteSessionConfig
                    {
                        Enabled = true,
                        BackgroundCompactionThreshold = _config.Copilot.CompactionThreshold,
                        BufferExhaustionThreshold = _config.Copilot.BufferExhaustionThreshold
                    }
                };

                var session = await _client.ResumeSessionAsync(lastSessionId, resumeConfig, ct);
                _logger.Information("Successfully resumed session {SessionId}", lastSessionId);
                return session;
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to resume session {SessionId}, falling back to new session", lastSessionId);
            }
        }

        // Fall back to creating a new session
        return await CreateSessionAsync(agentDefinition, agentPrompt, ct);
    }

    /// <summary>
    /// Determines if a user's answer indicates they want to transition to the next phase
    /// (e.g., "go to planning", "continue", "move on") rather than answering the agent's question.
    /// </summary>
    private static bool IsPhaseTransitionRequest(string userAnswer)
    {
        var lower = userAnswer.ToLowerInvariant().Trim();
        
        // Common phrases indicating the user wants to move forward
        var transitionPhrases = new[]
        {
            "go to planning", "continue planning", "start planning",
            "go to research", "continue research", "start research",
            "go to coding", "continue coding", "start coding",
            "go to review", "continue review", "start review",
            "move on", "move to next", "next phase", "next step",
            "skip to", "proceed", "let's go", "let's move on",
            "done", "that's it", "that's all", "i'm done", "im done",
            "finish", "finalize", "wrap up", "complete it",
            "continue", "go ahead"
        };
        
        return transitionPhrases.Any(phrase => lower.Contains(phrase));
    }

    /// <summary>
    /// Sends a message to the session with retry logic based on error classification.
    /// Rate-limited errors use a flat 60s delay; transient errors use exponential backoff.
    /// Non-retryable errors (authentication, context exhausted) are re-thrown immediately.
    /// </summary>
    internal async Task SendWithRetryAsync(
        CopilotSession session,
        string prompt,
        CancellationToken ct,
        IList<UserMessageAttachment>? attachments = null)
    {
        var attempt = 0;
        var options = new MessageOptions { Prompt = prompt };
        if (attachments is { Count: > 0 })
        {
            options.Attachments = attachments;
        }
        while (true)
        {
            try
            {
                _logger.Debug("Sending message to session (attempt {Attempt})", attempt + 1);
                await session.SendAsync(options);
                return;
            }
            catch (Exception ex)
            {
                var classification = _errorClassifier.Classify(ex);
                if (!classification.IsRetryable || attempt >= _config.Copilot.MaxRetryAttempts)
                    throw;

                attempt++;
                var delay = CalculateRetryDelay(classification, attempt, _config.Copilot.RetryBaseDelayMs);
                _logger.Warning(
                    ex,
                    "{Category} error on attempt {Attempt}/{MaxAttempts}. Retrying in {Delay}ms",
                    classification.Category,
                    attempt,
                    _config.Copilot.MaxRetryAttempts,
                    delay.TotalMilliseconds);
                await Task.Delay(delay, ct);
            }
        }
    }

    /// <summary>
    /// Calculates the retry delay based on error classification and attempt number.
    /// Rate-limited errors get a flat delay (typically 60s). Transient errors get
    /// exponential backoff using the classifier's suggested base delay.
    /// </summary>
    /// <param name="classification">The error classification from SessionErrorClassifier.</param>
    /// <param name="attempt">The current retry attempt number (1-based).</param>
    /// <param name="retryBaseDelayMs">Fallback base delay in milliseconds from config.</param>
    /// <returns>The calculated delay before the next retry.</returns>
    internal static TimeSpan CalculateRetryDelay(ErrorClassification classification, int attempt, int retryBaseDelayMs)
    {
        var baseDelay = classification.SuggestedDelay ?? TimeSpan.FromMilliseconds(retryBaseDelayMs);
        return classification.Category == ErrorCategory.RateLimited
            ? baseDelay  // Flat delay for rate limits (60s)
            : TimeSpan.FromTicks(baseDelay.Ticks * (long)Math.Pow(2, attempt - 1));  // Exponential for transient
    }

    /// <summary>
    /// Creates SDK attachment objects from document file paths.
    /// Each path becomes a UserMessageAttachmentFile for native SDK attachment handling.
    /// </summary>
    internal static IList<UserMessageAttachment>? CreateAttachments(IEnumerable<string> documentPaths)
    {
        var paths = documentPaths.ToList();
        if (paths.Count == 0) return null;

        var attachments = new List<UserMessageAttachment>();
        foreach (var path in paths)
        {
            if (string.IsNullOrWhiteSpace(path)) continue;

            attachments.Add(new UserMessageAttachmentFile
            {
                Path = path,
                DisplayName = Path.GetFileName(path)
            });
        }

        return attachments.Count > 0 ? attachments : null;
    }

    /// <summary>
    /// Handles a signal detected in agent output.
    /// Routes to appropriate handler based on signal type.
    /// </summary>
    /// <param name="classification">The detected signal.</param>
    /// <param name="currentPhase">Current phase name.</param>
    /// <param name="runId">Current run ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>QuestionResponse if input was requested, null otherwise.</returns>
    public async Task<QuestionResponse?> HandleSignalAsync(
        SignalClassification classification,
        string currentPhase,
        Guid runId,
        CancellationToken ct = default)
    {
        switch (classification.Type)
        {
            case SignalType.QuestionWithRecommendation:
            case SignalType.QuestionOpen:
            case SignalType.ModeSelection:
            case SignalType.NeedsUserInput:
                if (OnQuestionDetected != null)
                {
                    return await OnQuestionDetected(classification, currentPhase, runId, ct);
                }
                _logger.Warning("No OnQuestionDetected handler configured");
                return QuestionResponse.Pause();

            case SignalType.Blocker:
                // Check for auto-skip: tool-based skippable blockers in autonomous mode
                if (classification.Source == SignalSource.ToolCall
                    && classification.IsSkippable
                    && _config.Autonomous.Enabled)
                {
                    _logger.Warning(
                        "Auto-skipping skippable blocker in autonomous mode: {Description}",
                        classification.RawMatch ?? "Unknown");

                    return new QuestionResponse
                    {
                        ShouldPause = false,
                        Response = null,
                        DecisionMade = new Decision
                        {
                            Phase = currentPhase,
                            Question = $"Skippable blocker: {classification.RawMatch ?? "Unknown"}",
                            Options = new List<string> { "Skip", "Pause" },
                            Selected = "Skip",
                            Rationale = "Auto-skipped: isSkippable=true in autonomous mode",
                            Autonomous = true,
                            Timestamp = DateTime.UtcNow
                        }
                    };
                }

                // Existing behavior for non-skippable or text-based blockers
                var blockerResponse = _blockerHandler.HandleBlocker(classification, currentPhase);
                _logger.Error("Blocker detected: {Description}", blockerResponse.Description);
                // Convert blocker to question response format for consistent handling
                return new QuestionResponse
                {
                    ShouldPause = true,
                    Response = null,
                    DecisionMade = null
                };

            case SignalType.PhaseComplete:
                _logger.Information(
                    "Phase {PhaseNumber} complete signal detected",
                    classification.PhaseNumber);
                return null;

            case SignalType.WorkflowComplete:
                _logger.Information("Workflow complete signal detected");
                return null;

            default:
                return null;
        }
    }
    
    /// <summary>
    /// Handles a user input request using the SDK-native SdkQuestionHandler.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method provides SDK-native handling for user input requests. It converts
    /// SignalClassification to UserInputRequest and delegates to SdkQuestionHandler.
    /// </para>
    /// <para>
    /// Can be wired as an alternative to QuestionHandler when SDK-native callbacks
    /// become available in future SDK versions.
    /// </para>
    /// </remarks>
    /// <param name="classification">The signal classification from agent output.</param>
    /// <param name="currentPhase">Current phase name.</param>
    /// <param name="runId">Current run ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>QuestionResponse compatible with existing flow.</returns>
    public async Task<QuestionResponse?> HandleSdkUserInputAsync(
        SignalClassification classification,
        string currentPhase,
        Guid runId,
        CancellationToken ct = default)
    {
        // Only handle question-type signals via SDK handler
        if (classification.Type is not (SignalType.QuestionWithRecommendation or 
                                        SignalType.QuestionOpen or 
                                        SignalType.ModeSelection or 
                                        SignalType.NeedsUserInput))
        {
            return null;
        }
        
        // Convert to Hugh's internal UserInputRequest type
        var request = HughUserInputRequest.FromSignalClassification(classification);
        
        // Handle via SdkQuestionHandler
        var response = await _sdkQuestionHandler.HandleAsync(request, currentPhase, runId, ct);
        
        // Convert back to QuestionResponse for compatibility
        if (response.IsAutomatic)
        {
            // Auto-accepted - create decision record
            var decision = new Decision
            {
                Phase = currentPhase,
                Question = request.Prompt,
                Options = request.Choices?.ToList() ?? new List<string>(),
                Selected = response.Answer,
                Rationale = "Auto-accepted via SdkQuestionHandler",
                Autonomous = true,
                Timestamp = DateTime.UtcNow
            };
            
            return QuestionResponse.AutoRespond(response.Answer, decision);
        }
        
        // User provided input - no pause needed
        if (!string.IsNullOrEmpty(response.Answer))
        {
            return new QuestionResponse
            {
                ShouldPause = false,
                Response = response.Answer,
                QuestionText = request.Prompt
            };
        }
        
        // Empty response means user cancelled or error
        return QuestionResponse.Pause();
    }
    
    /// <summary>
    /// Gets the SdkQuestionHandler for direct access (primarily for testing).
    /// </summary>
    internal SdkQuestionHandler SdkQuestionHandler => _sdkQuestionHandler;

    /// <summary>
    /// Creates a new SdkResponseProcessor instance for use by external orchestrators
    /// that manage their own session lifecycle.
    /// </summary>
    internal SdkResponseProcessor CreateResponseProcessor()
    {
        var metricsCollector = new ToolMetricsCollector(_logger);
        return new SdkResponseProcessor(
            _signalClassifier,
            _pathExtractor,
            _loggerFactory.CreateLogger<SdkResponseProcessor>(),
            metricsCollector);
    }

    /// <summary>
    /// Gets the signal classifier for external use by orchestrators.
    /// </summary>
    internal SignalClassifier SignalClassifier => _signalClassifier;

    /// <summary>
    /// Gets the document path extractor for external use by orchestrators.
    /// </summary>
    internal DocumentPathExtractor PathExtractor => _pathExtractor;

    /// <summary>
    /// Parses a review outcome string from workflow tools into the ReviewOutcome enum.
    /// Returns null if the value is null, empty, or unrecognized.
    /// </summary>
    private static ReviewOutcome? ParseReviewOutcome(string? value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        return value.ToLowerInvariant() switch
        {
            "approved" => ReviewOutcome.Approved,
            "approved_with_findings" => ReviewOutcome.ApprovedWithFindings,
            "rejected" => ReviewOutcome.Rejected,
            _ => null
        };
    }
}
