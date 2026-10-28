namespace Hugh.Agents.Discovery;

/// <summary>
/// Discovers and syncs agent prompt files from a remote source (e.g., GitHub org).
/// </summary>
public interface IAgentDiscoveryService
{
    /// <summary>
    /// Discovers and downloads agent prompts from the configured source.
    /// Returns the local directory path where agents were synced, or a fallback result.
    /// </summary>
    Task<AgentDiscoveryResult> DiscoverAndSyncAsync(CancellationToken ct = default);
}

/// <summary>
/// Result of an agent discovery operation.
/// </summary>
public record AgentDiscoveryResult
{
    /// <summary>
    /// Whether agents are available (from any source).
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// The local path containing the agent files.
    /// Null if discovery failed entirely.
    /// </summary>
    public string? AgentsPath { get; init; }

    /// <summary>
    /// The source of the agents (e.g., "github:OrgName/.github-private").
    /// </summary>
    public string? Source { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public required string Message { get; init; }

    /// <summary>
    /// Whether cached agents from a previous sync were used (offline mode).
    /// </summary>
    public bool UsedCache { get; init; }

    /// <summary>
    /// Number of agent files available.
    /// </summary>
    public int AgentCount { get; init; }
}
