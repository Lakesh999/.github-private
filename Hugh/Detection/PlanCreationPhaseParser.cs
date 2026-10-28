using System.Text.RegularExpressions;

namespace Hugh.Detection;

/// <summary>
/// Represents a phase parsed from a plan skeleton's Execution Plan section.
/// Used during the planning sub-phase loop to determine which phases still
/// need detailed task tables filled in.
/// </summary>
public record PlanCreationPhase
{
    /// <summary>
    /// The phase number (1, 2, 3, etc.)
    /// </summary>
    public int Number { get; init; }

    /// <summary>
    /// The phase name/description (text after "Phase N:").
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Whether this phase already has a detailed task table.
    /// A phase is considered "complete" for planning purposes when its section body
    /// contains a Markdown table header row with both "Step" and "Task" columns.
    /// </summary>
    public bool HasTaskTable { get; init; }

    /// <summary>
    /// The full text of the phase section body (between this header and the next phase header or end-of-document).
    /// </summary>
    public string Scope { get; init; } = "";
}

/// <summary>
/// Parses plan skeleton documents to detect which phases have been filled in
/// with detailed task tables and which are still empty outlines.
/// Used by the planning sub-phase loop to determine the next phase that needs
/// detailed task generation.
/// </summary>
public partial class PlanCreationPhaseParser
{
    // Matches phase headers in the Execution Plan section: ### Phase 1: Name, ### Phase 1 - Name, ### Phase 1 — Name
    [GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*[:–—\-]\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex PhaseHeaderRegex();

    // Matches a task table header row containing a "| Step |" column header
    [GeneratedRegex(@"\|\s*Step\s*\|", RegexOptions.Compiled)]
    private static partial Regex StepColumnRegex();

    // Matches a task table header row containing a "| Task |" column header
    [GeneratedRegex(@"\|\s*Task\s*\|", RegexOptions.Compiled)]
    private static partial Regex TaskColumnRegex();

    /// <summary>
    /// Parses all phases from a plan document's Execution Plan section.
    /// Detects phase headers via <c>### Phase N: Name</c> regex and checks
    /// whether each phase's section body contains a task table.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>List of parsed plan creation phases in order.</returns>
    public IReadOnlyList<PlanCreationPhase> ParsePhases(string planContent)
    {
        if (string.IsNullOrWhiteSpace(planContent))
        {
            return Array.Empty<PlanCreationPhase>();
        }

        var matches = PhaseHeaderRegex().Matches(planContent);
        var phases = new List<PlanCreationPhase>(matches.Count);

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!int.TryParse(match.Groups[1].Value, out var phaseNumber))
                continue;

            var phaseName = match.Groups[2].Value.Trim();

            // Extract the section body: text between this header and the next phase header (or end-of-document)
            var sectionStart = match.Index + match.Length;
            var sectionEnd = i + 1 < matches.Count
                ? matches[i + 1].Index
                : planContent.Length;

            var sectionBody = planContent[sectionStart..sectionEnd];

            // Check if the section body contains both Step and Task column headers
            var hasTaskTable = StepColumnRegex().IsMatch(sectionBody)
                               && TaskColumnRegex().IsMatch(sectionBody);

            phases.Add(new PlanCreationPhase
            {
                Number = phaseNumber,
                Name = phaseName,
                HasTaskTable = hasTaskTable,
                Scope = sectionBody.Trim()
            });
        }

        return phases.OrderBy(p => p.Number).ToList();
    }

    /// <summary>
    /// Finds the next incomplete phase in the plan skeleton.
    /// Returns the first phase that does NOT have a task table.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>The next phase to fill in, or null if all phases have task tables.</returns>
    public PlanCreationPhase? FindNextIncompletePhase(string planContent)
    {
        var phases = ParsePhases(planContent);
        return phases.FirstOrDefault(p => !p.HasTaskTable);
    }

    /// <summary>
    /// Checks if there are any incomplete phases (phases without task tables).
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>True if there are phases that still need task tables.</returns>
    public bool HasIncompletePhases(string planContent)
    {
        var phases = ParsePhases(planContent);
        return phases.Any(p => !p.HasTaskTable);
    }

    /// <summary>
    /// Gets all incomplete phase numbers from the plan skeleton.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>List of phase numbers that do not yet have task tables.</returns>
    public IReadOnlyList<int> GetIncompletePhaseNumbers(string planContent)
    {
        var phases = ParsePhases(planContent);
        return phases
            .Where(p => !p.HasTaskTable)
            .Select(p => p.Number)
            .OrderBy(n => n)
            .ToList();
    }
}
