using Hugh.Models;

namespace Hugh.Agents;

/// <summary>
/// Maps loop phases to their corresponding agent definitions.
/// Provides centralized agent configuration lookup.
/// All agents use built-in embedded prompts loaded via <see cref="Hugh.Prompts.BuiltInPromptLoader"/>.
/// </summary>
public class AgentRegistry : IAgentRegistry
{
    /// <summary>
    /// Gets the agent definition for a specific loop phase.
    /// </summary>
    /// <param name="phase">The loop phase to get the agent for.</param>
    /// <returns>The agent definition for the specified phase.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the phase is not recognized.</exception>
    public AgentDefinition GetAgent(LoopPhase phase) => phase switch
    {
        LoopPhase.Vision => new AgentDefinition
        {
            Name = "pch-visionary",
            BuiltInPromptName = "vision",
            Phase = LoopPhase.Vision,
            DefaultTimeout = TimeSpan.FromMinutes(10)
        },
        LoopPhase.Research => new AgentDefinition
        {
            Name = "pch-researcher",
            BuiltInPromptName = "research",
            Phase = LoopPhase.Research,
            DefaultTimeout = TimeSpan.FromMinutes(30)
        },
        LoopPhase.Planning => new AgentDefinition
        {
            Name = "pch-planner",
            BuiltInPromptName = "planning",
            Phase = LoopPhase.Planning,
            DefaultTimeout = TimeSpan.FromMinutes(20)
        },
        LoopPhase.Review => new AgentDefinition
        {
            Name = "pch-plan-reviewer",
            BuiltInPromptName = "review",
            Phase = LoopPhase.Review,
            DefaultTimeout = TimeSpan.FromMinutes(15)
        },
        LoopPhase.Coding => new AgentDefinition
        {
            Name = "pch-coder",
            BuiltInPromptName = "coding",
            Phase = LoopPhase.Coding,
            DefaultTimeout = TimeSpan.FromMinutes(20)
        },
        _ => throw new ArgumentOutOfRangeException(nameof(phase), phase, "Unknown loop phase")
    };

    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    /// <returns>An enumerable of all agent definitions.</returns>
    public IEnumerable<AgentDefinition> GetAllAgents()
    {
        yield return GetAgent(LoopPhase.Vision);
        yield return GetAgent(LoopPhase.Research);
        yield return GetAgent(LoopPhase.Planning);
        yield return GetAgent(LoopPhase.Review);
        yield return GetAgent(LoopPhase.Coding);
    }

    /// <summary>
    /// Tries to get an agent by name.
    /// </summary>
    /// <param name="name">The agent name to look up.</param>
    /// <param name="agent">The found agent definition, or null if not found.</param>
    /// <returns>True if the agent was found, false otherwise.</returns>
    public bool TryGetAgentByName(string name, out AgentDefinition? agent)
    {
        agent = GetAllAgents().FirstOrDefault(a =>
            string.Equals(a.Name, name, StringComparison.OrdinalIgnoreCase));
        return agent != null;
    }
}
