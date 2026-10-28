namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether the GitHub Copilot CLI is installed.
/// Runs 'copilot --version' to verify installation.
/// </summary>
public class CopilotCliDetector : IPrerequisiteDetector
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="CopilotCliDetector"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner for executing CLI commands.</param>
    public CopilotCliDetector(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "copilot-cli";

    /// <inheritdoc/>
    public string DisplayName => "GitHub Copilot CLI";

    /// <inheritdoc/>
    public int Priority => 40;

    /// <inheritdoc/>
    public bool IsInstallable => true;

    /// <inheritdoc/>
    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _processRunner.RunAsync("copilot", "--version", ct);

            if (result.ExitCode == 0)
            {
                var version = ParseVersion(result.StandardOutput);
                return new PrerequisiteStatus
                {
                    PrerequisiteId = PrerequisiteId,
                    State = PrerequisiteState.Satisfied,
                    Version = version,
                    Message = $"GitHub Copilot CLI {version} is installed"
                };
            }

            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Satisfied,
                Message = "Standalone Copilot CLI not found — SDK bundles CLI internally (optional for debugging)"
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Satisfied,
                Message = "Standalone Copilot CLI not found — SDK bundles CLI internally (optional for debugging)"
            };
        }
    }

    private static string ParseVersion(string output)
    {
        // copilot --version typically outputs something like "copilot version 1.2.3"
        // Extract just the version number
        var trimmed = output.Trim();
        var parts = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        // Look for a part that looks like a version number
        foreach (var part in parts)
        {
            if (char.IsDigit(part.FirstOrDefault()))
            {
                return part;
            }
        }

        // If no version found, return the entire output trimmed
        return trimmed;
    }
}
