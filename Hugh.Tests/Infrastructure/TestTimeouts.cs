namespace Hugh.Tests.Infrastructure;

/// <summary>
/// Provides environment-aware timeout values for test categories.
/// CI environments (detected via CI or GITHUB_ACTIONS env vars) receive
/// longer timeouts to account for slower runners.
/// </summary>
public static class TestTimeouts
{
    /// <summary>
    /// Whether the current environment is a CI runner.
    /// </summary>
    public static bool IsCi { get; } = DetectCi();

    /// <summary>
    /// Timeout values per category: (local, CI) in seconds.
    /// </summary>
    private static readonly Dictionary<string, (int LocalSeconds, int CiSeconds)> TimeoutMap = new(StringComparer.OrdinalIgnoreCase)
    {
        [TestCategory.Unit] = (5, 10),
        [TestCategory.Integration] = (15, 30),
        [TestCategory.Smoke] = (30, 60),
        [TestCategory.LiveSdk] = (120, 180),
        [TestCategory.E2E] = (300, 600),
    };

    /// <summary>
    /// Creates a <see cref="CancellationTokenSource"/> with the appropriate timeout
    /// for the given test category and current environment.
    /// </summary>
    /// <param name="category">One of the <see cref="TestCategory"/> constants.</param>
    /// <returns>A CancellationTokenSource that will cancel after the configured timeout.</returns>
    /// <exception cref="ArgumentException">Thrown when category is not recognized.</exception>
    public static CancellationTokenSource CreateTimeout(string category)
    {
        if (!TimeoutMap.TryGetValue(category, out var values))
        {
            throw new ArgumentException(
                $"Unknown test category '{category}'. Use TestCategory constants.",
                nameof(category));
        }

        var seconds = IsCi ? values.CiSeconds : values.LocalSeconds;
        return new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
    }

    /// <summary>
    /// Gets the timeout duration for the given category without creating a CTS.
    /// </summary>
    public static TimeSpan GetTimeout(string category)
    {
        if (!TimeoutMap.TryGetValue(category, out var values))
        {
            throw new ArgumentException(
                $"Unknown test category '{category}'. Use TestCategory constants.",
                nameof(category));
        }

        var seconds = IsCi ? values.CiSeconds : values.LocalSeconds;
        return TimeSpan.FromSeconds(seconds);
    }

    private static bool DetectCi()
    {
        // Standard CI env var (used by most CI systems)
        var ci = Environment.GetEnvironmentVariable("CI");
        if (string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase))
            return true;

        // GitHub Actions specific
        var ghActions = Environment.GetEnvironmentVariable("GITHUB_ACTIONS");
        if (string.Equals(ghActions, "true", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }
}
