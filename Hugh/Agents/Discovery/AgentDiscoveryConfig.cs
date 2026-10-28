namespace Hugh.Agents.Discovery;

/// <summary>
/// Configuration for GitHub-based agent prompt discovery.
/// </summary>
public class AgentDiscoveryConfig
{
    /// <summary>
    /// Whether auto-discovery is enabled. Defaults to true.
    /// Set to false to use only bundled/local agents.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Pin discovery to a specific GitHub org.
    /// When null, Hugh enumerates all orgs the user belongs to and uses the first match.
    /// </summary>
    public string? PinnedOrg { get; set; }

    /// <summary>
    /// The repository name within the org to look for agents.
    /// Defaults to ".github-private".
    /// </summary>
    public string RepositoryName { get; set; } = ".github-private";

    /// <summary>
    /// The directory within the repository containing agent files.
    /// Defaults to "agents".
    /// </summary>
    public string AgentsDirectory { get; set; } = "agents";

    /// <summary>
    /// Maximum age of cached agents before a re-sync is attempted.
    /// Even with expired cache, offline operation will use stale cache.
    /// </summary>
    public TimeSpan CacheTtl { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Timeout for the entire discovery operation.
    /// If exceeded, falls back to cached/bundled agents.
    /// </summary>
    public TimeSpan DiscoveryTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Override path for the agent cache directory.
    /// When null, uses the default PathResolver.GetAgentsCachePath() (~/.hugh/agents).
    /// Primarily used for testing.
    /// </summary>
    public string? CacheBasePath { get; set; }
}
