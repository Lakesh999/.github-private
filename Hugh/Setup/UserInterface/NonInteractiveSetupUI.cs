using Hugh.Setup.Detection;
using Hugh.Setup.Installation;

namespace Hugh.Setup.UserInterface;

/// <summary>
/// Non-interactive implementation of <see cref="ISetupUserInterface"/>.
/// Used when console input is redirected or --no-interactive flag is set.
/// All prompts return Skip decisions; provides report-only output.
/// </summary>
public sealed class NonInteractiveSetupUI : ISetupUserInterface
{
    private readonly object _lock = new();

    /// <inheritdoc />
    public void ShowPrerequisiteReport(IReadOnlyList<PrerequisiteStatus> statuses)
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine("Prerequisite Check Results");
            Console.WriteLine(new string('─', 40));

            foreach (var status in statuses)
            {
                var icon = GetStatusIcon(status.State);
                var stateText = GetStateText(status.State);

                Console.Write($"  {icon} ");
                Console.Write($"{status.PrerequisiteId}: {stateText}");

                if (!string.IsNullOrEmpty(status.Version))
                {
                    Console.Write($" (v{status.Version})");
                }

                Console.WriteLine();

                if (!string.IsNullOrEmpty(status.Message))
                {
                    Console.WriteLine($"      {status.Message}");
                }

                if (status.Hints is { Length: > 0 })
                {
                    foreach (var hint in status.Hints)
                    {
                        Console.WriteLine($"      → {hint}");
                    }
                }
            }

            Console.WriteLine();
        }
    }

    /// <inheritdoc />
    public Task<FixDecision> PromptToFixAsync(
        PrerequisiteStatus status,
        IReadOnlyList<IInstaller> availableInstallers,
        CancellationToken ct = default)
    {
        // Non-interactive mode always skips
        lock (_lock)
        {
            Console.WriteLine($"ℹ Skipping fix for {status.PrerequisiteId} (non-interactive mode)");
        }

        return Task.FromResult(FixDecision.Skip);
    }

    /// <inheritdoc />
    public void ShowInstallationProgress(string prerequisiteId, string message)
    {
        lock (_lock)
        {
            Console.WriteLine($"  ... {prerequisiteId}: {message}");
        }
    }

    /// <inheritdoc />
    public void ShowInstallationResult(string prerequisiteId, InstallationResult result)
    {
        lock (_lock)
        {
            if (result.Success)
            {
                var versionInfo = !string.IsNullOrEmpty(result.Version) ? $" (v{result.Version})" : "";
                Console.WriteLine($"✓ {prerequisiteId}: Installed successfully{versionInfo}");

                if (result.RequiresRestart)
                {
                    Console.WriteLine($"  ⚠ Restart your terminal for PATH changes to take effect");
                }
            }
            else
            {
                Console.WriteLine($"✗ {prerequisiteId}: Installation failed");

                if (!string.IsNullOrEmpty(result.ErrorMessage))
                {
                    Console.WriteLine($"    Error: {result.ErrorMessage}");
                }
            }
        }
    }

    /// <inheritdoc />
    public void ShowManualInstructions(string prerequisiteId, string[] instructions)
    {
        lock (_lock)
        {
            Console.WriteLine();
            Console.WriteLine($"Manual Installation Instructions for {prerequisiteId}:");
            Console.WriteLine(new string('─', 40));

            for (var i = 0; i < instructions.Length; i++)
            {
                Console.WriteLine($"  {i + 1}. {instructions[i]}");
            }

            Console.WriteLine();
        }
    }

    /// <inheritdoc />
    public Task<AuthDecision> PromptForAuthenticationAsync(CancellationToken ct = default)
    {
        // Non-interactive mode always skips authentication
        lock (_lock)
        {
            Console.WriteLine("ℹ Skipping authentication (non-interactive mode)");
        }

        return Task.FromResult(AuthDecision.Skip);
    }

    private static string GetStatusIcon(PrerequisiteState state) => state switch
    {
        PrerequisiteState.Satisfied => "✓",
        PrerequisiteState.Missing => "✗",
        PrerequisiteState.Outdated => "⚠",
        PrerequisiteState.Misconfigured => "⚠",
        _ => "?"
    };

    private static string GetStateText(PrerequisiteState state) => state switch
    {
        PrerequisiteState.Satisfied => "OK",
        PrerequisiteState.Missing => "Missing",
        PrerequisiteState.Outdated => "Outdated",
        PrerequisiteState.Misconfigured => "Misconfigured",
        _ => "Unknown"
    };
}
