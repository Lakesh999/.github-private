using Hugh.Agents.Discovery;
using Hugh.Setup.Detection;

namespace Hugh.Tests.Agents.Discovery;

/// <summary>
/// Unit tests for AgentDiscoveryDetector.
/// Tests detection of agent availability via GitHub discovery cache and bundled fallback.
/// </summary>
public class AgentDiscoveryDetectorTests : IDisposable
{
    private readonly string _testDir;

    public AgentDiscoveryDetectorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"hugh-detector-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); }
            catch { /* cleanup best-effort */ }
        }
    }

    #region Properties

    [Fact]
    public void PrerequisiteId_ReturnsAgentDiscovery()
    {
        var detector = CreateDetector();
        Assert.Equal("agent-discovery", detector.PrerequisiteId);
    }

    [Fact]
    public void DisplayName_ReturnsAgentPromptDiscovery()
    {
        var detector = CreateDetector();
        Assert.Equal("Agent Prompt Discovery", detector.DisplayName);
    }

    [Fact]
    public void Priority_Returns5()
    {
        var detector = CreateDetector();
        Assert.Equal(5, detector.Priority);
    }

    [Fact]
    public void IsInstallable_ReturnsTrue()
    {
        var detector = CreateDetector();
        Assert.True(detector.IsInstallable);
    }

    #endregion

    #region Discovery Disabled

    [Fact]
    public async Task DetectAsync_WhenDiscoveryDisabled_ChecksBundledAgents()
    {
        var bundledPath = CreateBundledAgents();
        var config = new AgentDiscoveryConfig { Enabled = false };
        var detector = new AgentDiscoveryDetector(config, bundledPath);

        var result = await detector.DetectAsync();

        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("bundled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WhenDisabledAndNoBundledAgents_ReturnsMissing()
    {
        var emptyPath = Path.Combine(_testDir, "no-agents");
        var config = new AgentDiscoveryConfig { Enabled = false };
        var detector = new AgentDiscoveryDetector(config, emptyPath);

        var result = await detector.DetectAsync();

        Assert.Equal(PrerequisiteState.Missing, result.State);
    }

    #endregion

    #region Bundled Agents Fallback

    [Fact]
    public async Task DetectAsync_WithAllBundledAgents_ReturnsSatisfied()
    {
        var bundledPath = CreateBundledAgents();
        var emptyCachePath = Path.Combine(_testDir, "empty-cache");
        Directory.CreateDirectory(emptyCachePath);
        var config = new AgentDiscoveryConfig { Enabled = true, CacheBasePath = emptyCachePath };
        var detector = new AgentDiscoveryDetector(config, bundledPath);

        var result = await detector.DetectAsync();

        Assert.Equal(PrerequisiteState.Satisfied, result.State);
        Assert.Contains("bundled", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DetectAsync_WithPartialBundledAgents_ReturnsMisconfigured()
    {
        var bundledPath = Path.Combine(_testDir, "partial-agents");
        Directory.CreateDirectory(bundledPath);
        File.WriteAllText(Path.Combine(bundledPath, "pch-visionary.md"), "content");
        File.WriteAllText(Path.Combine(bundledPath, "pch-planner.md"), "content");
        // Missing the other 4 agents

        var emptyCachePath = Path.Combine(_testDir, "empty-cache-partial");
        Directory.CreateDirectory(emptyCachePath);
        var config = new AgentDiscoveryConfig { Enabled = true, CacheBasePath = emptyCachePath };
        var detector = new AgentDiscoveryDetector(config, bundledPath);

        var result = await detector.DetectAsync();

        Assert.Equal(PrerequisiteState.Misconfigured, result.State);
        Assert.Contains("2/6", result.Message);
    }

    [Fact]
    public async Task DetectAsync_WithEmptyBundledDir_ReturnsMissing()
    {
        var bundledPath = Path.Combine(_testDir, "empty-agents");
        Directory.CreateDirectory(bundledPath);

        var emptyCachePath = Path.Combine(_testDir, "empty-cache-empty");
        Directory.CreateDirectory(emptyCachePath);
        var config = new AgentDiscoveryConfig { Enabled = true, CacheBasePath = emptyCachePath };
        var detector = new AgentDiscoveryDetector(config, bundledPath);

        var result = await detector.DetectAsync();

        Assert.Equal(PrerequisiteState.Missing, result.State);
    }

    [Fact]
    public async Task DetectAsync_WithNoBundledDir_ReturnsMissing()
    {
        var emptyCachePath = Path.Combine(_testDir, "empty-cache-nodir");
        Directory.CreateDirectory(emptyCachePath);
        var config = new AgentDiscoveryConfig { Enabled = true, CacheBasePath = emptyCachePath };
        var detector = new AgentDiscoveryDetector(config, Path.Combine(_testDir, "nonexistent"));

        var result = await detector.DetectAsync();

        Assert.Equal(PrerequisiteState.Missing, result.State);
        Assert.NotNull(result.Hints);
        Assert.True(result.Hints.Length > 0);
    }

    #endregion

    #region Constructor

    [Fact]
    public void Constructor_WithNullConfig_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AgentDiscoveryDetector(null!, "path"));
    }

    [Fact]
    public void Constructor_WithNullPath_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new AgentDiscoveryDetector(new AgentDiscoveryConfig(), null!));
    }

    #endregion

    #region Helpers

    private AgentDiscoveryDetector CreateDetector(AgentDiscoveryConfig? config = null, string? bundledPath = null)
    {
        return new AgentDiscoveryDetector(
            config ?? new AgentDiscoveryConfig { Enabled = true },
            bundledPath ?? Path.Combine(_testDir, "agents"));
    }

    private string CreateBundledAgents()
    {
        var path = Path.Combine(_testDir, "bundled-agents");
        Directory.CreateDirectory(path);

        var agents = new[]
        {
            "pch-coder.md", "pch-helper.md", "pch-plan-reviewer.md",
            "pch-planner.md", "pch-researcher.md", "pch-visionary.md"
        };

        foreach (var agent in agents)
        {
            File.WriteAllText(Path.Combine(path, agent), $"# {agent} content");
        }

        return path;
    }

    #endregion
}
