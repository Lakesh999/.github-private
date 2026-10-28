using Hugh.Detection;
using Hugh.Metrics;
using Hugh.Models;

namespace Hugh.Tests.Models;

/// <summary>
/// Unit tests for PhaseResult enrichment properties.
/// Verifies default values and that 'with' expressions correctly set new values.
/// </summary>
public class PhaseResultTests
{
    #region Default Values

    [Fact]
    public void PhaseResult_CompactionOccurred_DefaultsFalse()
    {
        var result = new PhaseResult();
        Assert.False(result.CompactionOccurred);
    }

    [Fact]
    public void PhaseResult_ToolMetrics_DefaultsNull()
    {
        var result = new PhaseResult();
        Assert.Null(result.ToolMetrics);
    }

    [Fact]
    public void PhaseResult_ErrorClassification_DefaultsNull()
    {
        var result = new PhaseResult();
        Assert.Null(result.ErrorClassification);
    }

    [Fact]
    public void PhaseResult_RetryAttemptsMade_DefaultsZero()
    {
        var result = new PhaseResult();
        Assert.Equal(0, result.RetryAttemptsMade);
    }

    #endregion

    #region With Expressions

    [Fact]
    public void PhaseResult_WithCompactionOccurred_SetsValue()
    {
        var original = new PhaseResult();
        var updated = original with { CompactionOccurred = true };

        Assert.False(original.CompactionOccurred);
        Assert.True(updated.CompactionOccurred);
    }

    [Fact]
    public void PhaseResult_WithToolMetrics_SetsValue()
    {
        var metrics = new ToolMetricsSummary
        {
            TotalExecutions = 5,
            SuccessCount = 4,
            ErrorCount = 1
        };

        var original = new PhaseResult();
        var updated = original with { ToolMetrics = metrics };

        Assert.Null(original.ToolMetrics);
        Assert.NotNull(updated.ToolMetrics);
        Assert.Equal(5, updated.ToolMetrics.TotalExecutions);
        Assert.Equal(4, updated.ToolMetrics.SuccessCount);
        Assert.Equal(1, updated.ToolMetrics.ErrorCount);
    }

    [Fact]
    public void PhaseResult_WithErrorClassification_SetsValue()
    {
        var classification = new ErrorClassification
        {
            Category = ErrorCategory.Transient,
            IsRetryable = true,
            SuggestedDelay = TimeSpan.FromSeconds(2),
            SuggestedAction = "Retry after delay"
        };

        var original = new PhaseResult();
        var updated = original with { ErrorClassification = classification };

        Assert.Null(original.ErrorClassification);
        Assert.NotNull(updated.ErrorClassification);
        Assert.Equal(ErrorCategory.Transient, updated.ErrorClassification.Category);
        Assert.True(updated.ErrorClassification.IsRetryable);
    }

    [Fact]
    public void PhaseResult_WithRetryAttemptsMade_SetsValue()
    {
        var original = new PhaseResult();
        var updated = original with { RetryAttemptsMade = 3 };

        Assert.Equal(0, original.RetryAttemptsMade);
        Assert.Equal(3, updated.RetryAttemptsMade);
    }

    #endregion

    #region Combined Enrichment

    [Fact]
    public void PhaseResult_AllEnrichmentProperties_SetTogether()
    {
        var metrics = new ToolMetricsSummary
        {
            TotalExecutions = 10,
            SuccessCount = 9,
            ErrorCount = 1
        };

        var classification = new ErrorClassification
        {
            Category = ErrorCategory.RateLimited,
            IsRetryable = true,
            SuggestedDelay = TimeSpan.FromSeconds(60),
            SuggestedAction = "Wait and retry"
        };

        var result = new PhaseResult
        {
            Success = false,
            ErrorMessage = "Rate limited",
            CompactionOccurred = true,
            ToolMetrics = metrics,
            ErrorClassification = classification,
            RetryAttemptsMade = 2
        };

        Assert.False(result.Success);
        Assert.True(result.CompactionOccurred);
        Assert.NotNull(result.ToolMetrics);
        Assert.Equal(10, result.ToolMetrics.TotalExecutions);
        Assert.NotNull(result.ErrorClassification);
        Assert.Equal(ErrorCategory.RateLimited, result.ErrorClassification.Category);
        Assert.Equal(2, result.RetryAttemptsMade);
    }

    [Fact]
    public void PhaseResult_ExistingProperties_UnaffectedByEnrichment()
    {
        var result = new PhaseResult
        {
            Success = true,
            OutputDocumentPath = "/docs/plans/test.md",
            Duration = TimeSpan.FromMinutes(5),
            CompactionOccurred = true,
            RetryAttemptsMade = 1
        };

        Assert.True(result.Success);
        Assert.Equal("/docs/plans/test.md", result.OutputDocumentPath);
        Assert.Equal(TimeSpan.FromMinutes(5), result.Duration);
        Assert.True(result.CompactionOccurred);
        Assert.Equal(1, result.RetryAttemptsMade);
    }

    #endregion

    #region UsageMetrics Tests

    [Fact]
    public void PhaseResult_Usage_DefaultsNull()
    {
        var result = new PhaseResult();
        Assert.Null(result.Usage);
    }

    [Fact]
    public void PhaseResult_WithUsageMetrics_SetsValue()
    {
        var usage = new UsageMetrics
        {
            InputTokens = 1500,
            OutputTokens = 500,
            ReasoningTokens = 200,
            Duration = TimeSpan.FromSeconds(3.5),
            Model = "claude-sonnet-4-5"
        };

        var result = new PhaseResult { Usage = usage };

        Assert.NotNull(result.Usage);
        Assert.Equal(1500, result.Usage.InputTokens);
        Assert.Equal(500, result.Usage.OutputTokens);
        Assert.Equal(200, result.Usage.ReasoningTokens);
        Assert.Equal(TimeSpan.FromSeconds(3.5), result.Usage.Duration);
        Assert.Equal("claude-sonnet-4-5", result.Usage.Model);
    }

    [Fact]
    public void UsageMetrics_DefaultValues_AllZeroOrNull()
    {
        var usage = new UsageMetrics();

        Assert.Equal(0, usage.InputTokens);
        Assert.Equal(0, usage.OutputTokens);
        Assert.Equal(0, usage.ReasoningTokens);
        Assert.Equal(TimeSpan.Zero, usage.Duration);
        Assert.Null(usage.Model);
    }

    [Fact]
    public void UsageMetrics_Serialization_RoundTrips()
    {
        var usage = new UsageMetrics
        {
            InputTokens = 2000,
            OutputTokens = 800,
            ReasoningTokens = 100,
            Duration = TimeSpan.FromSeconds(2.1),
            Model = "test-model"
        };

        var json = System.Text.Json.JsonSerializer.Serialize(usage);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<UsageMetrics>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(usage.InputTokens, deserialized.InputTokens);
        Assert.Equal(usage.OutputTokens, deserialized.OutputTokens);
        Assert.Equal(usage.ReasoningTokens, deserialized.ReasoningTokens);
        Assert.Equal(usage.Model, deserialized.Model);
    }

    #endregion

    #region SubagentMetrics Tests

    [Fact]
    public void PhaseResult_Subagents_DefaultsNull()
    {
        var result = new PhaseResult();
        Assert.Null(result.Subagents);
    }

    [Fact]
    public void PhaseResult_WithSubagentMetrics_SetsValue()
    {
        var subagents = new SubagentMetrics
        {
            Started = 3,
            Completed = 2,
            Failed = 1
        };

        var result = new PhaseResult { Subagents = subagents };

        Assert.NotNull(result.Subagents);
        Assert.Equal(3, result.Subagents.Started);
        Assert.Equal(2, result.Subagents.Completed);
        Assert.Equal(1, result.Subagents.Failed);
    }

    [Fact]
    public void SubagentMetrics_DefaultValues_AllZero()
    {
        var metrics = new SubagentMetrics();

        Assert.Equal(0, metrics.Started);
        Assert.Equal(0, metrics.Completed);
        Assert.Equal(0, metrics.Failed);
    }

    [Fact]
    public void SubagentMetrics_Serialization_RoundTrips()
    {
        var metrics = new SubagentMetrics
        {
            Started = 5,
            Completed = 4,
            Failed = 1
        };

        var json = System.Text.Json.JsonSerializer.Serialize(metrics);
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<SubagentMetrics>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(metrics.Started, deserialized.Started);
        Assert.Equal(metrics.Completed, deserialized.Completed);
        Assert.Equal(metrics.Failed, deserialized.Failed);
    }

    #endregion
}
