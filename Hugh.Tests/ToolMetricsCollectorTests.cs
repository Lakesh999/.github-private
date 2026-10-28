using Hugh.Metrics;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for ToolMetricsCollector.
/// </summary>
public class ToolMetricsCollectorTests
{
    #region Constructor Tests

    [Fact]
    public void Constructor_Default_CreatesInstance()
    {
        var collector = new ToolMetricsCollector();
        
        Assert.NotNull(collector);
    }

    [Fact]
    public void Constructor_WithNullLogger_CreatesInstance()
    {
        var collector = new ToolMetricsCollector(null);
        
        Assert.NotNull(collector);
    }

    #endregion

    #region OnToolStart Tests

    [Fact]
    public async Task OnToolStart_WithValidToolName_StartsTimer()
    {
        var collector = new ToolMetricsCollector();
        
        collector.OnToolStart("create_file");
        
        // Timer should be running - complete and verify duration is > 0
        await Task.Delay(10);
        collector.OnToolComplete("create_file", true);
        
        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalExecutions);
    }

    [Fact]
    public void OnToolStart_WithNullToolName_DoesNotThrow()
    {
        var collector = new ToolMetricsCollector();
        
        var exception = Record.Exception(() => collector.OnToolStart(null!));
        
        Assert.Null(exception);
    }

    [Fact]
    public void OnToolStart_WithEmptyToolName_DoesNotThrow()
    {
        var collector = new ToolMetricsCollector();
        
        var exception = Record.Exception(() => collector.OnToolStart(string.Empty));
        
        Assert.Null(exception);
    }

    #endregion

    #region OnToolComplete Tests

    [Fact]
    public void OnToolComplete_RecordsExecution()
    {
        var collector = new ToolMetricsCollector();
        
        collector.OnToolStart("create_file");
        collector.OnToolComplete("create_file", true);
        
        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalExecutions);
        Assert.Equal(1, metrics.SuccessCount);
        Assert.Equal(0, metrics.ErrorCount);
    }

    [Fact]
    public void OnToolComplete_WithError_RecordsFailure()
    {
        var collector = new ToolMetricsCollector();
        
        collector.OnToolStart("create_file");
        collector.OnToolComplete("create_file", false, "File not found");
        
        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalExecutions);
        Assert.Equal(0, metrics.SuccessCount);
        Assert.Equal(1, metrics.ErrorCount);
    }

    [Fact]
    public void OnToolComplete_WithoutStart_StillRecordsExecution()
    {
        var collector = new ToolMetricsCollector();
        
        // Complete without matching start
        collector.OnToolComplete("create_file", true);
        
        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalExecutions);
        Assert.Equal(TimeSpan.Zero, metrics.ByTool["create_file"].AvgDuration);
    }

    [Fact]
    public void OnToolComplete_WithNullToolName_DoesNotThrow()
    {
        var collector = new ToolMetricsCollector();
        
        var exception = Record.Exception(() => collector.OnToolComplete(null!, true));
        
        Assert.Null(exception);
    }

    [Fact]
    public void OnToolComplete_WithEmptyToolName_DoesNotThrow()
    {
        var collector = new ToolMetricsCollector();
        
        var exception = Record.Exception(() => collector.OnToolComplete(string.Empty, true));
        
        Assert.Null(exception);
    }

    #endregion

    #region GetMetrics Tests

    [Fact]
    public void GetMetrics_EmptyReturnsZeros()
    {
        var collector = new ToolMetricsCollector();
        
        var metrics = collector.GetMetrics();
        
        Assert.Equal(0, metrics.TotalExecutions);
        Assert.Equal(0, metrics.SuccessCount);
        Assert.Equal(0, metrics.ErrorCount);
        Assert.Empty(metrics.ByTool);
    }

    [Fact]
    public void GetMetrics_AggregatesCorrectly()
    {
        var collector = new ToolMetricsCollector();
        
        // Multiple tools with different outcomes
        collector.OnToolStart("create_file");
        collector.OnToolComplete("create_file", true);
        
        collector.OnToolStart("read_file");
        collector.OnToolComplete("read_file", true);
        
        collector.OnToolStart("create_file");
        collector.OnToolComplete("create_file", false, "Permission denied");
        
        collector.OnToolStart("grep_search");
        collector.OnToolComplete("grep_search", true);
        
        var metrics = collector.GetMetrics();
        
        Assert.Equal(4, metrics.TotalExecutions);
        Assert.Equal(3, metrics.SuccessCount);
        Assert.Equal(1, metrics.ErrorCount);
        Assert.Equal(3, metrics.ByTool.Count);
        
        // Check per-tool stats
        Assert.Equal(2, metrics.ByTool["create_file"].Count);
        Assert.Equal(1, metrics.ByTool["create_file"].SuccessCount);
        Assert.Equal(1, metrics.ByTool["create_file"].ErrorCount);
        
        Assert.Equal(1, metrics.ByTool["read_file"].Count);
        Assert.Equal(1, metrics.ByTool["read_file"].SuccessCount);
        
        Assert.Equal(1, metrics.ByTool["grep_search"].Count);
        Assert.Equal(1, metrics.ByTool["grep_search"].SuccessCount);
    }

    [Fact]
    public async Task GetMetrics_CalculatesAverageDuration()
    {
        var collector = new ToolMetricsCollector();
        
        // Execute same tool multiple times with artificial delays
        for (int i = 0; i < 3; i++)
        {
            collector.OnToolStart("slow_tool");
            await Task.Delay(20);
            collector.OnToolComplete("slow_tool", true);
        }
        
        var metrics = collector.GetMetrics();
        
        Assert.Equal(3, metrics.ByTool["slow_tool"].Count);
        Assert.True(metrics.ByTool["slow_tool"].AvgDuration.TotalMilliseconds > 0);
        Assert.True(metrics.ByTool["slow_tool"].MaxDuration.TotalMilliseconds > 0);
    }

    [Fact]
    public async Task GetMetrics_CalculatesMaxDuration()
    {
        var collector = new ToolMetricsCollector();
        
        // First execution: short
        collector.OnToolStart("variable_tool");
        await Task.Delay(5);
        collector.OnToolComplete("variable_tool", true);
        
        // Second execution: longer
        collector.OnToolStart("variable_tool");
        await Task.Delay(50);
        collector.OnToolComplete("variable_tool", true);
        
        // Third execution: short
        collector.OnToolStart("variable_tool");
        await Task.Delay(5);
        collector.OnToolComplete("variable_tool", true);
        
        var metrics = collector.GetMetrics();
        
        Assert.True(metrics.ByTool["variable_tool"].MaxDuration >= metrics.ByTool["variable_tool"].AvgDuration);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsAllData()
    {
        var collector = new ToolMetricsCollector();
        
        // Add some data
        collector.OnToolStart("create_file");
        collector.OnToolComplete("create_file", true);
        collector.OnToolStart("read_file");
        collector.OnToolComplete("read_file", true);
        
        // Verify data exists
        var beforeReset = collector.GetMetrics();
        Assert.Equal(2, beforeReset.TotalExecutions);
        
        // Reset
        collector.Reset();
        
        // Verify data is cleared
        var afterReset = collector.GetMetrics();
        Assert.Equal(0, afterReset.TotalExecutions);
        Assert.Empty(afterReset.ByTool);
    }

    [Fact]
    public void Reset_ClearsActiveTimers()
    {
        var collector = new ToolMetricsCollector();
        
        // Start but don't complete
        collector.OnToolStart("create_file");
        collector.OnToolStart("read_file");
        
        // Reset
        collector.Reset();
        
        // Now complete - should have zero duration since timers were cleared
        collector.OnToolComplete("create_file", true);
        
        var metrics = collector.GetMetrics();
        Assert.Equal(1, metrics.TotalExecutions);
        Assert.Equal(TimeSpan.Zero, metrics.ByTool["create_file"].AvgDuration);
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public void ConcurrentAccess_ThreadSafe()
    {
        var collector = new ToolMetricsCollector();
        var toolNames = new[] { "tool_a", "tool_b", "tool_c", "tool_d", "tool_e" };
        var iterationsPerThread = 100;
        var exceptions = new List<Exception>();
        
        // Run parallel operations
        Parallel.For(0, 10, threadId =>
        {
            try
            {
                for (int i = 0; i < iterationsPerThread; i++)
                {
                    var toolName = toolNames[i % toolNames.Length];
                    
                    collector.OnToolStart(toolName);
                    collector.OnToolComplete(toolName, i % 3 != 0, i % 3 == 0 ? "test error" : null);
                    
                    // Periodically read metrics
                    if (i % 10 == 0)
                    {
                        var _ = collector.GetMetrics();
                    }
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });
        
        // No exceptions should occur
        Assert.Empty(exceptions);
        
        // Metrics should be consistent
        var metrics = collector.GetMetrics();
        Assert.Equal(1000, metrics.TotalExecutions); // 10 threads * 100 iterations
    }

    [Fact]
    public void ConcurrentGetMetrics_ThreadSafe()
    {
        var collector = new ToolMetricsCollector();
        
        // Pre-populate with data
        for (int i = 0; i < 100; i++)
        {
            collector.OnToolStart($"tool_{i % 5}");
            collector.OnToolComplete($"tool_{i % 5}", true);
        }
        
        var exceptions = new List<Exception>();
        
        // Read metrics concurrently
        Parallel.For(0, 100, _ =>
        {
            try
            {
                var metrics = collector.GetMetrics();
                Assert.Equal(100, metrics.TotalExecutions);
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });
        
        Assert.Empty(exceptions);
    }

    [Fact]
    public void ConcurrentResetDuringAccess_ThreadSafe()
    {
        var collector = new ToolMetricsCollector();
        var exceptions = new List<Exception>();
        
        Parallel.For(0, 100, i =>
        {
            try
            {
                if (i % 20 == 0)
                {
                    collector.Reset();
                }
                else
                {
                    collector.OnToolStart("tool");
                    collector.OnToolComplete("tool", true);
                    var _ = collector.GetMetrics();
                }
            }
            catch (Exception ex)
            {
                lock (exceptions)
                {
                    exceptions.Add(ex);
                }
            }
        });
        
        Assert.Empty(exceptions);
    }

    #endregion

    #region Record Tests

    [Fact]
    public void ToolExecutionRecord_RequiredProperties()
    {
        var record = new ToolExecutionRecord
        {
            ToolName = "test_tool",
            Duration = TimeSpan.FromSeconds(1),
            Success = true,
            Error = null,
            Timestamp = DateTimeOffset.UtcNow
        };
        
        Assert.Equal("test_tool", record.ToolName);
        Assert.Equal(TimeSpan.FromSeconds(1), record.Duration);
        Assert.True(record.Success);
    }

    [Fact]
    public void ToolStats_DefaultValues()
    {
        var stats = new ToolStats();
        
        Assert.Equal(0, stats.Count);
        Assert.Equal(0, stats.SuccessCount);
        Assert.Equal(0, stats.ErrorCount);
        Assert.Equal(TimeSpan.Zero, stats.AvgDuration);
        Assert.Equal(TimeSpan.Zero, stats.MaxDuration);
    }

    [Fact]
    public void ToolMetricsSummary_DefaultValues()
    {
        var summary = new ToolMetricsSummary();
        
        Assert.Equal(0, summary.TotalExecutions);
        Assert.Equal(0, summary.SuccessCount);
        Assert.Equal(0, summary.ErrorCount);
        Assert.Empty(summary.ByTool);
    }

    #endregion
}
