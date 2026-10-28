using System.Diagnostics;
using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Models;
using Hugh.Orchestration;
using Hugh.Tests.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Serilog;

namespace Hugh.Tests;

/// <summary>
/// Integration test harness for the Hugh development loop.
/// Uses a mock CopilotService for offline testing.
/// </summary>
public class IntegrationTests : IDisposable
{
    private readonly string _testWorkspace;
    private readonly ICopilotService _mockCopilotService;
    private readonly LoopStateManager _stateManager;
    private readonly SignalClassifier _signalClassifier;

    public IntegrationTests()
    {
        // Create a unique temp directory for each test run
        _testWorkspace = Path.Combine(Path.GetTempPath(), $"HughIntegrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testWorkspace);

        // Create agents folder for tests
        var agentsPath = Path.Combine(_testWorkspace, "agents");
        Directory.CreateDirectory(agentsPath);

        // Create mock agent files
        CreateMockAgentFiles(agentsPath);

        // Set up mock CopilotService
        _mockCopilotService = Substitute.For<ICopilotService>();

        // Create state manager
        _stateManager = new LoopStateManager(_testWorkspace, Log.Logger);

        // Create signal classifier
        _signalClassifier = new SignalClassifier();
    }

    public void Dispose()
    {
        // Clean up test directory
        if (Directory.Exists(_testWorkspace))
        {
            try
            {
                Directory.Delete(_testWorkspace, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }

    private static void CreateMockAgentFiles(string agentsPath)
    {
        var agents = new[] { "pch-visionary", "pch-researcher", "pch-planner", "pch-plan-reviewer", "pch-coder" };

        foreach (var agent in agents)
        {
            var content = $"""
                ---
                name: {agent}
                description: Mock {agent} for testing
                model: test
                ---

                You are {agent}.
                """;

            File.WriteAllText(Path.Combine(agentsPath, $"{agent}.md"), content);
        }
    }

    #region Mock CopilotService Setup

    private void SetupVisionPhaseResponse(string documentPath = "/docs/vision/001-test.md")
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Is<AgentDefinition>(a => a.Name == "pch-visionary"),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult
            {
                Success = true,
                OutputDocumentPath = documentPath,
                Duration = TimeSpan.FromSeconds(30)
            });
    }

    private void SetupResearchPhaseResponse(string documentPath = "/docs/research/001-test.md")
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Is<AgentDefinition>(a => a.Name == "pch-researcher"),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult
            {
                Success = true,
                OutputDocumentPath = documentPath,
                Duration = TimeSpan.FromSeconds(45)
            });
    }

    private void SetupPlanningPhaseResponse(string documentPath = "/docs/plans/001-test.md")
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Is<AgentDefinition>(a => a.Name == "pch-planner"),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult
            {
                Success = true,
                OutputDocumentPath = documentPath,
                Duration = TimeSpan.FromSeconds(60)
            });
    }

    private void SetupPhaseWithDecision(string agentName, Decision decision)
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Is<AgentDefinition>(a => a.Name == agentName),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult
            {
                Success = true,
                DecisionsMade = [decision],
                Duration = TimeSpan.FromSeconds(30)
            });
    }

    private void SetupFailingPhase(string agentName, string errorMessage)
    {
        _mockCopilotService
            .ExecutePhaseAsync(
                Arg.Is<AgentDefinition>(a => a.Name == agentName),
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<CancellationToken>())
            .Returns(new PhaseResult
            {
                Success = false,
                ErrorMessage = errorMessage,
                Duration = TimeSpan.FromSeconds(5)
            });
    }

    #endregion

    #region State Manager Interaction Tests

    [Fact]
    public async Task StateManager_CreatesStateForNewRun()
    {
        var state = await _stateManager.CreateRunAsync("Test feature request");

        Assert.NotNull(state);
        Assert.Equal(LoopPhase.Vision, state.CurrentPhase);
        Assert.Equal(PhaseStatus.NotStarted, state.Status);
    }

    [Fact]
    public async Task StateManager_PersistsAndLoadsState()
    {
        var originalState = await _stateManager.CreateRunAsync("Test feature");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress);

        var newManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var loadedState = await newManager.LoadStateAsync();

        Assert.NotNull(loadedState);
        Assert.Equal(originalState.RunId, loadedState.RunId);
        Assert.Equal(LoopPhase.Research, loadedState.CurrentPhase);
    }

    #endregion

    #region Signal Classification Integration Tests

    [Theory]
    [InlineData("✅ **Phase [1] Complete**", SignalType.PhaseComplete)]
    [InlineData("All Phases Complete", SignalType.WorkflowComplete)]
    [InlineData("**Recommendation:** A — Use option A", SignalType.QuestionWithRecommendation)]
    [InlineData("❌ **Blocked** - Missing API key", SignalType.Blocker)]
    [InlineData("🔄 **Execution Mode Selection**", SignalType.ModeSelection)]
    public void SignalClassifier_CorrectlyClassifiesAgentOutput(string agentOutput, SignalType expectedType)
    {
        var result = _signalClassifier.Classify(agentOutput);

        Assert.Equal(expectedType, result.Type);
    }

    #endregion

    #region Mock CopilotService Tests

    [Fact]
    public async Task MockCopilotService_VisionPhase_ReturnsSuccessResult()
    {
        SetupVisionPhaseResponse();
        var agentDef = new AgentDefinition { Name = "pch-visionary", BuiltInPromptName = "vision", Phase = LoopPhase.Vision };

        var result = await _mockCopilotService.ExecutePhaseAsync(
            agentDef,
            "You are pch-visionary",
            "Create vision for test feature",
            [],
            CancellationToken.None);

        Assert.True(result.Success);
        Assert.Equal("/docs/vision/001-test.md", result.OutputDocumentPath);
    }

    [Fact]
    public async Task MockCopilotService_FailingPhase_ReturnsErrorResult()
    {
        SetupFailingPhase("pch-visionary", "Connection timeout");
        var agentDef = new AgentDefinition { Name = "pch-visionary", BuiltInPromptName = "vision", Phase = LoopPhase.Vision };

        var result = await _mockCopilotService.ExecutePhaseAsync(
            agentDef,
            "You are pch-visionary",
            "Create vision for test feature",
            [],
            CancellationToken.None);

        Assert.False(result.Success);
        Assert.Equal("Connection timeout", result.ErrorMessage);
    }

    [Fact]
    public async Task MockCopilotService_PhaseWithDecision_RecordsDecision()
    {
        var decision = new Decision
        {
            Phase = "Vision",
            Question = "Architecture approach?",
            Options = ["Microservices", "Monolith"],
            Selected = "Microservices",
            Rationale = "Better scalability",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        };

        SetupPhaseWithDecision("pch-visionary", decision);
        var agentDef = new AgentDefinition { Name = "pch-visionary", BuiltInPromptName = "vision", Phase = LoopPhase.Vision };

        var result = await _mockCopilotService.ExecutePhaseAsync(
            agentDef,
            "You are pch-visionary",
            "Create vision",
            [],
            CancellationToken.None);

        Assert.Single(result.DecisionsMade);
        Assert.Equal("Microservices", result.DecisionsMade[0].Selected);
    }

    #endregion

    #region End-to-End Workflow Simulation Tests

    [Fact]
    public async Task EndToEnd_VisionToResearchTransition_UpdatesState()
    {
        // Simulate vision phase completion
        await _stateManager.CreateRunAsync("Implement OAuth2 authentication");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Vision, PhaseStatus.InProgress);
        await _stateManager.MarkPhaseCompleteAsync("/docs/vision/001-oauth.md");
        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(LoopPhase.Research, _stateManager.CurrentState!.CurrentPhase);
        Assert.Contains(LoopPhase.Vision, _stateManager.CurrentState.CompletedPhases);
    }

    [Fact]
    public async Task EndToEnd_FullWorkflow_CompletesAllPhases()
    {
        // Create run
        await _stateManager.CreateRunAsync("Full workflow test");

        // Simulate completing each phase
        var phases = new[]
        {
            (LoopPhase.Vision, "/docs/vision/001-test.md"),
            (LoopPhase.Research, "/docs/research/001-test.md"),
            (LoopPhase.Planning, "/docs/plans/001-test.md"),
            (LoopPhase.Review, (string?)null),
            (LoopPhase.Coding, (string?)null)
        };

        foreach (var (phase, docPath) in phases)
        {
            await _stateManager.UpdatePhaseAsync(phase, PhaseStatus.InProgress);
            await _stateManager.MarkPhaseCompleteAsync(docPath);

            if (phase != LoopPhase.Coding)
            {
                await _stateManager.AdvanceToNextPhaseAsync();
            }
        }

        // Advance after coding completes
        await _stateManager.AdvanceToNextPhaseAsync();

        Assert.Equal(PhaseStatus.Complete, _stateManager.CurrentState!.Status);
        Assert.Contains(LoopPhase.Vision, _stateManager.CurrentState.CompletedPhases);
        Assert.Contains(LoopPhase.Research, _stateManager.CurrentState.CompletedPhases);
        Assert.Contains(LoopPhase.Planning, _stateManager.CurrentState.CompletedPhases);
        Assert.Contains(LoopPhase.Review, _stateManager.CurrentState.CompletedPhases);
        Assert.Contains(LoopPhase.Coding, _stateManager.CurrentState.CompletedPhases);
    }

    [Fact]
    public async Task EndToEnd_FailureAndResume_RestoresState()
    {
        // Create and fail a run
        await _stateManager.CreateRunAsync("Failure test");
        await _stateManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.InProgress);
        await _stateManager.MarkPhaseFailedAsync("API rate limit exceeded");

        // Simulate a new process loading and resuming
        var resumeManager = new LoopStateManager(_testWorkspace, Log.Logger);
        var state = await resumeManager.LoadStateAsync();

        Assert.Equal(PhaseStatus.Failed, state!.Status);
        Assert.Equal("API rate limit exceeded", state.FailureReason);

        // Resume from failure
        await resumeManager.UpdatePhaseAsync(LoopPhase.Planning, PhaseStatus.InProgress);
        Assert.Equal(LoopPhase.Planning, resumeManager.CurrentState!.CurrentPhase);
        Assert.Equal(PhaseStatus.InProgress, resumeManager.CurrentState.Status);
    }

    #endregion

    #region Decision Tracking Tests

    [Fact]
    public async Task DecisionTracking_RecordsMultipleDecisions()
    {
        await _stateManager.CreateRunAsync("Decision tracking test");

        var decisions = new[]
        {
            new Decision
            {
                Phase = "Vision",
                Question = "Project scope?",
                Selected = "Full implementation",
                Autonomous = true,
                Timestamp = DateTime.UtcNow
            },
            new Decision
            {
                Phase = "Planning",
                Question = "Database choice?",
                Options = ["PostgreSQL", "MongoDB"],
                Selected = "PostgreSQL",
                Rationale = "relational data model",
                Autonomous = true,
                Timestamp = DateTime.UtcNow
            }
        };

        foreach (var decision in decisions)
        {
            await _stateManager.AddDecisionAsync(decision);
        }

        Assert.Equal(2, _stateManager.CurrentState!.Decisions.Count);
    }

    #endregion

    #region Orchestrator Folder Tests

    [Fact]
    public async Task OrchestratorFolder_CreatedOnFirstRun()
    {
        await _stateManager.CreateRunAsync("Test feature");

        var orchestratorPath = Path.Combine(_testWorkspace, ".orchestrator");
        Assert.True(Directory.Exists(orchestratorPath));
    }

    [Fact]
    public async Task OrchestratorFolder_ContainsStateFile()
    {
        await _stateManager.CreateRunAsync("Test feature");

        var stateFilePath = Path.Combine(_testWorkspace, ".orchestrator", "loop-state.json");
        Assert.True(File.Exists(stateFilePath));
    }

    #endregion

    #region Real CLI Integration Tests

    /// <summary>
    /// Integration test that verifies CopilotService works with the real Copilot CLI.
    /// This test is skipped when the CLI is not available (e.g., in CI environments).
    /// Run with: dotnet test --filter "Category=Integration"
    /// </summary>
    [SkippableFact]
    [Trait("Category", "Integration")]
    public async Task CopilotService_WithRealCLI_ExecutesPrompt()
    {
        // Skip if SDK not available
        Skip.IfNot(SdkAvailability.IsSdkAvailable(), "Copilot SDK not available");

        // Create a real CopilotService with test configuration
        var config = new HughConfig
        {
            AgentsPath = Path.Combine(_testWorkspace, "agents"),
            DocsPath = Path.Combine(_testWorkspace, "docs"),
            OrchestratorPath = Path.Combine(_testWorkspace, ".orchestrator")
        };

        await using var copilotService = new CopilotService(config);

        // Create a simple agent definition for testing
        var agentDef = new AgentDefinition
        {
            Name = "test-agent",
            BuiltInPromptName = "vision",
            Phase = LoopPhase.Vision
        };

        // Execute a simple prompt
        var result = await copilotService.ExecutePhaseAsync(
            agentDef,
            "You are a helpful assistant. Respond briefly.",
            "Say 'Hello, integration test!' and nothing else.",
            Array.Empty<string>(),
            CancellationToken.None);

        // Verify the response structure
        Assert.NotNull(result);
        // The result should have a duration > 0, indicating execution occurred
        Assert.True(result.Duration > TimeSpan.Zero, "Expected non-zero duration from real CLI execution");
    }

#endregion

    #region Global CLI Path Resolution Tests

    [Fact]
    public void PathResolver_ReturnsValidInstallationPath()
    {
        // Reset cached path for fresh test
        PathResolver.ResetCachedPath();

        var installPath = PathResolver.GetInstallationPath();

        // Verify path is not null/empty and exists
        Assert.False(string.IsNullOrWhiteSpace(installPath));
        Assert.True(Directory.Exists(installPath), $"Installation path does not exist: {installPath}");
    }

    [Fact]
    public void PathResolver_LogPathCreatesDirectory()
    {
        var logPath = PathResolver.GetLogPath();

        // Verify log directory is created
        Assert.True(Directory.Exists(logPath), $"Log path was not created: {logPath}");
        Assert.True(logPath.Contains(".hugh"), "Log path should be under ~/.hugh");
        Assert.True(logPath.EndsWith("logs"), "Log path should end with 'logs'");
    }

    [Fact]
    public void HughConfig_GetResolvedAgentsPath_ResolvesRelativeToInstallPath()
    {
        // Arrange
        PathResolver.ResetCachedPath();
        var config = new HughConfig { AgentsPath = "./agents" };

        // Act
        var resolvedPath = config.GetResolvedAgentsPath();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(resolvedPath));
        Assert.True(Path.IsPathRooted(resolvedPath), "Resolved path should be absolute");
        Assert.EndsWith("agents", resolvedPath);
    }

    [Fact]
    public void HughConfig_GetResolvedAgentsPath_PreservesAbsolutePath()
    {
        // Arrange
        var absolutePath = Path.Combine(Path.GetTempPath(), "test-agents");
        var config = new HughConfig { AgentsPath = absolutePath };

        // Act
        var resolvedPath = config.GetResolvedAgentsPath();

        // Assert
        Assert.Equal(absolutePath, resolvedPath);
    }

    [Fact]
    public void BuiltInPromptLoader_LoadsKnownPrompt()
    {
        // Arrange & Act
        var content = Hugh.Prompts.BuiltInPromptLoader.Load("vision");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(content));
        Assert.True(content.Length > 100, "Built-in vision prompt should have substantial content");
    }

    [Fact]
    public void BuiltInPromptLoader_ThrowsForUnknownPrompt()
    {
        // Arrange & Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => Hugh.Prompts.BuiltInPromptLoader.Load("nonexistent-prompt-name"));
    }

    #endregion


}
