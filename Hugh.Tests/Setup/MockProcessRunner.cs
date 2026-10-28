using Hugh.Setup.Detection;

namespace Hugh.Tests.Setup;

/// <summary>
/// Mock implementation of IProcessRunner for unit testing.
/// Allows configuring expected results for specific command invocations.
/// </summary>
public class MockProcessRunner : IProcessRunner
{
    private readonly Dictionary<(string, string), ProcessResult> _results = new();
    private readonly Dictionary<(string, string), Exception> _exceptions = new();
    private readonly List<(string FileName, string Arguments)> _invocations = new();

    /// <summary>
    /// Gets the list of invocations made to this mock.
    /// </summary>
    public IReadOnlyList<(string FileName, string Arguments)> Invocations => _invocations.AsReadOnly();

    /// <summary>
    /// Sets up a successful result for a specific command invocation.
    /// </summary>
    public MockProcessRunner SetupResult(string fileName, string arguments, ProcessResult result)
    {
        _results[(fileName, arguments)] = result;
        return this;
    }

    /// <summary>
    /// Sets up an exception to be thrown for a specific command invocation.
    /// </summary>
    public MockProcessRunner SetupException(string fileName, string arguments, Exception exception)
    {
        _exceptions[(fileName, arguments)] = exception;
        return this;
    }

    /// <summary>
    /// Sets up a result indicating the command was not found (process start failure).
    /// </summary>
    public MockProcessRunner SetupNotFound(string fileName, string arguments)
    {
        _exceptions[(fileName, arguments)] = new System.ComponentModel.Win32Exception(2, "The system cannot find the file specified");
        return this;
    }

    /// <summary>
    /// Sets up a successful command result with the given output.
    /// </summary>
    public MockProcessRunner SetupSuccess(string fileName, string arguments, string stdout, string? stderr = null)
    {
        return SetupResult(fileName, arguments, new ProcessResult
        {
            ExitCode = 0,
            StandardOutput = stdout,
            StandardError = stderr ?? ""
        });
    }

    /// <summary>
    /// Sets up a failed command result with the given exit code and error message.
    /// </summary>
    public MockProcessRunner SetupFailure(string fileName, string arguments, int exitCode = 1, string? stderr = null)
    {
        return SetupResult(fileName, arguments, new ProcessResult
        {
            ExitCode = exitCode,
            StandardOutput = "",
            StandardError = stderr ?? ""
        });
    }

    /// <inheritdoc/>
    public Task<ProcessResult> RunAsync(string fileName, string arguments, CancellationToken ct = default)
    {
        _invocations.Add((fileName, arguments));

        // Check for configured exception
        if (_exceptions.TryGetValue((fileName, arguments), out var exception))
        {
            throw exception;
        }

        // Check for configured result
        if (_results.TryGetValue((fileName, arguments), out var result))
        {
            return Task.FromResult(result);
        }

        // Default: command not found
        throw new System.ComponentModel.Win32Exception(2, $"No mock configured for: {fileName} {arguments}");
    }
}
