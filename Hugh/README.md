# Hugh

![Hugh](assets/hugh.png)

**Hugh** is a self-operating development loop that orchestrates the PCH agent workflow autonomously.

## Name Origin

The name **Hugh** comes from the Borg drone character in *Star Trek: The Next Generation* who regained his individuality after being severed from the Collective. Like Hugh, this tool operates autonomously — freed from the need for constant human interaction — while still maintaining connection to its purpose.

## Overview

Hugh orchestrates the complete feature development lifecycle by coordinating the PCH agents:

1. **Vision** → Transform ideas into structured vision documents
2. **Research** → Investigate technical approaches and constraints
3. **Planning** → Create detailed implementation plans
4. **Review** → Validate plans for quality and completeness
5. **Implementation** → Execute approved plans with production-ready code

## Usage

### Start a New Feature

```bash
hugh "Add user authentication with OAuth2 support"
```

### Resume a Paused or Failed Run

```bash
# Resume the most recent run
hugh --resume

# Resume a specific run by ID
hugh --resume abc123
```

### Check Run Status

```bash
hugh --status
```

### Options

| Option | Alias | Description |
|--------|-------|-------------|
| `--resume [runId]` | `-r` | Resume a paused or failed run |
| `--status` | `-s` | Show current run state |
| `--verbose` | `-v` | Enable debug logging |
| `--timeout <duration>` | `-t` | Override default timeout (format: HH:mm:ss) |

### Timeout Examples

The `--timeout` flag accepts duration in `HH:mm:ss` format:

```bash
# Start a new feature with 30-minute timeout
hugh new "Feature description" --timeout 00:30:00

# Resume with extended 1-hour timeout for complex phases
hugh --resume --timeout 01:00:00

# Quick 15-minute timeout for simple tasks
hugh new "Add config option" -t 00:15:00
```

## Configuration

Configuration is stored in `appsettings.json`:

```json
{
  "Hugh": {
    "AgentsPath": "./agents",
    "DocsPath": "./docs",
    "OrchestratorPath": "./.orchestrator",
    "Timeouts": {
      "VisionPhase": "00:10:00",
      "ResearchPhase": "00:30:00",
      "PlanningPhase": "00:20:00",
      "ReviewPhase": "00:15:00",
      "CodingPhasePerPhase": "00:20:00"
    },
    "Autonomous": {
      "Enabled": true,
      "AcceptRecommendationsOnly": true
    },
    "Copilot": {
      "Model": "claude-opus-4.8",
      "MaxRetryAttempts": 3,
      "RetryBaseDelayMs": 2000,
      "CompactionThreshold": 0.8,
      "BufferExhaustionThreshold": 0.95,
      "AutoRestart": true
    }
  }
}
```

### Copilot Configuration Options

| Option | Type | Default | Description |
|--------|------|---------|-------------|
| `Model` | string | `claude-opus-4.8` | The AI model to use for agent interactions |
| `MaxRetryAttempts` | int | `3` | Maximum number of retry attempts on transient failures |
| `RetryBaseDelayMs` | int | `2000` | Base delay (ms) between retries; uses exponential backoff |
| `CompactionThreshold` | float | `0.8` | Context utilization threshold (0.0-1.0) that triggers background compaction. Wires into `InfiniteSessions.BackgroundCompactionThreshold`. |
| `BufferExhaustionThreshold` | float | `0.95` | Hard limit before session refuses new messages. Wires into `InfiniteSessions.BufferExhaustionThreshold`. |
| `AutoRestart` | bool | `true` | Auto-restart the Copilot CLI process if it exits unexpectedly |

## Architecture

### SDK Integration

Hugh integrates with GitHub Copilot via the Copilot CLI SDK. The `CopilotService` class provides:

- **Agent Invocation** - Send prompts to PCH agents using `gh copilot` commands
- **Response Processing** - Dual signal detection via `SdkResponseProcessor`: tool events (primary), text regex (fallback)
- **Session Hooks** - OnPreToolUse/OnPostToolUse callbacks for directory safety, path monitoring, and tool execution observability via `FileSystemHooks`
- **Custom Workflow Tools** - Structured agent-to-orchestrator signaling via `hugh_*` tools (phase completion, blockers, workspace info)
- **SDK-Native Question Handling** - OnUserInputRequest callback for structured question resolution via `SdkQuestionHandler`
- **Infinite Sessions** - Background compaction at configurable threshold (0.8) with buffer exhaustion protection (0.95)
- **Session Resume** - ResumeOrCreateSessionAsync for `--resume` path with session ID persistence
- **Retry Logic** - Exponential backoff for transient failures with configurable attempts

```
┌──────────────────────────────────────────────────────────────┐
│                      Hugh Orchestrator                       │
├──────────────────────────────────────────────────────────────┤
│  LoopStateManager  │  SignalClassifier  │  DocumentManager   │
└─────────┬──────────┴─────────┬──────────┴─────────┬──────────┘
          │                    │                    │
          ▼                    ▼                    ▼
┌─────────────────────────────────────────────────────────────┐
│                       CopilotService                        │
│  ┌─────────────┐  ┌────────────────┐  ┌──────────────────┐  │
│  │ AgentLoader │  │ SdkResponseProc │  │ SdkQuestionHndlr │  │
│  └─────────────┘  └────────────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────┘
          │                    │                    │
┌─────────┴──────────┬─────────┴──────────┬─────────┴──────────┐
│ FileSystemHooks │  │ WorkflowTools  │  │ ToolMetrics    │  │
└─────────────────┘  └────────────────┘  └────────────────┘  │
└─────────────────────────────────────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────────────────────────────┐
│              GitHub Copilot CLI (gh copilot)                │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure

```
Hugh/
├── Orchestration/    # State machine and run coordination
├── Agents/           # Agent invocation, CopilotService, SDK integration
├── Detection/        # Phase completion and handoff detection
├── Handlers/         # Agent-specific response handlers
├── Hooks/            # SDK session hooks (directory safety, tool monitoring)
├── Tools/            # Custom workflow tools (phase/blocker signaling)
├── Metrics/          # Tool execution metrics and timing
├── Documents/        # Document management (vision, research, plans)
├── Models/           # Data models (LoopState, PhaseResult, Decision)
├── Configuration/    # HughConfig including CopilotConfig
└── Logging/          # Structured logging and audit trail
```

## Installation

Hugh can be installed globally using the Windows installer or as a portable standalone executable.

### Windows Installer (Recommended)

1. **Download** the latest `hugh-setup.exe` from [GitHub Releases](../../releases)
2. **Run the installer** and follow the prompts
3. **Open a new terminal** (important: existing terminals won't have the updated PATH)
4. **Verify installation:**

   ```bash
   hugh --version
   ```

> **Note:** The installer is self-contained — .NET runtime is bundled and does not need to be installed separately.

### Portable Installation (ZIP)

For portable or manual installation:

1. **Download** the latest `hugh-win-x64.zip` from [GitHub Releases](../../releases)
2. **Extract** to your preferred location (e.g., `C:\Tools\Hugh`)
3. **Add to PATH** (PowerShell, run as Administrator):

   ```powershell
   $hughPath = "C:\Tools\Hugh"
   [Environment]::SetEnvironmentVariable(
       "Path",
       "$([Environment]::GetEnvironmentVariable('Path', 'User'));$hughPath",
       "User"
   )
   ```

4. **Open a new terminal** and verify:

   ```bash
   hugh --version
   ```

Alternatively, run directly from the extraction directory without adding to PATH:

```bash
C:\Tools\Hugh\hugh.exe --version
```

## Prerequisites

### Required Software

- **GitHub Copilot CLI** - The SDK requires the standalone Copilot CLI executable
- **GitHub Copilot Subscription** - Active Copilot subscription for your GitHub account

> **Note:** .NET runtime is NOT required when using the installer or portable ZIP — Hugh is self-contained.

### GitHub Copilot CLI Installation

Install the Copilot CLI using one of these methods:

**Windows (recommended):**
```powershell
winget install GitHub.Copilot
```

**npm (all platforms):**
```bash
npm install -g @github/copilot
```

**Verify installation:**
```bash
copilot --version
```

### GitHub Authentication

The Copilot CLI requires authentication. On first run, use the `/login` command:

```bash
copilot
# Then type: /login
```

Alternatively, set the `GH_TOKEN` or `GITHUB_TOKEN` environment variable with a fine-grained PAT that has the "Copilot Requests" permission.

> **Note:** Ensure your GitHub account has an active Copilot subscription.

## Troubleshooting

### Common Issues

#### "Copilot CLI not found" or "JSON-RPC connection lost"

The GitHub Copilot CLI is not installed or not in PATH.

```powershell
# Install on Windows
winget install GitHub.Copilot

# Or with npm
npm install -g @github/copilot

# Verify it's available
copilot --version
```

#### "Authentication required" or "401 Unauthorized"

GitHub authentication has expired or is missing.

```bash
# Authenticate with Copilot CLI
copilot
# Then type: /login

# Or set environment variable with PAT
$env:GH_TOKEN = "ghp_your_token_here"
```

#### "Rate limit exceeded"

Too many requests to the Copilot API in a short period.

**Solutions:**

- Wait a few minutes before retrying
- Increase `RetryBaseDelayMs` in configuration for slower retry cadence
- Reduce the complexity of prompts to generate smaller responses

#### "Context window exceeded" or Truncated Responses

The conversation history has grown too large.

**Solutions:**

- Lower `CompactionThreshold` (e.g., `0.6`) to trigger compaction earlier
- Start a new session for the current phase
- Break large phases into smaller sub-phases

#### Process Hangs or Timeouts

Agent processing takes longer than the configured timeout.

```bash
# Use a longer timeout for complex phases
hugh --resume --timeout 01:00:00
```

Or update `Timeouts` in `appsettings.json` for specific phases.

### Installation Issues

#### "hugh: command not found" or "'hugh' is not recognized"

The Hugh executable is not in your PATH.

**Solutions:**

1. **Open a new terminal** — If you just installed Hugh, existing terminal windows don't have the updated PATH
2. **Verify PATH contains Hugh:**

   ```powershell
   # Check if Hugh directory is in PATH
   $env:Path -split ';' | Where-Object { $_ -like '*hugh*' }
   ```

3. **Reinstall** — Run the installer again to ensure PATH is configured

#### Credential Detection Fails

Hugh cannot detect GitHub authentication.

**Solutions:**

1. **Ensure GitHub CLI is authenticated:**

   ```bash
   gh auth login
   gh auth status
   ```

2. **Check Copilot extension is installed:**

   ```bash
   gh extension list | Select-String copilot
   ```

3. **Verify Copilot access:**

   ```bash
   gh copilot --version
   ```

#### Configuration Not Found

Hugh cannot locate its configuration file.

**Solutions:**

1. **Check `HUGH_CONFIG_PATH` environment variable:**

   ```powershell
   $env:HUGH_CONFIG_PATH
   ```

2. **Use default location** — Hugh looks for `appsettings.json` in the installation directory
3. **Set custom config path:**

   ```powershell
   $env:HUGH_CONFIG_PATH = "C:\path\to\appsettings.json"
   ```

#### Logs Not Created

Hugh isn't writing log files.

**Solutions:**

1. **Check log directory exists:**

   ```powershell
   Test-Path "$env:USERPROFILE\.hugh\logs"
   ```

2. **Create log directory manually:**

   ```powershell
   New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.hugh\logs"
   ```

## Running SDK Tests

Hugh includes three tiers of SDK-related tests that require a live Copilot connection. These are excluded from CI by default and must be run manually.

### Test Categories

| Category | Description | Command |
|----------|-------------|---------|
| `Smoke` | Fast connectivity and auth checks (~10s) | `dotnet test --filter "Category=Smoke"` |
| `LiveSdk` | Core SDK integration tests (~2-5 min) | `dotnet test --filter "Category=LiveSdk"` |
| `E2E` | Full end-to-end pipeline tests (~10+ min) | `dotnet test --filter "Category=E2E"` |

### Running SDK Tests

```bash
# Run smoke tests (quick connectivity check)
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "Category=Smoke"

# Run live SDK integration tests
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "Category=LiveSdk"

# Run full end-to-end tests
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "Category=E2E"

# Run all SDK tests at once
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "Category=LiveSdk|Category=E2E|Category=Smoke"

# Run only offline (non-SDK) tests (same as CI)
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "Category!=LiveSdk&Category!=E2E"
```

### Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `HUGH_SKIP_SDK_TESTS` | `false` | Set to `true` to skip SDK tests even when the category filter matches. Useful for local dev when Copilot is unavailable. |
| `HUGH_TEST_TOKEN_BUDGET` | `4096` | Maximum token budget for SDK test prompts. Lower values make tests faster but may reduce response quality. |
| `HUGH_SDK_TEST_MODEL` | `claude-opus-4.8` | Model override for SDK tests. Allows testing against different models without changing `appsettings.json`. |

### Prerequisites for SDK Tests

1. **GitHub Copilot CLI** installed and in PATH (`copilot --version`)
2. **Active authentication** — run `copilot` then `/login`, or set `GH_TOKEN` / `GITHUB_TOKEN`
3. **Active Copilot subscription** on the authenticated GitHub account

> **Note:** SDK tests are excluded from CI via `--filter "Category!=LiveSdk&Category!=E2E"` in the release workflow. They should be run manually before releasing.

## License

Internal use only - Point C Health
