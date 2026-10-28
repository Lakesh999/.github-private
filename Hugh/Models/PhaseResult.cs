using Hugh.Detection;
using Hugh.Metrics;

namespace Hugh.Models;

/// <summary>
/// Represents the result of executing a single phase.
/// </summary>
public record PhaseResult
{
    public bool Success { get; init; }
    public string? OutputDocumentPath { get; init; }
    public string? ErrorMessage { get; init; }
    public List<Decision> DecisionsMade { get; init; } = new();
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether context compaction occurred during this phase, indicating context pressure.
    /// </summary>
    public bool CompactionOccurred { get; init; }

    /// <summary>
    /// Tool execution metrics collected during this phase, if available.
    /// </summary>
    public ToolMetricsSummary? ToolMetrics { get; init; }

    /// <summary>
    /// Error classification for the phase failure, if an error occurred.
    /// Used by retry logic to determine retryability and delay.
    /// </summary>
    public ErrorClassification? ErrorClassification { get; init; }

    /// <summary>
    /// Number of retry attempts made before this result was produced.
    /// </summary>
    public int RetryAttemptsMade { get; init; }

    /// <summary>
    /// Review outcome from the Review phase, if applicable.
    /// Only populated for Review phase results.
    /// </summary>
    public ReviewOutcome? ReviewOutcome { get; init; }

    /// <summary>
    /// Token usage metrics captured from AssistantUsageEvent during this phase.
    /// </summary>
    public UsageMetrics? Usage { get; init; }

    /// <summary>
    /// Subagent lifecycle metrics captured during this phase.
    /// </summary>
    public SubagentMetrics? Subagents { get; init; }

    /// <summary>
    /// The session ID assigned by the Copilot SDK for this phase execution.
    /// </summary>
    public string? SessionId { get; init; }
}
