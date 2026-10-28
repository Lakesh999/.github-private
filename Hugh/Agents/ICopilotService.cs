using Hugh.Models;

namespace Hugh.Agents;

/// <summary>
/// Interface for interacting with GitHub Copilot agents.
/// Provides abstraction over the Copilot SDK for executing agent phases.
/// </summary>
public interface ICopilotService : IAsyncDisposable
{
    /// <summary>
    /// Executes an agent phase with the specified prompt and document context.
    /// </summary>
    /// <param name="agentDefinition">The agent definition containing metadata.</param>
    /// <param name="agentPrompt">The system message loaded by PhaseExecutor.</param>
    /// <param name="prompt">The user prompt to send to the agent.</param>
    /// <param name="documentPaths">Paths to documents to attach as context.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <param name="lastSessionId">Optional session ID to attempt resuming (used by --resume path).</param>
    /// <returns>The result of the phase execution.</returns>
    Task<PhaseResult> ExecutePhaseAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        string prompt,
        IEnumerable<string> documentPaths,
        CancellationToken ct,
        string? lastSessionId = null);

    /// <summary>
    /// Executes an interactive multi-turn Q&amp;A phase where the agent asks questions
    /// and the user provides answers via console input.
    /// </summary>
    /// <param name="agentDefinition">The agent definition containing metadata.</param>
    /// <param name="agentPrompt">The system message loaded by PhaseExecutor.</param>
    /// <param name="prompt">The initial prompt to send to the agent.</param>
    /// <param name="documentPaths">Paths to documents to attach as context.</param>
    /// <param name="expectedOutputPath">The expected path where the agent will create its output document.</param>
    /// <param name="maxTurns">Maximum number of Q&amp;A turns before forcing completion.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the phase execution.</returns>
    Task<PhaseResult> ExecuteInteractivePhaseAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        string prompt,
        IEnumerable<string> documentPaths,
        string expectedOutputPath,
        int maxTurns,
        CancellationToken ct);
}
