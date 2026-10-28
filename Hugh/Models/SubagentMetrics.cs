namespace Hugh.Models;

/// <summary>
/// Tracks subagent lifecycle events during a session.
/// </summary>
public sealed record SubagentMetrics
{
    public int Started { get; init; }
    public int Completed { get; init; }
    public int Failed { get; init; }
}
