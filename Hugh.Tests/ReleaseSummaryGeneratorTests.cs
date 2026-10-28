using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Models;
using Hugh.Orchestration;
using NSubstitute;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for ReleaseSummaryGenerator.
/// Tests summary generation via mocked CopilotService and GitCommitManager,
/// error handling, and directory creation behavior.
/// </summary>
public class ReleaseSummaryGeneratorTests : IDisposable
{
    private readonly string _testDir;
    private readonly ICopilotService _mockCopilotService;
    private readonly GitCommitManager _gitCommitManager;
    private readonly HughConfig _config;
    private readonly ILogger _logger;

    public ReleaseSummaryGeneratorTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"ReleaseSummaryTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
        _logger = Log.Logger;
        _mockCopilotService = Substitute.For<ICopilotService>();

        var gitConfig = new GitConfig();
        _gitCommitManager = new TestableGitCommitManager(gitConfig, _logger, diffOutput: "M\tsrc/file.cs");

        _config = new HughConfig
        {
            DocsPath = Path.Combine(_testDir, "docs")
        };
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); }
            catch { /* best-effort cleanup */ }
        }
    }

    #region GenerateSummaryAsync Tests

    [Fact]
    public async Task GenerateSummaryAsync_Success_CallsCopilotService()
    {
        // Arrange
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult { Success = true });

        // Create the expected output file to simulate agent writing it
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), _config.DocsPath, "releases");
        Directory.CreateDirectory(outputDir);
        var outputFile = Path.Combine(outputDir, "release-1-summary.md");
        await File.WriteAllTextAsync(outputFile, "# Release 1 Summary");

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        // Act
        var result = await generator.GenerateSummaryAsync(
            1, "Feature Alpha", planPath: null, researchPath: null,
            startingCommitSha: "abc123");

        // Assert
        Assert.NotNull(result);
        await _mockCopilotService.Received(1)
            .ExecutePhaseAsync(
                Arg.Is<AgentDefinition>(a =>
                    a.Name == "release-summary" &&
                    a.BuiltInPromptName == "release-summary"),
                Arg.Any<string>(),
                Arg.Is<string>(p => p.Contains("Release Number") && p.Contains("Feature Alpha")),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>());

        // Clean up the directory we created in CWD
        if (Directory.Exists(outputDir))
        {
            try { Directory.Delete(outputDir, recursive: true); }
            catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task GenerateSummaryAsync_CopilotFails_ReturnsNull()
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult { Success = false, ErrorMessage = "LLM session failed" });

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        var result = await generator.GenerateSummaryAsync(
            1, "Feature Alpha", planPath: null, researchPath: null,
            startingCommitSha: "abc123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateSummaryAsync_CopilotThrows_ReturnsNull()
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns<PhaseResult>(x => throw new InvalidOperationException("SDK failure"));

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        var result = await generator.GenerateSummaryAsync(
            1, "Feature Alpha", planPath: null, researchPath: null,
            startingCommitSha: "abc123");

        Assert.Null(result);
    }

    [Fact]
    public async Task GenerateSummaryAsync_NoStartingCommitSha_SkipsGitDiff()
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult { Success = true });

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        var result = await generator.GenerateSummaryAsync(
            2, "Feature Beta", planPath: null, researchPath: null,
            startingCommitSha: null);

        // Should still attempt generation, just without git diff
        await _mockCopilotService.Received(1)
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Is<string>(p => p.Contains("No git diff available")),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSummaryAsync_WithPlanAndResearch_AttachesBothDocuments()
    {
        // Create temp plan and research files
        var planPath = Path.Combine(_testDir, "plan.md");
        var researchPath = Path.Combine(_testDir, "research.md");
        await File.WriteAllTextAsync(planPath, "# Plan");
        await File.WriteAllTextAsync(researchPath, "# Research");

        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult { Success = true });

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        await generator.GenerateSummaryAsync(
            1, "Feature Alpha", planPath, researchPath,
            startingCommitSha: "abc123");

        // Verify both documents were attached
        await _mockCopilotService.Received(1)
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<IEnumerable<string>>(docs =>
                    docs.Contains(planPath) && docs.Contains(researchPath)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSummaryAsync_MissingDocuments_AttachesOnlyExisting()
    {
        // Plan exists but research doesn't
        var planPath = Path.Combine(_testDir, "plan.md");
        await File.WriteAllTextAsync(planPath, "# Plan");
        var researchPath = Path.Combine(_testDir, "nonexistent-research.md");

        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult { Success = true });

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        await generator.GenerateSummaryAsync(
            1, "Feature Alpha", planPath, researchPath,
            startingCommitSha: null);

        // Verify only plan was attached (research doesn't exist)
        await _mockCopilotService.Received(1)
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Is<IEnumerable<string>>(docs =>
                    docs.Contains(planPath) && !docs.Contains(researchPath)),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GenerateSummaryAsync_PromptContainsGitDiff()
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult { Success = true });

        var generator = new ReleaseSummaryGenerator(
            _mockCopilotService, _gitCommitManager, _config, _logger);

        await generator.GenerateSummaryAsync(
            1, "Feature Alpha", planPath: null, researchPath: null,
            startingCommitSha: "abc123");

        // Verify the user prompt contains the git diff content from TestableGitCommitManager
        await _mockCopilotService.Received(1)
            .ExecutePhaseAsync(
                Arg.Any<AgentDefinition>(),
                Arg.Any<string>(),
                Arg.Is<string>(p => p.Contains("M\tsrc/file.cs")),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>());
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_NullCopilotService_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReleaseSummaryGenerator(null!, _gitCommitManager, _config, _logger));
    }

    [Fact]
    public void Constructor_NullGitCommitManager_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReleaseSummaryGenerator(_mockCopilotService, null!, _config, _logger));
    }

    [Fact]
    public void Constructor_NullConfig_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReleaseSummaryGenerator(_mockCopilotService, _gitCommitManager, null!, _logger));
    }

    [Fact]
    public void Constructor_NullLogger_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new ReleaseSummaryGenerator(_mockCopilotService, _gitCommitManager, _config, null!));
    }

    #endregion

    #region EnsureDirectoryExists Tests

    [Fact]
    public void EnsureDirectoryExists_CreatesDirectory()
    {
        var newDir = Path.Combine(_testDir, "new-subdir");
        Assert.False(Directory.Exists(newDir));

        ReleaseSummaryGenerator.EnsureDirectoryExists(newDir);

        Assert.True(Directory.Exists(newDir));
    }

    [Fact]
    public void EnsureDirectoryExists_ExistingDirectory_NoError()
    {
        Assert.True(Directory.Exists(_testDir));

        // Should not throw
        ReleaseSummaryGenerator.EnsureDirectoryExists(_testDir);

        Assert.True(Directory.Exists(_testDir));
    }

    #endregion

    /// <summary>
    /// Testable subclass of GitCommitManager that intercepts git commands.
    /// </summary>
    private class TestableGitCommitManager : GitCommitManager
    {
        private readonly string? _diffOutput;

        public TestableGitCommitManager(
            GitConfig config, ILogger logger, string? diffOutput = null)
            : base(config, logger)
        {
            _diffOutput = diffOutput;
        }

        protected override Task<(bool Success, string Output)> RunGitCommandAsync(
            string arguments, CancellationToken ct)
        {
            if (arguments.StartsWith("diff"))
            {
                return Task.FromResult((true, _diffOutput ?? ""));
            }

            return Task.FromResult((true, ""));
        }
    }
}
