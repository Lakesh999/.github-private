namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether the workspace has write access by creating a test directory.
/// Creates and tests the .orchestrator/ directory for Hugh's operational files.
/// </summary>
public class WorkspaceDetector : IPrerequisiteDetector
{
    private readonly string _workspacePath;

    /// <summary>
    /// The name of the orchestrator directory used for Hugh's operational files.
    /// </summary>
    private const string OrchestratorDirName = ".orchestrator";

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceDetector"/> class.
    /// </summary>
    /// <param name="workspacePath">The root path of the workspace to check.</param>
    public WorkspaceDetector(string workspacePath)
    {
        _workspacePath = workspacePath ?? throw new ArgumentNullException(nameof(workspacePath));
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "workspace";

    /// <inheritdoc/>
    public string DisplayName => "Workspace Write Access";

    /// <inheritdoc/>
    public int Priority => 20;

    /// <inheritdoc/>
    public bool IsInstallable => false;

    /// <inheritdoc/>
    public Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        // First check if the workspace path exists
        if (!Directory.Exists(_workspacePath))
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "Workspace directory does not exist",
                Hints = new[]
                {
                    $"Expected workspace at: {_workspacePath}",
                    "Ensure you are running from a valid workspace directory"
                }
            });
        }

        var orchestratorPath = Path.Combine(_workspacePath, OrchestratorDirName);
        var testFilePath = Path.Combine(orchestratorPath, ".write-test");

        try
        {
            // Create the .orchestrator directory if it doesn't exist
            if (!Directory.Exists(orchestratorPath))
            {
                Directory.CreateDirectory(orchestratorPath);
            }

            // Test write access by creating and deleting a test file
            File.WriteAllText(testFilePath, "write-test");
            File.Delete(testFilePath);

            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Satisfied,
                Message = "Workspace has write access",
                Version = _workspacePath
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Misconfigured,
                Message = "Workspace does not have write access",
                Hints = new[]
                {
                    "Check file system permissions for the workspace directory",
                    $"Ensure the user has write access to: {_workspacePath}"
                }
            });
        }
        catch (IOException ex)
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Misconfigured,
                Message = $"Failed to write to workspace: {ex.Message}",
                Hints = new[]
                {
                    "Check if the disk is full or read-only",
                    "Ensure no other process is locking the directory"
                }
            });
        }
        catch (Exception ex)
        {
            return Task.FromResult(new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Misconfigured,
                Message = $"Unexpected error checking workspace: {ex.Message}",
                Hints = new[]
                {
                    "Check file system permissions",
                    $"Workspace path: {_workspacePath}"
                }
            });
        }
    }
}
