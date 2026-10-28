using System.Text.Json;
using System.Text.Json.Serialization;
using Hugh.Models;
using Serilog;

namespace Hugh.Orchestration;

/// <summary>
/// Manages the persistent state of a multi-release solution run.
/// State is persisted to .orchestrator/solution-state.json.
/// Per-release loop states are delegated to LoopStateManager instances
/// under .orchestrator/releases/release-N/.
/// </summary>
public class SolutionStateManager
{
    private const string OrchestratorFolder = ".orchestrator";
    private const string SolutionStateFileName = "solution-state.json";
    private const string ReleasesFolder = "releases";

    private readonly string _workspaceRoot;
    private readonly string _stateFilePath;
    private readonly ILogger _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    private SolutionState? _currentState;

    public SolutionStateManager(string workspaceRoot, ILogger? logger = null)
    {
        _workspaceRoot = workspaceRoot;
        _stateFilePath = Path.Combine(workspaceRoot, OrchestratorFolder, SolutionStateFileName);
        _logger = logger ?? Log.Logger;

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
        };
    }

    /// <summary>
    /// Gets the current solution state, if loaded.
    /// </summary>
    public SolutionState? CurrentState => _currentState;

    /// <summary>
    /// Creates a new solution run with the given user request.
    /// </summary>
    public async Task<SolutionState> CreateSolutionAsync(string originalRequest, CancellationToken ct = default)
    {
        _logger.Information("Creating new solution for request: {Request}", originalRequest);

        _currentState = new SolutionState
        {
            SolutionId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            OriginalRequest = originalRequest,
            Mode = SolutionMode.MultiRelease,
            CurrentPhase = SolutionPhase.Triage,
            Documents = new SolutionDocumentPaths(),
            Releases = new List<ReleaseDefinition>(),
            CurrentReleaseIndex = 0,
            Status = PhaseStatus.NotStarted,
            Decisions = new List<Decision>()
        };

        await SaveStateAsync(ct);

        _logger.Information("Created solution {SolutionId}", _currentState.SolutionId);
        return _currentState;
    }

    /// <summary>
    /// Loads the current solution state from disk.
    /// </summary>
    public async Task<SolutionState?> LoadSolutionAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_stateFilePath))
        {
            _logger.Debug("No existing solution state file found at {Path}", _stateFilePath);
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_stateFilePath, ct);
            _currentState = JsonSerializer.Deserialize<SolutionState>(json, _jsonOptions);
            _logger.Information("Loaded solution state for {SolutionId}", _currentState?.SolutionId);
            return _currentState;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to load solution state from {Path}", _stateFilePath);
            throw;
        }
    }

    /// <summary>
    /// Loads solution state for a specific solution ID.
    /// </summary>
    public async Task<SolutionState?> LoadSolutionAsync(Guid solutionId, CancellationToken ct = default)
    {
        var state = await LoadSolutionAsync(ct);
        if (state?.SolutionId == solutionId)
        {
            return state;
        }

        _logger.Warning("Requested solution {RequestedId} but found {FoundId}", solutionId, state?.SolutionId);
        return null;
    }

    /// <summary>
    /// Updates the current solution phase and status.
    /// </summary>
    public async Task UpdatePhaseAsync(
        SolutionPhase phase,
        PhaseStatus status,
        CancellationToken ct = default)
    {
        EnsureStateLoaded();

        _logger.Information("Updating solution phase to {Phase} with status {Status}", phase, status);

        _currentState = _currentState! with
        {
            CurrentPhase = phase,
            Status = status
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Sets the solution mode after triage.
    /// </summary>
    public async Task SetModeAsync(SolutionMode mode, CancellationToken ct = default)
    {
        EnsureStateLoaded();

        _logger.Information("Setting solution mode to {Mode}", mode);

        _currentState = _currentState! with { Mode = mode };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Updates the solution document paths.
    /// </summary>
    public async Task UpdateDocumentsAsync(SolutionDocumentPaths documents, CancellationToken ct = default)
    {
        EnsureStateLoaded();

        _currentState = _currentState! with { Documents = documents };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Populates the releases list after decomposition.
    /// </summary>
    public async Task SetReleasesAsync(List<ReleaseDefinition> releases, CancellationToken ct = default)
    {
        EnsureStateLoaded();

        _logger.Information("Setting {Count} releases", releases.Count);

        _currentState = _currentState! with { Releases = releases };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Updates the current release index and the status of that release.
    /// </summary>
    public async Task UpdateReleaseStatusAsync(
        int releaseIndex,
        ReleaseStatus status,
        CancellationToken ct = default)
    {
        EnsureStateLoaded();

        var releases = new List<ReleaseDefinition>(_currentState!.Releases);
        if (releaseIndex < 0 || releaseIndex >= releases.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseIndex),
                $"Release index {releaseIndex} is out of range (0-{releases.Count - 1})");
        }

        releases[releaseIndex] = releases[releaseIndex] with { Status = status };

        _logger.Information(
            "Updated release {Index} ({Name}) to {Status}",
            releaseIndex, releases[releaseIndex].Name, status);

        _currentState = _currentState with
        {
            Releases = releases,
            CurrentReleaseIndex = releaseIndex
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Marks the solution as complete.
    /// </summary>
    public async Task MarkCompleteAsync(CancellationToken ct = default)
    {
        EnsureStateLoaded();

        _logger.Information("Marking solution {SolutionId} as complete", _currentState!.SolutionId);

        _currentState = _currentState with
        {
            CurrentPhase = SolutionPhase.Complete,
            Status = PhaseStatus.Complete
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Marks the solution as failed with a reason.
    /// </summary>
    public async Task MarkFailedAsync(string reason, CancellationToken ct = default)
    {
        EnsureStateLoaded();

        _logger.Error("Marking solution {SolutionId} as failed: {Reason}", _currentState!.SolutionId, reason);

        _currentState = _currentState with
        {
            Status = PhaseStatus.Failed,
            FailureReason = reason
        };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Adds a decision to the solution state.
    /// </summary>
    public async Task AddDecisionAsync(Decision decision, CancellationToken ct = default)
    {
        EnsureStateLoaded();

        var decisions = new List<Decision>(_currentState!.Decisions) { decision };

        _currentState = _currentState with { Decisions = decisions };

        await SaveStateAsync(ct);
    }

    /// <summary>
    /// Gets the state directory path for a specific release.
    /// Creates the directory if it doesn't exist.
    /// </summary>
    public string GetReleaseStateDirectory(int releaseNumber)
    {
        var dir = Path.Combine(
            _workspaceRoot,
            OrchestratorFolder,
            ReleasesFolder,
            $"release-{releaseNumber}");

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
            _logger.Debug("Created release state directory: {Path}", dir);
        }

        return dir;
    }

    /// <summary>
    /// Creates a LoopStateManager instance for a specific release.
    /// </summary>
    public LoopStateManager CreateReleaseStateManager(int releaseNumber, IProgressReporter? progressReporter = null)
    {
        var stateDir = Path.Combine(
            OrchestratorFolder,
            ReleasesFolder,
            $"release-{releaseNumber}");

        return new LoopStateManager(_workspaceRoot, _logger, stateDir, progressReporter);
    }

    /// <summary>
    /// Checks whether a solution state file exists on disk.
    /// </summary>
    public bool HasExistingSolution()
    {
        return File.Exists(_stateFilePath);
    }

    private void EnsureStateLoaded()
    {
        if (_currentState == null)
        {
            throw new InvalidOperationException(
                "No solution state loaded. Call LoadSolutionAsync or CreateSolutionAsync first.");
        }
    }

    private async Task SaveStateAsync(CancellationToken ct)
    {
        if (_currentState == null) return;

        var directory = Path.GetDirectoryName(_stateFilePath)!;
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            _logger.Debug("Created orchestrator directory: {Path}", directory);
        }

        var json = JsonSerializer.Serialize(_currentState, _jsonOptions);
        await File.WriteAllTextAsync(_stateFilePath, json, ct);

        _logger.Debug("Solution state saved to {Path}", _stateFilePath);
    }
}
