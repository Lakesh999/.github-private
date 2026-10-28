using Hugh.Agents;
using Hugh.Detection;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for the retry delay calculation and error-classified retry behavior
/// in CopilotService.SendWithRetryAsync.
/// </summary>
public class CopilotServiceRetryTests
{
    private readonly SessionErrorClassifier _classifier = new();

    #region CalculateRetryDelay Tests

    [Fact]
    public void CalculateRetryDelay_RateLimited_ReturnsFlatDelay()
    {
        // Arrange: Rate-limited classification with 60s suggested delay
        var classification = new ErrorClassification
        {
            Category = ErrorCategory.RateLimited,
            IsRetryable = true,
            SuggestedDelay = TimeSpan.FromSeconds(60)
        };

        // Act: Calculate delay for multiple attempts
        var delay1 = CopilotService.CalculateRetryDelay(classification, attempt: 1, retryBaseDelayMs: 2000);
        var delay2 = CopilotService.CalculateRetryDelay(classification, attempt: 2, retryBaseDelayMs: 2000);
        var delay3 = CopilotService.CalculateRetryDelay(classification, attempt: 3, retryBaseDelayMs: 2000);

        // Assert: All attempts get the same flat 60s delay (no exponential backoff)
        Assert.Equal(TimeSpan.FromSeconds(60), delay1);
        Assert.Equal(TimeSpan.FromSeconds(60), delay2);
        Assert.Equal(TimeSpan.FromSeconds(60), delay3);
    }

    [Fact]
    public void CalculateRetryDelay_Transient_ReturnsExponentialBackoff()
    {
        // Arrange: Transient classification with 2s base delay
        var classification = new ErrorClassification
        {
            Category = ErrorCategory.Transient,
            IsRetryable = true,
            SuggestedDelay = TimeSpan.FromSeconds(2)
        };

        // Act: Calculate delay for successive attempts
        var delay1 = CopilotService.CalculateRetryDelay(classification, attempt: 1, retryBaseDelayMs: 2000);
        var delay2 = CopilotService.CalculateRetryDelay(classification, attempt: 2, retryBaseDelayMs: 2000);
        var delay3 = CopilotService.CalculateRetryDelay(classification, attempt: 3, retryBaseDelayMs: 2000);

        // Assert: Exponential backoff: 2s, 4s, 8s
        Assert.Equal(TimeSpan.FromSeconds(2), delay1);
        Assert.Equal(TimeSpan.FromSeconds(4), delay2);
        Assert.Equal(TimeSpan.FromSeconds(8), delay3);
    }

    [Fact]
    public void CalculateRetryDelay_Transient_UsesConfigFallbackWhenNoSuggestedDelay()
    {
        // Arrange: Transient classification with no suggested delay
        var classification = new ErrorClassification
        {
            Category = ErrorCategory.Transient,
            IsRetryable = true,
            SuggestedDelay = null
        };

        // Act: Should fall back to retryBaseDelayMs (3000ms)
        var delay1 = CopilotService.CalculateRetryDelay(classification, attempt: 1, retryBaseDelayMs: 3000);
        var delay2 = CopilotService.CalculateRetryDelay(classification, attempt: 2, retryBaseDelayMs: 3000);

        // Assert: Exponential backoff with 3s base: 3s, 6s
        Assert.Equal(TimeSpan.FromSeconds(3), delay1);
        Assert.Equal(TimeSpan.FromSeconds(6), delay2);
    }

    [Fact]
    public void CalculateRetryDelay_RateLimited_UsesSuggestedDelayOverConfig()
    {
        // Arrange: Rate-limited with explicit 60s suggested delay; config says 2000ms
        var classification = new ErrorClassification
        {
            Category = ErrorCategory.RateLimited,
            IsRetryable = true,
            SuggestedDelay = TimeSpan.FromSeconds(60)
        };

        // Act
        var delay = CopilotService.CalculateRetryDelay(classification, attempt: 1, retryBaseDelayMs: 2000);

        // Assert: Uses the 60s suggested delay, not the 2s config fallback
        Assert.Equal(TimeSpan.FromSeconds(60), delay);
    }

    #endregion

    #region Classifier-Driven Retry Decision Tests

    [Fact]
    public void Classify_RateLimitedError_IsRetryableWith60sDelay()
    {
        // Arrange & Act: Classify a rate-limited error
        var classification = _classifier.Classify("429 Too Many Requests");

        // Assert: Retryable with 60s suggested delay
        Assert.Equal(ErrorCategory.RateLimited, classification.Category);
        Assert.True(classification.IsRetryable);
        Assert.Equal(TimeSpan.FromSeconds(60), classification.SuggestedDelay);
    }

    [Fact]
    public void Classify_TransientError_IsRetryableWith2sBaseDelay()
    {
        // Arrange & Act: Classify a transient error
        var classification = _classifier.Classify("Connection reset by peer");

        // Assert: Retryable with 2s base delay for exponential backoff
        Assert.Equal(ErrorCategory.Transient, classification.Category);
        Assert.True(classification.IsRetryable);
        Assert.Equal(TimeSpan.FromSeconds(2), classification.SuggestedDelay);
    }

    [Fact]
    public void Classify_AuthenticationError_IsNotRetryable()
    {
        // Arrange & Act: Classify an authentication error
        var classification = _classifier.Classify("401 Unauthorized");

        // Assert: Not retryable — should be re-thrown immediately
        Assert.Equal(ErrorCategory.Authentication, classification.Category);
        Assert.False(classification.IsRetryable);
        Assert.Null(classification.SuggestedDelay);
    }

    [Fact]
    public void Classify_ContextExhaustedError_IsNotRetryable()
    {
        // Arrange & Act: Classify a context exhaustion error
        var classification = _classifier.Classify("Context window exceeded max tokens");

        // Assert: Not retryable — should be re-thrown immediately
        Assert.Equal(ErrorCategory.ContextExhausted, classification.Category);
        Assert.False(classification.IsRetryable);
        Assert.Null(classification.SuggestedDelay);
    }

    #endregion

    #region End-to-End Delay Flow Tests

    [Fact]
    public void RateLimitedError_FullFlow_Gets60sFlatDelay()
    {
        // End-to-end: classify a rate-limited exception and calculate the retry delay
        // Use base Exception (not HttpRequestException) so message-based classification applies
        var ex = new Exception("Rate limit exceeded (429 Too Many Requests)");
        var classification = _classifier.Classify(ex);
        var delay = CopilotService.CalculateRetryDelay(classification, attempt: 2, retryBaseDelayMs: 2000);

        // Rate-limited uses flat delay regardless of attempt number
        Assert.Equal(ErrorCategory.RateLimited, classification.Category);
        Assert.Equal(TimeSpan.FromSeconds(60), delay);
    }

    [Fact]
    public void TransientError_FullFlow_GetsExponentialBackoff()
    {
        // End-to-end: classify a transient exception and calculate retry delays
        // HttpRequestException is classified as Transient by exception type
        var ex = new HttpRequestException("Connection refused");
        var classification = _classifier.Classify(ex);

        var delay1 = CopilotService.CalculateRetryDelay(classification, attempt: 1, retryBaseDelayMs: 2000);
        var delay2 = CopilotService.CalculateRetryDelay(classification, attempt: 2, retryBaseDelayMs: 2000);
        var delay3 = CopilotService.CalculateRetryDelay(classification, attempt: 3, retryBaseDelayMs: 2000);

        // Transient uses exponential backoff: 2s base → 2s, 4s, 8s
        Assert.Equal(TimeSpan.FromSeconds(2), delay1);
        Assert.Equal(TimeSpan.FromSeconds(4), delay2);
        Assert.Equal(TimeSpan.FromSeconds(8), delay3);
    }

    [Fact]
    public void AuthenticationError_FullFlow_IsNotRetryable()
    {
        // End-to-end: auth errors should not be retried (SendWithRetryAsync re-throws)
        // Use base Exception (not HttpRequestException) so message-based classification applies
        var ex = new Exception("Authentication failed (401 Unauthorized)");
        var classification = _classifier.Classify(ex);

        // The retry logic checks IsRetryable before calculating delay
        Assert.Equal(ErrorCategory.Authentication, classification.Category);
        Assert.False(classification.IsRetryable);
        // Delay is irrelevant since the error would be re-thrown
    }

    #endregion
}
