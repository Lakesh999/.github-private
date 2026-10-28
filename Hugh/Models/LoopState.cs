using System.Text.Json.Serialization;

namespace Hugh.Models;

/// <summary>
/// Represents the phases in the self-operating development loop.
/// </summary>
public enum LoopPhase
{
    Vision,
    Research,
    Planning,
    Review,
    Coding  // Contains sub-phases (coding-1, coding-2, ...)
}

/// <summary>
/// Status of a phase execution.
/// </summary>
public enum PhaseStatus
{
    NotStarted,
    InProgress,
    Complete,
    Failed,
    Paused  // Waiting for human input
}

/// <summary>
/// Represents the paths to documents generated during the loop.
/// </summary>
public record DocumentPaths
{
    public string? Vision { get; init; }
    public string? Research { get; init; }
    public string? Plan { get; init; }
}

/// <summary>
/// Represents the current state of a development loop run.
/// Persisted to .orchestrator/loop-state.json for resumability.
/// </summary>
public record LoopState
{
    public Guid RunId { get; init; }
    public DateTime StartedAt { get; init; }
    public string FeatureRequest { get; init; } = "";
    public LoopPhase CurrentPhase { get; init; }
    /// <summary>
    /// Tracks the current sub-phase number during any phase loop (research, planning, coding).
    /// Only one loop executes at a time, so a single counter is sufficient.
    /// </summary>
    [JsonPropertyName("currentCodingSubPhase")]
    public int? CurrentSubPhase { get; init; }
    public List<LoopPhase> CompletedPhases { get; init; } = new();
    public DocumentPaths Documents { get; init; } = new();
    public List<Decision> Decisions { get; init; } = new();
    public PhaseStatus Status { get; init; }
    public string? FailureReason { get; init; }
    /// <summary>
    /// The last Copilot SDK session ID, persisted for --resume session continuity.
    /// Nullable: existing state files without this property will deserialize as null.
    /// </summary>
    public string? LastSessionId { get; init; }
}
