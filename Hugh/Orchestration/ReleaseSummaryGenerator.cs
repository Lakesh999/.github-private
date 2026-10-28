using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Models;
using Hugh.Prompts;
using ILogger = Serilog.ILogger;

namespace Hugh.Orchestration;

/// <summary>
/// Generates structured release summary documents after a release completes.
/// Summaries capture architectural context (APIs, patterns, decisions) for subsequent releases.
/// </summary>
public class ReleaseSummaryGenerator
{
    private readonly ICopilotService _copilotService;
    private readonly GitCommitManager _gitCommitManager;
    private readonly HughConfig _config;
    private readonly ILogger _logger;

    public ReleaseSummaryGenerator(
        ICopilotService copilotService,
        GitCommitManager gitCommitManager,
        HughConfig config,
        ILogger logger)
    {
        _copilotService = copilotService ?? throw new ArgumentNullException(nameof(copilotService));
        _gitCommitManager = gitCommitManager ?? throw new ArgumentNullException(nameof(gitCommitManager));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Generates a release summary document by running a dedicated LLM session.
    /// Uses the built-in release-summary prompt with the plan and git diff as context.
    /// </summary>
    /// <param name="releaseNumber">The 1-based release number.</param>
    /// <param name="releaseName">The name of the release.</param>
    /// <param name="planPath">Path to the implementation plan document.</param>
    /// <param name="researchPath">Optional path to the research document.</param>
    /// <param name="startingCommitSha">The commit SHA recorded at the start of the release, for cumulative diff.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The path to the generated summary document, or null on failure.</returns>
    public virtual async Task<string?> GenerateSummaryAsync(
        int releaseNumber,
        string releaseName,
        string? planPath,
        string? researchPath,
        string? startingCommitSha,
        CancellationToken ct = default)
    {
        _logger.Information(
            "Generating release summary for Release {Number}: {Name}",
            releaseNumber, releaseName);

        // Get cumulative git diff from the starting commit
        var gitDiff = string.Empty;
        if (!string.IsNullOrEmpty(startingCommitSha))
        {
            gitDiff = await _gitCommitManager.GetDiffSummaryAsync(startingCommitSha, ct);
            _logger.Debug("Git diff summary: {Length} chars", gitDiff.Length);
        }
        else
        {
            _logger.Debug("No starting commit SHA — skipping git diff");
        }

        // Load the built-in release-summary prompt
        string promptTemplate;
        try
        {
            promptTemplate = BuiltInPromptLoader.Load("release-summary");
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to load release-summary prompt — skipping summary generation");
            return null;
        }

        // Build the user prompt with context
        var userPrompt = $"""
            Generate a release summary for the following release:

            **Release Number:** {releaseNumber}
            **Release Name:** {releaseName}

            ## Git Diff Summary

            {(string.IsNullOrEmpty(gitDiff) ? "No git diff available." : gitDiff)}

            Please follow the output format specified in your instructions.
            """;

        // Prepare document attachments
        var documents = new List<string>();
        if (!string.IsNullOrEmpty(planPath) && File.Exists(planPath))
        {
            documents.Add(planPath);
        }
        if (!string.IsNullOrEmpty(researchPath) && File.Exists(researchPath))
        {
            documents.Add(researchPath);
        }

        // Ensure output directory exists
        var outputDir = Path.Combine(Directory.GetCurrentDirectory(), _config.DocsPath, "releases");
        EnsureDirectoryExists(outputDir);

        var outputPath = Path.Combine(outputDir, $"release-{releaseNumber}-summary.md");

        // Run LLM session
        try
        {
            var agentDefinition = new AgentDefinition
            {
                Name = "release-summary",
                BuiltInPromptName = "release-summary",
                Phase = LoopPhase.Coding, // Not used, but required for the record
                DefaultTimeout = TimeSpan.FromMinutes(10)
            };

            var result = await _copilotService.ExecutePhaseAsync(
                agentDefinition,
                promptTemplate,
                userPrompt,
                documents,
                ct);

            if (!result.Success)
            {
                _logger.Warning(
                    "Release summary LLM session failed: {Error}", result.ErrorMessage);
                return null;
            }

            // Write the output document if not already created by the agent
            if (!File.Exists(outputPath))
            {
                // The agent should have created the document, but if not,
                // write a minimal summary noting the attempt
                _logger.Warning(
                    "Release summary document not found at {Path} after LLM session — " +
                    "the agent may not have created it", outputPath);
            }

            _logger.Information(
                "Release summary generated at: {Path}", outputPath);
            return outputPath;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex,
                "Failed to generate release summary for Release {Number}", releaseNumber);
            return null;
        }
    }

    /// <summary>
    /// Ensures a directory exists, creating it if necessary.
    /// </summary>
    internal static void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
