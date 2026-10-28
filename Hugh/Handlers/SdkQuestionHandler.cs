using System.Text.RegularExpressions;
using GitHub.Copilot.SDK;
using Hugh.Configuration;
using Hugh.Logging;
using Hugh.Models;
using Serilog;
using ILogger = Serilog.ILogger;
using SdkUserInputRequest = GitHub.Copilot.SDK.UserInputRequest;

namespace Hugh.Handlers;

/// <summary>
/// Represents a Hugh-internal user input request for abstraction from SDK types.
/// </summary>
/// <remarks>
/// <para>
/// This class serves as a bridge between SDK-native UserInputRequest and Hugh's
/// question handling system. It provides a stable internal API while the SDK evolves.
/// </para>
/// </remarks>
public record HughUserInputRequest
{
    /// <summary>
    /// The prompt text displayed to the user.
    /// </summary>
    public string Prompt { get; init; } = string.Empty;

    /// <summary>
    /// Optional list of choices for the user to select from.
    /// </summary>
    public IReadOnlyList<string>? Choices { get; init; }

    /// <summary>
    /// Whether freeform text input is allowed.
    /// </summary>
    public bool AllowFreeform { get; init; } = true;

    /// <summary>
    /// Optional metadata about the question type.
    /// </summary>
    public string? QuestionType { get; init; }

    /// <summary>
    /// Creates a HughUserInputRequest from a SignalClassification.
    /// </summary>
    public static HughUserInputRequest FromSignalClassification(Detection.SignalClassification classification)
    {
        var choices = ParseChoices(classification.QuestionOptions);
        
        return new HughUserInputRequest
        {
            Prompt = classification.QuestionText ?? classification.RawMatch ?? "Question from agent",
            Choices = choices,
            AllowFreeform = choices == null || choices.Count == 0,
            QuestionType = classification.Type.ToString()
        };
    }
    
    /// <summary>
    /// Creates a HughUserInputRequest from an SDK UserInputRequest.
    /// </summary>
    /// <remarks>
    /// Uses the SDK 0.3.0 typed properties directly (Question, Choices, AllowFreeform).
    /// </remarks>
    public static HughUserInputRequest FromSdkRequest(SdkUserInputRequest sdkRequest)
    {
        return new HughUserInputRequest
        {
            Prompt = sdkRequest.Question ?? "SDK user input request",
            Choices = sdkRequest.Choices?.AsReadOnly(),
            AllowFreeform = sdkRequest.AllowFreeform ?? true,
            QuestionType = "SDK"
        };
    }

    private static IReadOnlyList<string>? ParseChoices(string? options)
    {
        if (string.IsNullOrWhiteSpace(options) || 
            options.Equals("open-ended", StringComparison.OrdinalIgnoreCase) ||
            options.Equals("none", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        // Parse format like "A — Option1, B — Option2"
        var parts = options.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length > 0 ? parts.ToList() : null;
    }
}

/// <summary>
/// Represents the response to a user input request.
/// </summary>
public record UserInputResponse
{
    /// <summary>
    /// The user's answer.
    /// </summary>
    public string Answer { get; init; } = string.Empty;

    /// <summary>
    /// Whether this response was auto-generated (not from user input).
    /// </summary>
    public bool IsAutomatic { get; init; }
}

/// <summary>
/// Handles user input requests with SDK-native semantics.
/// Auto-accepts recommendations when configured; pauses for open questions.
/// </summary>
/// <remarks>
/// <para>
/// This handler is designed to work directly with SDK user input mechanisms.
/// It implements the acceptance logic specified in the autonomous configuration:
/// </para>
/// <list type="bullet">
/// <item><description>Auto-accepts questions with recommendations when AcceptRecommendationsOnly is true</description></item>
/// <item><description>Auto-selects Manual mode for mode selection prompts</description></item>
/// <item><description>Pauses execution for open questions requiring human input</description></item>
/// </list>
/// </remarks>
public class SdkQuestionHandler
{
    private readonly DecisionLogger _decisionLogger;
    private readonly AutonomousConfig _autonomousConfig;
    private readonly ILogger _logger;

    /// <summary>
    /// Regex pattern for extracting recommendations from prompt text.
    /// Matches: **Recommendation:** A — Some option
    /// </summary>
    private static readonly Regex RecommendationRegex = new(
        @"\*\*Recommendation:\*\*\s+([A-E1-5])\s*[—–\-]\s*(.+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Regex pattern for detecting mode selection prompts.
    /// </summary>
    private static readonly Regex ModeSelectionRegex = new(
        @"Execution Mode Selection|🔄\s*\*\*Execution Mode Selection\*\*",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Creates a new SdkQuestionHandler.
    /// </summary>
    /// <param name="decisionLogger">Logger for recording decisions.</param>
    /// <param name="autonomousConfig">Autonomous mode configuration.</param>
    /// <param name="logger">Optional Serilog logger.</param>
    public SdkQuestionHandler(
        DecisionLogger decisionLogger,
        AutonomousConfig autonomousConfig,
        ILogger? logger = null)
    {
        _decisionLogger = decisionLogger ?? throw new ArgumentNullException(nameof(decisionLogger));
        _autonomousConfig = autonomousConfig ?? throw new ArgumentNullException(nameof(autonomousConfig));
        _logger = logger ?? Log.Logger;
    }

    /// <summary>
    /// Handles a user input request, potentially auto-accepting or pausing for user input.
    /// </summary>
    /// <param name="request">The user input request.</param>
    /// <param name="currentPhase">The current phase name for logging.</param>
    /// <param name="runId">The current run ID for decision logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user input response.</returns>
    public virtual async Task<UserInputResponse> HandleAsync(
        HughUserInputRequest request,
        string currentPhase,
        Guid runId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // 1. Check for mode selection pattern first
        if (IsModeSelection(request.Prompt))
        {
            return await HandleModeSelectionAsync(request, currentPhase, runId, ct);
        }

        // 2. Check for recommendation in prompt text
        var (recLetter, recOption) = DetectRecommendation(request.Prompt);
        if (recLetter != null && _autonomousConfig.AcceptRecommendationsOnly)
        {
            return await AutoAcceptRecommendationAsync(
                request, recLetter, recOption!, currentPhase, runId, ct);
        }

        // 3. Check if choices have a clear recommended option
        var recommendedChoice = GetRecommendedChoice(request.Choices);
        if (recommendedChoice != null && _autonomousConfig.AcceptRecommendationsOnly)
        {
            return await AutoAcceptChoiceAsync(
                request, recommendedChoice, currentPhase, runId, ct);
        }

        // 4. Open question - pause for human input
        return await HandleOpenQuestionAsync(request, ct);
    }
    
    /// <summary>
    /// Handles an SDK UserInputRequest by converting to HughUserInputRequest and delegating.
    /// </summary>
    /// <param name="sdkRequest">The SDK user input request.</param>
    /// <param name="currentPhase">The current phase name for logging.</param>
    /// <param name="runId">The current run ID for decision logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The user input response.</returns>
    public Task<UserInputResponse> HandleAsync(
        SdkUserInputRequest sdkRequest,
        string currentPhase,
        Guid runId,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sdkRequest);
        var hughRequest = HughUserInputRequest.FromSdkRequest(sdkRequest);
        return HandleAsync(hughRequest, currentPhase, runId, ct);
    }

    /// <summary>
    /// Detects a recommendation pattern in the prompt text.
    /// </summary>
    /// <param name="prompt">The prompt text to analyze.</param>
    /// <returns>Tuple of (letter, option) if found, (null, null) otherwise.</returns>
    public (string? Letter, string? Option) DetectRecommendation(string? prompt)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            return (null, null);
        }

        var match = RecommendationRegex.Match(prompt);
        if (match.Success)
        {
            return (match.Groups[1].Value, match.Groups[2].Value.Trim());
        }

        return (null, null);
    }

    /// <summary>
    /// Determines if auto-accept should be applied for this request.
    /// </summary>
    /// <param name="request">The user input request.</param>
    /// <returns>True if auto-accept should apply.</returns>
    public bool ShouldAutoAccept(HughUserInputRequest request)
    {
        if (!_autonomousConfig.AcceptRecommendationsOnly)
        {
            return false;
        }

        // Check for recommendation in prompt
        var (letter, _) = DetectRecommendation(request.Prompt);
        if (letter != null)
        {
            return true;
        }

        // Check for recommended choice
        var recommendedChoice = GetRecommendedChoice(request.Choices);
        return recommendedChoice != null;
    }
    
    /// <summary>
    /// Determines if auto-accept should be applied for an SDK request.
    /// </summary>
    /// <param name="sdkRequest">The SDK user input request.</param>
    /// <returns>True if auto-accept should apply.</returns>
    public bool ShouldAutoAccept(SdkUserInputRequest sdkRequest)
    {
        return ShouldAutoAccept(HughUserInputRequest.FromSdkRequest(sdkRequest));
    }

    /// <summary>
    /// Checks if the prompt represents a mode selection question.
    /// </summary>
    private static bool IsModeSelection(string? prompt)
    {
        return !string.IsNullOrWhiteSpace(prompt) && ModeSelectionRegex.IsMatch(prompt);
    }

    /// <summary>
    /// Gets the recommended choice from a list of choices, if one is marked.
    /// </summary>
    /// <remarks>
    /// Looks for patterns like "(recommended)" or "✓" in choice text.
    /// </remarks>
    private static string? GetRecommendedChoice(IReadOnlyList<string>? choices)
    {
        if (choices == null || choices.Count == 0)
        {
            return null;
        }

        // Look for explicitly marked recommended choices
        foreach (var choice in choices)
        {
            if (choice.Contains("(recommended)", StringComparison.OrdinalIgnoreCase) ||
                choice.Contains("✓", StringComparison.Ordinal) ||
                choice.Contains("[recommended]", StringComparison.OrdinalIgnoreCase))
            {
                // Extract the letter/number at the start
                var letterMatch = Regex.Match(choice, @"^([A-E1-5])\s*[—–\-:]");
                return letterMatch.Success ? letterMatch.Groups[1].Value : choice;
            }
        }

        return null;
    }

    /// <summary>
    /// Handles mode selection by auto-selecting Manual mode (option 1).
    /// </summary>
    private async Task<UserInputResponse> HandleModeSelectionAsync(
        HughUserInputRequest request,
        string currentPhase,
        Guid runId,
        CancellationToken ct)
    {
        _logger.Information("Mode selection detected - auto-selecting Manual mode (1)");

        var decision = new Decision
        {
            Phase = currentPhase,
            Question = "Execution Mode Selection",
            Options = new List<string> { "1 — Manual Mode", "2 — Automatic Mode" },
            Selected = "1",
            Rationale = "External orchestrator uses Manual mode to maintain phase-by-phase control",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        };

        await _decisionLogger.LogDecisionAsync(decision, runId, ct);

        return new UserInputResponse
        {
            Answer = "1",
            IsAutomatic = true
        };
    }

    /// <summary>
    /// Auto-accepts a recommendation detected in the prompt.
    /// </summary>
    private async Task<UserInputResponse> AutoAcceptRecommendationAsync(
        HughUserInputRequest request,
        string letter,
        string option,
        string currentPhase,
        Guid runId,
        CancellationToken ct)
    {
        _logger.Information(
            "Auto-accepting recommendation: {Letter} — {Option}",
            letter,
            option);

        var decision = new Decision
        {
            Phase = currentPhase,
            Question = request.Prompt,
            Options = new List<string> { $"{letter} — {option}" },
            Selected = letter,
            Rationale = $"Agent recommended option {letter}: {option}",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        };

        await _decisionLogger.LogDecisionAsync(decision, runId, ct);

        return new UserInputResponse
        {
            Answer = letter,
            IsAutomatic = true
        };
    }

    /// <summary>
    /// Auto-accepts a recommended choice from the choices list.
    /// </summary>
    private async Task<UserInputResponse> AutoAcceptChoiceAsync(
        HughUserInputRequest request,
        string recommendedChoice,
        string currentPhase,
        Guid runId,
        CancellationToken ct)
    {
        _logger.Information(
            "Auto-accepting recommended choice: {Choice}",
            recommendedChoice);

        var decision = new Decision
        {
            Phase = currentPhase,
            Question = request.Prompt,
            Options = request.Choices?.ToList() ?? new List<string>(),
            Selected = recommendedChoice,
            Rationale = $"Choice marked as recommended: {recommendedChoice}",
            Autonomous = true,
            Timestamp = DateTime.UtcNow
        };

        await _decisionLogger.LogDecisionAsync(decision, runId, ct);

        return new UserInputResponse
        {
            Answer = recommendedChoice,
            IsAutomatic = true
        };
    }

    /// <summary>
    /// Handles an open question by displaying it and collecting user input.
    /// </summary>
    private Task<UserInputResponse> HandleOpenQuestionAsync(
        HughUserInputRequest request,
        CancellationToken ct)
    {
        _logger.Warning(
            "Open question detected - pausing for human input. Question: {Question}",
            request.Prompt);

        ConsoleOutput.WriteSection("\u2753 Agent Question — User Input Required");

        if (!string.IsNullOrWhiteSpace(request.Prompt))
        {
            ConsoleOutput.WriteInfo(request.Prompt);
        }

        if (request.Choices != null && request.Choices.Count > 0)
        {
            Console.WriteLine();
            ConsoleOutput.WriteInfo("Options:");
            foreach (var choice in request.Choices)
            {
                ConsoleOutput.WriteInfo($"  {choice}");
            }
        }

        Console.WriteLine();
        ConsoleOutput.WriteWarning("Please provide your answer:");

        // Read from console - this blocks execution
        var userInput = Console.ReadLine()?.Trim() ?? string.Empty;

        _logger.Information("User provided answer: {Answer}", userInput);

        return Task.FromResult(new UserInputResponse
        {
            Answer = userInput,
            IsAutomatic = false
        });
    }
}
