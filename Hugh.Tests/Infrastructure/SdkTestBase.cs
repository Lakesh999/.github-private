using Hugh.Configuration;

namespace Hugh.Tests.Infrastructure;

/// <summary>
/// Abstract base class for SDK-dependent tests. Implements <see cref="IAsyncLifetime"/>
/// for async setup/teardown and <see cref="IDisposable"/> for synchronous cleanup.
/// Creates a temporary workspace with the standard Hugh directory structure and
/// minimal agent stubs required by the orchestration pipeline.
/// </summary>
public abstract class SdkTestBase : IAsyncLifetime, IDisposable
{
    /// <summary>
    /// Root path of the temporary test workspace.
    /// </summary>
    protected string WorkspacePath { get; private set; } = null!;

    /// <summary>
    /// Path to the agents directory within the test workspace.
    /// </summary>
    protected string AgentsPath { get; private set; } = null!;

    /// <summary>
    /// Path to the docs directory within the test workspace.
    /// </summary>
    protected string DocsPath { get; private set; } = null!;

    /// <summary>
    /// Path to the orchestrator state directory within the test workspace.
    /// </summary>
    protected string OrchestratorPath { get; private set; } = null!;

    /// <summary>
    /// Pre-configured HughConfig pointing to the test workspace paths.
    /// </summary>
    protected HughConfig Config { get; private set; } = null!;

    /// <summary>
    /// The 5 standard agent names written as stubs during initialization.
    /// </summary>
    private static readonly string[] AgentNames =
    {
        "pch-visionary",
        "pch-researcher",
        "pch-planner",
        "pch-plan-reviewer",
        "pch-coder"
    };

    /// <summary>
    /// Creates the temp workspace directory structure and writes agent stubs.
    /// </summary>
    public virtual async Task InitializeAsync()
    {
        WorkspacePath = Path.Combine(Path.GetTempPath(), $"HughSdkTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(WorkspacePath);

        // Create standard directory structure
        AgentsPath = Path.Combine(WorkspacePath, "agents");
        DocsPath = Path.Combine(WorkspacePath, "docs");
        OrchestratorPath = Path.Combine(WorkspacePath, ".orchestrator");

        Directory.CreateDirectory(AgentsPath);
        Directory.CreateDirectory(Path.Combine(DocsPath, "vision"));
        Directory.CreateDirectory(Path.Combine(DocsPath, "research"));
        Directory.CreateDirectory(Path.Combine(DocsPath, "plans"));
        Directory.CreateDirectory(OrchestratorPath);

        // Write minimal agent stubs
        foreach (var agent in AgentNames)
        {
            var content = $"""
                ---
                name: {agent}
                description: Mock {agent} for testing
                model: test
                ---

                You are {agent}.
                """;

            await File.WriteAllTextAsync(
                Path.Combine(AgentsPath, $"{agent}.md"),
                content);
        }

        // Build config pointing to test workspace
        Config = new HughConfig
        {
            AgentsPath = AgentsPath,
            DocsPath = DocsPath,
            OrchestratorPath = OrchestratorPath
        };
    }

    /// <summary>
    /// Async teardown — cleans up the temp workspace.
    /// </summary>
    public virtual Task DisposeAsync()
    {
        Cleanup();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Synchronous cleanup for IDisposable.
    /// </summary>
    public void Dispose()
    {
        Cleanup();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Skips the current test if the Copilot SDK is not available.
    /// Uses <see cref="SdkAvailability.IsSdkAvailable"/> for detection
    /// and Xunit.SkippableFact's Skip.IfNot for proper skip reporting.
    /// Requires the calling test to use [SkippableFact] instead of [Fact].
    /// </summary>
    protected static void SkipIfSdkUnavailable()
    {
        Skip.IfNot(SdkAvailability.IsSdkAvailable(), "Copilot SDK not available");
    }

    private void Cleanup()
    {
        if (!string.IsNullOrEmpty(WorkspacePath) && Directory.Exists(WorkspacePath))
        {
            try
            {
                Directory.Delete(WorkspacePath, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }
    }
}
