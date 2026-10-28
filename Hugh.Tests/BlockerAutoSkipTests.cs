using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Models;
using Hugh.Tools;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Tests for the auto-skip skippable blockers feature (Phase 5).
/// Validates that tool-based skippable blockers are auto-skipped in autonomous mode,
/// while non-skippable and text-based blockers continue to pause execution.
/// </summary>
public class BlockerAutoSkipTests
{
    private readonly ILogger _logger;

    public BlockerAutoSkipTests()
    {
        _logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .CreateLogger();
    }

    private CopilotService CreateService(bool autonomousEnabled)
    {
        var config = new HughConfig
        {
            Autonomous = new AutonomousConfig { Enabled = autonomousEnabled }
        };
        return new CopilotService(config, _logger);
    }

    #region Auto-Skip Tests

    [Fact]
    public async Task HandleSignalAsync_ToolBasedSkippableBlocker_AutoSkipsInAutonomousMode()
    {
        // Arrange
        var service = CreateService(autonomousEnabled: true);
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            Source = SignalSource.ToolCall,
            IsSkippable = true,
            RawMatch = "Test file not found"
        };

        // Act
        var result = await service.HandleSignalAsync(classification, "Coding", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.False(result.ShouldPause);
        Assert.Null(result.Response);
        Assert.NotNull(result.DecisionMade);
        Assert.Equal("Skip", result.DecisionMade.Selected);
        Assert.True(result.DecisionMade.Autonomous);
    }

    [Fact]
    public async Task HandleSignalAsync_ToolBasedNonSkippableBlocker_PausesExecution()
    {
        // Arrange
        var service = CreateService(autonomousEnabled: true);
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            Source = SignalSource.ToolCall,
            IsSkippable = false,
            RawMatch = "Critical dependency missing"
        };

        // Act
        var result = await service.HandleSignalAsync(classification, "Coding", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldPause);
        Assert.Null(result.DecisionMade);
    }

    [Fact]
    public async Task HandleSignalAsync_TextBasedBlocker_AlwaysPauses()
    {
        // Arrange — text-based blockers have Source = TextRegex (default) and IsSkippable = false (default)
        var service = CreateService(autonomousEnabled: true);
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            Source = SignalSource.TextRegex,
            RawMatch = "❌ **Blocked** — missing API key"
        };

        // Act
        var result = await service.HandleSignalAsync(classification, "Coding", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldPause);
        Assert.Null(result.DecisionMade);
    }

    [Fact]
    public async Task HandleSignalAsync_SkippableBlocker_PausesWhenAutonomousDisabled()
    {
        // Arrange — autonomous mode is OFF, so even skippable blockers should pause
        var service = CreateService(autonomousEnabled: false);
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            Source = SignalSource.ToolCall,
            IsSkippable = true,
            RawMatch = "Optional test file not found"
        };

        // Act
        var result = await service.HandleSignalAsync(classification, "Coding", Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ShouldPause);
        Assert.Null(result.DecisionMade);
    }

    [Fact]
    public async Task HandleSignalAsync_AutoSkip_RecordsDecisionWithCorrectFields()
    {
        // Arrange
        var service = CreateService(autonomousEnabled: true);
        var blockerReason = "Non-critical optional file missing";
        var phaseName = "Research";
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            Source = SignalSource.ToolCall,
            IsSkippable = true,
            RawMatch = blockerReason
        };

        // Act
        var result = await service.HandleSignalAsync(classification, phaseName, Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.DecisionMade);
        var decision = result.DecisionMade;
        Assert.Equal(phaseName, decision.Phase);
        Assert.Contains(blockerReason, decision.Question);
        Assert.Contains("Skip", decision.Options);
        Assert.Contains("Pause", decision.Options);
        Assert.Equal("Skip", decision.Selected);
        Assert.Contains("isSkippable=true", decision.Rationale);
        Assert.True(decision.Autonomous);
        Assert.True(decision.Timestamp <= DateTime.UtcNow);
    }

    #endregion

    #region HughWorkflowTools Blocker Metadata Tests

    [Fact]
    public void ReportBlocker_SetsLastBlockerIsSkippable_True()
    {
        // Arrange
        var tools = new HughWorkflowTools(_logger, Directory.GetCurrentDirectory());

        // Act
        tools.ReportBlocker("Some reason", "low", isSkippable: true);

        // Assert
        Assert.True(tools.LastBlockerIsSkippable);
        Assert.Equal("Some reason", tools.LastBlockerReason);
    }

    [Fact]
    public void ReportBlocker_SetsLastBlockerIsSkippable_False()
    {
        // Arrange
        var tools = new HughWorkflowTools(_logger, Directory.GetCurrentDirectory());

        // Act
        tools.ReportBlocker("Critical issue", "high", isSkippable: false);

        // Assert
        Assert.False(tools.LastBlockerIsSkippable);
        Assert.Equal("Critical issue", tools.LastBlockerReason);
    }

    [Fact]
    public void LastBlockerIsSkippable_DefaultsToFalse()
    {
        // Arrange & Act
        var tools = new HughWorkflowTools(_logger, Directory.GetCurrentDirectory());

        // Assert
        Assert.False(tools.LastBlockerIsSkippable);
        Assert.Null(tools.LastBlockerReason);
    }

    #endregion

    #region SignalClassification IsSkippable Tests

    [Fact]
    public void SignalClassification_IsSkippable_DefaultsFalse()
    {
        var classification = new SignalClassification { Type = SignalType.Blocker };
        Assert.False(classification.IsSkippable);
    }

    [Fact]
    public void SignalClassification_IsSkippable_CanBeSetTrue()
    {
        var classification = new SignalClassification
        {
            Type = SignalType.Blocker,
            IsSkippable = true
        };
        Assert.True(classification.IsSkippable);
    }

    [Fact]
    public void SignalClassification_WithExpression_PreservesIsSkippable()
    {
        var original = new SignalClassification
        {
            Type = SignalType.Blocker,
            Source = SignalSource.ToolCall
        };

        var enriched = original with { IsSkippable = true, RawMatch = "test reason" };

        Assert.True(enriched.IsSkippable);
        Assert.Equal("test reason", enriched.RawMatch);
        Assert.Equal(SignalSource.ToolCall, enriched.Source);
        Assert.Equal(SignalType.Blocker, enriched.Type);
    }

    #endregion
}
