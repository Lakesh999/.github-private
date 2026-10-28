id: "013"
type: research
title: "Hugh Copilot Agent Prompt"
status: complete
created: "2026-02-10"
current_phase: "3 of 3"
vision_source: /docs/vision/004-hugh-copilot-agent.md

introduction: |
  This research supports the creation of a new @hugh Copilot agent prompt
  that enables developers to install, run, and monitor Hugh CLI directly
  from VS Code's Copilot chat. The research covers existing agent prompt
  conventions, the full Hugh CLI command reference, and — critically —
  whether the current GUI-based Inno Setup installer should be replaced
  or augmented with a CLI-based installation mechanism so the agent can
  better orchestrate install and uninstall operations programmatically.

objectives:
  - "What YAML front matter fields and conventions do existing pch-* agents use, and what handoff patterns should the hugh agent follow?"
  - "What are all Hugh CLI commands, flags, expected outputs, common errors, and their resolutions?"
  - "Should the Hugh installation mechanism be changed from a GUI installer to a CLI-based approach (e.g., dotnet tool, winget, PowerShell script) to enable agent-driven install/uninstall?"
  - "What are the trade-offs between the current Inno Setup installer and CLI-native installation methods for AI agent orchestration?"

phases:
  - phase: 1
    name: "Existing Agent Prompt Patterns & Conventions"
    status: complete
    scope:
      - "What YAML front matter fields and conventions do existing pch-* agents use (name, description, model, handoffs)?"
      - "What handoff patterns should the hugh agent follow — does it hand off to other agents or is it standalone?"
      - "What agent invocation patterns work best for terminal-oriented workflows?"
      - "How do existing agents structure their instruction sections (role definition, rules, workflows)?"
      - "What is the typical size/length of an agent prompt, and are there practical limits?"
    session: "2026-02-10"
    findings: |
      ## YAML Front Matter Fields & Conventions

      All pch-* agents follow a consistent YAML front matter structure:

      ### Required Fields

      | Field | Type | Purpose | Example |
      |-------|------|---------|--------|
      | name | string | Agent identifier used for invocation (@agent-name) | pch-coder |
      | description | string | Concise summary of agent's role (displayed in VS Code) | "Creates phased research outlines..." |
      | model | string | AI model specification | Claude Opus 4.5 |
      | handoffs | array | Possible transitions to other agents | See structure below |

      ### Optional Fields

      | Field | Type | Purpose | Used By |
      |-------|------|---------|--------|
      | mcp-servers | array | MCP server configurations for external tool access | pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer |

      ### Handoffs Array Structure

      Each handoff entry contains:
      - label: Human-readable action name (displayed as clickable option in VS Code)
      - agent: target-agent-name
      - prompt: Multi-line or single-line context for the target agent
      - send: true (auto-execute) or false (require confirmation)

      ### MCP Server Configuration Pattern

      Agents needing external tool access (standards querying) use:
        mcp-servers:
          - config: JSON with github copilot type and copilot_spaces toolset

      ## Handoff Patterns Analysis

      ### Workflow Handoff Chain

      pch-visionary → pch-researcher → pch-planner → pch-plan-reviewer → pch-coder
      With self-handoff for phase continuation, backward handoff (coder→planner),
      and skip handoff (visionary→planner to skip research).

      ### Pattern Categories

      | Pattern | Description | Agents Using It |
      |---------|-------------|----------------|
      | Forward handoff | Chain to next agent in workflow | visionary→researcher, etc. |
      | Self-handoff | Continue work in new session | pch-visionary, pch-researcher |
      | Backward handoff | Return to previous agent for updates | pch-coder→pch-planner |
      | Skip handoff | Jump over optional agent | pch-visionary→pch-planner |
      | Hub handoff | Helper can hand off to any agent | pch-helper→all agents |

      ### Handoff Recommendation for Hugh

      Hugh should be a standalone agent (no handoffs to/from other pch-* agents).
      Hugh focuses on CLI execution, not document generation workflow.
      May optionally offer a "Get Help" handoff to pch-helper for workflow guidance.

      ## Terminal-Oriented Workflow Patterns

      Existing agents use run_in_terminal for:
      1. pch-coder: Git operations (git status, git init, git add, git commit)
      2. pch-coder: Running tests after implementation

      Best practices for terminal-oriented agents (from pch-coder's git handling):
      1. Detection before action: Always check state first (e.g., git status before git init)
      2. Clear acknowledgment: Report what was found and what action was taken
      3. Error interpretation: Parse terminal output and provide actionable guidance
      4. Structured output templates: Use consistent formatting for reporting terminal results

      Recommended patterns for Hugh agent:
      - Detection commands: hugh --version, where.exe hugh, Get-Command hugh
      - Status commands: hugh --status, hugh doctor
      - Action commands: hugh "<feature>", hugh --resume <runId>, hugh doctor --fix
      - Output interpretation: Parse stdout/stderr, interpret exit codes, provide guidance

      ## Instruction Section Structure

      All agents follow a consistent 10-section documentation structure:
      1. Role Definition (1-2 paragraphs): "You are a [role] that [primary function]"
      2. Hard Constraints (if applicable): Forbidden/permitted action lists with emoji markers
      3. Core Responsibilities (bulleted list): 5-10 primary duties
      4. Critical Rules (emphasized): Box diagrams, tables for quick reference
      5. Session Initialization: Decision tree for new request vs continuation
      6. Main Workflow: Detailed step-by-step instructions with examples
      7. Document/Output Format: YAML structure requirements and templates
      8. Handoff Messages: Templates for session transitions
      9. Quality Standards (checklists): Verification steps before completion
      10. Subagent Behavior (if applicable): Return format when running as subagent

      ## Agent Prompt Size Analysis

      | Agent | File Size | Lines | Role |
      |-------|-----------|-------|------|
      | pch-helper | 16 KB | 272 | Simple guide (no complex workflows) |
      | pch-plan-reviewer | 21 KB | 368 | Review process with checklists |
      | pch-planner | 23 KB | 356 | Planning Q&A workflow |
      | pch-coder | 43 KB | 823 | Complex subagent orchestration |
      | pch-visionary | 70 KB | 1304 | Extensive Q&A flow + phasing guidance |
      | pch-researcher | 73 KB | 1314 | Most complex orchestration + methodology |

      ### Size Recommendation for Hugh Agent

      Target: 400-600 lines (15-25 KB)
      Rationale:
      - Hugh is focused on CLI execution (simpler than orchestration agents)
      - No subagent orchestration needed
      - Needs installation guidance, command reference, error interpretation
      - Should be comprehensive but not bloated
    key_discoveries:
      - "All agents use identical YAML front matter: name, description, model, handoffs, optional mcp-servers"
      - "Handoffs support auto-send (send: true) and confirmation-required (send: false) patterns"
      - "Standalone agents like hugh can skip handoffs or offer minimal handoff to pch-helper"
      - "Agent prompts range 272-1314 lines; recommend 400-600 lines for a focused CLI agent"
      - "Terminal-oriented pattern: detect state first, acknowledge, interpret output, provide guidance"
      - "Instruction structure follows 10-section pattern: role → constraints → responsibilities → rules → session init → workflow → format → handoffs → quality → subagent"
      - "Documentation-only constraint pattern does not apply to hugh (it executes terminal commands)"
      - "send: true auto-invokes next agent; send: false shows clickable option requiring confirmation"
    relevant_files:
      - path: agents/pch-helper.md
        note: "Simplest agent (272 lines); good template for standalone agents; hub handoff pattern"
      - path: agents/pch-coder.md
        note: "Best example of terminal interaction (git checks); subagent orchestration patterns"
      - path: agents/pch-visionary.md
        note: "Most comprehensive Q&A flow; adaptive user handling patterns"
      - path: agents/pch-researcher.md
        note: "Subagent orchestration patterns; self-handoff for continuation"
      - path: agents/pch-planner.md
        note: "MCP standards integration; question format templates"
      - path: agents/pch-plan-reviewer.md
        note: "Auto-send handoff pattern (send: true)"
    external_sources: null
    gaps: null
    assumptions:
      - "Hugh agent will be standalone without handoffs to other pch-* agents (based on vision document)"
      - "Hugh agent won't need documentation-only constraint (it actively executes terminal commands)"
      - "Hugh doesn't need subagent orchestration (single-purpose CLI execution)"

  - phase: 2
    name: "Hugh CLI Complete Command & Output Reference"
    status: complete
    scope:
      - "What are all Hugh CLI commands, flags, and their expected outputs (new, --status, --resume, doctor, doctor --fix, --version, --check, --verbose, --timeout)?"
      - "What are the common error scenarios and their resolutions?"
      - "What are all exit codes, error messages, and output formats from Hugh?"
      - "What are the exact terminal output patterns for each Hugh command?"
      - "What does Hugh's appsettings.json control and what are the defaults?"
      - "What prerequisites does Hugh require and how does hugh doctor detect them?"
    session: "2026-02-10"
    findings: |
      ## Hugh CLI Commands and Flags

      ### Root Command Options

      | Option | Alias | Description | Example |
      |--------|-------|-------------|---------|
      | <feature> (argument) | - | The feature request to implement | hugh "Add authentication" |
      | --resume [runId] | -r | Resume a paused or failed run | hugh --resume or hugh --resume abc123 |
      | --status | -s | Show current run state | hugh --status |
      | --check | -c | Check prerequisites (alias for doctor) | hugh --check |
      | --verbose | -v | Enable debug logging | hugh --verbose |
      | --timeout <duration> | -t | Override default timeout (HH:mm:ss) | hugh --timeout 00:30:00 |

      ### Doctor Subcommand

      hugh doctor [options]

      | Option | Alias | Description |
      |--------|-------|-------------|
      | --fix | -f | Attempt to install missing prerequisites |
      | --no-interactive | -n | Disable interactive prompts (CI/CD mode) |

      Doctor detects prerequisites in priority order:
      agents-folder (10), workspace (20), github-cli (30), copilot-cli (40), github-auth (50)
      With --fix: Prompts to install missing via winget or shows manual instructions
      With --no-interactive --fix: Auto-installs without prompts

      ## Exit Codes

      | Exit Code | Condition | Source |
      |-----------|-----------|--------|
      | 0 | Success / All prerequisites satisfied | Program.cs |
      | 1 | Prerequisites not satisfied | Program.cs |
      | 1 | Fatal exception during execution | Program.cs |

      ## Configuration (appsettings.json)

      | Setting | Default | Purpose |
      |---------|---------|---------|
      | AgentsPath | ./agents | Relative path to agent prompts |
      | DocsPath | ./docs | Relative path to documentation output |
      | OrchestratorPath | ./.orchestrator | State persistence directory |
      | Timeouts.VisionPhase | 00:10:00 | Vision phase timeout |
      | Timeouts.ResearchPhase | 00:30:00 | Research phase timeout |
      | Timeouts.PlanningPhase | 00:20:00 | Planning phase timeout |
      | Timeouts.ReviewPhase | 00:15:00 | Review phase timeout |
      | Timeouts.CodingPhasePerPhase | 00:20:00 | Coding sub-phase timeout |
      | Autonomous.Enabled | true | Enable autonomous mode |
      | Autonomous.AcceptRecommendationsOnly | true | Only auto-accept when recommendation provided |
      | Copilot.Model | claude-sonnet-4-5 | AI model for agent interactions |
      | Copilot.MaxRetryAttempts | 3 | Retry count for failures |
      | Copilot.RetryBaseDelayMs | 2000 | Base delay for exponential backoff |
      | Copilot.CompactionThreshold | 0.8 | Context utilization threshold |

      Path resolution order (PathResolver.cs):
      1. HUGH_CONFIG_PATH environment variable
      2. Assembly location (for installed global CLI)
      3. Current working directory (dev mode fallback)

      User data paths:
      - ~/.hugh/ — User data directory
      - ~/.hugh/logs/ — Log files (daily rolling)

      ## Prerequisite Detection

      | Priority | ID | Display Name | Installable | Detection Method |
      |----------|-----|--------------|-------------|------------------|
      | 10 | agents-folder | PCH Agents Folder | Yes | Directory exists with pch-*.md files |
      | 20 | workspace | Workspace Write Access | No | Create/delete test file in .orchestrator/ |
      | 30 | github-cli | GitHub CLI | Yes | gh --version exit code 0 |
      | 40 | copilot-cli | GitHub Copilot CLI | Yes | copilot --version exit code 0 |
      | 50 | github-auth | GitHub Authentication | No | GH_TOKEN/GITHUB_TOKEN env var OR gh auth status |

      PrerequisiteState enum values: Satisfied, Missing, Outdated, Misconfigured

      Expected agent files: pch-coder, pch-helper, pch-plan-reviewer, pch-planner,
      pch-researcher, pch-visionary

      ## Console Output Patterns

      ### Header
      Unicode box-drawing characters with centered title:
      "Hugh - Self-Operating Dev Loop" / "Freed from constant interaction"

      ### Status Icons

      | Status | Icon | Color |
      |--------|------|-------|
      | Complete | checkmark | Green |
      | In Progress | spinner | Yellow |
      | Failed | cross | Red |
      | Paused | pause | DarkYellow |
      | Not Started | hourglass | DarkGray |
      | Info | info | Blue |
      | Warning | warning | Yellow |
      | Hint | arrow | DarkGray |

      ### Phase Status Format
      Starting: arrow + "Starting {Phase} phase" with agent name
      Complete: checkmark + "{Phase} phase complete ({duration}s)" with output path
      Failed: cross + "{Phase} phase failed" with error

      ### Run Status Display
      Formatted table showing Run ID, Started, Feature, Status, Current Phase,
      Coding Sub-Phase, phase status icons with document paths, and decision count

      ### Prerequisite Report
      Formatted list with checkmark/cross per prerequisite, version info,
      and hint arrows for resolution suggestions

      ## Signal Detection Patterns

      SignalClassifier detects these signal types from agent output:

      | Signal Type | Pattern | Action |
      |-------------|---------|--------|
      | PhaseComplete | Phase [N] Complete | Advance to next phase |
      | WorkflowComplete | Research Complete, All Phases Complete | Transition to next agent |
      | QuestionWithRecommendation | Recommendation: A — Option | Auto-select in autonomous mode |
      | QuestionOpen | How would you like to proceed? | Pause for human input |
      | Blocker | Blocked, Blocker | Pause and alert user |
      | ModeSelection | Execution Mode Selection | Auto-respond "1" |
      | NeedsUserInput | STATUS: needs_user_input + QUESTION_FOR_USER: | Display question, collect input |

      ## Error Scenarios and Resolutions

      ### "Copilot CLI not found"
      Resolution: winget install GitHub.Copilot or npm install -g @github/copilot
      Verify: copilot --version

      ### "Authentication required" / "401 Unauthorized"
      Resolution: Run copilot then /login, or set GH_TOKEN env var

      ### "Rate limit exceeded"
      Resolution: Wait before retrying, increase RetryBaseDelayMs, reduce prompt complexity

      ### "Context window exceeded"
      Resolution: Lower CompactionThreshold (e.g., 0.6), start new session, break phases smaller

      ### "hugh: command not found"
      Resolution: Open new terminal for PATH refresh, check PATH, reinstall

      ### "Configuration Not Found"
      Resolution: Check HUGH_CONFIG_PATH, ensure appsettings.json exists, set custom path

      ## State Persistence

      State persisted to .orchestrator/loop-state.json with fields:
      runId, startedAt, featureRequest, currentPhase, currentCodingSubPhase,
      completedPhases, documents (vision/research/plan paths), decisions, status, failureReason

      ## Phase Execution Flow

      Vision -> Research -> Planning -> Review -> Coding (sub-phases)

      Each phase: check prerequisites -> update state InProgress ->
      build prompt with documents -> execute via CopilotService ->
      on success: update Complete with output -> on failure: update Failed with reason

      Coding phase special handling: loops through plan phases via PlanPhaseParser,
      each sub-phase is separate execution, 2-second delay between sub-phases
    key_discoveries:
      - "Hugh uses System.CommandLine with feature argument, --resume, --status, --check, --verbose, --timeout plus doctor subcommand"
      - "Exit codes: 0 = success, 1 = failure (prerequisites, exceptions, or phase failure)"
      - "Prerequisites detected in priority order: agents-folder -> workspace -> github-cli -> copilot-cli -> github-auth"
      - "Configuration uses HUGH_CONFIG_PATH env var -> assembly location -> CWD fallback"
      - "Logs written to ~/.hugh/logs/ with daily rolling"
      - "State persisted to .orchestrator/loop-state.json for resumability"
      - "SignalClassifier uses compiled regex patterns to detect signals from agent output"
      - "Autonomous mode auto-accepts recommendations but pauses for open questions"
      - "Doctor command can auto-fix prerequisites using winget installer"
      - "ConsoleOutput uses Unicode symbols and color-coded output for status display"
    relevant_files:
      - path: Hugh/Program.cs
        note: "CLI command definitions, argument parsing, handler methods"
      - path: Hugh/appsettings.json
        note: "Configuration defaults for timeouts, paths, Copilot settings"
      - path: Hugh/ConsoleOutput.cs
        note: "All terminal output formatting, status display, phase summaries"
      - path: Hugh/Configuration/HughConfig.cs
        note: "Strongly-typed configuration classes"
      - path: Hugh/Configuration/PathResolver.cs
        note: "Installation path and log path resolution"
      - path: Hugh/Setup/SetupOrchestrator.cs
        note: "Prerequisite detection and installation workflow"
      - path: Hugh/Setup/Detection/GitHubCliDetector.cs
        note: "GitHub CLI detection via gh --version"
      - path: Hugh/Setup/Detection/CopilotCliDetector.cs
        note: "Copilot CLI detection via copilot --version"
      - path: Hugh/Setup/Detection/GitHubAuthDetector.cs
        note: "Auth detection via env vars or gh auth status"
      - path: Hugh/Setup/Detection/AgentsFolderDetector.cs
        note: "PCH agent file detection"
      - path: Hugh/Setup/Detection/WorkspaceDetector.cs
        note: "Workspace write access verification"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "Agent output signal detection patterns"
      - path: Hugh/Models/LoopState.cs
        note: "Run state persistence model with enums"
      - path: Hugh/Orchestration/LoopOrchestrator.cs
        note: "Main orchestration loop, phase execution"
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "Question and recommendation auto-handling"
      - path: Hugh/README.md
        note: "User documentation, usage examples, troubleshooting"
    external_sources: null
    gaps: null
    assumptions: null

  - phase: 3
    name: "CLI-Based Installation Approach for Agent Orchestration"
    status: complete
    scope:
      - "What are the limitations of the current Inno Setup GUI installer for AI agent orchestration via run_in_terminal?"
      - "What CLI-native installation approaches are available (dotnet tool install, winget package, PowerShell install script, self-extracting archive with CLI flags)?"
      - "What does Inno Setup's /SILENT and /VERYSILENT mode actually support — can an agent fully automate install/uninstall/upgrade with it?"
      - "Would a 'hugh install' / 'hugh uninstall' self-management command be viable (like rustup or nvm)?"
      - "What PATH management implications exist for each approach?"
      - "How do comparable CLI tools (gh CLI, dotnet, node/nvm, rustup) handle their own installation and self-update?"
      - "What approach gives the @hugh agent the most control over install/uninstall/upgrade with clear terminal output and exit codes?"
      - "Should the Inno Setup installer be kept for manual/enterprise use alongside a new CLI-based mechanism, or replaced entirely?"
    session: "2026-02-10"
    findings: |
      ## Current Inno Setup Installer Analysis

      ### Installer Capabilities (hugh-setup.iss)

      | Feature | Support | Details |
      |---------|---------|---------|
      | Silent install | Yes | /SILENT (progress visible), /VERYSILENT (no UI) |
      | Silent uninstall | Yes | unins000.exe /SILENT or /VERYSILENT |
      | Custom install path | Yes | /DIR="C:\CustomPath\Hugh" |
      | Suppress all prompts | Yes | /SUPPRESSMSGBOXES |
      | Logging | Yes | /LOG or /LOG="filename" |
      | Prevent restart | Yes | /NORESTART |
      | PATH management | Yes | User-level (HKCU), no admin required |
      | In-place upgrade | Yes | Same AppId allows upgrade over existing |
      | Exit codes | Yes | 0=success, 1-5=various errors |
      | Non-admin install | Yes | PrivilegesRequired=lowest |

      ### Silent Mode Command Examples

      Install: hugh-setup-1.0.0.exe /VERYSILENT /SUPPRESSMSGBOXES /NORESTART
      With custom location: /DIR="C:\Tools\Hugh"
      With logging: /LOG="C:\temp\hugh-install.log"
      Uninstall: "$env:LOCALAPPDATA\Programs\Hugh\unins000.exe" /VERYSILENT /SUPPRESSMSGBOXES

      ### Limitations for Agent Orchestration

      | Limitation | Impact |
      |------------|--------|
      | Two-file distribution | Agent must download .exe, run it, verify |
      | Version in filename | Agent must know version to construct URL |
      | Uninstaller path varies | Depends on install location |
      | No self-update | Upgrade requires new installer download |
      | Download requires GitHub API | Must fetch release info for URL |

      Verdict: Viable with effort. Can be fully automated but complex.

      ## CLI-Native Installation Approaches

      ### Approach A: dotnet tool install (Global Tool)

      Install: dotnet tool install -g hugh --version 1.0.0
      Update: dotnet tool update -g hugh
      Uninstall: dotnet tool uninstall -g hugh

      Assessment: Cleanest agent experience. Requires .NET runtime or self-contained NuGet.
      Automatic PATH on Windows. Native cross-platform support.

      ### Approach B: winget Package

      Install: winget install --id PCHDevelopment.Hugh --silent --accept-package-agreements
      Update: winget upgrade --id PCHDevelopment.Hugh --silent
      Uninstall: winget uninstall --id PCHDevelopment.Hugh --silent

      Assessment: Excellent agent automation. Windows only. Requires 1-2 week approval
      per release. Wraps existing Inno Setup installer.

      ### Approach C: PowerShell Install Script

      Install: irm https://github.com/.../Install-Hugh.ps1 | iex
      Or: Install-Hugh.ps1 -Version "1.0.0"
      Uninstall: Install-Hugh.ps1 -Uninstall
      Upgrade: Install-Hugh.ps1 -Upgrade

      Assessment: Full agent control, immediate deployment, no approval process.
      Works with existing portable ZIP. PowerShell execution policy may block.

      ### Approach D: Self-Extracting Archive

      Not recommended. Lower control than PowerShell, more complex than portable ZIP.

      ### Approach E: Self-Management (rustup pattern)

      Bootstrap: irm https://hugh.dev/install.ps1 | iex
      Self-upgrade: hugh upgrade
      Self-uninstall: hugh uninstall

      Assessment: Optimal long-term experience. Non-trivial to implement (cannot
      replace running binary on Windows). Rust, gh CLI follow this pattern.

      ## Comparable CLI Tool Patterns

      | Tool | Initial Install | Self-Update | Package Managers |
      |------|-----------------|-------------|------------------|
      | gh CLI | Various | gh upgrade | winget, choco, scoop |
      | dotnet | SDK installer | External | winget |
      | rustup | curl pipe sh | rustup update | None needed |
      | nvm-windows | Installer | Manual (limitation) | scoop |

      ## PATH Management Implications

      All approaches that modify PATH require a new terminal session.
      Agent must either: instruct user to open new terminal, or use full path
      ($env:LOCALAPPDATA\Programs\Hugh\Hugh.exe) until terminal restart.

      ## Trade-off Analysis

      | Criterion | Inno Setup Silent | dotnet tool | winget | PS Script | Self-Mgmt |
      |-----------|-------------------|-------------|--------|-----------|-----------|
      | Agent simplicity | Medium | High | High | High | Highest |
      | Time to implement | Already exists | 2-3 days | 1-2 wks | 1 day | 3-5 days |
      | Release speed | Immediate | Immediate | 1-2 wks | Immediate | Immediate |
      | Prerequisite | None | .NET runtime | winget | PowerShell | Bootstrap |
      | Cross-platform | No | Yes | No | Partial | Yes |
      | Enterprise | Yes | Partial | Yes | Medium | Medium |

      ## Recommendation: Hybrid Approach

      1. Keep Inno Setup installer: Enterprise/GPO/offline deployment, already works
      2. Create Install-Hugh.ps1 script: Simpler agent orchestration, no approval delays
      3. Add 'hugh upgrade' command: Long-term best agent/user experience
      4. Do NOT pursue winget submission yet: Approval delays not justified at current scale

      ### Agent Installation Flow

      1. Detection: where.exe hugh, check exit code
      2. Version check: hugh --version, compare to latest
      3. Installation: PowerShell script (preferred) or silent Inno Setup (fallback)
      4. PATH handling: Inform user to open new terminal, or use full path
      5. Verification: Hugh.exe --version using full path
      6. Upgrade (future): hugh upgrade
    key_discoveries:
      - "Inno Setup /VERYSILENT /SUPPRESSMSGBOXES /NORESTART provides full CLI automation with exit codes"
      - "Agent CAN automate current installer, but download + uninstaller-path-detection adds complexity"
      - "dotnet tool install offers cleanest agent experience but requires .NET runtime"
      - "PowerShell install script offers immediate deployment with full agent control and no approval process"
      - "Self-management pattern (hugh upgrade) is optimal long-term but requires implementation effort"
      - "PATH changes always require new terminal session — agent must handle explicitly"
      - "winget submission takes 1-2 weeks per release — not justified at current scale"
      - "gh CLI has self-update (gh upgrade); rustup is fully self-managing; nvm-windows lacks it and it's a limitation"
      - "Recommended hybrid: keep Inno Setup + add PowerShell script + future self-management commands"
    relevant_files:
      - path: installer/hugh-setup.iss
        note: "Current installer with /SILENT support, PATH management, non-admin capable"
      - path: installer/README.md
        note: "Documents silent install/uninstall commands and testing procedures"
      - path: .github/workflows/release.yml
        note: "Builds Inno Setup installer and portable ZIP for GitHub Releases"
      - path: Hugh/Hugh.csproj
        note: ".NET 8 self-contained single-file configuration; potential dotnet tool candidate"
      - path: Hugh/Program.cs
        note: "CLI structure via System.CommandLine; location for future install/upgrade commands"
      - path: Hugh/Setup/Installation/WingetInstaller.cs
        note: "Existing winget integration for prerequisites — pattern for self-install"
      - path: Hugh/Configuration/PathResolver.cs
        note: "Path resolution for installed vs. dev mode — relevant for self-update"
    external_sources:
      - url: https://jrsoftware.org/ishelp/index.php?topic=setupcmdline
        note: "Inno Setup command line parameters (/SILENT, /VERYSILENT, exit codes)"
      - url: https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-tool-install
        note: "dotnet tool install documentation"
      - url: https://learn.microsoft.com/en-us/windows/package-manager/winget/install
        note: "winget install command documentation"
      - url: https://rustup.rs/
        note: "Rustup self-management installation pattern"
    gaps: null
    assumptions:
      - "Agent will primarily target Windows initially (macOS is future per vision)"
      - "PowerShell 5.1 (Windows built-in) is acceptable minimum for install script"
      - "Users have internet access to GitHub for downloads"
      - "Enterprise users needing offline/GPO deployment will continue using Inno Setup"

overview: |
  This research provides the complete foundation for building the @hugh Copilot
  agent prompt. Three areas were investigated: existing agent conventions, the
  full Hugh CLI reference, and installation approaches suitable for AI agent
  orchestration.

  **Agent Prompt Conventions:** All six existing pch-* agents share an identical
  YAML front matter schema (name, description, model, handoffs) and follow a
  consistent 10-section instruction structure. The hugh agent should be standalone
  (no workflow handoffs), use the same front matter format, and target 400-600
  lines. Terminal-oriented patterns from pch-coder (detect state first, interpret
  output, provide actionable guidance) directly apply.

  **Hugh CLI Reference:** Hugh uses System.CommandLine with a feature argument,
  --resume, --status, --check, --verbose, and --timeout flags, plus a doctor
  subcommand with --fix and --no-interactive options. Exit codes are binary
  (0=success, 1=failure). Five prerequisites are checked in priority order.
  Configuration lives in appsettings.json with path resolution fallback chain.
  Console output uses Unicode symbols and color-coded status display. The agent
  prompt must embed this entire reference for FR-8 compliance.

  **Installation Approach:** The current Inno Setup installer fully supports
  silent automation (/VERYSILENT /SUPPRESSMSGBOXES /NORESTART), making it
  usable by the agent today. However, the download-install-verify workflow is
  complex. A PowerShell install script would simplify agent orchestration with
  no approval delays. Long-term, self-management commands (hugh upgrade) modeled
  after rustup would provide the cleanest experience. The recommendation is a
  hybrid: keep Inno Setup for enterprise, add a PowerShell script for agent use,
  and plan self-management commands as a future enhancement.

key_findings:
  - "All pch-* agents share identical YAML front matter (name, description, model, handoffs, optional mcp-servers) — hugh agent must follow the same schema"
  - "Agent prompts range 272-1314 lines; a CLI-focused agent like hugh should target 400-600 lines (15-25 KB)"
  - "Terminal-oriented agent pattern: detect state first, acknowledge findings, interpret output, provide actionable guidance"
  - "Hugh CLI uses System.CommandLine with binary exit codes (0=success, 1=failure) and 5 prerequisite checks"
  - "SignalClassifier detects phase completion, blockers, questions, and needs_user_input signals via compiled regex"
  - "Inno Setup /VERYSILENT mode fully supports agent automation but download workflow adds complexity"
  - "PowerShell install script is recommended for agent orchestration — immediate deployment, full control, no approval delays"
  - "Self-management commands (hugh upgrade/uninstall) are optimal long-term, modeled after rustup/gh patterns"
  - "PATH changes always require new terminal session — agent must use full path or instruct terminal restart"
  - "Configuration uses 3-level fallback: HUGH_CONFIG_PATH env var -> assembly location -> CWD"

actionable_conclusions:
  - "Create /agents/hugh.md with standard YAML front matter (name: hugh, model: Claude Opus 4.5, no handoffs or minimal pch-helper handoff)"
  - "Embed complete Hugh CLI command reference in the agent prompt for self-contained knowledge (FR-8, NFR-2)"
  - "Include installation guidance for both Inno Setup silent mode and portable ZIP extraction (FR-2)"
  - "Build detection-first workflow: check installation -> check version -> check prerequisites -> execute commands"
  - "Include full error scenario list with resolutions directly in the prompt"
  - "Handle PATH issues by using full executable path ($env:LOCALAPPDATA\Programs\Hugh\Hugh.exe) for verification after install"
  - "For initial release, the agent should guide users through current installation options (installer or portable ZIP)"
  - "Future enhancement: create Install-Hugh.ps1 script and later add hugh upgrade/uninstall self-management commands"

open_questions:
  - "Should the agent offer to modify appsettings.json or keep configuration as a non-goal per vision?"
  - "Should Install-Hugh.ps1 be created before or after the agent prompt (agent can work without it using Inno Setup silent mode)?"
  - "Should the agent have a handoff to pch-helper or be entirely standalone?"

standards: null

handoff:
  created_by: pch-researcher
  created_date: "2026-02-10"
  status: complete
  current_phase: complete
  path: /docs/research/013-hugh-copilot-agent-prompt.md
