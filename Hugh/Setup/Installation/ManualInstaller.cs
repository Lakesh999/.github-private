namespace Hugh.Setup.Installation;

/// <summary>
/// A fallback installer that provides manual installation instructions.
/// Always available as a last resort when automated installers are not present.
/// </summary>
public class ManualInstaller : IInstaller
{
    /// <summary>
    /// Manual installation instructions for each prerequisite.
    /// </summary>
    private static readonly Dictionary<string, string[]> InstructionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["copilot-cli"] = new[]
        {
            "Install GitHub Copilot CLI:",
            "1. Open a browser and navigate to: https://docs.github.com/en/copilot/github-copilot-in-the-cli/installing-github-copilot-in-the-cli",
            "2. Follow the installation instructions for your operating system",
            "3. After installation, restart your terminal/shell",
            "4. Verify installation by running: copilot --version"
        },
        ["github-cli"] = new[]
        {
            "Install GitHub CLI:",
            "1. Open a browser and navigate to: https://cli.github.com/",
            "2. Download and run the installer for your operating system",
            "3. After installation, restart your terminal/shell",
            "4. Verify installation by running: gh --version"
        },
        ["github-auth"] = new[]
        {
            "Authenticate with GitHub:",
            "Option 1: Using GitHub CLI (if installed)",
            "  - Run: gh auth login --web",
            "  - Follow the browser prompts to authenticate",
            "",
            "Option 2: Using Copilot CLI",
            "  - Run: copilot",
            "  - Type /login and press Enter",
            "  - Follow the browser prompts to authenticate",
            "",
            "After authentication, restart your terminal/shell to ensure credentials are available."
        },
        ["git"] = new[]
        {
            "Install Git:",
            "1. Open a browser and navigate to: https://git-scm.com/downloads",
            "2. Download and run the installer for your operating system",
            "3. After installation, restart your terminal/shell",
            "4. Verify installation by running: git --version"
        },
        ["dotnet-sdk"] = new[]
        {
            "Install .NET SDK:",
            "1. Open a browser and navigate to: https://dotnet.microsoft.com/download",
            "2. Download and install the .NET 8.0 SDK (or later)",
            "3. After installation, restart your terminal/shell",
            "4. Verify installation by running: dotnet --version"
        }
    };

    /// <inheritdoc />
    public string InstallerId => "manual";

    /// <inheritdoc />
    public Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        // Manual installer is always available as a fallback
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public bool CanInstall(string prerequisiteId)
    {
        return InstructionMappings.ContainsKey(prerequisiteId);
    }

    /// <inheritdoc />
    public Task<InstallationResult> InstallAsync(string prerequisiteId, CancellationToken ct = default)
    {
        if (!CanInstall(prerequisiteId))
        {
            return Task.FromResult(new InstallationResult
            {
                Success = false,
                ErrorMessage = $"ManualInstaller does not have instructions for: {prerequisiteId}"
            });
        }

        var instructions = InstructionMappings[prerequisiteId];

        // Return success with instructions - the user must complete the installation manually
        return Task.FromResult(new InstallationResult
        {
            Success = true,
            RequiresRestart = true,
            Instructions = instructions
        });
    }
}
