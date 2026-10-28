using System.Diagnostics;
using System.Text.RegularExpressions;
using Hugh.Agents;
using Hugh.Configuration;
using Hugh.Models;
using Hugh.Prompts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Hugh.Orchestration;

/// <summary>
/// Executes individual phases of the development loop.
/// Handles agent invocation, document attachment, and result extraction.
/// </summary>
public partial class PhaseExecutor : IAsyncDisposable
{
    private readonly ICopilotService _copilotService;
    private readonly IAgentRegistry _agentRegistry;
    private readonly LoopStateManager _stateManager;
    private readonly HughConfig _config;
    private readonly ILogger<PhaseExecutor> _logger;
    private readonly CancellationTokenSource _globalCts;

    /// <summary>
    /// Initializes a new instance of the PhaseExecutor.
    /// </summary>
    /// <param name="copilotService">The Copilot service for agent execution.</param>
    /// <param name="agentRegistry">The agent registry for looking up agent definitions.</param>
    /// <param name="stateManager">The loop state manager.</param>
    /// <param name="options">the Hugh configuration options.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="globalCts">The global cancellation token source for graceful shutdown.</param>
    public PhaseExecutor(
        ICopilotService copilotService,
        IAgentRegistry agentRegistry,
        LoopStateManager stateManager,
        IOptions<HughConfig> options,
        ILogger<PhaseExecutor> logger,
        CancellationTokenSource globalCts)
    {
        _copilotService = copilotService;
        _agentRegistry = agentRegistry;
        _stateManager = stateManager;
        _config = options.Value;
        _logger = logger;
        _globalCts = globalCts;
    }

    /// <summary>
    /// Executes a phase of the development loop.
    /// </summary>
    /// <param name="phase">The phase to execute.</param>
    /// <param name="prompt">The prompt to send to the agent.</param>
    /// <param name="docs">Paths to documents to attach as context.</param>
    /// <param name="lastSessionId">Optional session ID for resume (attempts session resume before creating new).</param>
    /// <returns>The result of the phase execution.</returns>
    public async Task<PhaseResult> ExecuteAsync(LoopPhase phase, string prompt, string[] docs, string? lastSessionId = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var agentDefinition = _agentRegistry.GetAgent(phase);
        var phaseTimeout = GetPhaseTimeout(phase);

        _logger.LogInformation(
            "Starting phase {Phase} with agent {AgentName} (timeout: {Timeout})",
            phase, agentDefinition.Name, phaseTimeout);

        // Create linked cancellation token for this phase
        using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
        phaseCts.CancelAfter(phaseTimeout);

        try
        {
            // Load built-in agent prompt
            var agentPrompt = BuiltInPromptLoader.Load(agentDefinition.BuiltInPromptName);
            _logger.LogDebug("Loaded built-in prompt '{PromptName}' ({Length} chars)", agentDefinition.BuiltInPromptName, agentPrompt.Length);

            // Prepare document attachments
            var attachments = PrepareDocumentAttachments(docs);
            _logger.LogDebug("Prepared {Count} document attachments", attachments.Count);

            // Execute the phase via Copilot service
            var result = await ExecuteWithTokenAsync(
                agentDefinition,
                agentPrompt,
                prompt,
                attachments,
                phaseCts.Token,
                lastSessionId);

            stopwatch.Stop();

            // Extract output document path from result
            var outputPath = ExtractOutputDocumentPath(result, phase);

            _logger.LogInformation(
                "Phase {Phase} completed successfully in {Duration}",
                phase, stopwatch.Elapsed);

            return new PhaseResult
            {
                Success = result.Success,
                OutputDocumentPath = outputPath ?? result.OutputDocumentPath,
                ErrorMessage = result.ErrorMessage,
                DecisionsMade = result.DecisionsMade,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (_globalCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Phase {Phase} cancelled by user after {Duration}", phase, stopwatch.Elapsed);

            // Mark phase as failed in state
            await _stateManager.MarkPhaseFailedAsync("Cancelled by user");

            throw; // Propagate for graceful shutdown
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Phase {Phase} timed out after {Timeout}", phase, phaseTimeout);

            // Mark phase as failed in state
            await _stateManager.MarkPhaseFailedAsync($"Timeout after {phaseTimeout}");

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Phase timed out after {phaseTimeout}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Phase {Phase} failed with error: {Message}", phase, ex.Message);

            // Mark phase as failed in state
            await _stateManager.MarkPhaseFailedAsync(ex.Message);

            return new PhaseResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Executes a phase interactively with multi-turn Q&amp;A.
    /// The agent asks questions and the user provides answers via console input.
    /// </summary>
    /// <param name="phase">The phase to execute.</param>
    /// <param name="prompt">The prompt to send to the agent.</param>
    /// <param name="docs">Paths to documents to attach as context.</param>
    /// <param name="maxTurns">Maximum Q&amp;A turns.</param>
    /// <returns>The result of the phase execution.</returns>
    public async Task<PhaseResult> ExecuteInteractiveAsync(
        LoopPhase phase, string prompt, string[] docs, int maxTurns)
    {
        var stopwatch = Stopwatch.StartNew();
        var agentDefinition = _agentRegistry.GetAgent(phase);
        var phaseTimeout = GetInteractivePhaseTimeout(phase);

        _logger.LogInformation(
            "Starting interactive phase {Phase} with agent {AgentName} (timeout: {Timeout}, maxTurns: {MaxTurns})",
            phase, agentDefinition.Name, phaseTimeout, maxTurns);

        using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
        phaseCts.CancelAfter(phaseTimeout);

        try
        {
            var agentPrompt = BuiltInPromptLoader.Load(agentDefinition.BuiltInPromptName);
            _logger.LogDebug("Loaded built-in prompt '{PromptName}' ({Length} chars)", agentDefinition.BuiltInPromptName, agentPrompt.Length);

            var attachments = PrepareDocumentAttachments(docs);
            var documentPaths = attachments.Select(a => a.Path);

            var expectedOutputPath = ComputeExpectedOutputPath(phase);

            var result = await _copilotService.ExecuteInteractivePhaseAsync(
                agentDefinition,
                agentPrompt,
                prompt,
                documentPaths,
                expectedOutputPath,
                maxTurns,
                phaseCts.Token);

            stopwatch.Stop();

            var outputPath = ExtractOutputDocumentPath(result, phase);

            _logger.LogInformation(
                "Interactive phase {Phase} completed in {Duration}",
                phase, stopwatch.Elapsed);

            return new PhaseResult
            {
                Success = result.Success,
                OutputDocumentPath = outputPath ?? result.OutputDocumentPath,
                ErrorMessage = result.ErrorMessage,
                DecisionsMade = result.DecisionsMade,
                Duration = stopwatch.Elapsed
            };
        }
        catch (OperationCanceledException) when (_globalCts.IsCancellationRequested)
        {
            stopwatch.Stop();
            _logger.LogWarning("Interactive phase {Phase} cancelled by user after {Duration}", phase, stopwatch.Elapsed);
            await _stateManager.MarkPhaseFailedAsync("Cancelled by user");
            throw;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            _logger.LogWarning("Interactive phase {Phase} timed out after {Timeout}", phase, phaseTimeout);
            await _stateManager.MarkPhaseFailedAsync($"Timeout after {phaseTimeout}");
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Interactive phase timed out after {phaseTimeout}",
                Duration = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "Interactive phase {Phase} failed with error: {Message}", phase, ex.Message);
            await _stateManager.MarkPhaseFailedAsync(ex.Message);
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = ex.Message,
                Duration = stopwatch.Elapsed
            };
        }
    }

    /// <summary>
    /// Gets the configured timeout for a specific phase.
    /// </summary>
    private TimeSpan GetPhaseTimeout(LoopPhase phase)
    {
        return phase switch
        {
            LoopPhase.Vision => _config.Timeouts.VisionPhase,
            LoopPhase.Research => _config.Timeouts.ResearchSubPhasePerPhase,
            LoopPhase.Planning => _config.Timeouts.PlanningPhase,
            LoopPhase.Review => _config.Timeouts.ReviewPhase,
            LoopPhase.Coding => _config.Timeouts.CodingPhasePerPhase,
            _ => TimeSpan.FromMinutes(15) // Default fallback
        };
    }

    /// <summary>
    /// Gets the configured timeout for an interactive phase.
    /// Falls back to standard timeout if no interactive override exists.
    /// </summary>
    private TimeSpan GetInteractivePhaseTimeout(LoopPhase phase)
    {
        return phase switch
        {
            LoopPhase.Vision => _config.Timeouts.InteractiveVisionPhase,
            _ => GetPhaseTimeout(phase)
        };
    }

    /// <summary>
    /// Computes the expected output document path for a phase.
    /// </summary>
    private string ComputeExpectedOutputPath(LoopPhase phase)
    {
        var docsPath = _config.DocsPath;
        var runId = _stateManager.CurrentState?.RunId.ToString()[..8] ?? "unknown";

        return phase switch
        {
            LoopPhase.Vision => Path.Combine(docsPath, "vision", $"{runId}-vision.md"),
            LoopPhase.Research => Path.Combine(docsPath, "research", $"{runId}-research.md"),
            LoopPhase.Planning => Path.Combine(docsPath, "plans", $"{runId}-plan.md"),
            _ => Path.Combine(docsPath, $"{runId}-output.md")
        };
    }

    /// <summary>
    /// Prepares document attachments from file paths.
    /// </summary>
    private List<FileAttachment> PrepareDocumentAttachments(string[] docs)
    {
        var attachments = new List<FileAttachment>();

        foreach (var docPath in docs)
        {
            if (string.IsNullOrWhiteSpace(docPath))
                continue;

            var fullPath = Path.IsPathRooted(docPath)
                ? docPath
                : Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), docPath));

            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("Document not found, skipping: {Path}", fullPath);
                continue;
            }

            attachments.Add(new FileAttachment
            {
                Path = fullPath,
                FileName = Path.GetFileName(fullPath),
                MimeType = GetMimeType(fullPath)
            });
        }

        return attachments;
    }

    /// <summary>
    /// Executes the phase with the provided cancellation token.
    /// </summary>
    private async Task<PhaseResult> ExecuteWithTokenAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,
        string prompt,
        List<FileAttachment> attachments,
        CancellationToken ct,
        string? lastSessionId = null)
    {
        // Pass document paths to the Copilot service
        var documentPaths = attachments.Select(a => a.Path);

        return await _copilotService.ExecutePhaseAsync(
            agentDefinition,
            agentPrompt,
            prompt,
            documentPaths,
            ct,
            lastSessionId);
    }

    /// <summary>
    /// Disposes the CopilotService to release SDK resources.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (_copilotService is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Extracts the output document path from the phase result.
    /// Uses DocumentPathExtractor as the primary method, with pattern-based fallback.
    /// </summary>
    private string? ExtractOutputDocumentPath(PhaseResult result, LoopPhase phase)
    {
        // If result already has an output path that exists on disk, use it
        if (!string.IsNullOrEmpty(result.OutputDocumentPath) && File.Exists(result.OutputDocumentPath))
            return result.OutputDocumentPath;

        // Fallback: compute expected path and check if file exists on disk
        var docsPath = _config.DocsPath;
        var runId = _stateManager.CurrentState?.RunId.ToString()[..8] ?? "unknown";

        var expectedPath = phase switch
        {
            LoopPhase.Vision => Path.Combine(docsPath, "vision", $"{runId}-vision.md"),
            LoopPhase.Research => Path.Combine(docsPath, "research", $"{runId}-research.md"),
            LoopPhase.Planning => Path.Combine(docsPath, "plans", $"{runId}-plan.md"),
            LoopPhase.Review => null,
            LoopPhase.Coding => null,
            _ => null
        };

        // If the result had a path but it didn't exist on disk, try the expected path
        if (!string.IsNullOrEmpty(result.OutputDocumentPath) && !File.Exists(result.OutputDocumentPath))
        {
            _logger.LogWarning(
                "Result output path does not exist on disk: {Path}. Trying expected path.",
                result.OutputDocumentPath);
        }

        // Check if expected path exists
        if (!string.IsNullOrEmpty(expectedPath) && File.Exists(expectedPath))
        {
            _logger.LogDebug("Using expected output path: {Path}", expectedPath);
            return expectedPath;
        }

        // Last resort: scan the expected directory for recently created .md files
        if (expectedPath != null)
        {
            var dir = Path.GetDirectoryName(expectedPath);
            if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
            {
                var recentFile = Directory.EnumerateFiles(dir, "*.md")
                    .Select(f => new FileInfo(f))
                    .Where(f => f.CreationTimeUtc > DateTime.UtcNow.AddMinutes(-30))
                    .OrderByDescending(f => f.CreationTimeUtc)
                    .FirstOrDefault();

                if (recentFile != null)
                {
                    _logger.LogInformation(
                        "Found recently created document in expected directory: {Path}",
                        recentFile.FullName);
                    return recentFile.FullName;
                }
            }
        }

        // Return result path even if not verified (may be created asynchronously)
        return result.OutputDocumentPath ?? expectedPath;
    }

    /// <summary>
    /// Gets the MIME type for a file based on its extension.
    /// </summary>
    private static string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".json" => "application/json",
            ".yaml" or ".yml" => "text/yaml",
            ".cs" => "text/x-csharp",
            ".ts" or ".tsx" => "text/typescript",
            ".js" or ".jsx" => "text/javascript",
            ".py" => "text/x-python",
            ".html" => "text/html",
            ".css" => "text/css",
            ".xml" => "application/xml",
            _ => "text/plain"
        };
    }

    /// <summary>
    /// Regex for extracting document paths from agent responses.
    /// </summary>
    [GeneratedRegex(@"(?:created|saved|wrote|generated)\s+(?:to|at)?:?\s*[`""']?([^\s`""']+\.md)[`""']?", RegexOptions.IgnoreCase)]
    private static partial Regex DocumentPathRegex();
}

/// <summary>
/// Represents a file attachment for document context.
/// </summary>
public record FileAttachment
{
    /// <summary>
    /// The full path to the file.
    /// </summary>
    public string Path { get; init; } = "";

    /// <summary>
    /// The file name without path.
    /// </summary>
    public string FileName { get; init; } = "";

    /// <summary>
    /// The MIME type of the file.
    /// </summary>
    public string MimeType { get; init; } = "text/plain";
}
