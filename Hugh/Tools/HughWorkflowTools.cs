using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.AI;
using ILogger = Serilog.ILogger;

namespace Hugh.Tools;

/// <summary>
/// Custom workflow tools that can be registered with the Copilot SDK session.
/// Provides signaling mechanisms for phase completion, blockers, and workspace structure queries.
/// </summary>
public class HughWorkflowTools
{
    private readonly ILogger _logger;
    private readonly string _workspacePath;

    /// <summary>
    /// Stores the isSkippable value from the most recent ReportBlocker call.
    /// Read by the orchestrator after tool-based blocker signal detection.
    /// Thread-safe: only one agent session uses a HughWorkflowTools instance at a time.
    /// </summary>
    public bool LastBlockerIsSkippable { get; private set; }

    /// <summary>
    /// Stores the reason from the most recent ReportBlocker call.
    /// </summary>
    public string? LastBlockerReason { get; private set; }

    /// <summary>
    /// Stores the reviewOutcome value from the most recent NotifyPhaseComplete call.
    /// Read by the orchestrator after tool-based phase complete signal detection.
    /// Thread-safe: only one agent session uses a HughWorkflowTools instance at a time.
    /// </summary>
    public string? LastReviewOutcome { get; private set; }

    public HughWorkflowTools(ILogger logger, string workspacePath)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
    }

    /// <summary>
    /// Notify the orchestrator that the current phase is complete.
    /// </summary>
    /// <param name="phase">The phase number that was completed.</param>
    /// <param name="summary">A summary of what was accomplished in this phase.</param>
    /// <param name="outputDocumentPath">Optional path to an output document created by this phase.</param>
    /// <param name="reviewOutcome">Optional review outcome: "approved", "approved_with_findings", or "rejected".</param>
    [Description("Notify the orchestrator that the current phase is complete")]
    public PhaseCompleteResult NotifyPhaseComplete(
        [Description("The phase number that was completed")] int phase,
        [Description("A summary of what was accomplished in this phase")] string summary,
        [Description("Optional path to an output document created by this phase")] string? outputDocumentPath = null,
        [Description("Optional review outcome: 'approved', 'approved_with_findings', or 'rejected'")] string? reviewOutcome = null)
    {
        _logger.Information("Phase {Phase} complete: {Summary}", phase, summary);

        if (!string.IsNullOrEmpty(outputDocumentPath))
        {
            _logger.Debug("Output document: {OutputDocumentPath}", outputDocumentPath);
        }

        if (!string.IsNullOrEmpty(reviewOutcome))
        {
            _logger.Information("Review outcome: {ReviewOutcome}", reviewOutcome);
            LastReviewOutcome = reviewOutcome;
        }

        return new PhaseCompleteResult
        {
            Acknowledged = true,
            NextPhaseNumber = phase + 1,
            Message = $"Phase {phase} completion acknowledged",
            ReviewOutcome = reviewOutcome
        };
    }

    /// <summary>
    /// Notify the orchestrator that the entire workflow is complete.
    /// </summary>
    /// <param name="summary">A summary of the overall workflow outcome.</param>
    /// <param name="documentsCreated">Optional list of paths to documents created during the workflow.</param>
    [Description("Notify the orchestrator that the entire workflow is complete")]
    public WorkflowCompleteResult NotifyWorkflowComplete(
        [Description("A summary of the overall workflow outcome")] string summary,
        [Description("Optional list of paths to documents created during the workflow")] List<string>? documentsCreated = null)
    {
        _logger.Information("Workflow complete: {Summary}", summary);

        if (documentsCreated != null && documentsCreated.Count > 0)
        {
            _logger.Debug("Documents created: {DocumentPaths}", string.Join(", ", documentsCreated));
        }

        return new WorkflowCompleteResult
        {
            Acknowledged = true,
            Message = "Workflow completion acknowledged"
        };
    }

    /// <summary>
    /// Report a blocker that prevents the agent from proceeding.
    /// </summary>
    /// <param name="reason">The reason for the blocker.</param>
    /// <param name="severity">The severity level (e.g., "low", "medium", "high", "critical").</param>
    /// <param name="isSkippable">Whether this blocker can be skipped to continue execution.</param>
    /// <param name="suggestedActions">Optional list of suggested actions to resolve the blocker.</param>
    [Description("Report a blocker that prevents the agent from proceeding")]
    public BlockerResult ReportBlocker(
        [Description("The reason for the blocker")] string reason,
        [Description("The severity level (e.g., 'low', 'medium', 'high', 'critical')")] string severity,
        [Description("Whether this blocker can be skipped to continue execution")] bool isSkippable,
        [Description("Optional list of suggested actions to resolve the blocker")] List<string>? suggestedActions = null)
    {
        LastBlockerIsSkippable = isSkippable;
        LastBlockerReason = reason;

        _logger.Error("Blocker reported: {Reason} (severity: {Severity})", reason, severity);

        if (suggestedActions != null && suggestedActions.Count > 0)
        {
            _logger.Debug("Suggested actions: {Actions}", string.Join("; ", suggestedActions));
        }

        return new BlockerResult
        {
            Acknowledged = true,
            Action = "paused",
            Message = $"Blocker acknowledged — execution paused. Skippable: {isSkippable}"
        };
    }

    /// <summary>
    /// Get the current workspace directory structure.
    /// </summary>
    /// <param name="maxDepth">Maximum directory depth to traverse (default: 3).</param>
    /// <param name="filterPath">Optional relative path to limit the scope of the structure query.</param>
    [Description("Get the current workspace directory structure")]
    public WorkspaceStructureResult GetWorkspaceStructure(
        [Description("Maximum directory depth to traverse (default: 3)")] int maxDepth = 3,
        [Description("Optional relative path to limit the scope of the structure query")] string? filterPath = null)
    {
        var rootPath = string.IsNullOrEmpty(filterPath)
            ? _workspacePath
            : Path.Combine(_workspacePath, filterPath);

        if (!Directory.Exists(rootPath))
        {
            return new WorkspaceStructureResult
            {
                RootPath = rootPath,
                DirectoryTree = $"Directory not found: {rootPath}",
                FileCount = 0
            };
        }

        var sb = new StringBuilder();
        var fileCount = 0;

        BuildDirectoryTree(rootPath, sb, "", maxDepth, 0, ref fileCount);

        return new WorkspaceStructureResult
        {
            RootPath = rootPath,
            DirectoryTree = sb.ToString(),
            FileCount = fileCount
        };
    }

    /// <summary>
    /// Recursively builds a directory tree string representation.
    /// </summary>
    private void BuildDirectoryTree(
        string path,
        StringBuilder sb,
        string indent,
        int maxDepth,
        int currentDepth,
        ref int fileCount)
    {
        if (currentDepth > maxDepth)
        {
            return;
        }

        try
        {
            // Add directories
            foreach (var dir in Directory.EnumerateDirectories(path).OrderBy(d => d))
            {
                var dirName = Path.GetFileName(dir);

                // Skip common non-essential directories
                if (dirName is "bin" or "obj" or "node_modules" or ".git")
                {
                    continue;
                }

                sb.AppendLine($"{indent}{dirName}/");
                BuildDirectoryTree(dir, sb, indent + "  ", maxDepth, currentDepth + 1, ref fileCount);
            }

            // Add files
            foreach (var file in Directory.EnumerateFiles(path).OrderBy(f => f))
            {
                var fileName = Path.GetFileName(file);
                sb.AppendLine($"{indent}{fileName}");
                fileCount++;
            }
        }
        catch (UnauthorizedAccessException)
        {
            sb.AppendLine($"{indent}[Access Denied]");
        }
        catch (Exception ex)
        {
            sb.AppendLine($"{indent}[Error: {ex.Message}]");
        }
    }

    /// <summary>
    /// Creates a tool set containing all workflow tools for SDK registration.
    /// </summary>
    public IList<AIFunction> CreateToolSet()
    {
        return new[]
        {
            AIFunctionFactory.Create(NotifyPhaseComplete,
                name: "hugh_notify_phase_complete",
                description: "Notify the orchestrator that the current phase is complete"),
            AIFunctionFactory.Create(NotifyWorkflowComplete,
                name: "hugh_notify_workflow_complete",
                description: "Notify the orchestrator that the entire workflow is complete"),
            AIFunctionFactory.Create(ReportBlocker,
                name: "hugh_report_blocker",
                description: "Report a blocker that prevents the agent from proceeding"),
            AIFunctionFactory.Create(GetWorkspaceStructure,
                name: "hugh_get_workspace_structure",
                description: "Get the current workspace directory structure")
        };
    }
}
