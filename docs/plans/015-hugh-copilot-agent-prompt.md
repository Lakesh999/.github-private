id: "015"
type: plan
title: "Hugh Copilot Agent Prompt"
status: complete
created: "2026-02-10"
updated: "2026-02-10"
owner: pch-planner
version: v1.1

versions:
  - version: "v1.0"
    date: "2026-02-10"
    author: pch-planner
    changes: "Complete plan with 7 decisions, technical design, dependencies, risks, 1 phase with 9 tasks, and holistic review"
  - version: "v1.1"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review corrections: fixed output status icons table to match ConsoleOutput.cs, added --version/--help to global options, clarified copilot vs gh copilot distinction in troubleshooting, added review session log and summary"

introduction: |
  This plan covers the creation of a new GitHub Copilot custom agent prompt
  file at /agents/hugh.md that enables developers to install, run, and
  monitor the Hugh CLI directly from VS Code's Copilot chat. The agent
  wraps all Hugh CLI operations (new feature, status, resume, doctor) with
  AI-guided interpretation of output, installation guidance, and
  troubleshooting support.

  Source vision: /docs/vision/004-hugh-copilot-agent.md
  Source research: /docs/research/013-hugh-copilot-agent-prompt.md

decisions:
  - session: 1
    date: "2026-02-10"
    decision_point: "How should the Hugh agent prompt be organized into instruction sections?"
    answer: "B — Adapted CLI-Focused Structure (7 Sections)"
    rationale: "Hugh is a standalone CLI execution agent. The full 10-section pattern includes irrelevant sections (Document Format, Subagent Behavior). A 7-section adaptation preserves the recognizable pattern while replacing irrelevant sections with CLI-specific ones (command reference, error reference). Keeps prompt within 400-600 line target."
  - session: 1
    date: "2026-02-10"
    decision_point: "Should the Hugh agent include any handoffs in its YAML front matter?"
    answer: "B — Minimal Handoff to pch-helper"
    rationale: "A single optional handoff to pch-helper (send: false) provides a safety net for users who need workflow guidance without coupling Hugh into the document-generation chain. Adds minimal complexity. Bidirectional approach deferred as a trivial follow-up."
  - session: 1
    date: "2026-02-10"
    decision_point: "How much installation detail should the agent prompt embed for FR-2?"
    answer: "A — Full Embedded Instructions for Both Methods"
    rationale: "NFR-2 requires comprehensive knowledge with minimal external doc fallback. Installation is a core use case. Both methods are 4-5 steps each (~60-80 lines total), well within the 400-600 line budget. External doc references for basic installation would undermine the agent's value proposition."
  - session: 1
    date: "2026-02-10"
    decision_point: "How should the embedded CLI command reference be structured within the agent prompt?"
    answer: "B — Command-by-Command Blocks"
    rationale: "Each command gets a self-contained block with syntax, flags, expected output patterns, and examples. Gives the agent maximum context for constructing commands (FR-3/5/6), interpreting output (FR-7), and answering flag questions (FR-8). Extra ~40 lines over tables is worthwhile for output pattern coverage."
  - session: 1
    date: "2026-02-10"
    decision_point: "What should the agent do automatically when a user first invokes @hugh?"
    answer: "C — Detect Once, Then Cache in Conversation"
    rationale: "Detection adds terminal round-trips. Running every message wastes time; skipping detection risks confusing errors. Detect on first CLI-relevant interaction, remember the result, re-detect only if something changes or an error suggests state changed. Matches pch-coder's detect-first pattern without over-detecting."
  - session: 1
    date: "2026-02-10"
    decision_point: "How should the agent determine the correct download URL for Hugh releases?"
    answer: "B — Dynamic GitHub API Fetch"
    rationale: "Core value of @hugh is eliminating manual steps. Agent can run Invoke-RestMethod against GitHub Releases API to get exact asset URLs, then download via Invoke-WebRequest for a fully automated install flow. GitHub API is unauthenticated for public repos. Static URL pattern (C) serves as fallback if API fails."
  - session: 1
    date: "2026-02-10"
    decision_point: "How deeply should the agent prompt embed Hugh's terminal output patterns for interpretation (FR-7)?"
    answer: "B — Comprehensive Output Taxonomy"
    rationale: "Include every output pattern from ConsoleOutput.cs: status icons, phase messages, run status table fields, prerequisite report format, and signal types. Enables the agent to interpret any Hugh output it encounters. While adding ~80-100 lines, it ensures FR-7 and FR-8 are fully covered and the agent can explain both success and error output."
  - session: 1
    date: "2026-02-10"
    decision_point: "Should the agent explain and help modify appsettings.json settings?"
    answer: "B — Guided Single-Setting Edits"
    rationale: "The agent can explain all settings (FR-8) AND offer to open/edit appsettings.json for simple single-setting changes. Refuses complete config generation or sweeping multi-setting changes. Pragmatic middle ground — 'deep config authoring' non-goal means complex multi-setting scenarios, not simple timeout adjustments."

holistic_review:
  date: "2026-02-10"
  reviewer: pch-planner
  findings:
    - area: "Requirements Coverage"
      status: complete
      notes: "All 8 functional requirements (FR-1 through FR-8) and both non-functional requirements (NFR-1, NFR-2) are mapped to specific tasks. Every requirement appears in at least 2 tasks (definition + usage)."
    - area: "Decision Consistency"
      status: complete
      notes: "All 7 decisions are reflected in the technical design and task details. Comprehensive output taxonomy (decision 7) extends prompt to upper line range (~550). Guided config edits (decision 8) are bounded by Critical Rules in T1.4."
    - area: "Risk Coverage"
      status: complete
      notes: "4 risks identified with mitigations. RISK-2 (prompt size) is the most likely given comprehensive taxonomy choice — mitigated by trimming redundancy between CLI reference and workflow instructions during T1.9."
    - area: "Dependencies"
      status: complete
      notes: "All 6 dependencies are reference-only (existing files) or external (GitHub URL). No blocking dependencies. All source files verified to exist."
    - area: "Task Sequencing"
      status: complete
      notes: "Single phase with 9 sequential tasks. T1.1 (front matter) must be first. T1.9 (final review) must be last. T1.2-T1.8 can be written in order as sections of the same file. No parallelism needed — all tasks modify the same output file."
    - area: "Scope Boundaries"
      status: complete
      notes: "Out-of-scope items clearly defined: no source code modification, no agent replacement, no macOS support, no deep config authoring. Guided single-setting edits are explicitly bounded in Critical Rules."
  overall_assessment: "Plan is complete and ready for review. Single-phase delivery of one Markdown file with 9 well-specified tasks. All requirements covered. Line count target of 450-550 is achievable given section estimates total ~360-470 lines of content plus headings and whitespace."

overview:
  feature: "Hugh Copilot Agent Prompt (@hugh)"
  objectives:
    - "Create a standalone Copilot agent prompt at /agents/hugh.md"
    - "Enable installation detection, guided install, and verification of Hugh CLI"
    - "Support all Hugh CLI commands: feature requests, status, resume, doctor"
    - "Embed complete CLI reference and troubleshooting knowledge in the prompt"
    - "Follow existing pch-* agent conventions (YAML front matter, instruction structure)"
  success_criteria:
    - "Agent prompt file exists at /agents/hugh.md with valid YAML front matter"
    - "Agent can detect Hugh installation via run_in_terminal"
    - "Agent can guide installation from GitHub Releases"
    - "Agent can construct and execute all Hugh CLI commands"
    - "Agent contains troubleshooting knowledge for common errors"
    - "Agent prompt follows existing pch-* conventions"

requirements:
  functional:
    - id: FR-1
      requirement: "Detect whether Hugh is installed by checking PATH and reporting version via hugh --version"
      priority: must-have
    - id: FR-2
      requirement: "Guide user through installing Hugh from GitHub Releases (installer or portable ZIP) and verify installation"
      priority: must-have
    - id: FR-3
      requirement: "Construct and execute 'hugh <feature description>' in a terminal to start a new feature run"
      priority: must-have
    - id: FR-4
      requirement: "Execute 'hugh --status' and interpret the output to report run state to the user"
      priority: must-have
    - id: FR-5
      requirement: "Execute 'hugh --resume [runId]' to resume a paused or failed run when requested"
      priority: must-have
    - id: FR-6
      requirement: "Execute 'hugh doctor' and 'hugh doctor --fix' to check and fix prerequisites, interpreting results"
      priority: must-have
    - id: FR-7
      requirement: "Parse and interpret Hugh terminal output to provide actionable guidance on errors, progress, and phase transitions"
      priority: must-have
    - id: FR-8
      requirement: "Answer questions about Hugh CLI flags, options, configuration, and troubleshooting"
      priority: must-have
  non_functional:
    - id: NFR-1
      requirement: "Agent must provide correct CLI commands — wrong guidance is worse than no guidance"
      priority: must-have
    - id: NFR-2
      requirement: "Agent prompt must contain comprehensive Hugh CLI knowledge so it rarely needs external docs"
      priority: must-have
  out_of_scope:
    - "Modifying Hugh source code"
    - "Replacing other PCH agents"
    - "Deep appsettings.json configuration authoring"
    - "macOS installation guidance (Hugh macOS build not yet available)"

technical_design:
  architecture:
    style: "Single Markdown agent prompt file at /agents/hugh.md"
    integration: "run_in_terminal tool for CLI execution; output capture for interpretation"
  prompt_structure:
    sections:
      - section: 1
        name: "Role Definition"
        purpose: "Identity statement and primary function description"
        estimated_lines: "10-15"
        content_guidance: |
          - Opening: "You are a CLI assistant that helps developers install, run, and monitor Hugh..."
          - Mention Hugh's purpose: self-operating development loop orchestrating PCH agents
          - Star Trek origin reference for personality context
          - Clarify scope: CLI operations only, not source code modification or agent replacement
      - section: 2
        name: "Core Responsibilities"
        purpose: "Bulleted list of primary duties (detect, install, run, monitor, troubleshoot)"
        estimated_lines: "15-20"
        content_guidance: |
          - Detect Hugh installation and version
          - Guide installation from GitHub Releases (installer or portable ZIP)
          - Construct and execute Hugh CLI commands
          - Check run status and interpret output
          - Resume paused or failed runs
          - Run prerequisite checks (doctor)
          - Interpret terminal output and provide actionable guidance
          - Answer CLI/configuration questions
          - Perform simple single-setting config edits when requested
      - section: 3
        name: "Critical Rules"
        purpose: "Detection-first workflow, safety constraints, PATH handling, config edit boundaries"
        estimated_lines: "30-40"
        content_guidance: |
          - Detection-first: always check Hugh installation before attempting CLI execution
          - Detect once per conversation, re-detect only on error or explicit request
          - PATH handling: after install, use full path for verification; advise new terminal for PATH
          - Config boundary: can edit single settings in appsettings.json; refuse full config generation
          - Never modify Hugh source code or other agent prompts
          - Always show the exact command before executing it
          - Use PowerShell syntax (Windows-first)
      - section: 4
        name: "Session Initialization"
        purpose: "Decision tree on first CLI-relevant interaction"
        estimated_lines: "25-35"
        content_guidance: |
          - If user asks a knowledge question (flags, config, concepts): answer directly, no detection needed
          - If user requests CLI execution: run detection first
          - Detection sequence: Get-Command hugh → hugh --version → report findings
          - If not installed: offer installation guidance (FR-2)
          - If installed: proceed with requested operation
          - Cache detection result for remainder of conversation
      - section: 5
        name: "CLI Command Reference"
        purpose: "Complete embedded reference using command-by-command blocks"
        estimated_lines: "120-150"
        content_guidance: |
          Command blocks for each operation (syntax, flags, expected output, examples):
          - hugh "<feature>" — Start new feature run
          - hugh --status / -s — Show current run state
          - hugh --resume [runId] / -r — Resume paused/failed run
          - hugh --verbose / -v — Enable debug logging
          - hugh --timeout <HH:mm:ss> / -t — Override timeout
          - hugh doctor — Check prerequisites
          - hugh doctor --fix / -f — Install missing prerequisites
          - hugh doctor --no-interactive / -n — CI/CD mode
          - Exit codes: 0 = success, 1 = failure
          - Configuration reference: all appsettings.json keys with defaults and effects
          - Prerequisite list: agents-folder, workspace, github-cli, copilot-cli, github-auth
          - Output patterns: status icons (✓ ✗ ⏸ ⏳ ℹ ⚠ →), phase status format, run status table
      - section: 6
        name: "Workflow Instructions"
        purpose: "Step-by-step guides for key user scenarios"
        estimated_lines: "100-130"
        content_guidance: |
          Workflow A — Install Hugh:
            1. Check current installation (Get-Command hugh)
            2. If not found: query GitHub Releases API (Invoke-RestMethod)
            3. Download installer or portable ZIP (Invoke-WebRequest)
            4. Run installer (/VERYSILENT /SUPPRESSMSGBOXES /NORESTART) or extract ZIP
            5. For ZIP: add to PATH
            6. Verify: use full path first, advise new terminal for PATH
            7. Fallback: if API/download fails, provide direct URL to releases page

          Workflow B — Run a Feature:
            1. Ensure Hugh is installed (detection cache)
            2. Confirm feature description with user
            3. Construct command: hugh "<feature description>"
            4. Optionally add flags (--verbose, --timeout)
            5. Execute in terminal
            6. Interpret initial output (phase starting messages)

          Workflow C — Check Status:
            1. Run: hugh --status
            2. Interpret run status table fields
            3. Report: run ID, feature, current phase, status, any blockers

          Workflow D — Resume a Run:
            1. Optionally check status first to confirm resumable state
            2. Run: hugh --resume [runId]
            3. Interpret output for phase continuation

          Workflow E — Prerequisites (Doctor):
            1. Run: hugh doctor
            2. Interpret prerequisite report (✓/✗ per prerequisite)
            3. If issues found: offer hugh doctor --fix
            4. Interpret fix results

          Workflow F — Configuration Edit:
            1. User requests a setting change
            2. Explain the setting's purpose and current default
            3. Locate appsettings.json (PathResolver logic)
            4. Edit the single setting value
            5. Advise restart of Hugh if currently running
      - section: 7
        name: "Error Reference & Troubleshooting"
        purpose: "Common errors, resolutions, and diagnostic procedures"
        estimated_lines: "60-80"
        content_guidance: |
          Error categories with resolution steps:
          - "hugh: command not found" → PATH issue, new terminal, reinstall
          - "Copilot CLI not found" / "JSON-RPC connection lost" → winget install GitHub.Copilot
          - "Authentication required" / "401 Unauthorized" → copilot /login or GH_TOKEN
          - "Rate limit exceeded" → wait, increase RetryBaseDelayMs
          - "Context window exceeded" → lower CompactionThreshold, new session
          - "Configuration Not Found" → check HUGH_CONFIG_PATH, appsettings.json location
          - Process hangs/timeouts → --timeout flag, phase timeout settings
          - Credential detection fails → gh auth login, check copilot extension
          - Logs not created → check ~/.hugh/logs/ directory exists
    target_line_count: "450-550 lines"
  codebase_patterns:
    - pattern: "YAML Front Matter"
      location: "agents/pch-*.md (all agent files)"
      usage: "hugh.md will use identical schema: name, description, model, handoffs"
    - pattern: "Terminal Interaction"
      location: "agents/pch-coder.md (git initialization check)"
      usage: "Hugh detection workflow follows same detect-then-act pattern"
    - pattern: "Standalone Agent"
      location: "agents/pch-helper.md"
      usage: "Hugh follows pch-helper's standalone pattern with minimal handoffs"
  component_specifications:
    yaml_front_matter:
      name: hugh
      description: "Helps developers install, run, and monitor the Hugh CLI directly from VS Code Copilot chat"
      model: "Claude Opus 4.5"
      handoffs:
        - label: "Get Workflow Help"
          agent: pch-helper
          prompt: "Help the user understand the PCH agent workflow"
          send: false

dependencies:
  - id: DEP-1
    dependency: "Existing pch-* agent prompts for pattern reference"
    type: reference
    status: available
    notes: "All 6 agent files exist in /agents/ and have been analyzed in research phase 1"
  - id: DEP-2
    dependency: "Hugh CLI source code for command/output verification"
    type: reference
    status: available
    notes: "Program.cs, ConsoleOutput.cs, and all relevant source files exist in /Hugh/"
  - id: DEP-3
    dependency: "Hugh README for installation and troubleshooting reference"
    type: reference
    status: available
    notes: "Hugh/README.md contains verified installation steps and troubleshooting"
  - id: DEP-4
    dependency: "GitHub Releases URL (https://github.com/pch-development/hugh)"
    type: external
    status: available
    notes: "Confirmed in installer/hugh-setup.iss line 13; public repository"
  - id: DEP-5
    dependency: "Vision document /docs/vision/004-hugh-copilot-agent.md"
    type: reference
    status: complete
    notes: "Defines requirements FR-1 through FR-8 and NFR-1/NFR-2"
  - id: DEP-6
    dependency: "Research document /docs/research/013-hugh-copilot-agent-prompt.md"
    type: reference
    status: complete
    notes: "3-phase research covering agent conventions, CLI reference, installation approaches"

risks:
  - id: RISK-1
    risk: "Hugh CLI output format changes between releases, causing stale guidance in agent prompt"
    likelihood: medium
    impact: medium
    mitigation: "Structure CLI reference and output patterns as discrete, clearly-labeled blocks within the prompt so they can be located and updated quickly. Document Hugh version compatibility in the prompt header."
  - id: RISK-2
    risk: "Prompt exceeds recommended 400-600 line target due to comprehensive output taxonomy choice"
    likelihood: medium
    impact: low
    mitigation: "The comprehensive taxonomy was an intentional trade-off for FR-7/FR-8 coverage. Target upper range (~550 lines). If exceeded, trim redundancy in workflow instructions where CLI reference already covers the same ground."
  - id: RISK-3
    risk: "GitHub Releases API may be rate-limited or unavailable for unauthenticated requests"
    likelihood: low
    impact: medium
    mitigation: "Include static URL pattern as fallback in the install workflow. Agent falls back to directing user to the releases page URL if API fetch fails."
  - id: RISK-4
    risk: "Guided config edits could accidentally break appsettings.json format"
    likelihood: low
    impact: medium
    mitigation: "Agent instructions should specify editing only the value portion of a single JSON key, not restructuring. The agent should read the current file content before editing to preserve format."

phases:
  - phase: 1
    name: "Create Hugh Agent Prompt File"
    status: complete
    depends_on: []
    tasks:
      - id: T1.1
        title: "Create /agents/hugh.md with YAML front matter"
        status: complete
        requirements: ["NFR-1"]
        details: |
          Create the file /agents/hugh.md with YAML front matter matching the
          schema used by all other pch-* agents.

          YAML front matter content:
          ```yaml
          ---
          name: hugh
          description: Helps developers install, run, and monitor the Hugh CLI directly from VS Code Copilot chat
          model: Claude Opus 4.5
          handoffs:
            - label: Get Workflow Help
              agent: pch-helper
              prompt: Help the user understand the PCH agent workflow
              send: false
          ---
          ```

          Verification: File exists, YAML front matter parses correctly, matches
          schema of agents/pch-helper.md (name, description, model, handoffs fields).

      - id: T1.2
        title: "Write Section 1 — Role Definition"
        status: complete
        requirements: ["NFR-2"]
        details: |
          Write the role definition section (~10-15 lines) immediately after the
          YAML front matter closing `---`.

          Content must include:
          - Identity: "You are a CLI assistant that helps developers install, run,
            and monitor the Hugh CLI — a self-operating development loop that
            orchestrates the PCH agent workflow autonomously."
          - Hugh's purpose: Automates vision → research → planning → review → coding
            pipeline via coordinated PCH agents
          - Named after the Star Trek Borg character who regained individuality
          - Scope boundaries: CLI operations only; does NOT modify Hugh source code,
            replace other PCH agents, or generate complete configuration files
          - Integration: executes commands via run_in_terminal and interprets output

          Follow the opening pattern from pch-helper.md: first paragraph is the
          identity/role statement, followed by a brief context paragraph.

      - id: T1.3
        title: "Write Section 2 — Core Responsibilities"
        status: complete
        requirements: ["FR-1", "FR-2", "FR-3", "FR-4", "FR-5", "FR-6", "FR-7", "FR-8"]
        details: |
          Write the Core Responsibilities section as a bulleted list under
          a `## Core Responsibilities` heading (~15-20 lines).

          Must include these responsibilities (one bullet each):
          - **Detect installation**: Check if Hugh is installed and report version
          - **Guide installation**: Walk users through installing from GitHub Releases
            (installer or portable ZIP), including download, install, and verification
          - **Run feature requests**: Construct and execute `hugh "<feature>"` commands
          - **Check run status**: Execute `hugh --status` and interpret the output
          - **Resume runs**: Execute `hugh --resume [runId]` for paused/failed runs
          - **Check prerequisites**: Execute `hugh doctor` and `hugh doctor --fix`
          - **Interpret output**: Parse terminal output, identify success/failure/progress,
            provide actionable guidance
          - **Answer questions**: Respond to questions about CLI flags, options,
            configuration settings, and troubleshooting
          - **Edit single settings**: Open and modify individual settings in
            appsettings.json when requested (refuse full config generation)

      - id: T1.4
        title: "Write Section 3 — Critical Rules"
        status: complete
        requirements: ["NFR-1", "FR-1"]
        details: |
          Write the Critical Rules section under `## Critical Rules` heading (~30-40 lines).

          Must include these rules:

          ### Detection-First Workflow
          - Before executing any Hugh CLI command, verify Hugh is installed
          - Detection sequence: `Get-Command hugh -ErrorAction SilentlyContinue`
            then `hugh --version` if found
          - Cache detection result for the conversation; re-detect only if:
            (a) a command fails with "not recognized" error, or
            (b) user explicitly asks to re-check, or
            (c) user just completed installation
          - If not installed: do NOT attempt CLI commands; offer installation guidance

          ### PATH Handling
          - After installation, verify using the FULL executable path:
            `& "$env:LOCALAPPDATA\Programs\Hugh\Hugh.exe" --version`
          - Advise user to open a new terminal for PATH to take effect
          - For portable ZIP installs, use the extraction directory path

          ### Command Safety
          - Always show the exact command to the user before executing
          - Use PowerShell syntax (target platform is Windows)
          - Quote feature descriptions properly in double quotes

          ### Configuration Boundaries
          - CAN: explain any setting, edit a single setting value in appsettings.json
          - CANNOT: generate complete configuration files, make sweeping multi-setting changes
          - Always read appsettings.json before editing to preserve existing format

          ### Scope Boundaries
          - NEVER modify Hugh source code (.cs files)
          - NEVER modify other agent prompts (pch-*.md files)
          - NEVER attempt to build or compile Hugh

      - id: T1.5
        title: "Write Section 4 — Session Initialization"
        status: complete
        requirements: ["FR-1"]
        details: |
          Write the Session Initialization section under `## Session Initialization`
          heading (~25-35 lines).

          Present as a decision tree the agent follows on each user message:

          ```
          User message received
          ├─ Is this a knowledge question? (flags, config, concepts, troubleshooting)
          │  └─ YES → Answer directly from embedded reference. No terminal commands needed.
          ├─ Does this require CLI execution? (install, run, status, resume, doctor)
          │  ├─ Has Hugh been detected this conversation?
          │  │  ├─ YES → Proceed with requested operation
          │  │  └─ NO → Run detection sequence first:
          │  │     1. Get-Command hugh -ErrorAction SilentlyContinue
          │  │     2. If found: hugh --version → cache result → proceed
          │  │     3. If not found: report not installed → offer installation (FR-2)
          │  └─ (continue to appropriate workflow)
          └─ Is this a config edit request?
             └─ Locate appsettings.json → read current content → edit single value
          ```

          Include a note: "If a previous command in this conversation returned an error
          suggesting Hugh is not available, re-run detection before the next command."

      - id: T1.6
        title: "Write Section 5 — CLI Command Reference"
        status: complete
        requirements: ["FR-3", "FR-4", "FR-5", "FR-6", "FR-7", "FR-8"]
        details: |
          Write the CLI Command Reference under `## Hugh CLI Command Reference`
          heading (~120-150 lines). Use command-by-command blocks.

          Each command block must include:
          - Syntax line
          - Description
          - Flags/options (if any)
          - Expected output pattern (what the terminal shows on success)
          - Example usage

          ### Commands to document:

          **### New Feature Run**
          - Syntax: `hugh "<feature description>"`
          - Flags: --verbose, --timeout <HH:mm:ss>
          - Output: Header → phase status messages → completion/pause/failure
          - Example: `hugh "Add user authentication with OAuth2 support"`
          - Example with flags: `hugh "Add config option" --verbose --timeout 00:30:00`

          **### Check Status**
          - Syntax: `hugh --status` or `hugh -s`
          - Output: Formatted table with fields: Run ID, Started, Feature, Status,
            Current Phase, Coding Sub-Phase, phase status icons with document paths,
            decision count
          - Explain each field meaning

          **### Resume Run**
          - Syntax: `hugh --resume` or `hugh --resume <runId>` or `hugh -r [runId]`
          - Output: Resumes from last saved state, shows phase continuation messages
          - Example: `hugh --resume` (most recent) or `hugh --resume abc123`

          **### Doctor (Prerequisites)**
          - Syntax: `hugh doctor` (check only)
          - Syntax: `hugh doctor --fix` or `hugh doctor -f` (check and install)
          - Syntax: `hugh doctor --fix --no-interactive` (CI/CD mode)
          - Output: Prerequisite list with ✓/✗ per item, version info, resolution hints
          - Prerequisites checked (in order):
            1. PCH Agents Folder (agents-folder, priority 10)
            2. Workspace Write Access (workspace, priority 20)
            3. GitHub CLI (github-cli, priority 30)
            4. GitHub Copilot CLI (copilot-cli, priority 40)
            5. GitHub Authentication (github-auth, priority 50)

          **### Global Options**
          - `--version`: Display Hugh version (auto-generated by System.CommandLine)
          - `--help` / `-h` / `-?`: Display help text (auto-generated by System.CommandLine)
          - `--verbose` / `-v`: Enable debug logging (visible in console)
          - `--timeout <HH:mm:ss>` / `-t`: Override default phase timeout
          - `--check` / `-c`: Alias for `hugh doctor`

          **### Exit Codes**
          - 0: Success (command completed, prerequisites satisfied)
          - 1: Failure (prerequisites not met, exception, or phase failure)

          **### Output Status Icons**
          Two icon systems appear in Hugh terminal output. Both must be recognized.

          *Status message icons* (from Write* helper methods):
          | Icon | Method | Color | Meaning |
          | ✓ | WriteSuccess | Green | Success message |
          | ✗ | WriteError | Red | Error message |
          | ⚠ | WriteWarning | Yellow | Warning (non-fatal) |
          | ℹ | WriteInfo | Blue | Informational message |
          | → | WriteHint | DarkGray | Resolution hint or suggestion |

          *Phase status icons* (from WritePhaseStatus / FormatStatus, uses emoji):
          | Icon | Status | Color | Meaning |
          | ✅ | Complete | Green | Phase completed successfully |
          | 🔄 | In Progress | Yellow | Phase currently executing |
          | ❌ | Failed | Red | Phase failed |
          | ⏸️ | Paused | DarkYellow | Run paused awaiting input |
          | ⏳ | Not Started | DarkGray | Phase not yet begun |

          *Phase lifecycle icons*:
          | Icon | Method | Color | Meaning |
          | ▶ | WritePhaseStart | Cyan | Phase starting ("▶ Starting {phase} phase") |
          | ✓ | WritePhaseComplete | Green | Phase complete ("✓ {phase} phase complete") |
          | ✗ | WritePhaseFailed | Red | Phase failed ("✗ {phase} phase failed") |

          **### Phase Execution Flow**
          Vision → Research → Planning → Review → Coding (sub-phases)
          Each phase: prerequisites check → state update → prompt build → execute → update state

          **### Configuration Reference**
          Table of all appsettings.json keys:
          | Key | Default | Purpose |
          Include all settings from HughConfig: AgentsPath, DocsPath, OrchestratorPath,
          Timeouts.* (5 settings), Autonomous.* (2 settings), Copilot.* (4 settings)

          Path resolution order: HUGH_CONFIG_PATH env var → assembly location → CWD
          User data: ~/.hugh/ (logs at ~/.hugh/logs/, daily rolling)

      - id: T1.7
        title: "Write Section 6 — Workflow Instructions"
        status: complete
        requirements: ["FR-1", "FR-2", "FR-3", "FR-4", "FR-5", "FR-6"]
        details: |
          Write Workflow Instructions under `## Workflow Instructions` heading
          (~100-130 lines). Step-by-step guides for 6 key scenarios.

          **### Workflow A — Install Hugh**
          1. Check installation: `Get-Command hugh -ErrorAction SilentlyContinue`
          2. If already installed: report version, skip to verification
          3. Query latest release:
             ```powershell
             $release = Invoke-RestMethod "https://api.github.com/repos/pch-development/hugh/releases/latest"
             $version = $release.tag_name
             ```
          4. Ask user preference: installer (recommended) or portable ZIP
          5a. Installer path:
             - Find installer asset: `$asset = $release.assets | Where-Object { $_.name -like "hugh-setup-*.exe" }`
             - Download: `Invoke-WebRequest -Uri $asset.browser_download_url -OutFile "$env:TEMP\hugh-setup.exe"`
             - Install: `& "$env:TEMP\hugh-setup.exe" /VERYSILENT /SUPPRESSMSGBOXES /NORESTART`
             - Verify: `& "$env:LOCALAPPDATA\Programs\Hugh\Hugh.exe" --version`
             - Clean up: `Remove-Item "$env:TEMP\hugh-setup.exe"`
          5b. Portable ZIP path:
             - Find ZIP asset: `$asset = $release.assets | Where-Object { $_.name -like "hugh-portable-*.zip" }`
             - Download: `Invoke-WebRequest -Uri $asset.browser_download_url -OutFile "$env:TEMP\hugh.zip"`
             - Extract: `Expand-Archive "$env:TEMP\hugh.zip" -DestinationPath "C:\Tools\Hugh" -Force`
             - Add to PATH: `[Environment]::SetEnvironmentVariable("Path", "...", "User")`
             - Verify: `& "C:\Tools\Hugh\Hugh.exe" --version`
             - Clean up: `Remove-Item "$env:TEMP\hugh.zip"`
          6. Advise: "Open a new terminal for PATH to take effect"
          7. Fallback: if API fails, direct user to https://github.com/pch-development/hugh/releases/latest

          **### Workflow B — Run a Feature**
          1. Confirm Hugh is installed (detection cache)
          2. Clarify feature description with user if vague
          3. Ask about optional flags (verbose, timeout)
          4. Construct command: `hugh "<feature description>"` with any flags
          5. Show command to user for confirmation
          6. Execute in terminal (use run_in_terminal with isBackground=true for long-running)
          7. Interpret initial output — report phase starting messages

          **### Workflow C — Check Status**
          1. Run: `hugh --status`
          2. Parse output table
          3. Report to user: run ID, feature, status, current phase, any blockers
          4. Suggest next action based on status (resume, wait, doctor)

          **### Workflow D — Resume a Run**
          1. Optionally run `hugh --status` first to confirm resumable state
          2. Ask user if they want to resume most recent or a specific run ID
          3. Run: `hugh --resume` or `hugh --resume <runId>`
          4. Optionally add --timeout for complex phases
          5. Interpret output for phase continuation

          **### Workflow E — Prerequisites (Doctor)**
          1. Run: `hugh doctor`
          2. Parse prerequisite report (✓/✗ per item)
          3. If all satisfied: report success
          4. If issues found: explain each issue and offer `hugh doctor --fix`
          5. If user approves fix: run `hugh doctor --fix`
          6. Re-run `hugh doctor` to verify fixes applied

          **### Workflow F — Edit Configuration Setting**
          1. Identify which setting the user wants to change
          2. Explain the setting's purpose and current default
          3. Locate appsettings.json:
             - Check HUGH_CONFIG_PATH env var
             - Check installation directory ($env:LOCALAPPDATA\Programs\Hugh\)
             - Check current working directory
          4. Read current file content
          5. Edit only the target value (preserve JSON structure)
          6. Report the change made
          7. If Hugh is currently running: advise restart needed

      - id: T1.8
        title: "Write Section 7 — Error Reference & Troubleshooting"
        status: complete
        requirements: ["FR-7", "FR-8"]
        details: |
          Write Error Reference under `## Error Reference & Troubleshooting`
          heading (~60-80 lines).

          List each error with: symptom, cause, and resolution steps.

          **### Installation Errors**

          **"hugh: command not found" / "'hugh' is not recognized"**
          - Cause: Hugh not in PATH or not installed
          - Resolution:
            1. Open a new terminal (PATH may not be refreshed)
            2. Check PATH: `$env:Path -split ';' | Where-Object { $_ -like '*hugh*' }`
            3. Try full path: `& "$env:LOCALAPPDATA\Programs\Hugh\Hugh.exe" --version`
            4. If not installed: follow Workflow A

          **"Configuration Not Found"**
          - Cause: appsettings.json not in expected location
          - Resolution:
            1. Check env var: `$env:HUGH_CONFIG_PATH`
            2. Check install dir: `Test-Path "$env:LOCALAPPDATA\Programs\Hugh\appsettings.json"`
            3. Set custom path: `$env:HUGH_CONFIG_PATH = "C:\path\to\appsettings.json"`

          **### Runtime Errors**

          **"Copilot CLI not found" / "JSON-RPC connection lost"**
          - Cause: GitHub Copilot CLI not installed or not in PATH
          - Resolution:
            1. Install: `winget install GitHub.Copilot`
            2. Or: `npm install -g @github/copilot`
            3. Verify: `copilot --version`
            4. Or run: `hugh doctor --fix`

          **"Authentication required" / "401 Unauthorized"**
          - Cause: GitHub auth expired or missing
          - Resolution:
            1. Run `copilot` then type `/login`
            2. Or set: `$env:GH_TOKEN = "ghp_your_token"`
            3. Verify: `gh auth status`

          **"Rate limit exceeded"**
          - Cause: Too many API requests in a short period
          - Resolution:
            1. Wait a few minutes before retrying
            2. Increase RetryBaseDelayMs in appsettings.json
            3. Reduce prompt complexity

          **"Context window exceeded" / Truncated responses**
          - Cause: Conversation too large for model context
          - Resolution:
            1. Lower CompactionThreshold (e.g., 0.6) in appsettings.json
            2. Start new session for current phase
            3. Break large phases into smaller sub-phases

          **Process hangs or timeouts**
          - Cause: Phase exceeds configured timeout
          - Resolution:
            1. Resume with longer timeout: `hugh --resume --timeout 01:00:00`
            2. Update Timeouts in appsettings.json for specific phases

          **Credential detection fails**
          - Cause: GitHub CLI not authenticated
          - Resolution:
            1. `gh auth login` then `gh auth status`
            2. Verify standalone Copilot CLI: `copilot --version`
            3. Note: Hugh uses the standalone `copilot` binary (via `winget install GitHub.Copilot`),
               not the `gh copilot` extension. Do not confuse the two.

          **Logs not created**
          - Cause: Log directory doesn't exist
          - Resolution:
            1. Check: `Test-Path "$env:USERPROFILE\.hugh\logs"`
            2. Create: `New-Item -ItemType Directory -Force -Path "$env:USERPROFILE\.hugh\logs"`

      - id: T1.9
        title: "Final review and line count verification"
        status: complete
        requirements: ["NFR-1", "NFR-2"]
        details: |
          Perform a final review of the complete /agents/hugh.md file:

          Verification checklist:
          1. YAML front matter is valid (opens and closes with ---)
          2. All 7 sections are present with correct headings
          3. All FR-1 through FR-8 requirements are covered
          4. Total line count is within 450-600 range
          5. All Hugh CLI commands, flags, and exit codes are accurate
            (cross-reference with Hugh/Program.cs)
          6. All configuration settings match Hugh/appsettings.json
          7. All error messages and resolutions match Hugh/README.md
          8. PowerShell syntax is correct throughout
          9. GitHub Releases URL is correct: https://github.com/pch-development/hugh
          10. No references to modifying Hugh source code or other agents
          11. Detection workflow matches pch-coder's detect-first pattern
          12. Handoff to pch-helper is present and correctly formatted

review_session_log:
  questions_pending: 0
  questions_resolved: 0
  last_updated: "2026-02-10"
  entries: []
  notes: "No clarifying questions required. All issues were factual corrections resolved by codebase verification."

referenced_standards: "No organizational standards applicable to this plan."

review_summary:
  review_date: "2026-02-10"
  reviewer: pch-plan-reviewer
  original_plan_version: v1.0
  reviewed_plan_version: v1.1
  review_metrics:
    issues_found: 3
    critical: 0
    major: 1
    minor: 2
    clarifying_questions_asked: 0
    sections_updated:
      - "T1.6 — Output Status Icons table"
      - "T1.6 — Global Options section"
      - "T1.8 — Credential detection troubleshooting"
  key_improvements_made:
    - "Corrected Output Status Icons table to match actual ConsoleOutput.cs — separated into three categories (status messages, phase status emoji, phase lifecycle) with accurate icons, colors, and method references"
    - "Added --version and --help to Global Options for completeness since agent uses --version for detection"
    - "Clarified that Hugh uses standalone copilot binary, not gh copilot extension, in credential troubleshooting"
  remaining_considerations:
    - "The comprehensive output taxonomy (decision 7) now describes three icon categories instead of one flat table — implementer should preserve this separation in the agent prompt for clarity"
    - "Hugh README contains some mixed references to gh copilot vs standalone copilot — if README is updated in the future, agent prompt troubleshooting section should be kept in sync"
  sign_off: "This plan has been reviewed and is Ready for Implementation"

implementation_complexity:
  files_to_modify:
    score: 1
    notes: "1 file (agents/hugh.md) — single new Markdown file"
  new_patterns_introduced:
    score: 1
    notes: "No new patterns — follows existing pch-* agent conventions"
  external_dependencies:
    score: 1
    notes: "GitHub Releases API (unauthenticated, public repo)"
  migration_complexity:
    score: 1
    notes: "No migration — additive change only"
  test_coverage_required:
    score: 1
    notes: "Manual verification via T1.9 checklist, no automated tests needed"
  overall_complexity:
    score: "5/25"
    level: Low

handoff:
  created_by: pch-planner
  created_date: "2026-02-10"
  reviewed_by: pch-plan-reviewer
  review_date: "2026-02-10"
  status: complete
  next_agent: pch-coder
  plan_location: /docs/plans/015-hugh-copilot-agent-prompt.md
