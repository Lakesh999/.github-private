namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether Git is installed.
/// Runs 'git --version' to verify installation.
/// Git is needed by agents (e.g., pch-coder) for version control operations during implementation.
/// </summary>
public class GitDetector : IPrerequisiteDetector
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitDetector"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner for executing CLI commands.</param>
    public GitDetector(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "git";

    /// <inheritdoc/>
    public string DisplayName => "Git";

    /// <inheritdoc/>
    public int Priority => 25;

    /// <inheritdoc/>
    public bool IsInstallable => true;

    /// <inheritdoc/>
    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _processRunner.RunAsync("git", "--version", ct);

            if (result.ExitCode == 0)
            {
                var version = ParseVersion(result.StandardOutput);
                return new PrerequisiteStatus
                {
                    PrerequisiteId = PrerequisiteId,
                    State = PrerequisiteState.Satisfied,
                    Version = version,
                    Message = $"Git {version} is installed"
                };
            }

            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "Git is not installed or not working correctly",
                Hints = new[]
                {
                    "Install Git: winget install Git.Git",
                    "Or download from: https://git-scm.com/downloads"
                }
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "Git executable not found",
                Hints = new[]
                {
                    "Install Git: winget install Git.Git",
                    "Or download from: https://git-scm.com/downloads"
                }
            };
        }
    }

    private static string ParseVersion(string output)
    {
        // git --version outputs something like "git version 2.43.0.windows.1"
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

        return trimmed;
    }
}
