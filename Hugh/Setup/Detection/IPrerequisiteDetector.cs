namespace Hugh.Setup.Detection;

/// <summary>
/// Interface for detecting the status of a prerequisite.
/// Implementations check if a specific dependency is installed and configured correctly.
/// </summary>
public interface IPrerequisiteDetector
{
    /// <summary>
    /// Gets the unique identifier for this prerequisite (e.g., "copilot-cli", "github-auth").
    /// </summary>
    string PrerequisiteId { get; }

    /// <summary>
    /// Gets the human-readable name for display purposes.
    /// </summary>
    string DisplayName { get; }

    /// <summary>
    /// Gets the execution priority. Lower values execute earlier.
    /// Used to ensure dependencies are checked in the correct order.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Gets a value indicating whether this prerequisite can be automatically installed.
    /// </summary>
    bool IsInstallable { get; }

    /// <summary>
    /// Detects the current state of this prerequisite.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The status of the prerequisite detection.</returns>
    Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default);
}
