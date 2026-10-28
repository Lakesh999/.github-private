using System.Diagnostics;
using Hugh;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Metrics;
using Hugh.Models;
using Hugh.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hugh.Orchestration;

/// <summary>
/// Main orchestration class for the self-operating development loop.
/// Coordinates the execution of Vision → Research → Planning → Review → Coding phases.
/// </summary>
public class LoopOrchestrator : IAsyncDisposable
{
    /// <summary>
    /// Phase execution order for the development loop.
    /// </summary>
    private static readonly LoopPhase[] PhaseOrder =
    {
        LoopPhase.Vision,
        LoopPhase.Research,
        LoopPhase.Planning,
        LoopPhase.Review,
        LoopPhase.Coding
    };

    private readonly PhaseExecutor _phaseExecutor;
    private readonly LoopStateManager _stateManager;
    private readonly PlanPhaseParser _planPhaseParser;
    private readonly ResearchPhaseParser _researchPhaseParser;
    private readonly PlanCreationPhaseParser _planCreationPhaseParser;
    private readonly TaskParser _taskParser;
    private readonly GitCommitManager? _gitCommitManager;
    private readonly HughConfig _config;
    private readonly ILogger<LoopOrchestrator> _logger;

    /// <summary>
    /// Optional release context when running as part of a multi-release solution.
    /// Set by RunForContextAsync and used by BuildPromptForPhase.
    /// </summary>
    private ReleaseContext? _releaseContext;

    /// <summary>
    /// Initializes a new instance of the LoopOrchestrator.
    /// </summary>
    /// <param name="phaseExecutor">The phase executor for running individual phases.</param>
    /// <param name="stateManager">The state manager for persistence.</param>
    /// <param name="planPhaseParser">The parser for plan phase detection.</param>
    /// <param name="researchPhaseParser">The parser for research phase detection.</param>
    /// <param name="planCreationPhaseParser">The parser for plan creation phase detection (skeleton filling).</param>
    /// <param name="taskParser">The parser for task-level detection within plan phases.</param>
    /// <param name="gitCommitManager">Optional git commit manager for per-phase commits.</param>
    /// <param name="options">Configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    public LoopOrchestrator(
        PhaseExecutor phaseExecutor,
        LoopStateManager stateManager,
        PlanPhaseParser planPhaseParser,
        ResearchPhaseParser researchPhaseParser,
        PlanCreationPhaseParser planCreationPhaseParser,
        TaskParser taskParser,
        IOptions<HughConfig> options,
        ILogger<LoopOrchestrator> logger,
        GitCommitManager? gitCommitManager = null)
    {
        _phaseExecutor = phaseExecutor;
        _stateManager = stateManager;
        _planPhaseParser = planPhaseParser;
        _researchPhaseParser = researchPhaseParser;
        _planCreationPhaseParser = planCreationPhaseParser;
        _taskParser = taskParser;
        _gitCommitManager = gitCommitManager;
        _config = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Runs the full development loop for a new feature request.
    /// </summary>
    /// <param name="featureRequest">The feature request to implement.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the loop completed successfully, false otherwise.</returns>
    public async Task<bool> RunAsync(string featureRequest, CancellationToken ct = default)
    {
        _logger.LogInformation("Starting development loop for feature: {Feature}", featureRequest);

        // Create new run state
        var state = await _stateManager.CreateRunAsync(featureRequest, ct);
        _logger.LogInformation("Created run {RunId}", state.RunId);

        // Execute the loop
        return await ExecuteLoopAsync(state, ct);
    }

    /// <summary>
    /// Runs the development loop starting from a specific phase, with optional release context.
    /// Used by SolutionOrchestrator to execute per-release loops starting at Research
    /// (skipping Vision, which is handled at the solution level).
    /// </summary>
    /// <param name="featureRequest">The feature/release description.</param>
    /// <param name="startPhase">The phase to start from (defaults to Vision).</param>
    /// <param name="releaseContext">Optional release context for multi-release solutions.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the loop completed successfully, false otherwise.</returns>
    public async Task<bool> RunForContextAsync(
        string featureRequest,
        LoopPhase startPhase = LoopPhase.Vision,
        ReleaseContext? releaseContext = null,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "Starting development loop from phase {StartPhase} for: {Feature}",
            startPhase, featureRequest);

        _releaseContext = releaseContext;

        LoopState state;

        if (startPhase != LoopPhase.Vision && releaseContext != null)
        {
            // Create run directly at the target phase with pre-populated documents
            var documents = new DocumentPaths
            {
                Vision = releaseContext.SolutionVisionPath
            };

            state = await _stateManager.CreateRunFromPhaseAsync(featureRequest, startPhase, documents, ct);
        }
        else
        {
            // Standard run starting from Vision
            state = await _stateManager.CreateRunAsync(featureRequest, ct);
        }

        _logger.LogInformation("Created run {RunId} starting at {Phase}", state.RunId, startPhase);

        return await ExecuteLoopAsync(state, ct);
    }

    /// <summary>
    /// Resumes a paused or failed run from its current state.
    /// </summary>
    /// <param name="runId">Optional run ID to resume. If null, resumes the most recent run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if the loop completed successfully, false otherwise.</returns>
    public async Task<bool> ResumeAsync(Guid? runId = null, CancellationToken ct = default)
    {
        _logger.LogInformation("Resuming run {RunId}", runId?.ToString() ?? "most recent");

        // Load the state
        LoopState? state;
        if (runId.HasValue)
        {
            state = await _stateManager.LoadStateAsync(runId.Value, ct);
        }
        else
        {
            state = await _stateManager.LoadStateAsync(ct);
        }

        if (state == null)
        {
            _logger.LogError("No run state found to resume");
            return false;
        }

        // Check if the run is in a resumable state
        if (state.Status == PhaseStatus.Complete)
        {
            _logger.LogWarning("Run {RunId} is already complete", state.RunId);
            return true;
        }

        _logger.LogInformation(
            "Resuming run {RunId} from phase {Phase} (status: {Status})",
            state.RunId, state.CurrentPhase, state.Status);

        // Execute from current position
        return await ExecuteLoopAsync(state, ct);
    }

    /// <summary>
    /// Gets the current status of a run.
    /// </summary>
    /// <param name="runId">Optional run ID. If null, gets the most recent run.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current state, or null if no run exists.</returns>
    public async Task<LoopState?> GetStatusAsync(Guid? runId = null, CancellationToken ct = default)
    {
        if (runId.HasValue)
        {
            return await _stateManager.LoadStateAsync(runId.Value, ct);
        }
        return await _stateManager.LoadStateAsync(ct);
    }

    /// <summary>
    /// Executes the development loop from the current state.
    /// </summary>
    internal async Task<bool> ExecuteLoopAsync(LoopState state, CancellationToken ct)
    {
        var currentPhaseIndex = GetPhaseIndex(state.CurrentPhase);
        var phaseMetrics = new List<PhaseMetricsEntry>();
        var loopStopwatch = Stopwatch.StartNew();

        // Execute phases from current position
        for (var i = currentPhaseIndex; i < PhaseOrder.Length; i++)
        {
            ct.ThrowIfCancellationRequested();

            var phase = PhaseOrder[i];

            // Skip already completed phases
            if (state.CompletedPhases.Contains(phase))
            {
                _logger.LogDebug("Skipping already completed phase: {Phase}", phase);
                continue;
            }

            // Update checkpoint before starting phase
            await _stateManager.UpdatePhaseAsync(phase, PhaseStatus.InProgress, null, ct);

            // Ensure output directory exists before running the phase
            EnsurePhaseOutputDirectoryExists(phase);

            // Execute the phase via dispatch (with per-phase retry for retryable errors)
            var result = await DispatchPhaseAsync(phase, state, ct);

            // Per-phase retry for non-interactive phases with retryable errors
            if (!result.Success && !IsInteractivePhase(phase)
                && result.ErrorClassification?.IsRetryable == true)
            {
                var maxRetries = _config.Copilot.MaxPhaseRetries;
                for (var retryAttempt = 1; retryAttempt <= maxRetries; retryAttempt++)
                {
                    var delay = result.ErrorClassification.SuggestedDelay
                        ?? TimeSpan.FromSeconds(5);
                    _logger.LogWarning(
                        "Phase {Phase} failed with retryable error ({Category}). " +
                        "Retry {Attempt}/{Max} after {Delay}s",
                        phase, result.ErrorClassification.Category,
                        retryAttempt, maxRetries, delay.TotalSeconds);

                    await Task.Delay(delay, ct);

                    // Re-dispatch the phase (coding re-reads plan from disk; completed sub-phases skipped)
                    result = await DispatchPhaseAsync(phase, state, ct);
                    result = result with { RetryAttemptsMade = retryAttempt };

                    if (result.Success) break;
                    if (result.ErrorClassification?.IsRetryable != true) break;
                }

                if (!result.Success)
                {
                    _logger.LogError(
                        "Phase {Phase} failed after exhausting {Max} retries. " +
                        "Last error: {Category} — {Error}",
                        phase, maxRetries,
                        result.ErrorClassification?.Category.ToString() ?? "Unknown",
                        result.ErrorMessage);
                }
            }

            if (!result.Success)
            {
                _logger.LogError("Phase {Phase} failed: {Error}", phase, result.ErrorMessage);
                await _stateManager.MarkPhaseFailedAsync(result.ErrorMessage ?? "Unknown error", ct);
                return false;
            }

            // Validate that document-producing phases actually created their output file
            if (PhaseProducesDocument(phase))
            {
                if (string.IsNullOrEmpty(result.OutputDocumentPath))
                {
                    _logger.LogError(
                        "Phase {Phase} succeeded but produced no output document path", phase);
                    await _stateManager.MarkPhaseFailedAsync(
                        $"{phase} phase completed but no output document path was returned", ct);
                    return false;
                }

                if (!File.Exists(result.OutputDocumentPath))
                {
                    _logger.LogError(
                        "Phase {Phase} succeeded but output document does not exist on disk: {Path}",
                        phase, result.OutputDocumentPath);
                    await _stateManager.MarkPhaseFailedAsync(
                        $"{phase} phase completed but output document was not found at: {result.OutputDocumentPath}", ct);
                    return false;
                }
            }

            // Mark phase complete and update document paths
            await _stateManager.MarkPhaseCompleteAsync(result.OutputDocumentPath, ct);

            // Persist session ID for --resume continuity
            if (!string.IsNullOrEmpty(result.SessionId))
            {
                await _stateManager.UpdateSessionIdAsync(result.SessionId, ct);
            }

            // Collect phase metrics for session report
            phaseMetrics.Add(new PhaseMetricsEntry
            {
                PhaseName = phase.ToString(),
                Usage = result.Usage,
                Subagents = result.Subagents,
                Tools = result.ToolMetrics,
                Duration = result.Duration
            });

            // Refresh state after completion
            state = _stateManager.CurrentState!;

            _logger.LogInformation(
                "Phase {Phase} completed. Output: {Output}",
                phase, result.OutputDocumentPath ?? "(none)");
        }

        loopStopwatch.Stop();

        // Build and display session report
        var report = BuildSessionReport(state.RunId, phaseMetrics, loopStopwatch.Elapsed);
        ConsoleOutput.WriteSessionReport(report);

        _logger.LogInformation("Development loop completed successfully for run {RunId}", state.RunId);
        return true;
    }

    /// <summary>
    /// Builds a SessionReport by aggregating phase metrics.
    /// </summary>
    private static SessionReport BuildSessionReport(Guid runId, List<PhaseMetricsEntry> phases, TimeSpan totalDuration)
    {
        var totalInput = 0;
        var totalOutput = 0;
        var totalReasoning = 0;
        var totalSubStarted = 0;
        var totalSubCompleted = 0;
        var totalSubFailed = 0;
        var compactionEvents = 0;

        foreach (var phase in phases)
        {
            if (phase.Usage != null)
            {
                totalInput += phase.Usage.InputTokens;
                totalOutput += phase.Usage.OutputTokens;
                totalReasoning += phase.Usage.ReasoningTokens;
            }
            if (phase.Subagents != null)
            {
                totalSubStarted += phase.Subagents.Started;
                totalSubCompleted += phase.Subagents.Completed;
                totalSubFailed += phase.Subagents.Failed;
            }
        }

        return new SessionReport
        {
            RunId = runId,
            Phases = phases,
            TotalUsage = new UsageMetrics
            {
                InputTokens = totalInput,
                OutputTokens = totalOutput,
                ReasoningTokens = totalReasoning,
                Duration = totalDuration
            },
            TotalSubagents = new SubagentMetrics
            {
                Started = totalSubStarted,
                Completed = totalSubCompleted,
                Failed = totalSubFailed
            },
            CompactionEvents = compactionEvents,
            TotalDuration = totalDuration
        };
    }

    /// <summary>
    /// Dispatches a phase to the appropriate executor.
    /// Extracted from ExecuteLoopAsync to enable per-phase retry and testability.
    /// </summary>
    /// <param name="phase">The phase to dispatch.</param>
    /// <param name="state">The current loop state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the phase execution.</returns>
    protected virtual async Task<PhaseResult> DispatchPhaseAsync(
        LoopPhase phase, LoopState state, CancellationToken ct)
    {
        if (phase == LoopPhase.Research)
        {
            return await ExecuteResearchPhasesAsync(state.Documents.Vision, ct);
        }

        if (phase == LoopPhase.Planning)
        {
            return await ExecutePlanningPhasesAsync(state, ct);
        }

        if (phase == LoopPhase.Review)
        {
            return await ExecuteReviewCycleAsync(state, ct);
        }

        if (phase == LoopPhase.Coding)
        {
            var planPath = state.Documents.Plan;
            if (string.IsNullOrEmpty(planPath))
            {
                _logger.LogError("Cannot start Coding phase: no plan document path in state");
                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage =
                        "Coding phase cannot start: no plan document path was recorded from the Planning phase"
                };
            }

            if (!File.Exists(planPath))
            {
                _logger.LogError(
                    "Cannot start Coding phase: plan document not found at {PlanPath}", planPath);
                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = $"Coding phase cannot start: plan document not found at: {planPath}"
                };
            }

            return await ExecuteCodingPhasesAsync(planPath, ct);
        }

        if (IsInteractivePhase(phase))
        {
            return await ExecuteInteractivePhaseAsync(phase, state, ct);
        }

        return await ExecuteSinglePhaseAsync(phase, state, ct);
    }

    /// <summary>
    /// Executes a single phase (non-coding).
    /// </summary>
    /// <summary>
    /// Executes a single phase (non-coding). Protected virtual to enable testing.
    /// </summary>
    protected virtual async Task<PhaseResult> ExecuteSinglePhaseAsync(
        LoopPhase phase, 
        LoopState state, 
        CancellationToken ct)
    {
        var prompt = BuildPromptForPhase(phase, state);
        var documents = GetDocumentsForPhase(phase, state.Documents);

        _logger.LogInformation(
            "Executing phase {Phase} with {DocCount} documents",
            phase, documents.Length);

        return await _phaseExecutor.ExecuteAsync(phase, prompt, documents, state.LastSessionId);
    }

    /// <summary>
    /// Detects the review outcome from a phase result using dual-detection:
    /// tool-based signal first (via <see cref="PhaseResult.ReviewOutcome"/>),
    /// then falls back to text regex via <see cref="SignalClassifier"/>.
    /// </summary>
    /// <param name="reviewResult">The result from the review phase execution.</param>
    /// <returns>The detected <see cref="ReviewOutcome"/>, defaulting to <see cref="ReviewOutcome.Approved"/> if undetermined.</returns>
    internal ReviewOutcome DetectReviewOutcome(PhaseResult reviewResult)
    {
        // 1. Tool-based detection (primary): check PhaseResult.ReviewOutcome set from workflow tools
        if (reviewResult.ReviewOutcome.HasValue)
        {
            _logger.LogDebug(
                "Review outcome detected via tool signal: {Outcome}",
                reviewResult.ReviewOutcome.Value);
            return reviewResult.ReviewOutcome.Value;
        }

        // 2. Text regex detection (fallback): scan agent output for review patterns
        var responseContent = reviewResult.ErrorMessage ?? string.Empty;
        // Note: For text fallback, we check the signal classifier patterns.
        // In practice, the review agent's output may be in ErrorMessage (for partial results)
        // or the output document content.

        // Check for revision signals first (higher priority indicates action needed)
        if (_planPhaseParser is { } parser)
        {
            // Try reading the plan document for review signals
            var state = _stateManager.CurrentState;
            if (state?.Documents.Plan != null && File.Exists(state.Documents.Plan))
            {
                try
                {
                    var planContent = File.ReadAllText(state.Documents.Plan);
                    var signalClassifier = new SignalClassifier();

                    if (signalClassifier.IsReviewNeedsRevision(planContent))
                    {
                        _logger.LogDebug("Review outcome detected via text regex: Rejected");
                        return ReviewOutcome.Rejected;
                    }

                    if (signalClassifier.IsReviewApproved(planContent))
                    {
                        _logger.LogDebug("Review outcome detected via text regex: Approved");
                        return ReviewOutcome.Approved;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to read plan document for review outcome detection");
                }
            }
        }

        // Default to Approved if no signal detected (prevents infinite loops)
        _logger.LogWarning(
            "No review outcome signal detected — defaulting to Approved to prevent blocking");
        return ReviewOutcome.Approved;
    }

    /// <summary>
    /// Executes the review phase as a review-revise cycle.
    /// Runs Review → detects outcome → if not approved and under MaxReviewCycles,
    /// runs a Planning revision session → re-reviews. Bounded by configuration.
    /// </summary>
    /// <param name="state">The current loop state.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The result of the final review execution.</returns>
    private async Task<PhaseResult> ExecuteReviewCycleAsync(LoopState state, CancellationToken ct)
    {
        var maxCycles = _config.Review.MaxReviewCycles;
        _logger.LogInformation(
            "Starting review-revise cycle (max {MaxCycles} revision cycles)", maxCycles);

        for (var cycle = 0; cycle <= maxCycles; cycle++)
        {
            ct.ThrowIfCancellationRequested();

            _logger.LogInformation("Review cycle {Cycle}/{MaxCycles}", cycle, maxCycles);

            // Run Review phase
            var reviewResult = await ExecuteSinglePhaseAsync(LoopPhase.Review, state, ct);

            if (!reviewResult.Success)
            {
                _logger.LogError("Review phase failed in cycle {Cycle}: {Error}",
                    cycle, reviewResult.ErrorMessage);
                return reviewResult;
            }

            var outcome = DetectReviewOutcome(reviewResult);

            switch (outcome)
            {
                case ReviewOutcome.Approved:
                    _logger.LogInformation("Review approved on cycle {Cycle}", cycle);
                    return reviewResult;

                case ReviewOutcome.ApprovedWithFindings:
                    _logger.LogWarning(
                        "Review approved with findings on cycle {Cycle} — proceeding to coding",
                        cycle);
                    return reviewResult;

                case ReviewOutcome.Rejected:
                    if (cycle == maxCycles)
                    {
                        _logger.LogWarning(
                            "Max review cycles ({MaxCycles}) reached with Rejected outcome — proceeding anyway",
                            maxCycles);
                        return reviewResult;
                    }

                    _logger.LogInformation(
                        "Review rejected on cycle {Cycle} — running planning revision",
                        cycle);

                    // Run Planning revision session
                    var revisionResult = await ExecuteSinglePhaseAsync(LoopPhase.Planning, state, ct);
                    if (!revisionResult.Success)
                    {
                        _logger.LogError("Planning revision failed in cycle {Cycle}: {Error}",
                            cycle, revisionResult.ErrorMessage);
                        return revisionResult;
                    }

                    // Refresh state after planning revision (document paths may have changed)
                    state = _stateManager.CurrentState!;
                    break;
            }
        }

        // Should not reach here, but return a safe result
        _logger.LogWarning("Review cycle loop exited unexpectedly — proceeding");
        return new PhaseResult
        {
            Success = true,
            Duration = TimeSpan.Zero
        };
    }

    /// <summary>
    /// Executes the research phase as a loop of sub-phases.
    /// First iteration creates the research outline, subsequent iterations execute each sub-phase.
    /// Mirrors the pattern used in ExecuteCodingPhasesAsync.
    /// </summary>
    private async Task<PhaseResult> ExecuteResearchPhasesAsync(string? visionPath, CancellationToken ct)
    {
        _logger.LogInformation("Starting research phase loop");

        // --- Iteration 0: Create the research outline ---
        var state = _stateManager.CurrentState!;
        var outlinePrompt = BuildPromptForPhase(LoopPhase.Research, state);
        var outlineDocuments = GetDocumentsForPhase(LoopPhase.Research, state.Documents);

        _logger.LogInformation("Executing research outline creation");

        var outlineResult = await _phaseExecutor.ExecuteAsync(
            LoopPhase.Research, outlinePrompt, outlineDocuments);

        if (!outlineResult.Success)
        {
            _logger.LogError("Research outline creation failed: {Error}", outlineResult.ErrorMessage);
            return outlineResult;
        }

        var researchDocPath = outlineResult.OutputDocumentPath;
        if (string.IsNullOrEmpty(researchDocPath) || !File.Exists(researchDocPath))
        {
            _logger.LogError(
                "Research outline completed but no document found at: {Path}",
                researchDocPath ?? "(null)");
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = "Research outline phase completed but no research document was created",
                Duration = outlineResult.Duration
            };
        }

        _logger.LogInformation("Research outline created at: {Path}", researchDocPath);

        var totalDuration = outlineResult.Duration;

        // --- Iteration 1..N: Execute each research sub-phase ---
        while (!ct.IsCancellationRequested)
        {
            // Re-read the research document from disk each iteration
            var researchContent = await File.ReadAllTextAsync(researchDocPath, ct);

            // Find the next incomplete research phase
            var nextPhase = _researchPhaseParser.FindNextIncompletePhase(researchContent);
            if (nextPhase == null)
            {
                _logger.LogInformation("All research sub-phases complete");
                break;
            }

            _logger.LogInformation(
                "Executing research sub-phase {SubPhase}: {PhaseName}",
                nextPhase.Number, nextPhase.Name);

            // Update checkpoint with current research sub-phase
            await _stateManager.UpdatePhaseAsync(
                LoopPhase.Research,
                PhaseStatus.InProgress,
                nextPhase.Number,
                ct);

            // Build continuation prompt with inlined scope
            var continuationPrompt = BuildResearchContinuationPrompt(researchDocPath, nextPhase);

            // Attach the research doc (and vision doc if available)
            var documents = string.IsNullOrEmpty(visionPath)
                ? new[] { researchDocPath }
                : new[] { researchDocPath, visionPath };

            var result = await _phaseExecutor.ExecuteAsync(
                LoopPhase.Research, continuationPrompt, documents);

            totalDuration += result.Duration;

            if (!result.Success)
            {
                _logger.LogError(
                    "Research sub-phase {SubPhase} failed: {Error}",
                    nextPhase.Number, result.ErrorMessage);

                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = $"Research sub-phase {nextPhase.Number} failed: {result.ErrorMessage}",
                    Duration = totalDuration
                };
            }

            _logger.LogInformation(
                "Research sub-phase {SubPhase} completed in {Duration}",
                nextPhase.Number, result.Duration);

            // Small delay between sub-phases to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        // Post-loop safety net: verify all phases are complete
        var finalContent = await File.ReadAllTextAsync(researchDocPath, ct);
        if (_researchPhaseParser.HasIncompletePhases(finalContent))
        {
            var incomplete = _researchPhaseParser.GetIncompletePhaseNumbers(finalContent);
            _logger.LogWarning(
                "Research loop ended but incomplete phases remain: {Phases}",
                string.Join(", ", incomplete));

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Research phase loop ended but phases [{string.Join(", ", incomplete)}] are still incomplete",
                Duration = totalDuration
            };
        }

        return new PhaseResult
        {
            Success = true,
            OutputDocumentPath = researchDocPath,
            Duration = totalDuration
        };
    }

    /// <summary>
    /// Builds the prompt for a research sub-phase continuation.
    /// </summary>
    private static string BuildResearchContinuationPrompt(string researchDocPath, ResearchPhase phase)
    {
        return $"""
            You are running inside the Hugh CLI — a self-operating development loop.
            Continue the research document at `{researchDocPath}`.
            Execute Phase {phase.Number}: {phase.Name}.

            Phase scope:
            {phase.Scope}

            The research document is attached for context.
            Execute ONLY this phase, then update the research document with your findings.
            Do not proceed to subsequent phases.
            """;
    }

    /// <summary>
    /// Executes the planning phase as a loop of sub-phases.
    /// First iteration creates the plan skeleton, subsequent iterations fill in
    /// each phase's detailed task table.
    /// Mirrors the pattern used in ExecuteResearchPhasesAsync.
    /// </summary>
    private async Task<PhaseResult> ExecutePlanningPhasesAsync(LoopState state, CancellationToken ct)
    {
        _logger.LogInformation("Starting planning phase loop");

        // --- Iteration 0: Create the plan skeleton ---
        var skeletonPrompt = BuildPromptForPhase(LoopPhase.Planning, state);
        var skeletonDocuments = GetDocumentsForPhase(LoopPhase.Planning, state.Documents);

        _logger.LogInformation("Executing planning skeleton creation");

        var skeletonResult = await _phaseExecutor.ExecuteAsync(
            LoopPhase.Planning, skeletonPrompt, skeletonDocuments);

        if (!skeletonResult.Success)
        {
            _logger.LogError("Planning skeleton creation failed: {Error}", skeletonResult.ErrorMessage);
            return skeletonResult;
        }

        var planDocPath = skeletonResult.OutputDocumentPath;
        if (string.IsNullOrEmpty(planDocPath) || !File.Exists(planDocPath))
        {
            _logger.LogError(
                "Planning skeleton completed but no document found at: {Path}",
                planDocPath ?? "(null)");
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = "Planning skeleton phase completed but no plan document was created",
                Duration = skeletonResult.Duration
            };
        }

        _logger.LogInformation("Planning skeleton created at: {Path}", planDocPath);

        var totalDuration = skeletonResult.Duration;

        // --- Iteration 1..N: Fill in each planning sub-phase ---
        while (!ct.IsCancellationRequested)
        {
            // Re-read the plan document from disk each iteration
            var planContent = await File.ReadAllTextAsync(planDocPath, ct);

            // Find the next incomplete planning phase (no task table yet)
            var nextPhase = _planCreationPhaseParser.FindNextIncompletePhase(planContent);
            if (nextPhase == null)
            {
                _logger.LogInformation("All planning sub-phases complete");
                break;
            }

            _logger.LogInformation(
                "Executing planning sub-phase {SubPhase}: {PhaseName}",
                nextPhase.Number, nextPhase.Name);

            // Update checkpoint with current planning sub-phase
            await _stateManager.UpdatePhaseAsync(
                LoopPhase.Planning,
                PhaseStatus.InProgress,
                nextPhase.Number,
                ct);

            // Build continuation prompt from built-in template
            var continuationPrompt = BuildPlanningContinuationPrompt(planDocPath, nextPhase);

            // Attach the plan doc (and vision + research if available)
            var documents = GetNonNullDocuments(
                planDocPath,
                state.Documents.Vision,
                state.Documents.Research);

            var result = await _phaseExecutor.ExecuteAsync(
                LoopPhase.Planning, continuationPrompt, documents);

            totalDuration += result.Duration;

            if (!result.Success)
            {
                _logger.LogError(
                    "Planning sub-phase {SubPhase} failed: {Error}",
                    nextPhase.Number, result.ErrorMessage);

                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = $"Planning sub-phase {nextPhase.Number} failed: {result.ErrorMessage}",
                    Duration = totalDuration
                };
            }

            _logger.LogInformation(
                "Planning sub-phase {SubPhase} completed in {Duration}",
                nextPhase.Number, result.Duration);

            // Small delay between sub-phases to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        // Post-loop safety net: verify all phases are complete
        var finalContent = await File.ReadAllTextAsync(planDocPath, ct);
        if (_planCreationPhaseParser.HasIncompletePhases(finalContent))
        {
            var incomplete = _planCreationPhaseParser.GetIncompletePhaseNumbers(finalContent);
            _logger.LogWarning(
                "Planning loop ended but incomplete phases remain: {Phases}",
                string.Join(", ", incomplete));

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Planning phase loop ended but phases [{string.Join(", ", incomplete)}] still need task tables",
                Duration = totalDuration
            };
        }

        return new PhaseResult
        {
            Success = true,
            OutputDocumentPath = planDocPath,
            Duration = totalDuration
        };
    }

    /// <summary>
    /// Builds the prompt for a planning sub-phase continuation using the built-in template.
    /// Loads the planning-continuation prompt and replaces placeholders.
    /// </summary>
    private static string BuildPlanningContinuationPrompt(string planDocPath, PlanCreationPhase phase)
    {
        var template = BuiltInPromptLoader.Load("planning-continuation");
        return template
            .Replace("{planPath}", planDocPath)
            .Replace("{phaseNumber}", phase.Number.ToString())
            .Replace("{phaseName}", phase.Name);
    }

    /// <summary>
    /// Determines if a phase should run in interactive mode based on configuration.
    /// Only applies to standalone feature loops (not multi-release solutions).
    /// </summary>
    protected virtual bool IsInteractivePhase(LoopPhase phase)
    {
        // Skip interactive mode when running as part of a multi-release solution.
        // SolutionOrchestrator handles its own clarification conversation.
        if (_releaseContext != null) return false;

        // Skip interactive mode in non-interactive environments
        if (Console.IsInputRedirected) return false;

        return _config.Autonomous.InteractivePhases
            .Any(p => string.Equals(p, phase.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Executes a phase interactively with multi-turn Q&amp;A.
    /// </summary>
    private async Task<PhaseResult> ExecuteInteractivePhaseAsync(
        LoopPhase phase,
        LoopState state,
        CancellationToken ct)
    {
        var prompt = BuildPromptForPhase(phase, state);
        var documents = GetDocumentsForPhase(phase, state.Documents);
        var maxTurns = _config.Autonomous.MaxInteractiveTurns;

        _logger.LogInformation(
            "Executing interactive phase {Phase} with {DocCount} documents (max {MaxTurns} turns)",
            phase, documents.Length, maxTurns);

        ConsoleOutput.WriteSection($"Interactive {phase} Phase");
        Console.WriteLine("  Hugh will ask questions to refine the vision. Type your answers below.");
        Console.WriteLine("  Press Enter with no text to accept the recommendation or finalize.");
        Console.WriteLine();

        return await _phaseExecutor.ExecuteInteractiveAsync(phase, prompt, documents, maxTurns);
    }

    /// <summary>
    /// Executes the coding phase as a loop of sub-phases.
    /// Each sub-phase corresponds to a phase in the implementation plan.
    /// When <c>Coding.Granularity</c> is <c>"task"</c>, each task within a phase
    /// gets its own isolated session for better context management.
    /// </summary>
    private async Task<PhaseResult> ExecuteCodingPhasesAsync(string planPath, CancellationToken ct)
    {
        _logger.LogInformation(
            "Starting coding phase loop for plan: {PlanPath} (granularity: {Granularity})",
            planPath, _config.Coding.Granularity);

        var codingPhaseNumber = 0;
        var totalDuration = TimeSpan.Zero;
        var useTaskGranularity = string.Equals(
            _config.Coding.Granularity, "task", StringComparison.OrdinalIgnoreCase);

        int? lastAttemptedPhaseNumber = null;
        int samePhaseAttempts = 0;
        const int MaxSamePhaseAttempts = 2;

        while (!ct.IsCancellationRequested)
        {
            // Verify plan file still exists (it's re-read each iteration)
            if (!File.Exists(planPath))
            {
                _logger.LogError("Plan document no longer exists at {PlanPath}", planPath);
                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = $"Plan document was deleted during coding phase execution: {planPath}",
                    Duration = totalDuration
                };
            }

            // Read the current plan content
            var planContent = await File.ReadAllTextAsync(planPath, ct);

            // Find the next incomplete phase
            var nextPhase = _planPhaseParser.FindNextIncompletePhase(planContent);
            if (nextPhase == null)
            {
                _logger.LogInformation("All coding phases complete");
                break;
            }

            codingPhaseNumber++;

            // Same-phase repetition guard: abort if stuck on the same phase
            if (nextPhase.Number == lastAttemptedPhaseNumber)
            {
                samePhaseAttempts++;
                if (samePhaseAttempts >= MaxSamePhaseAttempts)
                {
                    _logger.LogError(
                        "Phase {PlanPhase} ({PhaseName}) attempted {Count} times without progress. " +
                        "Aborting coding loop. Check that phase headers use '### Phase N: Name' format " +
                        "and task tables use '| N.M | Task | Files | Criteria |' format.",
                        nextPhase.Number, nextPhase.Name, samePhaseAttempts + 1);
                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = $"Phase {nextPhase.Number} stuck: attempted {samePhaseAttempts + 1} times " +
                            "without progress. Likely cause: task table format mismatch in plan document.",
                        Duration = totalDuration
                    };
                }
            }
            else
            {
                lastAttemptedPhaseNumber = nextPhase.Number;
                samePhaseAttempts = 0;
            }

            // Update checkpoint with current coding sub-phase
            await _stateManager.UpdatePhaseAsync(
                LoopPhase.Coding,
                PhaseStatus.InProgress,
                nextPhase.Number,
                ct);

            if (useTaskGranularity)
            {
                // Task-level granularity: iterate individual tasks within this phase
                var taskResult = await ExecuteTasksInPhaseAsync(
                    planPath, nextPhase, codingPhaseNumber, ct);
                totalDuration += taskResult.Duration;

                if (!taskResult.Success)
                {
                    return taskResult;
                }
            }
            else
            {
                // Phase-level granularity: execute the entire phase in one session
                _logger.LogInformation(
                    "Executing coding sub-phase {SubPhase}: Phase {PlanPhase} - {PhaseName}",
                    codingPhaseNumber, nextPhase.Number, nextPhase.Name);

                var prompt = BuildCodingPhasePrompt(nextPhase);

                var result = await _phaseExecutor.ExecuteAsync(
                    LoopPhase.Coding,
                    prompt,
                    new[] { planPath });

                totalDuration += result.Duration;

                if (!result.Success)
                {
                    _logger.LogError(
                        "Coding phase {SubPhase} failed: {Error}",
                        codingPhaseNumber, result.ErrorMessage);

                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = $"Coding sub-phase {nextPhase.Number} failed: {result.ErrorMessage}",
                        Duration = totalDuration
                    };
                }

                _logger.LogInformation(
                    "Coding sub-phase {SubPhase} completed in {Duration}",
                    codingPhaseNumber, result.Duration);
            }

            // Per-sub-phase build verification (catches compile errors early)
            // Only runs here for phase-level granularity or as a final check after all tasks;
            // task-level build verification is handled inside ExecuteTasksInPhaseAsync when VerifyBuildPerTask is enabled.
            if (_config.Verification.Enabled
                && _config.Verification.VerifyBuildPerSubPhase
                && !useTaskGranularity
                && !string.IsNullOrEmpty(_config.Verification.BuildCommand))
            {
                _logger.LogInformation(
                    "Running per-sub-phase build verification after coding sub-phase {SubPhase}",
                    codingPhaseNumber);

                var buildResult = await RunShellCommandAsync(_config.Verification.BuildCommand, ct);
                if (!buildResult.Success)
                {
                    _logger.LogWarning(
                        "Build failed after coding sub-phase {SubPhase}: {Output}",
                        codingPhaseNumber, Truncate(buildResult.Output, 500));

                    // Trigger repair for this sub-phase
                    var repairResult = await RunRepairPhaseAsync(
                        planPath, "build", buildResult.Output, ct);
                    totalDuration += repairResult.Duration;

                    if (!repairResult.Success)
                    {
                        return new PhaseResult
                        {
                            Success = false,
                            ErrorMessage = $"Build repair failed after coding sub-phase {codingPhaseNumber}: " +
                                $"{repairResult.ErrorMessage}",
                            Duration = totalDuration
                        };
                    }

                    _logger.LogInformation(
                        "Build repair succeeded after coding sub-phase {SubPhase}",
                        codingPhaseNumber);
                }
                else
                {
                    _logger.LogInformation(
                        "Per-sub-phase build verification passed for coding sub-phase {SubPhase}",
                        codingPhaseNumber);
                }
            }

            // Git commit after successful coding sub-phase (when configured)
            if (_gitCommitManager != null && _config.Git.CommitPerPhase)
            {
                try
                {
                    var phaseName = nextPhase.Name ?? $"Phase {nextPhase.Number}";
                    var committed = await _gitCommitManager.CommitPhaseAsync(
                        nextPhase.Number, phaseName, $"Coding sub-phase {codingPhaseNumber} complete", ct);
                    if (committed)
                    {
                        _logger.LogInformation(
                            "Git commit created for coding sub-phase {SubPhase} (plan phase {PlanPhase})",
                            codingPhaseNumber, nextPhase.Number);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex,
                        "Git commit failed for coding sub-phase {SubPhase} — continuing",
                        codingPhaseNumber);
                }
            }

            // Small delay between sub-phases to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(2), ct);
        }

        // Post-coding verification: build and test if configured
        if (_config.Verification.Enabled)
        {
            var verificationResult = await RunPostCodingVerificationAsync(planPath, totalDuration, ct);
            if (verificationResult != null)
                return verificationResult;
        }

        return new PhaseResult
        {
            Success = true,
            Duration = totalDuration
        };
    }

    /// <summary>
    /// Runs build and test verification after coding phases complete.
    /// Returns null if verification passes, or a failed PhaseResult if it fails after retries.
    /// </summary>
    protected virtual async Task<PhaseResult?> RunPostCodingVerificationAsync(
        string planPath, TimeSpan accumulatedDuration, CancellationToken ct)
    {
        var maxRetries = _config.Verification.MaxVerificationRetries;

        for (var attempt = 0; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            // Run build command
            if (!string.IsNullOrEmpty(_config.Verification.BuildCommand))
            {
                _logger.LogInformation("Running build verification (attempt {Attempt}/{Max}): {Command}",
                    attempt + 1, maxRetries + 1, _config.Verification.BuildCommand);

                var buildResult = await RunShellCommandAsync(_config.Verification.BuildCommand, ct);
                if (!buildResult.Success)
                {
                    _logger.LogWarning("Build failed (attempt {Attempt}): {Output}", attempt + 1, buildResult.Output);

                    if (attempt < maxRetries)
                    {
                        // Feed errors back to coding agent for repair
                        var fixResult = await RunRepairPhaseAsync(
                            planPath, "build", buildResult.Output, ct);
                        accumulatedDuration += fixResult.Duration;
                        if (!fixResult.Success)
                        {
                            return new PhaseResult
                            {
                                Success = false,
                                ErrorMessage = $"Build repair failed after attempt {attempt + 1}: {fixResult.ErrorMessage}",
                                Duration = accumulatedDuration
                            };
                        }
                        continue;
                    }

                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = $"Build verification failed after {maxRetries + 1} attempts. Last output: {Truncate(buildResult.Output, 500)}",
                        Duration = accumulatedDuration
                    };
                }

                _logger.LogInformation("Build verification passed");
            }

            // Run test command
            if (!string.IsNullOrEmpty(_config.Verification.TestCommand))
            {
                _logger.LogInformation("Running test verification (attempt {Attempt}/{Max}): {Command}",
                    attempt + 1, maxRetries + 1, _config.Verification.TestCommand);

                var testResult = await RunShellCommandAsync(_config.Verification.TestCommand, ct);
                if (!testResult.Success)
                {
                    _logger.LogWarning("Tests failed (attempt {Attempt}): {Output}", attempt + 1, testResult.Output);

                    if (attempt < maxRetries)
                    {
                        var fixResult = await RunRepairPhaseAsync(
                            planPath, "test", testResult.Output, ct);
                        accumulatedDuration += fixResult.Duration;
                        if (!fixResult.Success)
                        {
                            return new PhaseResult
                            {
                                Success = false,
                                ErrorMessage = $"Test repair failed after attempt {attempt + 1}: {fixResult.ErrorMessage}",
                                Duration = accumulatedDuration
                            };
                        }
                        continue;
                    }

                    return new PhaseResult
                    {
                        Success = false,
                        ErrorMessage = $"Test verification failed after {maxRetries + 1} attempts. Last output: {Truncate(testResult.Output, 500)}",
                        Duration = accumulatedDuration
                    };
                }

                _logger.LogInformation("Test verification passed");
            }

            // Both build and test passed
            return null;
        }

        return null;
    }

    /// <summary>
    /// Runs a repair coding phase to fix build or test failures.
    /// </summary>
    protected virtual async Task<PhaseResult> RunRepairPhaseAsync(
        string planPath, string failureType, string errorOutput, CancellationToken ct)
    {
        var prompt = $"""
            The previous coding phase completed but {failureType} verification FAILED.

            Fix the {failureType} errors shown below. Do NOT proceed to new features —
            only fix the errors that caused the {failureType} failure.

            Error output:
            ```
            {Truncate(errorOutput, 3000)}
            ```

            The implementation plan is attached for context.
            """;

        _logger.LogInformation("Running {FailureType} repair phase", failureType);

        return await _phaseExecutor.ExecuteAsync(
            LoopPhase.Coding,
            prompt,
            new[] { planPath });
    }

    /// <summary>
    /// Runs a shell command and returns success status and output.
    /// </summary>
    protected virtual async Task<(bool Success, string Output)> RunShellCommandAsync(
        string command, CancellationToken ct)
    {
        try
        {
            var isWindows = OperatingSystem.IsWindows();
            var psi = new ProcessStartInfo
            {
                FileName = isWindows ? "cmd.exe" : "/bin/sh",
                Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, "Failed to start process");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var stdout = await outputTask;
            var stderr = await errorTask;
            var combinedOutput = string.IsNullOrEmpty(stderr) ? stdout : $"{stdout}\n{stderr}";

            return (process.ExitCode == 0, combinedOutput);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to run verification command: {Command}", command);
            return (false, $"Command execution failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Truncates a string to the specified maximum length.
    /// </summary>
    private static string Truncate(string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;
        return value[..maxLength] + "... (truncated)";
    }

    /// <summary>
    /// Computes the expected output document path for a given phase.
    /// Used to tell agents where to create their output documents.
    /// </summary>
    private string ComputeExpectedOutputPath(LoopPhase phase)
    {
        var runId = _stateManager.CurrentState?.RunId.ToString()[..8] ?? "unknown";
        return phase switch
        {
            LoopPhase.Vision => Path.Combine(_config.DocsPath, "vision", $"{runId}-vision.md"),
            LoopPhase.Research => Path.Combine(_config.DocsPath, "research", $"{runId}-research.md"),
            LoopPhase.Planning => Path.Combine(_config.DocsPath, "plans", $"{runId}-plan.md"),
            _ => Path.Combine(_config.DocsPath, $"{runId}-output.md")
        };
    }

    /// <summary>
    /// Builds the appropriate prompt for a phase based on its type.
    /// When a release context is available, prompts include release-specific scope.
    /// </summary>
    private string BuildPromptForPhase(LoopPhase phase, LoopState state)
    {
        // When running as part of a multi-release solution, use release-aware prompts
        if (_releaseContext != null)
        {
            return BuildReleaseAwarePrompt(phase, state);
        }

        var expectedPath = ComputeExpectedOutputPath(phase);

        return phase switch
        {
            LoopPhase.Vision => $"""
                Implement the following feature request:

                {state.FeatureRequest}

                Please create a vision document that captures the requirements and goals.
                Create the document at: {expectedPath}
                """,

            LoopPhase.Research => $"""
                You are running inside the Hugh CLI — a self-operating development loop.
                Research the technical approach for implementing this feature.
                The vision document is attached for context.
                Create a research outline document with phased research topics.
                Do not execute any research phases yet — create only the outline.
                Create the document at: {expectedPath}
                """,

            LoopPhase.Planning => $"""
                You are running inside the Hugh CLI — a self-operating development loop.
                Hugh orchestrates the full pipeline: Vision → Research → Planning → Review → Coding.
                Create a detailed implementation plan for this feature.
                The vision and research documents are attached for context.
                Make reasonable implementation decisions based on the research findings.
                Document your rationale for each decision in the plan's Planning Session Log.
                Do not ask clarifying questions — decide autonomously based on available context.
                Create the document at: {expectedPath}
                """,

            LoopPhase.Review => """
                Review the implementation plan for completeness and quality.
                The vision, research, and plan documents are attached for context.
                Please approve the plan or provide specific feedback for improvements.
                """,

            LoopPhase.Coding => throw new InvalidOperationException(
                "Coding phase prompt should be built with BuildCodingPhasePrompt"),

            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
    }

    /// <summary>
    /// Builds release-contextualized prompts for per-release execution.
    /// </summary>
    private string BuildReleaseAwarePrompt(LoopPhase phase, LoopState state)
    {
        var rc = _releaseContext!;
        var expectedPath = ComputeExpectedOutputPath(phase);
        var completedInfo = rc.CompletedReleases.Count > 0
            ? $"Previously completed releases:\n{string.Join("\n", rc.CompletedReleases.Select(r => $"  - {r}"))}"
            : "This is the first release (MVP).";

        return phase switch
        {
            LoopPhase.Vision => $"""
                You are running inside the Hugh CLI — a self-operating development loop.
                Hugh orchestrates the full pipeline: Vision → Research → Planning → Review → Coding.
                You do NOT need to tell the user to invoke other agents (like @pch-planner or @pch-coder).
                Hugh will automatically transition to the next phase when you are done.

                When the user indicates they are satisfied or want to move on (e.g., empty response,
                "continue", "go to planning", "done", "that's it", or similar), treat that as a signal
                to finalize the vision document with reasonable defaults for any remaining sections.
                Do NOT tell the user to invoke another agent — just complete your work.

                When you present options (A/B/C), the user's answer will be typed in a terminal.
                Accept short answers like "A", "1", option labels, or brief descriptions as valid choices.

                Feature request:
                {state.FeatureRequest}

                Please create a vision document that captures the requirements and goals.
                Create the document at: {expectedPath}
                """,

            LoopPhase.Research => $"""
                You are running inside the Hugh CLI — a self-operating development loop.
                Research the technical approach for implementing Release {rc.ReleaseNumber}: {rc.ReleaseName}.
                Release Scope: {rc.ReleaseScope}
                {completedInfo}
                The clarified solution vision is attached for context.
                Create a research outline document focused on this release's scope.
                Do not execute any research phases yet — create only the outline.
                Create the document at: {expectedPath}
                """,

            LoopPhase.Planning => $"""
                You are running inside the Hugh CLI — a self-operating development loop.
                Create a detailed implementation plan for Release {rc.ReleaseNumber}: {rc.ReleaseName}.
                Release Scope: {rc.ReleaseScope}
                {completedInfo}
                The vision and research documents are attached for context.
                Make reasonable implementation decisions based on the research findings.
                Document your rationale in the plan's Planning Session Log.
                Plan ONLY the work for this release. Do not plan items from other releases.
                Create the document at: {expectedPath}
                """,

            LoopPhase.Review => $"""
                Review the implementation plan for Release {rc.ReleaseNumber}: {rc.ReleaseName}.

                This plan should cover ONLY the following scope:
                {rc.ReleaseScope}

                The vision, research, and plan documents are attached for context.
                Verify the plan is complete for this release's scope and does not bleed
                into other releases. Approve the plan or provide specific feedback.
                """,

            LoopPhase.Coding => throw new InvalidOperationException(
                "Coding phase prompt should be built with BuildCodingPhasePrompt"),

            _ => throw new ArgumentOutOfRangeException(nameof(phase))
        };
    }

    /// <summary>
    /// Builds the prompt for a specific coding sub-phase.
    /// </summary>
    private static string BuildCodingPhasePrompt(PlanPhase planPhase)
    {
        return $"""
            Continue implementing the plan — start Phase {planPhase.Number}: {planPhase.Name}

            Please execute ONLY Phase {planPhase.Number}. Do not proceed to subsequent phases.
            The implementation plan is attached for context.
            """;
    }

    /// <summary>
    /// Builds the prompt for a specific task within a coding phase.
    /// Instructs the agent to execute only the specified task and update its status.
    /// </summary>
    private static string BuildCodingTaskPrompt(PlanPhase phase, PlanTask task)
    {
        return $"""
            Continue implementing the plan — execute Task {task.StepId} in Phase {phase.Number}: {phase.Name}

            **Your Assignment:** Execute ONLY Task {task.StepId}: {task.Description}

            **Task Details:**
            - **Step ID:** {task.StepId}
            - **Description:** {task.Description}
            - **Files:** {task.Files}
            - **Acceptance Criteria:** {task.AcceptanceCriteria}

            **Instructions:**
            1. Read the full plan document for context on Technical Design, patterns, and architecture
            2. Locate Task {task.StepId} in Phase {phase.Number}'s task table
            3. Execute ONLY this task — follow its description, files, and acceptance criteria
            4. After completing the task, update the task row in the plan document by changing its status from ⏳ to ✅
            5. Do NOT execute other tasks — complete only Task {task.StepId}

            The implementation plan is attached for context.
            """;
    }

    /// <summary>
    /// Executes all tasks within a single plan phase using task-level granularity.
    /// Each task gets its own isolated session for better context management.
    /// </summary>
    private async Task<PhaseResult> ExecuteTasksInPhaseAsync(
        string planPath, PlanPhase phase, int codingPhaseNumber, CancellationToken ct)
    {
        _logger.LogInformation(
            "Executing coding sub-phase {SubPhase} at task level: Phase {PlanPhase} - {PhaseName}",
            codingPhaseNumber, phase.Number, phase.Name);

        var taskDuration = TimeSpan.Zero;
        var taskCount = 0;

        while (!ct.IsCancellationRequested)
        {
            // Re-read the plan each iteration to get updated task statuses
            if (!File.Exists(planPath))
            {
                _logger.LogError("Plan document no longer exists at {PlanPath}", planPath);
                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = $"Plan document was deleted during task execution: {planPath}",
                    Duration = taskDuration
                };
            }

            var planContent = await File.ReadAllTextAsync(planPath, ct);

            // Find the next incomplete task within this phase
            var nextTask = _taskParser.FindNextIncompleteTask(planContent, phase.Number);
            if (nextTask == null)
            {
                if (taskCount == 0)
                {
                    _logger.LogWarning(
                        "Phase {PlanPhase} ({PhaseName}) has 0 parseable tasks. " +
                        "Possible format mismatch in plan document. " +
                        "Expected header format: '### Phase {PlanPhase}: Name' with task rows '| N.M | ... |'",
                        phase.Number, phase.Name, phase.Number);
                }
                else
                {
                    _logger.LogInformation(
                        "All tasks in Phase {PlanPhase} complete ({TaskCount} tasks executed)",
                        phase.Number, taskCount);
                }
                break;
            }

            taskCount++;
            _logger.LogInformation(
                "Executing task {StepId}: {Description} (task {TaskCount} in phase {PlanPhase})",
                nextTask.StepId, nextTask.Description, taskCount, phase.Number);

            // Build task-specific prompt
            var prompt = BuildCodingTaskPrompt(phase, nextTask);

            // Execute the task in its own session
            var result = await _phaseExecutor.ExecuteAsync(
                LoopPhase.Coding,
                prompt,
                new[] { planPath });

            taskDuration += result.Duration;

            if (!result.Success)
            {
                _logger.LogError(
                    "Task {StepId} failed: {Error}",
                    nextTask.StepId, result.ErrorMessage);

                return new PhaseResult
                {
                    Success = false,
                    ErrorMessage = $"Task {nextTask.StepId} in Phase {phase.Number} failed: {result.ErrorMessage}",
                    Duration = taskDuration
                };
            }

            _logger.LogInformation(
                "Task {StepId} completed in {Duration}",
                nextTask.StepId, result.Duration);

            // Optional per-task build verification
            if (_config.Coding.VerifyBuildPerTask
                && _config.Verification.Enabled
                && !string.IsNullOrEmpty(_config.Verification.BuildCommand))
            {
                _logger.LogInformation(
                    "Running per-task build verification after task {StepId}",
                    nextTask.StepId);

                var buildResult = await RunShellCommandAsync(
                    _config.Verification.BuildCommand, ct);

                if (!buildResult.Success)
                {
                    _logger.LogWarning(
                        "Build failed after task {StepId}: {Output}",
                        nextTask.StepId, Truncate(buildResult.Output, 500));

                    // Trigger repair for this task
                    var repairResult = await RunRepairPhaseAsync(
                        planPath, "build", buildResult.Output, ct);
                    taskDuration += repairResult.Duration;

                    if (!repairResult.Success)
                    {
                        return new PhaseResult
                        {
                            Success = false,
                            ErrorMessage = $"Build repair failed after task {nextTask.StepId}: " +
                                $"{repairResult.ErrorMessage}",
                            Duration = taskDuration
                        };
                    }

                    _logger.LogInformation(
                        "Build repair succeeded after task {StepId}",
                        nextTask.StepId);
                }
                else
                {
                    _logger.LogInformation(
                        "Per-task build verification passed for task {StepId}",
                        nextTask.StepId);
                }
            }

            // Small delay between tasks to avoid rate limiting
            await Task.Delay(TimeSpan.FromSeconds(1), ct);
        }

        return new PhaseResult
        {
            Success = true,
            Duration = taskDuration
        };
    }

    /// <summary>
    /// Gets the documents to attach for a specific phase.
    /// When a release context is available, also attaches the release roadmap.
    /// </summary>
    private string[] GetDocumentsForPhase(LoopPhase phase, DocumentPaths documents)
    {
        var baseDocs = phase switch
        {
            // Vision starts fresh with no documents
            LoopPhase.Vision => Array.Empty<string>(),

            // Research needs the vision document
            LoopPhase.Research => GetNonNullDocuments(documents.Vision),

            // Planning needs vision and research
            LoopPhase.Planning => GetNonNullDocuments(documents.Vision, documents.Research),

            // Review needs vision, research, and plan
            LoopPhase.Review => GetNonNullDocuments(documents.Vision, documents.Research, documents.Plan),

            // Coding only needs the plan (vision/research are embedded)
            LoopPhase.Coding => GetNonNullDocuments(documents.Plan),

            _ => Array.Empty<string>()
        };

        // When running in release context, attach the roadmap for research and planning
        if (_releaseContext != null &&
            phase is LoopPhase.Research or LoopPhase.Planning &&
            !string.IsNullOrEmpty(_releaseContext.RoadmapPath))
        {
            var docs = baseDocs.Append(_releaseContext.RoadmapPath);

            // Attach release summaries from completed releases for inter-release learning
            if (_releaseContext.CompletedReleaseSummaries.Count > 0)
            {
                docs = docs.Concat(
                    _releaseContext.CompletedReleaseSummaries
                        .Where(p => !string.IsNullOrEmpty(p) && File.Exists(p)));
            }

            return docs.ToArray();
        }

        return baseDocs;
    }

    /// <summary>
    /// Filters null or empty strings from a params array.
    /// </summary>
    private static string[] GetNonNullDocuments(params string?[] documents)
    {
        return documents.Where(d => !string.IsNullOrEmpty(d)).ToArray()!;
    }

    /// <summary>
    /// Gets the index of a phase in the execution order.
    /// </summary>
    private static int GetPhaseIndex(LoopPhase phase)
    {
        for (var i = 0; i < PhaseOrder.Length; i++)
        {
            if (PhaseOrder[i] == phase)
                return i;
        }
        return 0;
    }

    /// <summary>
    /// Determines whether a phase is expected to produce an output document.
    /// </summary>
    private static bool PhaseProducesDocument(LoopPhase phase)
    {
        return phase is LoopPhase.Vision or LoopPhase.Research or LoopPhase.Planning;
    }

    /// <summary>
    /// Ensures the expected output directory exists for phases that produce documents.
    /// Defense-in-depth alongside FileSystemHooks.
    /// </summary>
    private void EnsurePhaseOutputDirectoryExists(LoopPhase phase)
    {
        var subdirectory = phase switch
        {
            LoopPhase.Vision => "vision",
            LoopPhase.Research => "research",
            LoopPhase.Planning => "plans",
            _ => null
        };

        if (subdirectory == null)
            return;

        var directoryPath = Path.Combine(_config.DocsPath, subdirectory);
        try
        {
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
                _logger.LogDebug("Pre-created output directory: {DirectoryPath}", directoryPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to pre-create output directory: {DirectoryPath}", directoryPath);
        }
    }

    /// <summary>
    /// Gets the plan document path from the current state, if available.
    /// Used by SolutionOrchestrator for release summary generation.
    /// </summary>
    public string? GetPlanDocumentPath()
    {
        return _stateManager.CurrentState?.Documents.Plan;
    }

    /// <summary>
    /// Gets the research document path from the current state, if available.
    /// Used by SolutionOrchestrator for release summary generation.
    /// </summary>
    public string? GetResearchDocumentPath()
    {
        return _stateManager.CurrentState?.Documents.Research;
    }

    /// <summary>
    /// Disposes the PhaseExecutor to release SDK resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await _phaseExecutor.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
