using Hugh.Agents.Discovery;
using Hugh.Configuration;

namespace Hugh.Setup.Detection;

/// <summary>
/// Detects agent availability via GitHub discovery cache or bundled fallback.
/// Runs at a higher priority (lower number) than AgentsFolderDetector so that
/// discovered agents are checked first.
/// </summary>
public class AgentDiscoveryDetector : IPrerequisiteDetector
{
    private readonly AgentDiscoveryConfig _config;
    private readonly string _bundledAgentsPath;

    /// <summary>
    /// The expected PCH agent file names (without extension).
    /// </summary>
    private static readonly string[] ExpectedAgents =
    [
        "pch-coder",
        "pch-helper",
        "pch-plan-reviewer",
        "pch-planner",
        "pch-researcher",
        "pch-visionary"
    ];

    public AgentDiscoveryDetector(AgentDiscoveryConfig config, string bundledAgentsPath)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _bundledAgentsPath = bundledAgentsPath ?? throw new ArgumentNullException(nameof(bundledAgentsPath));
    }

    public string PrerequisiteId => "agent-discovery";
    public string DisplayName => "Agent Prompt Discovery";
    public int Priority => 5; // Runs before AgentsFolderDetector (priority 10)
    public bool IsInstallable => true;

    public Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        if (!_config.Enabled)
        {
            // Discovery disabled — check bundled agents instead
            return Task.FromResult(CheckBundledAgents("Discovery disabled"));
        }

        // Check cached agents from GitHub discovery
        var cacheBase = !string.IsNullOrEmpty(_config.CacheBasePath)
            ? _config.CacheBasePath
            : PathResolver.GetAgentsCachePath();

        if (Directory.Exists(cacheBase))
        {
            // If pinned org, check that specific directory
            if (!string.IsNullOrEmpty(_config.PinnedOrg))
            {
                var orgPath = Path.Combine(cacheBase, _config.PinnedOrg);
                var result = CheckCachedAgents(orgPath, _config.PinnedOrg);
                if (result.State == PrerequisiteState.Satisfied)
                    return Task.FromResult(result);
            }
            else
            {
                // Check all org directories
                foreach (var orgDir in Directory.GetDirectories(cacheBase))
                {
                    var orgName = Path.GetFileName(orgDir);
                    var result = CheckCachedAgents(orgDir, orgName);
                    if (result.State == PrerequisiteState.Satisfied)
                        return Task.FromResult(result);
                }
            }
        }

        // No cached agents — check bundled as fallback
        return Task.FromResult(CheckBundledAgents("No cached agents from GitHub"));
    }

    private PrerequisiteStatus CheckCachedAgents(string orgCachePath, string orgName)
    {
        var manifest = AgentManifest.Load(orgCachePath);
        if (manifest == null || manifest.Files.Count == 0)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = $"Cache exists for {orgName} but no manifest or files"
            };
        }

        // Verify cached files actually exist on disk
        var existingFiles = manifest.Files
            .Where(f => File.Exists(Path.Combine(orgCachePath, f.FileName)))
            .ToList();

        if (existingFiles.Count == 0)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "Cached agent files are missing from disk"
            };
        }

        var age = DateTime.UtcNow - manifest.LastSyncUtc;
        var ageStr = FormatAge(manifest.LastSyncUtc);
        var source = $"github:{orgName}/{manifest.Repository}";

        if (existingFiles.Count < ExpectedAgents.Length)
        {
            var foundNames = existingFiles.Select(f => Path.GetFileNameWithoutExtension(f.FileName)).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = ExpectedAgents.Where(a => !foundNames.Contains(a)).ToList();

            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Misconfigured,
                Message = $"{existingFiles.Count}/{ExpectedAgents.Length} agents from {source} (synced {ageStr})",
                Version = $"{existingFiles.Count}/{ExpectedAgents.Length} agents",
                Hints = [$"Missing: {string.Join(", ", missing)}"]
            };
        }

        return new PrerequisiteStatus
        {
            PrerequisiteId = PrerequisiteId,
            State = PrerequisiteState.Satisfied,
            Message = $"{existingFiles.Count} agents from {source} (synced {ageStr})",
            Version = $"{existingFiles.Count} agents"
        };
    }

    private PrerequisiteStatus CheckBundledAgents(string context)
    {
        if (!Directory.Exists(_bundledAgentsPath))
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = $"{context}. No bundled agents found.",
                Hints =
                [
                    "Run 'hugh doctor --fix' to sync agents from your GitHub organization",
                    "Or ensure the 'agents/' folder is present alongside hugh.exe"
                ]
            };
        }

        var existingFiles = Directory.GetFiles(_bundledAgentsPath, "pch-*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (existingFiles.Count == 0)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = $"{context}. Bundled agents folder exists but is empty.",
                Hints = ["Run 'hugh doctor --fix' to sync agents from your GitHub organization"]
            };
        }

        var missingAgents = ExpectedAgents.Where(a => !existingFiles.Contains(a)).ToList();

        if (missingAgents.Count > 0)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Misconfigured,
                Message = $"{context}. Bundled: {existingFiles.Count}/{ExpectedAgents.Length} agents.",
                Version = $"{existingFiles.Count}/{ExpectedAgents.Length} agents",
                Hints = [$"Missing: {string.Join(", ", missingAgents)}"]
            };
        }

        return new PrerequisiteStatus
        {
            PrerequisiteId = PrerequisiteId,
            State = PrerequisiteState.Satisfied,
            Message = $"Using bundled agents ({existingFiles.Count} agents)",
            Version = $"{existingFiles.Count} agents (bundled)"
        };
    }

    private static string FormatAge(DateTime utcTime)
    {
        var age = DateTime.UtcNow - utcTime;
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
        return $"{(int)age.TotalDays}d ago";
    }
}
