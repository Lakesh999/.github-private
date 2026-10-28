using System.Text;
using System.Text.Json;
using Hugh.Agents.Discovery;
using Hugh.Tests.Setup;
using Microsoft.Extensions.Logging.Abstractions;

namespace Hugh.Tests.Agents.Discovery;

/// <summary>
/// Unit tests for GitHubAgentDiscoveryService.
/// Tests agent discovery, download, caching, and offline fallback scenarios.
/// </summary>
public class GitHubAgentDiscoveryServiceTests : IDisposable
{
    private readonly string _testCacheDir;
    private readonly string? _originalHughAgentsPath;

    public GitHubAgentDiscoveryServiceTests()
    {
        _testCacheDir = Path.Combine(Path.GetTempPath(), $"hugh-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testCacheDir);

        // Ensure HUGH_AGENTS_PATH doesn't interfere
        _originalHughAgentsPath = Environment.GetEnvironmentVariable("HUGH_AGENTS_PATH");
        Environment.SetEnvironmentVariable("HUGH_AGENTS_PATH", null);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testCacheDir))
        {
            try { Directory.Delete(_testCacheDir, recursive: true); }
            catch { /* cleanup best-effort */ }
        }
        Environment.SetEnvironmentVariable("HUGH_AGENTS_PATH", _originalHughAgentsPath);
    }

    #region Disabled Discovery

    [Fact]
    public async Task DiscoverAndSyncAsync_WhenDisabled_ReturnsFailure()
    {
        var config = new AgentDiscoveryConfig { Enabled = false };
        var service = CreateService(new MockProcessRunner(), config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.False(result.Success);
        Assert.Contains("disabled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_WhenDisabled_MakesNoGhCalls()
    {
        var mockRunner = new MockProcessRunner();
        var config = new AgentDiscoveryConfig { Enabled = false };
        var service = CreateService(mockRunner, config);

        await service.DiscoverAndSyncAsync();

        Assert.Empty(mockRunner.Invocations);
    }

    #endregion

    #region Org Discovery

    [Fact]
    public async Task DiscoverAndSyncAsync_WithPinnedOrg_SkipsOrgEnumeration()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        await service.DiscoverAndSyncAsync();

        // Should NOT have called user/orgs
        Assert.DoesNotContain(mockRunner.Invocations, i => i.Arguments.Contains("user/orgs"));
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_WithoutPinnedOrg_EnumeratesOrgs()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgsResponse(mockRunner, "OrgA", "OrgB");
        SetupOrgWithAgents(mockRunner, "OrgB");
        SetupNoAgents(mockRunner, "OrgA");
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.Contains("OrgB", result.Source!);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_NoOrgs_ReturnsFailure()
    {
        var mockRunner = new MockProcessRunner();
        mockRunner.SetupSuccess("gh", "api user/orgs --jq \".[].login\"", "");
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_FirstOrgWithAgentsWins()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgsResponse(mockRunner, "OrgA", "OrgB", "OrgC");
        SetupNoAgents(mockRunner, "OrgA");
        SetupOrgWithAgents(mockRunner, "OrgB");
        // OrgC should not be checked since OrgB matched
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.Contains("OrgB", result.Source!);
    }

    #endregion

    #region Sync and Download

    [Fact]
    public async Task DiscoverAndSyncAsync_DownloadsAgentFiles()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.NotNull(result.AgentsPath);
        Assert.Equal(2, result.AgentCount);

        // Verify files were written
        Assert.True(File.Exists(Path.Combine(result.AgentsPath!, "pch-visionary.md")));
        Assert.True(File.Exists(Path.Combine(result.AgentsPath!, "pch-planner.md")));
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_WritesManifest()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        var manifest = AgentManifest.Load(result.AgentsPath!);
        Assert.NotNull(manifest);
        Assert.Equal("TestOrg", manifest.Org);
        Assert.Equal(2, manifest.Files.Count);
        Assert.True((DateTime.UtcNow - manifest.LastSyncUtc).TotalMinutes < 1);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_DecodesBase64Content()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        var content = File.ReadAllText(Path.Combine(result.AgentsPath!, "pch-visionary.md"));
        Assert.Equal("# Visionary Agent Prompt", content);
    }

    #endregion

    #region Cache Behavior

    [Fact]
    public async Task DiscoverAndSyncAsync_WithFreshCache_SkipsRemoteCheck()
    {
        // Pre-create a fresh cache
        var orgPath = Path.Combine(_testCacheDir, "TestOrg");
        Directory.CreateDirectory(orgPath);
        File.WriteAllText(Path.Combine(orgPath, "pch-visionary.md"), "cached content");
        var manifest = new AgentManifest
        {
            Org = "TestOrg",
            Repository = ".github-private",
            LastSyncUtc = DateTime.UtcNow, // Fresh
            Files = [new AgentFileEntry { FileName = "pch-visionary.md", Sha = "abc123", Size = 100 }]
        };
        manifest.Save(orgPath);

        var mockRunner = new MockProcessRunner();
        // Only set up the listing API (should not be called due to fresh cache)
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.True(result.UsedCache);
        // The only API call should be the listing check for OrgHasAgentsAsync,
        // but no download calls since cache is fresh
        Assert.Equal(1, result.AgentCount);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_WithStaleCache_SyncsRemotely()
    {
        // Pre-create a stale cache
        var orgPath = Path.Combine(_testCacheDir, "TestOrg");
        Directory.CreateDirectory(orgPath);
        File.WriteAllText(Path.Combine(orgPath, "pch-visionary.md"), "old content");
        var manifest = new AgentManifest
        {
            Org = "TestOrg",
            Repository = ".github-private",
            LastSyncUtc = DateTime.UtcNow.AddHours(-2), // Stale (default TTL is 1h)
            Files = [new AgentFileEntry { FileName = "pch-visionary.md", Sha = "old-sha", Size = 100 }]
        };
        manifest.Save(orgPath);

        var mockRunner = new MockProcessRunner();
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.False(result.UsedCache); // Should have synced remotely
        Assert.Equal(2, result.AgentCount);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_IncrementalSync_OnlyDownloadsChanged()
    {
        // Pre-create cache with one file matching remote SHA
        var orgPath = Path.Combine(_testCacheDir, "TestOrg");
        Directory.CreateDirectory(orgPath);
        File.WriteAllText(Path.Combine(orgPath, "pch-visionary.md"), "# Visionary Agent Prompt");
        var manifest = new AgentManifest
        {
            Org = "TestOrg",
            Repository = ".github-private",
            LastSyncUtc = DateTime.UtcNow.AddHours(-2), // Stale to force remote check
            Files = [new AgentFileEntry { FileName = "pch-visionary.md", Sha = "sha-visionary", Size = 100 }]
        };
        manifest.Save(orgPath);

        var mockRunner = new MockProcessRunner();
        SetupOrgWithAgents(mockRunner, "TestOrg");
        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(2, result.AgentCount);

        // pch-visionary.md has same SHA so it shouldn't be re-downloaded
        // pch-planner.md is new so it should be downloaded
        // Check that only pch-planner download was called
        var downloadCalls = mockRunner.Invocations
            .Where(i => i.Arguments.Contains("contents/agents/pch-"))
            .ToList();
        Assert.Single(downloadCalls);
        Assert.Contains("pch-planner.md", downloadCalls[0].Arguments);
    }

    #endregion

    #region Offline Fallback

    [Fact]
    public async Task DiscoverAndSyncAsync_OfflineWithCache_UsesCache()
    {
        // Pre-create cache
        var orgPath = Path.Combine(_testCacheDir, "TestOrg");
        Directory.CreateDirectory(orgPath);
        File.WriteAllText(Path.Combine(orgPath, "pch-visionary.md"), "cached content");
        new AgentManifest
        {
            Org = "TestOrg",
            Repository = ".github-private",
            LastSyncUtc = DateTime.UtcNow.AddHours(-5), // Stale
            Files = [new AgentFileEntry { FileName = "pch-visionary.md", Sha = "abc", Size = 100 }]
        }.Save(orgPath);

        var mockRunner = new MockProcessRunner();
        // Simulate network failure
        mockRunner.SetupException("gh", "api user/orgs --jq \".[].login\"",
            new IOException("Network unreachable"));
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.True(result.UsedCache);
        Assert.Contains("cached", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_OfflineWithoutCache_ReturnsFailure()
    {
        var mockRunner = new MockProcessRunner();
        mockRunner.SetupException("gh", "api user/orgs --jq \".[].login\"",
            new IOException("Network unreachable"));
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.False(result.Success);
        Assert.Contains("No cached agents", result.Message);
    }

    #endregion

    #region Error Handling

    [Fact]
    public async Task DiscoverAndSyncAsync_Api404_SkipsOrg()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgsResponse(mockRunner, "NoRepoOrg");
        mockRunner.SetupFailure("gh", "api repos/NoRepoOrg/.github-private/contents/agents",
            exitCode: 1, stderr: "HTTP 404: Not Found");
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.False(result.Success); // No org had agents
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_Api403_SkipsOrg()
    {
        var mockRunner = new MockProcessRunner();
        SetupOrgsResponse(mockRunner, "RestrictedOrg");
        mockRunner.SetupFailure("gh", "api repos/RestrictedOrg/.github-private/contents/agents",
            exitCode: 1, stderr: "HTTP 403: Forbidden");
        var config = CreateConfig();
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.False(result.Success);
    }

    [Fact]
    public async Task DiscoverAndSyncAsync_PartialDownloadFailure_StillSyncsOtherFiles()
    {
        var mockRunner = new MockProcessRunner();
        var listingJson = CreateFileListingJson(("pch-visionary.md", "sha1", 100), ("pch-planner.md", "sha2", 200));

        // Org has agents listing
        mockRunner.SetupSuccess("gh", "api repos/TestOrg/.github-private/contents/agents", listingJson);
        // Visionary succeeds
        mockRunner.SetupSuccess("gh", "api repos/TestOrg/.github-private/contents/agents/pch-visionary.md",
            CreateFileContentJson("pch-visionary.md", "sha1", "# Visionary"));
        // Planner fails
        mockRunner.SetupFailure("gh", "api repos/TestOrg/.github-private/contents/agents/pch-planner.md",
            exitCode: 1, stderr: "Server error");

        var config = CreateConfig(pinnedOrg: "TestOrg");
        var service = CreateService(mockRunner, config);

        var result = await service.DiscoverAndSyncAsync();

        Assert.True(result.Success);
        Assert.Equal(1, result.AgentCount); // Only visionary succeeded
    }

    #endregion

    #region Manifest Serialization

    [Fact]
    public void AgentManifest_RoundTrips()
    {
        var dir = Path.Combine(_testCacheDir, "manifest-test");
        Directory.CreateDirectory(dir);

        var manifest = new AgentManifest
        {
            Org = "TestOrg",
            Repository = ".github-private",
            LastSyncUtc = new DateTime(2026, 1, 15, 12, 0, 0, DateTimeKind.Utc),
            Files =
            [
                new AgentFileEntry { FileName = "pch-visionary.md", Sha = "abc", Size = 100, DownloadedAtUtc = DateTime.UtcNow },
                new AgentFileEntry { FileName = "pch-planner.md", Sha = "def", Size = 200, DownloadedAtUtc = DateTime.UtcNow }
            ]
        };

        manifest.Save(dir);
        var loaded = AgentManifest.Load(dir);

        Assert.NotNull(loaded);
        Assert.Equal("TestOrg", loaded.Org);
        Assert.Equal(2, loaded.Files.Count);
        Assert.Equal("abc", loaded.Files[0].Sha);
    }

    [Fact]
    public void AgentManifest_Load_ReturnsNullForMissing()
    {
        var dir = Path.Combine(_testCacheDir, "no-manifest");
        Directory.CreateDirectory(dir);

        var result = AgentManifest.Load(dir);
        Assert.Null(result);
    }

    [Fact]
    public void AgentManifest_Load_ReturnsNullForCorruptJson()
    {
        var dir = Path.Combine(_testCacheDir, "corrupt-manifest");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "manifest.json"), "not valid json {{{");

        var result = AgentManifest.Load(dir);
        Assert.Null(result);
    }

    #endregion

    #region Helpers

    private GitHubAgentDiscoveryService CreateService(MockProcessRunner mockRunner, AgentDiscoveryConfig config)
    {
        // Ensure cache path points to test directory
        config.CacheBasePath ??= _testCacheDir;

        return new GitHubAgentDiscoveryService(
            mockRunner,
            config,
            NullLogger<GitHubAgentDiscoveryService>.Instance);
    }

    private AgentDiscoveryConfig CreateConfig(string? pinnedOrg = null)
    {
        return new AgentDiscoveryConfig
        {
            Enabled = true,
            PinnedOrg = pinnedOrg,
            RepositoryName = ".github-private",
            AgentsDirectory = "agents",
            CacheTtl = TimeSpan.FromHours(1),
            DiscoveryTimeout = TimeSpan.FromSeconds(30),
            CacheBasePath = _testCacheDir
        };
    }

    private static void SetupOrgsResponse(MockProcessRunner mockRunner, params string[] orgs)
    {
        var output = string.Join("\n", orgs);
        mockRunner.SetupSuccess("gh", "api user/orgs --jq \".[].login\"", output);
    }

    private static void SetupOrgWithAgents(MockProcessRunner mockRunner, string org)
    {
        var listingJson = CreateFileListingJson(
            ("pch-visionary.md", "sha-visionary", 100),
            ("pch-planner.md", "sha-planner", 200));

        mockRunner.SetupSuccess("gh", $"api repos/{org}/.github-private/contents/agents", listingJson);
        mockRunner.SetupSuccess("gh", $"api repos/{org}/.github-private/contents/agents/pch-visionary.md",
            CreateFileContentJson("pch-visionary.md", "sha-visionary", "# Visionary Agent Prompt"));
        mockRunner.SetupSuccess("gh", $"api repos/{org}/.github-private/contents/agents/pch-planner.md",
            CreateFileContentJson("pch-planner.md", "sha-planner", "# Planner Agent Prompt"));
    }

    private static void SetupNoAgents(MockProcessRunner mockRunner, string org)
    {
        // Return empty array or no pch-*.md files
        mockRunner.SetupSuccess("gh", $"api repos/{org}/.github-private/contents/agents", "[]");
    }

    private static string CreateFileListingJson(params (string name, string sha, long size)[] files)
    {
        var entries = files.Select(f => new
        {
            name = f.name,
            path = $"agents/{f.name}",
            sha = f.sha,
            size = f.size,
            type = "file",
            download_url = $"https://raw.githubusercontent.com/TestOrg/.github-private/main/agents/{f.name}"
        });
        return JsonSerializer.Serialize(entries, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
    }

    private static string CreateFileContentJson(string name, string sha, string content)
    {
        var base64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(content));
        var entry = new
        {
            name,
            path = $"agents/{name}",
            sha,
            size = (long)content.Length,
            type = "file",
            content = base64,
            encoding = "base64"
        };
        return JsonSerializer.Serialize(entry, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower });
    }

    #endregion
}
