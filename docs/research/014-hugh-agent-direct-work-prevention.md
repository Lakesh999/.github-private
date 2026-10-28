id: "014"
type: research
title: "Hugh Agent Direct Work Prevention — Ensuring CLI Delegation"
status: complete
created: "2026-02-10"
current_phase: "3 of 3"
vision_source: null

introduction: |
  When users pass vision documents or feature descriptions to the @hugh agent,
  the agent incorrectly begins doing the development work itself instead of
  delegating to the Hugh CLI. The hugh agent's role is strictly to operate
  the Hugh CLI on behalf of the user — detecting installation, running commands,
  interpreting output — never performing direct development work. This research
  investigates the root causes of this behavior in the current prompt, analyzes
  how other agents enforce scope boundaries, and identifies the specific prompt
  changes needed to ensure all work requests are routed through the Hugh CLI.

objectives:
  - "What specific gaps in the current hugh.md prompt allow the agent to perform direct work instead of delegating to the Hugh CLI?"
  - "How do other pch-* agents enforce scope boundaries and prevent out-of-scope actions?"
  - "What prompt patterns, guardrails, and routing logic are needed to ensure vision documents and feature requests always go through the Hugh CLI?"

phases:
  - phase: 1
    name: "Current Prompt Gap Analysis"
    status: complete
    scope:
      - "Review the session initialization decision tree — does it account for vision documents, feature descriptions, or general work requests?"
      - "Review the scope boundaries section — does it explicitly forbid the agent from doing development work (writing code, creating plans, conducting research)?"
      - "Identify what input types are NOT covered by the current routing (vision docs, attached files, feature ideas, 'build me X' requests)"
      - "Analyze the Critical Rules — do they establish that the agent is a CLI operator, not a development agent?"
      - "Check if the agent role definition clearly states it must NEVER do direct work"
      - "Compare the current hard constraints against what pch-researcher uses for its documentation-only boundary"
    session: "2026-02-10"
    findings: |
      ## Gap Analysis Summary

      The hugh.md prompt lacks the explicit scope enforcement patterns that other PCH agents use to prevent out-of-scope actions. This creates a critical vulnerability: when users provide vision documents, feature descriptions, or "build me X" requests, the agent has no routing logic to delegate these to the Hugh CLI and may instead attempt the development work itself.

      ### 1. Session Initialization Decision Tree Gaps

      Current routing in hugh.md only covers 3 input types:
      - Knowledge Question → Answer directly
      - CLI Execution (run, status, resume, doctor) → Detection Check → Execute
      - Config Edit → Locate appsettings.json → Edit

      Missing input types with NO routing:

      | Input Type | Example | Current Behavior | Correct Behavior |
      |------------|---------|------------------|------------------|
      | Vision document attached | User attaches /docs/vision/005-feature.md | UNDEFINED — agent may try to implement | Route to hugh "<feature from vision>" |
      | Feature description | "I need user authentication with OAuth2" | UNDEFINED — agent may start planning/coding | Route to hugh "Add user authentication with OAuth2" |
      | "Build me X" request | "Build a REST API for user management" | UNDEFINED — agent likely attempts work | Route to hugh "Build a REST API for user management" |
      | Plan document attached | User attaches a plan file | UNDEFINED — no handling | Extract context, route to Hugh CLI |
      | Research document attached | User attaches research file | UNDEFINED — no handling | Explain context, route to Hugh CLI |
      | Code attached for analysis | User pastes code asking for implementation | UNDEFINED — agent may write code | Refuse; route to Hugh CLI if feature is clear |

      ### 2. Missing Hard Constraint Section

      pch-researcher has a prominent "HARD CONSTRAINT: Documentation Only — No Code Changes" section
      with forbidden/permitted action lists, self-check patterns, and redirect templates. hugh.md has
      NO equivalent. The Scope Boundaries section (lines 79-89) only prevents modifying Hugh's own
      source code — NOT preventing development work on the user's feature.

      ### 3. Critical Rules Missing CLI Operator Identity

      Current Critical Rules cover: Detection-First Workflow, PATH Handling, Command Safety,
      Configuration Boundaries. Missing rules: "You are a CLI OPERATOR, not a development agent",
      "Your only tool is the Hugh CLI — all development work flows through it",
      "If a request implies development work, ALWAYS use hugh '<request>'".

      ### 4. No Forbidden Actions List

      The agent is not explicitly told it cannot: write code for the user, create plan documents,
      conduct research, create vision documents, run commands other than Hugh CLI commands,
      or use create_file/edit tools on files outside appsettings.json.

      ### 5. No Before ANY Action Self-Check Pattern

      pch-researcher has a 2-question checkpoint before any tool call. hugh.md has no equivalent.
      A suitable self-check would be: (1) Is this a Hugh CLI command? (2) Is this reading/editing
      ONLY appsettings.json? (3) Is this answering a KNOWLEDGE question about Hugh? If ALL No → STOP.

      ### 6. No Redirect Language for Out-of-Scope Requests

      pch-researcher responds with: "I'm the research agent — I investigate topics and document
      findings but don't write code..." hugh.md has no equivalent template for redirecting
      development work requests to the Hugh CLI.

      ### 7. Workflow B Doesn't Capture Vision Document Scenario

      Workflow B ("Run a Feature") is close but incomplete — no mention of vision documents,
      attached files, or ensuring this is the ONLY path for feature requests.

      ### 8. Role Definition Not Explicit Enough

      "Does NOT replace other PCH agents" is too vague. Doesn't explicitly state: "You NEVER do
      the work that Hugh orchestrates", "You NEVER write code, create plans, or conduct research",
      "Your ONLY job is to operate the CLI".

      ### 9. Constraint Effectiveness Comparison

      | Pattern | pch-researcher | hugh.md |
      |---------|----------------|---------|
      | Hard Constraint header with emoji | Yes | Missing |
      | "STOP. This is absolute" language | Yes | Missing |
      | Forbidden Actions with bullets | Yes (5+ items) | Missing |
      | Permitted Actions with bullets | Yes (4 items) | Missing |
      | Before ANY Tool Call Self-Check | Yes (2-question) | Missing |
      | Redirect template for out-of-scope | Yes | Missing |
      | WHAT YOU MUST NEVER DO section | Yes (table) | Missing |
      | Session init handles all input types | Yes | Only 3 types |
    key_discoveries:
      - "Session Initialization decision tree only handles 3 input types — vision documents, feature descriptions, 'build me X' requests, and attached files have NO routing"
      - "Scope Boundaries section focuses on protecting Hugh source code, NOT on preventing the agent from doing development work for the user"
      - "No HARD CONSTRAINT section, Forbidden Actions list, Before ANY Action Self-Check, or redirect template exists"
      - "Agent is not told it's a 'CLI OPERATOR' whose ONLY tool is the Hugh CLI — without this explicit identity, it interprets feature requests as things IT should do"
      - "Workflow B ('Run a Feature') doesn't explicitly route vision documents or attached files to the Hugh CLI"
      - "Role definition says 'Does NOT replace other PCH agents' but this is too vague to prevent development work"
      - "Critical Rules cover detection, PATH, command safety, and config — but no rule establishes ALL development work must flow through Hugh CLI"
    relevant_files:
      - path: agents/hugh.md
        note: "Complete agent prompt (571 lines) — analyzed for all gaps in scope enforcement, session routing, and constraint patterns"
      - path: agents/pch-researcher.md
        note: "Reference for effective hard constraint patterns (lines 22-89) — self-check, forbidden actions, redirect language"
      - path: agents/pch-visionary.md
        note: "Reference for hard constraint patterns — shows consistent pattern across agents"
      - path: agents/pch-helper.md
        note: "Reference for guidance-only agent pattern — limits itself to explaining workflows without doing actual work"
    external_sources: null
    gaps: null
    assumptions:
      - "When a user attaches a vision document to @hugh, the intended behavior is to extract the feature description and run hugh '<feature>' rather than implementing the vision directly"

  - phase: 2
    name: "Scope Enforcement Patterns from Other Agents"
    status: complete
    scope:
      - "How does pch-researcher enforce its 'documentation only, no code changes' constraint? What specific patterns make this effective?"
      - "How does pch-helper limit itself to guidance without doing actual work?"
      - "What 'Before ANY Tool Call — Self-Check' patterns exist in other agents that could be adapted?"
      - "How do agents handle requests that fall outside their scope (redirect language, handoff patterns)?"
      - "What 'NEVER' lists and forbidden action patterns are most effective across agents?"
      - "Are there any patterns for catching 'catch-all' or 'default' input types that don't match known workflows?"
    session: "2026-02-10"
    findings: |
      ## 1. pch-researcher — The Gold Standard

      Located immediately after YAML front matter, before Core Responsibilities.

      ### Constraint Language
      Uses dramatic "STOP" opener: "🚫 HARD CONSTRAINT: Documentation Only — No Code Changes"
      followed by "STOP. This constraint is absolute and non-negotiable."

      ### Forbidden/Permitted Actions Pattern
      Visual ❌/✅ icons with explicit tool names (replace_string_in_file, create_file,
      run_in_terminal), file extensions (*.cs, *.js, *.py, *.ts), and edge cases
      ("quick fixes" — no exceptions). Permitted actions list specific paths (/docs/research/).

      ### Self-Check Pattern
      Pre-tool-call validation with 2 boolean questions: (1) Is target file under /docs/research/?
      (2) Is file extension .md? If EITHER is No → STOP.

      ### Redirect Language
      Explicit template: "I'm the research agent — I investigate topics and document findings
      but don't write code. Once research is complete, pch-planner will create an implementation
      plan, and pch-coder will implement it."

      ## 2. pch-visionary — Nearly Identical Pattern

      Same position, same structure as pch-researcher. Only differences: permitted path
      is /docs/vision/, redirect mentions vision agent. Has additional "When Users Present
      Implementation-Ready Requests" handling.

      ## 3. pch-planner & pch-plan-reviewer — Weaker Pattern

      Uses softer "⚠️ Documentation Only" marker. Missing: "STOP" banner, self-check pattern,
      explicit redirect language, tool names. Uses bold instead of emoji icons.

      ## 4. pch-helper — No Explicit Constraint Section

      No forbidden/permitted actions. Scope implied through description/responsibilities
      but never explicitly stated.

      ## 5. Constraint Architecture Comparison

      | Pattern | pch-researcher | pch-visionary | pch-planner | pch-helper | hugh.md |
      |---------|---------------|---------------|-------------|------------|---------|
      | HARD CONSTRAINT banner | Yes | Yes | Weak | No | No |
      | Forbidden Actions list | Yes (tool names) | Yes (tool names) | Yes (no tools) | No | Partial |
      | Permitted Actions list | Yes | Yes | Yes | No | Partial |
      | Self-check pattern | Yes (2 questions) | Yes (2 questions) | No | No | No |
      | Redirect template | Yes | Yes | No | No | No |
      | Catch-all routing | No | No | No | No | No |

      ## 6. Effective NEVER List Patterns

      Most effective: (1) Categorize by action type (modify, create, delete, run),
      (2) List file extensions explicitly, (3) Name specific tools that must NOT be used,
      (4) Address edge cases ("quick fixes" — no exceptions), (5) Explain why.
    key_discoveries:
      - "pch-researcher and pch-visionary use identical constraint architecture: HARD CONSTRAINT banner → STOP imperative → Forbidden/Permitted lists → Self-Check → Redirect template"
      - "Self-check pattern is a pre-tool-call gate with 2 boolean questions that short-circuits violations"
      - "pch-planner/pch-plan-reviewer use noticeably weaker pattern without self-check or redirect"
      - "pch-helper has NO explicit scope constraint despite being guidance-only"
      - "Explicit tool naming in forbidden actions catches tool-level violations before they happen"
      - "Edge case handling ('quick fixes' — no exceptions) closes common loopholes"
      - "Decision trees in ALL agents lack explicit catch-all/default routing for unrecognized input types"
    relevant_files:
      - path: agents/pch-researcher.md
        note: "Gold standard HARD CONSTRAINT pattern with self-check (lines 22-57)"
      - path: agents/pch-visionary.md
        note: "Identical constraint architecture (lines 41-73)"
      - path: agents/pch-planner.md
        note: "Weaker constraint pattern (lines 26-33)"
      - path: agents/pch-plan-reviewer.md
        note: "Same weaker pattern as planner (lines 18-24)"
      - path: agents/pch-helper.md
        note: "No scope constraint section (gap identified)"
      - path: agents/hugh.md
        note: "Current partial constraints needing enhancement (lines 76-94)"
    external_sources: null
    gaps: null
    assumptions: null

  - phase: 3
    name: "Required Prompt Changes for CLI Delegation"
    status: complete
    scope:
      - "Design an updated session initialization decision tree that routes vision docs, feature descriptions, and ambiguous work requests to the Hugh CLI"
      - "Define the hard constraint section — what forbidden actions must be listed (creating files, writing code, running non-Hugh commands, doing agent workflow tasks)?"
      - "Define the 'Before ANY Action — Self-Check' pattern to prevent the agent from accidentally doing work"
      - "Design the redirect language for when users ask the agent to do work directly"
      - "Define handling for attached files (vision docs, plans, research docs) — should the agent extract the feature description and pass it to `hugh \"<feature>\"`?"
      - "Define handling for edge cases: user provides a vision doc but Hugh is not installed, user asks to 'implement this feature', user provides code context expecting the agent to code"
      - "Should the agent have a 'Workflow G — Process Vision Document' or should vision docs be routed through existing Workflow B (Run a Feature)?"
    session: "2026-02-10"
    findings: |
      ## 1. Updated Session Initialization Decision Tree

      Replace the current 3-path tree with a comprehensive 7-path tree:

      1. Attached File Detected? → Route by document type:
         - Vision doc → Extract feature → Workflow G
         - Plan doc → Redirect to @pch-coder
         - Research doc → Redirect to @pch-planner
      2. Feature Request? ("Add X", "Build Y", "Implement Z") → Workflow G — construct hugh "<feature>"
      3. Explicit Hugh Command? → Workflow B-E as appropriate
      4. Knowledge Question? → Answer directly
      5. Config Edit? → Workflow F
      6. Work Request Outside Scope? → REDIRECT using template
      7. Ambiguous Input → Ask clarifying question: "Run Hugh to build this, or asking about Hugh?"

      ## 2. Hard Constraint Section

      Add "🚫 HARD CONSTRAINT: CLI Operator Only — No Direct Development Work" section
      immediately after the role definition (line 21), before Core Responsibilities.

      ### Forbidden Actions (NEVER do these):
      - Create/modify source code files (*.cs, *.js, *.py, *.ts, etc.)
      - Create/modify config files EXCEPT appsettings.json per Workflow F
      - Write implementation code, tests, or documentation content
      - Execute terminal commands that modify codebase
      - Act as pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer, or pch-coder
      - Create vision documents, research documents, or implementation plans
      - Analyze code and suggest implementations
      - Make "quick fixes" or "small changes" to code — no exceptions

      ### Permitted Actions (ONLY these):
      - Run hugh "<feature>" to start autonomous development
      - Run hugh --status, --resume, doctor
      - Run detection commands: Get-Command, Test-Path, Get-Content
      - Install Hugh via Workflow A
      - Read/edit ONLY appsettings.json per Workflow F
      - Answer questions about Hugh's capabilities and configuration
      - Hand off to @pch-helper for agent workflow guidance

      ## 3. Self-Check Pattern

      3 boolean questions before ANY terminal command or file-editing tool:
      1. Is this a Hugh CLI command? (hugh, Get-Command hugh, Test-Path)
      2. Is this an installation command from Workflow A?
      3. Is this editing appsettings.json via Workflow F?

      If ALL answers are "No" → STOP. Do not proceed.

      Alternative simplified 2-question version:
      1. Is this running the hugh executable or detecting its presence?
      2. Am I following an explicit Workflow (A-G) step?
      If BOTH No → STOP.

      ## 4. Redirect Language

      Primary template: "I'm the Hugh CLI assistant — I run the hugh command to start
      autonomous development, but I don't write code or create documents myself.
      To build this feature, I can run: hugh '<feature>'. Hugh will orchestrate the
      full pipeline: vision → research → planning → review → coding.
      Would you like me to run this command?"

      Variants for: code attached, feature request without Hugh mention, code analysis.

      ## 5. Attached File Handling

      New section after Hard Constraint, before Core Responsibilities:

      - Vision docs (/docs/vision/*.md): Read file, extract feature summary, construct
        hugh "<feature>" command, confirm with user, execute
      - Plan docs (/docs/plans/*.md): Redirect to @pch-coder
      - Research docs (/docs/research/*.md): Redirect to @pch-planner or offer fresh Hugh run
      - Other files (code, configs): Do NOT analyze, apply redirect template

      ## 6. Edge Case Handling

      - Vision doc + Hugh not installed: Acknowledge vision, install Hugh first (Workflow A),
        then return to vision doc
      - "Implement this feature" / "build this": Treat as feature request → Workflow G
      - Code context expecting coding: Apply redirect template, do NOT analyze
      - Agent workflow questions: Hand off to @pch-helper

      ## 7. Workflow G — Process Vision Document (YES, create new workflow)

      Rationale: Workflow B is for text feature descriptions. Vision docs require file
      reading, feature extraction, and confirmation — a dedicated workflow is cleaner.

      Steps:
      1. Read vision document, identify Executive Summary or feature title
      2. Extract key requirements (first 2-3 bullets)
      3. Construct concise feature description
      4. Confirm with user: "I've extracted: '<feature>'. Run Hugh with this, or modify?"
      5. Detect installation (if not cached) and execute
      6. Monitor and report as with Workflow B

      ## Summary of Changes by Location

      | Location | Action | Content |
      |----------|--------|---------|
      | After line 21 (role definition) | INSERT | Hard Constraint section (~50 lines) |
      | After Hard Constraint | INSERT | Attached File Handling (~40 lines) |
      | Lines 96-133 (session init) | REPLACE | Updated 7-path decision tree (~45 lines) |
      | After Workflow F (~line 445) | INSERT | Workflow G (~35 lines) |

      Total estimated additions: ~170 lines. Final file ~740 lines.

      ## Implementation Priority

      1. Critical: Hard Constraint section with Forbidden/Permitted actions
      2. Critical: Updated Session Initialization decision tree
      3. High: Workflow G for vision documents
      4. High: Attached File Handling section
      5. Medium: Edge case handling subsection
      6. Medium: Redirect variants
    key_discoveries:
      - "Hugh agent needs identity reframe: 'CLI OPERATOR' not 'CLI assistant' — current language allows interpretation as a development agent"
      - "Vision document handling requires new Workflow G — overloading Workflow B would create confusion"
      - "Self-check pattern needs 3 questions (not 2) because Hugh has 3 permitted command categories: CLI, installation, and config"
      - "Redirect template must offer concrete hugh '<feature>' command, not just explain boundaries"
      - "Attached file handling must immediately classify by document type and route accordingly"
      - "'Quick fixes — no exceptions' clause is essential — users will try 'just this one small change'"
      - "Edge case for 'Hugh not installed + vision doc attached' requires sequenced handling: install first, then return to vision"
    relevant_files:
      - path: agents/hugh.md
        note: "Primary file to be modified — all changes target this file"
      - path: agents/pch-researcher.md
        note: "Gold standard for HARD CONSTRAINT pattern (lines 19-56)"
      - path: agents/pch-visionary.md
        note: "Secondary reference for HARD CONSTRAINT pattern (lines 39-64)"
    external_sources: null
    gaps:
      - "Did not design testing approach for verifying constraints in practice"
      - "No guidance on handling users who repeatedly ignore redirects"
    assumptions:
      - "Vision documents have an identifiable Executive Summary or title that can be extracted"
      - "Feature description extraction can be done reliably with read_file"

overview: |
  This research identifies why the @hugh Copilot agent fails to delegate work to
  the Hugh CLI when users provide vision documents or feature descriptions, and
  provides concrete prompt changes to fix the behavior.

  **Root Cause:** The hugh.md agent prompt lacks the scope enforcement infrastructure
  that other PCH agents use. The session initialization decision tree only routes 3
  input types (knowledge questions, explicit CLI commands, config edits) — vision
  documents, feature descriptions, "build me X" requests, and attached files have
  NO routing whatsoever. Without routing, the agent interprets these as tasks it
  should perform directly. There is no hard constraint section, no forbidden actions
  list, no self-check pattern, and no redirect template.

  **Pattern Analysis:** The pch-researcher and pch-visionary agents demonstrate
  the gold standard for scope enforcement: a prominent HARD CONSTRAINT banner,
  forbidden/permitted action lists with tool names, a pre-tool-call self-check gate,
  and redirect templates with actionable alternatives. These patterns are proven
  effective and directly adaptable for the Hugh agent.

  **Solution:** Four critical changes to agents/hugh.md will fix the behavior:
  1. Add a HARD CONSTRAINT section reframing the agent as a "CLI OPERATOR" (not
     "CLI assistant") with explicit forbidden/permitted actions and a 3-question
     self-check before any action.
  2. Replace the 3-path session initialization tree with a 7-path tree that routes
     attached files, feature descriptions, explicit commands, knowledge questions,
     config edits, out-of-scope requests, and ambiguous input.
  3. Add an Attached File Handling section that classifies documents by type
     (vision → extract and run Hugh, plan → redirect to pch-coder, research →
     redirect to pch-planner).
  4. Add Workflow G — Process Vision Document, which reads a vision doc, extracts
     the feature description, confirms with the user, and executes via Hugh CLI.

  These changes add ~170 lines, bringing the file from ~571 to ~740 lines — well
  within the recommended 400-800 line range for a focused CLI agent.

key_findings:
  - "Session initialization only routes 3 input types — vision docs, feature descriptions, and work requests fall through with no routing, causing the agent to attempt direct work"
  - "No HARD CONSTRAINT section exists — the agent has no explicit identity as a CLI operator and no forbidden actions preventing development work"
  - "pch-researcher/pch-visionary HARD CONSTRAINT pattern (banner → STOP → forbidden/permitted → self-check → redirect) is the proven gold standard for scope enforcement"
  - "Self-check pattern (boolean questions before any tool call) is the most critical missing element — it's the last gate before a constraint violation"
  - "Vision document handling requires a new Workflow G with feature extraction, confirmation, and CLI execution"
  - "Redirect template must offer a concrete hugh '<feature>' command, not just explain boundaries — users need an actionable path forward"
  - "The 'quick fixes — no exceptions' clause is essential for closing the common loophole"

actionable_conclusions:
  - "Add HARD CONSTRAINT section after line 21 of agents/hugh.md with forbidden/permitted actions, self-check, and redirect template (~50 lines)"
  - "Replace session initialization decision tree (lines 96-133) with 7-path routing covering all input types (~45 lines)"
  - "Add Attached File Handling section classifying vision/plan/research/other documents (~40 lines)"
  - "Add Workflow G — Process Vision Document after Workflow F (~35 lines)"
  - "Reframe role identity from 'CLI assistant' to 'CLI OPERATOR' in the role definition"
  - "Include explicit tool names in forbidden actions (create_file, replace_string_in_file, run_in_terminal for non-Hugh commands)"

open_questions:
  - "How to handle users who repeatedly ignore redirects and insist the agent do direct work"
  - "Should the YAML front matter handoffs be updated to include a pch-helper handoff for seamless agent switching?"
  - "How to verify these constraints work in practice — no testing framework for agent prompt behavior exists"

standards: null

handoff:
  created_by: pch-researcher
  created_date: "2026-02-10"
  status: complete
  current_phase: complete
  path: /docs/research/014-hugh-agent-direct-work-prevention.md
