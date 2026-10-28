id: "016"
type: plan
title: "Hugh Agent Direct Work Prevention — Ensuring CLI Delegation"
status: implemented
created: "2026-02-10"
updated: "2026-02-10"
owner: pch-planner
version: "v2.1"
research_source: /docs/research/014-hugh-agent-direct-work-prevention.md

versions:
  - version: "v1.0"
    date: "2026-02-10"
    author: pch-planner
    changes: Initial plan creation with skeleton structure
  - version: "v1.1"
    date: "2026-02-10"
    author: pch-planner
    changes: "All clarifying decisions recorded: self-check strictness, redirect approach, Workflow G, identity reframe, handoff metadata, scope boundaries merge"
  - version: "v2.0"
    date: "2026-02-10"
    author: pch-planner
    changes: "Requirements, technical design, phases 1-3, dependencies, risks, and holistic review completed"
  - version: "v2.1"
    date: "2026-02-10"
    author: pch-plan-reviewer
    changes: "Review complete: Fixed self-check gap for Workflow E (4-question gate), corrected Task 1.2 line references, noted minor issues for implementer"

introduction: |
  The @hugh Copilot agent currently lacks scope enforcement infrastructure,
  causing it to attempt direct development work when users provide vision
  documents, feature descriptions, or "build me X" requests. This plan
  specifies the exact changes to agents/hugh.md to add a HARD CONSTRAINT
  section, expand the session initialization decision tree, add attached
  file handling, and introduce Workflow G for vision document processing.

decisions:
  - session: 1
    date: "2026-02-10"
    decision_point: "Self-check pattern strictness"
    options: "A) Strict 3-Question Gate, B) Simplified 2-Question Gate, C) Category-Based Gate, D) Minimal — Forbidden Actions Only"
    answer: "A — Strict 3-Question Gate"
    rationale: "Maximum safety with 3 explicit boolean questions before every tool call: (1) Is this a Hugh CLI command? (2) Is this an installation command from Workflow A? (3) Is this editing appsettings.json via Workflow F? Mirrors the pch-researcher gold standard pattern exactly."
  - session: 1
    date: "2026-02-10"
    decision_point: "Redirect template approach for out-of-scope requests"
    options: "A) Actionable Command Redirect, B) Explanation + Agent Handoff, C) Firm Refusal Only, D) Context-Aware Redirect with Variants"
    answer: "D — Context-Aware Redirect with Variants"
    rationale: "Multiple redirect templates tailored to each input type: vision doc → extract and offer Hugh run; code pasted → refuse with Hugh alternative; feature request → offer Hugh command; agent work request → hand off to @pch-helper. Best UX while maintaining firm boundaries."
  - session: 1
    date: "2026-02-10"
    decision_point: "Vision document handling — new Workflow G vs. extending Workflow B"
    options: "A) New Workflow G — Process Vision Document, B) Extend Workflow B with sub-path, C) Merge into Attached File Handling section only"
    answer: "A — New Workflow G — Process Vision Document"
    rationale: "Vision doc handling requires file reading, feature extraction, user confirmation, and conditional installation — a multi-step process distinct from Workflow B's text-to-command flow. Dedicated workflow maintains consistency with A-F naming and avoids branching complexity."
  - session: 1
    date: "2026-02-10"
    decision_point: "Role identity reframing scope"
    options: "A) Full Identity Reframe (heading + role paragraph + all references + HARD CONSTRAINT), B) Targeted Identity in HARD CONSTRAINT Only, C) Heading + HARD CONSTRAINT Only"
    answer: "C — Heading + HARD CONSTRAINT Only"
    rationale: "Change heading from 'Hugh CLI Assistant' to 'Hugh CLI Operator' (high-impact single-word edit that sets the AI's frame) and add explicit identity statements in the HARD CONSTRAINT section. Avoids unnecessary churn in the role paragraph which already describes capabilities well."
  - session: 1
    date: "2026-02-10"
    decision_point: "Handoff metadata updates in YAML front matter"
    options: "A) Keep Current (pch-helper only), B) Add Handoffs for Common Redirect Targets, C) No Handoff Changes"
    answer: "A — Keep Current (pch-helper Only)"
    rationale: "Hugh CLI is the primary path for all development work. Adding handoffs to pch-coder or pch-planner would undermine the core message. The existing pch-helper handoff covers workflow guidance. Context-aware redirect templates mention other agents verbally without creating formal bypass routes."
  - session: 1
    date: "2026-02-10"
    decision_point: "Handling of existing Scope Boundaries section with new HARD CONSTRAINT"
    options: "A) Keep Both Sections Separate, B) Merge into HARD CONSTRAINT Section, C) Merge and Reorganize by Category"
    answer: "B — Merge into HARD CONSTRAINT Section"
    rationale: "Single consolidated section eliminates ambiguity from rules split across two locations. Matches pch-researcher and pch-visionary pattern of one authoritative HARD CONSTRAINT section. Existing Scope Boundaries items (no Hugh source, agent prompts, installer) fit naturally as additional forbidden actions."

holistic_review:
  decision_interactions:
    - interaction: "Self-Check (Decision 1) + Redirect Templates (Decision 2)"
      analysis: "The 3-question self-check gates tool calls, while redirect templates handle the user communication after a violation is caught. These are complementary layers: self-check prevents the action, redirect gives the user an alternative path. No conflict."
    - interaction: "Merge Scope Boundaries (Decision 6) + HARD CONSTRAINT (Decision 1)"
      analysis: "Merging existing scope items into HARD CONSTRAINT creates a single 13-item forbidden actions list. Risk: the list is longer than pch-researcher's 6 items. Mitigation: the items are all 'never do X' format and scan quickly. The length is justified by Hugh's broader scope (CLI ops + config + knowledge) vs. researcher's narrow scope (docs only)."
    - interaction: "Workflow G (Decision 3) + Session Initialization (implicit)"
      analysis: "The 7-path session tree routes vision docs to Workflow G, but Workflow G also needs feature requests to potentially route back to Workflow B for execution. The plan handles this correctly: Workflow G step 6 references Workflow B steps 4-5 for the actual CLI execution phase."
    - interaction: "Identity Reframe (Decision 4) + Keep Handoffs (Decision 5)"
      analysis: "Renaming to 'CLI Operator' while keeping only the pch-helper handoff is consistent — it reinforces that this agent operates the CLI, not the agent workflow. The redirect templates verbally mention @pch-coder and @pch-planner without formal handoff buttons, which avoids undermining the CLI-first message."
    - interaction: "Context-Aware Redirects (Decision 2) + Attached File Handling (implicit)"
      analysis: "The redirect templates (in HARD CONSTRAINT) and the Attached File Handling section both address document routing. Potential overlap: both mention plan docs → @pch-coder and research docs → @pch-planner. This is intentional reinforcement, not duplication — the Attached File Handling section provides the decision table, while redirects provide the conversational response."
  architectural_considerations:
    - "Prompt length grows from ~571 to ~740 lines — within recommended 400-800 range for a focused CLI agent"
    - "HARD CONSTRAINT section is positioned early (after role definition) matching proven pattern from pch-researcher/pch-visionary"
    - "All changes are in a single file (agents/hugh.md) — no cross-file coordination needed"
    - "Existing workflows A-F are completely untouched — zero regression risk for current functionality"
  trade_offs:
    - accepted: "Longer forbidden actions list (13 items vs. 6 in pch-researcher)"
      reason: "Hugh has broader scope than pure documentation agents — needs to cover both development work prevention AND Hugh source code protection"
    - accepted: "Some redundancy between Attached File Handling table and redirect templates"
      reason: "Intentional reinforcement — table gives structured routing, templates give conversational responses. Both strengthen the constraint."
    - accepted: "Keeping role paragraph unchanged despite 'Assistant' → 'Operator' heading change"
      reason: "Role paragraph describes capabilities accurately. Heading + HARD CONSTRAINT provide sufficient identity reframe without rewriting stable content."
  risks_acknowledged:
    - "Strict 3-question self-check adds cognitive overhead to every tool call — mitigated by clear boolean format"
    - "5 redirect templates add ~30 lines of prompt — mitigated by keeping each variant to 2-3 sentences"
    - "Vision document feature extraction may be imperfect if document lacks clear Executive Summary — mitigated by user confirmation step in Workflow G"

overview:
  feature: "Hugh Agent Direct Work Prevention"
  description: |
    Add scope enforcement infrastructure to agents/hugh.md to prevent the
    agent from performing direct development work and ensure all feature
    requests are routed through the Hugh CLI.
  objectives:
    - "Add HARD CONSTRAINT section with forbidden/permitted actions and self-check"
    - "Expand session initialization from 3-path to 7-path routing"
    - "Add attached file handling for vision/plan/research documents"
    - "Add Workflow G for processing vision documents via Hugh CLI"
    - "Reframe agent identity from 'CLI assistant' to 'CLI OPERATOR'"
  success_criteria:
    - "Vision documents attached to @hugh are routed to hugh CLI, not processed directly"
    - "Feature descriptions like 'build me X' trigger hugh '<feature>' commands"
    - "Agent refuses to write code, create plans, or conduct research"
    - "Self-check pattern prevents tool calls outside permitted scope"
    - "Redirect template offers actionable hugh CLI command alternatives"

requirements:
  functional:
    - id: FR-1
      description: "HARD CONSTRAINT section prevents agent from performing direct development work"
      details: "Forbidden actions list covering code creation, plan creation, research, agent impersonation, quick fixes. Permitted actions list covering CLI operations, detection, config editing, knowledge answers."
    - id: FR-2
      description: "4-question self-check gate before every tool call"
      details: "Agent verifies: (1) Is this a Hugh CLI command? (2) Is this an installation command from Workflow A? (3) Is this editing appsettings.json via Workflow F? (4) Is this a prerequisite detection or fix command from Workflow E? If ALL No → STOP."
    - id: FR-3
      description: "Session initialization routes 7 input types instead of 3"
      details: "Routes: attached files, feature requests, explicit CLI commands, knowledge questions, config edits, out-of-scope work requests, ambiguous input."
    - id: FR-4
      description: "Attached file handling classifies documents by type and routes accordingly"
      details: "Vision docs → extract feature and route to Workflow G. Plan docs → redirect to @pch-coder. Research docs → redirect to @pch-planner. Other files → refuse with redirect template."
    - id: FR-5
      description: "Workflow G processes vision documents through Hugh CLI"
      details: "Steps: read vision doc, extract feature summary, construct hugh command, confirm with user, detect installation, execute and monitor."
    - id: FR-6
      description: "Context-aware redirect templates for each out-of-scope input type"
      details: "Tailored responses for: vision doc attached, code pasted, feature request without Hugh mention, code analysis request, agent workflow questions."
    - id: FR-7
      description: "Heading changed from 'Hugh CLI Assistant' to 'Hugh CLI Operator'"
      details: "Identity reframe in heading and HARD CONSTRAINT section. Role paragraph unchanged."
    - id: FR-8
      description: "Existing Scope Boundaries section merged into HARD CONSTRAINT"
      details: "Items about Hugh source code, agent prompts, installer, and release workflows consolidated as forbidden actions in the HARD CONSTRAINT section. Original Scope Boundaries section removed."
  non_functional:
    - id: NFR-1
      description: "Prompt stays within 600-800 line range"
      details: "Current prompt is 571 lines. Changes add ~170 lines. Target ~740 lines."
    - id: NFR-2
      description: "Constraint patterns match pch-researcher/pch-visionary gold standard"
      details: "Use identical structure: HARD CONSTRAINT banner → STOP imperative → Forbidden/Permitted lists → Self-Check → Redirect templates."
    - id: NFR-3
      description: "No behavioral regression for existing workflows A-F"
      details: "Installation, feature runs, status checks, resume, doctor, and config editing must continue to work identically."
  out_of_scope:
    - "Changes to any agent file other than agents/hugh.md"
    - "Changes to Hugh CLI source code (Hugh/ directory)"
    - "Testing framework for agent prompt behavior validation"
    - "Handling users who repeatedly ignore redirects"

technical_design:
  codebase_patterns:
    - pattern: "HARD CONSTRAINT Section"
      location: "agents/pch-researcher.md lines 22-57, agents/pch-visionary.md lines 41-73"
      usage: "Identical structure adapted for Hugh CLI operator role"
    - pattern: "Session Initialization Decision Tree"
      location: "agents/hugh.md lines 96-110"
      usage: "Expanded from 3-path to 7-path routing with same ASCII tree format"
    - pattern: "Named Workflow Format"
      location: "agents/hugh.md lines 322-445 (Workflows A-F)"
      usage: "Workflow G follows same header/step/code-block format"
    - pattern: "Redirect Template"
      location: "agents/pch-researcher.md lines 52-54"
      usage: "Context-aware variants following same conversational tone"
  architecture: |
    All changes target a single file: agents/hugh.md (currently 571 lines).
    The changes are additive prompt engineering — no code, no configuration,
    no tests. The file will grow to approximately 740 lines.

    Change locations (in order from top to bottom of file):
    1. Line 12: Change heading "Hugh CLI Assistant" → "Hugh CLI Operator"
    2. After line 21 (role paragraph): INSERT HARD CONSTRAINT section (~60 lines)
       - Includes merged Scope Boundaries content
       - Includes 3-question self-check
       - Includes context-aware redirect templates
    3. Lines 19-27 (Scope Boundaries block): REMOVE (merged into HARD CONSTRAINT)
    4. After HARD CONSTRAINT: INSERT Attached File Handling section (~40 lines)
    5. Lines 96-110 (Session Initialization tree): REPLACE with 7-path tree (~30 lines)
    6. After Workflow F (~line 445): INSERT Workflow G (~35 lines)
  file_changes:
    - file: agents/hugh.md
      action: modify
      description: "All changes to this single file — heading, HARD CONSTRAINT insertion, Scope Boundaries removal, Attached File Handling insertion, Session Initialization replacement, Workflow G insertion"

dependencies:
  - id: DEP-1
    description: "Current agents/hugh.md must be at 571 lines with existing structure"
    type: prerequisite
    status: met
  - id: DEP-2
    description: "Research document 014 findings for gap analysis and recommended changes"
    type: reference
    status: met
    path: /docs/research/014-hugh-agent-direct-work-prevention.md
  - id: DEP-3
    description: "pch-researcher.md HARD CONSTRAINT pattern as template"
    type: reference
    status: met
    path: agents/pch-researcher.md

risks:
  - id: RISK-1
    severity: medium
    description: "Overly strict constraints may prevent legitimate file reads for context"
    mitigation: "Permitted actions explicitly include 'Read any file to understand context' and 'Answer questions about Hugh capabilities'. Self-check only gates tool calls that modify files or run terminal commands."
  - id: RISK-2
    severity: low
    description: "Expanded session initialization tree may be harder for AI to parse"
    mitigation: "Use same ASCII tree format as current prompt. Keep routing paths clear with descriptive labels."
  - id: RISK-3
    severity: medium
    description: "Context-aware redirect variants add prompt length that may dilute core instructions"
    mitigation: "Keep variants concise (1-2 sentences each). Total additions stay within 170 lines, keeping file under 800 lines."
  - id: RISK-4
    severity: low
    description: "Merging Scope Boundaries into HARD CONSTRAINT may lose visibility for Hugh source code protection rules"
    mitigation: "Include all existing scope items as explicit forbidden action bullets. Cross-reference in HARD CONSTRAINT header."

phases:
  - phase: 1
    name: "Identity Reframe and HARD CONSTRAINT Section"
    status: complete
    size: medium
    files_to_modify: 1
    prerequisites:
      - "agents/hugh.md exists at current 571-line version"
    entry_point: "agents/hugh.md"
    verification: "File exists and line 12 contains '# Hugh CLI Assistant'"
    tasks:
      - step: "1.1"
        task: "Change heading from 'Hugh CLI Assistant' to 'Hugh CLI Operator'"
        files:
          - "agents/hugh.md"
        details: |
          Line 12: Replace `# Hugh CLI Assistant` with `# Hugh CLI Operator`
          This is the identity reframe — single word change with outsized impact.
        acceptance_criteria:
          - "Line 12 reads '# Hugh CLI Operator'"
          - "No other heading changes in the file"

      - step: "1.2"
        task: "Remove existing Scope Boundaries section (lines 20-25)"
        files:
          - "agents/hugh.md"
        details: |
          Remove the current Scope Boundaries block that starts with
          '**Scope Boundaries:**' (line 20) and its 5 bullet items through
          the trailing blank line (line 26). Do NOT remove line 27 which is
          the '## Core Responsibilities' heading.
          This content will be merged into the HARD CONSTRAINT section.

          Content being removed (lines 20-25):
          ```
          **Scope Boundaries:**
          - This agent handles CLI operations ONLY
          - Does NOT modify Hugh source code or build Hugh
          - Does NOT replace other PCH agents for their specialized tasks
          - Does NOT generate complete configuration files from scratch
          - Executes commands via `run_in_terminal` and interprets their output
          ```
        acceptance_criteria:
          - "No '**Scope Boundaries:**' block exists between role paragraph and Core Responsibilities"
          - "Role paragraph and '## Core Responsibilities' heading remain intact"
          - "Line 27 ('## Core Responsibilities') is NOT removed"

      - step: "1.3"
        task: "Insert HARD CONSTRAINT section after role paragraph, before Core Responsibilities"
        files:
          - "agents/hugh.md"
        details: |
          Insert the full HARD CONSTRAINT section between the role definition
          paragraph (ending with '...minimal human intervention.') and
          '## Core Responsibilities'. Follow the pch-researcher pattern exactly.

          Section structure:
          ```markdown
          ## 🚫 HARD CONSTRAINT: CLI Operator Only — No Direct Development Work

          > **STOP. This constraint is absolute and non-negotiable.**

          You are a **CLI OPERATOR**, not a development agent. Your only tool is
          the Hugh CLI. All development work flows through `hugh "<feature>"`.

          ### Forbidden Actions (NEVER do these):
          - ❌ Create, modify, or delete source code files (*.cs, *.js, *.py, *.ts, etc.)
          - ❌ Create, modify, or delete configuration files EXCEPT appsettings.json per Workflow F
          - ❌ Write implementation code, tests, or documentation content
          - ❌ Execute terminal commands that modify the codebase (only Hugh CLI commands permitted)
          - ❌ Act as pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer, or pch-coder
          - ❌ Create vision documents, research documents, or implementation plans
          - ❌ Analyze code and suggest implementations
          - ❌ Make "quick fixes" or "small changes" to code — no exceptions
          - ❌ Modify Hugh source code (anything in `/Hugh/` directory)
          - ❌ Modify other PCH agent prompts (pch-*.md files)
          - ❌ Build, compile, or publish Hugh
          - ❌ Create new agent definitions
          - ❌ Modify the installer or release workflows

          ### Permitted Actions (ONLY these):
          - ✅ Run `hugh "<feature>"` to start autonomous development
          - ✅ Run `hugh --status`, `--resume`, `doctor`, `--version`, `--help`
          - ✅ Run detection commands: `Get-Command`, `Test-Path`, `Get-Content` (for appsettings.json)
          - ✅ Install Hugh via Workflow A
          - ✅ Run prerequisite fix commands via Workflow E (`winget install`, `gh auth login`)
          - ✅ Read/edit ONLY appsettings.json per Workflow F
          - ✅ Read any file to understand context for answering questions
          - ✅ Answer questions about Hugh's capabilities, configuration, and workflow
          - ✅ Hand off to @pch-helper for agent workflow guidance

          ### Before ANY Tool Call — Self-Check

          Before calling any file-editing tool (`replace_string_in_file`, `create_file`,
          `multi_replace_string_in_file`) or `run_in_terminal`, ask yourself:

          1. Is this a Hugh CLI command? (`hugh`, `Get-Command hugh`, `Test-Path`)
          2. Is this an installation command from Workflow A?
          3. Is this editing appsettings.json via Workflow F?
          4. Is this a prerequisite detection or fix command from Workflow E?

          If ALL answers are "No" → **STOP. Do not proceed.** You are about to
          violate your core constraint.

          For source code changes, hand off to `pch-helper` to guide the user
          through the appropriate agent workflow.
          ```
        acceptance_criteria:
          - "HARD CONSTRAINT section exists between role paragraph and Core Responsibilities"
          - "Contains '🚫 HARD CONSTRAINT: CLI Operator Only' heading"
          - "Forbidden Actions list has 13 items (8 new + 5 merged from old Scope Boundaries)"
          - "Permitted Actions list has 9 items"
          - "Self-Check has 4 boolean questions"
          - "Contains 'STOP. This constraint is absolute and non-negotiable.' banner"

      - step: "1.4"
        task: "Remove duplicate scope rules from Critical Rules section"
        files:
          - "agents/hugh.md"
        details: |
          The current Critical Rules section (around line 76-89) has a
          "Scope Boundaries" subsection under Critical Rules with NEVER items:
          ```
          ### Scope Boundaries

          **NEVER:**
          - Modify Hugh source code (anything in `/Hugh/` directory)
          - Modify other PCH agent prompts (pch-*.md files)
          - Build, compile, or publish Hugh
          - Create new agent definitions
          - Modify the installer or release workflows

          For source code changes, hand off to `pch-helper`...
          ```
          Remove this entire subsection since all items are now in the
          HARD CONSTRAINT section. Keep the rest of Critical Rules intact
          (Detection-First Workflow, PATH Handling, Command Safety,
          Configuration Boundaries).
        acceptance_criteria:
          - "No 'Scope Boundaries' subsection under Critical Rules"
          - "Detection-First Workflow, PATH Handling, Command Safety, Configuration Boundaries subsections remain intact"
          - "No duplicate forbidden action rules exist in the file"

  - phase: 2
    name: "Redirect Templates and Attached File Handling"
    status: complete
    size: medium
    files_to_modify: 1
    prerequisites:
      - "Phase 1 complete — HARD CONSTRAINT section exists in agents/hugh.md"
    entry_point: "agents/hugh.md"
    verification: "HARD CONSTRAINT section exists after role paragraph with self-check pattern"
    tasks:
      - step: "2.1"
        task: "Add context-aware redirect templates after Self-Check in HARD CONSTRAINT section"
        files:
          - "agents/hugh.md"
        details: |
          Insert redirect variants after the Self-Check subsection, before
          Core Responsibilities. Each variant handles a specific out-of-scope
          input type identified in the research.

          ```markdown
          ### If User Requests Direct Work — Redirect Templates

          **Feature request** ("build X", "implement Y", "add Z"):
          > "I'm the Hugh CLI operator — I run the `hugh` command to start autonomous
          > development, but I don't write code or create documents myself. To build
          > this feature, I can run: `hugh '<feature>'`. Hugh will orchestrate the
          > full pipeline: vision → research → planning → review → coding.
          > Would you like me to run this command?"

          **Code pasted for analysis or implementation:**
          > "I don't analyze or write code directly. If you'd like to implement a
          > feature based on this code, I can run Hugh to handle it autonomously.
          > What feature should I tell Hugh to build?"

          **Agent workflow request** ("create a plan", "research this", "write a vision"):
          > "I operate the Hugh CLI, which orchestrates all agent work automatically.
          > I don't act as pch-planner, pch-researcher, or pch-visionary myself.
          > To have Hugh run the full pipeline, I can run: `hugh '<feature>'`.
          > Or ask @pch-helper for guidance on using agents directly."

          **Plan document attached:**
          > "This looks like an implementation plan. To execute it, use `@pch-coder`
          > directly — the Hugh CLI is designed for end-to-end runs starting from
          > a feature request, not for executing existing plans."

          **Research document attached:**
          > "This is a research document. To create an implementation plan from it,
          > use `@pch-planner`. Or I can start a fresh Hugh run if you describe the
          > feature you want to build."
          ```
        acceptance_criteria:
          - "5 redirect templates exist in HARD CONSTRAINT section"
          - "Each template addresses a specific input type"
          - "Feature request template offers concrete hugh command"
          - "Plan doc template redirects to @pch-coder"
          - "Research doc template redirects to @pch-planner"
          - "Agent workflow template mentions @pch-helper"

      - step: "2.2"
        task: "Add Attached File Handling section after HARD CONSTRAINT, before Core Responsibilities"
        files:
          - "agents/hugh.md"
        details: |
          Insert a new section that describes how to handle attached files.
          This section routes documents by type before any workflow logic.

          ```markdown
          ## Attached File Handling

          When a user attaches a file to their message, classify it immediately:

          | File Type | Detection | Action |
          |-----------|-----------|--------|
          | Vision document | Path contains `/docs/vision/` or file has vision YAML structure | Read file → Extract feature from Executive Summary → Workflow G |
          | Plan document | Path contains `/docs/plans/` or file has plan YAML structure | Redirect: "Use `@pch-coder` to execute this plan" |
          | Research document | Path contains `/docs/research/` or file has research YAML structure | Redirect: "Use `@pch-planner` to create a plan from this research" |
          | Source code file | Extensions: *.cs, *.js, *.py, *.ts, etc. | Do NOT analyze. Apply redirect template for code |
          | Other/unknown | Any other file type | Ask: "What would you like me to do with this? I can run Hugh to build a feature, or answer questions about Hugh." |

          **Key rule:** Never attempt to implement, analyze, or process attached
          files directly. Vision documents are the only type that triggers a
          Hugh CLI execution (via Workflow G).
          ```
        acceptance_criteria:
          - "Attached File Handling section exists between HARD CONSTRAINT and Core Responsibilities"
          - "Table covers 5 file types with detection criteria and actions"
          - "Vision docs route to Workflow G"
          - "Plan docs redirect to @pch-coder"
          - "Research docs redirect to @pch-planner"
          - "Source code files are explicitly refused"
          - "Unknown files trigger a clarifying question"

  - phase: 3
    name: "Session Initialization Expansion and Workflow G"
    status: complete
    size: medium
    files_to_modify: 1
    prerequisites:
      - "Phase 2 complete — Redirect templates and Attached File Handling exist"
    entry_point: "agents/hugh.md"
    verification: "Attached File Handling section exists. HARD CONSTRAINT section has redirect templates."
    tasks:
      - step: "3.1"
        task: "Replace 3-path session initialization tree with 7-path tree"
        files:
          - "agents/hugh.md"
        details: |
          Replace the current Session Initialization decision tree (starts at
          '## Session Initialization' and contains the ASCII tree with 3 paths:
          Knowledge Question, CLI Execution, Config Edit).

          Replace the ASCII tree block with this expanded 7-path version:

          ```
          User Request
              │
              ├─► Attached File?
              │       └─► Classify by type (see Attached File Handling)
              │               ├─► Vision doc → Workflow G
              │               ├─► Plan doc → Redirect to @pch-coder
              │               ├─► Research doc → Redirect to @pch-planner
              │               └─► Other → Clarify intent
              │
              ├─► Feature Request? ("add X", "build Y", "implement Z")
              │       └─► Construct hugh "<feature>" → Workflow B
              │
              ├─► CLI Execution (run, status, resume, doctor)
              │       └─► Detection Check
              │               ├─► Installed → Execute command
              │               └─► Not Installed → Guide installation
              │
              ├─► Knowledge Question (how does X work?)
              │       └─► Answer directly from documentation
              │
              ├─► Config Edit (change setting X)
              │       └─► Locate appsettings.json → Workflow F
              │
              ├─► Work Request Outside Scope (write code, create plan, research)
              │       └─► Apply redirect template from HARD CONSTRAINT
              │
              └─► Ambiguous Input
                      └─► Ask: "Would you like me to run Hugh to build this,
                           or are you asking about Hugh itself?"
          ```

          Keep the '## Session Initialization' heading and intro paragraph.
          Replace only the ASCII tree block.
        acceptance_criteria:
          - "Session Initialization tree has 7 top-level paths"
          - "Attached File path references Attached File Handling section"
          - "Feature Request path routes to Workflow B with hugh command"
          - "CLI Execution path unchanged from original"
          - "Work Request Outside Scope path references HARD CONSTRAINT redirect"
          - "Ambiguous Input path asks clarifying question"
          - "ASCII tree format is consistent and properly indented"

      - step: "3.2"
        task: "Add Workflow G — Process Vision Document after Workflow F"
        files:
          - "agents/hugh.md"
        details: |
          Insert Workflow G after Workflow F (Edit Configuration Setting) and
          before the '## Error Reference & Troubleshooting' section. Follow the
          existing workflow naming and formatting conventions.

          ```markdown
          ### Workflow G — Process Vision Document

          When the user attaches a vision document or points to one:

          1. **Read the vision document:**
             ```powershell
             # Read the attached or referenced vision file
             ```
             Identify the Executive Summary or feature title.

          2. **Extract key requirements:**
             - Pull the feature name/title
             - Note the first 2-3 key requirements or objectives
             - Ignore implementation details — Hugh will handle those

          3. **Construct a concise feature description:**
             Synthesize a 1-2 sentence feature description suitable for
             the `hugh` command. Example: "Add user authentication with
             OAuth2 support and role-based access control"

          4. **Confirm with the user:**
             > "I've extracted this feature from the vision document:
             > **'[feature description]'**
             >
             > I'll run: `hugh '[feature description]'`
             >
             > Would you like me to proceed, or would you like to modify
             > the feature description first?"

          5. **Detect installation** (if not cached):
             ```powershell
             Get-Command hugh -ErrorAction SilentlyContinue
             ```
             If not installed, follow Workflow A first, then return here.

          6. **Execute and monitor:**
             Same as Workflow B steps 4-5: display command, run in foreground,
             stream output, interpret results, explain phase transitions.
          ```
        acceptance_criteria:
          - "Workflow G section exists after Workflow F and before Error Reference"
          - "Title is 'Workflow G — Process Vision Document'"
          - "Has 6 numbered steps matching existing workflow format"
          - "Step 4 includes user confirmation with example dialogue"
          - "Step 5 references Workflow A for installation fallback"
          - "Step 6 references Workflow B steps 4-5 for execution and monitoring"

      - step: "3.3"
        task: "Final validation — verify line count and structural integrity"
        files:
          - "agents/hugh.md"
        details: |
          After all changes, verify:
          1. File line count is approximately 720-760 lines
          2. All section headings are intact and properly ordered:
             - Hugh CLI Operator (heading)
             - Role paragraph
             - HARD CONSTRAINT section (with redirect templates)
             - Attached File Handling
             - Core Responsibilities
             - Critical Rules (Detection-First, PATH, Command Safety, Configuration Boundaries — NO Scope Boundaries)
             - Session Initialization (7-path tree)
             - Detection Check Flow
             - CLI Command Reference
             - Workflow Instructions (A through G)
             - Error Reference & Troubleshooting
          3. No duplicate content between HARD CONSTRAINT and other sections
          4. YAML front matter is unchanged
        acceptance_criteria:
          - "File is 700-780 lines"
          - "All section headings present in correct order"
          - "No 'Scope Boundaries' subsection under Critical Rules"
          - "No '**Scope Boundaries:**' block between role paragraph and Core Responsibilities"
          - "YAML front matter unchanged (same handoffs)"
          - "Workflows A-F content unchanged"
          - "New Workflow G present between F and Error Reference"

standards:
  referenced_standards:
    - standard: "No organizational standards applicable to this plan."

implementation_complexity:
  files_to_modify: 1
  files_to_modify_score: 1
  files_to_modify_notes: "Single file: agents/hugh.md"
  new_patterns_introduced: 2
  new_patterns_introduced_score: 2
  new_patterns_notes: "HARD CONSTRAINT section (adapted from existing pattern) and Workflow G (follows existing A-F format)"
  external_dependencies: 0
  external_dependencies_score: 1
  external_dependencies_notes: "No external dependencies — pure prompt engineering"
  migration_complexity: 0
  migration_complexity_score: 1
  migration_complexity_notes: "No migration — additive changes only"
  test_coverage_required: 1
  test_coverage_required_score: 1
  test_coverage_notes: "Manual behavioral testing only — no automated prompt testing framework"
  overall_score: "6/25"
  overall_assessment: "Low complexity"

review_summary:
  review_date: "2026-02-10"
  reviewer: pch-plan-reviewer
  original_plan_version: "v2.0"
  reviewed_plan_version: "v2.1"
  review_metrics:
    issues_found: 4
    critical: 1
    major: 1
    minor: 2
    clarifying_questions_asked: 1
    sections_updated:
      - "FR-2 (self-check gate description)"
      - "Task 1.2 (line reference correction)"
      - "Task 1.3 (self-check content, permitted actions, acceptance criteria)"
  key_improvements:
    - "Added 4th self-check question covering Workflow E prerequisite commands (winget install, gh auth login) — prevents false blocking of legitimate operations"
    - "Corrected Task 1.2 line references from 'lines 19-27' to 'lines 20-25' — prevents accidental deletion of the Core Responsibilities heading"
    - "Added explicit acceptance criterion to Task 1.2 protecting line 27"
    - "Updated Permitted Actions list from 8 to 9 items to include Workflow E commands"
  remaining_considerations:
    - "Minor: Self-check Q1 wording 'Is this a Hugh CLI command?' lists non-Hugh examples (Get-Command, Test-Path). Implementer should consider whether to reword as 'Is this a Hugh CLI or detection command?' for clarity."
    - "Minor: Technical design references 'lines 96-110' for Session Initialization — the heading is at line 90 but the ASCII tree content starts around line 96, so the reference is acceptable for the tree replacement scope."
    - "The 4-question self-check is slightly longer than pch-researcher's 2-question pattern, but justified by Hugh's broader operational scope."
  sign_off: "This plan has been reviewed and is Ready for Implementation"

review_session_log:
  questions_pending: 0
  questions_resolved: 1
  last_updated: "2026-02-10"
  entries:
    - number: 1
      issue: "Self-check gate gap — Workflow E prerequisite commands would be blocked"
      category: correctness
      decision: "Option A: Add 4th self-check question for Workflow E"
      plan_update: "FR-2 updated to 4-question gate; Task 1.3 self-check updated with Q4; Permitted Actions list updated with Workflow E entry; acceptance criteria updated to 4 questions and 9 permitted actions"

handoff:
  created_by: pch-planner
  created_date: "2026-02-10"
  reviewed_by: pch-plan-reviewer
  review_date: "2026-02-10"
  status: ready-for-implementation
  next_agent: pch-coder
  plan_location: /docs/plans/016-hugh-agent-direct-work-prevention.md
