namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether the GitHub CLI (gh) is installed.
/// Runs 'gh --version' to verify installation.
/// </summary>
public class GitHubCliDetector : IPrerequisiteDetector
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubCliDetector"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner for executing CLI commands.</param>
    public GitHubCliDetector(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "github-cli";

    /// <inheritdoc/>
    public string DisplayName => "GitHub CLI";

    /// <inheritdoc/>
    public int Priority => 30;

    /// <inheritdoc/>
    public bool IsInstallable => true;

    /// <inheritdoc/>
    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _processRunner.RunAsync("gh", "--version", ct);

            if (result.ExitCode == 0)
            {
                var version = ParseVersion(result.StandardOutput);
                return new PrerequisiteStatus
                {
                    PrerequisiteId = PrerequisiteId,
                    State = PrerequisiteState.Satisfied,
                    Version = version,
                    Message = $"GitHub CLI {version} is installed"
                };
            }

            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "GitHub CLI is not installed or not working correctly",
                Hints = new[]
                {
                    "Install GitHub CLI: winget install GitHub.cli",
                    "Or download from: https://cli.github.com/"
                }
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "GitHub CLI executable not found",
                Hints = new[]
                {
                    "Install GitHub CLI: winget install GitHub.cli",
                    "Or download from: https://cli.github.com/"
                }
            };
        }
    }

    private static string ParseVersion(string output)
    {
        // gh --version outputs something like "gh version 2.45.0 (2024-03-14)"
        // Extract just the version number
        var trimmed = output.Trim();
        var lines = trimmed.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        if (lines.Length > 0)
        {
            var firstLine = lines[0];
            var parts = firstLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            
            // Look for a part that looks like a version number
            foreach (var part in parts)
            {
                if (char.IsDigit(part.FirstOrDefault()))
                {
                    return part;
                }
            }
        }

        // If no version found, return the entire first line trimmed
        return lines.Length > 0 ? lines[0].Trim() : trimmed;
    }
}
