# 013 — Subagent Question Passthrough to End-User

## Document Information

```yaml
id: "013"
type: plan
title: "Subagent Question Passthrough to End-User"
status: ready-for-implementation
version: "v2.2"
created: "2026-02-08"
owner: pch-planner
research_ref: "/docs/research/012-subagent-question-passthrough-to-end-user.md"

versions:
  - version: "v1.0"
    date: "2026-02-08"
    author: pch-planner
    changes: Initial plan creation with outline skeleton
  - version: "v1.1"
    date: "2026-02-08"
    author: pch-planner
    changes: All clarifying questions answered (scope, agents, placement, re-invocation, pre-flight, mode, display, testing)
  - version: "v1.2"
    date: "2026-02-08"
    author: pch-planner
    changes: Requirements, technical design, dependencies, risks, and 6-phase execution plan completed
  - version: "v2.0"
    date: "2026-02-08"
    author: pch-planner
    changes: Holistic review completed — decision interactions analyzed, architectural concerns addressed
  - version: "v2.1"
    date: "2026-02-08"
    author: pch-plan-reviewer
    changes: "Review complete — 5 issues resolved: fixed task 5.3 file targets (CopilotService.cs), added tasks 5.4/5.5 for signal routing, removed CONTEXT field, added ask_questions tool guidance, fixed test file placement"
  - version: "v2.2"
    date: "2026-02-08"
    author: pch-plan-reviewer
    changes: "Second review — 4 issues resolved: removed stale CONTEXT reference from task 1.1, fixed FR-12 ReadLine attribution, clarified VS Code vs Hugh re-invocation strategy with loop (max 3), required dual-regex match for NeedsUserInput classification to prevent false positives"
```

## Planning Session Log

```yaml
decisions:
  - session: 1
    date: "2026-02-08"
    decision_point: "Implementation Scope — how many of the 5 Hybrid Approach layers to implement?"
    answer: "A — Full Hybrid — All 5 Layers"
    rationale: "Layers are designed as defense-in-depth; each covers a different failure mode. Hugh code changes are straightforward and align with existing patterns. No reason to defer any layer."
  - session: 2
    date: "2026-02-08"
    decision_point: "Affected Agents Scope — which agents should be updated with needs_user_input return format and question fields?"
    answer: "C — All Six Agents"
    rationale: "GitHub Copilot for VS Code may invoke subagents for agents like pch-planner and pch-plan-reviewer even when not explicitly defined in agent prompts. Embedding the needs_user_input return format in every agent file ensures consistent behavior regardless of how/when agents are invoked as subagents."
  - session: 3
    date: "2026-02-08"
    decision_point: "Question Field Placement — how to structure needs_user_input instructions within each agent file?"
    answer: "A — Dedicated 'Subagent Question Passthrough' Section in Each Agent"
    rationale: "Instructions need to be immediately visible in the agent's own context since Copilot may invoke any agent as a subagent unexpectedly. A dedicated section is unambiguous and doesn't depend on reading external files. Agent-specific guidance is valuable since each agent has different question formats."
  - session: 4
    date: "2026-02-08"
    decision_point: "Hugh Re-Invocation After User Answer — how to re-invoke subagent after collecting user response?"
    answer: "A — Full Subagent Restart with Answer Injected"
    rationale: "Aligns with stateless subagent architecture. Re-work cost is minimal since subagent stopped early at the question point. Clean restart with answer injected is simplest and follows the pattern used for blocked status retries."
    clarification: "This applies to VS Code's stateless runSubagent tool (parent creates a fresh subagent). For Hugh's SDK-based sessions, the more efficient approach is session continuation — send a follow-up message to the existing session with the user's answer. Hugh uses session continuation in CopilotService.ExecutePhaseAsync (Task 5.5) since SDK sessions support multi-turn and re-initialization is expensive."
  - session: 5
    date: "2026-02-08"
    decision_point: "Pre-Flight Clarification Expansion — which agents get expanded pre-flight checks and which triggers?"
    answer: "C — pch-researcher Only, Full Trigger Set"
    rationale: "pch-coder works from reviewed plans where ambiguities should already be resolved. pch-researcher works from exploratory outlines with frequent vague scope. Full trigger set is appropriate for researcher. Layer 2 (needs_user_input) still protects pch-coder for emergent questions."
  - session: 5
    date: "2026-02-08"
    decision_point: "pch-coder Mode Selection — should pch-coder still offer Manual vs Automatic mode?"
    answer: "Remove mode selection — pch-coder always runs in Automatic Mode"
    rationale: "User-directed decision. Simplifies coder workflow by eliminating mode selection prompt. New sessions are still required between reviewer and coder (session boundaries maintained). Hugh's ModeSelection handling for coder context will need updating."
  - session: 6
    date: "2026-02-08"
    decision_point: "Hugh Question Display and Input — how to display subagent questions and collect user answers in Hugh's console?"
    answer: "A — Formatted Console Output with ReadLine"
    rationale: "Consistent with existing ConsoleOutput formatting and pause mechanisms. Key improvement is actually displaying the question text — current HandleOpenQuestion just says 'pausing for input' without showing what was asked. ReadLine is simple and sufficient for CLI."
  - session: 7
    date: "2026-02-08"
    decision_point: "Testing Strategy — how to verify changes work correctly?"
    answer: "B — Unit Tests + Prompt Validation Checklist"
    rationale: "Unit tests for Hugh code follow established patterns in Hugh.Tests/. Prompt changes are non-deterministic and better verified via structured manual checklist. Checklist also serves as documentation of expected behavior."
```

## Overview

When pch-coder and pch-researcher agents run in Automatic Mode, they delegate work to subagents via the `runSubagent` tool. If a subagent encounters ambiguity requiring user clarification, its questions are intercepted by the parent agent instead of being surfaced to the end-user. This happens because:

1. Subagent returns are explicitly "not visible to the user" — the parent summarizes them
2. Return formats lack question-related fields (`QUESTION_FOR_USER`, etc.)
3. Parent processing instructions focus entirely on structured data parsing with no question detection
4. Explicit "cannot ask clarifying questions" language conditions the parent to not look for questions

This plan implements the **Hybrid Approach** recommended by Research 012 to ensure subagent questions reliably reach end-users through defense-in-depth: prevention (pre-flight checks), explicit signaling (`needs_user_input` status), structured data (question fields), relay mechanism (`ask_questions` tool), and reinforcing instructions.

## Objectives

- Add `needs_user_input` as a first-class STATUS value in subagent return formats
- Add structured question fields (`QUESTION_FOR_USER`, `QUESTION_OPTIONS`, `RECOMMENDATION`) to return formats
- Update parent orchestrator processing instructions to detect and relay subagent questions
- Expand pre-phase clarification checks to catch more predictable ambiguities before subagent invocation
- Update Hugh's detection and handling code to support the new signal type
- Ensure backward compatibility with existing `success | partial | blocked` flows

## Requirements

### Functional Requirements

1. **FR-1: `needs_user_input` Status Value** — Subagent return formats in all six agent files must support `needs_user_input` as a valid STATUS value alongside `success`, `partial`, and `blocked`
2. **FR-2: Structured Question Fields** — Subagent return formats must include `QUESTION_FOR_USER`, `QUESTION_TYPE`, `QUESTION_OPTIONS`, and `RECOMMENDATION` fields for conveying question details
3. **FR-3: Parent Question Detection** — pch-coder and pch-researcher orchestrator processing instructions must detect `needs_user_input` status and extract question fields from subagent returns
4. **FR-4: Parent Question Relay (VS Code)** — Parent orchestrators must display the subagent's question to the user (using `ask_questions` tool when appropriate) and wait for the user's response
5. **FR-5: Subagent Re-Invocation** — After receiving user's answer, the parent must re-invoke the subagent for the same phase with the user's answer injected into the prompt
6. **FR-6: "Never Answer Yourself" Rule** — Parent orchestrator instructions must explicitly forbid answering subagent questions autonomously
7. **FR-7: Expanded Pre-Flight Checks (Researcher)** — pch-researcher's Pre-Phase Clarification Check must include new decision point triggers: technology choices, configuration dependencies, multi-option scope, user preference implicit, external dependencies
8. **FR-8: Agent-Level Subagent Section** — All six agent files must contain a dedicated "When Running as a Subagent" section with `needs_user_input` return format instructions
9. **FR-9: pch-coder Always Automatic** — pch-coder must always run in Automatic Mode; remove mode selection prompt and default to automatic execution. Session boundaries between reviewer and coder must be maintained
10. **FR-10: Hugh NeedsUserInput Signal** — Hugh's `SignalType` enum must include a `NeedsUserInput` value
11. **FR-11: Hugh Signal Detection** — Hugh's `SignalClassifier` must detect `STATUS: needs_user_input` and extract `QUESTION_FOR_USER` content from agent output
12. **FR-12: Hugh Question Display** — Hugh's `QuestionHandler` must display the full question text and options to the user via `ConsoleOutput` formatting. The orchestration layer (`CopilotService.ExecutePhaseAsync`) collects the answer via `Console.ReadLine()` and handles re-invocation
13. **FR-13: Hugh Re-Invocation** — Hugh must support re-invoking a subagent for the same phase with the user's answer injected after `NeedsUserInput` handling

### Non-Functional Requirements

1. **NFR-1: Backward Compatibility** — Existing `success | partial | blocked` flows must continue working unchanged
2. **NFR-2: No Context Overhead** — The "When Running as a Subagent" section in each agent file should be ≤50 lines to minimize context window consumption
3. **NFR-3: Consistent Formatting** — Question display in Hugh must use existing `ConsoleOutput` formatting patterns (colored headers, indented content)
4. **NFR-4: Test Coverage** — All Hugh code changes must have unit tests following patterns in `Hugh.Tests/`

### Out of Scope

1. Interactive menu selection for Hugh console (future enhancement)
2. Timeout/auto-select for unanswered questions in Hugh (future enhancement)
3. `ask_questions` tool override of "obvious best answer" guidance (VS Code system-level)
4. Subagent-to-subagent question forwarding (multi-level nesting)
5. Automatic Mode for pch-researcher changes (only pch-coder mode selection is being changed)

## Technical Design

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: Subagent Return Format
    location: "agents/pch-coder.md (line ~290), agents/pch-researcher.md (line ~382)"
    usage: Add needs_user_input status and question fields to existing format
  - pattern: Parent Processing Instructions
    location: "agents/pch-coder.md (line ~284), agents/pch-researcher.md (line ~382)"
    usage: Add question detection and relay section
  - pattern: SignalType Enum
    location: "Hugh/Detection/SignalType.cs"
    usage: Add NeedsUserInput value following existing enum pattern
  - pattern: SignalClassifier Regex
    location: "Hugh/Detection/SignalClassifier.cs"
    usage: Add regex patterns following existing GeneratedRegex pattern
  - pattern: QuestionHandler
    location: "Hugh/Handlers/QuestionHandler.cs"
    usage: Add HandleNeedsUserInput method following existing handler pattern
  - pattern: Pre-Phase Clarification Check
    location: "agents/pch-researcher.md (line ~280)"
    usage: Expand ambiguity triggers table
```

### Architecture Overview

The changes span two domains: **agent prompt files** (Markdown) and **Hugh application code** (C#).

#### Agent Prompt Changes

```
┌─────────────────────────────────────────────────────────┐
│  All 6 Agent Files                                      │
│  + "When Running as a Subagent" section                 │
│    - Detect when user-directed question arises          │
│    - Return STATUS: needs_user_input                    │
│    - Include QUESTION_FOR_USER, QUESTION_OPTIONS, etc.  │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│  pch-coder.md + pch-researcher.md (Orchestrators)       │
│  + Subagent invocation template updates                 │
│    - Add needs_user_input to STATUS values              │
│    - Add question fields to return format               │
│  + Processing instructions updates                      │
│    - Detect needs_user_input status                     │
│    - Display question to user                           │
│    - Wait for answer                                    │
│    - Re-invoke subagent with answer                     │
│    - NEVER answer subagent questions yourself            │
│  + pch-coder: Remove mode selection, always automatic   │
└─────────────────────────────────────────────────────────┘
                         ▼
┌─────────────────────────────────────────────────────────┐
│  pch-researcher.md Only                                 │
│  + Expanded Pre-Phase Clarification Check               │
│    - Technology choices trigger                          │
│    - Configuration dependencies trigger                  │
│    - Multi-option scope trigger                          │
│    - User preference implicit trigger                    │
│    - External dependencies trigger                       │
└─────────────────────────────────────────────────────────┘
```

#### Hugh Code Changes

```
┌──────────────────────┐    ┌────────────────────────────┐
│ SignalType.cs         │    │ SignalClassifier.cs         │
│ + NeedsUserInput      │───>│ + NeedsUserInput regex     │
│   enum value          │    │ + QUESTION_FOR_USER extract │
└──────────────────────┘    └────────────────────────────┘
                                        │
                                        ▼
┌──────────────────────┐    ┌────────────────────────────┐
│ QuestionResponse.cs   │    │ QuestionHandler.cs          │
│ + PauseWithQuestion() │───>│ + HandleNeedsUserInput()   │
│   factory method      │    │   - Display question text   │
└──────────────────────┘    │   - Show options if present  │
                            │   - ReadLine for answer      │
                            │   - Return answer for relay  │
                            └────────────────────────────┘
                                        │
                                        ▼
                            ┌────────────────────────────┐
                            │ Orchestration layer         │
                            │ + Re-invoke subagent with   │
                            │   user answer injected      │
                            └────────────────────────────┘
```

### Prompt Changes

#### 1. Subagent Return Format (All 6 Agents — "When Running as a Subagent" Section)

Each agent file gets a new section near the end:

```markdown
## When Running as a Subagent

If you are invoked as a subagent (via `runSubagent`) and you encounter a situation
where you need user input to proceed (e.g., a clarifying question, a decision point,
ambiguous requirements), you MUST use the `needs_user_input` status to surface the 
question to the end-user.

**Do NOT attempt to answer user-directed questions yourself.** Return them for the
user to answer.

**Return Format When User Input is Needed:**

\```
STATUS: needs_user_input
QUESTION_FOR_USER: [The full question text, formatted clearly]
QUESTION_TYPE: elicitation | refinement | validation
QUESTION_OPTIONS: [Lettered options A/B/C/D if applicable, or "open-ended"]
RECOMMENDATION: [Your recommended option letter and name, or "none"]
\```

**QUESTION_TYPE values:**
- `elicitation`: Open-ended question seeking new information
- `refinement`: Multiple-choice question with defined options
- `validation`: Yes/no or confirm/deny question

**When to use `needs_user_input`:**
- You need a decision that affects the direction of your work
- Requirements are ambiguous and assumptions would be risky
- Multiple valid approaches exist and user preference matters
- You encounter something unexpected that needs user guidance

**When NOT to use (handle autonomously):**
- Minor implementation details with clear best practices
- Formatting or style choices covered by existing patterns
- Issues you can resolve by reading more context from the codebase
```

#### 2. Orchestrator Invocation Template Updates (pch-coder.md, pch-researcher.md)

Add `needs_user_input` to the STATUS values in the subagent invocation template:

**pch-coder invocation template change:**
```
STATUS: success | partial | blocked | needs_user_input
```

Add question fields to the return format:
```
QUESTION_FOR_USER: [question text if STATUS is needs_user_input, or "none"]
QUESTION_TYPE: [elicitation | refinement | validation, or "none"]
QUESTION_OPTIONS: [lettered options or "none"]
RECOMMENDATION: [recommended option or "none"]
```

**pch-researcher invocation template — same changes** to its return format block.

#### 3. Orchestrator Processing Instructions (pch-coder.md, pch-researcher.md)

Add a new section after the existing "Status Interpretation" block:

```markdown
### Handling `needs_user_input` Status

When a subagent returns `STATUS: needs_user_input`:

1. **Extract the question**: Read QUESTION_FOR_USER, QUESTION_TYPE, QUESTION_OPTIONS, and RECOMMENDATION
2. **Display to the user**: Present the question based on QUESTION_TYPE:
   - **`refinement`** (multiple-choice): Use the `ask_questions` tool with QUESTION_OPTIONS mapped to options and RECOMMENDATION marked as recommended
   - **`validation`** (yes/no): Use the `ask_questions` tool with confirm/deny options
   - **`elicitation`** (open-ended): Display the question as plain text and wait for user reply
3. **Wait for user response**: Do not proceed until the user answers
4. **Re-invoke the subagent**: Start a fresh subagent invocation for the SAME phase,
   adding the user's answer to the prompt:
   
   "The user was asked: [QUESTION_FOR_USER]
   The user answered: [user's response]
   
   Continue Phase {N} with this context."

5. **Process the re-invocation result**: Parse the return normally (success/partial/blocked)

🚨 **CRITICAL: Do NOT answer the question yourself.** Even if you think you know the
answer, the question was generated because the subagent determined user input is required.
Always surface the question to the user.
```

#### 4. pch-coder Mode Selection Removal

Remove the "Execution Mode Selection" section, "Mode Selection Prompt", "Mode Selection Logic", and "Choosing Between Modes" table from `pch-coder.md`. Replace with:

```markdown
## Execution Mode

pch-coder always runs in **Automatic Mode** using subagent orchestration. Each phase 
executes in an isolated subagent with fresh context.

**Prerequisites:**
- VS Code setting: `chat.customAgentInSubagent.enabled: true`
- Plan document must be reviewed and approved by `pch-plan-reviewer`

**Session Boundaries:** Always start a new agent session for pch-coder — do not continue
from a reviewer session. The fresh session ensures full context capacity for implementation.
```

#### 5. Pre-Phase Clarification Check Expansion (pch-researcher.md only)

Add new rows to the existing "Ambiguity Triggers" table:

| Trigger Type | Detection Pattern | Example |
|---|---|---|
| **Technology choices** | Phase involves selecting between technologies/frameworks | "Research authentication approach" with no selected method |
| **Configuration dependencies** | Phase requires config values not yet defined | "Set up database connection" with no connection string specified |
| **Multi-option scope** | Phase lists alternatives without a selected preference | "Evaluate Redis vs Memcached vs in-memory caching" |
| **User preference implicit** | Phase assumes a user preference not explicitly stated | "Design the UI layout" with no design guidance |
| **External dependencies** | Phase depends on external services/APIs not yet confirmed | "Integrate with payment provider" with no provider selected |

### Hugh Code Changes

#### 1. `SignalType.cs` — Add `NeedsUserInput` Enum Value

```csharp
/// <summary>
/// A subagent requires user input to proceed.
/// Contains a question that must be displayed to the user.
/// Orchestrator should pause, display the question, collect the answer,
/// and re-invoke the subagent with the user's response.
/// </summary>
NeedsUserInput
```

Add as the last value in the enum, after `ModeSelection`.

#### 2. `SignalClassifier.cs` — Add Detection Patterns

Add two new `GeneratedRegex` patterns:

```csharp
// Needs user input: STATUS: needs_user_input
[GeneratedRegex(@"STATUS:\s*needs_user_input", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
private static partial Regex NeedsUserInputRegex();

// Question for user extraction: QUESTION_FOR_USER: [text]
[GeneratedRegex(@"QUESTION_FOR_USER:\s*(.+?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline)]
private static partial Regex QuestionForUserRegex();
```

In the `Classify` method, add detection **after blocker check, before mode selection check** (high priority since it requires user action):

```csharp
// Check for needs_user_input (requires user action)
var needsInputMatch = NeedsUserInputRegex().Match(agentOutput);
if (needsInputMatch.Success)
{
    // Extract the question text if available
    var questionMatch = QuestionForUserRegex().Match(agentOutput);
    
    // Only classify as NeedsUserInput if QUESTION_FOR_USER is also present.
    // Prevents false positives from plan document text embedded in subagent output.
    if (questionMatch.Success)
    {
        var questionText = questionMatch.Groups[1].Value.Trim();
        
        // Also try to extract recommendation
        var recMatch = RecommendationRegex().Match(agentOutput);
        
        return new SignalClassification
        {
            Type = SignalType.NeedsUserInput,
            RawMatch = needsInputMatch.Value,
            QuestionText = questionText,
            RecommendationLetter = recMatch.Success ? recMatch.Groups[1].Value : null,
            RecommendationOption = recMatch.Success ? recMatch.Groups[2].Value.Trim() : null
        };
    }
    // If STATUS: needs_user_input found without QUESTION_FOR_USER,
    // fall through to lower-priority checks (likely plan document text)
}
```

#### 3. `SignalClassification` Record — Add `QuestionText` Property

```csharp
/// <summary>
/// For NeedsUserInput: the full question text to display to the user.
/// </summary>
public string? QuestionText { get; init; }
```

#### 4. `QuestionResponse.cs` — Add `PauseWithQuestion` Factory Method

```csharp
/// <summary>
/// The question text to display to the user when pausing.
/// Null if no question context is available.
/// </summary>
public string? QuestionText { get; init; }

/// <summary>
/// Creates a response indicating human input is required, with question context.
/// </summary>
public static QuestionResponse PauseWithQuestion(string questionText) =>
    new() { ShouldPause = true, QuestionText = questionText };
```

#### 5. `QuestionHandler.cs` — Add `HandleNeedsUserInput` Method

```csharp
/// <summary>
/// Handles a needs_user_input signal by displaying the question and collecting user input.
/// </summary>
private QuestionResponse HandleNeedsUserInput(SignalClassification classification)
{
    _logger.Information(
        "Subagent needs user input. Question: {Question}",
        classification.QuestionText ?? "No question text provided");

    // Display the question to the user via console
    ConsoleOutput.WriteSection("❓ Subagent Question — User Input Required");
    
    if (!string.IsNullOrWhiteSpace(classification.QuestionText))
    {
        ConsoleOutput.WriteInfo(classification.QuestionText);
    }
    
    if (classification.RecommendationLetter != null)
    {
        ConsoleOutput.WriteInfo(
            $"Recommendation: {classification.RecommendationLetter} — {classification.RecommendationOption}");
    }
    
    Console.WriteLine();
    ConsoleOutput.WriteWarning("Please provide your answer:");

    return QuestionResponse.PauseWithQuestion(
        classification.QuestionText ?? "Subagent requires user input");
}
```

Add to the `HandleQuestionAsync` switch expression:

```csharp
SignalType.NeedsUserInput => HandleNeedsUserInput(classification),
```

#### 6. Orchestration Layer — Re-Invocation Support

The orchestration layer (in `Hugh/Orchestration/`) must handle the `NeedsUserInput` signal by:
1. Displaying the question (done by `QuestionHandler`)
2. Reading user input from console (`Console.ReadLine()`)
3. Re-invoking the same phase with the user's answer injected into the prompt

This follows the existing pattern where `HandleOpenQuestion` returns `Pause()` and the orchestration layer reads from `Console.ReadLine()` — the difference is that `PauseWithQuestion` includes the question text for display.

## Dependencies and Prerequisites

### Prerequisites

1. **Research 012 Complete** — [docs/research/012-subagent-question-passthrough-to-end-user.md](../research/012-subagent-question-passthrough-to-end-user.md) must be complete (status: complete ✅)
2. **Existing Test Suite Passes** — Run `dotnet test` in `Hugh.Tests/` to verify baseline before making changes
3. **Agent files accessible** — All six agent files under `agents/` available for modification

### Dependencies Between Changes

| Change | Depends On |
|--------|-----------|
| Orchestrator processing instructions (pch-coder, pch-researcher) | Subagent return format fields defined |
| Hugh `SignalClassifier` update | `SignalType.NeedsUserInput` enum value added |
| Hugh `QuestionHandler` update | `SignalClassification.QuestionText` property added, `QuestionResponse.PauseWithQuestion()` method added |
| Unit tests | All Hugh code changes complete |
| Prompt validation checklist | All agent prompt changes complete |

## Risk Assessment and Mitigation

| Risk | Severity | Likelihood | Mitigation |
|------|----------|------------|------------|
| Subagents ignore `needs_user_input` instructions and answer questions autonomously | High | Medium | Defense-in-depth: "Never Answer Yourself" rule in parent + structured return format make it harder to bypass. Pre-flight checks catch predictable cases before subagent invocation |
| Parent agent still answers questions despite "never answer yourself" instruction | High | Low | The `needs_user_input` STATUS is a structured field that triggers flow control, not prose the parent can reinterpret. Combined with explicit `ask_questions` tool usage instructions |
| `QuestionResponse.PauseWithQuestion` breaks existing `Pause()` consumers | Medium | Low | `PauseWithQuestion` is additive — new factory method, doesn't modify existing `Pause()`. `QuestionText` property defaults to null for backward compatibility |
| Expanded pre-flight triggers cause excessive false-positive pauses | Medium | Medium | New triggers are specific and pattern-based (not subjective). Monitor and tune thresholds post-deployment |
| Removing pch-coder mode selection breaks users expecting manual mode | Medium | Low | Session boundary requirement is maintained. Users who need manual control can use the researcher workflow or invoke pch-coder with explicit phase instructions |
| LLMs inconsistently populate the new return format fields | Medium | Medium | Structured format with clear field names and examples. `QUESTION_TYPE` and `QUESTION_OPTIONS` have defined values. Fallback: if `QUESTION_FOR_USER` is missing but `needs_user_input` is detected, display raw match as question |

## Referenced Standards

```yaml
referenced_standards: "No organizational standards applicable to this plan."
```

## Progress Summary

```yaml
progress_summary:
  - phase: 1
    description: Agent Subagent Sections — All 6 Agents
    status: complete
    tasks: 6
  - phase: 2
    description: Orchestrator Invocation & Processing Updates
    status: complete
    tasks: 5
  - phase: 3
    description: Pre-Flight Clarification Expansion (pch-researcher)
    status: complete
    tasks: 2
  - phase: 4
    description: Hugh Code Changes — SignalType, SignalClassifier, Models
    status: complete
    tasks: 5
  - phase: 5
    description: Hugh Code Changes — QuestionHandler, CopilotService & Signal Routing
    status: complete
    tasks: 5
  - phase: 6
    description: Unit Tests & Prompt Validation Checklist
    status: complete
    tasks: 6
```

## Execution Plan

```yaml
phases:
  - phase: 1
    name: "Agent Subagent Sections — All 6 Agents"
    status: complete
    size: medium
    files_to_modify: 6
    prerequisites:
      - "None — first phase"
    entry_point: "agents/pch-coder.md"
    verification: "Each agent file contains a 'When Running as a Subagent' section with needs_user_input return format"
    tasks:
      - step: "1.1"
        task: "Add 'When Running as a Subagent' section to pch-coder.md"
        status: complete
        notes: "Added at line 910, 35 lines. All 5 return format fields present."
        files:
          - "agents/pch-coder.md"
        details: |
          Add immediately before the final section of the file. Include:
          - needs_user_input STATUS explanation
          - Return format template with STATUS, QUESTION_FOR_USER, QUESTION_TYPE, QUESTION_OPTIONS, RECOMMENDATION
          - QUESTION_TYPE value definitions (elicitation, refinement, validation)
          - When to use vs. when not to use guidance
          - "Do NOT attempt to answer user-directed questions yourself" rule
          Use the template from Technical Design > Prompt Changes > Section 1.
        acceptance_criteria:
          - "Section header '## When Running as a Subagent' exists"
          - "Return format block includes all 5 fields: STATUS, QUESTION_FOR_USER, QUESTION_TYPE, QUESTION_OPTIONS, RECOMMENDATION"
          - "Contains explicit 'Do NOT attempt to answer' instruction"
          - "Section is ≤50 lines"

      - step: "1.2"
        task: "Add 'When Running as a Subagent' section to pch-researcher.md"
        status: complete
        notes: "Added at line 1451, 34 lines. All required fields and guidance present."
        files:
          - "agents/pch-researcher.md"
        details: |
          Same template as 1.1, adapted for researcher context.
          Add near end of file before any handoff metadata.
        acceptance_criteria:
          - "Section follows same structure as pch-coder version"
          - "Contains all required fields and guidance"

      - step: "1.3"
        task: "Add 'When Running as a Subagent' section to pch-planner.md"
        status: complete
        notes: "Added at line 394, 37 lines. References planner's Recommendation format."
        files:
          - "agents/pch-planner.md"
        details: |
          Same template. Note pch-planner uses specific structured question format
          with **Recommendation:** [Letter] — [Option]. The subagent section should
          reference that the planner's existing question format should be included
          in the QUESTION_FOR_USER field when running as a subagent.
        acceptance_criteria:
          - "Section follows same structure as pch-coder version"
          - "References planner's existing question format for QUESTION_FOR_USER content"

      - step: "1.4"
        task: "Add 'When Running as a Subagent' section to pch-plan-reviewer.md"
        status: complete
        notes: "Added at line 430, 37 lines. References reviewer's Review Question format."
        files:
          - "agents/pch-plan-reviewer.md"
        details: |
          Same template. Note reviewer uses "Review Question [N of M]:" format
          with table-formatted options. The subagent section should reference
          that the reviewer's existing question format feeds into QUESTION_FOR_USER.
        acceptance_criteria:
          - "Section follows same structure as pch-coder version"
          - "References reviewer's existing question format"

      - step: "1.5"
        task: "Add 'When Running as a Subagent' section to pch-visionary.md"
        status: complete
        notes: "Added at line 1538, 34 lines. Maps Type A/B/C to elicitation/refinement/validation."
        files:
          - "agents/pch-visionary.md"
        details: |
          Same template. Note visionary has 3 question types (elicitation,
          refinement, validation). Map these to QUESTION_TYPE values.
          Elicitation questions → QUESTION_TYPE: elicitation, QUESTION_OPTIONS: "open-ended"
          Refinement questions → QUESTION_TYPE: refinement, QUESTION_OPTIONS: [A/B/C/D/E]
          Validation questions → QUESTION_TYPE: validation, QUESTION_OPTIONS: [1/2/3]
        acceptance_criteria:
          - "Section follows same structure as pch-coder version"
          - "Maps visionary's 3 question types to QUESTION_TYPE values"

      - step: "1.6"
        task: "Add 'When Running as a Subagent' section to pch-helper.md"
        status: complete
        notes: "Added at line 326, 34 lines. Present for consistency."
        files:
          - "agents/pch-helper.md"
        details: |
          Same template. pch-helper primarily guides users and is unlikely to
          generate questions as a subagent, but include the section for
          consistency since Copilot may invoke it as a subagent.
        acceptance_criteria:
          - "Section follows same structure as pch-coder version"
          - "Section is present even though pch-helper rarely needs it"

  - phase: 2
    name: "Orchestrator Invocation & Processing Updates"
    status: complete
    size: medium
    files_to_modify: 2
    prerequisites:
      - "Phase 1 complete — subagent sections exist in all agents"
    entry_point: "agents/pch-coder.md"
    verification: "Phase 1 verified — all 6 agents have 'When Running as a Subagent' sections"
    tasks:
      - step: "2.1"
        task: "Update pch-coder subagent invocation template — add needs_user_input to STATUS and add question fields"
        status: complete
        notes: "Added needs_user_input to STATUS line and 4 question fields to return format"
        files:
          - "agents/pch-coder.md"
        details: |
          Locate the "Subagent Invocation" section (~line 230). Find the Return Format block.
          Change: STATUS: success | partial | blocked
          To:     STATUS: success | partial | blocked | needs_user_input
          
          Add after the VERIFICATION field:
          QUESTION_FOR_USER: [question text if STATUS is needs_user_input, or "none"]
          QUESTION_TYPE: [elicitation | refinement | validation, or "none"]
          QUESTION_OPTIONS: [lettered options or "none"]
          RECOMMENDATION: [recommended option or "none"]
        acceptance_criteria:
          - "Invocation template STATUS line includes needs_user_input"
          - "Four question fields added to return format block"
          - "Field descriptions are clear and include 'none' default"

      - step: "2.2"
        task: "Update pch-researcher subagent invocation template — same changes as 2.1"
        status: complete
        notes: "Same changes as 2.1 applied to pch-researcher invocation template"
        files:
          - "agents/pch-researcher.md"
        details: |
          Locate the "Subagent Invocation" section (~line 340). Find the Return Format block.
          Apply identical STATUS and question field changes as step 2.1.
        acceptance_criteria:
          - "Invocation template STATUS line includes needs_user_input"
          - "Four question fields added to return format block"

      - step: "2.3"
        task: "Add 'Handling needs_user_input Status' section to pch-coder Processing Subagent Results"
        status: complete
        notes: "Added needs_user_input to Status Interpretation, Parsing Each Field table, and new Handling subsection with 5-step workflow"
        files:
          - "agents/pch-coder.md"
        details: |
          Locate "Processing Subagent Results" section (~line 284). Find the "Status Interpretation" block.
          Add a new subsection "### Handling `needs_user_input` Status" after Status Interpretation.
          Include the 5-step workflow from Technical Design > Section 3:
          1. Extract the question
          2. Display to the user
          3. Wait for user response
          4. Re-invoke the subagent (same phase, answer injected)
          5. Process the re-invocation result
          Include the 🚨 CRITICAL "Do NOT answer the question yourself" warning.
          
          Also add needs_user_input to the "Parsing Each Field" table and "Status Interpretation" list:
          - `needs_user_input`: Subagent requires user input. Surface the question and wait.
        acceptance_criteria:
          - "'Handling needs_user_input Status' subsection exists"
          - "5-step workflow is documented with re-invocation prompt template"
          - "Contains 'Do NOT answer the question yourself' rule with 🚨 emphasis"
          - "needs_user_input added to Status Interpretation list"
          - "QUESTION_FOR_USER, QUESTION_TYPE, QUESTION_OPTIONS, RECOMMENDATION added to Parsing Each Field table"

      - step: "2.4"
        task: "Add 'Handling needs_user_input Status' section to pch-researcher Processing Subagent Results"
        status: complete
        notes: "Same handling section added to pch-researcher, adapted for research context"
        files:
          - "agents/pch-researcher.md"
        details: |
          Locate "Processing Subagent Results" section (~line 382). Apply identical changes as step 2.3.
          Adapt re-invocation prompt template for researcher context (research phases instead of implementation phases).
        acceptance_criteria:
          - "Same structure as pch-coder version, adapted for research context"
          - "Re-invocation prompt references research document path and phase"

      - step: "2.5"
        task: "Remove mode selection from pch-coder — always use Automatic Mode"
        status: complete
        notes: "Removed mode selection sections, added Execution Mode section, updated session init and git check references"
        files:
          - "agents/pch-coder.md"
        details: |
          Remove or replace these sections:
          - "Execution Mode Selection" section header and content
          - "Mode Selection Prompt" block
          - "Mode Selection Logic" subsection
          - "Choosing Between Modes" table
          - "Prerequisites for Automatic Mode" section (keep prerequisites content, move to new section)
          
          Replace with a concise "## Execution Mode" section per Technical Design > Section 4:
          - State that pch-coder always runs in Automatic Mode
          - List prerequisites (VS Code setting, reviewed plan)
          - Emphasize session boundaries: always start a new session for pch-coder
          
          Also update "Session Initialization" section to remove mode selection references.
          Remove "Mode Persistence" paragraph.
          Update any references to "If user chooses manual/automatic" throughout the file.
        acceptance_criteria:
          - "No 'Mode Selection Prompt' block exists in the file"
          - "No 'Choosing Between Modes' table exists"
          - "'## Execution Mode' section states always automatic"
          - "Session boundary requirement is documented"
          - "Prerequisites (VS Code setting, reviewed plan) are preserved"
          - "No references to 'manual mode' remain as a user choice (except in historical context)"
          
          Note for implementer: Hugh's ModeSelection handler (HandleModeSelectionAsync in
          QuestionHandler.cs) does NOT need updating. The ModeSelectionRegex matches
          '🔄 **Execution Mode Selection**' which pch-coder will no longer emit.
          The regex simply won't match for coder output — no code change required.
          Hugh's ModeSelection handler remains needed for pch-researcher which retains mode selection.

  - phase: 3
    name: "Pre-Flight Clarification Expansion (pch-researcher)"
    status: complete
    size: small
    files_to_modify: 1
    prerequisites:
      - "Phase 2 complete — orchestrator processing instructions updated"
    entry_point: "agents/pch-researcher.md"
    verification: "Phase 2 verified — pch-coder and pch-researcher have needs_user_input processing sections"
    tasks:
      - step: "3.1"
        task: "Add new decision point triggers to Pre-Phase Clarification Check"
        status: complete
        notes: "Added 5 new rows to Ambiguity Triggers table (10 total)"
        files:
          - "agents/pch-researcher.md"
        details: |
          Locate "Pre-Phase Clarification Check" section (~line 280). Find the "Ambiguity Triggers" table.
          Add 5 new rows to the table after the existing 5 triggers:
          
          | Technology choices | Phase involves selecting between technologies/frameworks | "Research authentication approach" with no selected method |
          | Configuration dependencies | Phase requires config values not yet defined | "Set up database connection" with no connection string specified |
          | Multi-option scope | Phase lists alternatives without a selected preference | "Evaluate Redis vs Memcached vs in-memory caching" |
          | User preference implicit | Phase assumes a user preference not explicitly stated | "Design the UI layout" with no design guidance |
          | External dependencies | Phase depends on external services/APIs not yet confirmed | "Integrate with payment provider" with no provider selected |
          
          Use bold formatting consistent with existing trigger names.
        acceptance_criteria:
          - "Ambiguity Triggers table has 10 rows total (5 existing + 5 new)"
          - "Each new trigger has a detection pattern and example"
          - "Formatting matches existing table rows"

      - step: "3.2"
        task: "Add clarification pause examples for new triggers"
        status: complete
        notes: "Added 3 new clarification pause examples for technology choices, configuration dependencies, and external dependencies"
        files:
          - "agents/pch-researcher.md"
        details: |
          Locate "Clarification Pause Examples" list (~after the Ambiguity Triggers table).
          Add examples for the new trigger types:
          
          - **Technology choices:** "Phase 2 mentions 'authentication approach' but no specific method is selected. Which authentication mechanism should I focus on? (OAuth 2.0, JWT, API Keys, SAML, or compare all?)"
          - **Configuration dependencies:** "Phase 4 requires a database connection but no connection string or database type is specified. Which database are you targeting? (SQL Server, PostgreSQL, SQLite, CosmosDB?)"
          - **External dependencies:** "Phase 3 says 'integrate with payment provider' but no provider is selected. Which payment provider should I research? (Stripe, PayPal, Square, Adyen?)"
        acceptance_criteria:
          - "At least 3 new clarification pause examples added"
          - "Examples follow existing format with bold trigger name and quoted question"

  - phase: 4
    name: "Hugh Code Changes — SignalType, SignalClassifier, Models"
    status: complete
    size: small
    files_to_modify: 3
    prerequisites:
      - "Phase 1 complete — subagent return format defined (informs regex patterns)"
    entry_point: "Hugh/Detection/SignalType.cs"
    verification: "Phase 3 verified — pch-researcher pre-flight checks expanded. Also verify Phase 1 return format aligns with regex patterns."
    tasks:
      - step: "4.1"
        task: "Add NeedsUserInput to SignalType enum"
        status: complete
        notes: "Added NeedsUserInput with XML doc comment after ModeSelection"
        files:
          - "Hugh/Detection/SignalType.cs"
        details: |
          Add new enum value after ModeSelection (last current value):
          
          /// <summary>
          /// A subagent requires user input to proceed.
          /// Contains a question that must be displayed to the user.
          /// Orchestrator should pause, display the question, collect the answer,
          /// and re-invoke the subagent with the user's response.
          /// </summary>
          NeedsUserInput
        acceptance_criteria:
          - "NeedsUserInput is a valid SignalType enum value"
          - "XML doc comment explains the purpose and expected orchestrator behavior"
          - "Compiles without errors"

      - step: "4.2"
        task: "Add QuestionText property to SignalClassification record"
        status: complete
        notes: "Added nullable string QuestionText property after RawMatch"
        files:
          - "Hugh/Detection/SignalClassifier.cs"
        details: |
          Add to the SignalClassification record after the RawMatch property:
          
          /// <summary>
          /// For NeedsUserInput: the full question text to display to the user.
          /// </summary>
          public string? QuestionText { get; init; }
        acceptance_criteria:
          - "QuestionText property exists on SignalClassification"
          - "Property is nullable string with init setter"
          - "Compiles without errors"

      - step: "4.3"
        task: "Add NeedsUserInput and QuestionForUser regex patterns to SignalClassifier"
        status: complete
        notes: "Added NeedsUserInputRegex and QuestionForUserRegex GeneratedRegex patterns"
        files:
          - "Hugh/Detection/SignalClassifier.cs"
        details: |
          Add two new GeneratedRegex patterns after the existing ModeSelectionRegex:
          
          // Needs user input: STATUS: needs_user_input
          [GeneratedRegex(@"STATUS:\s*needs_user_input", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
          private static partial Regex NeedsUserInputRegex();
          
          // Question for user extraction: QUESTION_FOR_USER: [text]
          [GeneratedRegex(@"QUESTION_FOR_USER:\s*(.+?)(?:\n|$)", RegexOptions.Compiled | RegexOptions.Singleline)]
          private static partial Regex QuestionForUserRegex();
          
          Follow the existing GeneratedRegex pattern used by other regexes in the class.
        acceptance_criteria:
          - "NeedsUserInputRegex() partial method exists with correct pattern"
          - "QuestionForUserRegex() partial method exists with capture group for question text"
          - "Both use [GeneratedRegex] attribute with RegexOptions.Compiled"

      - step: "4.4"
        task: "Add NeedsUserInput detection to Classify method"
        status: complete
        notes: "Added dual-regex check after blocker, before mode selection. Falls through if only STATUS matches."
        files:
          - "Hugh/Detection/SignalClassifier.cs"
        details: |
          In the Classify method, add NeedsUserInput detection AFTER blocker check
          and BEFORE mode selection check (priority: needs user action).
          
          IMPORTANT: Require BOTH NeedsUserInputRegex AND QuestionForUserRegex to match
          before classifying as NeedsUserInput. This prevents false positives when plan
          document content (which contains STATUS: needs_user_input as examples) is
          embedded in subagent output. Plan text has STATUS examples but not paired
          QUESTION_FOR_USER lines, so requiring both eliminates false matches.
          
          var needsInputMatch = NeedsUserInputRegex().Match(agentOutput);
          if (needsInputMatch.Success)
          {
              var questionMatch = QuestionForUserRegex().Match(agentOutput);
              
              // Only classify as NeedsUserInput if QUESTION_FOR_USER is also present.
              // This prevents false positives from plan document text embedded in output.
              if (questionMatch.Success)
              {
                  var questionText = questionMatch.Groups[1].Value.Trim();
                  var recMatch = RecommendationRegex().Match(agentOutput);
                  
                  return new SignalClassification
                  {
                      Type = SignalType.NeedsUserInput,
                      RawMatch = needsInputMatch.Value,
                      QuestionText = questionText,
                      RecommendationLetter = recMatch.Success ? recMatch.Groups[1].Value : null,
                      RecommendationOption = recMatch.Success ? recMatch.Groups[2].Value.Trim() : null
                  };
              }
              // If STATUS: needs_user_input found but no QUESTION_FOR_USER,
              // fall through to lower-priority signal checks (likely plan text, not a real signal)
          }
          
          Insert between the blocker check block and the mode selection check block.
        acceptance_criteria:
          - "NeedsUserInput detected before ModeSelection in priority order"
          - "Classification requires BOTH NeedsUserInputRegex AND QuestionForUserRegex to match"
          - "If only STATUS: needs_user_input matches without QUESTION_FOR_USER, falls through to lower-priority checks"
          - "QuestionText is extracted from QUESTION_FOR_USER field"
          - "RecommendationLetter and RecommendationOption are extracted if present"

      - step: "4.5"
        task: "Add QuestionText and PauseWithQuestion to QuestionResponse model"
        status: complete
        notes: "Added QuestionText property and PauseWithQuestion factory method. Existing Pause() unchanged."
        files:
          - "Hugh/Models/QuestionResponse.cs"
        details: |
          Add to QuestionResponse record:
          
          1. New property:
          /// <summary>
          /// The question text to display to the user when pausing.
          /// Null if no question context is available.
          /// </summary>
          public string? QuestionText { get; init; }
          
          2. New factory method:
          /// <summary>
          /// Creates a response indicating human input is required, with question context.
          /// </summary>
          public static QuestionResponse PauseWithQuestion(string questionText) =>
              new() { ShouldPause = true, QuestionText = questionText };
          
          Existing Pause() method remains unchanged for backward compatibility.
        acceptance_criteria:
          - "QuestionText property exists with null default"
          - "PauseWithQuestion(string) factory method exists"
          - "Existing Pause() method unchanged"
          - "Compiles without errors"

  - phase: 5
    name: "Hugh Code Changes — QuestionHandler, CopilotService & Signal Routing"
    status: complete
    size: medium
    files_to_modify: 3
    prerequisites:
      - "Phase 4 complete — SignalType.NeedsUserInput, SignalClassification.QuestionText, QuestionResponse.PauseWithQuestion exist"
    entry_point: "Hugh/Handlers/QuestionHandler.cs"
    verification: "Phase 4 verified — `dotnet build` succeeds with new types"
    tasks:
      - step: "5.1"
        task: "Add HandleNeedsUserInput method to QuestionHandler"
        status: complete
        notes: "Added method with ConsoleOutput display, recommendation handling, and PauseWithQuestion return"
        files:
          - "Hugh/Handlers/QuestionHandler.cs"
        details: |
          Add new private method after HandleOpenQuestion:
          
          /// <summary>
          /// Handles a needs_user_input signal by displaying the question and collecting user input.
          /// </summary>
          private QuestionResponse HandleNeedsUserInput(SignalClassification classification)
          {
              _logger.Information(
                  "Subagent needs user input. Question: {Question}",
                  classification.QuestionText ?? "No question text provided");
          
              ConsoleOutput.WriteSection("❓ Subagent Question — User Input Required");
              
              if (!string.IsNullOrWhiteSpace(classification.QuestionText))
              {
                  ConsoleOutput.WriteInfo(classification.QuestionText);
              }
              
              if (classification.RecommendationLetter != null)
              {
                  ConsoleOutput.WriteInfo(
                      $"Recommendation: {classification.RecommendationLetter} — {classification.RecommendationOption}");
              }
              
              Console.WriteLine();
              ConsoleOutput.WriteWarning("Please provide your answer:");
          
              return QuestionResponse.PauseWithQuestion(
                  classification.QuestionText ?? "Subagent requires user input");
          }
          
          Follow the pattern of HandleOpenQuestion — returns a QuestionResponse, does not
          call Console.ReadLine() itself (that's the orchestration layer's responsibility).
        acceptance_criteria:
          - "HandleNeedsUserInput method exists"
          - "Displays question text via ConsoleOutput.WriteSection and WriteInfo"
          - "Displays recommendation if present"
          - "Returns PauseWithQuestion with question text"
          - "Does NOT call Console.ReadLine() — orchestration layer handles input"

      - step: "5.2"
        task: "Add NeedsUserInput case to HandleQuestionAsync switch"
        status: complete
        notes: "Added after ModeSelection case, before default"
        files:
          - "Hugh/Handlers/QuestionHandler.cs"
        details: |
          In HandleQuestionAsync method, add to the switch expression:
          
          SignalType.NeedsUserInput => HandleNeedsUserInput(classification),
          
          Add BEFORE the default case (_ => null).
          After ModeSelection case.
        acceptance_criteria:
          - "SignalType.NeedsUserInput is handled in the switch expression"
          - "Calls HandleNeedsUserInput method"
          - "Existing cases unchanged"

      - step: "5.3"
        task: "Add NeedsUserInput to CopilotService.HandleSignalAsync routing"
        status: complete
        notes: "Added NeedsUserInput to the OnUserInputRequest case group"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In HandleSignalAsync (line ~325), add NeedsUserInput to the switch case that
          routes to OnUserInputRequest. Currently the switch handles:
          - QuestionWithRecommendation, QuestionOpen, ModeSelection → OnUserInputRequest
          - Blocker → _blockerHandler
          - PhaseComplete, WorkflowComplete → null
          
          Add NeedsUserInput to the first group:
          
          case SignalType.QuestionWithRecommendation:
          case SignalType.QuestionOpen:
          case SignalType.ModeSelection:
          case SignalType.NeedsUserInput:  // <-- ADD THIS
              if (OnUserInputRequest != null) ...
        acceptance_criteria:
          - "NeedsUserInput is routed to OnUserInputRequest handler"
          - "Existing signal routing unchanged"

      - step: "5.4"
        task: "Add NeedsUserInput to SdkResponseProcessor.IsPauseSignal"
        status: complete
        notes: "Added NeedsUserInput to the pattern match"
        files:
          - "Hugh/Agents/SdkResponseProcessor.cs"
        details: |
          In IsPauseSignal (line ~191), add NeedsUserInput to the pattern match.
          
          Current:
          return signalType is SignalType.QuestionOpen
              or SignalType.QuestionWithRecommendation
              or SignalType.Blocker
              or SignalType.ModeSelection;
          
          Change to:
          return signalType is SignalType.QuestionOpen
              or SignalType.QuestionWithRecommendation
              or SignalType.Blocker
              or SignalType.ModeSelection
              or SignalType.NeedsUserInput;
          
          Without this change, NeedsUserInput signals are silently ignored
          because ExecutePhaseAsync only calls HandleSignalAsync when
          IsPauseSignal returns true.
        acceptance_criteria:
          - "IsPauseSignal returns true for NeedsUserInput"
          - "Existing pause signals unchanged"

      - step: "5.5"
        task: "Add re-invocation loop in CopilotService.ExecutePhaseAsync for NeedsUserInput"
        status: complete
        notes: "Added loop with max 3 attempts, Console.ReadLine, session continuation, and fallback to existing pause behavior"
        files:
          - "Hugh/Agents/CopilotService.cs"
        details: |
          In ExecutePhaseAsync (line ~165-175), after HandleSignalAsync returns with
          ShouldPause == true, check if QuestionText is non-null (NeedsUserInput case).
          
          Current behavior: returns PhaseResult with Success=false, "Execution paused for user input"
          
          New behavior for NeedsUserInput (wrap in a loop, max 3 iterations):
          1. Display is already handled by QuestionHandler.HandleNeedsUserInput
          2. Read user answer via Console.ReadLine()
          3. Build a follow-up prompt:
             "The user was asked: {questionResponse.QuestionText}
              The user answered: {userAnswer}
              Continue the current phase with this context."
          4. Send the follow-up prompt via SendWithRetryAsync to the SAME session
             (session continuation — not a full restart, because SDK sessions support
             multi-turn and re-initialization is expensive. This differs from VS Code's
             stateless runSubagent which does a full restart per Decision 4.)
          5. Re-process the response via SdkResponseProcessor
          6. If the new response also returns NeedsUserInput, loop back to step 1
             (up to max 3 attempts total to prevent infinite loops)
          7. If max attempts exceeded, return PhaseResult with Success=false and
             error "Max user input attempts exceeded for this phase"
          8. If response is success/partial/blocked, continue normal flow
          
          If QuestionText is null (regular Pause), preserve existing behavior unchanged.
          
          This keeps the re-invocation internal to CopilotService, maintaining the
          existing contract where callers just see a PhaseResult.
        acceptance_criteria:
          - "When ShouldPause is true AND QuestionText is non-null, re-invocation loop begins"
          - "Loop supports up to 3 NeedsUserInput round-trips per phase"
          - "Console.ReadLine() collects user answer on each iteration"
          - "Follow-up prompt sent to same session with answer injected (session continuation)"
          - "Response re-processed normally after each re-invocation"
          - "If max attempts exceeded, returns PhaseResult with Success=false"
          - "Existing Pause() handling (QuestionText == null) returns failure as before"

  - phase: 6
    name: "Unit Tests & Prompt Validation Checklist"
    status: complete
    size: medium
    files_to_modify: 3
    prerequisites:
      - "Phase 5 complete — all Hugh code changes implemented and compiling"
      - "Phases 1-3 complete — all agent prompt changes made"
    entry_point: "Hugh.Tests/SignalClassifierTests.cs"
    verification: "Phase 5 verified — `dotnet build` succeeds for Hugh project"
    tasks:
      - step: "6.1"
        task: "Add NeedsUserInput detection tests to SignalClassifierTests"
        status: complete
        notes: "9 tests added: basic detection, extraction, recommendation, false-positive prevention, case sensitivity, surrounding content, priority (2 tests), plan text false positive"
        files:
          - "Hugh.Tests/SignalClassifierTests.cs"
        details: |
          Add a new test region "#region NeedsUserInput Tests" following existing test patterns.
          
          Tests to add:
          1. Classify_NeedsUserInput_ReturnsCorrectSignal — basic STATUS: needs_user_input + QUESTION_FOR_USER detection
          2. Classify_NeedsUserInput_ExtractsQuestionText — verifies QUESTION_FOR_USER extraction
          3. Classify_NeedsUserInput_ExtractsRecommendation — when both needs_user_input and Recommendation present
          4. Classify_NeedsUserInput_StatusOnly_FallsThrough — STATUS: needs_user_input WITHOUT QUESTION_FOR_USER should NOT classify as NeedsUserInput (prevents false positives from embedded plan text)
          5. Classify_NeedsUserInput_CaseInsensitive — STATUS: NEEDS_USER_INPUT and STATUS: Needs_User_Input
          6. Classify_NeedsUserInput_WithSurroundingContent — needs_user_input embedded in larger output
          7. Classify_NeedsUserInput_PriorityOverModeSelection — should be detected before ModeSelection
          8. Classify_NeedsUserInput_PriorityAfterBlocker — blocker should still win over needs_user_input
          9. Classify_NeedsUserInput_PlanTextFalsePositive — plan document containing STATUS: needs_user_input as example text (without paired QUESTION_FOR_USER) should NOT match
          
          Use [Theory] with [InlineData] for parameterized tests following existing patterns.
        acceptance_criteria:
          - "All 9 test methods exist and pass"
          - "Tests cover: basic detection, question extraction, recommendation extraction, false-positive prevention, case sensitivity, priority order"
          - "Follow existing test naming convention: Classify_{Signal}_{Expectation}"

      - step: "6.2"
        task: "Add QuestionResponse.PauseWithQuestion tests"
        status: complete
        notes: "4 tests in new QuestionResponseTests.cs file"
        files:
          - "Hugh.Tests/QuestionResponseTests.cs"
        details: |
          Add tests for the new PauseWithQuestion factory method (can be in same file or a new QuestionResponseTests.cs):
          
          1. PauseWithQuestion_SetsShouldPause_True
          2. PauseWithQuestion_SetsQuestionText
          3. PauseWithQuestion_ResponseIsNull
          4. Pause_QuestionTextIsNull — verify existing Pause() still returns null QuestionText
        acceptance_criteria:
          - "All 4 tests pass"
          - "Verifies backward compatibility of existing Pause() method"

      - step: "6.3"
        task: "Add or update QuestionHandler tests for NeedsUserInput"
        status: complete
        notes: "7 tests in new QuestionHandlerTests.cs — covers NeedsUserInput, null QuestionText fallback, and regression for existing types"
        files:
          - "Hugh.Tests/QuestionHandlerTests.cs"
        details: |
          If QuestionHandler tests exist, add to them. Otherwise create basic tests:
          
          1. HandleQuestionAsync_NeedsUserInput_ReturnsPauseWithQuestion
          2. HandleQuestionAsync_NeedsUserInput_IncludesQuestionText
          3. HandleQuestionAsync_ExistingTypes_Unchanged — regression test for QuestionOpen and QuestionWithRecommendation
          
          Note: These tests may need to mock ConsoleOutput (static class).
          If mocking ConsoleOutput is complex, test at the return-value level only
          (verify PauseWithQuestion is returned with correct text).
        acceptance_criteria:
          - "NeedsUserInput handler tests pass"
          - "Existing QuestionOpen and QuestionWithRecommendation behavior verified unchanged"

      - step: "6.4"
        task: "Run full test suite and verify all tests pass"
        status: complete
        notes: "332 tests passed, 0 failures"
        files:
          - "Hugh.Tests/"
        details: |
          Run `dotnet test` in the Hugh.Tests directory.
          Verify all existing tests plus new tests pass.
          Fix any regressions introduced by the changes.
        acceptance_criteria:
          - "`dotnet test` exits with 0 failures"
          - "No existing tests broken by the changes"

      - step: "6.5"
        task: "Add Prompt Validation Checklist to plan document"
        status: complete
        notes: "10-item checklist added to Implementation Notes section"
        files:
          - "docs/plans/013-subagent-question-passthrough.md"
        details: |
          Add a "## Prompt Validation Checklist" section to the Implementation Notes.
          Include these manual validation scenarios:
          
          1. Subagent Section Presence: Verify all 6 agent files contain "When Running as a Subagent" section
          2. pch-coder Invocation Template: Verify STATUS includes needs_user_input, question fields in return format
          3. pch-researcher Invocation Template: Same as above
          4. pch-coder Processing: Verify "Handling needs_user_input Status" subsection with 5-step workflow
          5. pch-researcher Processing: Same as above
          6. pch-coder Mode Removal: Verify no "Mode Selection Prompt" exists, "Execution Mode" states always automatic
          7. pch-researcher Pre-Flight: Verify Ambiguity Triggers table has 10 rows
          8. Never Answer Rule: Verify both orchestrators contain "Do NOT answer the question yourself" instruction
          9. VS Code Test: Invoke any agent as subagent, trigger a question, verify parent surfaces it
          10. Backward Compat: Run existing success/partial/blocked flows and verify no regression
        acceptance_criteria:
          - "Checklist section exists in the plan document"
          - "10 validation scenarios documented"
          - "Each scenario has clear pass/fail criteria"

      - step: "6.6"
        task: "Update copilot-instructions.md to document pch-coder always-automatic behavior"
        status: complete
        notes: "Updated Execution Modes, Manual vs Automatic Mode, Mode Selection, and Document Context sections"
        files:
          - ".github/copilot-instructions.md"
        details: |
          Update the "Execution Modes (pch-coder & pch-researcher)" section to reflect
          that pch-coder no longer offers Manual vs Automatic mode selection — it always
          runs in Automatic Mode. Update references in the Mode Selection table.
          Also update the "Agent Workflow" section if it references coder mode selection.
        acceptance_criteria:
          - "copilot-instructions.md reflects pch-coder always-automatic behavior"
          - "Mode Selection table updated for coder context"
          - "Session boundary requirement documented"
```

*(To be populated during implementation by pch-coder)*

## Implementation Notes

### Phase 1 — Agent Subagent Sections — All 6 Agents

**Completed:** 2026-02-08

**Files Modified:**

- `agents/pch-coder.md` — Added "When Running as a Subagent" section at line 910 (35 lines)
- `agents/pch-researcher.md` — Added "When Running as a Subagent" section at line 1451 (34 lines)
- `agents/pch-planner.md` — Added "When Running as a Subagent" section at line 394 (37 lines, includes planner question format mapping)
- `agents/pch-plan-reviewer.md` — Added "When Running as a Subagent" section at line 430 (37 lines, includes reviewer question format mapping)
- `agents/pch-visionary.md` — Added "When Running as a Subagent" section at line 1538 (34 lines, includes Type A/B/C → QUESTION_TYPE mapping)
- `agents/pch-helper.md` — Added "When Running as a Subagent" section at line 326 (34 lines, for consistency)

**Deviations from Plan:** None

**Discoveries:**

- All sections fit within the ≤50 lines constraint (max was 37 lines for planner and reviewer which include agent-specific format mapping notes)
- pch-visionary section maps all 3 question types (Type A → elicitation, Type B → refinement with A/B/C/D/E, Type C → validation with 1/2/3)
- pch-planner section references the `**Recommendation:** [Letter] — [Option Name]` format
- pch-plan-reviewer section references the `**Review Question [N of M]:**` format

### Phase 2 — Orchestrator Invocation & Processing Updates

**Completed:** 2026-02-09
**Execution Mode:** Automatic (Subagent)

**Files Modified:**

- `agents/pch-coder.md` — Updated subagent invocation template with `needs_user_input` STATUS and question fields; added `Handling needs_user_input Status` section; removed mode selection and replaced with always-automatic `Execution Mode` section
- `agents/pch-researcher.md` — Updated subagent invocation template with `needs_user_input` STATUS and question fields; added `Handling needs_user_input Status` section adapted for research context

**Deviations from Plan:** None

**Discoveries:**

- Renamed "One Phase Per Session (Manual Mode)" to "One Phase Per Session" since it applies to session boundaries under automatic mode too
- Git check section updated to remove manual/automatic mode references

### Phase 3 — Pre-Flight Clarification Expansion (pch-researcher)

**Completed:** 2026-02-09
**Execution Mode:** Automatic (Subagent)

**Files Modified:**

- `agents/pch-researcher.md` — Added 5 new triggers to Ambiguity Triggers table (10 total) and 3 new Clarification Pause Examples

**Deviations from Plan:** None

### Phase 4 — Hugh Code Changes — SignalType, SignalClassifier, Models

**Completed:** 2026-02-09
**Execution Mode:** Automatic (Direct)

**Files Modified:**

- `Hugh/Detection/SignalType.cs` — Added `NeedsUserInput` enum value with XML doc comment
- `Hugh/Detection/SignalClassifier.cs` — Added `QuestionText` property to `SignalClassification`, `NeedsUserInputRegex` and `QuestionForUserRegex` patterns, and dual-regex detection logic in `Classify` method
- `Hugh/Models/QuestionResponse.cs` — Added `QuestionText` property and `PauseWithQuestion` factory method

**Deviations from Plan:** None

### Phase 5 — Hugh Code Changes — QuestionHandler, CopilotService & Signal Routing

**Completed:** 2026-02-09
**Execution Mode:** Automatic (Direct)

**Files Modified:**

- `Hugh/Handlers/QuestionHandler.cs` — Added `HandleNeedsUserInput` method and `NeedsUserInput` case to switch expression
- `Hugh/Agents/CopilotService.cs` — Added `NeedsUserInput` to `HandleSignalAsync` routing and re-invocation loop (max 3 attempts) in `ExecutePhaseAsync`
- `Hugh/Agents/SdkResponseProcessor.cs` — Added `NeedsUserInput` to `IsPauseSignal`

**Deviations from Plan:** None

### Phase 6 — Unit Tests & Prompt Validation Checklist

**Completed:** 2026-02-09
**Execution Mode:** Automatic (Direct)

**Files Created:**

- `Hugh.Tests/QuestionResponseTests.cs` — 4 tests for PauseWithQuestion and Pause backward compatibility
- `Hugh.Tests/QuestionHandlerTests.cs` — 7 tests for NeedsUserInput handler and existing behavior regression

**Files Modified:**

- `Hugh.Tests/SignalClassifierTests.cs` — 9 new NeedsUserInput tests added (detection, extraction, false positive prevention, priority)
- `.github/copilot-instructions.md` — Updated to reflect pch-coder always-automatic behavior

**Test Results:** 332 tests passed, 0 failures

## Prompt Validation Checklist

| # | Scenario | Expected Behavior | Pass/Fail |
|---|----------|-------------------|-----------|
| 1 | **Subagent Section Presence** — All 6 agent files contain "When Running as a Subagent" section | Each file has section with `needs_user_input` return format, QUESTION_FOR_USER, QUESTION_TYPE, QUESTION_OPTIONS, RECOMMENDATION fields | ✅ Pass |
| 2 | **pch-coder Invocation Template** — STATUS includes `needs_user_input`, question fields in return format | Subagent Invocation section has `STATUS: success \| partial \| blocked \| needs_user_input` and 4 question fields | ✅ Pass |
| 3 | **pch-researcher Invocation Template** — Same as above | Researcher's subagent invocation has matching STATUS and question fields | ✅ Pass |
| 4 | **pch-coder Processing** — "Handling `needs_user_input` Status" subsection with 5-step workflow | Section exists with extract → display → wait → re-invoke → process workflow | ✅ Pass |
| 5 | **pch-researcher Processing** — Same as above, adapted for research | Section references research document path and phases | ✅ Pass |
| 6 | **pch-coder Mode Removal** — No "Mode Selection Prompt", "Execution Mode" states always automatic | No mode selection UI, "## Execution Mode" section states always Automatic Mode | ✅ Pass |
| 7 | **pch-researcher Pre-Flight** — Ambiguity Triggers table has 10 rows | 5 existing + 5 new triggers (Technology choices, Configuration dependencies, Multi-option scope, User preference implicit, External dependencies) | ✅ Pass |
| 8 | **Never Answer Rule** — Both orchestrators contain "Do NOT answer the question yourself" | 🚨 CRITICAL warning present in both pch-coder and pch-researcher handling sections | ✅ Pass |
| 9 | **VS Code Test** — Invoke any agent as subagent, trigger a question, verify parent surfaces it | Parent agent displays question to user via ask_questions tool or plain text | ⬜ Blocked (requires manual VS Code Copilot chat testing) |
| 10 | **Backward Compat** — Run existing success/partial/blocked flows and verify no regression | All existing STATUS values processed correctly; no behavioral changes | ⬜ Blocked (requires manual Hugh execution testing) |

### Validation Notes (Phase 3 — 2026-02-10)

**Automated Verification Results:**
- **Items 1-8**: Verified programmatically by reading all 6 agent files
- **Item 1**: All 6 files contain "## When Running as a Subagent" section (pch-coder.md:929, pch-researcher.md:1739, pch-planner.md:399, pch-plan-reviewer.md:430, pch-visionary.md:1783, pch-helper.md:320)
- **Item 2-3**: Both orchestrator files have `STATUS: success | partial | blocked | needs_user_input` and 4 question fields in subagent invocation templates
- **Item 4-5**: Both files contain "Handling `needs_user_input` Status" section with 5-step workflow and 🚨 CRITICAL warning
- **Item 6**: pch-coder.md contains "## Execution Mode" section stating always Automatic Mode; no mode selection prompt found
- **Item 7**: pch-researcher.md Ambiguity Triggers table verified at line 370 with 10 rows (5 existing + 5 new)
- **Item 8**: Both pch-coder.md and pch-researcher.md contain 🚨 CRITICAL "Do NOT answer the question yourself" in handling sections

**Manual Tests Blocked:**
- **Item 9**: Requires interactive VS Code Copilot chat session to invoke `@pch-coder` with an ambiguous task and verify question passthrough
- **Item 10**: Requires running Hugh CLI against a workspace with completed plans to verify success/partial/blocked STATUS processing has no regression

## Holistic Review Summary

### Key Decision Interactions

1. **All-Agents Scope × Dedicated Section Placement:** Updating all 6 agents with dedicated "When Running as a Subagent" sections (decisions 2+3) adds ~180-300 total lines across agent files. This is acceptable because each section is ≤50 lines and agents that rarely need it (pch-helper) have minimal overhead. The dedicated section approach ensures reliability even when Copilot unexpectedly invokes agents as subagents.

2. **Always-Automatic pch-coder × NeedsUserInput Status:** Removing manual mode from pch-coder (decision 5b) increases reliance on the `needs_user_input` mechanism — since there's no manual fallback, the question passthrough must work reliably. This makes the defense-in-depth approach (decision 1: all 5 layers) critical rather than optional. The Full Hybrid scope is a good match for the always-automatic requirement.

3. **Full Subagent Restart × Pre-Flight Prevention:** The restart strategy (decision 4) means a `needs_user_input` return costs a full re-invocation. This makes the pre-flight prevention layer (decision 5) more valuable — catching ambiguities before subagent invocation saves compute/time. These decisions reinforce each other well.

4. **pch-researcher Pre-Flight Only × pch-coder Always Automatic:** Not expanding pre-flight checks for pch-coder (decision 5) is justified because plans go through planner + reviewer before coder. However, with always-automatic mode (decision 5b), any plan-level ambiguity that slipped through will trigger `needs_user_input` rather than being ignored. This is the intended fallback.

5. **Console ReadLine × Hugh Re-Invocation:** The simple ReadLine approach (decision 6) works well with the full restart strategy (decision 4) since the orchestration loop only needs to append the answer to a re-invocation prompt. No complex state management required.

### Architectural Considerations Addressed

- **Backward Compatibility:** All changes are additive. `Pause()` factory method unchanged, existing STATUS values preserved, new enum value added at end. No breaking changes to existing signal processing.
- **Priority Order in SignalClassifier:** `NeedsUserInput` is placed after Blocker but before ModeSelection. A blocker is more severe (cannot proceed at all). ModeSelection is less urgent. This priority chain is correct.
- **Context Window Impact:** 6 agents × ~40-50 lines = ~240-300 lines of total prompt additions. Within acceptable bounds (NFR-2 ≤50 per agent).
- **Circular Dependency Risk:** `QuestionHandler` references `ConsoleOutput` (static class) — no injection needed. `PauseWithQuestion` adds a property to an existing record — no new dependencies.

### Trade-offs Accepted

- **Re-work on restart:** Full subagent restart means some work is redone. Accepted because phases are small (≤12 tasks) and the simplicity benefit outweighs the cost.
- **No timeout on ReadLine:** Hugh will block indefinitely waiting for user input. Accepted for initial implementation; timeout is documented as out-of-scope future enhancement.
- **LLM compliance uncertainty:** Subagents may not consistently populate `QUESTION_FOR_USER`. Mitigated by fallback (if field is missing but `needs_user_input` detected, display raw match as question text).

### Risks Acknowledged

- Biggest risk is LLM subagents choosing to answer questions autonomously rather than returning `needs_user_input`. Mitigated by explicit "Do NOT attempt to answer" instructions in both the subagent section (all 6 agents) and the parent processing instructions (pch-coder, pch-researcher).
- Second risk is excessive pausing from false-positive `needs_user_input` returns. Mitigated by clear "When NOT to use" guidance in the subagent section.

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | 6 agent prompts + 4 Hugh files across 2 domains |
| New patterns introduced | 2 | NeedsUserInput signal + re-invocation loop (extends existing) |
| External dependencies | 1 | No new external dependencies |
| Migration complexity | 1 | All changes additive, no data migration |
| Test coverage required | 3 | Unit tests + manual prompt validation checklist |
| **Overall Complexity** | **10/25** | **Low** — changes follow established patterns across both domains |

## Review Summary

**Review Date:** 2026-02-08
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.2 (two review passes)

### Review Metrics — Pass 1 (v2.0 → v2.1)
- Issues Found: 5 (Critical: 2, Major: 2, Minor: 1)
- Clarifying Questions Asked: 5
- Sections Updated: Phase 5 (rewritten), Phase 6 (test files), Technical Design (processing instructions, subagent template)

### Review Metrics — Pass 2 (v2.1 → v2.2)
- Issues Found: 4 (Critical: 1, Major: 1, Minor: 2)
- Clarifying Questions Asked: 4
- Sections Updated: Task 1.1 details, FR-12, Decision 4, Task 4.4, Technical Design (Classify code), Task 5.5, Task 6.1

### Key Improvements Made (Pass 1)
1. **Task 5.3 corrected** — Retargeted from LoopOrchestrator.cs/PhaseExecutor.cs to CopilotService.cs where signal handling actually occurs
2. **Tasks 5.3, 5.4, 5.5 added** — NeedsUserInput routing in HandleSignalAsync, IsPauseSignal gate, and re-invocation loop in ExecutePhaseAsync
3. **CONTEXT field removed** — Eliminated inconsistency where subagent template included CONTEXT but no consumer parsed it
4. **ask_questions tool guidance added** — Processing instructions now specify tool usage by question type
5. **Test file placement fixed** — QuestionResponse tests -> QuestionResponseTests.cs, QuestionHandler tests -> QuestionHandlerTests.cs

### Key Improvements Made (Pass 2)
1. **Task 1.1 CONTEXT reference removed** — Stale CONTEXT field mention in task details eliminated (was removed in v2.1 but details not updated)
2. **FR-12 corrected** — ReadLine responsibility correctly attributed to orchestration layer (CopilotService.ExecutePhaseAsync), not QuestionHandler
3. **VS Code vs Hugh strategy clarified** — Decision 4 clarified as applying to VS Code stateless subagents; Hugh uses session continuation. Task 5.5 updated with max 3 retry loop to handle recursive NeedsUserInput signals
4. **Dual-regex classification required** — NeedsUserInput detection now requires BOTH `STATUS: needs_user_input` AND `QUESTION_FOR_USER:` to match, preventing false positives from embedded plan document text. Test case 9 added for false-positive prevention

### Remaining Considerations for Implementers
- Hugh's `HandleModeSelectionAsync` auto-selects Manual mode ("1") for pch-researcher — this remains correct but is worth knowing
- The `QuestionForUserRegex` uses `.+?` with singleline — with the lazy quantifier this captures up to the first newline only (single-line question text). Multi-line QUESTION_FOR_USER values will be truncated to the first line
- Phase 5 has 5 tasks; Task 5.5 is the most complex (re-invocation loop)
- Step 6.1 now has 9 test cases (up from 8) with the new false-positive prevention test
- Step 6.6 references `.github/copilot-instructions.md` — path confirmed correct

### Agent Prompt Compatibility Notes
- **GitHub Copilot (VS Code):** The `ask_questions` tool usage for refinement/validation questions requires the orchestrator LLM to map QUESTION_OPTIONS to tool parameters. The processing instructions now specify this mapping explicitly.
- **Hugh:** Hugh's re-invocation loop is fully internal to CopilotService.ExecutePhaseAsync. It uses session continuation (not restart). The orchestration layer (LoopOrchestrator) sees a normal PhaseResult — no awareness of NeedsUserInput needed at that level.
- **Both contexts:** The "When Running as a Subagent" section works identically in both contexts since it instructs the subagent (not the parent) on return format. The parent handling diverges correctly: VS Code uses ask_questions tool + chat display (full restart), Hugh uses ConsoleOutput + Console.ReadLine (session continuation).

### Sign-off
This plan has been reviewed (two passes) and is **Ready for Implementation**.

---

## Review Session Log

```yaml
review_session_log:
  questions_pending: 0
  questions_resolved: 9
  last_updated: "2026-02-08"
  entries:
    # --- Pass 1 (v2.0 → v2.1) ---
    - number: 1
      issue: "Task 5.3 targets wrong files — CopilotService.cs is the actual integration point"
      category: correctness
      decision: "Option A: CopilotService.ExecutePhaseAsync internal re-invoke loop"
      plan_update: "Task 5.3 rewritten; tasks 5.4, 5.5 added"
    - number: 2
      issue: "CopilotService.HandleSignalAsync and SdkResponseProcessor.IsPauseSignal missing NeedsUserInput"
      category: completeness
      decision: "Addressed by new tasks 5.3 (HandleSignalAsync) and 5.4 (IsPauseSignal)"
      plan_update: "Tasks 5.3, 5.4 added in question 1 resolution"
    - number: 3
      issue: "CONTEXT field present in subagent template but absent from orchestrator return format and Hugh regex"
      category: clarity
      decision: "Option A: Remove CONTEXT field entirely"
      plan_update: "Subagent template and acceptance criteria updated"
    - number: 4
      issue: "ask_questions tool usage not specified for VS Code orchestrator question relay"
      category: specificity
      decision: "Option A: ask_questions for refinement/validation, plain text for elicitation"
      plan_update: "Processing instructions template updated with tool usage guidance"
    - number: 5
      issue: "Test file placement — QuestionResponse and QuestionHandler tests assigned to SignalClassifierTests.cs"
      category: correctness
      decision: "Option A: Separate test files for each class"
      plan_update: "Steps 6.2 and 6.3 updated with correct test file paths"
    # --- Pass 2 (v2.1 → v2.2) ---
    - number: 6
      issue: "Task 1.1 details still reference removed CONTEXT field"
      category: clarity
      decision: "Option A: Remove CONTEXT from task 1.1 details"
      plan_update: "Task 1.1 details updated to list 5 fields without CONTEXT"
    - number: 7
      issue: "FR-12 says QuestionHandler collects via ReadLine, but design has orchestration layer doing ReadLine"
      category: clarity
      decision: "Option A: Fix FR-12 wording"
      plan_update: "FR-12 updated to attribute ReadLine to CopilotService.ExecutePhaseAsync"
    - number: 8
      issue: "Task 5.5 'same session' contradicts Decision 4 'Full Subagent Restart'; no recursive questioning support"
      category: correctness
      decision: "Option A: Clarify VS Code vs Hugh distinction + add loop with max 3 retries"
      plan_update: "Decision 4 clarification added; Task 5.5 updated with loop construct and acceptance criteria"
    - number: 9
      issue: "NeedsUserInputRegex may false-positive on plan document text embedded in subagent output"
      category: correctness
      decision: "Option A: Require both STATUS + QUESTION_FOR_USER for classification"
      plan_update: "Task 4.4 updated with dual-regex requirement; Technical Design code example updated; test case 9 added to Task 6.1"
```

---

```yaml
handoff:
  created_by: pch-planner
  created_date: "2026-02-08"
  reviewed_by: pch-plan-reviewer
  review_date: "2026-02-08"
  review_passes: 2
  status: ready-for-implementation
  next_agent: pch-coder
  plan_location: "/docs/plans/013-subagent-question-passthrough.md"
```
