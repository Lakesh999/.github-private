namespace Hugh.Tests.Infrastructure;

/// <summary>
/// Thread-safe token budget tracker for controlling total token consumption
/// across SDK test runs. Reads the budget limit from the HUGH_TEST_TOKEN_BUDGET
/// environment variable (default: 50000).
/// </summary>
public static class TokenBudgetTracker
{
    /// <summary>
    /// Default token budget when env var is not set.
    /// </summary>
    private const int DefaultBudget = 50_000;

    /// <summary>
    /// Environment variable name for overriding the token budget.
    /// </summary>
    private const string BudgetEnvVar = "HUGH_TEST_TOKEN_BUDGET";

    private static int _totalUsed;
    private static readonly int Budget = ReadBudget();

    /// <summary>
    /// Records token usage and returns true if the total remains within budget.
    /// Thread-safe via <see cref="Interlocked.Add"/>.
    /// </summary>
    /// <param name="tokens">Number of tokens consumed.</param>
    /// <returns>True if total usage (including this addition) is within the budget limit.</returns>
    public static bool RecordUsage(int tokens)
    {
        var newTotal = Interlocked.Add(ref _totalUsed, tokens);
        return newTotal <= Budget;
    }

    /// <summary>
    /// Returns true if the current total usage is within the budget limit.
    /// </summary>
    public static bool IsWithinBudget()
    {
        return Volatile.Read(ref _totalUsed) <= Budget;
    }

    /// <summary>
    /// Gets the current total token usage.
    /// </summary>
    public static int CurrentUsage => Volatile.Read(ref _totalUsed);

    /// <summary>
    /// Gets the configured budget limit.
    /// </summary>
    public static int BudgetLimit => Budget;

    /// <summary>
    /// Resets the usage counter to zero. Intended for test isolation.
    /// </summary>
    public static void Reset()
    {
        Interlocked.Exchange(ref _totalUsed, 0);
    }

    private static int ReadBudget()
    {
        var envValue = Environment.GetEnvironmentVariable(BudgetEnvVar);
        if (!string.IsNullOrEmpty(envValue) && int.TryParse(envValue, out var parsed) && parsed > 0)
        {
            return parsed;
        }

        return DefaultBudget;
    }
}
