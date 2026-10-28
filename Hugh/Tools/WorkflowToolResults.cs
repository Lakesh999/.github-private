namespace Hugh.Tools;

/// <summary>
/// Result returned when a phase is complete.
/// </summary>
public record PhaseCompleteResult
{
    public bool Acknowledged { get; init; } = true;
    public int? NextPhaseNumber { get; init; }
    public string Message { get; init; } = "Phase completion acknowledged";

    /// <summary>
    /// Optional review outcome from the review phase: "approved", "approved_with_findings", or "rejected".
    /// </summary>
    public string? ReviewOutcome { get; init; }
}

/// <summary>
/// Result returned when the entire workflow is complete.
/// </summary>
public record WorkflowCompleteResult
{
    public bool Acknowledged { get; init; } = true;
    public string Message { get; init; } = "Workflow completion acknowledged";
}

/// <summary>
/// Result returned when a blocker is reported.
/// </summary>
public record BlockerResult
{
    public bool Acknowledged { get; init; } = true;
    public string Action { get; init; } = "paused";
    public string Message { get; init; } = "Blocker acknowledged — execution paused";
}

/// <summary>
/// Result containing workspace directory structure information.
/// </summary>
public record WorkspaceStructureResult
{
    public string RootPath { get; init; } = "";
    public string DirectoryTree { get; init; } = "";
    public int FileCount { get; init; }
}
