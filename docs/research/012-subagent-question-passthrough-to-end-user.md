id: "012"
type: research
title: "Subagent Question Passthrough to End-User"
status: complete
created: "2026-02-08"
current_phase: "4 of 4"

introduction: |
  When pch-visionary, pch-researcher, pch-planner, and pch-plan-reviewer agents
  run as subagents, they may generate clarifying questions intended for the end-user.
  However, the parent/orchestrating agent sometimes intercepts and answers these
  questions itself instead of passing them through. This research investigates the
  mechanics of why this happens and identifies strategies to ensure subagent
  questions reliably reach the end-user.

objectives:
  - "Understand why the main agent answers questions meant for the end-user when running subagents"
  - "Identify what mechanisms exist (or are missing) in the runSubagent tool to surface questions"
  - "Catalog how each affected agent (visionary, researcher, planner, reviewer) generates clarifying questions"
  - "Propose concrete prompt engineering and/or architectural strategies to ensure end-user questions are passed through"

phases:
  - phase: 1
    name: "Subagent Communication Mechanics"
    status: complete
    scope:
      - "Analyze how the runSubagent tool works — what gets sent to the subagent and what comes back"
      - "Determine what happens to subagent output that contains questions — does the parent see it as a 'final report' to interpret?"
      - "Identify the fundamental constraint: subagents cannot interact with the user directly; they return a single message to the parent"
      - "Research whether the parent agent's system prompt or tooling instructs it to 'incorporate decisions and continue' vs. surface questions"
      - "Examine existing runSubagent documentation/behavior for any built-in question-passthrough patterns"
    session: "2026-02-08"
    findings: |
      ## Phase 1 Findings: Subagent Communication Mechanics
      
      ### 1. How the runSubagent Tool Works
      
      The `runSubagent` tool is the built-in VS Code mechanism for invoking subagents. Based on system prompt documentation and prior research:
      
      **Tool Parameters:**
      | Parameter | Type | Required | Description |
      |-----------|------|----------|-------------|
      | `prompt` | string | Yes | A detailed description of the task for the agent to perform |
      | `description` | string | Yes | A short (3-5 word) description of the task |
      | `agentName` | string | No | Optional name of a specific agent to invoke |
      
      **Critical Behavioral Characteristics:**
      
      1. **Synchronous Execution**: Subagents do NOT run asynchronously — the parent waits for completion
      2. **Single Return Message**: When done, the subagent returns **only one message** to the parent
      3. **Result Invisible to User**: From tool docs: "The result returned by the agent is **not visible to the user** — you must send a text message summarizing the result"
      4. **Stateless Invocation**: Each invocation is independent — cannot send follow-up messages
      5. **Autonomous Operation**: Subagents operate **without pausing for user feedback**
      
      **Source:** [docs/research/001-subagent-integration-for-pch-coder.md](001-subagent-integration-for-pch-coder.md) lines 121-137
      
      ### 2. The Fundamental Constraint: No User Interaction
      
      The research unequivocally confirms:
      
      > "Subagents cannot ask the user questions during execution"
      > — [docs/research/001-subagent-integration-for-pch-coder.md](001-subagent-integration-for-pch-coder.md) line 178
      
      > "No User Interaction: Subagents cannot ask clarifying questions"
      > — [docs/research/001-subagent-integration-for-pch-coder.md](001-subagent-integration-for-pch-coder.md) line 396
      
      This is a **hard architectural constraint**, not a prompt engineering issue. The subagent communication channel is:
      
      ```
      Parent Agent → runSubagent(prompt) → Subagent executes autonomously → Single result → Parent
                                                  ↑
                                      NO user interaction possible here
      ```
      
      When a subagent encounters ambiguity requiring clarification, the documented workaround is:
      1. Make a reasonable assumption and document it, OR
      2. Return a "blocked" status with questions for the **orchestrator** (not the user)
      
      **Source:** [docs/research/001-subagent-integration-for-pch-coder.md](001-subagent-integration-for-pch-coder.md) lines 961-963
      
      ### 3. What Happens to Subagent Output Containing Questions
      
      The parent agent receives the subagent's return as a **result to be processed**, not a message to be forwarded. Key evidence:
      
      **From runSubagent tool documentation:**
      > "The result returned by the agent is **not visible to the user** — you must send a text message summarizing the result"
      
      This instruction establishes the parent's role: **interpret and summarize**, not **forward verbatim**. When a subagent's output contains a question, the parent's default behavior is to treat it as information to incorporate into its own processing, not as a message requiring user response.
      
      **Current PCH agent return formats reinforce this:**
      
      pch-coder subagent return format:
      ```
      PHASE: {N}
      STATUS: success | partial | blocked
      TASKS_COMPLETED: [...]
      TASKS_BLOCKED: [...]
      FILES_MODIFIED: [...]
      DEVIATIONS: [...]
      NOTES: [...]
      ```
      
      pch-researcher subagent return format:
      ```
      PHASE: {N}
      STATUS: success | partial | blocked
      SCOPE_COMPLETED: [...]
      FINDINGS: [...]
      KEY_DISCOVERIES: [...]
      GAPS_FOUND: [...]
      ```
      
      **Notably absent**: Any field like `QUESTION_FOR_USER`, `CLARIFICATION_NEEDED`, or `USER_INPUT_REQUIRED`. The return formats are designed for structured data extraction, not question forwarding.
      
      ### 4. Parent Agent Instructions for Subagent Results
      
      The pch-coder and pch-researcher agents include explicit parsing instructions for subagent results. From [agents/pch-coder.md](../../agents/pch-coder.md) lines 290-320:
      
      **Status Interpretation:**
      - `success`: All tasks completed → Proceed to next phase
      - `partial`: Some tasks blocked → Pause and present options
      - `blocked`: No progress → Stop and present blocker details
      
      **There is NO instruction** that says: "If the subagent return contains a question, surface it to the user." Instead, the parent is instructed to:
      1. Parse the structured fields
      2. Update the plan/research document
      3. Display a phase report
      4. Continue or pause based on STATUS
      
      When `blocked` status is returned, the orchestrator presents **its own options** to the user (Continue, Skip, Stop, Retry) rather than forwarding the subagent's original question.
      
      ### 5. No Built-in Question-Passthrough Mechanism
      
      The research confirms there is **no existing mechanism** in:
      - The `runSubagent` tool itself
      - The subagent return format conventions
      - The parent agent processing instructions
      
      ...that automatically surfaces subagent questions to the user.
      
      The closest pattern is the `ask_user` tool in the ACP (Agent Communication Protocol) layer, but this is used by agents to ask users directly — it's not available within the subagent context for passthrough purposes.
      
      **Key insight from prior research:**
      > "Clarifying questions are incompatible with subagents: Must handle before invocation or instruct subagent to document uncertainty"
      > — [docs/research/002-beta-pch-researcher-subagent-integration.md](002-beta-pch-researcher-subagent-integration.md) line 283
      
      ### 6. Why the Parent Answers Questions Itself
      
      Based on the mechanics analyzed, the parent agent answers subagent questions because:
      
      1. **Design Intent**: The runSubagent tool explicitly states results are "not visible to the user" — the parent is expected to summarize/interpret
      
      2. **No Question Detection**: Current return formats don't include question markers, so the parent has no signal to forward anything
      
      3. **Autonomy Expectation**: Subagents are designed for autonomous execution — the system assumes they'll make decisions, not ask questions
      
      4. **Structured Parsing**: Parent agents parse for STATUS, FILES, NOTES etc. — questions embedded in prose are treated as context, not action items
      
      5. **"Trust the Output"**: Tool documentation says "The agent's outputs should generally be trusted" — encouraging the parent to incorporate rather than question
      
    key_discoveries:
      - "runSubagent returns are explicitly 'not visible to the user' — parent must summarize, not forward"
      - "No built-in mechanism exists to passthrough subagent questions to end-users"
      - "Subagent return formats (STATUS, FILES, NOTES) lack any question-related fields"
      - "Parent agents parse structured returns and are not instructed to look for embedded questions"
      - "'Blocked' status triggers orchestrator-generated options, not subagent question forwarding"
      - "The fundamental constraint is architectural: subagents cannot interact with users during execution"
    relevant_files:
      - path: docs/research/001-subagent-integration-for-pch-coder.md
        note: "Primary source for runSubagent tool mechanics and limitations"
      - path: docs/research/002-beta-pch-researcher-subagent-integration.md
        note: "Documents incompatibility between clarifying questions and subagents"
      - path: agents/pch-coder.md
        note: "Contains subagent return format and processing instructions"
      - path: agents/pch-researcher.md
        note: "Contains subagent return format for research phases"
      - path: docs/research/008-self-operating-development-loop.md
        note: "Documents ask_user tool and question interception patterns"
    external_sources:
      - "VS Code Documentation: Manage chat sessions - Context-isolated subagents"
    gaps:
      - "Could not access live VS Code system prompts to verify exact runSubagent tool description wording"
      - "No direct observation of parent agent behavior when receiving question-containing subagent output"
    assumptions:
      - "Assumed the runSubagent tool documentation quoted in prior research remains current"
      - "Assumed PCH agents use the documented return formats consistently"

  - phase: 2
    name: "Agent Question Patterns and Detection"
    status: complete
    scope:
      - "Catalog the specific question formats used by pch-visionary, pch-planner, pch-plan-reviewer, and pch-researcher"
      - "Identify structural markers that distinguish user-directed questions from internal reasoning"
      - "Analyze which question types are most vulnerable to parent-agent interception (open-ended vs. structured multi-choice)"
      - "Document how Hugh's SignalClassifier and QuestionHandler detect questions — patterns that could inform prompt design"
      - "Determine if agents use any 'question tagging' or explicit user-directed markers currently"
    session: "2026-02-08"
    execution_mode: automatic
    findings: |
      ## Question Formats by Agent

      ### pch-visionary: Three Question Types

      Type A — Elicitation Questions (Open-Ended):
      Uses bold "Question:" prefix with italicized guidance text. No options provided, purely open-ended.
      Example format: "### [Topic]\n**Question:** [Open-ended question]\n*Take your time...*"

      Type B — Refinement Questions (Multiple Choice):
      Structured with lettered options A-E, bold option names with descriptions, and a
      "**Recommendation:** [Letter] — [Option Name]" block with rationale.

      Type C — Validation Questions (Confirm/Refine):
      Uses numbered options (1, 2, 3) with block quote summary. Format:
      "**Is this accurate?** 1. Yes 2. Partially correct 3. No, let me clarify"

      ### pch-planner: Structured Multiple Choice

      Enforces standardized format: question stated first, options labeled A-E with 3-5 options,
      recommendation after options with letter reference, justified rationale required.
      Uses exact "**Recommendation:** [Letter] — [Option]" pattern that Hugh can auto-accept.

      ### pch-plan-reviewer: Numbered Questions with Table

      Uses "**Review Question [N of M]:**" prefix with progress counts, table-formatted options
      (3 columns: Option, Description, Trade-offs), and remaining count italicized at bottom.

      ### pch-researcher: Clarification Pause Format

      Uses "⏸️ **Automatic Execution Paused — Clarification Needed**" marker with
      "**Clarification Request:**" section header and programmatic response options.

      ## Hugh's Signal Detection System

      ### SignalType Enum

      Six signal types in SignalClassifier:
      - None: No signal detected
      - PhaseComplete: Phase finished — advance
      - WorkflowComplete: All phases done — transition
      - QuestionWithRecommendation: Question + recommendation — auto-accept
      - QuestionOpen: Open question — pause for human input
      - Blocker: Progress blocked — pause and alert
      - ModeSelection: Mode choice — auto-select Manual

      ### SignalClassifier Regex Patterns

      Recommendation Detection (key for questions):
      Regex: \*\*Recommendation:\*\*\s+([A-E1-5])\s*[—–\-]\s*(.+)
      Captures letter (A-E or 1-5) and option text.

      Open Question Detection:
      Regex: \*\*(?:How would you like to proceed|What would you like|Please (?:choose|select|specify))\?\*\*

      ### QuestionHandler Behavior

      - QuestionWithRecommendation -> Auto-accept recommendation letter
      - QuestionOpen -> Pause (returns Pause() — requires human input)
      - ModeSelection -> Auto-select "1" (Manual)

      The handler auto-responds ONLY when recommendation letter pattern is detected.
      Open questions force a pause but don't relay question content to users.

      ## Structural Markers Analysis

      Current user-directed question markers:
      - "**Question:**" bold prefix (visionary, planner)
      - "**Review Question [N of M]:**" numbered prefix (reviewer)
      - "**Clarification Request:**" section header (researcher)
      - "**Options:**" block label (visionary, planner)
      - "**Recommendation:**" block (all agents with choices)
      - ⏸️ emoji pause/clarification marker (researcher)

      No explicit "question tagging" found — no agents use [USER-QUESTION] tags,
      structured metadata for question routing, or machine-readable question identifiers.
      Questions are distinguished only by visual formatting, structural position, and keyword patterns.

      ## Vulnerability Analysis: Parent-Agent Interception

      Question types by passthrough risk:
      - Open-ended (Type A): HIGH risk — no structure for parent to detect or forward
      - Multi-choice with recommendation: LOW risk — auto-accepted by Hugh
      - Validation (numbered): MEDIUM risk — may lack recommendation pattern
      - Clarification pause: HIGH risk — parent treats as blocker, not passthrough

      Pattern Coverage Gaps in SignalClassifier:
      - Does NOT detect elicitation questions without **Recommendation:**
      - Does NOT detect validation questions using numbered options (1/2/3)
      - Does NOT detect follow-up questions embedded in prose
      - Does NOT detect pch-researcher's clarification format
      Only questions with exact **Recommendation:** [A-E1-5] — [text] pattern are auto-handled.
    key_discoveries:
      - "pch-visionary uses 3 distinct question types: Elicitation (open-ended), Refinement (A-E options + recommendation), and Validation (1/2/3 numbered confirmation)"
      - "pch-planner enforces strict **Recommendation:** [Letter] — [Option] format which Hugh can auto-accept"
      - "pch-plan-reviewer uniquely uses **Review Question [N of M]:** prefix with table-formatted options"
      - "pch-researcher uses ⏸️ Clarification Needed and Clarification Request markers for pause states"
      - "Hugh's SignalClassifier only detects questions via **Recommendation:** regex — open-ended questions cause pause, not forwarding"
      - "QuestionHandler auto-accepts recommendations but pauses on open questions without relaying content"
      - "No agents currently use explicit question tagging or [USER-QUESTION] markers"
      - "Open-ended and validation questions are most vulnerable to interception because they lack the recommendation pattern"
    relevant_files:
      - path: agents/pch-visionary.md
        note: "Three question types (Elicitation/Refinement/Validation)"
      - path: agents/pch-planner.md
        note: "Structured question format with required recommendation block"
      - path: agents/pch-plan-reviewer.md
        note: "Numbered review question format with table options"
      - path: agents/pch-researcher.md
        note: "Clarification pause format with ambiguity triggers"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "Regex patterns for question detection, recommendation extraction"
      - path: Hugh/Detection/SignalType.cs
        note: "Enum defining QuestionWithRecommendation vs QuestionOpen signals"
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "Handler logic for auto-accepting recommendations vs pausing on open questions"
    external_sources: null
    gaps: null
    assumptions:
      - "Current agent prompts represent the complete question format catalog — agents may generate ad-hoc questions not covered by templates"
      - "Hugh's SignalClassifier represents the only question detection mechanism in use"

  - phase: 3
    name: "Parent Agent Interception Analysis"
    status: complete
    scope:
      - "Examine the parent agent's instructions when it receives a subagent response containing questions"
      - "Identify specific prompt language that causes the parent to answer on behalf of the user (e.g., 'incorporate decisions', 'continue without re-asking')"
      - "Analyze the ask_questions tool — could subagents or the parent use it to surface questions reliably?"
      - "Research whether the parent agent treats subagent returns differently from user messages"
      - "Investigate if there's a pattern where the parent correctly passes through questions vs. when it doesn't"
    session: "2026-02-08"
    execution_mode: automatic
    findings: |
      ## Parent Agent Instructions When Receiving Subagent Responses

      Both pch-coder and pch-researcher as parent orchestrators have explicit instructions for
      processing subagent results. The instructions are 100% focused on structured data parsing
      with no mention of question detection or passthrough.

      Parsing tables in both agents include: PHASE, STATUS, TASKS_COMPLETED/FINDINGS, GAPS_FOUND,
      etc. — all data fields. Neither parsing table includes ANY instruction related to detecting
      questions, forwarding questions to the user, or treating question-containing responses differently.

      ## Specific Prompt Language Causing Interception

      Three categories of language contribute to question interception:

      Category A — "Cannot Ask" Preemptive Statements:
      Both agents include: "Automatic mode cannot ask clarifying questions during execution."
      This conditions the parent to expect that questions WON'T exist in subagent output.

      Category B — Autonomous Completion Expectation:
      Subagent invocation templates tell subagents to complete work without questions.
      Expected outputs are completion statuses, not interactive dialogue. Gaps are expected
      to be reported as data, not as interactive questions requiring user response.

      Category C — "Blocked" Status Triggers Parent-Generated Options:
      When blocked status returns, the parent does NOT forward the subagent's question.
      Instead, it generates its own 5-option menu (Continue, Skip, Stop, Retry, Clarify).
      The blocker details are displayed as informational context, but the QUESTIONS asked
      are parent-generated, not forwarded from the subagent.

      ## The ask_questions Tool Analysis

      The ask_questions tool (VS Code system prompt level) has specific characteristics:
      - Designed for parent agents to ask users clarifying questions
      - Parameters: header, question text, options array, multiSelect flag
      - Constraint: "Only use this tool when the user's answer provides information you cannot
        determine or reasonably assume yourself"
      - Available to both parent and subagent contexts

      Why it doesn't help subagent passthrough currently:
      1. Subagent context isolation — if subagent uses it, parent doesn't see the interaction
      2. Parent isn't instructed to use it for relaying subagent questions
      3. The ask_questions tool description says "If a question has an obvious best answer,
         take that action instead of asking" — this actually ENCOURAGES the parent to answer
         questions itself rather than passing them through

      Could the parent use ask_questions to relay subagent questions? YES, theoretically:
      1. Detect that subagent output contains a user-directed question
      2. Extract the question text
      3. Invoke ask_questions with that text
      4. Await the response before continuing

      But NO PCH agent currently has instructions to do this.

      ## Differential Treatment: Subagent Returns vs User Messages

      Key difference: Subagent returns are treated as data, user messages as dialogue.

      runSubagent tool documentation: "The result returned by the agent is not visible to the
      user — you must send a text message summarizing the result." This explicitly tells the parent
      that subagent output is raw data requiring interpretation and summarization.

      When subagent output contains something that LOOKS like a question, the parent's processing
      mindset treats it as context/notes to incorporate, not a dialogue turn to relay.

      ## Successful Passthrough Pattern: Pre-Phase Clarification Check

      One pattern where questions correctly reach the user: Pre-Phase Clarification Check
      (BEFORE subagent invocation).

      pch-researcher checks for ambiguity triggers (TBD markers, empty scope, vague language)
      BEFORE invoking a subagent. When detected, it pauses with "Clarification Needed" and
      asks the user directly. This works because:
      1. The parent agent itself generates the question
      2. The question is asked IN the parent context (not subagent context)
      3. The user can reply directly
      4. The parent receives the reply as a normal user message

      Pattern contrast:
      - Pre-phase clarification (parent generates): User SEES question
      - Subagent blocked status (subagent returns in NOTES): User does NOT see question
      - Subagent embeds in FINDINGS (prose): User does NOT see question

      ## Hugh's External Orchestrator Behavior

      Hugh's QuestionHandler pauses on open questions but does NOT relay question text to users.
      The pause message says "Execution paused for user input" but doesn't include WHAT question
      was asked. The question text (classification.RawMatch) is only logged, not displayed.
    key_discoveries:
      - "Parent agent instructions focus 100% on structured data parsing with ZERO mention of question detection or passthrough"
      - "Explicit 'Automatic mode cannot ask clarifying questions' language conditions parent to not expect/look for questions"
      - "When blocked status occurs, parent generates its own options menu instead of forwarding subagent's original questions"
      - "ask_questions tool exists and could theoretically relay subagent questions, but no agent is instructed to use it this way"
      - "ask_questions tool description actually ENCOURAGES answering questions rather than asking: 'If a question has an obvious best answer, take that action'"
      - "Subagent returns are explicitly framed as 'data to process' not 'dialogue to forward'"
      - "Pre-phase clarification check is the ONLY pattern where questions reach users — because parent generates them directly"
      - "Hugh's QuestionHandler pauses on open questions but does NOT relay question text to user — only logs it"
    relevant_files:
      - path: agents/pch-coder.md
        note: "'Cannot ask clarifying questions' language and Processing Subagent Results section with no question handling"
      - path: agents/pch-researcher.md
        note: "Pre-Phase Clarification Check (successful pattern) and Processing Subagent Results (no question handling)"
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "HandleOpenQuestion pauses but doesn't relay question content"
      - path: Hugh/Agents/CopilotService.cs
        note: "Pause message says 'for user input' but doesn't include the question"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "Recommendation regex is only question detection pattern"
      - path: docs/research/008-self-operating-development-loop.md
        note: "ask_user tool analysis and ACP interception patterns"
    external_sources: null
    gaps:
      - "Could not directly test whether parent agents would correctly forward questions if explicitly instructed to via prompt changes"
      - "Did not verify if recent VS Code updates have added new subagent question-handling capabilities"
    assumptions:
      - "Current documented behavior in PCH agent prompts is actively enforced"
      - "Hugh's SignalClassifier.cs represents the complete detection logic used in practice"

  - phase: 4
    name: "Mitigation Strategies and Recommendations"
    status: complete
    scope:
      - "Strategy 1: Subagent return format — explicit QUESTION_FOR_USER field that the parent is instructed to surface"
      - "Strategy 2: Parent prompt engineering — instructions to never answer questions from subagents but always pass them through"
      - "Strategy 3: Pre-flight question extraction — have the parent detect and surface questions BEFORE invoking subagents"
      - "Strategy 4: ask_questions tool usage — instruct the parent to use the ask_questions tool when subagent returns contain questions"
      - "Strategy 5: Subagent prompt changes — instruct subagents to return STATUS: needs_user_input with structured question data"
      - "Evaluate each strategy for feasibility, reliability, and impact on the existing workflow"
      - "Provide concrete prompt language changes for the recommended approach(es)"
    session: "2026-02-08"
    execution_mode: automatic
    findings: |
      ## Strategy 1: Subagent Return Format — QUESTION_FOR_USER Field

      Add an explicit field to the subagent return format that flags questions requiring user input:
      QUESTION_FOR_USER, QUESTION_TYPE (elicitation/refinement/validation), QUESTION_OPTIONS, RECOMMENDATION.

      Feasibility: HIGH — subagents can add any field to return format.
      Reliability: MEDIUM — depends on subagents correctly populating the field.
      Workflow Impact: LOW — new fields are additive, existing logic remains intact.
      Hugh Impact: MEDIUM — needs code updates to detect new field.

      ## Strategy 2: Parent Prompt Engineering — "Never Answer, Always Pass Through"

      Add instructions forbidding parent from answering any questions in subagent output.

      Feasibility: PARTIALLY FEASIBLE — challenge is detecting questions without structured markers.
      Reliability: LOW — without explicit markers, LLM pattern matching is inconsistent.
      May cause over-pausing with false positives. Not recommended as standalone strategy.

      ## Strategy 3: Pre-Flight Question Extraction (Before Subagent Invocation)

      Extend the proven Pre-Phase Clarification Check pattern to catch more ambiguities before
      invoking subagents. Add new "Decision Point Triggers" alongside existing "Ambiguity Triggers":
      technology choices, configuration dependencies, multi-option scope, user preference implicit,
      external dependencies.

      Feasibility: HIGH — extension of proven pattern.
      Reliability: HIGH for known ambiguities, MEDIUM for emergent questions during execution.
      Workflow Impact: LOW — natural extension of existing pattern.
      Cannot catch questions that arise only during subagent execution.

      ## Strategy 4: ask_questions Tool Usage by Parent

      Instruct parent to use VS Code ask_questions tool when detecting subagent question content.

      Feasibility: HIGH in VS Code, NOT FEASIBLE in Hugh (external CLI orchestrator).
      Reliability: MEDIUM — still requires detecting questions first.
      The ask_questions tool description says "If a question has an obvious best answer, take
      that action instead" which ENCOURAGES answering rather than relaying.
      Context-dependent: works for VS Code but breaks Hugh workflow.

      ## Strategy 5: Subagent Prompt Changes — STATUS: needs_user_input (RECOMMENDED PRIMARY)

      Add new STATUS value alongside success/partial/blocked. Makes question passthrough a
      first-class flow control mechanism that cannot be ignored:
      - success -> Continue to next phase
      - partial -> Pause with options
      - blocked -> Stop with blocker details
      - needs_user_input -> Pause, display question, wait for answer, re-invoke

      Feasibility: HIGH — adding a status value is straightforward.
      Reliability: HIGH — STATUS is already the primary control mechanism; new value is impossible to miss.
      Workflow Impact: LOW — additive change, existing statuses unchanged.

      ## Recommended Hybrid Approach

      Combine multiple strategies for defense-in-depth:

      | Layer | Strategy | Purpose |
      | Prevention | Strategy 3 (Pre-Flight) | Catch predictable ambiguities BEFORE subagent invocation |
      | Signal | Strategy 5 (needs_user_input) | Explicit status for emergent questions during execution |
      | Format | Strategy 1 (QUESTION_FOR_USER) | Structured data for question details |
      | Relay | Strategy 4 (ask_questions) | VS Code mechanism to surface questions |
      | Backup | Strategy 2 (Never Answer) | Reinforcing instruction for edge cases |

      Implementation Order:
      1. Subagent return format update (Strategy 1+5) — add needs_user_input status and question fields
      2. Parent processing update (Strategy 4+2) — add question detection and relay logic
      3. Pre-flight enhancement (Strategy 3) — expand clarification check triggers
      4. Hugh code update (Strategy 5 detection) — add NeedsUserInput signal type and handler

      ## Concrete Changes Required

      Prompt changes in pch-coder.md and pch-researcher.md:
      - Subagent invocation template: add needs_user_input to STATUS values, add QUESTION_FOR_USER,
        QUESTION_OPTIONS, RECOMMENDATION fields
      - Parent processing: add "Handling needs_user_input Status" section with instructions
        to display question, wait for response, and re-invoke subagent with answer
      - Critical instruction: "Do NOT answer the question yourself"

      Hugh code changes:
      - SignalType.cs: add NeedsUserInput enum value
      - SignalClassifier.cs: add regex for STATUS: needs_user_input and QUESTION_FOR_USER extraction
      - QuestionHandler.cs: add HandleNeedsUserInput method that DISPLAYS question text
        (unlike current HandleOpenQuestion which just says "pausing for input")

      ## Strategy Assessment Summary

      | Strategy | Feasibility | Reliability | Impact | Verdict |
      | 1. QUESTION_FOR_USER | HIGH | MEDIUM | Low | Include |
      | 2. Never Answer | HIGH | LOW | Medium | Supplement only |
      | 3. Pre-Flight | HIGH | HIGH (known) | Low | Include |
      | 4. ask_questions | VS Code only | MEDIUM | Low | VS Code only |
      | 5. needs_user_input | HIGH | HIGH | Low | PRIMARY |
    key_discoveries:
      - "Strategy 5 (STATUS: needs_user_input) is the MOST reliable — makes question passthrough a first-class flow control mechanism"
      - "Strategy 3 (Pre-Flight Expansion) is the MOST preventive — catches ambiguities before subagent invocation"
      - "Strategy 4 (ask_questions) is CONTEXT-DEPENDENT — works for VS Code but NOT for Hugh CLI"
      - "Strategy 2 (Never Answer) is UNRELIABLE alone — without structured markers, detection is inconsistent"
      - "Hybrid Approach (5+1+3+4) provides defense-in-depth: prevention + explicit signaling + structured data + relay"
      - "Hugh requires CODE CHANGES — SignalClassifier needs NeedsUserInput type and QuestionHandler must display question text"
      - "ask_questions tool description ENCOURAGES answering rather than asking — 'If obvious best answer, take that action'"
    relevant_files:
      - path: agents/pch-coder.md
        note: "Subagent invocation template and Processing Subagent Results need updates"
      - path: agents/pch-researcher.md
        note: "Subagent invocation template and Pre-Phase Clarification Check need updates"
      - path: Hugh/Detection/SignalClassifier.cs
        note: "Needs NeedsUserInput signal type and regex patterns"
      - path: Hugh/Detection/SignalType.cs
        note: "Needs NeedsUserInput enum value"
      - path: Hugh/Handlers/QuestionHandler.cs
        note: "Needs HandleNeedsUserInput method that displays question text"
    external_sources: null
    gaps:
      - "Cannot verify ask_questions tool behavior without live testing in VS Code"
      - "Hugh's Console I/O handling for user answers during paused execution not fully mapped"
    assumptions:
      - "Subagents will comply with new return format instructions when properly instructed"
      - "Hugh's QuestionResponse.Pause() can be extended to include question display"

overview: |
  This research investigated why subagent questions intended for end-users get intercepted
  by parent/orchestrating agents in the PCH workflow, and identified concrete strategies to
  fix it. The root cause is architectural: the runSubagent tool explicitly states that results
  are "not visible to the user" and instructs the parent to summarize/interpret. Combined with
  return formats that lack question-related fields, parent parsing instructions that focus purely
  on structured data (STATUS/FILES/NOTES), and explicit "cannot ask clarifying questions" language,
  the system is designed for autonomous execution — not interactive question passthrough.

  Agent question patterns vary significantly: pch-visionary uses 3 types (open-ended elicitation,
  multi-choice refinement, numbered validation), pch-planner enforces strict Recommendation format
  that Hugh auto-accepts, and pch-researcher uses ⏸️ Clarification Needed markers. Hugh's
  SignalClassifier only detects the **Recommendation:** regex pattern — open-ended questions
  cause a pause but their content is never relayed to users.

  The recommended solution is a Hybrid Approach with defense-in-depth: (1) expand the pre-phase
  clarification check to catch predictable ambiguities before invoking subagents, (2) add
  STATUS: needs_user_input as a first-class flow control signal, (3) add QUESTION_FOR_USER
  fields to subagent return formats, (4) instruct the parent to relay questions via ask_questions
  tool (VS Code) or console display (Hugh), and (5) reinforce with "never answer subagent
  questions yourself" instructions. This requires both prompt changes (pch-coder, pch-researcher)
  and Hugh code changes (SignalType, SignalClassifier, QuestionHandler).

key_findings:
  - "The root cause is architectural: runSubagent returns are 'not visible to the user' and parent is instructed to summarize, not forward"
  - "Subagent return formats entirely lack question-related fields — no QUESTION_FOR_USER or similar markers exist"
  - "Parent processing instructions are 100% data-parsing focused with zero question detection or passthrough logic"
  - "'Automatic mode cannot ask clarifying questions' language actively prevents parents from expecting or handling questions"
  - "Hugh's SignalClassifier only detects **Recommendation:** regex — open-ended questions cause pause but content is never relayed"
  - "Pre-phase clarification check is the ONLY working pattern because the parent generates questions directly, not via subagent relay"
  - "STATUS: needs_user_input as a first-class status value is the most reliable solution — impossible to ignore in the existing flow"
  - "ask_questions tool description ENCOURAGES answering rather than asking ('if obvious best answer, take that action')"
  - "The Hybrid Approach (prevention + signaling + structured format + relay + reinforcement) provides defense-in-depth"

actionable_conclusions:
  - "Add STATUS: needs_user_input to subagent return formats in both pch-coder.md and pch-researcher.md"
  - "Add QUESTION_FOR_USER, QUESTION_OPTIONS, RECOMMENDATION fields to subagent return format templates"
  - "Add 'Handling needs_user_input Status' section to parent processing instructions with explicit 'do NOT answer yourself' rule"
  - "Expand Pre-Phase Clarification Check with new decision point triggers (technology choices, config dependencies, multi-option scope)"
  - "Hugh code changes: add NeedsUserInput to SignalType enum, add regex detection to SignalClassifier, update QuestionHandler to display question text"
  - "Remove or qualify 'Automatic mode cannot ask clarifying questions' language — replace with conditional guidance"

open_questions:
  - "Will LLM subagents reliably populate QUESTION_FOR_USER when instructed to, or will they still try to answer autonomously?"
  - "How should Hugh's re-invocation work after receiving a user answer — full subagent restart or continuation prompt?"
  - "Should the ask_questions tool's 'obvious best answer' guidance be overridden in parent processing context?"
  - "What's the UX for displaying long multi-option questions in Hugh's console interface?"

standards: null

handoff:
  created_by: pch-researcher
  created_date: "2026-02-08"
  status: complete
  current_phase: complete
  path: /docs/research/012-subagent-question-passthrough-to-end-user.md
