using System.Text.Json;
using Hugh.Configuration;
using Hugh.Setup.Detection;
using Microsoft.Extensions.Logging;

namespace Hugh.Agents.Discovery;

/// <summary>
/// Discovers and syncs agent prompt files from a GitHub organization's .github-private repository.
/// Uses the gh CLI (already a prerequisite) to interact with the GitHub API.
/// </summary>
public class GitHubAgentDiscoveryService : IAgentDiscoveryService
{
    private readonly IProcessRunner _processRunner;
    private readonly AgentDiscoveryConfig _config;
    private readonly ILogger<GitHubAgentDiscoveryService> _logger;

    public GitHubAgentDiscoveryService(
        IProcessRunner processRunner,
        AgentDiscoveryConfig config,
        ILogger<GitHubAgentDiscoveryService> logger)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<AgentDiscoveryResult> DiscoverAndSyncAsync(CancellationToken ct = default)
    {
        if (!_config.Enabled)
        {
            return new AgentDiscoveryResult
            {
                Success = false,
                Message = "Agent discovery is disabled"
            };
        }

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(_config.DiscoveryTimeout);

        try
        {
            return await DiscoverAndSyncCoreAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            _logger.LogWarning("Agent discovery timed out after {Timeout}", _config.DiscoveryTimeout);
            return FallbackToCachedAgents("Discovery timed out");
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or IOException)
        {
            _logger.LogWarning(ex, "Agent discovery failed: {Message}", ex.Message);
            return FallbackToCachedAgents($"Discovery failed: {ex.Message}");
        }
    }

    private async Task<AgentDiscoveryResult> DiscoverAndSyncCoreAsync(CancellationToken ct)
    {
        // Resolve which org to use
        var org = await ResolveOrgAsync(ct);
        if (org == null)
        {
            _logger.LogInformation("No GitHub org with agent prompts found");
            return FallbackToCachedAgents("No GitHub org with agent prompts found");
        }

        var cachePath = GetOrgCachePath(org);
        var manifest = AgentManifest.Load(cachePath);
        var source = $"github:{org}/{_config.RepositoryName}";

        // If cache is fresh, skip remote check
        if (manifest != null && IsCacheFresh(manifest))
        {
            var cachedCount = manifest.Files.Count;
            _logger.LogDebug("Using fresh cached agents from {Org} ({Count} agents, synced {Ago} ago)",
                org, cachedCount, DateTime.UtcNow - manifest.LastSyncUtc);

            return new AgentDiscoveryResult
            {
                Success = true,
                AgentsPath = cachePath,
                Source = source,
                Message = $"Using cached agents (synced {FormatAge(manifest.LastSyncUtc)})",
                UsedCache = true,
                AgentCount = cachedCount
            };
        }

        // Fetch remote agent listing
        var remoteFiles = await ListRemoteAgentsAsync(org, ct);
        if (remoteFiles == null || remoteFiles.Count == 0)
        {
            _logger.LogWarning("No agent files found in {Org}/{Repo}/{Dir}",
                org, _config.RepositoryName, _config.AgentsDirectory);

            if (manifest != null)
                return CachedFallbackResult(cachePath, manifest, source, "Remote listing empty, using cache");

            return new AgentDiscoveryResult
            {
                Success = false,
                Message = $"No agent files found in {source}"
            };
        }

        // Determine which files need downloading (incremental sync)
        var existingShas = manifest?.Files.ToDictionary(f => f.FileName, f => f.Sha)
            ?? new Dictionary<string, string>();

        var filesToDownload = remoteFiles
            .Where(f => !existingShas.TryGetValue(f.Name, out var sha) || sha != f.Sha)
            .ToList();

        _logger.LogInformation("Agent sync: {Total} remote, {Changed} changed, {Cached} cached",
            remoteFiles.Count, filesToDownload.Count, remoteFiles.Count - filesToDownload.Count);

        // Download changed files
        Directory.CreateDirectory(cachePath);
        var downloadedEntries = new List<AgentFileEntry>();
        var existingEntries = manifest?.Files
            .Where(f => remoteFiles.Any(r => r.Name == f.FileName && r.Sha == f.Sha))
            .ToList() ?? new List<AgentFileEntry>();

        foreach (var file in filesToDownload)
        {
            var downloaded = await DownloadAgentFileAsync(org, file.Name, cachePath, ct);
            if (downloaded)
            {
                downloadedEntries.Add(new AgentFileEntry
                {
                    FileName = file.Name,
                    Sha = file.Sha,
                    Size = file.Size,
                    DownloadedAtUtc = DateTime.UtcNow
                });
            }
            else
            {
                _logger.LogWarning("Failed to download agent file: {FileName}", file.Name);
            }
        }

        // Build updated manifest
        var allEntries = existingEntries.Concat(downloadedEntries).ToList();
        var newManifest = new AgentManifest
        {
            Org = org,
            Repository = _config.RepositoryName,
            LastSyncUtc = DateTime.UtcNow,
            Files = allEntries
        };
        newManifest.Save(cachePath);

        _logger.LogInformation("Agent sync complete: {Count} agents from {Source}", allEntries.Count, source);

        return new AgentDiscoveryResult
        {
            Success = true,
            AgentsPath = cachePath,
            Source = source,
            Message = $"Synced {allEntries.Count} agents from {source}",
            UsedCache = false,
            AgentCount = allEntries.Count
        };
    }

    /// <summary>
    /// Resolves which GitHub org to fetch agents from.
    /// If PinnedOrg is set, validates it has agents. Otherwise enumerates all orgs.
    /// </summary>
    private async Task<string?> ResolveOrgAsync(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_config.PinnedOrg))
        {
            _logger.LogDebug("Using pinned org: {Org}", _config.PinnedOrg);
            var hasAgents = await OrgHasAgentsAsync(_config.PinnedOrg, ct);
            return hasAgents ? _config.PinnedOrg : null;
        }

        // Enumerate user's orgs
        var orgs = await ListUserOrgsAsync(ct);
        if (orgs == null || orgs.Count == 0)
        {
            _logger.LogDebug("User does not belong to any GitHub organizations");
            return null;
        }

        // Check each org for agents (first match wins)
        foreach (var org in orgs)
        {
            _logger.LogDebug("Checking org {Org} for agent prompts...", org);
            var hasAgents = await OrgHasAgentsAsync(org, ct);
            if (hasAgents)
            {
                _logger.LogInformation("Found agent prompts in org: {Org}", org);
                return org;
            }
        }

        return null;
    }

    /// <summary>
    /// Lists the user's GitHub organizations.
    /// </summary>
    private async Task<List<string>?> ListUserOrgsAsync(CancellationToken ct)
    {
        var result = await _processRunner.RunAsync("gh", "api user/orgs --jq \".[].login\"", ct);
        if (result.ExitCode != 0)
        {
            _logger.LogWarning("Failed to list user orgs: {Error}", result.StandardError.Trim());
            return null;
        }

        var orgs = result.StandardOutput
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        _logger.LogDebug("User belongs to {Count} org(s): {Orgs}", orgs.Count, string.Join(", ", orgs));
        return orgs;
    }

    /// <summary>
    /// Checks if an org has agent prompt files in its .github-private repo.
    /// </summary>
    private async Task<bool> OrgHasAgentsAsync(string org, CancellationToken ct)
    {
        var agents = await ListRemoteAgentsAsync(org, ct);
        return agents != null && agents.Count > 0;
    }

    /// <summary>
    /// Lists agent files from a GitHub org's .github-private repository.
    /// </summary>
    internal async Task<List<GitHubContentEntry>?> ListRemoteAgentsAsync(string org, CancellationToken ct)
    {
        var apiPath = $"repos/{org}/{_config.RepositoryName}/contents/{_config.AgentsDirectory}";
        var result = await _processRunner.RunAsync("gh", $"api {apiPath}", ct);

        if (result.ExitCode != 0)
        {
            var stderr = result.StandardError.Trim();
            if (stderr.Contains("404") || stderr.Contains("Not Found"))
            {
                _logger.LogDebug("Org {Org} does not have {Repo}/{Dir}", org, _config.RepositoryName, _config.AgentsDirectory);
                return null;
            }
            if (stderr.Contains("403") || stderr.Contains("Forbidden"))
            {
                _logger.LogDebug("No access to {Org}/{Repo}", org, _config.RepositoryName);
                return null;
            }

            _logger.LogWarning("Unexpected error listing agents for {Org}: {Error}", org, stderr);
            return null;
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<GitHubContentEntry>>(result.StandardOutput, JsonOptions);
            if (entries == null)
                return null;

            // Filter to pch-*.md files
            var agentFiles = entries
                .Where(e => e.Type == "file" && e.Name.StartsWith("pch-") && e.Name.EndsWith(".md"))
                .ToList();

            _logger.LogDebug("Found {Count} agent file(s) in {Org}", agentFiles.Count, org);
            return agentFiles;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse agent listing response from {Org}", org);
            return null;
        }
    }

    /// <summary>
    /// Downloads a single agent file from GitHub and writes it to the cache directory.
    /// Uses atomic writes to prevent corruption.
    /// </summary>
    private async Task<bool> DownloadAgentFileAsync(string org, string fileName, string targetDir, CancellationToken ct)
    {
        var apiPath = $"repos/{org}/{_config.RepositoryName}/contents/{_config.AgentsDirectory}/{fileName}";
        var result = await _processRunner.RunAsync("gh", $"api {apiPath}", ct);

        if (result.ExitCode != 0)
        {
            _logger.LogWarning("Failed to download {FileName} from {Org}: {Error}", fileName, org, result.StandardError.Trim());
            return false;
        }

        try
        {
            var fileEntry = JsonSerializer.Deserialize<GitHubContentEntry>(result.StandardOutput, JsonOptions);
            if (fileEntry?.Content == null)
            {
                _logger.LogWarning("No content in response for {FileName}", fileName);
                return false;
            }

            // Decode base64 content (GitHub returns content with newlines in the base64)
            var cleanBase64 = fileEntry.Content.Replace("\n", "").Replace("\r", "");
            var content = Convert.FromBase64String(cleanBase64);

            // Atomic write: write to .tmp, then rename
            var targetPath = Path.Combine(targetDir, fileName);
            var tmpPath = targetPath + ".tmp";
            await File.WriteAllBytesAsync(tmpPath, content, ct);
            File.Move(tmpPath, targetPath, overwrite: true);

            _logger.LogDebug("Downloaded {FileName} ({Size} bytes)", fileName, content.Length);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or FormatException or IOException)
        {
            _logger.LogWarning(ex, "Error processing downloaded file {FileName}", fileName);
            return false;
        }
    }

    /// <summary>
    /// Gets the base cache directory, using the configured override if set.
    /// </summary>
    private string GetCacheBasePath()
    {
        return !string.IsNullOrEmpty(_config.CacheBasePath)
            ? _config.CacheBasePath
            : PathResolver.GetAgentsCachePath();
    }

    /// <summary>
    /// Returns a result using cached agents when remote discovery fails.
    /// </summary>
    private AgentDiscoveryResult FallbackToCachedAgents(string reason)
    {
        // Check all org cache directories for any cached agents
        var cacheBase = GetCacheBasePath();

        if (!Directory.Exists(cacheBase))
        {
            return new AgentDiscoveryResult
            {
                Success = false,
                Message = $"{reason}. No cached agents available."
            };
        }

        // If pinned org, check that specific cache
        if (!string.IsNullOrEmpty(_config.PinnedOrg))
        {
            var pinnedPath = GetOrgCachePath(_config.PinnedOrg);
            var pinnedManifest = AgentManifest.Load(pinnedPath);
            if (pinnedManifest != null && pinnedManifest.Files.Count > 0)
            {
                return CachedFallbackResult(pinnedPath, pinnedManifest,
                    $"github:{_config.PinnedOrg}/{_config.RepositoryName}", reason);
            }
        }

        // Search all cached org directories
        foreach (var orgDir in Directory.GetDirectories(cacheBase))
        {
            var manifest = AgentManifest.Load(orgDir);
            if (manifest != null && manifest.Files.Count > 0)
            {
                return CachedFallbackResult(orgDir, manifest,
                    $"github:{manifest.Org}/{manifest.Repository}", reason);
            }
        }

        return new AgentDiscoveryResult
        {
            Success = false,
            Message = $"{reason}. No cached agents available."
        };
    }

    private static AgentDiscoveryResult CachedFallbackResult(
        string cachePath, AgentManifest manifest, string source, string reason)
    {
        return new AgentDiscoveryResult
        {
            Success = true,
            AgentsPath = cachePath,
            Source = source,
            Message = $"{reason}. Using cached agents (synced {FormatAge(manifest.LastSyncUtc)})",
            UsedCache = true,
            AgentCount = manifest.Files.Count
        };
    }

    private bool IsCacheFresh(AgentManifest manifest)
    {
        return (DateTime.UtcNow - manifest.LastSyncUtc) < _config.CacheTtl;
    }

    private string GetOrgCachePath(string org)
    {
        return Path.Combine(GetCacheBasePath(), org);
    }

    private static string FormatAge(DateTime utcTime)
    {
        var age = DateTime.UtcNow - utcTime;
        if (age.TotalMinutes < 1) return "just now";
        if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes}m ago";
        if (age.TotalHours < 24) return $"{(int)age.TotalHours}h ago";
        return $"{(int)age.TotalDays}d ago";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };
}

/// <summary>
/// Represents a file/directory entry from the GitHub Contents API.
/// </summary>
internal record GitHubContentEntry
{
    public string Name { get; init; } = "";
    public string Path { get; init; } = "";
    public string Sha { get; init; } = "";
    public long Size { get; init; }
    public string Type { get; init; } = "";
    public string? Content { get; init; }
    public string? Encoding { get; init; }
    public string? DownloadUrl { get; init; }
}
