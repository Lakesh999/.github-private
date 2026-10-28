using System.Collections.Concurrent;
using System.Diagnostics;
using ILogger = Serilog.ILogger;

namespace Hugh.Metrics;

/// <summary>
/// Record of a single tool execution with timing and outcome.
/// </summary>
public sealed record ToolExecutionRecord
{
    /// <summary>
    /// The name of the tool that was executed.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Time taken to execute the tool.
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Whether the tool execution succeeded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Error message if the execution failed.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// When the tool execution completed.
    /// </summary>
    public DateTimeOffset Timestamp { get; init; }
}

/// <summary>
/// Statistics for a specific tool.
/// </summary>
public sealed record ToolStats
{
    /// <summary>
    /// Total number of executions.
    /// </summary>
    public int Count { get; init; }

    /// <summary>
    /// Number of successful executions.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Number of failed executions.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Average duration across all executions.
    /// </summary>
    public TimeSpan AvgDuration { get; init; }

    /// <summary>
    /// Maximum duration of any execution.
    /// </summary>
    public TimeSpan MaxDuration { get; init; }
}

/// <summary>
/// Summary of all tool execution metrics.
/// </summary>
public sealed record ToolMetricsSummary
{
    /// <summary>
    /// Total number of tool executions.
    /// </summary>
    public int TotalExecutions { get; init; }

    /// <summary>
    /// Total number of successful executions.
    /// </summary>
    public int SuccessCount { get; init; }

    /// <summary>
    /// Total number of failed executions.
    /// </summary>
    public int ErrorCount { get; init; }

    /// <summary>
    /// Statistics per tool name.
    /// </summary>
    public IReadOnlyDictionary<string, ToolStats> ByTool { get; init; } = 
        new Dictionary<string, ToolStats>();
}

/// <summary>
/// Collects metrics on tool execution for observability.
/// Thread-safe for concurrent access from event handlers.
/// </summary>
public sealed class ToolMetricsCollector
{
    private readonly ConcurrentDictionary<string, List<ToolExecutionRecord>> _toolExecutions = new();
    /// <summary>
    /// Maps tool call IDs (or tool names as fallback) to active stopwatches.
    /// Using call IDs prevents collisions when multiple concurrent invocations
    /// of the same tool (e.g., "edit") overlap.
    /// </summary>
    private readonly ConcurrentDictionary<string, Stopwatch> _activeTools = new();
    private readonly object _executionsLock = new();
    private readonly ILogger? _logger;

    /// <summary>
    /// Creates a new ToolMetricsCollector.
    /// </summary>
    public ToolMetricsCollector()
    {
    }

    /// <summary>
    /// Creates a new ToolMetricsCollector with logging.
    /// </summary>
    /// <param name="logger">Logger for diagnostic output.</param>
    public ToolMetricsCollector(ILogger? logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Records the start of a tool execution.
    /// </summary>
    /// <param name="toolName">Name of the tool starting execution.</param>
    /// <param name="toolCallId">Optional unique call ID for correlating start/complete events. 
    /// When provided, enables accurate tracking of concurrent invocations of the same tool.</param>
    public void OnToolStart(string toolName, string? toolCallId = null)
    {
        if (string.IsNullOrEmpty(toolName))
            return;

        var trackingKey = !string.IsNullOrEmpty(toolCallId) ? toolCallId : toolName;
        var stopwatch = Stopwatch.StartNew();
        _activeTools[trackingKey] = stopwatch;
        
        _logger?.Debug("Tool execution started: {ToolName} (TrackingKey: {Key})", toolName, trackingKey);
    }

    /// <summary>
    /// Records the completion of a tool execution.
    /// </summary>
    /// <param name="toolName">Name of the tool that completed.</param>
    /// <param name="success">Whether the execution succeeded.</param>
    /// <param name="error">Error message if execution failed.</param>
    /// <param name="toolCallId">Optional unique call ID for correlating with the start event.</param>
    public void OnToolComplete(string toolName, bool success, string? error = null, string? toolCallId = null)
    {
        if (string.IsNullOrEmpty(toolName))
            return;

        // Try call ID first, then fall back to tool name
        var trackingKey = !string.IsNullOrEmpty(toolCallId) ? toolCallId : toolName;
        TimeSpan duration;
        if (_activeTools.TryRemove(trackingKey, out var stopwatch))
        {
            stopwatch.Stop();
            duration = stopwatch.Elapsed;
        }
        else if (!string.IsNullOrEmpty(toolCallId) && _activeTools.TryRemove(toolName, out stopwatch))
        {
            // Fallback: try tool name if call ID didn't match (e.g., start event had no call ID)
            stopwatch.Stop();
            duration = stopwatch.Elapsed;
        }
        else
        {
            // Tool completed without a matching start event — log at debug level
            // since this commonly happens with SDK-internal tool calls
            duration = TimeSpan.Zero;
            _logger?.Debug(
                "Tool completion without matching start event: {ToolName} (TrackingKey: {Key})", 
                toolName, trackingKey);
        }

        var record = new ToolExecutionRecord
        {
            ToolName = toolName,
            Duration = duration,
            Success = success,
            Error = error,
            Timestamp = DateTimeOffset.UtcNow
        };

        AddExecutionRecord(toolName, record);
        
        _logger?.Debug(
            "Tool execution complete: {ToolName}, Success: {Success}, Duration: {Duration}ms",
            toolName, success, duration.TotalMilliseconds);
    }

    private void AddExecutionRecord(string toolName, ToolExecutionRecord record)
    {
        _toolExecutions.AddOrUpdate(
            toolName,
            _ => new List<ToolExecutionRecord> { record },
            (_, list) =>
            {
                lock (_executionsLock)
                {
                    list.Add(record);
                }
                return list;
            });
    }

    /// <summary>
    /// Gets aggregated metrics for all tool executions.
    /// </summary>
    /// <returns>Summary of all tool metrics.</returns>
    public ToolMetricsSummary GetMetrics()
    {
        var byTool = new Dictionary<string, ToolStats>();
        var totalExecutions = 0;
        var totalSuccess = 0;
        var totalErrors = 0;

        foreach (var kvp in _toolExecutions)
        {
            List<ToolExecutionRecord> recordsCopy;
            lock (_executionsLock)
            {
                recordsCopy = kvp.Value.ToList();
            }

            if (recordsCopy.Count == 0)
                continue;

            var count = recordsCopy.Count;
            var successCount = recordsCopy.Count(r => r.Success);
            var errorCount = count - successCount;
            var avgDuration = TimeSpan.FromTicks((long)recordsCopy.Average(r => r.Duration.Ticks));
            var maxDuration = recordsCopy.Max(r => r.Duration);

            byTool[kvp.Key] = new ToolStats
            {
                Count = count,
                SuccessCount = successCount,
                ErrorCount = errorCount,
                AvgDuration = avgDuration,
                MaxDuration = maxDuration
            };

            totalExecutions += count;
            totalSuccess += successCount;
            totalErrors += errorCount;
        }

        return new ToolMetricsSummary
        {
            TotalExecutions = totalExecutions,
            SuccessCount = totalSuccess,
            ErrorCount = totalErrors,
            ByTool = byTool
        };
    }

    /// <summary>
    /// Clears all collected metrics and resets state.
    /// </summary>
    public void Reset()
    {
        _activeTools.Clear();
        _toolExecutions.Clear();
        
        _logger?.Debug("ToolMetricsCollector reset");
    }
}
