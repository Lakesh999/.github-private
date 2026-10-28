using Hugh.Setup.Detection;
using Hugh.Setup.Installation;

namespace Hugh.Setup.UserInterface;

/// <summary>
/// Represents the user's decision when prompted to fix a missing prerequisite.
/// </summary>
public enum FixDecision
{
    /// <summary>
    /// User chose to install the prerequisite.
    /// </summary>
    Install,

    /// <summary>
    /// User chose to skip this prerequisite.
    /// </summary>
    Skip,

    /// <summary>
    /// User chose to abort the setup process.
    /// </summary>
    Abort
}

/// <summary>
/// Represents the user's decision when prompted for authentication.
/// </summary>
public enum AuthDecision
{
    /// <summary>
    /// User chose to open browser for authentication.
    /// </summary>
    Browser,

    /// <summary>
    /// User chose to skip authentication.
    /// </summary>
    Skip
}

/// <summary>
/// Defines the user interface abstraction for prerequisite setup operations.
/// Provides both interactive and non-interactive implementations.
/// </summary>
public interface ISetupUserInterface
{
    /// <summary>
    /// Displays the prerequisite check results to the user.
    /// </summary>
    /// <param name="statuses">The list of prerequisite statuses to display.</param>
    void ShowPrerequisiteReport(IReadOnlyList<PrerequisiteStatus> statuses);

    /// <summary>
    /// Prompts the user to decide how to fix a missing or misconfigured prerequisite.
    /// </summary>
    /// <param name="status">The status of the prerequisite that needs fixing.</param>
    /// <param name="availableInstallers">The list of available installers that can fix this prerequisite.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The user's decision on how to proceed.</returns>
    Task<FixDecision> PromptToFixAsync(
        PrerequisiteStatus status,
        IReadOnlyList<IInstaller> availableInstallers,
        CancellationToken ct = default);

    /// <summary>
    /// Shows installation progress for a prerequisite.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite being installed.</param>
    /// <param name="message">The progress message to display.</param>
    void ShowInstallationProgress(string prerequisiteId, string message);

    /// <summary>
    /// Shows the result of an installation attempt.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite that was installed.</param>
    /// <param name="result">The installation result.</param>
    void ShowInstallationResult(string prerequisiteId, InstallationResult result);

    /// <summary>
    /// Shows manual installation instructions for a prerequisite.
    /// </summary>
    /// <param name="prerequisiteId">The identifier of the prerequisite.</param>
    /// <param name="instructions">The manual installation instructions.</param>
    void ShowManualInstructions(string prerequisiteId, string[] instructions);

    /// <summary>
    /// Prompts the user to decide how to handle authentication.
    /// </summary>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The user's decision on how to proceed with authentication.</returns>
    Task<AuthDecision> PromptForAuthenticationAsync(CancellationToken ct = default);
}
