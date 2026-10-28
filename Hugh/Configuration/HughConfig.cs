using System.Text.Json.Serialization;
using Hugh.Agents.Discovery;

namespace Hugh.Configuration;

/// <summary>
/// Configuration settings for the Hugh self-operating development loop.
/// Binds to the "Hugh" section in appsettings.json.
/// </summary>
public class HughConfig
{
    /// <summary>
    /// Path to the agents directory containing agent prompt files.
    /// Used as a fallback when agent discovery is unavailable.
    /// </summary>
    public string AgentsPath { get; set; } = "./agents";

    /// <summary>
    /// Path to the docs directory for generated documents.
    /// </summary>
    public string DocsPath { get; set; } = "./docs";

    /// <summary>
    /// Path to the orchestrator state directory.
    /// </summary>
    public string OrchestratorPath { get; set; } = "./.orchestrator";

    /// <summary>
    /// Phase-specific timeout configuration.
    /// </summary>
    public TimeoutConfig Timeouts { get; set; } = new();

    /// <summary>
    /// Autonomous mode configuration.
    /// </summary>
    public AutonomousConfig Autonomous { get; set; } = new();

    /// <summary>
    /// Copilot SDK configuration.
    /// </summary>
    public CopilotConfig Copilot { get; set; } = new();

    /// <summary>
    /// Agent discovery configuration for fetching prompts from GitHub.
    /// </summary>
    public AgentDiscoveryConfig AgentDiscovery { get; set; } = new();

    /// <summary>
    /// Solution developer configuration for multi-release workflows.
    /// </summary>
    public SolutionConfig Solution { get; set; } = new();

    /// <summary>
    /// Post-coding verification configuration for build and test execution.
    /// </summary>
    public VerificationConfig Verification { get; set; } = new();

    /// <summary>
    /// Git commit management configuration.
    /// </summary>
    public GitConfig Git { get; set; } = new();

    /// <summary>
    /// Review-revise cycle configuration.
    /// </summary>
    public ReviewConfig Review { get; set; } = new();

    /// <summary>
    /// Coding session granularity configuration.
    /// </summary>
    public CodingConfig Coding { get; set; } = new();

    /// <summary>
    /// Runtime-set path to discovered agents. Set by the discovery service at startup.
    /// Not serialized to config files.
    /// </summary>
    [JsonIgnore]
    public string? DiscoveredAgentsPath { get; set; }

    /// <summary>
    /// Gets the fully resolved agents path.
    /// Priority: HUGH_AGENTS_PATH env var → discovered agents → configured path.
    /// </summary>
    public string GetResolvedAgentsPath()
    {
        // 1. Explicit environment variable override
        var envPath = Environment.GetEnvironmentVariable("HUGH_AGENTS_PATH");
        if (!string.IsNullOrEmpty(envPath) && Directory.Exists(envPath))
            return envPath;

        // 2. Discovered agents from GitHub
        if (!string.IsNullOrEmpty(DiscoveredAgentsPath))
            return DiscoveredAgentsPath;

        // 3. Configured path (bundled or local)
        if (Path.IsPathRooted(AgentsPath)) return AgentsPath;
        return Path.GetFullPath(Path.Combine(PathResolver.GetInstallationPath(), AgentsPath));
    }
}

/// <summary>
/// Timeout configuration for each phase.
/// </summary>
public class TimeoutConfig
{
    /// <summary>
    /// Timeout for the Vision phase.
    /// </summary>
    public TimeSpan VisionPhase { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>
    /// Timeout for the Research phase (single-shot).
    /// Superseded by <see cref="ResearchSubPhasePerPhase"/> for sub-phase loop execution.
    /// Retained for backward config compatibility.
    /// </summary>
    public TimeSpan ResearchPhase { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Timeout for each Research sub-phase when executing the research sub-phase loop.
    /// </summary>
    public TimeSpan ResearchSubPhasePerPhase { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Timeout for the Planning phase.
    /// Superseded by <see cref="PlanningSubPhasePerPhase"/> for sub-phase loop execution.
    /// Retained for backward config compatibility.
    /// </summary>
    public TimeSpan PlanningPhase { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Timeout for each Planning sub-phase when executing the planning sub-phase loop.
    /// </summary>
    public TimeSpan PlanningSubPhasePerPhase { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Timeout for the Review phase.
    /// </summary>
    public TimeSpan ReviewPhase { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Timeout for each Coding sub-phase.
    /// </summary>
    public TimeSpan CodingPhasePerPhase { get; set; } = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Timeout for interactive Vision phase. Longer than autonomous because
    /// it includes human response time.
    /// </summary>
    public TimeSpan InteractiveVisionPhase { get; set; } = TimeSpan.FromMinutes(30);
}

/// <summary>
/// Autonomous mode settings.
/// </summary>
public class AutonomousConfig
{
    /// <summary>
    /// Whether autonomous mode is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// If true, only accept recommendations without user confirmation.
    /// </summary>
    public bool AcceptRecommendationsOnly { get; set; } = true;

    /// <summary>
    /// Phases that allow multi-turn interactive Q&amp;A with the user.
    /// When a phase is listed here, it uses a conversation loop instead of
    /// the standard autonomous single-shot execution.
    /// </summary>
    public List<string> InteractivePhases { get; set; } = new() { "Vision" };

    /// <summary>
    /// Maximum number of Q&amp;A turns per interactive phase before forcing completion.
    /// </summary>
    public int MaxInteractiveTurns { get; set; } = 15;
}

/// <summary>
/// Copilot SDK configuration.
/// </summary>
public class CopilotConfig
{
    /// <summary>
    /// The model to use for Copilot interactions.
    /// </summary>
    public string Model { get; set; } = "claude-opus-4.8";

    /// <summary>
    /// Maximum number of retry attempts for SDK calls.
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay in milliseconds for exponential backoff retry.
    /// </summary>
    public int RetryBaseDelayMs { get; set; } = 2000;

    /// <summary>
    /// Context compaction threshold (0.0-1.0). When context usage exceeds this ratio, compaction is triggered.
    /// </summary>
    public double CompactionThreshold { get; set; } = 0.8;

    /// <summary>
    /// Buffer exhaustion threshold (0.0-1.0). When context usage exceeds this ratio, the session is considered exhausted.
    /// </summary>
    public double BufferExhaustionThreshold { get; set; } = 0.95;

    /// <summary>
    /// Optional path to the Copilot CLI binary. If null, the SDK uses its default discovery.
    /// </summary>
    public string? CliPath { get; set; }

    /// <summary>
    /// Whether to automatically restart the Copilot CLI process if it exits unexpectedly.
    /// </summary>
    public bool AutoRestart { get; set; } = true;

    /// <summary>
    /// Maximum number of retry attempts per phase when a retryable error occurs.
    /// Non-retryable errors (Authentication, ContextExhausted) fail immediately regardless of this setting.
    /// </summary>
    public int MaxPhaseRetries { get; set; } = 2;
}

/// <summary>
/// Configuration for post-coding build and test verification.
/// </summary>
public class VerificationConfig
{
    /// <summary>
    /// Whether post-coding verification is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Shell command to run for build verification after coding completes.
    /// </summary>
    public string BuildCommand { get; set; } = "dotnet build";

    /// <summary>
    /// Shell command to run for test verification after build passes.
    /// </summary>
    public string TestCommand { get; set; } = "dotnet test";

    /// <summary>
    /// Maximum number of repair attempts when build or test fails.
    /// </summary>
    public int MaxVerificationRetries { get; set; } = 3;

    /// <summary>
    /// Whether to run build/test verification after each coding sub-phase
    /// instead of only after all sub-phases complete.
    /// </summary>
    public bool VerifyBuildPerSubPhase { get; set; } = true;
}

/// <summary>
/// Configuration for the solution developer multi-release workflow.
/// </summary>
public class SolutionConfig
{
    /// <summary>
    /// Timeout for the triage classification phase.
    /// </summary>
    public TimeSpan TriageTimeout { get; set; } = TimeSpan.FromMinutes(3);

    /// <summary>
    /// Timeout for the entire clarification conversation.
    /// </summary>
    public TimeSpan ClarificationTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// Timeout for the decomposition phase.
    /// </summary>
    public TimeSpan DecompositionTimeout { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Maximum number of Q&amp;A turns during clarification before forcing completion.
    /// </summary>
    public int MaxClarificationTurns { get; set; } = 20;
}

/// <summary>
/// Git commit management configuration.
/// Controls per-phase commit behavior during the coding loop.
/// </summary>
public class GitConfig
{
    /// <summary>
    /// Whether to create a git commit after each coding sub-phase completes.
    /// </summary>
    public bool CommitPerPhase { get; set; } = true;

    /// <summary>
    /// Prefix for automated commit messages.
    /// </summary>
    public string CommitMessagePrefix { get; set; } = "[Hugh]";
}

/// <summary>
/// Review-revise cycle configuration.
/// Controls how many review iterations are allowed before proceeding to coding.
/// </summary>
public class ReviewConfig
{
    /// <summary>
    /// Maximum number of review-revise cycles before proceeding regardless of outcome.
    /// </summary>
    public int MaxReviewCycles { get; set; } = 2;

    /// <summary>
    /// Whether a clean approval (no findings) is required before proceeding to coding.
    /// When false, ApprovedWithFindings is treated the same as Approved.
    /// </summary>
    public bool RequireCleanApproval { get; set; } = false;
}

/// <summary>
/// Coding session granularity configuration.
/// Controls whether coding sessions operate at the phase or task level.
/// </summary>
public class CodingConfig
{
    /// <summary>
    /// Coding granularity: "phase" (one session per plan phase) or "task" (one session per task within a phase).
    /// Task-level provides better context isolation and consistent quality for large phases.
    /// </summary>
    public string Granularity { get; set; } = "task";

    /// <summary>
    /// Whether to run build verification after each individual task (when Granularity is "task").
    /// When false, build verification only runs after all tasks in a phase complete.
    /// </summary>
    public bool VerifyBuildPerTask { get; set; } = false;
}
