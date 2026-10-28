using System.Text.RegularExpressions;

namespace Hugh.Detection;

/// <summary>
/// Represents a phase parsed from a research document's Research Phases table.
/// </summary>
public record ResearchPhase
{
    /// <summary>
    /// The phase number (1, 2, 3, etc.)
    /// </summary>
    public int Number { get; init; }

    /// <summary>
    /// The phase name/description.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The status emoji (✅, 🔄, ⏳, ❌).
    /// </summary>
    public string StatusEmoji { get; init; } = "";

    /// <summary>
    /// Parsed status from the emoji.
    /// </summary>
    public PhaseProgressStatus Status { get; init; }

    /// <summary>
    /// The scope text for this research phase.
    /// </summary>
    public string Scope { get; init; } = "";
}

/// <summary>
/// Parses Research Phases tables from research documents to detect phase status.
/// Used to determine which research sub-phases still need execution.
/// </summary>
public partial class ResearchPhaseParser
{
    // Matches a phase row in the Research Phases table
    // | 1 | Phase Name | ⏳ Not Started | Scope text | — |
    // | 2 | Phase Name | ✅ Complete | Scope text | 2026-02-23 |
    [GeneratedRegex(@"\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅|🔄|⏳|❌)\s*([^|]*)\|\s*([^|]*)\|\s*([^|]*)\|", RegexOptions.Compiled)]
    private static partial Regex ResearchPhaseRowRegex();

    // Matches incomplete phase status (⏳ or 🔄)
    [GeneratedRegex(@"\|\s*(\d+)\s*\|[^|]+\|\s*(⏳|🔄)\s*[^|]*\|", RegexOptions.Compiled)]
    private static partial Regex IncompleteResearchPhaseRegex();

    /// <summary>
    /// Parses all phases from a research document's Research Phases table.
    /// </summary>
    /// <param name="researchContent">The full content of the research document.</param>
    /// <returns>List of parsed research phases in order.</returns>
    public IReadOnlyList<ResearchPhase> ParsePhases(string researchContent)
    {
        if (string.IsNullOrWhiteSpace(researchContent))
        {
            return Array.Empty<ResearchPhase>();
        }

        var matches = ResearchPhaseRowRegex().Matches(researchContent);
        var phases = new List<ResearchPhase>(matches.Count);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var phaseNumber))
            {
                var statusEmoji = match.Groups[3].Value.Trim();
                phases.Add(new ResearchPhase
                {
                    Number = phaseNumber,
                    Name = match.Groups[2].Value.Trim(),
                    StatusEmoji = statusEmoji,
                    Status = ParseStatusEmoji(statusEmoji),
                    Scope = match.Groups[5].Value.Trim()
                });
            }
        }

        return phases.OrderBy(p => p.Number).ToList();
    }

    /// <summary>
    /// Finds the next incomplete phase in the research document.
    /// Returns the first phase marked as ⏳ Not Started or 🔄 In Progress.
    /// </summary>
    /// <param name="researchContent">The full content of the research document.</param>
    /// <returns>The next phase to execute, or null if all phases are complete.</returns>
    public ResearchPhase? FindNextIncompletePhase(string researchContent)
    {
        var phases = ParsePhases(researchContent);

        // First, look for any in-progress phase (should resume that)
        var inProgress = phases.FirstOrDefault(p => p.Status == PhaseProgressStatus.InProgress);
        if (inProgress != null)
        {
            return inProgress;
        }

        // Then, look for the first not-started phase
        return phases.FirstOrDefault(p => p.Status == PhaseProgressStatus.NotStarted);
    }

    /// <summary>
    /// Quickly checks if there are any incomplete phases using regex.
    /// More efficient than full parsing when you only need to know if work remains.
    /// </summary>
    /// <param name="researchContent">The full content of the research document.</param>
    /// <returns>True if there are incomplete phases.</returns>
    public bool HasIncompletePhases(string researchContent)
    {
        if (string.IsNullOrWhiteSpace(researchContent))
        {
            return false;
        }

        return IncompleteResearchPhaseRegex().IsMatch(researchContent);
    }

    /// <summary>
    /// Gets all incomplete phase numbers from the research document.
    /// </summary>
    /// <param name="researchContent">The full content of the research document.</param>
    /// <returns>List of phase numbers that are not yet complete.</returns>
    public IReadOnlyList<int> GetIncompletePhaseNumbers(string researchContent)
    {
        if (string.IsNullOrWhiteSpace(researchContent))
        {
            return Array.Empty<int>();
        }

        var matches = IncompleteResearchPhaseRegex().Matches(researchContent);
        var phaseNumbers = new List<int>(matches.Count);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var phaseNumber))
            {
                phaseNumbers.Add(phaseNumber);
            }
        }

        return phaseNumbers.Distinct().OrderBy(n => n).ToList();
    }

    private static PhaseProgressStatus ParseStatusEmoji(string emoji)
    {
        return emoji switch
        {
            "✅" => PhaseProgressStatus.Complete,
            "🔄" => PhaseProgressStatus.InProgress,
            "⏳" => PhaseProgressStatus.NotStarted,
            "❌" => PhaseProgressStatus.Blocked,
            _ => PhaseProgressStatus.NotStarted
        };
    }
}
