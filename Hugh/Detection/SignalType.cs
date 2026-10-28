namespace Hugh.Detection;

/// <summary>
/// Types of signals detected in agent output.
/// Used to determine how the orchestrator should respond.
/// </summary>
public enum SignalType
{
    /// <summary>
    /// No recognizable signal detected in the output.
    /// </summary>
    None,

    /// <summary>
    /// A single phase has completed (e.g., "✅ Phase 3 Complete").
    /// Orchestrator should advance to the next phase.
    /// </summary>
    PhaseComplete,

    /// <summary>
    /// The entire workflow has completed (e.g., "Research Complete", "All Phases Complete").
    /// Orchestrator should transition to the next agent in the chain.
    /// </summary>
    WorkflowComplete,

    /// <summary>
    /// Agent is asking a question and has provided a recommendation.
    /// Orchestrator can auto-select the recommendation in autonomous mode.
    /// </summary>
    QuestionWithRecommendation,

    /// <summary>
    /// Agent is asking an open-ended question without a clear recommendation.
    /// Requires human input; orchestrator should pause.
    /// </summary>
    QuestionOpen,

    /// <summary>
    /// A blocker has been encountered that prevents progress.
    /// Orchestrator should pause and alert the user.
    /// </summary>
    Blocker,

    /// <summary>
    /// Agent is presenting execution mode selection (Manual vs Automatic).
    /// External orchestrator should auto-respond with "1" for Manual mode.
    /// </summary>
    ModeSelection,

    /// <summary>
    /// A subagent requires user input to proceed.
    /// Contains a question that must be displayed to the user.
    /// Orchestrator should pause, display the question, collect the answer,
    /// and re-invoke the subagent with the user's response.
    /// </summary>
    NeedsUserInput
}
