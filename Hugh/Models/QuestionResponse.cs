namespace Hugh.Models;

/// <summary>
/// Response from question handling indicating whether to pause or auto-respond.
/// </summary>
public record QuestionResponse
{
    /// <summary>
    /// True if human input is needed (pause execution).
    /// </summary>
    public bool ShouldPause { get; init; }

    /// <summary>
    /// The answer to send to the agent (e.g., "A", "1").
    /// Null if ShouldPause is true.
    /// </summary>
    public string? Response { get; init; }

    /// <summary>
    /// The decision record for audit logging.
    /// Null if no autonomous decision was made.
    /// </summary>
    public Decision? DecisionMade { get; init; }

    /// <summary>
    /// The question text to display to the user when pausing.
    /// Null if no question context is available.
    /// </summary>
    public string? QuestionText { get; init; }

    /// <summary>
    /// Creates a response indicating human input is required.
    /// </summary>
    public static QuestionResponse Pause() => new() { ShouldPause = true };

    /// <summary>
    /// Creates a response indicating human input is required, with question context.
    /// </summary>
    public static QuestionResponse PauseWithQuestion(string questionText) =>
        new() { ShouldPause = true, QuestionText = questionText };

    /// <summary>
    /// Creates an autonomous response with the specified answer.
    /// </summary>
    public static QuestionResponse AutoRespond(string response, Decision decision) =>
        new()
        {
            ShouldPause = false,
            Response = response,
            DecisionMade = decision
        };
}
