namespace Hugh.Configuration;

/// <summary>
/// Static utility class for resolving paths in both development and installed scenarios.
/// Enables Hugh to run correctly from any working directory.
/// </summary>
public static class PathResolver
{
    private static string? _installationPath;

    /// <summary>
    /// Gets the installation path where Hugh's configuration and agents reside.
    /// Resolution order: ENV variable → Assembly location → CWD fallback.
    /// </summary>
    public static string GetInstallationPath()
    {
        if (_installationPath != null) return _installationPath;

        // 1. Check environment variable override
        var envPath = Environment.GetEnvironmentVariable("HUGH_CONFIG_PATH");
        if (!string.IsNullOrEmpty(envPath) &&
            File.Exists(Path.Combine(envPath, "appsettings.json")))
        {
            _installationPath = envPath;
            return _installationPath;
        }

        // 2. Try assembly location (installed global CLI)
        // Use AppContext.BaseDirectory - works correctly for single-file publish
        var assemblyPath = AppContext.BaseDirectory;
        if (File.Exists(Path.Combine(assemblyPath, "appsettings.json")))
        {
            _installationPath = assemblyPath;
            return _installationPath;
        }

        // 3. Fall back to CWD (dev mode)
        _installationPath = Directory.GetCurrentDirectory();
        return _installationPath;
    }

    /// <summary>
    /// Gets the user's Hugh data directory (~/.hugh).
    /// Creates directory if it doesn't exist.
    /// </summary>
    public static string GetUserDataPath()
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var hughPath = Path.Combine(userProfile, ".hugh");
        Directory.CreateDirectory(hughPath);
        return hughPath;
    }

    /// <summary>
    /// Gets the agents cache directory path (~/.hugh/agents).
    /// Creates directory if it doesn't exist.
    /// </summary>
    public static string GetAgentsCachePath()
    {
        var cachePath = Path.Combine(GetUserDataPath(), "agents");
        Directory.CreateDirectory(cachePath);
        return cachePath;
    }

    /// <summary>
    /// Gets the log directory path (~/.hugh/logs).
    /// Creates directory if it doesn't exist.
    /// </summary>
    public static string GetLogPath()
    {
        var logPath = Path.Combine(GetUserDataPath(), "logs");
        Directory.CreateDirectory(logPath);
        return logPath;
    }

    /// <summary>
    /// Resets the cached installation path. Used for testing.
    /// </summary>
    internal static void ResetCachedPath()
    {
        _installationPath = null;
    }
}
