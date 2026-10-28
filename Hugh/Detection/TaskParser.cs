using System.Text.RegularExpressions;

namespace Hugh.Detection;

/// <summary>
/// Represents a task parsed from a plan's task table within a specific phase.
/// Used during the task-level coding loop to determine which tasks still
/// need implementation within a plan phase.
/// </summary>
public record PlanTask
{
    /// <summary>
    /// The phase number this task belongs to (e.g., 5).
    /// </summary>
    public int PhaseNumber { get; init; }

    /// <summary>
    /// The task number within the phase (e.g., 1 for step 5.1).
    /// </summary>
    public int TaskNumber { get; init; }

    /// <summary>
    /// The full step ID (e.g., "5.1").
    /// </summary>
    public string StepId { get; init; } = "";

    /// <summary>
    /// The task description from the Task column.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Whether this task has been marked complete (row contains ✅).
    /// </summary>
    public bool IsComplete { get; init; }

    /// <summary>
    /// The files referenced in the Files column.
    /// </summary>
    public string Files { get; init; } = "";

    /// <summary>
    /// The acceptance criteria from the Acceptance Criteria column.
    /// </summary>
    public string AcceptanceCriteria { get; init; } = "";
}

/// <summary>
/// Parses task tables from plan documents to detect which tasks within a phase
/// have been completed and which still need implementation.
/// Used by the task-level coding loop to iterate individual tasks within a phase.
/// </summary>
public partial class TaskParser
{
    // Matches phase headers: ### Phase 1: Name, ### Phase 1 - Name, ### Phase 1 — Name, ### Phase 1 – Name
    [GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*[:–—\-]\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
    private static partial Regex PhaseHeaderRegex();

    // Matches a task row in a plan table: | StepId | Description | Files | Acceptance Criteria |
    // StepId format: N.M (e.g., "5.1", "12.3")
    [GeneratedRegex(@"\|\s*(\d+\.\d+)\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|", RegexOptions.Compiled)]
    private static partial Regex TaskRowRegex();

    /// <summary>
    /// Parses all tasks from a specific phase's task table in the plan document.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <param name="phaseNumber">The phase number to parse tasks from.</param>
    /// <returns>List of parsed tasks in order.</returns>
    public IReadOnlyList<PlanTask> ParseTasks(string planContent, int phaseNumber)
    {
        if (string.IsNullOrWhiteSpace(planContent))
        {
            return Array.Empty<PlanTask>();
        }

        var sectionBody = ExtractPhaseSection(planContent, phaseNumber);
        if (sectionBody == null)
        {
            return Array.Empty<PlanTask>();
        }

        var matches = TaskRowRegex().Matches(sectionBody);
        var tasks = new List<PlanTask>(matches.Count);

        foreach (Match match in matches)
        {
            var stepId = match.Groups[1].Value.Trim();
            var parts = stepId.Split('.');
            if (parts.Length != 2
                || !int.TryParse(parts[0], out var parsedPhase)
                || !int.TryParse(parts[1], out var taskNum))
            {
                continue;
            }

            // Only include tasks whose step ID matches the requested phase
            if (parsedPhase != phaseNumber)
            {
                continue;
            }

            // A row is complete if it contains ✅ anywhere in the row text
            var fullRow = match.Value;
            var isComplete = fullRow.Contains("✅");

            tasks.Add(new PlanTask
            {
                PhaseNumber = phaseNumber,
                TaskNumber = taskNum,
                StepId = stepId,
                Description = match.Groups[2].Value.Trim(),
                IsComplete = isComplete,
                Files = match.Groups[3].Value.Trim(),
                AcceptanceCriteria = match.Groups[4].Value.Trim()
            });
        }

        return tasks;
    }

    /// <summary>
    /// Finds the next incomplete task within a specific phase.
    /// Returns the first task that does NOT contain ✅ in its row.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <param name="phaseNumber">The phase number to search within.</param>
    /// <returns>The next incomplete task, or null if all tasks are complete.</returns>
    public PlanTask? FindNextIncompleteTask(string planContent, int phaseNumber)
    {
        var tasks = ParseTasks(planContent, phaseNumber);
        return tasks.FirstOrDefault(t => !t.IsComplete);
    }

    /// <summary>
    /// Checks if there are any incomplete tasks within a specific phase.
    /// </summary>
    /// <param name="planContent">The full content of the plan document.</param>
    /// <param name="phaseNumber">The phase number to check.</param>
    /// <returns>True if there are incomplete tasks in the phase.</returns>
    public bool HasIncompleteTasks(string planContent, int phaseNumber)
    {
        var tasks = ParseTasks(planContent, phaseNumber);
        return tasks.Any(t => !t.IsComplete);
    }

    /// <summary>
    /// Extracts the section body for a specific phase from the plan document.
    /// The section extends from the phase header to the next phase header or end-of-document.
    /// </summary>
    private static string? ExtractPhaseSection(string planContent, int phaseNumber)
    {
        var matches = PhaseHeaderRegex().Matches(planContent);

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            if (!int.TryParse(match.Groups[1].Value, out var num) || num != phaseNumber)
            {
                continue;
            }

            var sectionStart = match.Index + match.Length;
            var sectionEnd = i + 1 < matches.Count
                ? matches[i + 1].Index
                : planContent.Length;

            return planContent[sectionStart..sectionEnd];
        }

        return null;
    }
}
