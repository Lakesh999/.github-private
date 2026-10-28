using Hugh.Agents;
using Hugh.Detection;

namespace Hugh.Tests.Infrastructure;

/// <summary>
/// Provides retry logic for SDK-dependent tests using the same
/// error classification and backoff strategies as production code.
/// </summary>
public static class SdkTestRetry
{
    private static readonly SessionErrorClassifier ErrorClassifier = new();

    /// <summary>
    /// Default base delay for retry backoff (milliseconds).
    /// Matches the production default in CopilotConfig.
    /// </summary>
    private const int DefaultBaseDelayMs = 2000;

    /// <summary>
    /// Executes an async action with retry logic for transient SDK errors.
    /// Uses <see cref="SessionErrorClassifier"/> to determine retryability
    /// and <see cref="CopilotService.CalculateRetryDelay"/> for backoff timing.
    /// </summary>
    /// <param name="action">The async action to execute.</param>
    /// <param name="maxRetries">Maximum number of retry attempts (default: 2).</param>
    /// <param name="baseDelayMs">Base delay in milliseconds for backoff calculation (default: 2000).</param>
    /// <exception cref="Exception">
    /// Rethrows immediately for non-retryable errors.
    /// Rethrows the last exception after all retries are exhausted.
    /// </exception>
    public static async Task WithRetryAsync(Func<Task> action, int maxRetries = 2, int baseDelayMs = DefaultBaseDelayMs)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            try
            {
                await action();
                return; // Success
            }
            catch (Exception ex)
            {
                lastException = ex;

                var classification = ErrorClassifier.Classify(ex);

                // Non-retryable errors fail immediately
                if (!classification.IsRetryable)
                    throw;

                // No more retries — rethrow
                if (attempt > maxRetries)
                    throw;

                // Calculate delay using production backoff logic
                var delay = CopilotService.CalculateRetryDelay(classification, attempt, baseDelayMs);
                await Task.Delay(delay);
            }
        }

        // Should not reach here, but satisfy compiler
        if (lastException != null) throw lastException;
    }
}
