using System.Text.Json;
using GitHub.Copilot.SDK;
using ILogger = Serilog.ILogger;

namespace Hugh.Hooks;

/// <summary>
/// Provides file system safety hooks for the Copilot SDK.
/// Intercepts file operations to auto-create directories and monitor path usage.
/// </summary>
public class FileSystemHooks
{
    private readonly ILogger _logger;
    private readonly string _workspacePath;
    private readonly string _agentName;
    private readonly Dictionary<string, string[]> _expectedPaths;

    /// <summary>
    /// File-writing tool names that should trigger directory auto-creation.
    /// </summary>
    private static readonly HashSet<string> FileWriteTools = new(StringComparer.OrdinalIgnoreCase)
    {
        "create_file",
        "replace_string_in_file",
        "multi_replace_string_in_file"
    };

    /// <summary>
    /// Creates FileSystemHooks with default expected path mappings.
    /// </summary>
    /// <param name="logger">Serilog logger for diagnostics.</param>
    /// <param name="workspacePath">Root workspace path for relative path resolution.</param>
    public FileSystemHooks(ILogger logger, string workspacePath)
        : this(logger, workspacePath, string.Empty)
    {
    }

    /// <summary>
    /// Creates FileSystemHooks with agent-specific path monitoring.
    /// </summary>
    /// <param name="logger">Serilog logger for diagnostics.</param>
    /// <param name="workspacePath">Root workspace path for relative path resolution.</param>
    /// <param name="agentName">Agent name for expected path validation.</param>
    public FileSystemHooks(ILogger logger, string workspacePath, string agentName)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
        _agentName = agentName ?? string.Empty;
        _expectedPaths = CreateExpectedPaths();
    }

    /// <summary>
    /// Creates the default expected path mappings for PCH agents.
    /// </summary>
    /// <returns>Dictionary mapping agent names to expected write path prefixes.</returns>
    internal static Dictionary<string, string[]> CreateExpectedPaths()
    {
        return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["pch-researcher"] = new[] { "docs/research/" },
            ["pch-planner"] = new[] { "docs/plans/" },
            ["pch-visionary"] = new[] { "docs/vision/" },
            ["pch-plan-reviewer"] = new[] { "docs/plans/" },
            ["pch-coder"] = Array.Empty<string>(),  // No restrictions
            ["pch-helper"] = Array.Empty<string>()  // No expected writes
        };
    }

    /// <summary>
    /// Pre-tool-use hook that ensures directories exist and monitors path usage.
    /// </summary>
    public async Task<PreToolUseHookOutput?> HandlePreToolUseAsync(
        PreToolUseHookInput input,
        HookInvocation invocation)
    {
        try
        {
            var toolName = GetToolName(input);
            
            if (FileWriteTools.Contains(toolName))
            {
                var filePath = ExtractFilePath(toolName, input);
                
                if (!string.IsNullOrEmpty(filePath))
                {
                    EnsureDirectoryExists(filePath);
                    CheckExpectedPath(filePath);
                }
            }
        }
        catch (Exception ex)
        {
            // Log but don't block - hooks should be resilient
            _logger.Warning(ex, "Error in pre-tool-use hook, allowing tool execution");
        }

        // Always allow - this hook is for safety, not blocking
        return await Task.FromResult(new PreToolUseHookOutput
        {
            PermissionDecision = "allow"
        });
    }

    /// <summary>
    /// Post-tool-use hook for completion logging.
    /// </summary>
    public async Task<PostToolUseHookOutput?> HandlePostToolUseAsync(
        PostToolUseHookInput input,
        HookInvocation invocation)
    {
        try
        {
            var toolName = GetToolName(input);
            _logger.Debug("Tool {ToolName} completed", toolName);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error in post-tool-use hook");
        }

        return await Task.FromResult(new PostToolUseHookOutput());
    }

    /// <summary>
    /// Creates SessionHooks configured with this instance's handlers.
    /// </summary>
    public SessionHooks CreateHooks()
    {
        return new SessionHooks
        {
            OnPreToolUse = HandlePreToolUseAsync,
            OnPostToolUse = HandlePostToolUseAsync
        };
    }

    /// <summary>
    /// Extracts the file path from tool arguments based on tool type.
    /// </summary>
    /// <param name="toolName">Name of the tool being invoked.</param>
    /// <param name="input">Pre-tool hook input containing tool arguments.</param>
    /// <returns>File path if found, null otherwise.</returns>
    internal string? ExtractFilePath(string toolName, PreToolUseHookInput input)
    {
        try
        {
            var args = GetToolArgs(input);
            if (args == null || !args.HasValue)
            {
                return null;
            }

            return ExtractFilePathFromArgs(toolName, args.Value);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to extract file path from {ToolName} arguments", toolName);
            return null;
        }
    }

    /// <summary>
    /// Extracts file path from tool arguments (JsonElement version for unit testing).
    /// </summary>
    /// <param name="toolName">Name of the tool being invoked.</param>
    /// <param name="toolArgs">Tool arguments as JsonElement.</param>
    /// <returns>File path if found, null otherwise.</returns>
    internal string? ExtractFilePathFromArgs(string toolName, JsonElement toolArgs)
    {
        try
        {
            if (string.Equals(toolName, "multi_replace_string_in_file", StringComparison.OrdinalIgnoreCase))
            {
                // Extract from replacements array
                if (toolArgs.TryGetProperty("replacements", out var replacements) &&
                    replacements.ValueKind == JsonValueKind.Array)
                {
                    foreach (var replacement in replacements.EnumerateArray())
                    {
                        if (replacement.TryGetProperty("filePath", out var pathProp))
                        {
                            var path = pathProp.GetString();
                            if (!string.IsNullOrEmpty(path))
                            {
                                return path;
                            }
                        }
                    }
                }
            }
            else
            {
                // Single filePath property
                if (toolArgs.TryGetProperty("filePath", out var filePathProp))
                {
                    return filePathProp.GetString();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to parse tool arguments for {ToolName}", toolName);
        }

        return null;
    }

    /// <summary>
    /// Ensures the parent directory exists for the given file path.
    /// </summary>
    /// <param name="filePath">File path whose parent directory should exist.</param>
    internal void EnsureDirectoryExists(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return;
        }

        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.Information("Auto-created directory for file operation: {DirPath}", directory);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to create directory for {FilePath}", filePath);
        }
    }

    /// <summary>
    /// Checks if a file path matches expected paths for the current agent.
    /// Logs a warning if the path is unexpected, but never blocks.
    /// </summary>
    /// <param name="filePath">File path to check.</param>
    internal void CheckExpectedPath(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrEmpty(_agentName))
        {
            return;
        }

        if (!IsExpectedPath(_agentName, filePath))
        {
            var expectedPathsList = _expectedPaths.TryGetValue(_agentName, out var paths)
                ? string.Join(", ", paths)
                : "unknown";
            
            _logger.Warning(
                "Agent {AgentName} writing to unexpected path: {FilePath} (expected: {ExpectedPaths})",
                _agentName,
                filePath,
                expectedPathsList);
        }
    }

    /// <summary>
    /// Determines if a file path is expected for the given agent.
    /// </summary>
    /// <param name="agentName">Name of the agent.</param>
    /// <param name="filePath">File path to check.</param>
    /// <returns>True if the path is expected or the agent has no restrictions.</returns>
    internal bool IsExpectedPath(string agentName, string filePath)
    {
        if (string.IsNullOrEmpty(agentName) || string.IsNullOrEmpty(filePath))
        {
            return true;  // No agent or path = no restrictions
        }

        if (!_expectedPaths.TryGetValue(agentName, out var expectedPrefixes))
        {
            return true;  // Unknown agent = no restrictions
        }

        if (expectedPrefixes.Length == 0)
        {
            return true;  // Empty prefixes = no restrictions
        }

        // Normalize path for comparison
        var normalizedPath = filePath.Replace('\\', '/');
        
        // Check if the workspace path is in the file path and extract relative
        var normalizedWorkspace = _workspacePath.Replace('\\', '/').TrimEnd('/') + "/";
        if (normalizedPath.StartsWith(normalizedWorkspace, StringComparison.OrdinalIgnoreCase))
        {
            normalizedPath = normalizedPath.Substring(normalizedWorkspace.Length);
        }

        foreach (var prefix in expectedPrefixes)
        {
            var normalizedPrefix = prefix.Replace('\\', '/');
            if (normalizedPath.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the tool name from the hook input.
    /// </summary>
    private static string GetToolName(PreToolUseHookInput input)
    {
        // Access ToolName property - SDK provides this
        return input.ToolName ?? string.Empty;
    }

    /// <summary>
    /// Gets the tool name from the post-tool hook input.
    /// </summary>
    private static string GetToolName(PostToolUseHookInput input)
    {
        return input.ToolName ?? string.Empty;
    }

    /// <summary>
    /// Gets tool arguments as JsonElement from the hook input.
    /// </summary>
    private static JsonElement? GetToolArgs(PreToolUseHookInput input)
    {
        // SDK provides ToolArgs - the type may vary, so we handle JsonElement
        if (input.ToolArgs is JsonElement jsonElement)
        {
            return jsonElement;
        }

        // If it's a string, try to parse it
        if (input.ToolArgs is string jsonString && !string.IsNullOrEmpty(jsonString))
        {
            try
            {
                return JsonSerializer.Deserialize<JsonElement>(jsonString);
            }
            catch
            {
                return null;
            }
        }

        // Try to serialize and re-parse for other types
        if (input.ToolArgs != null)
        {
            try
            {
                var json = JsonSerializer.Serialize(input.ToolArgs);
                return JsonSerializer.Deserialize<JsonElement>(json);
            }
            catch
            {
                return null;
            }
        }

        return null;
    }
}
