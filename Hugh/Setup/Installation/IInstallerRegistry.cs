namespace Hugh.Setup.Installation;

/// <summary>
/// Registry for discovering and selecting installers for prerequisites.
/// Provides auto-selection of the best available installer.
/// </summary>
public interface IInstallerRegistry
{
    /// <summary>
    /// Gets the best available installer for a prerequisite.
    /// Returns the highest-priority installer that is available and can install the prerequisite.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite (e.g., "copilot-cli", "github-cli").</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The best available installer, or null if no installer can handle the prerequisite.</returns>
    Task<IInstaller?> GetInstallerAsync(string prerequisiteId, CancellationToken ct = default);

    /// <summary>
    /// Gets all available installers for a prerequisite.
    /// Useful when presenting the user with a choice of installation methods.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite (e.g., "copilot-cli", "github-cli").</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>A list of all installers that can install the prerequisite and are currently available.</returns>
    Task<IReadOnlyList<IInstaller>> GetAvailableInstallersAsync(string prerequisiteId, CancellationToken ct = default);
}
