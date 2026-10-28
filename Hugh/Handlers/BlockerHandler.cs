using Hugh.Detection;
using Hugh.Models;
using Serilog;

namespace Hugh.Handlers;

/// <summary>
/// Response from blocker handling indicating the action taken.
/// </summary>
public record BlockerResponse
{
    /// <summary>
    /// Indicates execution should pause and wait for human intervention.
    /// </summary>
    public bool ShouldPause { get; init; } = true;

    /// <summary>
    /// Description of the blocker for logging and display.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// The raw blocker text from the agent output.
    /// </summary>
    public string? RawBlockerText { get; init; }

    /// <summary>
    /// Timestamp when the blocker was detected.
    /// </summary>
    public DateTime DetectedAt { get; init; }
}

/// <summary>
/// Handles blocker signals from agents during the development loop.
/// Blockers require human intervention and pause execution.
/// </summary>
public class BlockerHandler
{
    private readonly ILogger _logger;

    public BlockerHandler(ILogger? logger = null)
    {
        _logger = logger ?? Log.Logger;
    }

    /// <summary>
    /// Handles a blocker signal from an agent.
    /// Always returns a response indicating execution should pause.
    /// </summary>
    /// <param name="classification">The signal classification containing blocker details.</param>
    /// <param name="currentPhase">The current phase name for logging.</param>
    /// <param name="state">The current loop state to update.</param>
    /// <returns>BlockerResponse with pause indication and blocker details.</returns>
    public BlockerResponse HandleBlocker(
        SignalClassification classification,
        string currentPhase,
        LoopState? state = null)
    {
        if (classification.Type != SignalType.Blocker)
        {
            throw new ArgumentException(
                $"Expected Blocker signal type, got {classification.Type}",
                nameof(classification));
        }

        var blockerText = classification.RawMatch ?? "Unknown blocker";

        _logger.Error(
            "Blocker detected in phase {Phase}: {Blocker}",
            currentPhase,
            blockerText);

        _logger.Warning(
            "Execution paused - human intervention required to resolve blocker");

        // If state is provided, we can inspect it for additional context
        if (state != null)
        {
            _logger.Information(
                "Run {RunId} paused at phase {Phase} (sub-phase: {SubPhase})",
                state.RunId,
                state.CurrentPhase,
                state.CurrentSubPhase?.ToString() ?? "N/A");
        }

        return new BlockerResponse
        {
            ShouldPause = true,
            Description = ExtractBlockerDescription(blockerText),
            RawBlockerText = blockerText,
            DetectedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Checks if a signal classification is a blocker.
    /// </summary>
    public static bool IsBlocker(SignalClassification classification) =>
        classification.Type == SignalType.Blocker;

    /// <summary>
    /// Extracts a human-readable description from the blocker text.
    /// Cleans up markdown formatting for display.
    /// </summary>
    private static string ExtractBlockerDescription(string rawText)
    {
        // Remove common markdown patterns
        var description = rawText
            .Replace("❌", "")
            .Replace("⚠️", "")
            .Replace("**", "")
            .Trim();

        // Capitalize first letter if needed
        if (!string.IsNullOrEmpty(description) && char.IsLower(description[0]))
        {
            description = char.ToUpper(description[0]) + description[1..];
        }

        return description;
    }
}
