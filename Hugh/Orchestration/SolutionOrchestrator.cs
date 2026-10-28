using System.Text.RegularExpressions;
using GitHub.Copilot.SDK;
using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Logging;
using Hugh.Models;
using Hugh.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Serilog;
using ILogger = Serilog.ILogger;

namespace Hugh.Orchestration;

/// <summary>
/// Top-level orchestrator for the automated solution developer.
/// Handles triage (single-feature vs multi-release), clarification,
/// decomposition into releases, and per-release execution.
/// </summary>
public partial class SolutionOrchestrator : IAsyncDisposable
{
    private readonly CopilotService _copilotService;
    private readonly LoopOrchestrator _loopOrchestrator;
    private readonly SolutionStateManager _solutionStateManager;
    private readonly ReleaseRoadmapParser _roadmapParser;
    private readonly ReleaseSummaryGenerator? _releaseSummaryGenerator;
    private readonly GitCommitManager? _gitCommitManager;
    private readonly HughConfig _config;
    private readonly ILogger _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly CancellationTokenSource _globalCts;

    // Regex for parsing triage classification from agent response
    [GeneratedRegex(
        @"\*\*Classification:\*\*\s*(FEATURE|SOLUTION)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex TriageClassificationRegex();

    public SolutionOrchestrator(
        CopilotService copilotService,
        LoopOrchestrator loopOrchestrator,
        SolutionStateManager solutionStateManager,
        ReleaseRoadmapParser roadmapParser,
        HughConfig config,
        ILogger logger,
        ILoggerFactory loggerFactory,
        CancellationTokenSource globalCts,
        ReleaseSummaryGenerator? releaseSummaryGenerator = null,
        GitCommitManager? gitCommitManager = null)
    {
        _copilotService = copilotService;
        _loopOrchestrator = loopOrchestrator;
        _solutionStateManager = solutionStateManager;
        _roadmapParser = roadmapParser;
        _releaseSummaryGenerator = releaseSummaryGenerator;
        _gitCommitManager = gitCommitManager;
        _config = config;
        _logger = logger;
        _loggerFactory = loggerFactory;
        _globalCts = globalCts;
    }

    /// <summary>
    /// Runs the solution developer workflow. Triages the input to determine
    /// if it's a single feature (delegates to LoopOrchestrator) or a multi-release
    /// solution (runs clarification → decomposition → per-release execution).
    /// </summary>
    public async Task<bool> RunAsync(string userRequest, CancellationToken ct = default)
    {
        _logger.Information("Starting solution developer for request: {Request}", userRequest);

        // Create solution state
        var state = await _solutionStateManager.CreateSolutionAsync(userRequest, ct);

        // Phase 1: Triage
        await _solutionStateManager.UpdatePhaseAsync(SolutionPhase.Triage, PhaseStatus.InProgress, ct);
        var mode = await RunTriageAsync(userRequest, ct);

        await _solutionStateManager.SetModeAsync(mode, ct);

        if (mode == SolutionMode.SingleFeature)
        {
            _logger.Information("Triage classified as single feature — delegating to LoopOrchestrator");
            ConsoleOutput.WriteInfo("Classified as single feature request. Starting development loop...");
            return await _loopOrchestrator.RunAsync(userRequest, ct);
        }

        _logger.Information("Triage classified as multi-release solution — starting solution workflow");
        ConsoleOutput.WriteInfo("Classified as multi-release solution. Starting clarification...");

        return await ExecuteSolutionAsync(state, userRequest, ct);
    }

    /// <summary>
    /// Resumes a paused or failed solution run.
    /// </summary>
    public async Task<bool> ResumeAsync(Guid? solutionId = null, CancellationToken ct = default)
    {
        SolutionState? state;
        if (solutionId.HasValue)
        {
            state = await _solutionStateManager.LoadSolutionAsync(solutionId.Value, ct);
        }
        else
        {
            state = await _solutionStateManager.LoadSolutionAsync(ct);
        }

        if (state == null)
        {
            _logger.Error("No solution state found to resume");
            return false;
        }

        if (state.Status == PhaseStatus.Complete)
        {
            _logger.Warning("Solution {SolutionId} is already complete", state.SolutionId);
            return true;
        }

        // If it was classified as single-feature, delegate to loop orchestrator
        if (state.Mode == SolutionMode.SingleFeature)
        {
            return await _loopOrchestrator.ResumeAsync(ct: ct);
        }

        _logger.Information(
            "Resuming solution {SolutionId} from phase {Phase}",
            state.SolutionId, state.CurrentPhase);

        return await ExecuteSolutionAsync(state, state.OriginalRequest, ct);
    }

    /// <summary>
    /// Gets the current status of a solution.
    /// </summary>
    public async Task<SolutionState?> GetStatusAsync(Guid? solutionId = null, CancellationToken ct = default)
    {
        if (solutionId.HasValue)
        {
            return await _solutionStateManager.LoadSolutionAsync(solutionId.Value, ct);
        }
        return await _solutionStateManager.LoadSolutionAsync(ct);
    }

    /// <summary>
    /// Checks if a solution state exists (vs a simple loop state).
    /// </summary>
    public bool HasExistingSolution()
    {
        return _solutionStateManager.HasExistingSolution();
    }

    /// <summary>
    /// Executes the multi-release solution workflow from the current phase.
    /// </summary>
    private async Task<bool> ExecuteSolutionAsync(
        SolutionState state,
        string userRequest,
        CancellationToken ct)
    {
        // Phase 2: Clarification (skip if already completed)
        if (state.CurrentPhase <= SolutionPhase.Clarification &&
            string.IsNullOrEmpty(state.Documents.ClarifiedVision))
        {
            await _solutionStateManager.UpdatePhaseAsync(
                SolutionPhase.Clarification, PhaseStatus.InProgress, ct);

            var visionPath = await RunClarificationAsync(userRequest, state.SolutionId, ct);
            if (visionPath == null)
            {
                await _solutionStateManager.MarkFailedAsync("Clarification phase failed", ct);
                return false;
            }

            await _solutionStateManager.UpdateDocumentsAsync(
                state.Documents with { ClarifiedVision = visionPath }, ct);

            state = _solutionStateManager.CurrentState!;
            _logger.Information("Clarification complete: {Path}", visionPath);
        }

        // Phase 3: Decomposition (skip if already completed)
        if (state.CurrentPhase <= SolutionPhase.Decomposition &&
            string.IsNullOrEmpty(state.Documents.Roadmap))
        {
            await _solutionStateManager.UpdatePhaseAsync(
                SolutionPhase.Decomposition, PhaseStatus.InProgress, ct);

            var roadmapPath = await RunDecompositionAsync(
                state.Documents.ClarifiedVision!, state.SolutionId, ct);
            if (roadmapPath == null)
            {
                await _solutionStateManager.MarkFailedAsync("Decomposition phase failed", ct);
                return false;
            }

            await _solutionStateManager.UpdateDocumentsAsync(
                state.Documents with { Roadmap = roadmapPath }, ct);

            // Parse the roadmap and populate releases
            var roadmapContent = await File.ReadAllTextAsync(roadmapPath, ct);
            var parsedReleases = _roadmapParser.ParseReleases(roadmapContent);

            if (parsedReleases.Count == 0)
            {
                _logger.Error("No releases found in roadmap document");
                await _solutionStateManager.MarkFailedAsync(
                    "Decomposition produced no parseable releases", ct);
                return false;
            }

            var releaseDefinitions = parsedReleases.Select(r => new ReleaseDefinition
            {
                ReleaseNumber = r.Number,
                Name = r.Name,
                Description = r.Goal,
                Scope = string.Join("\n", r.Scope.Select(s => $"- {s}")),
                RunId = Guid.NewGuid(),
                Status = ReleaseStatus.Pending
            }).ToList();

            await _solutionStateManager.SetReleasesAsync(releaseDefinitions, ct);
            state = _solutionStateManager.CurrentState!;

            ConsoleOutput.WriteSection($"Release Roadmap: {parsedReleases.Count} releases");
            foreach (var release in parsedReleases)
            {
                Console.WriteLine($"  Release {release.Number}: {release.Name}");
            }
            Console.WriteLine();

            _logger.Information("Decomposition complete: {Count} releases", parsedReleases.Count);
        }

        // Phase 4: Execute releases
        await _solutionStateManager.UpdatePhaseAsync(
            SolutionPhase.Execution, PhaseStatus.InProgress, ct);

        return await ExecuteReleasesAsync(state, ct);
    }

    /// <summary>
    /// Runs the triage agent to classify the input.
    /// </summary>
    private async Task<SolutionMode> RunTriageAsync(string userRequest, CancellationToken ct)
    {
        _logger.Information("Running triage classification...");

        try
        {
            await _copilotService.EnsureClientStartedAsync(ct);

            var agentDef = new AgentDefinition
            {
                Name = "hugh-triager",
                BuiltInPromptName = "triage",
                Phase = LoopPhase.Vision, // Unused, but required by AgentDefinition
                DefaultTimeout = _config.Solution.TriageTimeout
            };

            var agentPrompt = BuiltInPromptLoader.Load("triage");

            await using var session = await _copilotService.CreateSessionAsync(agentDef, agentPrompt, ct);
            var responseProcessor = _copilotService.CreateResponseProcessor();

            var prompt = $"Classify this request:\n\n{userRequest}";
            await _copilotService.SendWithRetryAsync(session, prompt, ct);

            var response = await responseProcessor.ProcessResponseAsync(session, ct);

            // Parse classification from response
            var match = TriageClassificationRegex().Match(response.Content);
            if (match.Success)
            {
                var classification = match.Groups[1].Value.ToUpperInvariant();
                _logger.Information("Triage classification: {Classification}", classification);
                return classification == "SOLUTION" ? SolutionMode.MultiRelease : SolutionMode.SingleFeature;
            }

            _logger.Warning("Could not parse triage classification, defaulting to single feature");
            return SolutionMode.SingleFeature;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Triage failed, defaulting to single feature");
            return SolutionMode.SingleFeature;
        }
    }

    /// <summary>
    /// Runs the clarification agent in a multi-turn conversation loop.
    /// Returns the path to the clarified vision document on success, null on failure.
    /// </summary>
    private async Task<string?> RunClarificationAsync(
        string userRequest,
        Guid solutionId,
        CancellationToken ct)
    {
        _logger.Information("Starting clarification phase...");
        ConsoleOutput.WriteSection("Solution Clarification");
        Console.WriteLine("  Hugh will ask questions to refine your vision. Type your answers below.");
        Console.WriteLine();

        try
        {
            await _copilotService.EnsureClientStartedAsync(ct);

            var agentDef = new AgentDefinition
            {
                Name = "hugh-clarifier",
                BuiltInPromptName = "clarification",
                Phase = LoopPhase.Vision,
                DefaultTimeout = _config.Solution.ClarificationTimeout
            };

            var agentPrompt = BuiltInPromptLoader.Load("clarification");
            var visionId = solutionId.ToString()[..8];
            var visionPath = Path.Combine(_config.DocsPath, "vision", $"{visionId}-clarified-vision.md");

            // Ensure vision directory exists
            var visionDir = Path.GetDirectoryName(visionPath)!;
            if (!Directory.Exists(visionDir))
            {
                Directory.CreateDirectory(visionDir);
            }

            await using var session = await _copilotService.CreateSessionAsync(agentDef, agentPrompt, ct);
            var responseProcessor = _copilotService.CreateResponseProcessor();

            // Send initial request with output path instruction
            var initialPrompt = $"""
                The user wants to build the following:

                {userRequest}

                Please create the clarified vision document at: {visionPath}

                Begin by asking your first clarifying question.
                """;

            await _copilotService.SendWithRetryAsync(session, initialPrompt, ct);

            var maxTurns = _config.Solution.MaxClarificationTurns;
            for (var turn = 0; turn < maxTurns; turn++)
            {
                ct.ThrowIfCancellationRequested();

                var response = await responseProcessor.ProcessResponseAsync(session, ct);

                if (response.Error != null)
                {
                    _logger.Error("Clarification error: {Error}", response.Error.Message);
                    return null;
                }

                // Check if clarification is complete
                if (response.Signal.Type == SignalType.WorkflowComplete ||
                    response.Signal.Type == SignalType.PhaseComplete)
                {
                    _logger.Information("Clarification complete after {Turns} turns", turn + 1);

                    // Check if vision document was created
                    var outputPath = response.OutputDocumentPath ?? visionPath;
                    if (File.Exists(outputPath))
                    {
                        return outputPath;
                    }
                    if (File.Exists(visionPath))
                    {
                        return visionPath;
                    }

                    _logger.Warning("Clarification signaled complete but no vision document found");
                    return null;
                }

                // Display agent's question/response to the user
                if (!string.IsNullOrWhiteSpace(response.Content))
                {
                    Console.WriteLine();
                    Console.WriteLine(response.Content.Trim());
                    Console.WriteLine();
                }

                // Check if agent needs user input
                if (SdkResponseProcessor.IsPauseSignal(response.Signal.Type) ||
                    !string.IsNullOrWhiteSpace(response.Content))
                {
                    // Read user's answer
                    Console.Write("  Your answer: ");
                    var userAnswer = Console.ReadLine()?.Trim();

                    if (string.IsNullOrEmpty(userAnswer))
                    {
                        _logger.Information("User provided empty answer, signaling to proceed");
                        userAnswer = "That's enough information. Please proceed to create the vision document.";
                    }

                    // Send answer back to the session
                    await _copilotService.SendWithRetryAsync(session, userAnswer, ct);
                }
            }

            _logger.Warning("Clarification hit max turns ({MaxTurns}), checking for output", maxTurns);

            // Check if vision document was created despite hitting max turns
            if (File.Exists(visionPath))
            {
                return visionPath;
            }

            return null;
        }
        catch (OperationCanceledException)
        {
            _logger.Warning("Clarification cancelled");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Clarification failed");
            return null;
        }
    }

    /// <summary>
    /// Runs the decomposition agent to produce a release roadmap.
    /// Returns the path to the roadmap document on success, null on failure.
    /// </summary>
    private async Task<string?> RunDecompositionAsync(
        string clarifiedVisionPath,
        Guid solutionId,
        CancellationToken ct)
    {
        _logger.Information("Starting decomposition phase...");
        ConsoleOutput.WriteSection("Release Decomposition");
        Console.WriteLine("  Breaking the vision into releases...");
        Console.WriteLine();

        try
        {
            await _copilotService.EnsureClientStartedAsync(ct);

            var agentDef = new AgentDefinition
            {
                Name = "hugh-decomposer",
                BuiltInPromptName = "decomposition",
                Phase = LoopPhase.Vision,
                DefaultTimeout = _config.Solution.DecompositionTimeout
            };

            var agentPrompt = BuiltInPromptLoader.Load("decomposition");
            var roadmapId = solutionId.ToString()[..8];
            var roadmapPath = Path.Combine(_config.DocsPath, "roadmap", $"{roadmapId}-roadmap.md");

            // Ensure roadmap directory exists
            var roadmapDir = Path.GetDirectoryName(roadmapPath)!;
            if (!Directory.Exists(roadmapDir))
            {
                Directory.CreateDirectory(roadmapDir);
            }

            await using var session = await _copilotService.CreateSessionAsync(agentDef, agentPrompt, ct);
            var responseProcessor = _copilotService.CreateResponseProcessor();

            var prompt = $"""
                Decompose the attached clarified vision into a release roadmap.
                Create the roadmap document at: {roadmapPath}
                """;
            var attachments = CopilotService.CreateAttachments(new[] { clarifiedVisionPath });

            await _copilotService.SendWithRetryAsync(session, prompt, ct, attachments);

            var response = await responseProcessor.ProcessResponseAsync(session, ct);

            if (response.Error != null)
            {
                _logger.Error("Decomposition error: {Error}", response.Error.Message);
                return null;
            }

            // Check for output document
            var outputPath = response.OutputDocumentPath ?? roadmapPath;
            if (File.Exists(outputPath))
            {
                return outputPath;
            }
            if (File.Exists(roadmapPath))
            {
                return roadmapPath;
            }

            _logger.Error("Decomposition completed but no roadmap document found");
            return null;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Decomposition failed");
            return null;
        }
    }

    /// <summary>
    /// Executes all releases in order. Each release runs Research → Planning → Review → Coding
    /// via the LoopOrchestrator.
    /// </summary>
    private async Task<bool> ExecuteReleasesAsync(SolutionState state, CancellationToken ct)
    {
        var releases = state.Releases;
        var startIndex = state.CurrentReleaseIndex;

        // Find the first non-complete release to start from
        for (var i = 0; i < releases.Count; i++)
        {
            if (releases[i].Status == ReleaseStatus.Complete)
            {
                startIndex = i + 1;
            }
            else
            {
                startIndex = i;
                break;
            }
        }

        var completedReleaseNames = new List<string>();
        var completedReleaseSummaries = new List<string>();
        for (var i = 0; i < startIndex && i < releases.Count; i++)
        {
            completedReleaseNames.Add($"Release {releases[i].ReleaseNumber}: {releases[i].Name}");
        }

        for (var i = startIndex; i < releases.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var release = releases[i];
            if (release.Status == ReleaseStatus.Complete)
            {
                _logger.Debug("Skipping completed release {Number}: {Name}", release.ReleaseNumber, release.Name);
                continue;
            }

            ConsoleOutput.WriteSection($"Release {release.ReleaseNumber}: {release.Name}");
            _logger.Information(
                "Starting release {Number}/{Total}: {Name}",
                release.ReleaseNumber, releases.Count, release.Name);

            // Record starting commit SHA for cumulative diff (used by release summary)
            string? startingCommitSha = null;
            if (_gitCommitManager != null)
            {
                startingCommitSha = await _gitCommitManager.GetCurrentCommitShaAsync(ct);
                _logger.Debug("Recorded starting commit SHA for Release {Number}: {Sha}",
                    release.ReleaseNumber, startingCommitSha);
            }

            await _solutionStateManager.UpdateReleaseStatusAsync(i, ReleaseStatus.InProgress, ct);

            var releaseContext = new ReleaseContext
            {
                ReleaseNumber = release.ReleaseNumber,
                ReleaseName = release.Name,
                ReleaseScope = release.Scope,
                SolutionVisionPath = state.Documents.ClarifiedVision!,
                RoadmapPath = state.Documents.Roadmap!,
                CompletedReleases = completedReleaseNames.ToList(),
                CompletedReleaseSummaries = completedReleaseSummaries.ToList()
            };

            // Create a per-release LoopOrchestrator
            var releaseOrchestrator = BuildReleaseOrchestrator(release.ReleaseNumber);

            try
            {
                var success = await releaseOrchestrator.RunForContextAsync(
                    $"Release {release.ReleaseNumber}: {release.Name}\n\n{release.Scope}",
                    LoopPhase.Research,
                    releaseContext,
                    ct);

                if (!success)
                {
                    _logger.Error("Release {Number} failed: {Name}", release.ReleaseNumber, release.Name);
                    await _solutionStateManager.UpdateReleaseStatusAsync(i, ReleaseStatus.Failed, ct);
                    await _solutionStateManager.MarkFailedAsync(
                        $"Release {release.ReleaseNumber} ({release.Name}) failed", ct);
                    return false;
                }

                await _solutionStateManager.UpdateReleaseStatusAsync(i, ReleaseStatus.Complete, ct);
                completedReleaseNames.Add($"Release {release.ReleaseNumber}: {release.Name}");

                // Generate release summary for inter-release learning (skip for final release)
                if (_releaseSummaryGenerator != null && i < releases.Count - 1)
                {
                    _logger.Information(
                        "Generating release summary for Release {Number}", release.ReleaseNumber);

                    var summaryPath = await _releaseSummaryGenerator.GenerateSummaryAsync(
                        release.ReleaseNumber,
                        release.Name,
                        releaseOrchestrator.GetPlanDocumentPath(),
                        releaseOrchestrator.GetResearchDocumentPath(),
                        startingCommitSha,
                        ct);

                    if (!string.IsNullOrEmpty(summaryPath))
                    {
                        completedReleaseSummaries.Add(summaryPath);
                        _logger.Information(
                            "Release summary saved at {Path}", summaryPath);
                    }
                }

                ConsoleOutput.WriteSuccess(
                    $"Release {release.ReleaseNumber}/{releases.Count} complete: {release.Name}");

                // Small delay between releases to avoid rate limiting
                if (i < releases.Count - 1)
                {
                    await Task.Delay(TimeSpan.FromSeconds(3), ct);
                }
            }
            finally
            {
                await releaseOrchestrator.DisposeAsync();
            }
        }

        await _solutionStateManager.MarkCompleteAsync(ct);
        ConsoleOutput.WriteSuccess("All releases complete! Solution fully implemented.");
        return true;
    }

    /// <summary>
    /// Builds a LoopOrchestrator configured for a specific release,
    /// with its own state directory under .orchestrator/releases/release-N/.
    /// </summary>
    private LoopOrchestrator BuildReleaseOrchestrator(int releaseNumber)
    {
        var configOptions = Options.Create(_config);

        var agentRegistry = new AgentRegistry();
        var planPhaseParser = new PlanPhaseParser();
        var researchPhaseParser = new ResearchPhaseParser();
        var planCreationPhaseParser = new PlanCreationPhaseParser();
        var taskParser = new TaskParser();

        // Create a per-release ProgressReporter scoped to the release
        var workspaceRoot = Directory.GetCurrentDirectory();
        var docsPath = Path.Combine(workspaceRoot, _config.DocsPath);
        var progressReporter = new ProgressReporter(
            docsPath,
            researchPhaseParser,
            planPhaseParser,
            _logger,
            $"release-{releaseNumber}-progress.md");

        // Create state manager with progress reporter
        var stateManager = _solutionStateManager.CreateReleaseStateManager(releaseNumber, progressReporter);

        var phaseExecutor = new PhaseExecutor(
            _copilotService,
            agentRegistry,
            stateManager,
            configOptions,
            _loggerFactory.CreateLogger<PhaseExecutor>(),
            _globalCts);

        return new LoopOrchestrator(
            phaseExecutor,
            stateManager,
            planPhaseParser,
            researchPhaseParser,
            planCreationPhaseParser,
            taskParser,
            configOptions,
            _loggerFactory.CreateLogger<LoopOrchestrator>());
    }

    public async ValueTask DisposeAsync()
    {
        await _loopOrchestrator.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
