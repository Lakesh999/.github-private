---
id: "023"
type: research
title: "Hugh Research Phase Skip Failure — Outline-Only Execution"
status: ✅ Complete
created: "2026-02-23"
current_phase: "5 of 5"
---

## Introduction

When Hugh was asked to create a single-player Pong game in HTML/JavaScript, it created the vision document and research outline document correctly, but executed **none of the actual research phases** before moving on to planning, plan review, and coding. The planning phase also asked no questions, which is unusual. This research investigates the root causes of this failure across the orchestration pipeline and documents possible fixes.

## Objectives

- Why does Hugh treat a research outline document as "complete" research?
- How does `LoopOrchestrator` dispatch the Research phase — single-shot, interactive, or multi-session?
- What does `pch-researcher` return when it creates only an outline, and how does `SdkResponseProcessor` interpret that?
- Why did the planning phase ask no questions — was it missing research context, or was the prompt structured to suppress interaction?
- What architectural changes are needed so Hugh executes all research sub-phases before advancing?

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Research Phase Dispatch & Execution Flow | ✅ Complete | How `LoopOrchestrator.DispatchPhaseAsync` routes the Research phase; `ExecuteSinglePhaseAsync` vs `ExecuteInteractivePhaseAsync`; `BuildPromptForPhase` for Research; how `PhaseExecutor.ExecuteAsync` invokes the SDK session; what constitutes "success" for the Research phase | 2026-02-23 |
| 2 | pch-researcher Agent Behavior Under Hugh | ✅ Complete | What `pch-researcher` does when invoked as a single-shot phase; how the agent prompt's "create outline then STOP" instruction conflicts with Hugh's single-session model; whether the agent returns structured completion signals or just text; how `SdkResponseProcessor` parses the research response | 2026-02-23 |
| 3 | Document Path Extraction & Validation | ✅ Complete | How `DocumentPathExtractor` finds the research document path from agent output; whether an outline-only document passes the `PhaseProducesDocument` / `File.Exists` validation; how state manager records the research document path for downstream phases | 2026-02-23 |
| 4 | Planning Phase Question Suppression | ✅ Complete | How planning prompt is constructed (`BuildPromptForPhase` for Planning); what documents are attached; whether an outline-only research doc causes planner to skip questions; `IsInteractivePhase` for Planning; `ExecuteInteractivePhaseAsync` vs `ExecuteSinglePhaseAsync` for Planning | 2026-02-23 |
| 5 | Fix Analysis & Architectural Options | ✅ Complete | Possible fixes: inner research sub-phase loop (like coding); research phase completeness validation (parse outline phases, verify all complete); prompt changes to make pch-researcher execute all phases in one session; trade-offs of each approach; recommended fix strategy | 2026-02-23 |

## Phase 1: Research Phase Dispatch & Execution Flow

**Status:** ✅ Complete  
**Session:** 2026-02-23

### 1. How `DispatchPhaseAsync` Routes the Research Phase

`DispatchPhaseAsync` at `Hugh/Orchestration/LoopOrchestrator.cs` (lines 338-370) has a three-way dispatch:

```csharp
protected virtual async Task<PhaseResult> DispatchPhaseAsync(
    LoopPhase phase, LoopState state, CancellationToken ct)
{
    if (phase == LoopPhase.Coding)
        return await ExecuteCodingPhasesAsync(planPath, ct);

    if (IsInteractivePhase(phase))
        return await ExecuteInteractivePhaseAsync(phase, state, ct);

    return await ExecuteSinglePhaseAsync(phase, state, ct);
}
```

**Research is routed to `ExecuteSinglePhaseAsync`** because:
- It is not `LoopPhase.Coding`, so the coding sub-phase loop is skipped.
- `IsInteractivePhase(LoopPhase.Research)` returns `false` because the config `InteractivePhases` only contains `["Vision"]` (see `Hugh/appsettings.json` line 21).

### 2. `ExecuteSinglePhaseAsync` — Single-Shot Execution

At lines 372-384:

```csharp
private async Task<PhaseResult> ExecuteSinglePhaseAsync(
    LoopPhase phase, LoopState state, CancellationToken ct)
{
    var prompt = BuildPromptForPhase(phase, state);
    var documents = GetDocumentsForPhase(phase, state.Documents);
    return await _phaseExecutor.ExecuteAsync(phase, prompt, documents);
}
```

This calls `PhaseExecutor.ExecuteAsync` **exactly once**. No loop, no sub-phase detection, no re-invocation.

### 3. `BuildPromptForPhase` for Research

The standalone Research prompt (lines 768-773):

```csharp
LoopPhase.Research => """
    Research the technical approach for implementing this feature.
    The vision document is attached for context.
    Please select Automatic Mode and execute all research phases.
    """,
```

**Fundamental mismatch:** The prompt asks pch-researcher to "execute all research phases" in a single SDK session, but pch-researcher is hard-wired to create an outline then STOP. The phrase "select Automatic Mode" is from an older design where mode selection existed — the current agent has no mode selection.

### 4. What Constitutes "Success" for the Research Phase

When pch-researcher creates an outline and outputs "📋 **Research Outline Created**":

- `SignalClassifier` finds **no matching pattern** — the handoff message doesn't match `BlockerRegex`, `NeedsUserInputRegex`, `WorkflowCompleteRegex`, or `PhaseCompleteRegex`
- Result: `SignalType.None`
- `IsPauseSignal(SignalType.None)` returns `false`
- `CopilotService.ExecutePhaseAsync` returns `Success = true`

Document validation also passes:
- `PhaseProducesDocument(Research)` → `true`
- `DocumentPathExtractor` extracts the outline path from the handoff message
- `File.Exists(outlinePath)` → `true` (the file was created by the agent)

### 5. Comparison with Coding Sub-Phase Loop

Coding has `ExecuteCodingPhasesAsync` (lines 436-500):

```csharp
private async Task<PhaseResult> ExecuteCodingPhasesAsync(string planPath, CancellationToken ct)
{
    while (!ct.IsCancellationRequested)
    {
        var planContent = await File.ReadAllTextAsync(planPath, ct);
        var nextPhase = _planPhaseParser.FindNextIncompletePhase(planContent);
        if (nextPhase == null) break;  // All coding phases complete
        // Execute one coding sub-phase...
    }
}
```

Key differences:
- **Coding has a loop** that re-reads the plan document each iteration
- **Research has NO loop** — single `ExecuteAsync` call
- **Coding uses `PlanPhaseParser.FindNextIncompletePhase()`** to drive sub-phases
- **Research has no equivalent parser** — no mechanism to detect `⏳ Not Started` research sub-phases

### 6. The Root Cause Chain

```
1. Hugh sends single prompt: "Research... execute all research phases."
2. pch-researcher receives this as a NEW request (no existing research doc attached)
3. Per its hard rules: creates outline → outputs handoff message → STOPS
4. Agent output has no error, no pause signal → SignalType.None
5. CopilotService returns Success = true with the outline document path
6. ExecuteLoopAsync validates: document exists on disk → passes
7. MarkPhaseCompleteAsync: Research marked complete, outline stored as research
8. Loop advances to Planning, which receives the OUTLINE (not research) as context
```

**Key Discoveries:**
- Research is dispatched as `ExecuteSinglePhaseAsync` — single-shot, no sub-phase loop unlike Coding's `ExecuteCodingPhasesAsync`
- `IsInteractivePhase(Research)` returns `false` (config only lists Vision)
- The prompt says "execute all research phases" but pch-researcher is hard-wired to create only an outline and STOP
- `CopilotService` returns `Success = true` when no error/pause signal is detected — the outline handoff doesn't match any `SignalClassifier` pattern
- Document validation passes because the outline file exists on disk
- There is no `ResearchPhaseParser` equivalent — Coding uses `PlanPhaseParser` for its loop; Research has no analogous mechanism
- The outline path is stored as `documents.Research` and passed to Planning as if it were completed research

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | Core orchestration: dispatch routing, single-shot execution, prompt building, coding sub-phase loop, validation |
| `Hugh/Orchestration/PhaseExecutor.cs` | ExecuteAsync single-shot executor, no content inspection |
| `Hugh/Agents/CopilotService.cs` | ExecutePhaseAsync success determination, signal handling |
| `Hugh/Agents/SdkResponseProcessor.cs` | Dual-detection signal processing, IsPauseSignal checks |
| `Hugh/Detection/SignalClassifier.cs` | All regex patterns for signal detection |
| `Hugh/appsettings.json` | InteractivePhases: ["Vision"], Research timeout 30min |

**Gaps:** None  
**Assumptions:** When pch-researcher receives a new request prompt without an existing research document attached, it follows its "new request" path and creates only an outline. Based on agent prompt design but actual behavior may vary.

## Phase 2: pch-researcher Agent Behavior Under Hugh

**Status:** ✅ Complete  
**Session:** 2026-02-23

### 1. Hugh's Research Prompt vs. Agent Decision Tree

Hugh sends (from `LoopOrchestrator.BuildPromptForPhase`):

```
Research the technical approach for implementing this feature.
The vision document is attached for context.
Please select Automatic Mode and execute all research phases.
```

pch-researcher's decision tree evaluates:
1. Does user reference an existing research document? → **NO**
2. Does user reference a vision document? → **YES** (attached vision path contains `/docs/vision/`)

This classifies Hugh's prompt as **"VISION-DRIVEN NEW REQUEST"**, triggering: read vision → derive phases → create outline → **STOP**.

The agent's "🚨 ONE RULE THAT MATTERS" instruction takes absolute precedence over Hugh's "execute all research phases" text.

### 2. What pch-researcher Actually Outputs

The agent creates a research outline at `/docs/research/NNN-description.md` and outputs:

```
📋 **Research Outline Created**

The research document has been created at: `[document path]`
...
🚨 **IMPORTANT:** I will NOT start Phase 1 now.
⚠️ **Please start a new chat session...**
```

### 3. Signal Detection Analysis

No `hugh_*` tool is called during outline creation — it's not a phase completion or workflow completion.

Text regex fallback through `SignalClassifier`:

| Regex | Match? | Why |
|-------|--------|-----|
| `BlockerRegex` | ❌ | No ❌ or blocker keywords |
| `NeedsUserInputRegex` | ❌ | No "STATUS: needs_user_input" |
| `ModeSelectionRegex` | ❌ | No "🔄 Execution Mode Selection" |
| `WorkflowCompleteRegex` | ❌ | "Research Outline Created" ≠ "Research Complete" |
| `PhaseCompleteRegex` | ❌ | No "✅ Phase [N] Complete" |

**Result: `SignalType.None`** — the outline handoff is invisible to Hugh's signal system.

### 4. CopilotService Processing

`IsPauseSignal(SignalType.None)` → `false` → phase returns `Success = true`.

The `DocumentPathExtractor` successfully extracts the outline path from the handoff message (matches `BacktickPathRegex` on "created at: `path`").

### 5. No Built-in Prompt Override for Research

Unlike the Vision phase, which has explicit autonomous override instructions in `BuildPromptForPhase` (telling the visionary to work autonomously in Hugh's context), the Research phase has **no such override**. The prompt simply says "Please select Automatic Mode" without explaining Hugh's execution model.

### 6. The "Automatic Mode" Text is Vestigial

"Please select Automatic Mode" refers to an older design where pch-researcher offered a mode selection menu. The current agent always runs in automatic mode with no selection UI. This phrase is ignored.

**Key Discoveries:**
- pch-researcher classifies Hugh's prompt as "VISION-DRIVEN NEW REQUEST" → outline-only + STOP
- The agent's hard "ONE RULE" instruction to stop after outline takes precedence over Hugh's prompt
- No `hugh_*` tool is called during outline creation (it's not a phase/workflow completion)
- The handoff message matches ZERO `SignalClassifier` patterns → `SignalType.None`
- `None` is not a pause signal → `Success = true` — treating an incomplete outline as complete research
- No built-in prompt override exists for Research (unlike Vision's autonomous override)
- "Please select Automatic Mode" in Hugh's prompt is vestigial and ignored
- The agent is working as designed; Hugh is the component that needs to adapt

| File | Relevance |
|------|----------|
| `agents/pch-researcher.md` | Decision tree, ONE RULE, handoff message, Structured Communication |
| `Hugh/Agents/CopilotService.cs` | ExecutePhaseAsync signal processing, success path |
| `Hugh/Agents/SdkResponseProcessor.cs` | Dual detection: tool first, text fallback |
| `Hugh/Detection/SignalClassifier.cs` | Regex priority chain, WorkflowComplete vs outline |
| `Hugh/Prompts/BuiltIn/` | Only triage/clarification/decomposition — no research override |

**Gaps:** None  
**Assumptions:** pch-researcher faithfully follows its prompt (creating only an outline for new requests). LLMs might occasionally ignore STOP instructions, but the prompt heavily emphasizes this behavior.

## Phase 3: Document Path Extraction & Validation

**Status:** ✅ Complete  
**Session:** 2026-02-23

### 1. DocumentPathExtractor Regex Patterns

`DocumentPathExtractor` uses 4 regex patterns in priority order:

| Priority | Pattern | Example Match |
|----------|---------|---------------|
| 1 | ExplicitCreation | "Created vision document at: docs/vision/001.md" |
| 2 | HandoffDocument | "**Document:** docs/research/001.md" |
| 3 | BacktickPath | "created at: \`docs/research/001.md\`" |
| 4 | MarkdownLink | "[001.md](docs/plans/001.md)" |

pch-researcher's handoff message "The research document has been created at: \`[path]\`" matches both Pattern 1 (ExplicitCreation) and Pattern 3 (BacktickPath). The path IS extractable.

### 2. PhaseExecutor's 4-Level Extraction Fallback

`PhaseExecutor.ExtractOutputDocumentPath` has a 4-level fallback:

1. **Level 1**: Path from `SdkResponseProcessor` (text/tool extraction) → likely succeeds
2. **Level 2**: Expected path using `{runId}-research.md` → **fails** (agent uses `NNN-description.md`)
3. **Level 3**: Check expected path on disk → fails (naming mismatch)
4. **Level 4**: Scan directory for recently created `.md` files → succeeds as fallback

### 3. Post-Phase Validation is Existence-Only

```csharp
if (PhaseProducesDocument(phase))  // Vision, Research, Planning
{
    if (string.IsNullOrEmpty(result.OutputDocumentPath)) → FAIL
    if (!File.Exists(result.OutputDocumentPath)) → FAIL
}
```

Validation checks:
- ✅ Is the path non-null/empty?
- ✅ Does the file exist on disk?
- ❌ Does NOT check if phases are complete
- ❌ Does NOT check if document has content beyond outline headers
- ❌ Does NOT parse the `## Research Phases` table for status markers

An outline-only document (all phases ⏳ Not Started) passes both checks because the file physically exists.

### 4. Document Path Propagation

`MarkPhaseCompleteAsync` stores the outline path as `Documents.Research`. `GetDocumentsForPhase` then attaches it:

```csharp
LoopPhase.Planning => GetNonNullDocuments(documents.Vision, documents.Research),
LoopPhase.Review => GetNonNullDocuments(documents.Vision, documents.Research, documents.Plan),
```

The **outline-only research document** is attached to Planning and Review phases — planner receives a document with zero findings.

### 5. No ResearchPhaseParser Exists

`PlanPhaseParser` can detect incomplete phases via emoji status markers (✅/🔄/⏳/❌) in table rows. Research documents use the **same emoji format** in their `## Research Phases` table. A `ResearchPhaseParser` modeled on `PlanPhaseParser` would be straightforward.

**Key Discoveries:**
- `DocumentPathExtractor` successfully extracts the outline path from the handoff message
- Post-phase validation is existence-only — no content/completeness validation
- An outline-only document passes all validation because the file exists on disk
- The outline path is stored as `Documents.Research` and propagated to Planning and Review
- No `ResearchPhaseParser` exists to detect incomplete research phases
- Research documents use the same emoji-status table format as plans — parsing is feasible
- Expected path naming (`{runId}-research.md`) doesn't match agent naming (`NNN-description.md`)

| File | Relevance |
|------|----------|
| `Hugh/Agents/DocumentPathExtractor.cs` | All 4 regex patterns for path extraction |
| `Hugh/Orchestration/PhaseExecutor.cs` | ExtractOutputDocumentPath 4-level fallback chain |
| `Hugh/Orchestration/LoopOrchestrator.cs` | PhaseProducesDocument validation, GetDocumentsForPhase |
| `Hugh/Orchestration/LoopStateManager.cs` | MarkPhaseCompleteAsync document path storage |
| `Hugh/Detection/PlanPhaseParser.cs` | Template for future ResearchPhaseParser |

**Gaps:** None  
**Assumptions:** Agent outputs handoff message as plain text (not wrapped in code fence), so regex extraction works.

## Phase 4: Planning Phase Question Suppression

**Status:** ✅ Complete  
**Session:** 2026-02-23

### 1. Planning Runs as Single-Shot, NOT Interactive

`IsInteractivePhase(LoopPhase.Planning)` returns `false` — `InteractivePhases` in config only contains `["Vision"]`. Planning is dispatched via `ExecuteSinglePhaseAsync`.

This is a **critical architectural mismatch**. `pch-planner` is designed for multi-turn Q&A (one question at a time, stop-and-wait), but Hugh runs it single-shot.

### 2. The Planning Prompt is Misleading

```csharp
LoopPhase.Planning => """
    Create a detailed implementation plan for this feature.
    The vision and research documents are attached for context.
    Please select Automatic Mode and create the complete plan.
    """,
```

`pch-planner` has **no concept of "Automatic Mode"** — no mode selection, no automatic behavior. This instruction is either ignored or misinterpreted as "work autonomously without questions." It appears to be a copy-paste from the Research phase prompt.

### 3. Documents Are Path References, Not Inlined

`BuildPromptWithDocuments` only lists file paths:
```
**Attached Documents:**
- /docs/vision/abc12345-vision.md
- /docs/research/abc12345-research.md
```

The planner must use `read_file` to load content. It receives both vision and research paths.

### 4. Question Handling in Single-Shot (Autonomous) Mode

In `ExecutePhaseAsync`, if the planner asks questions:
- Questions **with recommendations** → auto-accepted via `SdkQuestionHandler` (up to 3 in a re-invocation loop)
- **Open questions** → `Success = false` in autonomous mode (console input skipped)

```csharp
if (_config.Autonomous.Enabled || Console.IsInputRedirected)
{
    return new PhaseResult
    {
        Success = false,
        ErrorMessage = $"Phase paused — user input required: {questionResponse.QuestionText}",
    };
}
```

### 5. Why the Planner Produced No Questions

Multiple converging factors:

| Factor | Explanation |
|--------|-------------|
| **Single-shot framing** | "Create the complete plan" frames as a directive, not iterative Q&A |
| **"Automatic Mode" instruction** | LLM may interpret as "work autonomously without questions" |
| **Outline-only research** | Zero findings = fewer decision points to surface as questions |
| **Planner design: "create document first"** | In single-shot mode, planner creates skeleton and fills with defaults |
| **Auto-accept chain** | If questions ARE asked with recommendations, they're silently auto-accepted |

### 6. The Planning Failure Chain

```
1. Research outline-only doc stored as Documents.Research
2. Planning dispatched as ExecuteSinglePhaseAsync (NOT interactive)
3. Prompt: "create complete plan... Automatic Mode" + path references
4. Planner LLM skips Q&A workflow due to single-shot framing
5. No questions = no decisions logged in Session Log
6. Plan created without human input on key decisions
```

**Key Discoveries:**
- Planning is NOT interactive (`InteractivePhases` only has `["Vision"]`)
- "Select Automatic Mode" is meaningless to `pch-planner` — copy-paste artifact from Research prompt
- Single-shot execution is incompatible with planner's multi-turn Q&A design
- Open planner questions would cause `Success = false` in autonomous mode
- Questions with recommendations are auto-accepted silently (up to 3)
- Outline-only research reduces decision points the planner might surface

| File | Relevance |
|------|----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | IsInteractivePhase, BuildPromptForPhase, GetDocumentsForPhase |
| `Hugh/appsettings.json` | `InteractivePhases: ["Vision"]` excludes Planning |
| `Hugh/Agents/CopilotService.cs` | ExecutePhaseAsync single-shot path, auto-accept + pause logic |
| `Hugh/Handlers/SdkQuestionHandler.cs` | Auto-accept for recommendations, pause for open questions |
| `agents/pch-planner.md` | Designed for iterative Q&A, no Automatic Mode concept |

**Gaps:** None  
**Assumptions:** Planner LLM behavior (generating plan without questions) inferred from code structure, not directly observed.

## Phase 5: Fix Analysis & Architectural Options

**Status:** ✅ Complete  
**Session:** 2026-02-23

### Four Fix Approaches Analyzed

#### Approach A: Research Sub-Phase Loop (Like Coding)

Add `ExecuteResearchPhasesAsync` modeled on `ExecuteCodingPhasesAsync`. First invocation creates outline; subsequent iterations re-read the document, find next `⏳ Not Started` phase via `ResearchPhaseParser`, and re-invoke pch-researcher with a continuation prompt.

- **Complexity:** Medium-High (~150-200 lines)
- **Pros:** Mirrors proven coding pattern; fresh context per sub-phase; no agent prompt changes
- **Cons:** Needs `ResearchPhaseParser` (new component); doesn't fix planning suppression

#### Approach B: Completeness Validation Only

After `ExecuteSinglePhaseAsync` for Research, parse document for incomplete phases. If `⏳ Not Started` phases remain, mark as failed.

- **Complexity:** Low (~50-80 lines)
- **Pros:** Small change, detects problem reliably
- **Cons:** **Detects but doesn't fix.** Pipeline halts. Retry re-invokes same prompt → same outline → infinite retry loop.

#### Approach C: Prompt Override (Single-Session Research)

Override pch-researcher's "create outline then STOP" with Hugh-specific instructions to execute all research in one session.

- **Complexity:** Low (~20-30 lines)
- **Pros:** Minimal code change
- **Cons:** **Unreliable.** Competes with agent's heavily-emphasized "ONE RULE." Breaks phased context management. No progress persistence on failure.

#### Approach D: Hybrid (Recommended)

Combine loop (A) + validation (B) + prompt cleanup into a layered solution.

**Components:**
1. `ResearchPhaseParser` — Parse research document phase tables
2. `ExecuteResearchPhasesAsync` — Sub-phase loop for research
3. Completeness validation — Post-loop safety net
4. Research prompt cleanup — Remove vestigial "Automatic Mode", add Hugh context
5. Planning prompt fix — Remove "Automatic Mode", add autonomous decision instruction

### Evaluation Matrix

| Criterion | A: Loop | B: Validate | C: Override | D: Hybrid |
|-----------|---------|-------------|-------------|----------|
| Fixes research skip? | ✅ | ❌ Detect only | ⚠️ Maybe | ✅ |
| Preserves VS Code compat? | ✅ | ✅ | ⚠️ | ✅ |
| Fixes planning suppression? | ❌ | ❌ | ❌ | ✅ |
| Handles "Automatic Mode" text? | ❌ | ❌ | ✅ | ✅ |
| Context preservation? | ✅ Fresh/phase | N/A | ❌ One session | ✅ Fresh/phase |
| Progress persistence? | ✅ | N/A | ❌ | ✅ |
| Retry-safe? | ✅ | ❌ Infinite loop | ⚠️ | ✅ |
| New code | ~200 lines | ~80 lines | ~30 lines | ~400 lines |

### `ResearchPhaseParser` Design

Research documents use the **same emoji-status table format** as plans:

```
| Phase | Name | Status | Scope | Session |
| 1 | Phase Name | ⏳ Not Started | Scope | — |
| 2 | Phase Name | ✅ Complete | Scope | 2026-02-23 |
```

Parser methods: `ParsePhases()`, `FindNextIncompletePhase()`, `HasIncompletePhases()`

### Planning Fix Options

**Option P1:** Add Planning to `InteractivePhases` → enables multi-turn Q&A (risk: many questions, slow)

**Option P2 (Recommended):** Better single-shot prompt with Hugh context and autonomous decision instruction:

```
You are running inside the Hugh CLI — a self-operating development loop.
Create a detailed implementation plan for this feature.
The vision and research documents are attached for context.
Make reasonable implementation decisions based on the research findings.
Document your rationale in the plan's Session Log.
```

### Recommended Implementation Strategy

**Phase 1 — Foundation (must ship together):**
1. Create `ResearchPhaseParser`
2. Add `ExecuteResearchPhasesAsync` to `LoopOrchestrator`
3. Update `DispatchPhaseAsync` to route Research through the loop
4. Clean up Research prompt
5. Add completeness validation safety net
6. Unit tests for `ResearchPhaseParser`

**Phase 2 — Planning prompt cleanup (can ship separately):**
1. Rewrite Planning prompt (standalone + release-aware)
2. Add autonomous decision-making instruction

**Phase 3 — Hardening (follow-up):**
1. Per-sub-phase timeout config
2. Per-sub-phase retry for retryable errors
3. Integration tests

**Key Discoveries:**
- Approach D (Hybrid) recommended: sub-phase loop + validation + prompt cleanup
- `ResearchPhaseParser` needed for any approach; straightforward adaptation of `PlanPhaseParser`
- Prompt override (C) is unreliable — agent's "ONE RULE" outweighs user prompt
- Validation-only (B) detects but doesn't fix — creates infinite retry loops
- Research loop mirrors proven `ExecuteCodingPhasesAsync` pattern
- Planning needs separate fix: remove "Automatic Mode" text, add Hugh-aware prompt
- Estimated scope: ~400 lines total including tests
- No changes needed to agent prompt files in `agents/`

| File | Relevance |
|------|----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | Core file: add loop, update dispatch, rewrite prompts |
| `Hugh/Detection/PlanPhaseParser.cs` | Template for `ResearchPhaseParser` |
| `Hugh/Configuration/HughConfig.cs` | Optional per-sub-phase timeout |
| `Hugh/appsettings.json` | Config for timeouts, InteractivePhases |
| `Hugh/Program.cs` | DI wiring for new parser |

**Gaps:** None  
**Assumptions:** pch-researcher follows its "CONTINUATION" path when given a prompt referencing an existing research document path.

## Overview

The failure where Hugh skipped research phase execution is caused by a **fundamental architectural mismatch** between Hugh's single-shot phase execution model and `pch-researcher`'s multi-session design. Hugh dispatches the Research phase via `ExecuteSinglePhaseAsync` — one prompt in, one response out. But `pch-researcher` is hard-wired to create only a research outline document and STOP on new requests, expecting continuation sessions to execute actual research phases. Hugh interprets the outline document as completed research because: (1) `SignalType.None` maps to `Success = true`, (2) the outline file physically exists on disk when checked by `File.Exists()`, and (3) no content-level validation distinguishes an empty outline from completed research.

The planning phase asking no questions is a secondary failure with the same root pattern: Planning also runs as single-shot (`ExecuteSinglePhaseAsync`), and its prompt contains vestigial "Select Automatic Mode" text that is meaningless to `pch-planner` but may be interpreted by the LLM as a directive to work without questions. Combined with an outline-only research document providing no decision points, the planner generates plans unilaterally.

The Coding phase does NOT have this problem because it uses `ExecuteCodingPhasesAsync` — a dedicated sub-phase loop that re-reads the plan document each iteration and uses `PlanPhaseParser.FindNextIncompletePhase()` to drive progress. **Research has no equivalent loop or parser.**

The recommended fix (Approach D: Hybrid) adds a `ResearchPhaseParser` and `ExecuteResearchPhasesAsync` loop mirroring the coding pattern, plus completeness validation as a safety net and prompt cleanup for both Research and Planning. This preserves dual-environment compatibility (no agent prompt file changes), maintains phased context management (fresh context per sub-phase), and addresses both the research skip and planning suppression bugs.

## Key Findings

1. **Research is dispatched as `ExecuteSinglePhaseAsync`** — single-shot with no sub-phase loop, unlike Coding's `ExecuteCodingPhasesAsync`
2. **pch-researcher correctly creates an outline and STOPs** — the agent is working as designed; Hugh is the component that needs to adapt
3. **`SignalType.None` → `Success = true`** — the outline handoff message ("Research Outline Created") matches zero `SignalClassifier` patterns, causing CopilotService to treat it as successful completion
4. **Post-phase validation is existence-only** — `File.Exists()` passes for outline documents; no content/completeness checking exists
5. **No `ResearchPhaseParser` exists** — unlike `PlanPhaseParser` for coding, nothing can detect incomplete research phases in the document
6. **Planning is also single-shot** — `InteractivePhases` only contains `["Vision"]`; Planning's multi-turn Q&A design is incompatible with `ExecuteSinglePhaseAsync`
7. **"Select Automatic Mode" is vestigial** — pch-planner has no Automatic Mode concept; the text misleads or suppresses question-asking
8. **Vision has an autonomous override, Research does not** — `BuildPromptForPhase` adds Hugh-context instructions for Vision but not Research
9. **The coding sub-phase loop (`ExecuteCodingPhasesAsync` + `PlanPhaseParser`) is the proven pattern** to replicate for Research

## Actionable Conclusions

1. **Implement Approach D (Hybrid fix):** Create `ResearchPhaseParser` + `ExecuteResearchPhasesAsync` loop + completeness validation + prompt cleanup
2. **Create `ResearchPhaseParser`** modeled on `PlanPhaseParser` — research documents use the same emoji-status table format (⏳/✅/🔄/❌)
3. **Add `ExecuteResearchPhasesAsync`** to `LoopOrchestrator` — first invocation creates outline, subsequent iterations find next incomplete phase and invoke pch-researcher with continuation prompt
4. **Update `DispatchPhaseAsync`** to route `LoopPhase.Research` through the new loop instead of `ExecuteSinglePhaseAsync`
5. **Rewrite Research prompt** — remove "Please select Automatic Mode", add Hugh CLI context instructions
6. **Rewrite Planning prompt** — remove "Automatic Mode" text, add autonomous decision-making instruction with rationale logging
7. **Add completeness validation** as safety net after research loop completes
8. **Do NOT modify agent prompt files** in `agents/` — preserve VS Code compatibility
9. **Do NOT use Approach C (prompt override)** — unreliable against agent's "ONE RULE", defeats phased context management
10. **Estimated scope:** ~400 lines total including tests, across `LoopOrchestrator.cs`, new `ResearchPhaseParser.cs`, `Program.cs`, and test files

## Open Questions

- Will pch-researcher reliably follow its "CONTINUATION" decision tree path when Hugh provides a continuation prompt referencing the research document? (Needs integration testing)
- Should Planning be added to `InteractivePhases` for multi-turn Q&A, or is a better single-shot prompt sufficient?
- Should `ResearchPhaseParser` be a separate class or reuse `PlanPhaseParser` with a configuration flag? (Separate class recommended for clarity)
- What per-sub-phase timeout is appropriate for research phases? (Research phases can involve web fetching and extensive codebase search)

## Standards Applied

No organizational standards applicable to this research.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2026-02-23 |
| Status | ✅ Complete |
| Current Phase | ✅ Complete |
| Path | /docs/research/023-hugh-research-phase-skip-failure.md |
