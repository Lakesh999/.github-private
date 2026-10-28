using Hugh.Detection;

namespace Hugh.Tests;

/// <summary>
/// Unit tests for SessionErrorClassifier.
/// Verifies error classification into correct categories with appropriate recommendations.
/// </summary>
public class SessionErrorClassifierTests
{
    private readonly SessionErrorClassifier _classifier = new();

    #region Transient Error Tests

    [Fact]
    public void Classify_Timeout_ReturnsTransient()
    {
        var result = _classifier.Classify("Request timeout occurred");

        Assert.Equal(ErrorCategory.Transient, result.Category);
        Assert.True(result.IsRetryable);
        Assert.NotNull(result.SuggestedDelay);
    }

    [Fact]
    public void Classify_ConnectionReset_ReturnsTransient()
    {
        var result = _classifier.Classify("Connection reset by peer");

        Assert.Equal(ErrorCategory.Transient, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_503_ReturnsTransient()
    {
        var result = _classifier.Classify("HTTP 503 Service Unavailable");

        Assert.Equal(ErrorCategory.Transient, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_502_ReturnsTransient()
    {
        var result = _classifier.Classify("502 Bad Gateway");

        Assert.Equal(ErrorCategory.Transient, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_GatewayTimeout_ReturnsTransient()
    {
        var result = _classifier.Classify("504 Gateway Timeout");

        Assert.Equal(ErrorCategory.Transient, result.Category);
    }

    [Fact]
    public void Classify_TemporarilyUnavailable_ReturnsTransient()
    {
        var result = _classifier.Classify("Service temporarily unavailable, please retry");

        Assert.Equal(ErrorCategory.Transient, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_NetworkError_ReturnsTransient()
    {
        var result = _classifier.Classify("Network error: Failed to establish connection");

        Assert.Equal(ErrorCategory.Transient, result.Category);
    }

    [Fact]
    public void Classify_HttpRequestException_ReturnsTransient()
    {
        var ex = new HttpRequestException("Connection failed");
        var result = _classifier.Classify(ex);

        Assert.Equal(ErrorCategory.Transient, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_TimeoutException_ReturnsTransient()
    {
        var ex = new TimeoutException("Operation timed out");
        var result = _classifier.Classify(ex);

        Assert.Equal(ErrorCategory.Transient, result.Category);
    }

    [Fact]
    public void Classify_TaskCanceledException_ReturnsTransient()
    {
        var ex = new TaskCanceledException("HTTP request was cancelled");
        var result = _classifier.Classify(ex);

        Assert.Equal(ErrorCategory.Transient, result.Category);
    }

    #endregion

    #region Rate Limit Tests

    [Fact]
    public void Classify_RateLimit_ReturnsRateLimited()
    {
        var result = _classifier.Classify("Rate limit exceeded, please wait");

        Assert.Equal(ErrorCategory.RateLimited, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_429_ReturnsRateLimited()
    {
        var result = _classifier.Classify("HTTP 429 Too Many Requests");

        Assert.Equal(ErrorCategory.RateLimited, result.Category);
        Assert.True(result.IsRetryable);
    }

    [Fact]
    public void Classify_Throttled_ReturnsRateLimited()
    {
        var result = _classifier.Classify("Request throttled");

        Assert.Equal(ErrorCategory.RateLimited, result.Category);
    }

    [Fact]
    public void Classify_QuotaExceeded_ReturnsRateLimited()
    {
        var result = _classifier.Classify("API quota exceeded for this period");

        Assert.Equal(ErrorCategory.RateLimited, result.Category);
    }

    #endregion

    #region Authentication Tests

    [Fact]
    public void Classify_Unauthorized_ReturnsAuthentication()
    {
        var result = _classifier.Classify("401 Unauthorized");

        Assert.Equal(ErrorCategory.Authentication, result.Category);
        Assert.False(result.IsRetryable);
    }

    [Fact]
    public void Classify_AuthFailed_ReturnsAuthentication()
    {
        var result = _classifier.Classify("Authentication failed: invalid credentials");

        Assert.Equal(ErrorCategory.Authentication, result.Category);
    }

    [Fact]
    public void Classify_Forbidden_ReturnsAuthentication()
    {
        var result = _classifier.Classify("403 Forbidden - Access denied");

        Assert.Equal(ErrorCategory.Authentication, result.Category);
    }

    [Fact]
    public void Classify_TokenExpired_ReturnsAuthentication()
    {
        var result = _classifier.Classify("Token expired, please re-authenticate");

        Assert.Equal(ErrorCategory.Authentication, result.Category);
    }

    #endregion

    #region Context Exhausted Tests

    [Fact]
    public void Classify_ContextWindow_ReturnsContextExhausted()
    {
        var result = _classifier.Classify("Context window exceeded maximum size");

        Assert.Equal(ErrorCategory.ContextExhausted, result.Category);
        Assert.False(result.IsRetryable);
    }

    [Fact]
    public void Classify_TokenLimit_ReturnsContextExhausted()
    {
        var result = _classifier.Classify("Token limit reached");

        Assert.Equal(ErrorCategory.ContextExhausted, result.Category);
    }

    [Fact]
    public void Classify_MaxTokens_ReturnsContextExhausted()
    {
        var result = _classifier.Classify("Max tokens exceeded for request");

        Assert.Equal(ErrorCategory.ContextExhausted, result.Category);
    }

    [Fact]
    public void Classify_ContextExhaustedMessage_ReturnsContextExhausted()
    {
        var result = _classifier.Classify("Context exhausted, cannot process more input");

        Assert.Equal(ErrorCategory.ContextExhausted, result.Category);
    }

    #endregion

    #region Tool Failure Tests

    [Fact]
    public void Classify_ToolError_ReturnsToolFailure()
    {
        var result = _classifier.Classify("Tool error: file_read failed to execute");

        Assert.Equal(ErrorCategory.ToolFailure, result.Category);
        Assert.False(result.IsRetryable);
    }

    [Fact]
    public void Classify_ToolExecutionFailed_ReturnsToolFailure()
    {
        var result = _classifier.Classify("Tool execution failed: invalid parameters");

        Assert.Equal(ErrorCategory.ToolFailure, result.Category);
    }

    [Fact]
    public void Classify_ToolNotFound_ReturnsToolFailure()
    {
        var result = _classifier.Classify("Tool not found: unknown_tool");

        Assert.Equal(ErrorCategory.ToolFailure, result.Category);
    }

    [Fact]
    public void Classify_FunctionCallFailed_ReturnsToolFailure()
    {
        var result = _classifier.Classify("Function call failed with exception");

        Assert.Equal(ErrorCategory.ToolFailure, result.Category);
    }

    #endregion

    #region Unknown Error Tests

    [Fact]
    public void Classify_Unknown_ReturnsUnknown()
    {
        var result = _classifier.Classify("Something unexpected happened");

        Assert.Equal(ErrorCategory.Unknown, result.Category);
        Assert.False(result.IsRetryable);
    }

    [Fact]
    public void Classify_EmptyMessage_ReturnsUnknown()
    {
        var result = _classifier.Classify("");

        Assert.Equal(ErrorCategory.Unknown, result.Category);
    }

    [Fact]
    public void Classify_NullMessage_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _classifier.Classify((Exception)null!));
    }

    [Fact]
    public void Classify_GenericException_ReturnsUnknown()
    {
        var ex = new InvalidOperationException("Generic invalid operation");
        var result = _classifier.Classify(ex);

        Assert.Equal(ErrorCategory.Unknown, result.Category);
    }

    #endregion

    #region IsRetryable Tests

    [Theory]
    [InlineData("timeout", true)]
    [InlineData("rate limit exceeded", true)]
    [InlineData("unauthorized access", false)]
    [InlineData("context window full", false)]
    [InlineData("tool execution failed", false)]
    [InlineData("unknown error", false)]
    public void Classify_IsRetryable_CorrectPerCategory(string errorMessage, bool expectedRetryable)
    {
        var result = _classifier.Classify(errorMessage);

        Assert.Equal(expectedRetryable, result.IsRetryable);
    }

    #endregion

    #region SuggestedDelay Tests

    [Fact]
    public void Classify_Transient_SuggestsShortDelay()
    {
        var result = _classifier.Classify("timeout error");

        Assert.NotNull(result.SuggestedDelay);
        Assert.Equal(TimeSpan.FromSeconds(2), result.SuggestedDelay);
    }

    [Fact]
    public void Classify_RateLimited_SuggestsLongerDelay()
    {
        var result = _classifier.Classify("rate limit exceeded");

        Assert.NotNull(result.SuggestedDelay);
        Assert.Equal(TimeSpan.FromSeconds(60), result.SuggestedDelay);
    }

    [Fact]
    public void Classify_NonRetryable_NoSuggestedDelay()
    {
        var result = _classifier.Classify("authentication failed");

        Assert.Null(result.SuggestedDelay);
    }

    [Theory]
    [InlineData("timeout", "Retry with exponential backoff")]
    [InlineData("rate limit", "Wait for rate limit window to reset")]
    [InlineData("unauthorized", "Re-authenticate or check credentials")]
    [InlineData("context window exceeded", "Start new session with fresh context")]
    [InlineData("tool error", "Check tool configuration and parameters")]
    [InlineData("some unknown", "Investigate error details")]
    public void Classify_SuggestedAction_CorrectPerCategory(string errorMessage, string expectedAction)
    {
        var result = _classifier.Classify(errorMessage);

        Assert.Equal(expectedAction, result.SuggestedAction);
    }

    #endregion

    #region Case Insensitivity Tests

    [Theory]
    [InlineData("TIMEOUT")]
    [InlineData("Timeout")]
    [InlineData("TimeOut")]
    [InlineData("timeout")]
    public void Classify_IsCaseInsensitive(string errorMessage)
    {
        var result = _classifier.Classify(errorMessage);

        Assert.Equal(ErrorCategory.Transient, result.Category);
    }

    #endregion
}
