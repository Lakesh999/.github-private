using System.Text.Json;
using Hugh.Hooks;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for FileSystemHooks — verifies directory creation and path monitoring.
/// </summary>
public class FileSystemHooksTests
{
    private readonly FileSystemHooks _hooks;
    private readonly string _tempDir;

    public FileSystemHooksTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "hugh_test_" + Guid.NewGuid().ToString("N")[..8]);
        _hooks = new FileSystemHooks(Log.Logger, _tempDir, "pch-coder");
    }

    #region ExtractFilePathFromArgs Tests

    [Fact]
    public void ExtractFilePathFromArgs_CreateFile_ReturnsFilePath()
    {
        // Arrange
        var args = JsonSerializer.Deserialize<JsonElement>("""
            {
                "filePath": "/docs/test.md",
                "content": "test content"
            }
            """);

        // Act
        var result = _hooks.ExtractFilePathFromArgs("create_file", args);

        // Assert
        Assert.Equal("/docs/test.md", result);
    }

    [Fact]
    public void ExtractFilePathFromArgs_ReplaceStringInFile_ReturnsFilePath()
    {
        // Arrange
        var args = JsonSerializer.Deserialize<JsonElement>("""
            {
                "filePath": "/src/app.cs",
                "oldString": "old",
                "newString": "new"
            }
            """);

        // Act
        var result = _hooks.ExtractFilePathFromArgs("replace_string_in_file", args);

        // Assert
        Assert.Equal("/src/app.cs", result);
    }

    [Fact]
    public void ExtractFilePathFromArgs_MultiReplace_ReturnsFirstFilePath()
    {
        // Arrange
        var args = JsonSerializer.Deserialize<JsonElement>("""
            {
                "replacements": [
                    { "filePath": "/first/file.cs", "oldString": "a", "newString": "b" },
                    { "filePath": "/second/file.cs", "oldString": "c", "newString": "d" }
                ]
            }
            """);

        // Act
        var result = _hooks.ExtractFilePathFromArgs("multi_replace_string_in_file", args);

        // Assert
        Assert.Equal("/first/file.cs", result);
    }

    [Fact]
    public void ExtractFilePathFromArgs_UnknownTool_ReturnsNull()
    {
        // Arrange
        var args = JsonSerializer.Deserialize<JsonElement>("""
            {
                "someProperty": "value"
            }
            """);

        // Act
        var result = _hooks.ExtractFilePathFromArgs("unknown_tool", args);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFilePathFromArgs_MalformedArgs_ReturnsNull()
    {
        // Arrange - empty object with no filePath
        var args = JsonSerializer.Deserialize<JsonElement>("{}");

        // Act
        var result = _hooks.ExtractFilePathFromArgs("create_file", args);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractFilePathFromArgs_MultiReplace_EmptyReplacements_ReturnsNull()
    {
        // Arrange
        var args = JsonSerializer.Deserialize<JsonElement>("""
            {
                "replacements": []
            }
            """);

        // Act
        var result = _hooks.ExtractFilePathFromArgs("multi_replace_string_in_file", args);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region EnsureDirectoryExists Tests

    [Fact]
    public void EnsureDirectoryExists_CreatesParentDirectory()
    {
        // Arrange
        var testDir = Path.Combine(_tempDir, "subdir", "nested");
        var filePath = Path.Combine(testDir, "test.txt");

        try
        {
            // Ensure directory doesn't exist
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, recursive: true);
            }

            // Act
            _hooks.EnsureDirectoryExists(filePath);

            // Assert
            Assert.True(Directory.Exists(testDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void EnsureDirectoryExists_ExistingDirectory_NoOp()
    {
        // Arrange
        var testDir = Path.Combine(_tempDir, "existing");
        var filePath = Path.Combine(testDir, "test.txt");
        Directory.CreateDirectory(testDir);

        try
        {
            // Act - should not throw
            _hooks.EnsureDirectoryExists(filePath);

            // Assert - directory still exists
            Assert.True(Directory.Exists(testDir));
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, recursive: true);
            }
        }
    }

    [Fact]
    public void EnsureDirectoryExists_NullPath_NoError()
    {
        // Act - should not throw
        var exception = Record.Exception(() => _hooks.EnsureDirectoryExists(null));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureDirectoryExists_EmptyPath_NoError()
    {
        // Act - should not throw
        var exception = Record.Exception(() => _hooks.EnsureDirectoryExists(string.Empty));

        // Assert
        Assert.Null(exception);
    }

    #endregion

    #region IsExpectedPath Tests

    [Fact]
    public void IsExpectedPath_Researcher_DocsResearch_ReturnsTrue()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-researcher");

        // Act
        var result = hooks.IsExpectedPath("pch-researcher", "docs/research/001-topic.md");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_Researcher_SrcCode_ReturnsFalse()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-researcher");

        // Act
        var result = hooks.IsExpectedPath("pch-researcher", "src/MyClass.cs");

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsExpectedPath_Coder_AnyPath_ReturnsTrue()
    {
        // Arrange - pch-coder has no restrictions
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-coder");

        // Act
        var result = hooks.IsExpectedPath("pch-coder", "src/anything/anywhere.cs");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_UnknownAgent_ReturnsTrue()
    {
        // Arrange - unknown agents have no restrictions
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "unknown-agent");

        // Act
        var result = hooks.IsExpectedPath("unknown-agent", "random/path/file.txt");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_Planner_DocsPlans_ReturnsTrue()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-planner");

        // Act
        var result = hooks.IsExpectedPath("pch-planner", "docs/plans/001-feature.md");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_Visionary_DocsVision_ReturnsTrue()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-visionary");

        // Act
        var result = hooks.IsExpectedPath("pch-visionary", "docs/vision/001-idea.md");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_WithAbsolutePath_ExtractsRelative()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-researcher");

        // Act
        var result = hooks.IsExpectedPath("pch-researcher", "/workspace/docs/research/topic.md");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_WindowsPath_NormalizesSlashes()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "C:\\workspace", "pch-planner");

        // Act
        var result = hooks.IsExpectedPath("pch-planner", "C:\\workspace\\docs\\plans\\feature.md");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_NullAgentName_ReturnsTrue()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-coder");

        // Act
        var result = hooks.IsExpectedPath(null!, "some/path.txt");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsExpectedPath_NullFilePath_ReturnsTrue()
    {
        // Arrange
        var hooks = new FileSystemHooks(Log.Logger, "/workspace", "pch-researcher");

        // Act
        var result = hooks.IsExpectedPath("pch-researcher", null!);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region CreateExpectedPaths Tests

    [Fact]
    public void CreateExpectedPaths_ReturnsAllAgents()
    {
        // Act
        var paths = FileSystemHooks.CreateExpectedPaths();

        // Assert
        Assert.True(paths.ContainsKey("pch-researcher"));
        Assert.True(paths.ContainsKey("pch-planner"));
        Assert.True(paths.ContainsKey("pch-visionary"));
        Assert.True(paths.ContainsKey("pch-plan-reviewer"));
        Assert.True(paths.ContainsKey("pch-coder"));
        Assert.True(paths.ContainsKey("pch-helper"));
    }

    [Fact]
    public void CreateExpectedPaths_CoderHasNoRestrictions()
    {
        // Act
        var paths = FileSystemHooks.CreateExpectedPaths();

        // Assert
        Assert.Empty(paths["pch-coder"]);
    }

    [Fact]
    public void CreateExpectedPaths_ResearcherHasDocsResearch()
    {
        // Act
        var paths = FileSystemHooks.CreateExpectedPaths();

        // Assert
        Assert.Contains("docs/research/", paths["pch-researcher"]);
    }

    #endregion

    #region CreateHooks Tests

    [Fact]
    public void CreateHooks_ReturnsSessionHooks()
    {
        // Act
        var sessionHooks = _hooks.CreateHooks();

        // Assert
        Assert.NotNull(sessionHooks);
        Assert.NotNull(sessionHooks.OnPreToolUse);
        Assert.NotNull(sessionHooks.OnPostToolUse);
    }

    #endregion
}
