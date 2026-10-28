namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether the .NET SDK is installed.
/// Runs 'dotnet --version' to verify installation.
/// The .NET SDK is needed for Hugh's post-coding verification (build and test commands).
/// </summary>
public class DotNetSdkDetector : IPrerequisiteDetector
{
    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DotNetSdkDetector"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner for executing CLI commands.</param>
    public DotNetSdkDetector(IProcessRunner processRunner)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "dotnet-sdk";

    /// <inheritdoc/>
    public string DisplayName => ".NET SDK";

    /// <inheritdoc/>
    public int Priority => 35;

    /// <inheritdoc/>
    public bool IsInstallable => true;

    /// <inheritdoc/>
    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        try
        {
            var result = await _processRunner.RunAsync("dotnet", "--version", ct);

            if (result.ExitCode == 0)
            {
                var version = result.StandardOutput.Trim();
                return new PrerequisiteStatus
                {
                    PrerequisiteId = PrerequisiteId,
                    State = PrerequisiteState.Satisfied,
                    Version = version,
                    Message = $".NET SDK {version} is installed"
                };
            }

            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = ".NET SDK is not installed or not working correctly",
                Hints = new[]
                {
                    "Install .NET SDK: winget install Microsoft.DotNet.SDK.8",
                    "Or download from: https://dotnet.microsoft.com/download"
                }
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = ".NET SDK executable not found",
                Hints = new[]
                {
                    "Install .NET SDK: winget install Microsoft.DotNet.SDK.8",
                    "Or download from: https://dotnet.microsoft.com/download"
                }
            };
        }
    }
}
