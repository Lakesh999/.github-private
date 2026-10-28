using Hugh.Models;

namespace Hugh.Agents;

/// <summary>
/// Defines an agent configuration for a specific phase of the development loop.
/// </summary>
public record AgentDefinition
{
    /// <summary>
    /// The name of the agent (e.g., "pch-visionary", "pch-planner").
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The name of the built-in embedded prompt resource for this agent
    /// (e.g., "vision", "planning"). Loaded via <see cref="Hugh.Prompts.BuiltInPromptLoader"/>.
    /// </summary>
    public required string BuiltInPromptName { get; init; }

    /// <summary>
    /// The loop phase this agent handles.
    /// </summary>
    public LoopPhase Phase { get; init; }

    /// <summary>
    /// Default timeout for agent execution.
    /// </summary>
    public TimeSpan DefaultTimeout { get; init; }
}
