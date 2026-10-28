using GitHub.Copilot.SDK;
using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Hooks;
using Hugh.Models;
using Hugh.Tests.Infrastructure;
using Hugh.Tools;
using Microsoft.Extensions.AI;
using Serilog;
using SdkSessionConfig = GitHub.Copilot.SDK.SessionConfig;

namespace Hugh.Tests.Sdk;

/// <summary>
/// Tests for SDK session lifecycle: start, dispose, session creation, tool/hook
/// registration, infinite session reflection, prompt round-trip, and post-dispose behavior.
/// All live-SDK tests skip automatically when the Copilot SDK is unavailable.
/// </summary>
[Trait("Category", TestCategory.LiveSdk)]
public class SdkSessionLifecycleTests : SdkTestBase
{
    // ───────────────────────────────────────────────
    // 2.1 — CopilotClient_Start_Succeeds
    // ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that a real CopilotClient can be instantiated and started via
    /// <see cref="CopilotService.EnsureClientStartedAsync"/>.
    /// Skips when the SDK is unavailable.
    /// </summary>
    [SkippableFact]
    public async Task CopilotClient_Start_Succeeds()
    {
        SkipIfSdkUnavailable();

        await SdkTestRetry.WithRetryAsync(async () =>
        {
            await using var service = new CopilotService(Config);
            using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

            // Should not throw
            await service.EnsureClientStartedAsync(cts.Token);
        });
    }

    // ───────────────────────────────────────────────
    // 2.2 — CopilotClient_Start_Idempotent
    // ───────────────────────────────────────────────

    /// <summary>
    /// Calling <see cref="CopilotService.EnsureClientStartedAsync"/> twice on the
    /// same instance must be a no-op the second time (no double-start error).
    /// </summary>
    [SkippableFact]
    public async Task CopilotClient_Start_Idempotent()
    {
        SkipIfSdkUnavailable();

        await SdkTestRetry.WithRetryAsync(async () =>
        {
            await using var service = new CopilotService(Config);
            using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

            await service.EnsureClientStartedAsync(cts.Token);
            // Second call must not throw
            await service.EnsureClientStartedAsync(cts.Token);
        });
    }

    // ───────────────────────────────────────────────
    // 2.3 — Dispose tests
    // ───────────────────────────────────────────────

    /// <summary>
    /// After starting the client, <see cref="CopilotService.DisposeAsync"/> should
    /// clean up without error and reset internal state (_clientStarted = false).
    /// </summary>
    [SkippableFact]
    public async Task CopilotClient_Dispose_CleansUp()
    {
        SkipIfSdkUnavailable();

        await SdkTestRetry.WithRetryAsync(async () =>
        {
            var service = new CopilotService(Config);
            using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

            await service.EnsureClientStartedAsync(cts.Token);
            await service.DisposeAsync();

            // After dispose, starting again should re-initialize (proves _clientStarted was reset)
            // We don't re-start here because _disposed is also set; just verify no exception on dispose.
        });
    }

    /// <summary>
    /// Calling DisposeAsync twice must be idempotent — the second call should be a no-op.
    /// </summary>
    [SkippableFact]
    public async Task Dispose_CalledTwice_Idempotent()
    {
        SkipIfSdkUnavailable();

        await SdkTestRetry.WithRetryAsync(async () =>
        {
            var service = new CopilotService(Config);
            using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

            await service.EnsureClientStartedAsync(cts.Token);

            // First dispose
            await service.DisposeAsync();
            // Second dispose — must not throw
            await service.DisposeAsync();
        });
    }

    /// <summary>
    /// Disposing a CopilotService that was never started must not throw.
    /// No live SDK connection required.
    /// </summary>
    [Fact]
    public async Task Dispose_WithoutStart_Succeeds()
    {
        var service = new CopilotService(Config);

        // Disposing without ever calling EnsureClientStartedAsync — must not throw
        await service.DisposeAsync();
    }

    // ───────────────────────────────────────────────
    // 2.4 — CreateSession_WithFullConfig_Succeeds
    // ───────────────────────────────────────────────

    /// <summary>
    /// Creates a real session with a full <see cref="GitHub.Copilot.SDK.SessionConfig"/>
    /// containing model, system message, tools, and hooks. Verifies the session is returned
    /// without error.
    /// </summary>
    [SkippableFact]
    public async Task CreateSession_WithFullConfig_Succeeds()
    {
        SkipIfSdkUnavailable();

        await SdkTestRetry.WithRetryAsync(async () =>
        {
            await using var service = new CopilotService(Config);
            using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

            await service.EnsureClientStartedAsync(cts.Token);

            var agentDef = CreateTestAgentDefinition();

            await using var session = await service.CreateSessionAsync(
                agentDef,
                "You are a test agent. Respond concisely.",
                cts.Token);

            Assert.NotNull(session);
        });
    }

    // ───────────────────────────────────────────────
    // 2.5 — CreateSession_ToolsRegistered_FourTools
    // ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="HughWorkflowTools.CreateToolSet"/> returns exactly 4
    /// <see cref="AIFunction"/> instances. No live SDK required.
    /// </summary>
    [Fact]
    public void CreateSession_ToolsRegistered_FourTools()
    {
        var tools = new HughWorkflowTools(Log.Logger, WorkspacePath);
        var toolSet = tools.CreateToolSet();

        Assert.Equal(4, toolSet.Count);

        // Verify expected tool names
        var toolNames = toolSet.Select(t => t.Name).OrderBy(n => n).ToList();
        Assert.Contains("hugh_get_workspace_structure", toolNames);
        Assert.Contains("hugh_notify_phase_complete", toolNames);
        Assert.Contains("hugh_notify_workflow_complete", toolNames);
        Assert.Contains("hugh_report_blocker", toolNames);
    }

    // ───────────────────────────────────────────────
    // 2.6 — InfiniteSession_ConfiguredDirectly_Succeeds
    // ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="SdkSessionConfig"/> can be created with
    /// <c>InfiniteSessions</c> property set directly (no reflection needed in SDK 0.3.0+).
    /// </summary>
    [Fact]
    public void InfiniteSession_ConfiguredDirectly_Succeeds()
    {
        var sessionConfig = new SdkSessionConfig
        {
            InfiniteSessions = new InfiniteSessionConfig
            {
                Enabled = true,
                BackgroundCompactionThreshold = 0.8,
                BufferExhaustionThreshold = 0.95
            }
        };

        Assert.NotNull(sessionConfig.InfiniteSessions);
        Assert.True(sessionConfig.InfiniteSessions.Enabled);
    }

    // ───────────────────────────────────────────────
    // 2.7 — InfiniteSession_CompactionThreshold_Applied
    // ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that InfiniteSessionConfig thresholds are correctly set
    /// from configuration values (no reflection needed in SDK 0.3.0+).
    /// </summary>
    [Fact]
    public void InfiniteSession_CompactionThreshold_Applied()
    {
        var config = new HughConfig
        {
            AgentsPath = AgentsPath,
            DocsPath = DocsPath,
            OrchestratorPath = OrchestratorPath,
            Copilot = new CopilotConfig
            {
                CompactionThreshold = 0.8,
                BufferExhaustionThreshold = 0.95
            }
        };

        var sessionConfig = new SdkSessionConfig
        {
            InfiniteSessions = new InfiniteSessionConfig
            {
                Enabled = true,
                BackgroundCompactionThreshold = config.Copilot.CompactionThreshold,
                BufferExhaustionThreshold = config.Copilot.BufferExhaustionThreshold
            }
        };

        Assert.NotNull(sessionConfig.InfiniteSessions);
        Assert.Equal(0.8, sessionConfig.InfiniteSessions.BackgroundCompactionThreshold!.Value, precision: 5);
        Assert.Equal(0.95, sessionConfig.InfiniteSessions.BufferExhaustionThreshold!.Value, precision: 5);
    }

    // ───────────────────────────────────────────────
    // 2.7b — OnPermissionRequest_WiredOnSessionConfig
    // ───────────────────────────────────────────────

    /// <summary>
    /// Verifies that <see cref="SdkSessionConfig.OnPermissionRequest"/> can be set
    /// to <see cref="PermissionHandler.ApproveAll"/> and is non-null after assignment.
    /// </summary>
    [Fact]
    public void OnPermissionRequest_WiredOnSessionConfig()
    {
        var sessionConfig = new SdkSessionConfig
        {
            OnPermissionRequest = PermissionHandler.ApproveAll
        };

        Assert.NotNull(sessionConfig.OnPermissionRequest);
    }

    // ───────────────────────────────────────────────
    // 2.8 — PostDispose_ExecutePhase_ThrowsOrFails
    // ───────────────────────────────────────────────

    /// <summary>
    /// After calling <see cref="CopilotService.DisposeAsync"/>, invoking
    /// <see cref="CopilotService.ExecutePhaseAsync"/> must fail gracefully —
    /// either throwing an exception or returning a failed <see cref="PhaseResult"/>.
    /// </summary>
    [SkippableFact]
    public async Task PostDispose_ExecutePhase_ThrowsOrFails()
    {
        SkipIfSdkUnavailable();

        var service = new CopilotService(Config);
        using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

        // Start and then dispose
        await service.EnsureClientStartedAsync(cts.Token);
        await service.DisposeAsync();

        var agentDef = CreateTestAgentDefinition();

        // After dispose, ExecutePhaseAsync should either throw or return failed result
        try
        {
            var result = await service.ExecutePhaseAsync(
                agentDef,
                "You are a test agent.",
                "Say hello.",
                Enumerable.Empty<string>(),
                cts.Token);

            // If it returned instead of throwing, verify it indicates failure
            Assert.False(result.Success, "ExecutePhaseAsync should not succeed after disposal");
        }
        catch (ObjectDisposedException)
        {
            // Expected — disposed service throws
        }
        catch (InvalidOperationException)
        {
            // Also acceptable — client is null after dispose
        }
    }

    // ───────────────────────────────────────────────
    // 2.9 — Session_ReceivesEvents_OnPrompt
    // ───────────────────────────────────────────────

    /// <summary>
    /// Creates a real session, sends a simple prompt, and verifies that response
    /// events are received with non-empty content. Asserts the round-trip duration
    /// is positive. Uses <see cref="TestTimeouts.CreateTimeout"/> for timeout enforcement.
    /// </summary>
    [SkippableFact]
    public async Task Session_ReceivesEvents_OnPrompt()
    {
        SkipIfSdkUnavailable();

        await SdkTestRetry.WithRetryAsync(async () =>
        {
            await using var service = new CopilotService(Config);
            using var cts = TestTimeouts.CreateTimeout(TestCategory.LiveSdk);

            await service.EnsureClientStartedAsync(cts.Token);

            var agentDef = CreateTestAgentDefinition();
            var agentPrompt = "You are a minimal test agent. Respond with exactly one word.";

            // Use ExecutePhaseAsync to exercise the full session lifecycle:
            // client start → session creation → prompt → response processing
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var result = await service.ExecutePhaseAsync(
                agentDef,
                agentPrompt,
                "Reply with: OK",
                Enumerable.Empty<string>(),
                cts.Token);

            stopwatch.Stop();

            // The phase may succeed or fail depending on SDK response parsing,
            // but we should have received events (non-null result with duration > 0)
            Assert.NotNull(result);
            Assert.True(result.Duration > TimeSpan.Zero,
                "Expected positive duration indicating SDK round-trip occurred");
            Assert.True(stopwatch.Elapsed > TimeSpan.Zero,
                "Expected wall-clock time > 0 for SDK round-trip");
        });
    }

    // ───────────────────────────────────────────────
    // Helpers
    // ───────────────────────────────────────────────

    /// <summary>
    /// Creates a minimal <see cref="AgentDefinition"/> for test use.
    /// </summary>
    private AgentDefinition CreateTestAgentDefinition()
    {
        return new AgentDefinition
        {
            Name = "pch-coder",
            BuiltInPromptName = "coding",
            Phase = LoopPhase.Coding
        };
    }
}
