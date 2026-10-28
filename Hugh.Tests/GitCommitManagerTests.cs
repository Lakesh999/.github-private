using Hugh.Configuration;
using Hugh.Orchestration;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for GitCommitManager.
/// Tests commit message formatting, config-disabled behavior, and error handling.
/// </summary>
public class GitCommitManagerTests : IDisposable
{
    private readonly string _testDir;
    private readonly ILogger _logger;

    public GitCommitManagerTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"GitCommitTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _logger = Log.Logger;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    #region BuildCommitMessage Tests

    [Fact]
    public void BuildCommitMessage_StandardFormat()
    {
        var config = new GitConfig { CommitMessagePrefix = "[Hugh]" };
        var manager = new GitCommitManager(config, _logger);

        var message = manager.BuildCommitMessage(3, "Add API Endpoints", "Implemented REST API");

        Assert.Equal("[Hugh] Phase 3: Add API Endpoints — Implemented REST API", message);
    }

    [Fact]
    public void BuildCommitMessage_CustomPrefix()
    {
        var config = new GitConfig { CommitMessagePrefix = "[Auto]" };
        var manager = new GitCommitManager(config, _logger);

        var message = manager.BuildCommitMessage(1, "Setup", "Initial setup");

        Assert.Equal("[Auto] Phase 1: Setup — Initial setup", message);
    }

    [Fact]
    public void BuildCommitMessage_TruncatesLongSummary()
    {
        var config = new GitConfig { CommitMessagePrefix = "[Hugh]" };
        var manager = new GitCommitManager(config, _logger);

        var longSummary = new string('x', 200);
        var message = manager.BuildCommitMessage(1, "Big Phase", longSummary);

        // Summary truncated to 100 chars + "..."
        Assert.Contains("...", message);
        // Total message should be reasonable length
        Assert.True(message.Length < 200);
    }

    [Fact]
    public void BuildCommitMessage_ShortSummaryNotTruncated()
    {
        var config = new GitConfig { CommitMessagePrefix = "[Hugh]" };
        var manager = new GitCommitManager(config, _logger);

        var message = manager.BuildCommitMessage(2, "Tests", "Added unit tests");

        Assert.DoesNotContain("...", message);
        Assert.Equal("[Hugh] Phase 2: Tests — Added unit tests", message);
    }

    #endregion

    #region CommitPhaseAsync Tests

    [Fact]
    public async Task CommitPhaseAsync_ConfigDisabled_SkipsCommit()
    {
        var config = new GitConfig { CommitPerPhase = false };
        var manager = new GitCommitManager(config, _logger);

        var result = await manager.CommitPhaseAsync(1, "Test Phase", "summary");

        Assert.False(result);
    }

    [Fact]
    public async Task CommitPhaseAsync_NoChanges_ReturnsFalse()
    {
        // Use a testable subclass that simulates "no changes"
        var config = new GitConfig { CommitPerPhase = true };
        var manager = new TestableGitCommitManager(config, _logger, hasChanges: false);

        var result = await manager.CommitPhaseAsync(1, "Test Phase", "summary");

        Assert.False(result);
    }

    [Fact]
    public async Task CommitPhaseAsync_WithChanges_CreatesCommit()
    {
        var config = new GitConfig { CommitPerPhase = true };
        var manager = new TestableGitCommitManager(config, _logger, hasChanges: true, commitSuccess: true);

        var result = await manager.CommitPhaseAsync(1, "Test Phase", "summary");

        Assert.True(result);
        Assert.Contains("add -A", manager.ExecutedCommands);
        Assert.True(manager.ExecutedCommands.Any(c => c.StartsWith("commit -m")));
    }

    [Fact]
    public async Task CommitPhaseAsync_CommitFails_ReturnsFalse()
    {
        var config = new GitConfig { CommitPerPhase = true };
        var manager = new TestableGitCommitManager(config, _logger, hasChanges: true, commitSuccess: false);

        var result = await manager.CommitPhaseAsync(1, "Test Phase", "summary");

        Assert.False(result);
    }

    [Fact]
    public async Task CommitPhaseAsync_NothingToCommitOutput_ReturnsFalse()
    {
        var config = new GitConfig { CommitPerPhase = true };
        var manager = new TestableGitCommitManager(
            config, _logger, hasChanges: true, commitSuccess: false,
            commitOutput: "nothing to commit, working tree clean");

        var result = await manager.CommitPhaseAsync(1, "Test Phase", "summary");

        // Should not report as an error — graceful handling
        Assert.False(result);
    }

    #endregion

    #region GetCurrentCommitShaAsync Tests

    [Fact]
    public async Task GetCurrentCommitShaAsync_ReturnsTrimedSha()
    {
        var config = new GitConfig();
        var manager = new TestableGitCommitManager(config, _logger,
            revParseSha: "abc123def456\n");

        var sha = await manager.GetCurrentCommitShaAsync();

        Assert.Equal("abc123def456", sha);
    }

    [Fact]
    public async Task GetCurrentCommitShaAsync_GitFails_ReturnsNull()
    {
        var config = new GitConfig();
        var manager = new TestableGitCommitManager(config, _logger, revParseFails: true);

        var sha = await manager.GetCurrentCommitShaAsync();

        Assert.Null(sha);
    }

    #endregion

    #region GetDiffSummaryAsync Tests

    [Fact]
    public async Task GetDiffSummaryAsync_WithBaseRef_UsesBaseRef()
    {
        var config = new GitConfig();
        var manager = new TestableGitCommitManager(config, _logger, diffOutput: "M\tsrc/file.cs");

        var result = await manager.GetDiffSummaryAsync(baseRef: "abc123");

        Assert.Equal("M\tsrc/file.cs", result);
        Assert.Contains("diff --name-status abc123", manager.ExecutedCommands);
    }

    [Fact]
    public async Task GetDiffSummaryAsync_NoBaseRef_UsesHeadTilde1()
    {
        var config = new GitConfig();
        var manager = new TestableGitCommitManager(config, _logger, diffOutput: "A\tnew-file.cs");

        var result = await manager.GetDiffSummaryAsync();

        Assert.Equal("A\tnew-file.cs", result);
        Assert.Contains("diff --name-status HEAD~1", manager.ExecutedCommands);
    }

    [Fact]
    public async Task GetDiffSummaryAsync_GitFails_ReturnsEmpty()
    {
        var config = new GitConfig();
        var manager = new TestableGitCommitManager(config, _logger, diffFails: true);

        var result = await manager.GetDiffSummaryAsync();

        Assert.Equal(string.Empty, result);
    }

    #endregion

    /// <summary>
    /// Testable subclass that intercepts git commands instead of running them.
    /// </summary>
    private class TestableGitCommitManager : GitCommitManager
    {
        private readonly bool _hasChanges;
        private readonly bool _commitSuccess;
        private readonly string? _commitOutput;
        private readonly string? _revParseSha;
        private readonly bool _revParseFails;
        private readonly string? _diffOutput;
        private readonly bool _diffFails;

        public List<string> ExecutedCommands { get; } = new();

        public TestableGitCommitManager(
            GitConfig config,
            ILogger logger,
            bool hasChanges = false,
            bool commitSuccess = true,
            string? commitOutput = null,
            string? revParseSha = null,
            bool revParseFails = false,
            string? diffOutput = null,
            bool diffFails = false)
            : base(config, logger)
        {
            _hasChanges = hasChanges;
            _commitSuccess = commitSuccess;
            _commitOutput = commitOutput;
            _revParseSha = revParseSha;
            _revParseFails = revParseFails;
            _diffOutput = diffOutput;
            _diffFails = diffFails;
        }

        protected override Task<(bool Success, string Output)> RunGitCommandAsync(
            string arguments, CancellationToken ct)
        {
            ExecutedCommands.Add(arguments);

            // Route based on command
            if (arguments.StartsWith("status --porcelain"))
            {
                return Task.FromResult(
                    (true, _hasChanges ? "M src/file.cs" : ""));
            }

            if (arguments.StartsWith("add"))
            {
                return Task.FromResult((true, ""));
            }

            if (arguments.StartsWith("commit"))
            {
                var output = _commitOutput ?? (_commitSuccess ? "1 file changed" : "error: failed");
                return Task.FromResult((_commitSuccess, output));
            }

            if (arguments.StartsWith("rev-parse"))
            {
                if (_revParseFails)
                    return Task.FromResult((false, "fatal: not a git repository"));
                return Task.FromResult((true, _revParseSha ?? "abc123\n"));
            }

            if (arguments.StartsWith("diff"))
            {
                if (_diffFails)
                    return Task.FromResult((false, "fatal: bad revision"));
                return Task.FromResult((true, _diffOutput ?? ""));
            }

            return Task.FromResult((false, $"Unknown command: {arguments}"));
        }
    }
}
