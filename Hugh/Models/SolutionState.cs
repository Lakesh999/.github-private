namespace Hugh.Models;

/// <summary>
/// Whether the request was classified as a single feature or a multi-release solution.
/// </summary>
public enum SolutionMode
{
    SingleFeature,
    MultiRelease
}

/// <summary>
/// Top-level phases of the solution developer workflow.
/// </summary>
public enum SolutionPhase
{
    Triage,
    Clarification,
    Decomposition,
    Execution,
    Complete
}

/// <summary>
/// Status of an individual release within a solution.
/// </summary>
public enum ReleaseStatus
{
    Pending,
    InProgress,
    Complete,
    Failed,
    Skipped
}

/// <summary>
/// Defines a single release within a multi-release solution roadmap.
/// </summary>
public record ReleaseDefinition
{
    /// <summary>
    /// The 1-based release number in the roadmap sequence.
    /// </summary>
    public int ReleaseNumber { get; init; }

    /// <summary>
    /// Short name for the release (e.g., "MVP", "Collaboration Features").
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Brief description of what this release delivers.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Detailed scope of what is included in this release.
    /// </summary>
    public string Scope { get; init; } = "";

    /// <summary>
    /// Links to the per-release LoopState stored under .orchestrator/releases/release-N/.
    /// </summary>
    public Guid RunId { get; init; }

    /// <summary>
    /// Current status of this release.
    /// </summary>
    public ReleaseStatus Status { get; init; }
}

/// <summary>
/// Paths to documents generated during the solution workflow.
/// </summary>
public record SolutionDocumentPaths
{
    /// <summary>
    /// Path to the clarified vision document produced by the clarification phase.
    /// </summary>
    public string? ClarifiedVision { get; init; }

    /// <summary>
    /// Path to the release roadmap document produced by the decomposition phase.
    /// </summary>
    public string? Roadmap { get; init; }
}

/// <summary>
/// Represents the current state of a multi-release solution run.
/// Persisted to .orchestrator/solution-state.json for resumability.
/// </summary>
public record SolutionState
{
    public Guid SolutionId { get; init; }
    public DateTime StartedAt { get; init; }
    public string OriginalRequest { get; init; } = "";
    public SolutionMode Mode { get; init; }
    public SolutionPhase CurrentPhase { get; init; }
    public SolutionDocumentPaths Documents { get; init; } = new();
    public List<ReleaseDefinition> Releases { get; init; } = new();
    public int CurrentReleaseIndex { get; init; }
    public PhaseStatus Status { get; init; }
    public string? FailureReason { get; init; }
    public List<Decision> Decisions { get; init; } = new();
}
