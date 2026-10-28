using Hugh.Models;

namespace Hugh.Agents;

/// <summary>
/// Interface for looking up agent definitions by phase or name.
/// </summary>
public interface IAgentRegistry
{
    /// <summary>
    /// Gets the agent definition for a specific loop phase.
    /// </summary>
    /// <param name="phase">The loop phase to get the agent for.</param>
    /// <returns>The agent definition for the specified phase.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the phase is not recognized.</exception>
    AgentDefinition GetAgent(LoopPhase phase);

    /// <summary>
    /// Gets all registered agents.
    /// </summary>
    /// <returns>An enumerable of all agent definitions.</returns>
    IEnumerable<AgentDefinition> GetAllAgents();

    /// <summary>
    /// Tries to get an agent by name.
    /// </summary>
    /// <param name="name">The agent name to look up.</param>
    /// <param name="agent">The found agent definition, or null if not found.</param>
    /// <returns>True if the agent was found, false otherwise.</returns>
    bool TryGetAgentByName(string name, out AgentDefinition? agent);
}
