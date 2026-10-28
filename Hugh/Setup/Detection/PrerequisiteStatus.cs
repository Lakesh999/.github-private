namespace Hugh.Setup.Detection;

/// <summary>
/// Represents the current state of a prerequisite.
/// </summary>
public enum PrerequisiteState
{
    /// <summary>
    /// The prerequisite is installed and working correctly.
    /// </summary>
    Satisfied,

    /// <summary>
    /// The prerequisite is not installed.
    /// </summary>
    Missing,

    /// <summary>
    /// The prerequisite is installed but needs an update.
    /// </summary>
    Outdated,

    /// <summary>
    /// The prerequisite is installed but not properly configured.
    /// </summary>
    Misconfigured
}

/// <summary>
/// Represents the detection result for a single prerequisite.
/// </summary>
public record PrerequisiteStatus
{
    /// <summary>
    /// Gets the unique identifier for the prerequisite (e.g., "copilot-cli", "github-auth").
    /// </summary>
    public required string PrerequisiteId { get; init; }

    /// <summary>
    /// Gets the current state of the prerequisite.
    /// </summary>
    public required PrerequisiteState State { get; init; }

    /// <summary>
    /// Gets the detected version of the prerequisite, if applicable.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets a human-readable message describing the status.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Gets hints for resolving issues with this prerequisite.
    /// </summary>
    public string[]? Hints { get; init; }
}
