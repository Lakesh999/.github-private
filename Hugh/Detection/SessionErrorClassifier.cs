namespace Hugh.Detection;

/// <summary>
/// Category of session error for classification and handling decisions.
/// </summary>
public enum ErrorCategory
{
    /// <summary>
    /// Network, timeout, connection reset, 502/503 errors.
    /// </summary>
    Transient,

    /// <summary>
    /// Rate limit responses (429).
    /// </summary>
    RateLimited,

    /// <summary>
    /// Authentication failures (401, unauthorized).
    /// </summary>
    Authentication,

    /// <summary>
    /// Tool execution errors.
    /// </summary>
    ToolFailure,

    /// <summary>
    /// Context window full or token limit exceeded.
    /// </summary>
    ContextExhausted,

    /// <summary>
    /// Unclassified or unknown errors.
    /// </summary>
    Unknown
}

/// <summary>
/// Classification result containing error category and handling recommendations.
/// </summary>
public record ErrorClassification
{
    /// <summary>
    /// The category of the error.
    /// </summary>
    public ErrorCategory Category { get; init; }

    /// <summary>
    /// Whether the error is retryable (true for Transient, RateLimited).
    /// </summary>
    public bool IsRetryable { get; init; }

    /// <summary>
    /// Suggested delay before retry, if applicable.
    /// </summary>
    public TimeSpan? SuggestedDelay { get; init; }

    /// <summary>
    /// Suggested action for handling the error.
    /// </summary>
    public string? SuggestedAction { get; init; }
}

/// <summary>
/// Classifies session errors into categories with retry and delay recommendations.
/// </summary>
public class SessionErrorClassifier
{
    /// <summary>
    /// Default delay for rate-limited errors (60 seconds).
    /// </summary>
    private static readonly TimeSpan RateLimitDelay = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Base delay for transient errors (2 seconds, used with exponential backoff).
    /// </summary>
    private static readonly TimeSpan TransientBaseDelay = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Pattern keywords for each error category.
    /// </summary>
    private static readonly string[] TransientPatterns =
    {
        "timeout", "connection reset", "connection refused", "network",
        "502", "503", "service unavailable", "temporarily unavailable",
        "gateway timeout", "bad gateway"
    };

    private static readonly string[] RateLimitPatterns =
    {
        "rate limit", "rate-limit", "ratelimit", "429", "too many requests",
        "throttled", "quota exceeded"
    };

    private static readonly string[] AuthPatterns =
    {
        "unauthorized", "401", "authentication", "auth failed",
        "forbidden", "403", "invalid token", "token expired"
    };

    private static readonly string[] ContextPatterns =
    {
        "context window", "context limit", "token limit", "max tokens",
        "too long", "context exceeded", "context exhausted", "context full"
    };

    private static readonly string[] ToolPatterns =
    {
        "tool", "execution failed", "tool error", "tool execution",
        "function call failed", "tool not found"
    };

    /// <summary>
    /// Classifies an error based on the exception or error message.
    /// </summary>
    /// <param name="ex">The exception to classify.</param>
    /// <returns>An ErrorClassification with category and handling recommendations.</returns>
    public ErrorClassification Classify(Exception ex)
    {
        ArgumentNullException.ThrowIfNull(ex);
        return Classify(ex.Message, ex.GetType());
    }

    /// <summary>
    /// Classifies an error based on the error message.
    /// </summary>
    /// <param name="errorMessage">The error message to classify.</param>
    /// <returns>An ErrorClassification with category and handling recommendations.</returns>
    public ErrorClassification Classify(string errorMessage)
    {
        return Classify(errorMessage, null);
    }

    /// <summary>
    /// Classifies an error based on message and optional exception type.
    /// </summary>
    private ErrorClassification Classify(string errorMessage, Type? exceptionType)
    {
        if (string.IsNullOrWhiteSpace(errorMessage))
        {
            return CreateClassification(ErrorCategory.Unknown);
        }

        var message = errorMessage.ToLowerInvariant();

        // Check for transient errors (includes exception type check)
        if (exceptionType is not null &&
            (exceptionType == typeof(HttpRequestException) ||
             exceptionType == typeof(TimeoutException) ||
             exceptionType == typeof(TaskCanceledException)))
        {
            return CreateClassification(ErrorCategory.Transient);
        }

        if (MatchesAny(message, TransientPatterns))
        {
            return CreateClassification(ErrorCategory.Transient);
        }

        // Check for rate limiting
        if (MatchesAny(message, RateLimitPatterns))
        {
            return CreateClassification(ErrorCategory.RateLimited);
        }

        // Check for authentication errors
        if (MatchesAny(message, AuthPatterns))
        {
            return CreateClassification(ErrorCategory.Authentication);
        }

        // Check for context exhaustion
        if (MatchesAny(message, ContextPatterns))
        {
            return CreateClassification(ErrorCategory.ContextExhausted);
        }

        // Check for tool failures
        if (MatchesAny(message, ToolPatterns))
        {
            return CreateClassification(ErrorCategory.ToolFailure);
        }

        return CreateClassification(ErrorCategory.Unknown);
    }

    /// <summary>
    /// Checks if the message matches any of the given patterns.
    /// </summary>
    private static bool MatchesAny(string message, string[] patterns)
    {
        foreach (var pattern in patterns)
        {
            if (message.Contains(pattern, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Creates an ErrorClassification for the given category.
    /// </summary>
    private static ErrorClassification CreateClassification(ErrorCategory category)
    {
        return category switch
        {
            ErrorCategory.Transient => new ErrorClassification
            {
                Category = ErrorCategory.Transient,
                IsRetryable = true,
                SuggestedDelay = TransientBaseDelay,
                SuggestedAction = "Retry with exponential backoff"
            },
            ErrorCategory.RateLimited => new ErrorClassification
            {
                Category = ErrorCategory.RateLimited,
                IsRetryable = true,
                SuggestedDelay = RateLimitDelay,
                SuggestedAction = "Wait for rate limit window to reset"
            },
            ErrorCategory.Authentication => new ErrorClassification
            {
                Category = ErrorCategory.Authentication,
                IsRetryable = false,
                SuggestedDelay = null,
                SuggestedAction = "Re-authenticate or check credentials"
            },
            ErrorCategory.ContextExhausted => new ErrorClassification
            {
                Category = ErrorCategory.ContextExhausted,
                IsRetryable = false,
                SuggestedDelay = null,
                SuggestedAction = "Start new session with fresh context"
            },
            ErrorCategory.ToolFailure => new ErrorClassification
            {
                Category = ErrorCategory.ToolFailure,
                IsRetryable = false,
                SuggestedDelay = null,
                SuggestedAction = "Check tool configuration and parameters"
            },
            _ => new ErrorClassification
            {
                Category = ErrorCategory.Unknown,
                IsRetryable = false,
                SuggestedDelay = null,
                SuggestedAction = "Investigate error details"
            }
        };
    }
}
