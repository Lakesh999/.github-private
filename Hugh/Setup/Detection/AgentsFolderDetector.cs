namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether the agents folder exists and contains the required PCH agent files.
/// </summary>
public class AgentsFolderDetector : IPrerequisiteDetector
{
    private readonly string _workspacePath;

    /// <summary>
    /// The expected PCH agent file names (without extension).
    /// </summary>
    private static readonly string[] ExpectedAgents = new[]
    {
        "pch-coder",
        "pch-helper",
        "pch-plan-reviewer",
        "pch-planner",
        "pch-researcher",
        "pch-visionary"
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentsFolderDetector"/> class.
    /// </summary>
    /// <param name="workspacePath">The root path of the workspace to check.</param>
    public AgentsFolderDetector(string workspacePath)
    {
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "agents-folder";

    /// <inheritdoc/>
    public string DisplayName => "PCH Agents Folder";

    /// <inheritdoc/>
    public int Priority => 10;

    /// <inheritdoc/>
    public bool IsInstallable => true;

    /// <inheritdoc/>
    public Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        var agentsPath = Path.Combine(_workspacePath, "agents");

        // Check if agents directory exists
        if (!Directory.Exists(agentsPath))
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "Agents folder does not exist",
                Hints = new[]
                {
                    $"Expected agents folder at: {agentsPath}",
                    "Clone the .github-private repository containing PCH agents"
                }
            });
        }

        // Check for pch-*.md files
        var existingAgentFiles = Directory.GetFiles(agentsPath, "pch-*.md")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => name != null)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var missingAgents = ExpectedAgents
            .Where(expected => !existingAgentFiles.Contains(expected))
            .ToList();

        if (missingAgents.Count == 0)
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Satisfied,
                Message = $"All {ExpectedAgents.Length} PCH agents found",
                Version = $"{existingAgentFiles.Count} agents"
            });
        }

        if (existingAgentFiles.Count == 0)
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "No PCH agent files found in agents folder",
                Hints = new[]
                {
                    "Agents folder exists but contains no pch-*.md files",
                    $"Expected agents: {string.Join(", ", ExpectedAgents)}"
                }
            });
        }

        // Some agents found, some missing
        var hints = new List<string>
        {
            $"Found {existingAgentFiles.Count} of {ExpectedAgents.Length} expected agents",
            $"Missing: {string.Join(", ", missingAgents)}"
        };

        return Task.FromResult(new PrerequisiteStatus
        {
            PrerequisiteId = PrerequisiteId,
            State = PrerequisiteState.Misconfigured,
            Message = $"Some PCH agents are missing ({missingAgents.Count} of {ExpectedAgents.Length})",
            Version = $"{existingAgentFiles.Count}/{ExpectedAgents.Length} agents",
            Hints = hints.ToArray()
        });
    }
}
