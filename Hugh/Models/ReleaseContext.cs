namespace Hugh.Models;

/// <summary>
/// Context passed to the LoopOrchestrator when executing a release within a multi-release solution.
/// Provides the release-specific scope and links to solution-level documents.
/// </summary>
public record ReleaseContext
{
    /// <summary>
    /// The 1-based release number.
    /// </summary>
    public int ReleaseNumber { get; init; }

    /// <summary>
    /// Short name for the release (e.g., "MVP", "Collaboration Features").
    /// </summary>
    public string ReleaseName { get; init; } = "";

    /// <summary>
    /// Detailed scope of what this release covers.
    /// </summary>
    public string ReleaseScope { get; init; } = "";

    /// <summary>
    /// Path to the clarified solution vision document.
    /// </summary>
    public string SolutionVisionPath { get; init; } = "";

    /// <summary>
    /// Path to the release roadmap document.
    /// </summary>
    public string RoadmapPath { get; init; } = "";

    /// <summary>
    /// Names of previously completed releases, for context.
    /// </summary>
    public List<string> CompletedReleases { get; init; } = new();

    /// <summary>
    /// Paths to LLM-generated release summary documents for completed releases.
    /// Used to propagate inter-release context to subsequent Research and Planning phases.
    /// </summary>
    public List<string> CompletedReleaseSummaries { get; init; } = new();
}
