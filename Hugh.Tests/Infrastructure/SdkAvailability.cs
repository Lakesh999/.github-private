using System.Diagnostics;

namespace Hugh.Tests.Infrastructure;

/// <summary>
/// Centralized SDK availability detection for test infrastructure.
/// Checks for GitHub Copilot CLI and authentication, caches the result,
/// and respects the HUGH_SKIP_SDK_TESTS environment variable.
/// </summary>
public static class SdkAvailability
{
    private static bool? _cachedResult;
    private static readonly object Lock = new();

    /// <summary>
    /// Process timeout for CLI checks (5 seconds).
    /// </summary>
    private const int ProcessTimeoutMs = 5000;

    /// <summary>
    /// Returns true if the GitHub Copilot SDK is available for live tests.
    /// Checks (in order):
    /// 1. HUGH_SKIP_SDK_TESTS env var — if set to "true"/"1", returns false immediately
    /// 2. `gh copilot --version` — must exit with code 0
    /// 3. `gh auth status` — must exit with code 0
    /// Result is cached for the lifetime of the test process.
    /// </summary>
    public static bool IsSdkAvailable()
    {
        if (_cachedResult.HasValue)
            return _cachedResult.Value;

        lock (Lock)
        {
            // Double-check after acquiring lock
            if (_cachedResult.HasValue)
                return _cachedResult.Value;

            _cachedResult = DetectSdkAvailability();
            return _cachedResult.Value;
        }
    }

    /// <summary>
    /// Resets the cached result. Intended for test isolation only.
    /// </summary>
    internal static void Reset()
    {
        lock (Lock)
        {
            _cachedResult = null;
        }
    }

    private static bool DetectSdkAvailability()
    {
        // Check environment variable override
        var skipEnv = Environment.GetEnvironmentVariable("HUGH_SKIP_SDK_TESTS");
        if (string.Equals(skipEnv, "true", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(skipEnv, "1", StringComparison.Ordinal))
        {
            return false;
        }

        // Check gh copilot --version
        if (!RunCliCheck("gh", "copilot --version"))
            return false;

        // Check gh auth status
        if (!RunCliCheck("gh", "auth status"))
            return false;

        return true;
    }

    /// <summary>
    /// Runs a CLI command and returns true if it exits with code 0 within the timeout.
    /// Follows the same Process.Start pattern as the original IsCopilotCliAvailable().
    /// </summary>
    private static bool RunCliCheck(string fileName, string arguments)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return false;

            process.WaitForExit(ProcessTimeoutMs);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
