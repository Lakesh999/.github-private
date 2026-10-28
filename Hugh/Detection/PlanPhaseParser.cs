using System.Text.RegularExpressions;

namespace Hugh.Detection;

/// <summary>
/// Represents a phase parsed from a plan's Progress Summary table.
/// </summary>
public record PlanPhase
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
    /// The status emoji/text (✅, 🔄, ⏳, ❌).
    /// </summary>
    public string StatusEmoji { get; init; } = "";

    /// <summary>
    /// Parsed status from the emoji.
    /// </summary>
    public PhaseProgressStatus Status { get; init; }

    /// <summary>
    /// Number of tasks completed in this phase.
    /// </summary>
    public int TasksCompleted { get; init; }

    /// <summary>
    /// Total number of tasks in this phase.
    /// </summary>
    public int TasksTotal { get; init; }
}

/// <summary>
/// Status of a phase in the plan progress table.
/// </summary>
public enum PhaseProgressStatus
{
    NotStarted,
    InProgress,
    Complete,
    Blocked
}

/// <summary>
/// Parses Progress Summary tables from implementation plans.
/// Used to determine the next phase to execute.
/// </summary>
public partial class PlanPhaseParser
{
    // Matches a phase row in the Progress Summary table
    // | 1 | Phase Name | ⏳ Not Started | 0/5 |
    // | 2 | Phase Name | 🔄 In Progress | 2/8 |
    // | 3 | Phase Name | ✅ Complete | 5/5 |
    [GeneratedRegex(@"\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅|🔄|⏳|❌)\s*([^|]*)\|\s*(\d+)/(\d+)\s*\|", RegexOptions.Compiled)]
    private static partial Regex PlanPhaseRowRegex();

    // Simpler pattern for phase rows without task counts
    // | 1 | Phase Name | ⏳ Not Started |
    [GeneratedRegex(@"\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅|🔄|⏳|❌)\s*([^|]*)\|", RegexOptions.Compiled)]
    private static partial Regex PlanPhaseRowSimpleRegex();

    // Matches incomplete phase status (⏳ or 🔄)
    [GeneratedRegex(@"\|\s*(\d+)\s*\|[^|]+\|\s*(⏳|🔄)\s*[^|]*\|", RegexOptions.Compiled)]
    private static partial Regex IncompletePhaseRegex();

    /// <summary>
    /// Parses all phases from a plan document's Progress Summary table.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>List of parsed phases in order.</returns>
    public IReadOnlyList<PlanPhase> ParsePhases(string planContent)
    {
        if (string.IsNullOrWhiteSpace(planContent))
        {
            return Array.Empty<PlanPhase>();
        }

        var matches = PlanPhaseRowRegex().Matches(planContent);
        var phases = new List<PlanPhase>(matches.Count);

        foreach (Match match in matches)
        {
            if (int.TryParse(match.Groups[1].Value, out var phaseNumber) &&
                int.TryParse(match.Groups[5].Value, out var tasksCompleted) &&
                int.TryParse(match.Groups[6].Value, out var tasksTotal))
            {
                var statusEmoji = match.Groups[3].Value.Trim();
                phases.Add(new PlanPhase
                {
                    Number = phaseNumber,
                    Name = match.Groups[2].Value.Trim(),
                    StatusEmoji = statusEmoji,
                    Status = ParseStatusEmoji(statusEmoji),
                    TasksCompleted = tasksCompleted,
                    TasksTotal = tasksTotal
                });
            }
        }

        // If no matches with task counts, try simpler pattern
        if (phases.Count == 0)
        {
            var simpleMatches = PlanPhaseRowSimpleRegex().Matches(planContent);
            foreach (Match match in simpleMatches)
            {
                if (int.TryParse(match.Groups[1].Value, out var phaseNumber))
                {
                    var statusEmoji = match.Groups[3].Value.Trim();
                    phases.Add(new PlanPhase
                    {
                        Number = phaseNumber,
                        Name = match.Groups[2].Value.Trim(),
                        StatusEmoji = statusEmoji,
                        Status = ParseStatusEmoji(statusEmoji),
                        TasksCompleted = 0,
                        TasksTotal = 0
                    });
                }
            }
        }

        return phases.OrderBy(p => p.Number).ToList();
    }

    /// <summary>
    /// Finds the next incomplete phase in the plan document.
    /// Returns the first phase marked as ⏳ Not Started or 🔄 In Progress.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>The next phase to execute, or null if all phases are complete.</returns>
    public PlanPhase? FindNextIncompletePhase(string planContent)
    {
        var phases = ParsePhases(planContent);

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
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>True if there are incomplete phases.</returns>
    public bool HasIncompletePhases(string planContent)
    {
        if (string.IsNullOrWhiteSpace(planContent))
        {
            return false;
        }

        return IncompletePhaseRegex().IsMatch(planContent);
    }

    /// <summary>
    /// Gets all incomplete phase numbers from the plan.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>List of phase numbers that are not yet complete.</returns>
    public IReadOnlyList<int> GetIncompletePhaseNumbers(string planContent)
    {
        if (string.IsNullOrWhiteSpace(planContent))
        {
            return Array.Empty<int>();
        }

        var matches = IncompletePhaseRegex().Matches(planContent);
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

    /// <summary>
    /// Calculates overall progress of the plan.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <returns>Tuple of (completed phases, total phases, completion percentage).</returns>
    public (int Completed, int Total, double Percentage) CalculateProgress(string planContent)
    {
        var phases = ParsePhases(planContent);
        if (phases.Count == 0)
        {
            return (0, 0, 0.0);
        }

        var completed = phases.Count(p => p.Status == PhaseProgressStatus.Complete);
        var total = phases.Count;
        var percentage = (double)completed / total * 100;

        return (completed, total, Math.Round(percentage, 1));
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
