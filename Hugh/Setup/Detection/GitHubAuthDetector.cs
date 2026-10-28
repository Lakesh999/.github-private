using GitHub.Copilot.SDK;

namespace Hugh.Setup.Detection;

/// <summary>
/// Detects whether GitHub authentication is configured.
/// Checks for GH_TOKEN/GITHUB_TOKEN environment variables first,
/// then attempts SDK-based auth verification via CopilotClient.GetAuthStatusAsync(),
/// and falls back to 'gh auth status' shell command if the SDK is unavailable.
/// </summary>
public class GitHubAuthDetector : IPrerequisiteDetector
{
    private readonly IProcessRunner _processRunner;
    private readonly Func<CopilotClient>? _clientFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubAuthDetector"/> class.
    /// </summary>
    /// <param name="processRunner">The process runner for executing CLI commands (fallback).</param>
    /// <param name="clientFactory">Optional factory to create a CopilotClient for SDK-based auth check.</param>
    public GitHubAuthDetector(IProcessRunner processRunner, Func<CopilotClient>? clientFactory = null)
    {
        _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        _clientFactory = clientFactory;
    }

    /// <inheritdoc/>
    public string PrerequisiteId => "github-auth";

    /// <inheritdoc/>
    public string DisplayName => "GitHub Authentication";

    /// <inheritdoc/>
    public int Priority => 50;

    /// <inheritdoc/>
    public bool IsInstallable => true;

    /// <inheritdoc/>
    public async Task<PrerequisiteStatus> DetectAsync(CancellationToken ct = default)
    {
        // First, check for environment variable tokens
        var ghToken = Environment.GetEnvironmentVariable("GH_TOKEN");
        var githubToken = Environment.GetEnvironmentVariable("GITHUB_TOKEN");

        if (!string.IsNullOrWhiteSpace(ghToken) || !string.IsNullOrWhiteSpace(githubToken))
        {
            var tokenSource = !string.IsNullOrWhiteSpace(ghToken) ? "GH_TOKEN" : "GITHUB_TOKEN";
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Satisfied,
                Message = $"Authenticated via {tokenSource} environment variable"
            };
        }

        // Try SDK-based auth check if client factory is available
        if (_clientFactory != null)
        {
            try
            {
                var client = _clientFactory();
                await client.StartAsync();
                try
                {
                    var authStatus = await client.GetAuthStatusAsync(ct);
                    if (authStatus.IsAuthenticated)
                    {
                        return new PrerequisiteStatus
                        {
                            PrerequisiteId = PrerequisiteId,
                            State = PrerequisiteState.Satisfied,
                            Message = !string.IsNullOrEmpty(authStatus.Login)
                                ? $"Authenticated to GitHub as {authStatus.Login} (via SDK)"
                                : "Authenticated to GitHub (via SDK)"
                        };
                    }

                    return new PrerequisiteStatus
                    {
                        PrerequisiteId = PrerequisiteId,
                        State = PrerequisiteState.Misconfigured,
                        Message = authStatus.StatusMessage ?? "GitHub authentication not configured",
                        Hints = new[]
                        {
                            "Run 'gh auth login' to authenticate with GitHub",
                            "Or set the GH_TOKEN environment variable with a personal access token"
                        }
                    };
                }
                finally
                {
                    await client.StopAsync();
                }
            }
            catch
            {
                // SDK client failed to start — fall through to shell-based check
            }
        }

        // Fall back to gh auth status (shell-based)
        try
        {
            var result = await _processRunner.RunAsync("gh", "auth status", ct);

            if (result.ExitCode == 0)
            {
                var username = ParseUsername(result.StandardOutput + result.StandardError);
                return new PrerequisiteStatus
                {
                    PrerequisiteId = PrerequisiteId,
                    State = PrerequisiteState.Satisfied,
                    Message = username != null 
                        ? $"Authenticated to GitHub as {username}" 
                        : "Authenticated to GitHub"
                };
            }

            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Misconfigured,
                Message = "GitHub CLI is not authenticated",
                Hints = new[]
                {
                    "Run 'gh auth login' to authenticate with GitHub",
                    "Or set the GH_TOKEN environment variable with a personal access token"
                }
            };
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or FileNotFoundException)
        {
            return new PrerequisiteStatus
            {
                PrerequisiteId = PrerequisiteId,
                State = PrerequisiteState.Missing,
                Message = "Cannot check authentication: GitHub CLI is not installed",
                Hints = new[]
                {
                    "Install GitHub CLI first, then authenticate with 'gh auth login'",
                    "Or set the GH_TOKEN environment variable with a personal access token"
                }
            };
        }
    }

    private static string? ParseUsername(string output)
    {
        // gh auth status output includes "Logged in to github.com as USERNAME"
        // or "Logged in to github.com account USERNAME"
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var line in lines)
        {
            // Look for "as <username>" pattern
            var asIndex = line.IndexOf(" as ", StringComparison.OrdinalIgnoreCase);
            if (asIndex >= 0)
            {
                var afterAs = line.Substring(asIndex + 4).Trim();
                var username = afterAs.Split(' ', '(', '\r')[0].Trim();
                if (!string.IsNullOrEmpty(username))
                {
                    return username;
                }
            }

            // Look for "account <username>" pattern
            var accountIndex = line.IndexOf("account ", StringComparison.OrdinalIgnoreCase);
            if (accountIndex >= 0)
            {
                var afterAccount = line.Substring(accountIndex + 8).Trim();
                var username = afterAccount.Split(' ', '(', '\r')[0].Trim();
                if (!string.IsNullOrEmpty(username))
                {
                    return username;
                }
            }
        }

        return null;
    }
}
