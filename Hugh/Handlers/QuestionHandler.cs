using Hugh.Detection;
using Hugh.Models;
using Serilog;

namespace Hugh.Handlers;

/// <summary>
/// Handles questions from agents during the development loop.
/// Delegates all decision logic to SdkQuestionHandler for unified behavior.
/// </summary>
/// <remarks>
/// <para>
/// This class serves as a thin wrapper that converts SignalClassification from
/// text-based agent output parsing to HughUserInputRequest, then delegates to
/// SdkQuestionHandler for the actual decision logic (auto-accept recommendations,
/// auto-select mode, or pause for open questions).
/// </para>
/// <para>
/// This ensures a single source of truth for question handling behavior whether
/// questions come from SDK callbacks or text-based fallback parsing.
/// </para>
/// </remarks>
public class QuestionHandler
{
    private readonly SdkQuestionHandler _sdkQuestionHandler;
    private readonly ILogger _logger;

    public QuestionHandler(SdkQuestionHandler sdkQuestionHandler, ILogger? logger = null)
    {
        _sdkQuestionHandler = sdkQuestionHandler ?? throw new ArgumentNullException(nameof(sdkQuestionHandler));
        _logger = logger ?? Log.Logger;
    }

    /// <summary>
    /// Handles a question signal from an agent.
    /// </summary>
    /// <param name="classification">The signal classification containing question details.</param>
    /// <param name="currentPhase">The current phase name for decision logging.</param>
    /// <param name="runId">The current run ID for decision logging.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>
    /// QuestionResponse indicating whether to pause or auto-respond.
    /// Returns null if the signal type is not a question.
    /// </returns>
    public async Task<QuestionResponse?> HandleQuestionAsync(
        SignalClassification classification,
        string currentPhase,
        Guid runId,
        CancellationToken ct = default)
    {
        // Only handle question-type signals
        if (classification.Type is not (SignalType.QuestionWithRecommendation or
                                        SignalType.QuestionOpen or
                                        SignalType.ModeSelection or
                                        SignalType.NeedsUserInput))
        {
            return null;  // Not a question signal
        }

        _logger.Debug(
            "Converting SignalClassification {Type} to HughUserInputRequest for delegation",
            classification.Type);

        // Convert text-parsed SignalClassification to HughUserInputRequest
        var request = HughUserInputRequest.FromSignalClassification(classification);

        // Delegate to SdkQuestionHandler for all decision logic
        var response = await _sdkQuestionHandler.HandleAsync(request, currentPhase, runId, ct);

        // Convert UserInputResponse back to QuestionResponse
        return ConvertToQuestionResponse(response, request, currentPhase);
    }

    /// <summary>
    /// Converts UserInputResponse from SdkQuestionHandler to QuestionResponse.
    /// </summary>
    private QuestionResponse ConvertToQuestionResponse(
        UserInputResponse response,
        HughUserInputRequest request,
        string currentPhase)
    {
        if (response.IsAutomatic)
        {
            // Auto-accepted (recommendation or mode selection)
            // Decision was already logged by SdkQuestionHandler
            var decision = new Decision
            {
                Phase = currentPhase,
                Question = request.Prompt,
                Options = request.Choices?.ToList() ?? new List<string>(),
                Selected = response.Answer,
                Rationale = "Auto-accepted via SdkQuestionHandler",
                Autonomous = true,
                Timestamp = DateTime.UtcNow
            };

            return QuestionResponse.AutoRespond(response.Answer, decision);
        }

        // User provided input or open question completed
        if (!string.IsNullOrEmpty(response.Answer))
        {
            // User provided an answer (via console input in SdkQuestionHandler)
            return new QuestionResponse
            {
                ShouldPause = false,
                Response = response.Answer,
                QuestionText = request.Prompt
            };
        }

        // Empty response means user cancelled or needs pause
        return QuestionResponse.PauseWithQuestion(request.Prompt);
    }
}
