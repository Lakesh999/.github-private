using Hugh.Metrics;

namespace Hugh.Models;

/// <summary>
/// Aggregated metrics for an entire Hugh workflow run across all phases.
/// </summary>
public sealed record SessionReport
{
    public Guid RunId { get; init; }
    public List<PhaseMetricsEntry> Phases { get; init; } = new();
    public UsageMetrics TotalUsage { get; init; } = new();
    public SubagentMetrics TotalSubagents { get; init; } = new();
    public int CompactionEvents { get; init; }
    public TimeSpan TotalDuration { get; init; }
}

/// <summary>
/// Metrics for a single phase execution within a session report.
/// </summary>
public sealed record PhaseMetricsEntry
{
    public string PhaseName { get; init; } = "";
    public UsageMetrics? Usage { get; init; }
    public SubagentMetrics? Subagents { get; init; }
    public ToolMetricsSummary? Tools { get; init; }
    public TimeSpan Duration { get; init; }
}
