using System.Text.RegularExpressions;

namespace Hugh.Detection;

/// <summary>
/// Result of classifying agent output for signals.
/// </summary>
public record SignalClassification
{
    /// <summary>
    /// The type of signal detected.
    /// </summary>
    public SignalType Type { get; init; }

    /// <summary>
    /// The source of this signal — whether it came from a tool call or text regex parsing.
    /// </summary>
    public SignalSource Source { get; init; } = SignalSource.TextRegex;

    /// <summary>
    /// For QuestionWithRecommendation: the letter (A-E) of the recommendation.
    /// </summary>
    public string? RecommendationLetter { get; init; }

    /// <summary>
    /// For QuestionWithRecommendation: the option name/description.
    /// </summary>
    public string? RecommendationOption { get; init; }

    /// <summary>
    /// For PhaseComplete: the phase number that completed.
    /// </summary>
    public int? PhaseNumber { get; init; }

    /// <summary>
    /// The raw text that matched the signal pattern.
    /// </summary>
    public string? RawMatch { get; init; }

    /// <summary>
    /// For NeedsUserInput: the full question text to display to the user.
    /// </summary>
    public string? QuestionText { get; init; }

    /// <summary>
    /// For NeedsUserInput: the question options (e.g., "A — Option1, B — Option2") or "open-ended".
    /// </summary>
    public string? QuestionOptions { get; init; }

    /// <summary>
    /// For Blocker signals from tool calls: whether the blocker can be auto-skipped.
    /// Always false for text-based blocker signals (which lack this metadata).
    /// </summary>
    public bool IsSkippable { get; init; }
}

/// <summary>
/// Indicates the source of a detected signal.
/// </summary>
public enum SignalSource
{
    /// <summary>
    /// Signal detected via text regex parsing of agent output (fallback).
    /// </summary>
    TextRegex,

    /// <summary>
    /// Signal detected via hugh_* tool call (primary, unambiguous).
    /// </summary>
    ToolCall
}

/// <summary>
/// Classifies agent output to detect completion signals, questions, and blockers.
/// Uses compiled regex patterns for performance.
/// </summary>
public partial class SignalClassifier
{
    // Phase completion: ✅ **Phase [N] Complete** or ✅ **Phase N Completed**
    [GeneratedRegex(@"✅\s*\*\*Phase\s+\[?(\d+)\]?\s+(Complete|Completed)", RegexOptions.Compiled)]
    private static partial Regex PhaseCompleteRegex();

    // Workflow completion signals
    [GeneratedRegex(@"(Research Complete|All Phases Complete|Plan Complete|Implementation Finished)", RegexOptions.Compiled)]
    private static partial Regex WorkflowCompleteRegex();

    // Recommendation extraction: **Recommendation:** A — Option name
    // Supports em-dash (—), en-dash (–), and hyphen (-)
    [GeneratedRegex(@"\*\*Recommendation:\*\*\s+([A-E1-5])\s*[—–\-]\s*(.+)", RegexOptions.Compiled)]
    private static partial Regex RecommendationRegex();

    // Open question detection (question mark after agent output patterns)
    [GeneratedRegex(@"\*\*(?:How would you like to proceed|What would you like|Please (?:choose|select|specify))\?\*\*", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex OpenQuestionRegex();

    // Blocker detection
    [GeneratedRegex(@"(?:❌|⚠️)\s*\*\*(?:Blocked|Blocker|Implementation Paused|Execution Stopped)", RegexOptions.Compiled)]
    private static partial Regex BlockerRegex();

    // Mode selection prompt detection
    [GeneratedRegex(@"🔄\s*\*\*Execution Mode Selection\*\*", RegexOptions.Compiled)]
    private static partial Regex ModeSelectionRegex();

    // Needs user input: STATUS: needs_user_input
    [GeneratedRegex(@"STATUS:\s*needs_user_input", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex NeedsUserInputRegex();

    // Question for user extraction: QUESTION_FOR_USER: [text]
    [GeneratedRegex(@"QUESTION_FOR_USER:\s*(.+?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex QuestionForUserRegex();

    // Question options extraction: QUESTION_OPTIONS: [options]
    [GeneratedRegex(@"QUESTION_OPTIONS:\s*(.+?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline)]
    private static partial Regex QuestionOptionsRegex();

    // Review approved: "Ready for Implementation" / "Plan Approved"
    [GeneratedRegex(@"(Ready for Implementation|Plan Approved)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ReviewApprovedRegex();

    // Review needs revision: "Issues Found" / "Requires Revision" / "Plan Rejected"
    [GeneratedRegex(@"(Issues Found|Requires Revision|Plan Rejected)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
    private static partial Regex ReviewNeedsRevisionRegex();

    /// <summary>
    /// Classifies agent output to detect the primary signal type.
    /// Returns the most significant signal found (priority order: Blocker > WorkflowComplete > PhaseComplete > etc.)
    /// </summary>
    /// <param name="agentOutput">The raw output from the agent.</param>
    /// <returns>A SignalClassification with the detected signal type and extracted data.</returns>
    public SignalClassification Classify(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
        {
            return new SignalClassification { Type = SignalType.None };
        }

        // Check for blockers first (highest priority)
        var blockerMatch = BlockerRegex().Match(agentOutput);
        if (blockerMatch.Success)
        {
            return new SignalClassification
            {
                Type = SignalType.Blocker,
                RawMatch = blockerMatch.Value
            };
        }

        // Check for needs_user_input (requires user action)
        var needsInputMatch = NeedsUserInputRegex().Match(agentOutput);
        if (needsInputMatch.Success)
        {
            // Extract the question text if available
            var questionMatch = QuestionForUserRegex().Match(agentOutput);

            // Only classify as NeedsUserInput if QUESTION_FOR_USER is also present.
            // Prevents false positives from plan document text embedded in subagent output.
            if (questionMatch.Success)
            {
                var questionText = questionMatch.Groups[1].Value.Trim();

                // Also try to extract recommendation
                var recMatch = RecommendationRegex().Match(agentOutput);

                // Also try to extract question options
                var optionsMatch = QuestionOptionsRegex().Match(agentOutput);
                var optionsText = optionsMatch.Success ? optionsMatch.Groups[1].Value.Trim() : null;
                // Treat "open-ended" and "none" as no options
                if (optionsText != null && (optionsText.Equals("open-ended", StringComparison.OrdinalIgnoreCase) 
                    || optionsText.Equals("none", StringComparison.OrdinalIgnoreCase)))
                {
                    optionsText = null;
                }

                return new SignalClassification
                {
                    Type = SignalType.NeedsUserInput,
                    RawMatch = needsInputMatch.Value,
                    QuestionText = questionText,
                    QuestionOptions = optionsText,
                    RecommendationLetter = recMatch.Success ? recMatch.Groups[1].Value : null,
                    RecommendationOption = recMatch.Success ? recMatch.Groups[2].Value.Trim() : null
                };
            }
            // If STATUS: needs_user_input found without QUESTION_FOR_USER,
            // fall through to lower-priority checks (likely plan document text)
        }

        // Check for mode selection prompt (should auto-respond with "1")
        var modeMatch = ModeSelectionRegex().Match(agentOutput);
        if (modeMatch.Success)
        {
            return new SignalClassification
            {
                Type = SignalType.ModeSelection,
                RawMatch = modeMatch.Value
            };
        }

        // Check for workflow completion
        var workflowMatch = WorkflowCompleteRegex().Match(agentOutput);
        if (workflowMatch.Success)
        {
            return new SignalClassification
            {
                Type = SignalType.WorkflowComplete,
                RawMatch = workflowMatch.Value
            };
        }

        // Check for phase completion
        var phaseMatch = PhaseCompleteRegex().Match(agentOutput);
        if (phaseMatch.Success)
        {
            int? phaseNumber = null;
            if (phaseMatch.Groups.Count > 1 && int.TryParse(phaseMatch.Groups[1].Value, out var parsed))
            {
                phaseNumber = parsed;
            }

            return new SignalClassification
            {
                Type = SignalType.PhaseComplete,
                PhaseNumber = phaseNumber,
                RawMatch = phaseMatch.Value
            };
        }

        // Check for questions with recommendations
        var recommendationMatch = RecommendationRegex().Match(agentOutput);
        if (recommendationMatch.Success)
        {
            return new SignalClassification
            {
                Type = SignalType.QuestionWithRecommendation,
                RecommendationLetter = recommendationMatch.Groups[1].Value,
                RecommendationOption = recommendationMatch.Groups[2].Value.Trim(),
                RawMatch = recommendationMatch.Value
            };
        }

        // Check for open questions (no recommendation)
        var openQuestionMatch = OpenQuestionRegex().Match(agentOutput);
        if (openQuestionMatch.Success)
        {
            return new SignalClassification
            {
                Type = SignalType.QuestionOpen,
                RawMatch = openQuestionMatch.Value
            };
        }

        // No signal detected
        return new SignalClassification { Type = SignalType.None };
    }

    /// <summary>
    /// Finds all phase completion signals in the output.
    /// Useful when agent output may contain multiple phase completions.
    /// </summary>
    /// <param name="agentOutput">The raw output from the agent.</param>
    /// <returns>List of all phase completion matches with their phase numbers.</returns>
    public IReadOnlyList<SignalClassification> FindAllPhaseCompletions(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
        {
            return Array.Empty<SignalClassification>();
        }

        var matches = PhaseCompleteRegex().Matches(agentOutput);
        var results = new List<SignalClassification>(matches.Count);

        foreach (Match match in matches)
        {
            int? phaseNumber = null;
            if (match.Groups.Count > 1 && int.TryParse(match.Groups[1].Value, out var parsed))
            {
                phaseNumber = parsed;
            }

            results.Add(new SignalClassification
            {
                Type = SignalType.PhaseComplete,
                PhaseNumber = phaseNumber,
                RawMatch = match.Value
            });
        }

        return results;
    }

    /// <summary>
    /// Extracts all recommendations from the output.
    /// Some agents may present multiple options with recommendations.
    /// </summary>
    /// <param name="agentOutput">The raw output from the agent.</param>
    /// <returns>List of all recommendation matches.</returns>
    public IReadOnlyList<SignalClassification> FindAllRecommendations(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput))
        {
            return Array.Empty<SignalClassification>();
        }

        var matches = RecommendationRegex().Matches(agentOutput);
        var results = new List<SignalClassification>(matches.Count);

        foreach (Match match in matches)
        {
            results.Add(new SignalClassification
            {
                Type = SignalType.QuestionWithRecommendation,
                RecommendationLetter = match.Groups[1].Value,
                RecommendationOption = match.Groups[2].Value.Trim(),
                RawMatch = match.Value
            });
        }

        return results;
    }

    /// <summary>
    /// Checks whether the agent output contains a review-approved signal.
    /// Looks for patterns like "Ready for Implementation" or "Plan Approved".
    /// </summary>
    /// <param name="agentOutput">The raw output from the agent.</param>
    /// <returns>True if approval signal detected, false otherwise.</returns>
    public bool IsReviewApproved(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput)) return false;
        return ReviewApprovedRegex().IsMatch(agentOutput);
    }

    /// <summary>
    /// Checks whether the agent output contains a review-needs-revision signal.
    /// Looks for patterns like "Issues Found", "Requires Revision", or "Plan Rejected".
    /// </summary>
    /// <param name="agentOutput">The raw output from the agent.</param>
    /// <returns>True if revision signal detected, false otherwise.</returns>
    public bool IsReviewNeedsRevision(string agentOutput)
    {
        if (string.IsNullOrWhiteSpace(agentOutput)) return false;
        return ReviewNeedsRevisionRegex().IsMatch(agentOutput);
    }
}
