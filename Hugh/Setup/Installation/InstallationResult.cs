namespace Hugh.Setup.Installation;

/// <summary>
/// Represents the result of an installation attempt.
/// </summary>
public record InstallationResult
{
    /// <summary>
    /// Gets a value indicating whether the installation succeeded.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Gets the installed version, if installation succeeded.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the error message if installation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets a value indicating whether a terminal/shell restart is required
    /// for PATH changes to take effect.
    /// </summary>
    public bool RequiresRestart { get; init; }

    /// <summary>
    /// Gets manual installation instructions when automatic installation is not available.
    /// Populated by ManualInstaller.
    /// </summary>
    public string[]? Instructions { get; init; }
}
