using Hugh.Setup.Detection;
using Hugh.Setup.Installation;

namespace Hugh.Setup;

/// <summary>
/// Options for running the setup orchestrator.
/// </summary>
public record SetupOptions
{
    /// <summary>
    /// Gets a value indicating whether to automatically fix missing or misconfigured prerequisites.
    /// Corresponds to the --fix command line flag.
    /// </summary>
    public bool Fix { get; init; } = false;

    /// <summary>
    /// Gets a value indicating whether to run in non-interactive mode.
    /// When true, skips all prompts (useful for CI/CD environments).
    /// </summary>
    public bool NonInteractive { get; init; } = false;

    /// <summary>
    /// Gets the override path to the agents directory.
    /// If null, uses the default from configuration.
    /// </summary>
    public string? AgentsPath { get; init; }

    /// <summary>
    /// Gets the current workspace/working directory path.
    /// Used for detecting workspace-specific configuration.
    /// </summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>
/// Represents the result of running the setup orchestrator.
/// </summary>
public record SetupResult
{
    /// <summary>
    /// Gets a value indicating whether all prerequisites are satisfied.
    /// </summary>
    public required bool AllSatisfied { get; init; }

    /// <summary>
    /// Gets the status of each prerequisite that was checked.
    /// </summary>
    public required IReadOnlyList<PrerequisiteStatus> Statuses { get; init; }

    /// <summary>
    /// Gets the results of any installation attempts, if --fix was enabled.
    /// Null if no installations were attempted.
    /// </summary>
    public IReadOnlyList<InstallationResult>? InstallationResults { get; init; }
}
