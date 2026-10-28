using Hugh.Setup.Detection;

namespace Hugh.Setup.Installation;

/// <summary>
/// Installs prerequisites using Windows Package Manager (winget).
/// </summary>
public class WingetInstaller : IInstaller
{
    private readonly IProcessRunner _processRunner;
    private bool? _isAvailable;

    /// <summary>
    /// Maps prerequisite IDs to winget package IDs.
    /// </summary>
    private static readonly Dictionary<string, string> PackageMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        ["copilot-cli"] = "GitHub.Copilot",
        ["github-cli"] = "GitHub.cli",
        ["git"] = "Git.Git",
        ["dotnet-sdk"] = "Microsoft.DotNet.SDK.8"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="WingetInstaller"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner for executing winget commands.</param>
    public WingetInstaller(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc />
    public string InstallerId => "winget";

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        // Cache the result to avoid repeated checks
        if (_isAvailable.HasValue)
        {
            return _isAvailable.Value;
        }

        try
        {
            var result = await _processRunner.RunAsync("winget", "--version", ct);
            _isAvailable = result.ExitCode == 0 && !string.IsNullOrWhiteSpace(result.StandardOutput);
            return _isAvailable.Value;
        }
        catch
        {
            _isAvailable = false;
            return false;
        }
    }

    /// <inheritdoc />
    public bool CanInstall(string prerequisiteId)
    {
        return PackageMappings.ContainsKey(prerequisiteId);
    }

    /// <inheritdoc />
    public async Task<InstallationResult> InstallAsync(string prerequisiteId, CancellationToken ct = default)
    {
        if (!CanInstall(prerequisiteId))
        {
            return new InstallationResult
            {
                Success = false,
                ErrorMessage = $"WingetInstaller does not support prerequisite: {prerequisiteId}"
            };
        }

        if (!await IsAvailableAsync(ct))
        {
            return new InstallationResult
            {
                Success = false,
                ErrorMessage = "winget is not available on this system"
            };
        }

        var packageId = PackageMappings[prerequisiteId];
        var arguments = $"install {packageId} --silent --accept-source-agreements --accept-package-agreements --scope user";

        try
        {
            var result = await _processRunner.RunAsync("winget", arguments, ct);

            if (result.ExitCode == 0)
            {
                // Try to extract version from output if available
                var version = ExtractVersionFromOutput(result.StandardOutput);

                return new InstallationResult
                {
                    Success = true,
                    Version = version,
                    RequiresRestart = true // PATH may need refresh
                };
            }

            // Check for "already installed" scenario (exit code varies)
            if (result.StandardOutput.Contains("already installed", StringComparison.OrdinalIgnoreCase) ||
                result.StandardError.Contains("already installed", StringComparison.OrdinalIgnoreCase))
            {
                return new InstallationResult
                {
                    Success = true,
                    RequiresRestart = false
                };
            }

            return new InstallationResult
            {
                Success = false,
                ErrorMessage = !string.IsNullOrWhiteSpace(result.StandardError)
                    ? result.StandardError.Trim()
                    : $"winget install failed with exit code {result.ExitCode}"
            };
        }
        catch (Exception ex)
        {
            return new InstallationResult
            {
                Success = false,
                ErrorMessage = $"Failed to execute winget: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Attempts to extract version information from winget output.
    /// </summary>
    private static string? ExtractVersionFromOutput(string output)
    {
        // Winget output varies, but successful installs often contain version info
        // This is a best-effort extraction
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (line.Contains("version", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("v", StringComparison.OrdinalIgnoreCase))
            {
                // Look for version-like patterns (e.g., "1.0.0", "v1.2.3")
                var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                foreach (var part in parts)
                {
                    var trimmed = part.TrimStart('v', 'V');
                    if (Version.TryParse(trimmed.Split('-')[0], out _))
                    {
                        return trimmed;
                    }
                }
            }
        }
        return null;
    }
}
