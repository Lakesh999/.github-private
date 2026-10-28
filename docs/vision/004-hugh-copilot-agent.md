id: "004"
type: vision
title: "Hugh CLI Copilot Agent Prompt"
status: complete
created: "2026-02-10"
owner: pch-visionary
version: v1.0

introduction: |
  This vision captures the design for a new GitHub Copilot custom agent prompt
  called "hugh" that acts as an AI-powered interface to the Hugh CLI. The agent
  enables users to install, configure, run, and monitor Hugh directly from
  within VS Code's Copilot chat — eliminating the need to switch to a terminal
  and remember CLI syntax.

decisions:
  - session: 1
    date: "2026-02-10"
    stage: 1
    topic: "Problem Definition"
    question: "What problem are we solving?"
    answer: "Users need to interface the Hugh CLI with GitHub Copilot in VS Code via a custom agent prompt. Currently there is no agent that knows how to install Hugh from its GitHub release, verify the installation, pass command-line arguments, and monitor CLI output — forcing users to leave the Copilot chat context and operate the terminal manually."
    impact: "Defines core scope: agent prompt that wraps Hugh CLI operations with AI guidance"
  - session: 1
    date: "2026-02-10"
    stage: 1
    topic: "Business Context"
    question: "Who is impacted, what's the cost, why now?"
    answer: "Developers use the PCH agent workflow to go from vision to functional code. The workflow is productive but has many manual human steps — copying documents between AI context windows costs real time and money. Hugh automates those orchestration steps into a single command. The agent prompt makes Hugh itself accessible from within Copilot chat so developers don't need to leave that context either."
    impact: "Establishes ROI framing: reduce manual workflow steps, save developer time and cost"
  - session: 1
    date: "2026-02-10"
    stage: 1
    topic: "Success Metrics"
    question: "How will you know this agent prompt is successful?"
    answer: "Combination of milestone-based (C) and qualitative (B): the agent can install Hugh, verify it, run a feature request, and show output from chat; and developers feel confident operating Hugh without reading docs."
    impact: "Success defined by capability milestones plus developer confidence"
  - session: 1
    date: "2026-02-10"
    stage: 2
    topic: "Primary Users"
    question: "Who is the primary audience for the hugh agent prompt?"
    answer: "A — PCH workflow developers who already use the pch-visionary / pch-planner / pch-coder agents and want to automate the full pipeline via Hugh."
    impact: "Agent prompt can assume familiarity with the PCH workflow; focus on Hugh CLI operations rather than explaining the agent system"
  - session: 1
    date: "2026-02-10"
    stage: 3
    topic: "Primary Goals"
    question: "What are the top goals for the hugh agent prompt?"
    answer: "Three goals confirmed: (1) Install & Setup — install Hugh from GitHub Releases and verify, (2) Run & Orchestrate — pass feature requests and CLI commands to Hugh from chat, (3) Monitor & Troubleshoot — show output, interpret status, help resolve errors."
    impact: "Defines the three core capability areas the agent prompt must cover"
  - session: 1
    date: "2026-02-10"
    stage: 3
    topic: "Non-Goals"
    question: "What is explicitly not the hugh agent's job?"
    answer: "A (no modifying Hugh source code), B (no replacing other PCH agents), D (no deep config authoring). Cross-platform (C) is NOT a non-goal — macOS support for Hugh is planned for the future."
    impact: "Agent scope is strictly CLI operation/guidance; cross-platform awareness should be designed in from the start"
  - session: 1
    date: "2026-02-10"
    stage: 4
    topic: "Functional Requirements"
    question: "What must the hugh agent be able to do?"
    answer: "All 8 capabilities confirmed: detect installation, install from GitHub Releases, run feature requests, check status, resume runs, run doctor, interpret output, explain commands."
    impact: "Defines the full functional surface of the agent prompt"
  - session: 1
    date: "2026-02-10"
    stage: 4
    topic: "Quality Attributes"
    question: "What quality attributes matter most for this agent prompt?"
    answer: "A (Accuracy) and B (Completeness) — the agent must give correct CLI commands and contain enough Hugh knowledge to rarely need fallback to docs."
    impact: "Prompt must embed comprehensive, verified Hugh CLI reference material"
  - session: 1
    date: "2026-02-10"
    stage: 5
    topic: "Solution Approach"
    question: "What is the integration approach for the hugh agent?"
    answer: "A — Standalone agent. Its own file at /agents/hugh.md with its own YAML front matter, invoked via @hugh in Copilot chat. Uses run_in_terminal to execute Hugh CLI commands."
    impact: "Agent is self-contained; no dependency on pch-helper for Hugh operations"
  - session: 1
    date: "2026-02-10"
    stage: 5
    topic: "GitHub Release URL"
    question: "What is the GitHub repository URL for Hugh releases?"
    answer: "Confirmed: https://github.com/pch-development/hugh — defined in installer/hugh-setup.iss line 13"
    impact: "Agent prompt will use this URL for install/download guidance"
  - session: 1
    date: "2026-02-10"
    stage: 6
    topic: "Phase Grouping"
    question: "How should we phase the deliverable?"
    answer: "A — Single phase. All 8 requirements in one phase since the deliverable is a single Markdown file with no build dependencies."
    impact: "Simplifies downstream workflow: one research session, one planning session, one coding session"
  - session: 1
    date: "2026-02-10"
    stage: 6
    topic: "Done Criteria"
    question: "Are the proposed phase completion criteria sufficient?"
    answer: "Yes — all 6 criteria confirmed as-is."
    impact: "Clear acceptance criteria for downstream agents"
  - session: 1
    date: "2026-02-10"
    stage: 6
    topic: "Vision Validation"
    question: "Does the vision summary look correct? Anything to add, remove, or change?"
    answer: "Confirmed — vision is good as-is."
    impact: "Vision finalized and ready for handoff"
  - session: 1
    date: "2026-02-10"
    stage: 6
    topic: "Vision Validation"
    question: "Does the vision summary look correct? Anything to add, remove, or change?"
    answer: "Confirmed — vision is good as-is."
    impact: "Vision finalized and ready for handoff"

problem:
  context: "The PCH agent workflow (vision → research → planning → review → coding) produces consistent, high-quality AI-assisted development output, but requires many manual handoff steps between agents. Each manual step — copying documents, switching context windows, invoking the next agent — costs real developer time and money."
  current_state: "Developers must manually orchestrate the PCH workflow step by step, copying required documents between AI context windows. Hugh CLI exists to automate this but must be installed and operated from a terminal, separate from the Copilot chat environment where developers already work."
  desired_state: "A Copilot agent prompt that lets developers install, configure, run, and monitor Hugh directly from within VS Code's Copilot chat — keeping them in a single AI-assisted context from start to finish."
  success_metrics:
    - metric: "Install & verify capability"
      target: "Agent can guide user through installing Hugh from GitHub Releases and verify installation via hugh --version"
      method: "Manual validation: invoke agent, request install, confirm Hugh runs"
    - metric: "Run feature request capability"
      target: "Agent can construct and execute a hugh command with user's feature description and show output"
      method: "Manual validation: invoke agent, provide feature idea, confirm Hugh executes"
    - metric: "Monitor & troubleshoot capability"
      target: "Agent can check Hugh run status, interpret output, and suggest fixes for common errors"
      method: "Manual validation: invoke agent during/after a Hugh run, confirm useful guidance"
    - metric: "Developer confidence"
      target: "Developers feel confident operating Hugh without reading docs or asking for help"
      method: "Qualitative feedback from team after first use"

stakeholders:
  - role: "PCH workflow developer"
    goals: "Automate the full vision-to-code pipeline via Hugh CLI without leaving Copilot chat"
    pain_points: "Must switch to terminal to install, run, and monitor Hugh; needs to remember CLI syntax and flags"
    proficiency: technical

goals:
  primary:
    - id: G-1
      goal: "Enable developers to install Hugh from GitHub Releases and verify the installation, guided through Copilot chat"
      priority: must-have
      success_indicator: "Agent can download, install, and confirm hugh --version output"
    - id: G-2
      goal: "Let developers pass feature requests and CLI commands to Hugh and launch execution from Copilot chat"
      priority: must-have
      success_indicator: "Agent constructs and runs hugh commands with correct arguments and flags"
    - id: G-3
      goal: "Show Hugh's output, interpret run status, and help resolve common errors without leaving chat"
      priority: must-have
      success_indicator: "Agent can run hugh --status, interpret output, and suggest fixes for known issues"
  non_goals:
    - "Modifying Hugh source code — the agent helps use Hugh, not develop it"
    - "Replacing the other PCH agents — the agent orchestrates Hugh which calls those agents, it does not perform vision/research/planning/coding itself"
    - "Hugh configuration authoring — the agent knows defaults and points users to appsettings.json but does not generate or deeply customize configuration"

requirements:
  functional:
    - id: FR-1
      requirement: "Detect whether Hugh is installed by checking PATH and reporting version via hugh --version"
      priority: must-have
      phase: 1
    - id: FR-2
      requirement: "Guide user through installing Hugh from GitHub Releases (installer or portable ZIP) and verify installation"
      priority: must-have
      phase: 1
    - id: FR-3
      requirement: "Construct and execute 'hugh <feature description>' in a terminal to start a new feature run"
      priority: must-have
      phase: 1
    - id: FR-4
      requirement: "Execute 'hugh --status' and interpret the output to report run state to the user"
      priority: must-have
      phase: 1
    - id: FR-5
      requirement: "Execute 'hugh --resume [runId]' to resume a paused or failed run when requested"
      priority: must-have
      phase: 1
    - id: FR-6
      requirement: "Execute 'hugh doctor' and 'hugh doctor --fix' to check and fix prerequisites, interpreting results"
      priority: must-have
      phase: 1
    - id: FR-7
      requirement: "Parse and interpret Hugh terminal output to provide actionable guidance on errors, progress, and phase transitions"
      priority: must-have
      phase: 1
    - id: FR-8
      requirement: "Answer questions about Hugh CLI flags, options, configuration, and troubleshooting"
      priority: must-have
      phase: 1
  non_functional:
    - id: NFR-1
      requirement: "Agent must provide correct CLI commands, flags, URLs, and installation steps — wrong guidance is worse than no guidance"
      priority: must-have
      type: reliability
    - id: NFR-2
      requirement: "Agent prompt must contain comprehensive Hugh CLI knowledge so it rarely needs to fall back to external docs"
      priority: must-have
      type: usability
  constraints:
    - type: technical
      description: "Agent prompt is a single Markdown file deployed alongside the existing pch-* agent prompts in /agents/"
      impact: "All Hugh knowledge must be embedded in the prompt text; no external data sources at runtime"
    - type: technical
      description: "Must follow the same YAML front matter format and conventions as existing pch-* agents"
      impact: "Consistent integration with the agent ecosystem and any tooling that reads agent metadata"

architecture:
  style: cli
  rationale: "Single Markdown agent prompt file deployed at /agents/hugh.md, invoked via @hugh in VS Code Copilot chat. Uses run_in_terminal tool to execute Hugh CLI commands and interprets terminal output."
  technology_decisions:
    - area: Platform
      choice: "GitHub Copilot custom agent prompt (Markdown file with YAML front matter)"
      rationale: "Consistent with existing pch-* agent prompts; no additional infrastructure needed"
    - area: Integration
      choice: "run_in_terminal tool for CLI execution"
      rationale: "Standard VS Code Copilot tool for executing shell commands; provides output capture for interpretation"
  integrations:
    - system: "Hugh CLI (hugh.exe)"
      type: api
      direction: both
      criticality: mvp
    - system: "GitHub Releases (pch-development/hugh)"
      type: api
      direction: read
      criticality: mvp
  security:
    - concern: "Data sensitivity"
      approach: "Agent does not handle credentials directly; Hugh manages its own GitHub authentication via gh auth or GH_TOKEN"

phasing:
  ordering_rationale: |
    Single-phase delivery. The entire deliverable is one Markdown agent prompt
    file. All functional requirements contribute sections to the same file with
    no build dependencies between them. One research, planning, and coding
    session each is sufficient.
  phases:
    - phase: 1
      name: "Hugh Agent Prompt"
      category: interface
      scope:
        - "FR-1: Detect Hugh installation and report version"
        - "FR-2: Install Hugh from GitHub Releases (installer or portable ZIP)"
        - "FR-3: Run a feature request via hugh command"
        - "FR-4: Check run status via hugh --status"
        - "FR-5: Resume paused/failed runs via hugh --resume"
        - "FR-6: Run prerequisite checks via hugh doctor"
        - "FR-7: Interpret Hugh terminal output and provide actionable guidance"
        - "FR-8: Answer questions about Hugh CLI flags, options, and configuration"
      requirements: ["FR-1", "FR-2", "FR-3", "FR-4", "FR-5", "FR-6", "FR-7", "FR-8"]
      depends_on: []
      research_topics:
        - topic: "Existing pch-* agent prompt patterns"
          priority: medium
          questions:
            - "What YAML front matter fields and conventions do existing agents use?"
            - "What handoff patterns should the hugh agent follow?"
        - topic: "Hugh CLI complete command reference"
          priority: high
          questions:
            - "What are all Hugh CLI commands, flags, and their expected outputs?"
            - "What are the common error scenarios and their resolutions?"
      done_criteria:
        - "Agent prompt file exists at /agents/hugh.md with valid YAML front matter"
        - "Agent can detect Hugh installation via run_in_terminal"
        - "Agent can guide installation from GitHub Releases"
        - "Agent can construct and execute all Hugh CLI commands (new, status, resume, doctor)"
        - "Agent contains troubleshooting knowledge for common errors"
        - "Agent prompt follows existing pch-* conventions"
  deferred:
    - item: "macOS installation guidance"
      reason: "Hugh macOS build not yet available; planned for future"
      target_phase: future
    - item: "Deep appsettings.json configuration authoring"
      reason: "Defined as non-goal; agent points users to file but does not generate configs"
      target_phase: future

research_topics:
  - topic: "Existing pch-* agent prompt structure and conventions"
    priority: high
    questions:
      - "What YAML front matter schema do existing agents follow (name, description, model, handoffs)?"
      - "What agent invocation patterns work best for terminal-oriented workflows?"
    applies_to_phases: [1]
  - topic: "Hugh CLI complete command and output reference"
    priority: high
    questions:
      - "What are all exit codes, error messages, and output formats from Hugh?"
      - "What are the exact terminal output patterns for each Hugh command?"
    applies_to_phases: [1]

risks:
  - risk: "Hugh CLI output format may change between releases, causing stale guidance in the agent prompt"
    likelihood: medium
    impact: medium
    mitigation: "Structure the prompt so CLI reference sections are easy to locate and update; document version compatibility"
  - risk: "Agent prompt may become too large if all troubleshooting knowledge is embedded"
    likelihood: low
    impact: medium
    mitigation: "Focus on the most common scenarios; defer edge cases to Hugh README"

standards: []

handoff:
  created_by: pch-visionary
  created_date: "2026-02-10"
  status: complete
  next_agent: pch-planner
  path: /docs/vision/004-hugh-copilot-agent.md
