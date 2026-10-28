namespace Hugh.Setup;

/// <summary>
/// Coordinates prerequisite detection and optional installation.
/// Manages the workflow of checking prerequisites and fixing issues when requested.
/// </summary>
public interface ISetupOrchestrator
{
    /// <summary>
    /// Runs prerequisite checks and optionally fixes any issues.
    /// </summary>
    /// <param name="options">The setup options controlling behavior.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the setup operation, including all prerequisite statuses.</returns>
    Task<SetupResult> RunAsync(SetupOptions options, CancellationToken ct = default);
}
