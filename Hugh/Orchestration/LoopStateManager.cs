using System.Text.Json;
using System.Text.Json.Serialization;
using Hugh.Models;
using Serilog;

namespace Hugh.Orchestration;

/// <summary>
/// Manages the persistent state of the development loop.
/// State is persisted to .orchestrator/loop-state.json for resumability.
/// </summary>
public class LoopStateManager
{
    private const string OrchestratorFolder = ".orchestrator";
    private const string StateFileName = "loop-state.json";

    private readonly string _workspaceRoot;
    private readonly string _stateFilePath;
    private readonly ILogger _logger;
    private readonly IProgressReporter? _progressReporter;
    private readonly JsonSerializerOptions _jsonOptions;

    private LoopState? _currentState;

    public LoopStateManager(string workspaceRoot, ILogger? logger = null, string? stateDirectory = null, IProgressReporter? progressReporter = null)
    {
        _workspaceRoot = workspaceRoot;
        var stateDir = stateDirectory ?? OrchestratorFolder;
        _stateFilePath = Path.Combine(workspaceRoot, stateDir, StateFileName);
        _logger = logger ?? Log.Logger;
        _progressReporter = progressReporter;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Gets the current state, if loaded.
    /// </summary>
    public LoopState? CurrentState => _currentState;

    /// <summary>
    /// Creates a new run with the given feature request.
    /// </summary>
    public async Task<LoopState> CreateRunAsync(string featureRequest, CancellationToken ct = default)
    {
        _logger.Information("Creating new run for feature: {FeatureRequest}", featureRequest);

        _currentState = new LoopState
        {
            RunId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            FeatureRequest = featureRequest,
            CurrentPhase = LoopPhase.Vision,
            CurrentSubPhase = null,
            CompletedPhases = new List<LoopPhase>(),
            Documents = new DocumentPaths(),
            Decisions = new List<Decision>(),
            Status = PhaseStatus.NotStarted,
            FailureReason = null
        };

        await SaveStateAsync(ct);

        _logger.Information("Created run {RunId}", _currentState.RunId);
        return _currentState;
    }

    /// <summary>
    /// Creates a new run that starts directly at the specified phase, with prior phases
    /// pre-populated as complete and their documents already set. This avoids the iterative
    /// MarkPhaseCompleteAsync hack used previously.
    /// </summary>
    /// <param name="featureRequest">The feature/release description.</param>
    /// <param name="startPhase">The phase to start at.</param>
    /// <param name="documents">Pre-populated document paths for completed phases.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The new state positioned at <paramref name="startPhase"/>.</returns>
    public async Task<LoopState> CreateRunFromPhaseAsync(
        string featureRequest,
        LoopPhase startPhase,
        DocumentPaths? documents = null,
        CancellationToken ct = default)
    {
        _logger.Information(
            "Creating new run starting at {StartPhase} for feature: {FeatureRequest}",
            startPhase, featureRequest);

        // Build the list of phases that are already complete (everything before startPhase)
        var completedPhases = new List<LoopPhase>();
        foreach (var phase in AllPhases)
        {
            if (phase == startPhase) break;
            completedPhases.Add(phase);
        }

        _currentState = new LoopState
        {
            RunId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            FeatureRequest = featureRequest,
            CurrentPhase = startPhase,
            CurrentSubPhase = startPhase == LoopPhase.Coding ? 1 : null,
            CompletedPhases = completedPhases,
            Documents = documents ?? new DocumentPaths(),
            Decisions = new List<Decision>(),
            Status = PhaseStatus.NotStarted,
            FailureReason = null
        };

        await SaveStateAsync(ct);

        _logger.Information("Created run {RunId} starting at {StartPhase}", _currentState.RunId, startPhase);
        return _currentState;
    }

    /// <summary>
    /// Ordered list of all phases in the development loop.
    /// </summary>
    private static readonly LoopPhase[] AllPhases =
    {
        LoopPhase.Vision,
        LoopPhase.Research,
        LoopPhase.Planning,
        LoopPhase.Review,
        LoopPhase.Coding
    };

    /// <summary>
    /// Loads the current state from disk.
    /// </summary>
    /// <returns>The loaded state, or null if no state file exists.</returns>
    public async Task<LoopState?> LoadStateAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_stateFilePath))
        {
            _logger.Debug("No existing state file found at {Path}", _stateFilePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_stateFilePath, ct);
            _currentState = JsonSerializer.Deserialize<LoopState>(json, _jsonOptions);
            _logger.Information("Loaded state for run {RunId}", _currentState?.RunId);
            return _currentState;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load state from {Path}", _stateFilePath);
            throw;
        }
    }

    /// <summary>
    /// Loads state for a specific run ID.
    /// </summary>
    public async Task<LoopState?> LoadStateAsync(Guid runId, CancellationToken ct = default)
    {
        var state = await LoadStateAsync(ct);
        if (state?.RunId == runId)
        {
            return state;
        }

        _logger.Warning("Requested run {RequestedId} but found {FoundId}", runId, state?.RunId);
        return null;
    }

    /// <summary>
    /// Updates the current phase and saves state.
    /// </summary>
    public async Task UpdatePhaseAsync(
        LoopPhase phase,
        PhaseStatus status,
        int? subPhase = null,
        CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        _logger.Information("Updating phase to {Phase} with status {Status}", phase, status);

        _currentState = _currentState with
        {
            CurrentPhase = phase,
            CurrentSubPhase = subPhase,
            Status = status
        };

        await SaveStateAsync(ct);
        await NotifyProgressReporterAsync(r => r.OnPhaseUpdatedAsync(phase, status, subPhase));
    }

    /// <summary>
    /// Marks the current phase as complete and optionally advances to the next phase.
    /// </summary>
    public async Task MarkPhaseCompleteAsync(
        string? outputDocumentPath = null,
        CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        var currentPhase = _currentState.CurrentPhase;
        _logger.Information("Marking phase {Phase} as complete", currentPhase);

        // Update completed phases
        var completedPhases = new List<LoopPhase>(_currentState.CompletedPhases);
        if (!completedPhases.Contains(currentPhase))
        {
            completedPhases.Add(currentPhase);
        }

        // Update document paths if provided
        var documents = _currentState.Documents;
        if (!string.IsNullOrEmpty(outputDocumentPath))
        {
            if (!File.Exists(outputDocumentPath))
            {
                _logger.Warning(
                    "Phase {Phase} completed with document path that does not exist on disk: {Path}",
                    currentPhase, outputDocumentPath);
            }

            documents = currentPhase switch
            {
                LoopPhase.Vision => documents with { Vision = outputDocumentPath },
                LoopPhase.Research => documents with { Research = outputDocumentPath },
                LoopPhase.Planning => documents with { Plan = outputDocumentPath },
                _ => documents
            };
        }

        _currentState = _currentState with
        {
            CompletedPhases = completedPhases,
            Documents = documents,
            Status = PhaseStatus.Complete
        };

        await SaveStateAsync(ct);
        await NotifyProgressReporterAsync(r => r.OnPhaseCompletedAsync(currentPhase, outputDocumentPath));
    }

    /// <summary>
    /// Marks the current phase as failed with a reason.
    /// </summary>
    public async Task MarkPhaseFailedAsync(string reason, CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        _logger.Error("Marking phase {Phase} as failed: {Reason}", _currentState.CurrentPhase, reason);

        _currentState = _currentState with
        {
            Status = PhaseStatus.Failed,
            FailureReason = reason
        };

        await SaveStateAsync(ct);
        await NotifyProgressReporterAsync(r => r.OnPhaseFailedAsync(_currentState.CurrentPhase, reason));
    }

    /// <summary>
    /// Marks the run as paused (waiting for human input).
    /// </summary>
    public async Task MarkPausedAsync(CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        _logger.Information("Marking run as paused");

        _currentState = _currentState with
        {
            Status = PhaseStatus.Paused
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Adds a decision to the current state.
    /// </summary>
    public async Task AddDecisionAsync(Decision decision, CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        _logger.Debug("Adding decision for phase {Phase}: {Question}", decision.Phase, decision.Question);

        var decisions = new List<Decision>(_currentState.Decisions) { decision };

        _currentState = _currentState with
        {
            Decisions = decisions
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Updates the last session ID for --resume session continuity.
    /// </summary>
    public async Task UpdateSessionIdAsync(string sessionId, CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        _logger.Debug("Persisting session ID for resume: {SessionId}", sessionId);

        _currentState = _currentState with
        {
            LastSessionId = sessionId
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Advances to the next phase in the loop.
    /// </summary>
    public async Task AdvanceToNextPhaseAsync(CancellationToken ct = default)
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException("No state loaded. Call LoadStateAsync or CreateRunAsync first.");
        }

        var nextPhase = GetNextPhase(_currentState.CurrentPhase);
        if (nextPhase == null)
        {
            _logger.Information("All phases complete");
            _currentState = _currentState with { Status = PhaseStatus.Complete };
        }
        else
        {
            _logger.Information("Advancing from {Current} to {Next}", _currentState.CurrentPhase, nextPhase);
            _currentState = _currentState with
            {
                CurrentPhase = nextPhase.Value,
                Status = PhaseStatus.NotStarted,
                CurrentSubPhase = nextPhase == LoopPhase.Coding ? 1 : null
            };
        }

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Gets the next phase after the given phase.
    /// </summary>
    private static LoopPhase? GetNextPhase(LoopPhase current)
    {
        return current switch
        {
            LoopPhase.Vision => LoopPhase.Research,
            LoopPhase.Research => LoopPhase.Planning,
            LoopPhase.Planning => LoopPhase.Review,
            LoopPhase.Review => LoopPhase.Coding,
            LoopPhase.Coding => null,  // Coding is the final phase
            _ => null
        };
    }

    /// <summary>
    /// Saves the current state to disk.
    /// </summary>
    private async Task SaveStateAsync(CancellationToken ct)
    {
        if (_currentState == null) return;

        // Ensure directory exists
        var directory = Path.GetDirectoryName(_stateFilePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Debug("Created orchestrator directory: {Path}", directory);
        }

        var json = JsonSerializer.Serialize(_currentState, _jsonOptions);
        await File.WriteAllTextAsync(_stateFilePath, json, ct);

        _logger.Debug("State saved to {Path}", _stateFilePath);

        // Generate progress document after each state save
        if (_progressReporter is ProgressReporter fullReporter && _currentState != null)
        {
            try
            {
                await fullReporter.GenerateProgressDocumentAsync(_currentState, ct);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Failed to generate progress document");
            }
        }
    }

    /// <summary>
    /// Safely invokes a progress reporter callback. Exceptions are logged but not propagated.
    /// </summary>
    private async Task NotifyProgressReporterAsync(Func<IProgressReporter, Task> action)
    {
        if (_progressReporter == null) return;

        try
        {
            await action(_progressReporter);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Progress reporter callback failed");
        }
    }
}
