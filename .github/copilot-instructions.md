# Copilot Instructions for .github-private Repository

## Project Overview

This repository contains specialized AI agent prompts designed for GitHub Copilot Workspace. These agents follow a structured workflow for planning, reviewing, and implementing software features using a phased, incremental approach that respects AI context window limitations.

## Agent Architecture

The project includes six core agents that work together in a coordinated workflow:

1. **pch-visionary** - Transforms raw ideas into structured vision documents through guided Q&A, combining business analyst and solution architect perspectives
2. **pch-researcher** - Creates phased research outlines first, then delegates phase execution (always Automatic Mode)
3. **pch-planner** - Creates detailed implementation plans
4. **pch-plan-reviewer** - Reviews plans for quality and completeness
5. **pch-coder** - Executes implementation from approved plans (always Automatic Mode)
6. **pch-helper** - Guides users on agent workflow and provides usage advice

### Execution Modes (pch-coder & pch-researcher)

Both `pch-coder` and `pch-researcher` always run in **Automatic Mode** using subagent orchestration. No mode selection is presented. Each phase executes in an isolated subagent with fresh context.

> ⚠️ Automatic Mode requires VS Code setting: `chat.customAgentInSubagent.enabled: true`
> If subagent invocation fails, both agents display an enablement message and offer to execute directly in the current context as a fallback.
>
> **pch-coder session boundaries:** Always start a new agent session for pch-coder — do not continue from a reviewer session.

### Agent Workflow

```
Vision → Research → Planning → Review → Implementation
                             ↑
                        pch-helper (guidance at any stage)
```

Each agent hands off to the next in the chain, with specific handoff prompts defined in their YAML front matter. The `pch-helper` agent can be invoked at any time to provide guidance on which agent to use and how to navigate the workflow.

## Agent File Format

All agent prompt files follow a standardized structure:

### YAML Front Matter

Each agent file begins with **YAML front matter** containing metadata:

```yaml
---
name: agent-name
description: Brief description of the agent's purpose and expertise
model: Claude Opus 4.8
handoffs:
  - label: Human-readable label for the handoff action
    agent: target-agent-name
    prompt: The prompt that will be sent to the target agent
    send: true/false  # Whether to auto-send or just prepare the handoff
---
```

**Front Matter Fields:**

- `name`: The agent's identifier (used when invoking the agent)
- `description`: A concise summary of the agent's role and capabilities
- `model`: The AI model the agent uses (typically Claude Opus 4.8)
- `handoffs`: Array of possible handoff transitions to other agents
  - `label`: UI-friendly name for the handoff button/action
  - `agent`: Target agent's name to hand off to
  - `prompt`: The context/instruction passed to the next agent
  - `send`: Boolean indicating if handoff should auto-execute or require confirmation

### Content Structure

After the YAML front matter, each agent file contains:

1. **Role Definition** - Clear statement of the agent's identity and purpose
2. **Core Responsibilities** - Bulleted list of primary duties
3. **Critical Rules/Workflows** - Important constraints and procedures (often emphasized)
4. **Detailed Instructions** - Comprehensive guidance on how to perform tasks
5. **Examples** (where applicable) - Sample formats, questions, or outputs

## Key Design Principles

### Context Window Management

All agents emphasize **phased execution** to avoid context window exhaustion:

- **One phase per session** - Agents complete discrete units of work then stop
- **Explicit session boundaries** - Agents request new sessions between phases
- **Document-based persistence** - All progress is saved to files before stopping
- **Fresh context for each phase** - New sessions provide full context capacity

### Incremental Document Creation

To prevent large response errors:

- Build documents **one section at a time**
- Update documents after each decision point
- Never attempt to generate entire large documents in a single response
- Prefer multiple small edits over large file operations

### Structured Interaction

- **One question at a time** - Planner and reviewer ask questions individually
- **Multiple choice format** - Questions provide 3-5 options with recommendations
- **Decision logging** - All decisions are documented in session logs within plan documents
- **Progress tracking** - Clear status values (`not-started`, `in-progress`, `complete`, `blocked`)

## Working with Agent Files

### When Creating New Agents

1. **Always include YAML front matter** with required fields
2. **Define clear handoffs** to integrate with existing workflow
3. **Emphasize context management** - Include rules about phased work and stopping points
4. **Provide structured formats** - Use tables, templates, and examples
5. **Document critical rules prominently** - Use headers like "🚨 CRITICAL RULES"

### When Modifying Agents

1. **Preserve YAML front matter structure** - Don't break the schema
2. **Test handoff chains** - Ensure agent transitions still work
3. **Maintain consistency** - Use similar formatting and conventions across agents
4. **Update descriptions** - Keep front matter descriptions accurate

### File Naming Convention

- Agent files use kebab-case: `pch-agent-name.md`
- The `pch-` prefix identifies agents in this system
- Names should be descriptive and match the `name` field in front matter

## Document Locations

- **Agent Prompts**: `/agents/`
- **Vision Documents**: `/docs/vision/` (created by pch-visionary)
- **Research Documents**: `/docs/research/` (created by pch-researcher)
- **Implementation Plans**: `/docs/plans/` (created by pch-planner)
- **Presentations**: `/docs/presentations/` (Marp-based slide decks)

## Presentations

The project includes a Marp-based presentation system for agent workflow documentation:

- **Source**: `docs/presentations/pch-agent-workflow.md` (Marp Markdown)
- **Output**: `docs/presentations/pch-agent-workflow.html` (built slides)
- **Build Script**: `docs/presentations/Build-Presentation.ps1` (PowerShell, runs Marp CLI)
- **CI/CD**: `.github/workflows/deploy-presentation.yml` (GitHub Pages deployment)

## Testing Agents

When testing or validating agent behavior:

1. Check that YAML front matter is valid and properly closed with `---`
2. Verify handoffs point to existing agents
3. Ensure critical rules are clearly marked and emphasized
4. Confirm document creation paths are correct
5. Test that phased workflows properly stop and request new sessions

## Common Patterns

### Progress Status Values

Standard throughout all agents (with emoji for visual scanning):
- ⏳ Not Started (`not-started`)
- 🔄 In Progress (`in-progress`)
- ✅ Complete (`complete`)
- ❌ Blocked (`blocked`)
- ⚠️ Partial (`partial`)

### Session Log Structure

Plans and research documents include session logs tracking decisions in Markdown table format:

```markdown
## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|----------|
| 1 | [Question] | [Choice] | [Reasoning] |
| 2 | [Question] | [Choice] | [Reasoning] |
```

### Phase Completion

All agents that work in phases follow this pattern:
1. Complete the phase work
2. Update the document with status `complete`
3. Save all progress to files
4. Request a new session for next phase
5. **Never continue to subsequent phases automatically** — subagent orchestration handles phase transitions in Automatic Mode

### Automatic Mode

Both `pch-coder` and `pch-researcher` always run in Automatic Mode using subagent orchestration. No mode selection is presented.

#### How It Works

- **Subagent Delegation** - Each phase runs in an isolated subagent with fresh context
- **Orchestrator Control** - Main agent coordinates phases and processes results
- **Structured Returns** - Subagents return structured results (status, files modified, blockers)
- **Question Passthrough** - Subagents can return `STATUS: needs_user_input` to surface questions to the end-user
- **Blocker Handling** - Options to continue, skip, stop, retry, or provide clarification when issues arise
- **Checkpoints** - Automatic pauses for deployment phases (coder) or ambiguous scope (researcher)
- **Post-Phase Reports** - Summary displayed after each phase completes

**Prerequisites:**

1. VS Code setting `chat.customAgentInSubagent.enabled: true`
2. Complete, reviewed plan/research document
3. If subagent invocation fails, both agents display an enablement message and offer to execute the phase directly in the current context as a fallback

#### Document Context: Attaching vs. Path Reference

When continuing work on a plan or research document, users can provide context in two ways:

| Method | Description | Agent Behavior |
|--------|-------------|----------------|
| **Attach document** | User adds the file as an attachment to the chat | Agent reads attached content immediately, then begins automatic execution |
| **Reference path** | User types the document path in their message | Agent loads the document using `read_file`, then begins automatic execution |

**Both methods result in immediate automatic execution.** The agents are designed to detect the document context regardless of how it's provided.

**Example user messages (both valid):**
- With attachment: "Continue implementing — start Phase 3" (document attached)
- With path: "Continue implementing the plan at `/docs/plans/001-feature.md` — start Phase 3"

### Standards Integration

The `pch-planner`, `pch-plan-reviewer`, and `pch-researcher` agents can query organizational standards from a GitHub Copilot Space named "pch-standards-space" via the GitHub MCP server. This enables agents to automatically discover and apply PCH organizational standards during planning, review, and research activities. Standards access requires:

- GitHub MCP server extension installed in VS Code
- A Copilot Space named "pch-standards-space" created and shared in the organization

If MCP or the Copilot Space is unavailable, agents will continue functioning normally with a warning message.

## Hugh - Self-Operating Development Loop

The `Hugh/` directory contains a .NET 8 console application that orchestrates the PCH agent workflow autonomously. Hugh integrates with GitHub Copilot via the Copilot CLI SDK and can be installed globally as a Windows CLI tool.

### Hugh Architecture

Hugh is organized into the following directories and key components:

#### Core Orchestration (`Hugh/Orchestration/`)

- **LoopOrchestrator** (`LoopOrchestrator.cs`) - Main orchestration class coordinating the Vision → Research → Planning → Review → Coding phase pipeline
- **LoopStateManager** (`LoopStateManager.cs`) - Persists run state to `.orchestrator/` directory for resume capability
- **PhaseExecutor** (`PhaseExecutor.cs`) - Executes individual phases by invoking agents via CopilotService

#### Agent Integration (`Hugh/Agents/`)

- **CopilotService** (`CopilotService.cs`) / **ICopilotService** - Primary interface to the Copilot CLI SDK
  - Invokes agents via `gh copilot` commands
  - Manages conversation context and session state
  - Implements retry logic with exponential backoff
  - SessionConfig configures Hooks, Tools, OnUserInputRequest, InfiniteSessions, OnPermissionRequest in addition to Model and SystemMessage

- **SdkResponseProcessor** (`SdkResponseProcessor.cs`) - Parses structured agent responses
  - Dual signal detection: tool events (primary), text regex (fallback per D10)
  - Handles ToolExecution events for workflow tool signals
  - Handles SessionCompaction events for context management
  - Populates ToolMetrics from tool execution events
  - Extracts phase status, completed tasks, and blockers
  - Handles `success`, `partial`, `blocked`, and `needs_user_input` status values

- **AgentRegistry** / **IAgentRegistry** (`AgentRegistry.cs`, `IAgentRegistry.cs`) - Instance-based DI service for agent definitions
- **AgentPromptLoader** (`AgentPromptLoader.cs`) - Loads agent prompt files from the agents directory
- **AgentDefinition** (`AgentDefinition.cs`) - Model representing an agent's metadata
- **DocumentPathExtractor** (`DocumentPathExtractor.cs`) - Extracts document paths from agent responses

#### Hooks (`Hugh/Hooks/`)

- **FileSystemHooks** (`FileSystemHooks.cs`) - SDK session hooks for file system safety
  - OnPreToolUse callback for directory auto-creation before file writes
  - OnPostToolUse callback for path monitoring (logging-only) and tool completion logging
  - Integrates with SessionConfig.Hooks for SDK session configuration

#### Custom Workflow Tools (`Hugh/Tools/`)

- **HughWorkflowTools** (`HughWorkflowTools.cs`) - Custom tools for agent-to-orchestrator signaling
  - `hugh_notify_phase_complete` - Structured phase completion signal
  - `hugh_report_blocker` - Structured blocker reporting
  - `hugh_notify_workflow_complete` - Workflow completion signal
  - `hugh_get_workspace_structure` - Workspace discovery for agents
- **WorkflowToolResults** (`WorkflowToolResults.cs`) - Result models for workflow tools
  - `PhaseCompleteResult` - Phase completion details (tasks, files, deviations)
  - `BlockerResult` - Blocker information (task, reason, suggestions)
  - `WorkflowCompleteResult` - Workflow summary (all phases, total files)
  - `WorkspaceStructureResult` - Workspace tree for agent context

#### Metrics (`Hugh/Metrics/`)

- **ToolMetricsCollector** (`ToolMetricsCollector.cs`) - Tracks tool execution observability
  - Records tool execution timing (start, end, duration)
  - Counts tool invocations by name
  - Tracks error rates and failure patterns
  - Populated by SdkResponseProcessor from ToolExecution events

#### Signal Detection (`Hugh/Detection/`)

- **SignalClassifier** (`SignalClassifier.cs`) - Determines next actions from agent output
  - Classifies signals: `PhaseComplete`, `BlockerDetected`, `QuestionAsked`, `HandoffRequest`
  - Drives the orchestration state machine transitions
  - Regex patterns are permanent (text-fallback for dual-environment compatibility per D9/D10)
- **SessionErrorClassifier** (`SessionErrorClassifier.cs`) - Categorizes session errors into 6 types
  - Authentication errors, rate limiting, context overflow, network failures, SDK errors, unknown errors
  - Enables targeted recovery strategies based on error type
- **PlanPhaseParser** (`PlanPhaseParser.cs`) - Parses plan documents to detect phase structure and status
- **SignalType** (`SignalType.cs`) - Enum defining signal categories

#### Question Handling (`Hugh/Handlers/`)

- **SdkQuestionHandler** (`SdkQuestionHandler.cs`) - Handles SDK-native UserInputRequest objects
  - Single source of truth for all question resolution logic (auto-accept, pause, logging)
  - Auto-accepts recommendations when `Autonomous.AcceptRecommendationsOnly` is true
  - Pauses execution for open-ended questions requiring human input
  - Returns `QuestionResponse` indicating whether to pause or auto-respond
  - Logs all autonomous decisions via DecisionLogger
- **QuestionHandler** (`QuestionHandler.cs`) - Thin delegation wrapper for text-detected questions (per D11)
  - Converts text-based question signals to UserInputRequest format
  - Delegates all decision logic to SdkQuestionHandler
  - Maintains backward compatibility with text-based agent responses
- **BlockerHandler** (`BlockerHandler.cs`) - Handles blocker signals with retry/skip/stop options

#### Configuration (`Hugh/Configuration/`)

- **HughConfig** (`HughConfig.cs`) - Strongly-typed configuration model bound from `appsettings.json`
- **PathResolver** (`PathResolver.cs`) - Static utility resolving paths for both dev and installed scenarios
  - Resolution order: `HUGH_CONFIG_PATH` env var → Assembly location → CWD fallback
  - User data stored at `~/.hugh/` (logs in `~/.hugh/logs/`)

#### Models (`Hugh/Models/`)

- **LoopState** (`LoopState.cs`) - Serializable run state for persistence/resume
- **PhaseResult** (`PhaseResult.cs`) - Result of a phase execution (includes Usage, Subagents, SessionId)
- **UsageMetrics** (`UsageMetrics.cs`) - Token usage per phase (input, output, reasoning, duration, model)
- **SubagentMetrics** (`SubagentMetrics.cs`) - Subagent lifecycle counts (started, completed, failed)
- **SessionReport** (`SessionReport.cs`) - End-of-run summary (all phases, total usage, compaction events)
- **QuestionResponse** (`QuestionResponse.cs`) - Response from question handling (`ShouldPause`, `Response`, `DecisionMade`)
- **Decision** (`Decision.cs`) - Audit record for autonomous decisions

#### Other Components

- **Documents** (`Hugh/Documents/DocumentManager.cs`) - Document read/write operations
- **Logging** (`Hugh/Logging/DecisionLogger.cs`) - Logs autonomous decisions for audit trail
- **ConsoleOutput** (`Hugh/ConsoleOutput.cs`) - Terminal formatting and colored output
- **Program** (`Hugh/Program.cs`) - Entry point with `System.CommandLine` CLI definition

#### Setup & Prerequisites (`Hugh/Setup/`)

Hugh includes a prerequisite detection and installation system:

- **SetupOrchestrator** / **ISetupOrchestrator** - Coordinates prerequisite checks and optional installation
- **Detection/**:
  - `GitHubCliDetector` - Checks for `gh` CLI
  - `CopilotCliDetector` - Checks for `gh copilot` extension
  - `GitHubAuthDetector` - Checks for `gh auth` status
  - `AgentsFolderDetector` - Verifies agents directory exists
  - `WorkspaceDetector` - Validates workspace structure
  - `IPrerequisiteDetector` / `PrerequisiteStatus` - Common interfaces
  - `ProcessRunner` / `IProcessRunner` - Abstraction for running external processes
- **Installation/**:
  - `WingetInstaller` - Installs prerequisites via `winget`
  - `ManualInstaller` - Provides manual installation instructions
  - `InstallerRegistry` / `IInstallerRegistry` - Maps prerequisites to installers
  - `InstallationResult` - Result model
- **UserInterface/**:
  - `InteractiveSetupUI` - Interactive terminal prompts
  - `NonInteractiveSetupUI` - CI/CD-compatible output (no prompts)
  - `ISetupUserInterface` - Common interface

### CLI Usage

Hugh uses `System.CommandLine` for its CLI interface:

```bash
# Run with a feature request
hugh "Add user authentication"

# Resume a paused/failed run
hugh --resume [runId]

# Show current run state
hugh --status

# Check prerequisites
hugh --check
hugh doctor

# Check and auto-fix prerequisites
hugh doctor --fix

# Non-interactive mode (CI/CD)
hugh doctor --fix --no-interactive

# Verbose logging
hugh --verbose

# Custom timeout
hugh --timeout 00:30:00
```

### Configuration

Hugh reads configuration from `Hugh/appsettings.json`:

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

| Setting | Purpose |
|---------|--------|
| `AgentsPath` | Relative path to agent prompt files |
| `DocsPath` | Relative path to docs directory (plans, research, vision) |
| `OrchestratorPath` | State persistence directory for run resume |
| `Timeouts.*` | Per-phase timeout limits |
| `Autonomous.Enabled` | Enable autonomous execution mode |
| `Autonomous.AcceptRecommendationsOnly` | Auto-accept only when a recommendation is provided; pause for open questions |
| `Copilot.Model` | AI model for agent interactions |
| `Copilot.MaxRetryAttempts` | Retry count for transient failures |
| `Copilot.RetryBaseDelayMs` | Base delay between retries (exponential backoff) |
| `Copilot.CompactionThreshold` | Context utilization threshold triggering compaction. Wires into `InfiniteSessions.BackgroundCompactionThreshold` for SDK session configuration. |
| `Copilot.BufferExhaustionThreshold` | Hard limit before session refuses new messages (0.95). Wires into `InfiniteSessions.BufferExhaustionThreshold`. |
| `Copilot.AutoRestart` | Auto-restart the Copilot CLI process if it exits unexpectedly (default: true) |

### Dual-Environment Agent Signaling

Hugh supports a dual-environment architecture where agent prompts (in `agents/`) are shared between VS Code Copilot chat and Hugh CLI orchestration. Each environment has different capabilities:

| Capability | VS Code Copilot | Hugh CLI |
|---|---|---|
| Signal detection | Text regex (orchestrator agent) | Tool events (primary) + text regex (fallback) |
| Question handling | Text return format | SDK OnUserInputRequest (primary) + text fallback via QuestionHandler delegation |
| Custom tools | Not available | `hugh_*` tools via SessionConfig.Tools |
| Session hooks | Not available | FileSystemHooks via SessionConfig.Hooks |
| InfiniteSessions | Not available | BackgroundCompactionThreshold = 0.8, BufferExhaustionThreshold = 0.95 |

**Architecture Implications:**

- Agent prompts contain BOTH tool-based instructions ("Structured Communication") AND text-based format instructions — both must be preserved
- `SignalClassifier` regex patterns are permanent (not deprecated) for VS Code compatibility (D9, D10)
- `QuestionHandler` delegates to `SdkQuestionHandler` for decision logic, maintaining single source of truth (D11)
- `SdkResponseProcessor` implements tool-first priority with text fallback for hybrid operation

> ⚠️ **Never remove text-based signal formats from agent prompts.** They are required for VS Code Copilot chat compatibility (Decisions D9, D10, D11).

### Distribution & Installation

Hugh supports two distribution methods:

- **Installer** (`installer/hugh-setup.iss`) - Inno Setup Windows installer with PATH integration
- **Portable ZIP** - Self-contained single-file executable for restricted environments

Both are built and published automatically via the release workflow.

### GitHub Actions Workflows

| Workflow | File | Trigger | Purpose |
|----------|------|---------|---------|
| **Release Hugh** | `.github/workflows/release.yml` | `v*.*.*` tags or `workflow_dispatch` | Builds, tests, creates installer + portable ZIP, publishes to GitHub Releases |
| **Deploy Presentation** | `.github/workflows/deploy-presentation.yml` | Push to presentation files | Builds and deploys Marp presentations to GitHub Pages |

#### Release Workflow Details

The release workflow at `.github/workflows/release.yml`:
1. Triggers on `v*.*.*` tags or manual `workflow_dispatch` with version input
2. Runs on `windows-latest`
3. Executes `dotnet test` before publishing
4. Publishes self-contained single-file x64 binary
5. Creates portable ZIP: `hugh-portable-{version}-win-x64.zip`
6. Compiles Inno Setup installer: `hugh-setup-{version}.exe` (via `Minionguyjpro/Inno-Setup-Action@v1.2.4`)
7. Uploads both artifacts to GitHub Release (via `softprops/action-gh-release@v2`)

### Testing Hugh Components

Unit tests are in `Hugh.Tests/`. Key test files:

**Core Tests:**
- `SdkResponseProcessorTests.cs` - Response parsing scenarios; includes tool-first priority tests
- `SignalClassifierTests.cs` - Signal detection logic
- `DocumentPathExtractorTests.cs` - Path extraction from agent responses
- `LoopStateManagerTests.cs` - State persistence and resume
- `QuestionHandlerTests.cs` - Question handling delegation pattern tests
- `SdkQuestionHandlerTests.cs` - SDK-native question handling and auto-accept logic
- `QuestionResponseTests.cs` - QuestionResponse model behavior
- `PathResolverTests.cs` - Path resolution for dev/installed modes
- `IntegrationTests.cs` - End-to-end integration tests
- `FileSystemHooksTests.cs` - File system hook callbacks and directory safety
- `ToolMetricsCollectorTests.cs` - Tool execution metrics tracking
- `HughWorkflowToolsTests.cs` - Custom workflow tool definitions and results
- `SessionErrorClassifierTests.cs` - Session error categorization

**Setup Tests** (`Hugh.Tests/Setup/`):
- `SetupOrchestratorTests.cs` - Setup orchestration flow
- `GitHubCliDetectorTests.cs` - GitHub CLI detection
- `CopilotCliDetectorTests.cs` - Copilot CLI detection
- `GitHubAuthDetectorTests.cs` - GitHub auth detection
- `InstallerRegistryTests.cs` - Installer registry mapping
- `SetupIntegrationTests.cs` - Setup system integration
- `MockProcessRunner.cs` - Test double for process execution

Run tests with:

```bash
cd Hugh.Tests
dotnet test
```
