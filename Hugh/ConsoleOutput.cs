using Hugh.Models;

namespace Hugh;

/// <summary>
/// Provides polished console output with progress indicators and formatting.
/// </summary>
public static class ConsoleOutput
{
    private static readonly object _lock = new();
    private static bool _colorsEnabled = true;

    /// <summary>
    /// Enables or disables colored output.
    /// </summary>
    public static bool ColorsEnabled
    {
        get => _colorsEnabled;
        set => _colorsEnabled = value;
    }

    #region Section Headers

    /// <summary>
    /// Writes a section header with a separator line.
    /// </summary>
    public static void WriteSection(string title)
    {
        lock (_lock)
        {
            Console.WriteLine();
            WriteColored(title, ConsoleColor.Cyan);
            Console.WriteLine(new string('─', Math.Max(40, title.Length + 10)));
        }
    }

    /// <summary>
    /// Writes the application header.
    /// </summary>
    public static void WriteHeader()
    {
        lock (_lock)
        {
            Console.WriteLine();
            WriteColored("╔══════════════════════════════════════════╗", ConsoleColor.DarkCyan);
            WriteColored("║      Hugh - Self-Operating Dev Loop      ║", ConsoleColor.Cyan);
            WriteColored("║    \"Freed from constant interaction\"     ║", ConsoleColor.DarkGray);
            WriteColored("╚══════════════════════════════════════════╝", ConsoleColor.DarkCyan);
            Console.WriteLine();
        }
    }

    #endregion

    #region Status Messages

    /// <summary>
    /// Writes a success message with a checkmark.
    /// </summary>
    public static void WriteSuccess(string message)
    {
        lock (_lock)
        {
            WriteColored("✓ ", ConsoleColor.Green);
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes an error message with an X.
    /// </summary>
    public static void WriteError(string message)
    {
        lock (_lock)
        {
            WriteColored("✗ ", ConsoleColor.Red);
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes a warning message.
    /// </summary>
    public static void WriteWarning(string message)
    {
        lock (_lock)
        {
            WriteColored("⚠ ", ConsoleColor.Yellow);
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes an info message.
    /// </summary>
    public static void WriteInfo(string message)
    {
        lock (_lock)
        {
            WriteColored("ℹ ", ConsoleColor.Blue);
            Console.WriteLine(message);
        }
    }

    /// <summary>
    /// Writes a hint/suggestion message.
    /// </summary>
    public static void WriteHint(string message)
    {
        lock (_lock)
        {
            Console.Write("  ");
            WriteColored("→ ", ConsoleColor.DarkGray);
            WriteColored(message, ConsoleColor.DarkGray);
            Console.WriteLine();
        }
    }

    #endregion

    #region Progress Indicators

    /// <summary>
    /// Writes a phase status with visual indicator.
    /// </summary>
    public static void WritePhaseStatus(string phaseName, PhaseStatus status, string? details = null)
    {
        lock (_lock)
        {
            var (icon, color) = status switch
            {
                PhaseStatus.Complete => ("✅", ConsoleColor.Green),
                PhaseStatus.InProgress => ("🔄", ConsoleColor.Yellow),
                PhaseStatus.Failed => ("❌", ConsoleColor.Red),
                PhaseStatus.Paused => ("⏸️", ConsoleColor.DarkYellow),
                _ => ("⏳", ConsoleColor.DarkGray)
            };

            Console.Write("  ");
            Console.Write(icon);
            Console.Write(" ");
            WriteColored(phaseName.PadRight(12), color);

            if (!string.IsNullOrEmpty(details))
            {
                WriteColored($" - {details}", ConsoleColor.DarkGray);
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Writes a progress bar.
    /// </summary>
    public static void WriteProgressBar(int current, int total, string label = "")
    {
        lock (_lock)
        {
            const int barWidth = 30;
            var progress = total > 0 ? (double)current / total : 0;
            var filled = (int)(progress * barWidth);
            var empty = barWidth - filled;

            Console.Write($"  {label} [");
            WriteColored(new string('█', filled), ConsoleColor.Green);
            WriteColored(new string('░', empty), ConsoleColor.DarkGray);
            Console.WriteLine($"] {current}/{total} ({progress:P0})");
        }
    }

    #endregion

    #region Status Display

    /// <summary>
    /// Displays a formatted run status.
    /// </summary>
    public static void WriteRunStatus(LoopState state)
    {
        WriteSection("Hugh Status");

        // Run info
        WriteLabel("Run ID", state.RunId.ToString());
        WriteLabel("Started", state.StartedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        WriteLabel("Feature", TruncateText(state.FeatureRequest, 50));
        WriteLabel("Status", FormatStatus(state.Status));
        WriteLabel("Current Phase", state.CurrentPhase.ToString());

        if (state.CurrentSubPhase.HasValue)
        {
            WriteLabel("Sub-Phase", state.CurrentSubPhase.Value.ToString());
        }

        Console.WriteLine();

        // Completed phases
        WriteColored("Completed Phases:", ConsoleColor.White);
        Console.WriteLine();

        var phases = new[] { LoopPhase.Vision, LoopPhase.Research, LoopPhase.Planning, LoopPhase.Review, LoopPhase.Coding };
        foreach (var phase in phases)
        {
            var status = GetPhaseDisplayStatus(phase, state);
            var doc = GetDocumentPath(phase, state);
            WritePhaseStatus(phase.ToString(), status, doc);
        }

        Console.WriteLine();

        // Decisions count
        WriteLabel("Decisions Made", state.Decisions.Count.ToString());

        if (!string.IsNullOrEmpty(state.FailureReason))
        {
            Console.WriteLine();
            WriteError($"Failure Reason: {state.FailureReason}");
        }

        Console.WriteLine(new string('─', 40));
    }

    private static PhaseStatus GetPhaseDisplayStatus(LoopPhase phase, LoopState state)
    {
        if (state.CompletedPhases.Contains(phase))
            return PhaseStatus.Complete;
        if (state.CurrentPhase == phase)
            return state.Status;
        return PhaseStatus.NotStarted;
    }

    private static string? GetDocumentPath(LoopPhase phase, LoopState state)
    {
        return phase switch
        {
            LoopPhase.Vision => state.Documents.Vision,
            LoopPhase.Research => state.Documents.Research,
            LoopPhase.Planning => state.Documents.Plan,
            _ => null
        };
    }

    private static string FormatStatus(PhaseStatus status)
    {
        return status switch
        {
            PhaseStatus.Complete => "✅ Complete",
            PhaseStatus.InProgress => "🔄 In Progress",
            PhaseStatus.Failed => "❌ Failed",
            PhaseStatus.Paused => "⏸️ Paused",
            _ => "⏳ Not Started"
        };
    }

    #endregion

    #region Helpers

    private static void WriteLabel(string label, string value)
    {
        lock (_lock)
        {
            WriteColored($"  {label.PadRight(16)}", ConsoleColor.DarkGray);
            Console.WriteLine(value);
        }
    }

    private static void WriteColored(string text, ConsoleColor color)
    {
        if (_colorsEnabled)
        {
            var original = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = original;
        }
        else
        {
            Console.Write(text);
        }
    }

    private static string TruncateText(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text) || text.Length <= maxLength)
            return text;

        return text[..(maxLength - 3)] + "...";
    }

    #endregion

    #region Phase Summaries

    /// <summary>
    /// Writes a phase start message.
    /// </summary>
    public static void WritePhaseStart(LoopPhase phase, string agentName)
    {
        lock (_lock)
        {
            Console.WriteLine();
            WriteColored($"▶ Starting {phase} phase", ConsoleColor.Cyan);
            Console.WriteLine();
            WriteColored($"  Agent: ", ConsoleColor.DarkGray);
            Console.WriteLine(agentName);
            Console.WriteLine();
        }
    }

    /// <summary>
    /// Writes a phase complete message.
    /// </summary>
    public static void WritePhaseComplete(LoopPhase phase, TimeSpan duration, string? outputPath = null)
    {
        lock (_lock)
        {
            Console.WriteLine();
            WriteColored($"✓ {phase} phase complete", ConsoleColor.Green);
            WriteColored($" ({duration.TotalSeconds:F1}s)", ConsoleColor.DarkGray);
            Console.WriteLine();

            if (!string.IsNullOrEmpty(outputPath))
            {
                WriteColored($"  Output: ", ConsoleColor.DarkGray);
                Console.WriteLine(outputPath);
            }
        }
    }

    /// <summary>
    /// Writes a phase failure message.
    /// </summary>
    public static void WritePhaseFailed(LoopPhase phase, string error)
    {
        lock (_lock)
        {
            Console.WriteLine();
            WriteColored($"✗ {phase} phase failed", ConsoleColor.Red);
            Console.WriteLine();
            WriteError(error);
        }
    }

    /// <summary>
    /// Writes the final completion summary.
    /// </summary>
    public static void WriteCompletionSummary(LoopState state, TimeSpan totalDuration)
    {
        WriteSection("Implementation Complete");

        WriteSuccess("All phases completed successfully!");
        Console.WriteLine();

        WriteLabel("Total Duration", $"{totalDuration.TotalMinutes:F1} minutes");
        WriteLabel("Decisions Made", state.Decisions.Count.ToString());
        WriteLabel("Phases", $"{state.CompletedPhases.Count}/5");

        Console.WriteLine();

        if (!string.IsNullOrEmpty(state.Documents.Vision))
            WriteLabel("Vision", state.Documents.Vision);
        if (!string.IsNullOrEmpty(state.Documents.Research))
            WriteLabel("Research", state.Documents.Research);
        if (!string.IsNullOrEmpty(state.Documents.Plan))
            WriteLabel("Plan", state.Documents.Plan);

        Console.WriteLine(new string('─', 40));
    }

    #endregion

    #region Session Report

    /// <summary>
    /// Writes a session metrics summary table showing per-phase token usage, duration, and tool counts.
    /// </summary>
    public static void WriteSessionReport(SessionReport report)
    {
        lock (_lock)
        {
            WriteSection("Session Metrics Report");

            // Header row
            WriteColored(
                $"  {"Phase",-14} {"Input",-8} {"Output",-8} {"Reason",-8} {"Duration",-10} {"Tools",-6} {"Model",-20}",
                ConsoleColor.White);
            Console.WriteLine();
            Console.WriteLine($"  {new string('─', 76)}");

            // Per-phase rows
            foreach (var phase in report.Phases)
            {
                var input = phase.Usage?.InputTokens.ToString() ?? "—";
                var output = phase.Usage?.OutputTokens.ToString() ?? "—";
                var reasoning = phase.Usage?.ReasoningTokens.ToString() ?? "—";
                var duration = phase.Duration.TotalSeconds > 0 ? $"{phase.Duration.TotalSeconds:F1}s" : "—";
                var tools = phase.Tools?.TotalExecutions.ToString() ?? "—";
                var model = phase.Usage?.Model ?? "—";

                Console.Write("  ");
                WriteColored($"{phase.PhaseName,-14}", ConsoleColor.Cyan);
                Console.WriteLine($" {input,-8} {output,-8} {reasoning,-8} {duration,-10} {tools,-6} {model,-20}");
            }

            // Totals
            Console.WriteLine($"  {new string('─', 76)}");
            Console.Write("  ");
            WriteColored($"{"TOTAL",-14}", ConsoleColor.Yellow);
            Console.WriteLine(
                $" {report.TotalUsage.InputTokens,-8} {report.TotalUsage.OutputTokens,-8} " +
                $"{report.TotalUsage.ReasoningTokens,-8} {report.TotalDuration.TotalSeconds:F1}s");

            Console.WriteLine();

            // Additional summary
            if (report.CompactionEvents > 0)
            {
                WriteWarning($"Context compaction events: {report.CompactionEvents}");
            }

            if (report.TotalSubagents.Started > 0)
            {
                WriteInfo($"Subagents: {report.TotalSubagents.Started} started, " +
                          $"{report.TotalSubagents.Completed} completed, " +
                          $"{report.TotalSubagents.Failed} failed");
            }

            Console.WriteLine(new string('─', 40));
        }
    }

    #endregion
}
