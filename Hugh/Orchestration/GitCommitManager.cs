using System.Diagnostics;
using Hugh.Configuration;
using ILogger = Serilog.ILogger;

namespace Hugh.Orchestration;

/// <summary>
/// Manages git commits during the coding phase pipeline.
/// Creates structured commits after each coding sub-phase on the current branch.
/// </summary>
public class GitCommitManager
{
    private readonly GitConfig _config;
    private readonly ILogger _logger;

    public GitCommitManager(GitConfig config, ILogger logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the current HEAD commit SHA. Used to record starting points for release diffs.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current commit SHA, or null if git is not available or no commits exist.</returns>
    public virtual async Task<string?> GetCurrentCommitShaAsync(CancellationToken ct = default)
    {
        var (success, output) = await RunGitCommandAsync("rev-parse HEAD", ct);
        if (!success)
        {
            _logger.Warning("Failed to get current commit SHA: {Output}", output);
            return null;
        }

        return output.Trim();
    }

    /// <summary>
    /// Checks whether there are uncommitted changes in the working directory.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if there are uncommitted changes, false otherwise.</returns>
    public virtual async Task<bool> HasUncommittedChangesAsync(CancellationToken ct = default)
    {
        var (success, output) = await RunGitCommandAsync("status --porcelain", ct);
        if (!success)
        {
            _logger.Warning("Failed to check git status: {Output}", output);
            return false;
        }

        return !string.IsNullOrWhiteSpace(output);
    }

    /// <summary>
    /// Commits all changes after a coding sub-phase completes.
    /// Stages all changes with <c>git add -A</c>, then creates a commit with a structured message.
    /// Handles "nothing to commit" gracefully (not treated as an error).
    /// </summary>
    /// <param name="phaseNumber">The plan phase number that completed.</param>
    /// <param name="phaseName">The name/description of the phase.</param>
    /// <param name="summary">Brief summary of what was accomplished.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>True if a commit was created, false if nothing to commit or git unavailable.</returns>
    public virtual async Task<bool> CommitPhaseAsync(
        int phaseNumber, string phaseName, string summary, CancellationToken ct = default)
    {
        if (!_config.CommitPerPhase)
        {
            _logger.Debug("Git commit skipped — CommitPerPhase is disabled");
            return false;
        }

        // Check for uncommitted changes first
        var hasChanges = await HasUncommittedChangesAsync(ct);
        if (!hasChanges)
        {
            _logger.Information(
                "No uncommitted changes after Phase {PhaseNumber} — skipping commit",
                phaseNumber);
            return false;
        }

        // Stage all changes
        var (addSuccess, addOutput) = await RunGitCommandAsync("add -A", ct);
        if (!addSuccess)
        {
            _logger.Warning("Failed to stage changes: {Output}", addOutput);
            return false;
        }

        // Build commit message
        var commitMessage = BuildCommitMessage(phaseNumber, phaseName, summary);

        // Create commit — escape the message for the shell
        var escapedMessage = commitMessage.Replace("\"", "\\\"");
        var (commitSuccess, commitOutput) = await RunGitCommandAsync(
            $"commit -m \"{escapedMessage}\"", ct);

        if (!commitSuccess)
        {
            // "nothing to commit" is not an error
            if (commitOutput.Contains("nothing to commit", StringComparison.OrdinalIgnoreCase))
            {
                _logger.Information("Nothing to commit after staging — no changes detected");
                return false;
            }

            _logger.Warning("Failed to create commit: {Output}", commitOutput);
            return false;
        }

        _logger.Information(
            "Created commit for Phase {PhaseNumber}: {PhaseName}", phaseNumber, phaseName);
        return true;
    }

    /// <summary>
    /// Gets a diff summary of changes from a base reference.
    /// When <paramref name="baseRef"/> is provided, shows cumulative diff from that point.
    /// When null, shows diff from the previous commit (HEAD~1).
    /// </summary>
    /// <param name="baseRef">Optional base reference (commit SHA) for cumulative diffs.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The diff summary output (name-status format), or empty string on failure.</returns>
    public virtual async Task<string> GetDiffSummaryAsync(
        string? baseRef = null, CancellationToken ct = default)
    {
        var diffRef = baseRef ?? "HEAD~1";
        var (success, output) = await RunGitCommandAsync(
            $"diff --name-status {diffRef}", ct);

        if (!success)
        {
            _logger.Warning("Failed to get diff summary from {Ref}: {Output}", diffRef, output);
            return string.Empty;
        }

        return output.Trim();
    }

    /// <summary>
    /// Builds a structured commit message using the configured prefix.
    /// Format: [Hugh] Phase {N}: {PhaseName} — {summary}
    /// </summary>
    internal string BuildCommitMessage(int phaseNumber, string phaseName, string summary)
    {
        var prefix = _config.CommitMessagePrefix;
        var truncatedSummary = summary.Length > 100 ? summary[..100] + "..." : summary;
        return $"{prefix} Phase {phaseNumber}: {phaseName} — {truncatedSummary}";
    }

    /// <summary>
    /// Runs a git command and returns the result.
    /// </summary>
    protected virtual async Task<(bool Success, string Output)> RunGitCommandAsync(
        string arguments, CancellationToken ct)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                return (false, "Failed to start git process");
            }

            var outputTask = process.StandardOutput.ReadToEndAsync(ct);
            var errorTask = process.StandardError.ReadToEndAsync(ct);

            await process.WaitForExitAsync(ct);

            var stdout = await outputTask;
            var stderr = await errorTask;
            var combinedOutput = string.IsNullOrEmpty(stderr) ? stdout : $"{stdout}\n{stderr}";

            return (process.ExitCode == 0, combinedOutput);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to run git command: git {Arguments}", arguments);
            return (false, $"Git command execution failed: {ex.Message}");
        }
    }
}
