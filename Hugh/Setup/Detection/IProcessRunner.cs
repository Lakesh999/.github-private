namespace Hugh.Setup.Detection;

/// <summary>
/// Abstraction for running external processes.
/// Enables unit testing by allowing mock implementations.
/// </summary>
public interface IProcessRunner
{
    /// <summary>
    /// Runs a process and returns the result including stdout, stderr, and exit code.
    /// </summary>
    /// <param name="fileName">The name or path of the executable to run.</param>
    /// <param name="arguments">The command-line arguments to pass.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>The result of the process execution.</returns>
    Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct = default);
}

/// <summary>
/// Represents the result of a process execution.
/// </summary>
public record ProcessResult
{
    /// <summary>
    /// Gets the exit code of the process.
    /// </summary>
    public required int ExitCode { get; init; }

    /// <summary>
    /// Gets the contents of standard output.
    /// </summary>
    public required string StandardOutput { get; init; }

    /// <summary>
    /// Gets the contents of standard error.
    /// </summary>
    public required string StandardError { get; init; }
}
