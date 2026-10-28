namespace Hugh.Models;

/// <summary>
/// Token usage and model information captured from an AssistantUsageEvent.
/// </summary>
public sealed record UsageMetrics
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int ReasoningTokens { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Model { get; init; }
}
