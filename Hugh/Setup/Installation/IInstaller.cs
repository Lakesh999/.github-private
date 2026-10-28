namespace Hugh.Setup.Installation;

/// <summary>
/// Defines a strategy for installing prerequisites.
/// Implementations provide different installation mechanisms (e.g., winget, npm, manual).
/// </summary>
public interface IInstaller
{
    /// <summary>
    /// Gets the unique identifier for this installer (e.g., "winget", "npm", "manual").
    /// </summary>
    string InstallerId { get; }

    /// <summary>
    /// Checks if this installer is available on the current system.
    /// For example, WingetInstaller checks if winget.exe is present.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the installer is available and can be used; otherwise false.</returns>
    Task<bool> IsAvailableAsync(CancellationToken ct = default);

    /// <summary>
    /// Checks if this installer can install the given prerequisite.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite (e.g., "copilot-cli", "github-cli").</param>
    /// <returns>True if this installer supports installing the specified prerequisite.</returns>
    bool CanInstall(string prerequisiteId);

    /// <summary>
    /// Installs the specified prerequisite.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite to install.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the installation attempt.</returns>
    Task<InstallationResult> InstallAsync(string prerequisiteId, CancellationToken ct = default);
}
