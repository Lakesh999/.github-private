using Hugh.Tools;
using Serilog;

namespace Hugh.Tests;

public class HughWorkflowToolsTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly string _tempDir;
    private readonly HughWorkflowTools _tools;

    public HughWorkflowToolsTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();

        // Create a temp directory for workspace structure tests
        _tempDir = Path.Combine(Path.GetTempPath(), $"hugh_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        _tools = new HughWorkflowTools(_logger, _tempDir);
    }

    public void Dispose()
    {
        // Clean up temp directory
        if (Directory.Exists(_tempDir))
        {
            try
            {
                Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    #region NotifyPhaseComplete Tests

    [Fact]
    public void NotifyPhaseComplete_ReturnsAcknowledged()
    {
        // Act
        var result = _tools.NotifyPhaseComplete(1, "Phase 1 complete", null);

        // Assert
        Assert.True(result.Acknowledged);
    }

    [Fact]
    public void NotifyPhaseComplete_SetsNextPhaseNumber()
    {
        // Act
        var result = _tools.NotifyPhaseComplete(3, "Phase 3 complete", null);

        // Assert
        Assert.Equal(4, result.NextPhaseNumber);
    }

    [Fact]
    public void NotifyPhaseComplete_IncludesPhaseInMessage()
    {
        // Act
        var result = _tools.NotifyPhaseComplete(2, "Summary", "/docs/output.md");

        // Assert
        Assert.Contains("Phase 2", result.Message);
    }

    #endregion

    #region NotifyWorkflowComplete Tests

    [Fact]
    public void NotifyWorkflowComplete_ReturnsAcknowledged()
    {
        // Act
        var result = _tools.NotifyWorkflowComplete("All done", null);

        // Assert
        Assert.True(result.Acknowledged);
    }

    [Fact]
    public void NotifyWorkflowComplete_ReturnsAcknowledgementMessage()
    {
        // Act
        var result = _tools.NotifyWorkflowComplete("Feature implemented", new List<string> { "/docs/plan.md" });

        // Assert
        Assert.Contains("acknowledged", result.Message.ToLower());
    }

    #endregion

    #region ReportBlocker Tests

    [Fact]
    public void ReportBlocker_ReturnsPausedAction()
    {
        // Act
        var result = _tools.ReportBlocker("Test blocker", "high", false, null);

        // Assert
        Assert.Equal("paused", result.Action);
    }

    [Fact]
    public void ReportBlocker_ReturnsAcknowledged()
    {
        // Act
        var result = _tools.ReportBlocker("Test blocker", "critical", true, new List<string> { "Fix the issue" });

        // Assert
        Assert.True(result.Acknowledged);
    }

    [Fact]
    public void ReportBlocker_IncludesSkippableInMessage()
    {
        // Act
        var result = _tools.ReportBlocker("Test blocker", "low", true, null);

        // Assert
        Assert.Contains("Skippable", result.Message);
    }

    #endregion

    #region GetWorkspaceStructure Tests

    [Fact]
    public void GetWorkspaceStructure_ReturnsTree()
    {
        // Arrange
        Directory.CreateDirectory(Path.Combine(_tempDir, "subdir"));
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "content");

        // Act
        var result = _tools.GetWorkspaceStructure();

        // Assert
        Assert.NotEmpty(result.DirectoryTree);
        Assert.Contains("subdir", result.DirectoryTree);
        Assert.Contains("file1.txt", result.DirectoryTree);
    }

    [Fact]
    public void GetWorkspaceStructure_RespectsMaxDepth()
    {
        // Arrange
        var level1 = Path.Combine(_tempDir, "level1");
        var level2 = Path.Combine(level1, "level2");
        var level3 = Path.Combine(level2, "level3");
        Directory.CreateDirectory(level3);
        File.WriteAllText(Path.Combine(level3, "deep.txt"), "content");

        // Act - maxDepth of 1 should not show level3 contents
        var result = _tools.GetWorkspaceStructure(maxDepth: 1);

        // Assert
        Assert.Contains("level1", result.DirectoryTree);
        Assert.Contains("level2", result.DirectoryTree); // level2 is at depth 1
        Assert.DoesNotContain("deep.txt", result.DirectoryTree); // deep.txt is at depth 3
    }

    [Fact]
    public void GetWorkspaceStructure_WithFilter_LimitsScope()
    {
        // Arrange
        var subdir1 = Path.Combine(_tempDir, "subdir1");
        var subdir2 = Path.Combine(_tempDir, "subdir2");
        Directory.CreateDirectory(subdir1);
        Directory.CreateDirectory(subdir2);
        File.WriteAllText(Path.Combine(subdir1, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(subdir2, "file2.txt"), "content2");

        // Act
        var result = _tools.GetWorkspaceStructure(filterPath: "subdir1");

        // Assert
        Assert.Contains("file1.txt", result.DirectoryTree);
        Assert.DoesNotContain("subdir2", result.DirectoryTree);
        Assert.DoesNotContain("file2.txt", result.DirectoryTree);
    }

    [Fact]
    public void GetWorkspaceStructure_CountsFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(_tempDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(_tempDir, "file2.txt"), "content2");
        File.WriteAllText(Path.Combine(_tempDir, "file3.txt"), "content3");

        // Act
        var result = _tools.GetWorkspaceStructure();

        // Assert
        Assert.Equal(3, result.FileCount);
    }

    [Fact]
    public void GetWorkspaceStructure_ReturnsRootPath()
    {
        // Act
        var result = _tools.GetWorkspaceStructure();

        // Assert
        Assert.Equal(_tempDir, result.RootPath);
    }

    [Fact]
    public void GetWorkspaceStructure_WithInvalidFilter_ReturnsNotFoundMessage()
    {
        // Act
        var result = _tools.GetWorkspaceStructure(filterPath: "nonexistent");

        // Assert
        Assert.Contains("not found", result.DirectoryTree.ToLower());
        Assert.Equal(0, result.FileCount);
    }

    #endregion

    #region CreateToolSet Tests

    [Fact]
    public void CreateToolSet_ReturnsFourTools()
    {
        // Act
        var tools = _tools.CreateToolSet();

        // Assert
        Assert.Equal(4, tools.Count);
    }

    [Fact]
    public void CreateToolSet_ToolNamesHaveHughPrefix()
    {
        // Act
        var tools = _tools.CreateToolSet();

        // Assert
        foreach (var tool in tools)
        {
            Assert.StartsWith("hugh_", tool.Name);
        }
    }

    [Fact]
    public void CreateToolSet_ContainsExpectedToolNames()
    {
        // Act
        var tools = _tools.CreateToolSet();
        var toolNames = tools.Select(t => t.Name).ToList();

        // Assert
        Assert.Contains("hugh_notify_phase_complete", toolNames);
        Assert.Contains("hugh_notify_workflow_complete", toolNames);
        Assert.Contains("hugh_report_blocker", toolNames);
        Assert.Contains("hugh_get_workspace_structure", toolNames);
    }

    [Fact]
    public void CreateToolSet_ToolsHaveDescriptions()
    {
        // Act
        var tools = _tools.CreateToolSet();

        // Assert
        foreach (var tool in tools)
        {
            Assert.False(string.IsNullOrEmpty(tool.Description), $"Tool {tool.Name} should have a description");
        }
    }

    #endregion

    #region Constructor Tests

    [Fact]
    public void Constructor_ThrowsOnNullLogger()
    {
        Assert.Throws<ArgumentNullException>(() => new HughWorkflowTools(null!, _tempDir));
    }

    [Fact]
    public void Constructor_ThrowsOnNullWorkspacePath()
    {
        Assert.Throws<ArgumentNullException>(() => new HughWorkflowTools(_logger, null!));
    }

    #endregion
}
