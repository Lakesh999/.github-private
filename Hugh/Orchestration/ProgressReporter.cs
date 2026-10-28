using Hugh.Detection;
using Hugh.Models;
using Serilog;

namespace Hugh.Orchestration;

/// <summary>
/// Interface for progress reporting hooks called by LoopStateManager
/// after state transitions.
/// </summary>
public interface IProgressReporter
{
    /// <summary>
    /// Called when a phase's status is updated (e.g., started, sub-phase advanced).
    /// </summary>
    Task OnPhaseUpdatedAsync(LoopPhase phase, PhaseStatus status, int? subPhase);

    /// <summary>
    /// Called when a phase completes successfully.
    /// </summary>
    Task OnPhaseCompletedAsync(LoopPhase phase, string? outputDocPath);

    /// <summary>
    /// Called when a phase fails.
    /// </summary>
    Task OnPhaseFailedAsync(LoopPhase phase, string errorMessage);
}

/// <summary>
/// Generates and updates a markdown progress document after state transitions.
/// Renders release overview, per-phase status, sub-phase detail, timing, and decisions.
/// </summary>
public class ProgressReporter : IProgressReporter
{
    private readonly string _docsPath;
    private readonly string _progressFileName;
    private readonly ResearchPhaseParser _researchPhaseParser;
    private readonly PlanPhaseParser _planPhaseParser;
    private readonly ILogger _logger;

    private LoopState? _lastKnownState;

    /// <summary>
    /// Initializes a new instance of the ProgressReporter.
    /// </summary>
    /// <param name="docsPath">Root docs directory (e.g., "./docs").</param>
    /// <param name="researchPhaseParser">Parser for research sub-phase detail.</param>
    /// <param name="planPhaseParser">Parser for plan sub-phase detail.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="progressFileName">
    /// Override the progress file name (default: "progress.md").
    /// For multi-release, pass "{solutionId}-progress.md".
    /// </param>
    public ProgressReporter(
        string docsPath,
        ResearchPhaseParser researchPhaseParser,
        PlanPhaseParser planPhaseParser,
        ILogger? logger = null,
        string? progressFileName = null)
    {
        _docsPath = docsPath;
        _researchPhaseParser = researchPhaseParser;
        _planPhaseParser = planPhaseParser;
        _logger = logger ?? Log.Logger;
        _progressFileName = progressFileName ?? "progress.md";
    }

    /// <summary>
    /// Gets the full path to the progress document.
    /// </summary>
    public string ProgressDocPath => Path.Combine(_docsPath, "progress", _progressFileName);

    /// <inheritdoc />
    public async Task OnPhaseUpdatedAsync(LoopPhase phase, PhaseStatus status, int? subPhase)
    {
        _logger.Debug("Progress: Phase {Phase} updated to {Status} (sub-phase: {SubPhase})", phase, status, subPhase);
        // We don't have full state here, but we can note the update.
        // The actual document regeneration happens via GenerateProgressDocumentAsync
        // called with the full LoopState after SaveStateAsync.
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnPhaseCompletedAsync(LoopPhase phase, string? outputDocPath)
    {
        _logger.Information("Progress: Phase {Phase} completed. Output: {OutputDocPath}", phase, outputDocPath);
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task OnPhaseFailedAsync(LoopPhase phase, string errorMessage)
    {
        _logger.Warning("Progress: Phase {Phase} failed: {ErrorMessage}", phase, errorMessage);
        await Task.CompletedTask;
    }

    /// <summary>
    /// Generates (or updates) the progress document from the current loop state.
    /// Called by LoopStateManager after each SaveStateAsync.
    /// </summary>
    public async Task GenerateProgressDocumentAsync(LoopState state, CancellationToken ct = default)
    {
        _lastKnownState = state;

        try
        {
            EnsureProgressDirectory();

            var markdown = RenderProgressMarkdown(state);
            await File.WriteAllTextAsync(ProgressDocPath, markdown, ct);

            _logger.Debug("Progress document updated at {Path}", ProgressDocPath);
        }
        catch (Exception ex)
        {
            // Progress reporting should never break the main pipeline
            _logger.Warning(ex, "Failed to update progress document at {Path}", ProgressDocPath);
        }
    }

    /// <summary>
    /// Renders the full progress document as markdown.
    /// </summary>
    public string RenderProgressMarkdown(LoopState state)
    {
        var sb = new System.Text.StringBuilder();

        // Header
        sb.AppendLine("# Hugh Progress Report");
        sb.AppendLine();
        sb.AppendLine($"**Run ID:** `{state.RunId}`");
        sb.AppendLine($"**Feature:** {state.FeatureRequest}");
        sb.AppendLine($"**Started:** {state.StartedAt:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine($"**Current Phase:** {state.CurrentPhase}");
        sb.AppendLine($"**Status:** {FormatPhaseStatus(state.Status)}");
        if (state.CurrentSubPhase.HasValue)
        {
            sb.AppendLine($"**Current Sub-Phase:** {state.CurrentSubPhase.Value}");
        }
        sb.AppendLine();

        // Phase overview table
        sb.AppendLine("## Phase Overview");
        sb.AppendLine();
        sb.AppendLine("| Phase | Status | Document |");
        sb.AppendLine("|-------|--------|----------|");

        foreach (var phase in Enum.GetValues<LoopPhase>())
        {
            var status = GetPhaseDisplayStatus(state, phase);
            var doc = GetPhaseDocumentPath(state, phase);
            sb.AppendLine($"| {phase} | {status} | {doc} |");
        }
        sb.AppendLine();

        // Sub-phase detail sections
        var researchDetail = RenderResearchProgress(state.Documents.Research);
        if (!string.IsNullOrEmpty(researchDetail))
        {
            sb.AppendLine("## Research Sub-Phase Detail");
            sb.AppendLine();
            sb.Append(researchDetail);
            sb.AppendLine();
        }

        var planDetail = RenderPlanProgress(state.Documents.Plan);
        if (!string.IsNullOrEmpty(planDetail))
        {
            sb.AppendLine("## Plan Phase Detail");
            sb.AppendLine();
            sb.Append(planDetail);
            sb.AppendLine();
        }

        // Decisions
        if (state.Decisions.Count > 0)
        {
            sb.AppendLine("## Decisions");
            sb.AppendLine();
            sb.AppendLine("| # | Phase | Question | Selected | Rationale |");
            sb.AppendLine("|---|-------|----------|----------|-----------|" );

            for (var i = 0; i < state.Decisions.Count; i++)
            {
                var d = state.Decisions[i];
                sb.AppendLine($"| {i + 1} | {d.Phase} | {EscapeTableCell(d.Question)} | {EscapeTableCell(d.Selected)} | {EscapeTableCell(d.Rationale)} |");
            }
            sb.AppendLine();
        }

        // Failure info
        if (state.Status == PhaseStatus.Failed && !string.IsNullOrEmpty(state.FailureReason))
        {
            sb.AppendLine("## Failure Details");
            sb.AppendLine();
            sb.AppendLine($"**Phase:** {state.CurrentPhase}");
            sb.AppendLine($"**Reason:** {state.FailureReason}");
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine($"*Last updated: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC*");

        return sb.ToString();
    }

    /// <summary>
    /// Renders research sub-phase progress detail by parsing the research document.
    /// </summary>
    public string RenderResearchProgress(string? researchDocPath)
    {
        if (string.IsNullOrEmpty(researchDocPath) || !File.Exists(researchDocPath))
        {
            return string.Empty;
        }

        try
        {
            var content = File.ReadAllText(researchDocPath);
            var phases = _researchPhaseParser.ParsePhases(content);

            if (phases.Count == 0)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("| # | Phase | Status | Scope |");
            sb.AppendLine("|---|-------|--------|-------|");

            foreach (var phase in phases)
            {
                sb.AppendLine($"| {phase.Number} | {EscapeTableCell(phase.Name)} | {phase.StatusEmoji} | {EscapeTableCell(phase.Scope)} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse research document for progress: {Path}", researchDocPath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Renders plan sub-phase progress detail by parsing the plan document.
    /// </summary>
    public string RenderPlanProgress(string? planDocPath)
    {
        if (string.IsNullOrEmpty(planDocPath) || !File.Exists(planDocPath))
        {
            return string.Empty;
        }

        try
        {
            var content = File.ReadAllText(planDocPath);
            var phases = _planPhaseParser.ParsePhases(content);

            if (phases.Count == 0)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("| # | Phase | Status | Tasks |");
            sb.AppendLine("|---|-------|--------|-------|");

            foreach (var phase in phases)
            {
                sb.AppendLine($"| {phase.Number} | {EscapeTableCell(phase.Name)} | {phase.StatusEmoji} | {phase.TasksCompleted}/{phase.TasksTotal} |");
            }

            return sb.ToString();
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse plan document for progress: {Path}", planDocPath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Ensures the docs/progress/ directory exists.
    /// </summary>
    private void EnsureProgressDirectory()
    {
        var dir = Path.GetDirectoryName(ProgressDocPath)!;
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            _logger.Debug("Created progress directory: {Path}", dir);
        }
    }

    /// <summary>
    /// Gets the display status for a phase based on current loop state.
    /// </summary>
    private static string GetPhaseDisplayStatus(LoopState state, LoopPhase phase)
    {
        if (state.CompletedPhases.Contains(phase))
        {
            return "✅ Complete";
        }

        if (state.CurrentPhase == phase)
        {
            return state.Status switch
            {
                PhaseStatus.InProgress => "🔄 In Progress",
                PhaseStatus.Failed => "❌ Failed",
                PhaseStatus.Paused => "⏸️ Paused",
                PhaseStatus.NotStarted => "⏳ Not Started",
                PhaseStatus.Complete => "✅ Complete",
                _ => "⏳ Not Started"
            };
        }

        // Phase hasn't been reached yet
        return "⏳ Not Started";
    }

    /// <summary>
    /// Gets the document path for a phase, or "—" if not yet created.
    /// </summary>
    private static string GetPhaseDocumentPath(LoopState state, LoopPhase phase)
    {
        var path = phase switch
        {
            LoopPhase.Vision => state.Documents.Vision,
            LoopPhase.Research => state.Documents.Research,
            LoopPhase.Planning => state.Documents.Plan,
            _ => null
        };

        return string.IsNullOrEmpty(path) ? "—" : $"`{path}`";
    }

    /// <summary>
    /// Formats a PhaseStatus enum value for display.
    /// </summary>
    private static string FormatPhaseStatus(PhaseStatus status)
    {
        return status switch
        {
            PhaseStatus.NotStarted => "⏳ Not Started",
            PhaseStatus.InProgress => "🔄 In Progress",
            PhaseStatus.Complete => "✅ Complete",
            PhaseStatus.Failed => "❌ Failed",
            PhaseStatus.Paused => "⏸️ Paused",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Escapes pipe characters in table cell content to prevent markdown table breakage.
    /// </summary>
    private static string EscapeTableCell(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "—";
        }

        return value
            .Replace("|", "\\|")
            .Replace("\n", " ")
            .Replace("\r", "");
    }
}
