using System.Text.Json;
using System.Text.Json.Serialization;
using Hugh.Models;
using Serilog;

namespace Hugh.Logging;

/// <summary>
/// Logs decisions made during the development loop to a JSONL file for audit purposes.
/// Decisions are appended to .orchestrator/decisions.jsonl
/// </summary>
public class DecisionLogger
{
    private const string OrchestratorFolder = ".orchestrator";
    private const string DecisionsFileName = "decisions.jsonl";

    private readonly string _workspaceRoot;
    private readonly string _decisionsFilePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public DecisionLogger(string workspaceRoot, ILogger? logger = null)
    {
        _workspaceRoot = workspaceRoot;
        _decisionsFilePath = Path.Combine(workspaceRoot, OrchestratorFolder, DecisionsFileName);
        _logger = logger ?? Log.Logger;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Logs a decision to the decisions file.
    /// </summary>
    /// <param name="decision">The decision to log.</param>
    /// <param name="runId">The run ID this decision belongs to.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task LogDecisionAsync(Decision decision, Guid runId, CancellationToken ct = default)
    {
        _logger.Information(
            "Decision logged: Phase={Phase}, Question={Question}, Selected={Selected}, Autonomous={Autonomous}",
            decision.Phase,
            decision.Question,
            decision.Selected,
            decision.Autonomous);

        // Create entry with run context
        var entry = new DecisionLogEntry
        {
            RunId = runId,
            Phase = decision.Phase,
            Question = decision.Question,
            Options = decision.Options,
            Selected = decision.Selected,
            Rationale = decision.Rationale,
            Autonomous = decision.Autonomous,
            Timestamp = decision.Timestamp
        };

        await AppendEntryAsync(entry, ct);
    }

    /// <summary>
    /// Logs multiple decisions at once.
    /// </summary>
    public async Task LogDecisionsAsync(
        IEnumerable<Decision> decisions,
        Guid runId,
        CancellationToken ct = default)
    {
        foreach (var decision in decisions)
        {
            await LogDecisionAsync(decision, runId, ct);
        }
    }

    /// <summary>
    /// Reads all decisions for a specific run.
    /// </summary>
    public async Task<IReadOnlyList<Decision>> GetDecisionsForRunAsync(
        Guid runId,
        CancellationToken ct = default)
    {
        if (!File.Exists(_decisionsFilePath))
        {
            return Array.Empty<Decision>();
        }

        var decisions = new List<Decision>();

        await foreach (var line in ReadLinesAsync(_decisionsFilePath, ct))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize<DecisionLogEntry>(line, _jsonOptions);
                if (entry?.RunId == runId)
                {
                    decisions.Add(new Decision
                    {
                        Phase = entry.Phase,
                        Question = entry.Question,
                        Options = entry.Options,
                        Selected = entry.Selected,
                        Rationale = entry.Rationale,
                        Autonomous = entry.Autonomous,
                        Timestamp = entry.Timestamp
                    });
                }
            }
            catch (JsonException ex)
            {
                _logger.Warning(ex, "Failed to parse decision log entry: {Line}", line);
            }
        }

        return decisions;
    }

    /// <summary>
    /// Gets all unique run IDs from the decision log.
    /// </summary>
    public async Task<IReadOnlyList<Guid>> GetAllRunIdsAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_decisionsFilePath))
        {
            return Array.Empty<Guid>();
        }

        var runIds = new HashSet<Guid>();

        await foreach (var line in ReadLinesAsync(_decisionsFilePath, ct))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var entry = JsonSerializer.Deserialize<DecisionLogEntry>(line, _jsonOptions);
                if (entry != null)
                {
                    runIds.Add(entry.RunId);
                }
            }
            catch (JsonException)
            {
                // Skip malformed entries
            }
        }

        return runIds.OrderBy(id => id).ToList();
    }

    /// <summary>
    /// Appends a decision entry to the JSONL file.
    /// </summary>
    private async Task AppendEntryAsync(DecisionLogEntry entry, CancellationToken ct)
    {
        // Ensure directory exists
        var directory = Path.GetDirectoryName(_decisionsFilePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Debug("Created orchestrator directory: {Path}", directory);
        }

        var json = JsonSerializer.Serialize(entry, _jsonOptions);
        await File.AppendAllTextAsync(_decisionsFilePath, json + Environment.NewLine, ct);

        _logger.Debug("Decision entry appended to {Path}", _decisionsFilePath);
    }

    /// <summary>
    /// Reads lines from a file asynchronously.
    /// </summary>
    private static async IAsyncEnumerable<string> ReadLinesAsync(
        string path,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct = default)
    {
        using var reader = new StreamReader(path);
        while (!reader.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();
            var line = await reader.ReadLineAsync(ct);
            if (line != null)
            {
                yield return line;
            }
        }
    }

    /// <summary>
    /// Internal entry format for the JSONL file.
    /// Includes run context alongside decision data.
    /// </summary>
    private record DecisionLogEntry
    {
        public Guid RunId { get; init; }
        public string Phase { get; init; } = "";
        public string Question { get; init; } = "";
        public List<string> Options { get; init; } = new();
        public string Selected { get; init; } = "";
        public string Rationale { get; init; } = "";
        public bool Autonomous { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
