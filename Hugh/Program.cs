using System.CommandLine;
using Hugh.Agents;
using Hugh.Agents.Discovery;
using Hugh.Configuration;
using Hugh.Detection;
using Hugh.Logging;
using Hugh.Orchestration;
using Hugh.Setup;
using Hugh.Setup.Detection;
using Hugh.Setup.Installation;
using Hugh.Setup.UserInterface;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Serilog;

namespace Hugh;

/// <summary>
/// Entry point for the Hugh self-operating development loop.
/// Hugh operates autonomously — freed from the need for constant human interaction.
/// </summary>
public class Program
{
    private static LoopOrchestrator? _orchestrator;
    private static SolutionOrchestrator? _solutionOrchestrator;
    private static CancellationTokenSource _globalCts = new();

    public static async Task<int> Main(string[] args)
    {
        // Resolve installation path for configuration and agents
        var installPath = PathResolver.GetInstallationPath();

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .SetBasePath(installPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        // Ensure log directory exists before Serilog initialization
        Directory.CreateDirectory(PathResolver.GetLogPath());

        // Configure Serilog - write logs to user's home directory (~/.hugh/logs)
        var logPath = Path.Combine(PathResolver.GetLogPath(), "hugh-.log");
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .WriteTo.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .Enrich.FromLogContext()
            .CreateLogger();

        // Set up graceful shutdown
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            Log.Warning("Shutdown requested. Completing current phase...");
            _globalCts.Cancel();
        };

        try
        {
            // Display header
            ConsoleOutput.WriteHeader();

            Log.Information("Hugh starting...");

            // Build configuration and orchestrator
            var HughConfig = new HughConfig();
            configuration.GetSection("Hugh").Bind(HughConfig);

            // Build orchestrator with manual dependency wiring
            _orchestrator = BuildOrchestrator(configuration);

            // Define CLI options
            var featureArgument = new Argument<string?>(
                name: "feature",
                description: "The feature request to implement",
                getDefaultValue: () => null);

            var resumeOption = new Option<string?>(
                aliases: ["--resume", "-r"],
                description: "Resume a paused or failed run. Optionally specify a runId.");

            var statusOption = new Option<bool>(
                aliases: ["--status", "-s"],
                description: "Show current run state");

            var checkOption = new Option<bool>(
                aliases: ["--check", "-c"],
                description: "Check prerequisites without running (alias for 'doctor' command)");

            var verboseOption = new Option<bool>(
                aliases: ["--verbose", "-v"],
                description: "Enable debug logging");

            var timeoutOption = new Option<string?>(
                aliases: ["--timeout", "-t"],
                description: "Override default timeout (format: HH:mm:ss)");

            // Build doctor command
            var fixOption = new Option<bool>(
                aliases: ["--fix", "-f"],
                description: "Attempt to install missing prerequisites");

            var noInteractiveOption = new Option<bool>(
                aliases: ["--no-interactive", "-n"],
                description: "Disable interactive prompts (CI/CD mode)");

            var doctorCommand = new Command("doctor", "Check and optionally fix Hugh prerequisites")
            {
                fixOption,
                noInteractiveOption
            };

            doctorCommand.SetHandler(async (fix, noInteractive) =>
            {
                var options = new SetupOptions
                {
                    Fix = fix,
                    NonInteractive = noInteractive || Console.IsInputRedirected,
                    AgentsPath = HughConfig.GetResolvedAgentsPath(),
                    WorkspacePath = Directory.GetCurrentDirectory()
                };

                var setupOrchestrator = BuildSetupOrchestrator(options, HughConfig.AgentDiscovery);
                var result = await setupOrchestrator.RunAsync(options, _globalCts.Token);
                Environment.ExitCode = result.AllSatisfied ? 0 : 1;
            }, fixOption, noInteractiveOption);

            // Build root command
            var rootCommand = new RootCommand("Hugh - Self-Operating Development Loop")
            {
                featureArgument,
                resumeOption,
                statusOption,
                checkOption,
                verboseOption,
                timeoutOption,
                noInteractiveOption
            };

            rootCommand.AddCommand(doctorCommand);

            rootCommand.SetHandler(async (feature, resume, status, check, verbose, timeout, noInteractive) =>
            {
                // Apply --no-interactive flag: disable interactive phases
                if (noInteractive)
                {
                    HughConfig.Autonomous.InteractivePhases.Clear();
                }

                if (verbose)
                {
                    Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Debug()
                        .WriteTo.Console(
                            outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .WriteTo.File(
                            path: Path.Combine(PathResolver.GetLogPath(), "hugh-.log"),
                            rollingInterval: RollingInterval.Day,
                            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
                        .Enrich.FromLogContext()
                        .CreateLogger();

                    Log.Debug("Verbose logging enabled");
                }

                // Handle --check command (alias for 'doctor' command)
                if (check)
                {
                    var options = new SetupOptions
                    {
                        Fix = false,
                        NonInteractive = noInteractive || Console.IsInputRedirected,
                        AgentsPath = HughConfig.GetResolvedAgentsPath(),
                        WorkspacePath = Directory.GetCurrentDirectory()
                    };

                    var setupOrchestrator = BuildSetupOrchestrator(options, HughConfig.AgentDiscovery);
                    var result = await setupOrchestrator.RunAsync(options, _globalCts.Token);
                    Environment.ExitCode = result.AllSatisfied ? 0 : 1;
                    return;
                }

                if (status)
                {
                    await HandleStatusCommand();
                }
                else if (!string.IsNullOrEmpty(resume))
                {
                    await HandleResumeCommand(resume, timeout, noInteractive, HughConfig, configuration);
                }
                else if (resume != null)
                {
                    // --resume with no value: resume most recent
                    await HandleResumeCommand(null, timeout, noInteractive, HughConfig, configuration);
                }
                else if (!string.IsNullOrEmpty(feature))
                {
                    await HandleNewFeatureCommand(feature, timeout, noInteractive, HughConfig, configuration);
                }
                else
                {
                    ConsoleOutput.WriteInfo("No command specified. Use --help for usage information.");
                    ConsoleOutput.WriteHint("Example: hugh \"Add user authentication feature\"");
                    ConsoleOutput.WriteHint("Example: hugh --status");
                    ConsoleOutput.WriteHint("Example: hugh --resume");
                }
            }, featureArgument, resumeOption, statusOption, checkOption, verboseOption, timeoutOption, noInteractiveOption);

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Hugh terminated unexpectedly");
            return 1;
        }
        finally
        {
            if (_solutionOrchestrator != null)
            {
                await _solutionOrchestrator.DisposeAsync();
            }
            else if (_orchestrator != null)
            {
                await _orchestrator.DisposeAsync();
            }
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task HandleStatusCommand()
    {
        Log.Debug("Checking run status...");

        var state = await _orchestrator!.GetStatusAsync(ct: _globalCts.Token);

        if (state == null)
        {
            ConsoleOutput.WriteInfo("No active runs found.");
            ConsoleOutput.WriteHint("Start a new run with: hugh \"Your feature request\"");
            return;
        }

        ConsoleOutput.WriteRunStatus(state);
    }

    private static async Task HandleResumeCommand(string? runId, string? timeout, bool noInteractive, HughConfig config, IConfiguration configuration)
    {
        // Validate prerequisites first using the new orchestrator
        var options = new SetupOptions
        {
            Fix = false,
            NonInteractive = noInteractive || Console.IsInputRedirected,
            AgentsPath = config.GetResolvedAgentsPath(),
            WorkspacePath = Directory.GetCurrentDirectory()
        };

        var setupOrchestrator = BuildSetupOrchestrator(options, config.AgentDiscovery);
        var prereqResult = await setupOrchestrator.RunAsync(options, _globalCts.Token);
        if (!prereqResult.AllSatisfied)
        {
            Environment.ExitCode = 1;
            return;
        }

        // Discover and sync agent prompts from GitHub
        await DiscoverAgentsAsync(config);

        // Rebuild orchestrators with discovered agents path
        _orchestrator = BuildOrchestrator(configuration, config);
        _solutionOrchestrator = BuildSolutionOrchestrator(config, _orchestrator);

        Guid? runGuid = null;
        if (!string.IsNullOrEmpty(runId) && Guid.TryParse(runId, out var parsedGuid))
        {
            runGuid = parsedGuid;
            ConsoleOutput.WriteInfo($"Resuming run: {runId}");
        }
        else
        {
            ConsoleOutput.WriteInfo("Resuming most recent run...");
        }

        if (!string.IsNullOrEmpty(timeout))
        {
            Log.Information("Using timeout override: {Timeout}", timeout);
            var parsedTimeout = ParseTimeoutOverride(timeout);
            if (parsedTimeout.HasValue)
            {
                ApplyTimeoutOverride(config, parsedTimeout.Value);
            }
        }

        // Check if this is a solution-level resume or a loop-level resume
        bool success;
        if (_solutionOrchestrator.HasExistingSolution())
        {
            ConsoleOutput.WriteInfo("Resuming multi-release solution...");
            success = await _solutionOrchestrator.ResumeAsync(runGuid, _globalCts.Token);
        }
        else
        {
            success = await _orchestrator!.ResumeAsync(runGuid, _globalCts.Token);
        }

        if (success)
        {
            ConsoleOutput.WriteSuccess("Run resumed and completed successfully.");
        }
        else
        {
            ConsoleOutput.WriteError("Run failed or was cancelled.");
        }
    }

    private static async Task HandleNewFeatureCommand(string feature, string? timeout, bool noInteractive, HughConfig config, IConfiguration configuration)
    {
        // Validate prerequisites first using the new orchestrator
        var options = new SetupOptions
        {
            Fix = false,
            NonInteractive = noInteractive || Console.IsInputRedirected,
            AgentsPath = config.GetResolvedAgentsPath(),
            WorkspacePath = Directory.GetCurrentDirectory()
        };

        var setupOrchestrator = BuildSetupOrchestrator(options, config.AgentDiscovery);
        var prereqResult = await setupOrchestrator.RunAsync(options, _globalCts.Token);
        if (!prereqResult.AllSatisfied)
        {
            Environment.ExitCode = 1;
            return;
        }

        // Discover and sync agent prompts from GitHub
        await DiscoverAgentsAsync(config);

        // Rebuild orchestrators with discovered agents path
        _orchestrator = BuildOrchestrator(configuration, config);
        _solutionOrchestrator = BuildSolutionOrchestrator(config, _orchestrator);

        ConsoleOutput.WriteSection("New Request");
        Console.WriteLine($"  {feature}");
        Console.WriteLine();

        if (!string.IsNullOrEmpty(timeout))
        {
            Log.Information("Using timeout override: {Timeout}", timeout);
            var parsedTimeout = ParseTimeoutOverride(timeout);
            if (parsedTimeout.HasValue)
            {
                ApplyTimeoutOverride(config, parsedTimeout.Value);
            }
        }

        // Route through SolutionOrchestrator which handles triage
        // (auto-detects single-feature vs multi-release)
        var success = await _solutionOrchestrator.RunAsync(feature, _globalCts.Token);

        if (success)
        {
            ConsoleOutput.WriteSuccess("Implementation completed successfully.");
        }
        else
        {
            ConsoleOutput.WriteError("Implementation failed or was cancelled.");
        }
    }

    private static LoopOrchestrator BuildOrchestrator(IConfiguration configuration, HughConfig? existingConfig = null)
    {
        // Use provided config or bind from configuration
        var hughConfig = existingConfig ?? new HughConfig();
        if (existingConfig == null)
            configuration.GetSection("Hugh").Bind(hughConfig);
        var configOptions = Options.Create(hughConfig);

        // Create null loggers for components (using Serilog directly for now)
        var nullLoggerFactory = NullLoggerFactory.Instance;

        // Build dependencies manually
        var workspaceRoot = Directory.GetCurrentDirectory();

        var planPhaseParser = new PlanPhaseParser();
        var researchPhaseParser = new ResearchPhaseParser();
        var docsPath = Path.Combine(workspaceRoot, hughConfig.DocsPath);
        var progressReporter = new ProgressReporter(docsPath, researchPhaseParser, planPhaseParser, Log.Logger);
        var stateManager = new LoopStateManager(workspaceRoot, Log.Logger, progressReporter: progressReporter);
        var decisionLogger = new DecisionLogger(workspaceRoot, Log.Logger);
        var copilotService = new CopilotService(hughConfig, decisionLogger, Log.Logger);
        var agentRegistry = new AgentRegistry();
        var planCreationPhaseParser = new PlanCreationPhaseParser();
        var taskParser = new TaskParser();
        var gitCommitManager = new GitCommitManager(hughConfig.Git, Log.Logger);

        var phaseExecutor = new PhaseExecutor(
            copilotService,
            agentRegistry,
            stateManager,
            configOptions,
            nullLoggerFactory.CreateLogger<PhaseExecutor>(),
            _globalCts);

        var orchestrator = new LoopOrchestrator(
            phaseExecutor,
            stateManager,
            planPhaseParser,
            researchPhaseParser,
            planCreationPhaseParser,
            taskParser,
            configOptions,
            nullLoggerFactory.CreateLogger<LoopOrchestrator>(),
            gitCommitManager);

        return orchestrator;
    }

    /// <summary>
    /// Builds a SolutionOrchestrator that wraps the LoopOrchestrator for multi-release support.
    /// </summary>
    private static SolutionOrchestrator BuildSolutionOrchestrator(HughConfig config, LoopOrchestrator loopOrchestrator)
    {
        var workspaceRoot = Directory.GetCurrentDirectory();
        var decisionLogger = new DecisionLogger(workspaceRoot, Log.Logger);
        var copilotService = new CopilotService(config, decisionLogger, Log.Logger);
        var solutionStateManager = new SolutionStateManager(workspaceRoot, Log.Logger);
        var roadmapParser = new ReleaseRoadmapParser();
        var gitCommitManager = new GitCommitManager(config.Git, Log.Logger);
        var releaseSummaryGenerator = new ReleaseSummaryGenerator(
            copilotService, gitCommitManager, config, Log.Logger);
        var loggerFactory = new Serilog.Extensions.Logging.SerilogLoggerFactory(Log.Logger);

        return new SolutionOrchestrator(
            copilotService,
            loopOrchestrator,
            solutionStateManager,
            roadmapParser,
            config,
            Log.Logger,
            loggerFactory,
            _globalCts,
            releaseSummaryGenerator,
            gitCommitManager);
    }

    /// <summary>
    /// Discovers and syncs agent prompts from GitHub, updating the config with the discovered path.
    /// </summary>
    private static async Task DiscoverAgentsAsync(HughConfig config)
    {
        if (!config.AgentDiscovery.Enabled)
        {
            Log.Debug("Agent discovery is disabled");
            return;
        }

        var processRunner = new ProcessRunner();
        var loggerFactory = NullLoggerFactory.Instance;
        var discoveryService = new GitHubAgentDiscoveryService(
            processRunner,
            config.AgentDiscovery,
            loggerFactory.CreateLogger<GitHubAgentDiscoveryService>());

        var result = await discoveryService.DiscoverAndSyncAsync(_globalCts.Token);

        if (result.Success && result.AgentsPath != null)
        {
            config.DiscoveredAgentsPath = result.AgentsPath;
            ConsoleOutput.WriteSuccess($"Agents: {result.Message}");
        }
        else
        {
            ConsoleOutput.WriteWarning($"Agent discovery: {result.Message}");
            ConsoleOutput.WriteInfo("Falling back to bundled agents");
        }
    }

    /// <summary>
    /// Builds the setup orchestrator with all prerequisite detectors, installers, and UI.
    /// </summary>
    /// <param name="options">The setup options to configure the orchestrator.</param>
    /// <param name="discoveryConfig">The agent discovery configuration.</param>
    /// <returns>A configured setup orchestrator instance.</returns>
    private static ISetupOrchestrator BuildSetupOrchestrator(SetupOptions options, AgentDiscoveryConfig discoveryConfig)
    {
        var workspacePath = options.WorkspacePath ?? Directory.GetCurrentDirectory();
        var processRunner = new ProcessRunner();
        var bundledAgentsPath = options.AgentsPath ?? Path.Combine(PathResolver.GetInstallationPath(), "agents");

        // Client factory for SDK-based auth detection (best-effort; falls back to shell if unavailable)
        Func<GitHub.Copilot.SDK.CopilotClient> clientFactory = () => new GitHub.Copilot.SDK.CopilotClient(
            new GitHub.Copilot.SDK.CopilotClientOptions
            {
                Cwd = workspacePath,
                UseLoggedInUser = true
            });

        // Create detectors in priority order
        var detectors = new List<IPrerequisiteDetector>
        {
            new AgentDiscoveryDetector(discoveryConfig, bundledAgentsPath),
            new WorkspaceDetector(workspacePath),
            new GitDetector(processRunner),
            new GitHubCliDetector(processRunner),
            new DotNetSdkDetector(processRunner),
            new CopilotCliDetector(processRunner),
            new GitHubAuthDetector(processRunner, clientFactory)
        };

        // Create installers in priority order (highest priority first)
        var installers = new List<IInstaller>
        {
            new WingetInstaller(processRunner),
            new ManualInstaller()
        };

        var installerRegistry = new InstallerRegistry(installers);

        // Create UI based on interactive mode
        ISetupUserInterface userInterface = options.NonInteractive
            ? new NonInteractiveSetupUI()
            : new InteractiveSetupUI();

        return new SetupOrchestrator(detectors, installerRegistry, userInterface);
    }

    /// <summary>
    /// Parses a timeout string in HH:mm:ss format to a TimeSpan.
    /// </summary>
    /// <param name="timeout">The timeout string to parse.</param>
    /// <returns>The parsed TimeSpan, or null if parsing fails or input is empty.</returns>
    internal static TimeSpan? ParseTimeoutOverride(string? timeout)
    {
        if (string.IsNullOrWhiteSpace(timeout))
            return null;

        if (TimeSpan.TryParse(timeout, out var result))
            return result;

        Log.Warning("Invalid timeout format: {Timeout}. Expected HH:mm:ss format. Using defaults.", timeout);
        return null;
    }

    /// <summary>
    /// Applies a timeout override to all phase timeouts in the configuration.
    /// </summary>
    /// <param name="config">The configuration to modify.</param>
    /// <param name="timeout">The timeout value to apply to all phases.</param>
    internal static void ApplyTimeoutOverride(HughConfig config, TimeSpan timeout)
    {
        config.Timeouts.VisionPhase = timeout;
        config.Timeouts.ResearchPhase = timeout;
        config.Timeouts.PlanningPhase = timeout;
        config.Timeouts.ReviewPhase = timeout;
        config.Timeouts.CodingPhasePerPhase = timeout;
        Log.Information("All phase timeouts set to: {Timeout}", timeout);
    }
}

