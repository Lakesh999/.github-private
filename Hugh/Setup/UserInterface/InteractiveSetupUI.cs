using System.Diagnostics;
using Hugh.Setup.Detection;
using Hugh.Setup.Installation;
using Spectre.Console;

namespace Hugh.Setup.UserInterface;

/// <summary>
/// Interactive implementation of <see cref="ISetupUserInterface"/> using Spectre.Console.
/// Provides rich terminal UI with selection prompts, spinners, and colorized output.
/// </summary>
public sealed class InteractiveSetupUI : ISetupUserInterface
{
    /// <inheritdoc />
    public void ShowPrerequisiteReport(IReadOnlyList<PrerequisiteStatus> statuses)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold cyan]Prerequisite Check Results[/]");
        AnsiConsole.WriteLine(new string('─', 40));

        foreach (var status in statuses)
        {
            var (icon, color) = GetStatusDisplay(status.State);
            var stateText = GetStateText(status.State);

            var versionInfo = !string.IsNullOrEmpty(status.Version) ? $" (v{status.Version})" : "";
            AnsiConsole.MarkupLine($"  [{color}]{icon}[/] {Markup.Escape(status.PrerequisiteId)}: [{color}]{stateText}[/]{Markup.Escape(versionInfo)}");

            if (!string.IsNullOrEmpty(status.Message))
            {
                AnsiConsole.MarkupLine($"      [dim]{Markup.Escape(status.Message)}[/]");
            }

            if (status.Hints is { Length: > 0 })
            {
                foreach (var hint in status.Hints)
                {
                    AnsiConsole.MarkupLine($"      [dim]→ {Markup.Escape(hint)}[/]");
                }
            }
        }

        AnsiConsole.WriteLine();
    }

    /// <inheritdoc />
    public Task<FixDecision> PromptToFixAsync(
        PrerequisiteStatus status,
        IReadOnlyList<IInstaller> availableInstallers,
        CancellationToken ct = default)
    {
        var choices = new List<string>();
        var choiceMap = new Dictionary<string, FixDecision>();

        // Add install options for each available installer
        foreach (var installer in availableInstallers)
        {
            if (installer.CanInstall(status.PrerequisiteId))
            {
                var choiceText = $"Install with {installer.InstallerId}";
                choices.Add(choiceText);
                choiceMap[choiceText] = FixDecision.Install;
            }
        }

        // Add show manual instructions option
        var manualChoice = "Show manual instructions";
        choices.Add(manualChoice);
        choiceMap[manualChoice] = FixDecision.Skip; // Will show instructions then skip

        // Add skip and abort options
        var skipChoice = "Skip";
        var abortChoice = "Abort setup";
        choices.Add(skipChoice);
        choices.Add(abortChoice);
        choiceMap[skipChoice] = FixDecision.Skip;
        choiceMap[abortChoice] = FixDecision.Abort;

        var stateDescription = status.State switch
        {
            PrerequisiteState.Missing => "is [red]missing[/]",
            PrerequisiteState.Outdated => "is [yellow]outdated[/]",
            PrerequisiteState.Misconfigured => "is [yellow]misconfigured[/]",
            _ => "needs attention"
        };

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[bold]{Markup.Escape(status.PrerequisiteId)}[/] {stateDescription}. What would you like to do?")
                .PageSize(10)
                .AddChoices(choices));

        // Handle manual instructions specially - show them but return Skip
        if (selection == manualChoice)
        {
            if (status.Hints is { Length: > 0 })
            {
                ShowManualInstructions(status.PrerequisiteId, status.Hints);
            }
            else
            {
                AnsiConsole.MarkupLine("[dim]No manual instructions available for this prerequisite.[/]");
            }
        }

        return Task.FromResult(choiceMap[selection]);
    }

    /// <inheritdoc />
    public void ShowInstallationProgress(string prerequisiteId, string message)
    {
        AnsiConsole.MarkupLine($"  [dim]...[/] {Markup.Escape(prerequisiteId)}: [dim]{Markup.Escape(message)}[/]");
    }

    /// <inheritdoc />
    public void ShowInstallationResult(string prerequisiteId, InstallationResult result)
    {
        if (result.Success)
        {
            var versionInfo = !string.IsNullOrEmpty(result.Version) ? $" (v{result.Version})" : "";
            AnsiConsole.MarkupLine($"[green]✓[/] {Markup.Escape(prerequisiteId)}: [green]Installed successfully[/]{Markup.Escape(versionInfo)}");

            if (result.RequiresRestart)
            {
                AnsiConsole.MarkupLine($"  [yellow]⚠[/] Restart your terminal for PATH changes to take effect");
            }
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]✗[/] {Markup.Escape(prerequisiteId)}: [red]Installation failed[/]");

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                AnsiConsole.MarkupLine($"    [dim]Error: {Markup.Escape(result.ErrorMessage)}[/]");
            }
        }
    }

    /// <inheritdoc />
    public void ShowManualInstructions(string prerequisiteId, string[] instructions)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[bold]Manual Installation Instructions for {Markup.Escape(prerequisiteId)}:[/]");
        AnsiConsole.WriteLine(new string('─', 40));

        for (var i = 0; i < instructions.Length; i++)
        {
            AnsiConsole.MarkupLine($"  [cyan]{i + 1}.[/] {Markup.Escape(instructions[i])}");
        }

        AnsiConsole.WriteLine();
    }

    /// <inheritdoc />
    public Task<AuthDecision> PromptForAuthenticationAsync(CancellationToken ct = default)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold yellow]⚠[/] GitHub authentication is required.");
        AnsiConsole.MarkupLine("[dim]You need to authenticate with GitHub to use Copilot CLI.[/]");
        AnsiConsole.WriteLine();

        var choices = new List<string>
        {
            "Open browser to authenticate",
            "Skip (authenticate later)"
        };

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("How would you like to proceed?")
                .PageSize(5)
                .AddChoices(choices));

        if (selection == choices[0])
        {
            // User chose to open browser
            AnsiConsole.MarkupLine("[dim]Opening browser for GitHub authentication...[/]");

            try
            {
                // Execute gh auth login --web
                var startInfo = new ProcessStartInfo
                {
                    FileName = "gh",
                    Arguments = "auth login --web",
                    UseShellExecute = true
                };
                Process.Start(startInfo);
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]✗[/] Failed to open browser: [dim]{Markup.Escape(ex.Message)}[/]");
                AnsiConsole.MarkupLine("[dim]Please run 'gh auth login --web' manually.[/]");
            }

            return Task.FromResult(AuthDecision.Browser);
        }

        return Task.FromResult(AuthDecision.Skip);
    }

    /// <summary>
    /// Executes an action with a spinner displayed.
    /// </summary>
    /// <param name="statusMessage">The message to display while working.</param>
    /// <param name="action">The async action to execute.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <typeparam name="T">The return type of the action.</typeparam>
    /// <returns>The result of the action.</returns>
    public async Task<T> WithSpinnerAsync<T>(string statusMessage, Func<Task<T>> action, CancellationToken ct = default)
    {
        return await AnsiConsole.Status()
            .Spinner(Spinner.Known.Dots)
            .StartAsync(statusMessage, async _ => await action());
    }

    private static (string Icon, string Color) GetStatusDisplay(PrerequisiteState state) => state switch
    {
        PrerequisiteState.Satisfied => ("✓", "green"),
        PrerequisiteState.Missing => ("✗", "red"),
        PrerequisiteState.Outdated => ("⚠", "yellow"),
        PrerequisiteState.Misconfigured => ("⚠", "yellow"),
        _ => ("?", "dim")
    };

    private static string GetStateText(PrerequisiteState state) => state switch
    {
        PrerequisiteState.Satisfied => "OK",
        PrerequisiteState.Missing => "Missing",
        PrerequisiteState.Outdated => "Outdated",
        PrerequisiteState.Misconfigured => "Misconfigured",
        _ => "Unknown"
    };
}
