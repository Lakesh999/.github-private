using System.Diagnostics;
using Hugh.Setup.Detection;
using Hugh.Setup.Installation;
using Hugh.Setup.UserInterface;

namespace Hugh.Setup;

/// <summary>
/// Coordinates prerequisite detection and optional installation.
/// Implements the workflow of checking prerequisites, displaying reports,
/// and fixing issues when the --fix flag is enabled.
/// </summary>
public class SetupOrchestrator : ISetupOrchestrator
{
    private const string GitHubAuthPrerequisiteId = "github-auth";

    private readonly IEnumerable<IPrerequisiteDetector> _detectors;
    private readonly IInstallerRegistry _installerRegistry;
    private readonly ISetupUserInterface _userInterface;

    /// <summary>
    /// Initializes a new instance of the <see cref="SetupOrchestrator"/> class.
    /// </summary>
    /// <param name="detectors">The collection of prerequisite detectors to run.</param>
    /// <param name="installerRegistry">The registry for discovering installers.</param>
    /// <param name="userInterface">The user interface for displaying progress and prompts.</param>
    public SetupOrchestrator(
        IEnumerable<IPrerequisiteDetector> detectors,
        IInstallerRegistry installerRegistry,
        ISetupUserInterface userInterface)
    {
        _detectors = detectors ?? throw new ArgumentNullException(nameof(detectors));
        _installerRegistry = installerRegistry ?? throw new ArgumentNullException(nameof(installerRegistry));
        _userInterface = userInterface ?? throw new ArgumentNullException(nameof(userInterface));
    }

    /// <inheritdoc/>
    public async Task<SetupResult> RunAsync(SetupOptions options, CancellationToken ct = default)
    {
        // Step 1: Sort detectors by priority and run all detections
        var sortedDetectors = _detectors.OrderBy(d => d.Priority).ToList();
        var statuses = new List<PrerequisiteStatus>();

        foreach (var detector in sortedDetectors)
        {
            ct.ThrowIfCancellationRequested();
            var status = await detector.DetectAsync(ct);
            statuses.Add(status);
        }

        // Step 2: Display the prerequisite report
        _userInterface.ShowPrerequisiteReport(statuses);

        // Step 3: If --fix is not enabled or all prerequisites are satisfied, return early
        var allSatisfied = statuses.All(s => s.State == PrerequisiteState.Satisfied);
        if (!options.Fix || allSatisfied)
        {
            return new SetupResult
            {
                AllSatisfied = allSatisfied,
                Statuses = statuses.AsReadOnly(),
                InstallationResults = null
            };
        }

        // Step 4: Process each unsatisfied prerequisite
        var installationResults = new List<InstallationResult>();
        var updatedStatuses = new List<PrerequisiteStatus>(statuses);

        for (int i = 0; i < sortedDetectors.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var detector = sortedDetectors[i];
            var status = updatedStatuses[i];

            // Skip already satisfied prerequisites
            if (status.State == PrerequisiteState.Satisfied)
            {
                continue;
            }

            // Handle GitHub authentication specially
            if (status.PrerequisiteId == GitHubAuthPrerequisiteId
                && (status.State == PrerequisiteState.Missing || status.State == PrerequisiteState.Misconfigured))
            {
                var authResult = await HandleAuthenticationAsync(detector, options, ct);
                if (authResult.HasValue)
                {
                    updatedStatuses[i] = authResult.Value.status;
                    if (authResult.Value.installResult != null)
                    {
                        installationResults.Add(authResult.Value.installResult);
                    }
                }
                continue;
            }

            // Get available installers for this prerequisite
            var availableInstallers = await _installerRegistry.GetAvailableInstallersAsync(status.PrerequisiteId, ct);

            if (availableInstallers.Count == 0)
            {
                // No installers available - show hints if any
                if (status.Hints != null && status.Hints.Length > 0)
                {
                    _userInterface.ShowManualInstructions(status.PrerequisiteId, status.Hints);
                }
                continue;
            }

            // Non-interactive mode: auto-install without prompting
            FixDecision decision;
            if (options.NonInteractive)
            {
                decision = FixDecision.Install;
            }
            else
            {
                // Prompt user for decision
                decision = await _userInterface.PromptToFixAsync(status, availableInstallers, ct);
            }

            switch (decision)
            {
                case FixDecision.Install:
                    var result = await InstallAndVerifyAsync(detector, availableInstallers, ct);
                    installationResults.Add(result.installResult);
                    updatedStatuses[i] = result.newStatus;
                    break;

                case FixDecision.Skip:
                    // Continue to next prerequisite
                    continue;

                case FixDecision.Abort:
                    // Stop processing and return current state
                    return new SetupResult
                    {
                        AllSatisfied = false,
                        Statuses = updatedStatuses.AsReadOnly(),
                        InstallationResults = installationResults.AsReadOnly()
                    };
            }
        }

        // Calculate final satisfaction state
        var finalAllSatisfied = updatedStatuses.All(s => s.State == PrerequisiteState.Satisfied);

        return new SetupResult
        {
            AllSatisfied = finalAllSatisfied,
            Statuses = updatedStatuses.AsReadOnly(),
            InstallationResults = installationResults.Count > 0 ? installationResults.AsReadOnly() : null
        };
    }

    /// <summary>
    /// Handles GitHub authentication flow when the prerequisite is missing.
    /// </summary>
    private async Task<(PrerequisiteStatus status, InstallationResult? installResult)?> HandleAuthenticationAsync(
        IPrerequisiteDetector detector,
        SetupOptions options,
        CancellationToken ct)
    {
        AuthDecision authDecision;

        if (options.NonInteractive)
        {
            // Can't do interactive auth in non-interactive mode
            return null;
        }

        authDecision = await _userInterface.PromptForAuthenticationAsync(ct);

        if (authDecision == AuthDecision.Skip)
        {
            return null;
        }

        // Browser authentication selected - launch gh auth login --web
        _userInterface.ShowInstallationProgress(GitHubAuthPrerequisiteId, "Opening browser for GitHub authentication...");

        InstallationResult installResult;
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "gh",
                Arguments = "auth login --web",
                UseShellExecute = false,
                CreateNoWindow = false
            };

            using var process = Process.Start(processStartInfo);
            if (process != null)
            {
                await process.WaitForExitAsync(ct);

                if (process.ExitCode == 0)
                {
                    // Re-verify authentication after successful login
                    var newStatus = await detector.DetectAsync(ct);

                    installResult = new InstallationResult
                    {
                        Success = newStatus.State == PrerequisiteState.Satisfied,
                        ErrorMessage = newStatus.State != PrerequisiteState.Satisfied
                            ? "Authentication completed but verification failed"
                            : null
                    };

                    _userInterface.ShowInstallationResult(GitHubAuthPrerequisiteId, installResult);
                    return (newStatus, installResult);
                }
                else
                {
                    installResult = new InstallationResult
                    {
                        Success = false,
                        ErrorMessage = $"gh auth login exited with code {process.ExitCode}"
                    };
                    _userInterface.ShowInstallationResult(GitHubAuthPrerequisiteId, installResult);

                    // Return original status since auth failed
                    var currentStatus = await detector.DetectAsync(ct);
                    return (currentStatus, installResult);
                }
            }
            else
            {
                installResult = new InstallationResult
                {
                    Success = false,
                    ErrorMessage = "Failed to start gh auth login process"
                };
                _userInterface.ShowInstallationResult(GitHubAuthPrerequisiteId, installResult);

                var currentStatus = await detector.DetectAsync(ct);
                return (currentStatus, installResult);
            }
        }
        catch (Exception ex)
        {
            installResult = new InstallationResult
            {
                Success = false,
                ErrorMessage = $"Error during authentication: {ex.Message}"
            };
            _userInterface.ShowInstallationResult(GitHubAuthPrerequisiteId, installResult);

            var currentStatus = await detector.DetectAsync(ct);
            return (currentStatus, installResult);
        }
    }

    /// <summary>
    /// Installs a prerequisite using the first available installer and re-verifies.
    /// </summary>
    private async Task<(InstallationResult installResult, PrerequisiteStatus newStatus)> InstallAndVerifyAsync(
        IPrerequisiteDetector detector,
        IReadOnlyList<IInstaller> installers,
        CancellationToken ct)
    {
        // Use the first (highest priority) available installer
        var installer = installers[0];

        _userInterface.ShowInstallationProgress(detector.PrerequisiteId, $"Installing via {installer.InstallerId}...");

        // Perform installation
        var installResult = await installer.InstallAsync(detector.PrerequisiteId, ct);

        _userInterface.ShowInstallationResult(detector.PrerequisiteId, installResult);

        // If installation provides manual instructions, show them
        if (installResult.Instructions != null && installResult.Instructions.Length > 0)
        {
            _userInterface.ShowManualInstructions(detector.PrerequisiteId, installResult.Instructions);
        }

        // Re-verify: run the detector again to confirm the prerequisite is now satisfied
        PrerequisiteStatus newStatus;
        if (installResult.Success && !installResult.RequiresRestart)
        {
            newStatus = await detector.DetectAsync(ct);
        }
        else if (installResult.Success && installResult.RequiresRestart)
        {
            // Can't re-verify if restart is required; assume it will work
            newStatus = new PrerequisiteStatus
            {
                PrerequisiteId = detector.PrerequisiteId,
                State = PrerequisiteState.Satisfied,
                Version = installResult.Version,
                Message = "Installed successfully. Restart may be required for changes to take effect."
            };
        }
        else
        {
            // Installation failed; re-run detector to get current state
            newStatus = await detector.DetectAsync(ct);
        }

        return (installResult, newStatus);
    }
}
