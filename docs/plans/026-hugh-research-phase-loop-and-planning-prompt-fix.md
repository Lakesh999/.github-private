---
id: "026"
type: plan
title: "Hugh Research Phase Sub-Phase Loop & Planning Prompt Fix"
status: ✅ Complete
created: "2026-02-23"
updated: "2026-02-23"
completed: "2026-02-23"
owner: pch-planner
version: v2.1
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-23 | pch-planner | Initial plan skeleton |
| v1.1 | 2026-02-23 | pch-planner | Added parser design decision (D1) |
| v1.2 | 2026-02-23 | pch-planner | Added continuation prompt strategy (D2) |
| v1.3 | 2026-02-23 | pch-planner | Added constructor change strategy (D3) |
| v1.4 | 2026-02-23 | pch-planner | Added planning autonomous behavior (D4) |
| v1.5 | 2026-02-23 | pch-planner | Added timeout config decision (D5) |
| v2.0 | 2026-02-23 | pch-planner | Holistic review completed; Technical Design and Execution Plan populated |
| v2.1 | 2026-02-23 | pch-plan-reviewer | Review fixes: corrected method names, resolved Risks table contradiction, added missing tasks, clarified test approach |
| v3.0 | 2026-02-23 | pch-coder | Implementation complete — all 4 phases executed successfully |

## Introduction

Hugh's Research phase is dispatched as a single-shot execution (`ExecuteSinglePhaseAsync`), but `pch-researcher` is designed to create only a research outline document and STOP, expecting continuation sessions to execute actual research phases. This causes Hugh to treat an outline-only document as completed research. Additionally, the Planning phase prompt contains vestigial "Automatic Mode" text that suppresses the planner's question-asking behavior. This plan implements the Hybrid fix (Approach D) recommended by [research 023](../research/023-hugh-research-phase-skip-failure.md): a research sub-phase loop mirroring the coding pattern, completeness validation, and prompt cleanup for both Research and Planning.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| 1 | ResearchPhaseParser design approach | A — Separate class | Research tables have different column structure than plan tables; separate class keeps parsers focused, avoids breaking existing PlanPhaseParser tests, matches codebase convention |
| 2 | Research continuation prompt strategy | B — Inline phase scope | Gives agent precise scope text without consuming full context window; reduces risk of agent misidentifying which phase to execute; document still attached as file reference for full context if needed |
| 3 | LoopOrchestrator constructor change | A — Required parameter | Matches existing PlanPhaseParser injection pattern; explicit dependency visible in constructor; straightforward mocking in tests; only Program.cs and direct test instantiations need updating |
| 4 | Planning phase autonomous behavior | A — Better single-shot prompt | Clear Hugh-context prompt matching Vision's autonomous override pattern; planner has complete research to make autonomous decisions; avoids latency of multi-turn Q&A; simplest fix with highest reliability |
| 5 | Research sub-phase timeout config | B — New `ResearchSubPhasePerPhase` | Mirrors existing `CodingPhasePerPhase` pattern; explicit per-sub-phase control; default 15 min avoids excessive total time; consistent with coding loop design |

## Review Session Log

**Questions Pending:** 0  
**Questions Resolved:** 1  
**Last Updated:** 2026-02-23

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | Constructor change strategy contradiction (D3 says required but Risks table says optional with null) | correctness | A — Required parameter (consistent with D3) | Risks table row updated; plan confirmed as required parameter throughout |

## Holistic Review

### Decision Interactions

1. **Separate parser (D1) + required constructor parameter (D3):** These decisions reinforce each other. A separate `ResearchPhaseParser` class naturally becomes a distinct constructor dependency, keeping the DI graph clean and testable. If we'd chosen a shared base class (D1-B), injection would have been more complex (injecting two instances of the same base type).

2. **Inline scope prompt (D2) + separate parser (D1):** The parser must expose the `Scope` field from research table rows for D2 to work. The `ResearchPhase` record includes a `Scope` property specifically to support this. `PlanPhaseParser`'s `PlanPhase` record doesn't have this field, further justifying a separate parser.

3. **Autonomous planning prompt (D4) + research loop fix (D1+D2):** These are complementary. The research loop ensures the planner receives *complete* research findings (not just an outline), which gives the planner enough context to make autonomous decisions without asking questions. If research were still broken, the autonomous planning prompt would produce low-quality plans due to missing context.

4. **Per-sub-phase timeout (D5) + `GetPhaseTimeout` update:** Changing `GetPhaseTimeout(Research)` to return `ResearchSubPhasePerPhase` (15 min) affects ALL `PhaseExecutor.ExecuteAsync(Research, ...)` calls — both the first outline-creation call and subsequent sub-phase calls. 15 minutes is sufficient for outline creation (which is fast) and individual research sub-phases. The old 30-min `ResearchPhase` timeout becomes unused but can remain in config for backward compatibility.

### Architectural Considerations

- **State tracking reuse:** `CurrentCodingSubPhase` on `LoopState` is reused for research sub-phase tracking. This is acceptable because Research and Coding never execute simultaneously. The field name is slightly misleading but renaming it would break serialized state backward compatibility. A comment should be added to clarify the field is used for both.
- **No agent prompt changes:** All fixes are in Hugh's orchestration layer. Agent prompts in `agents/` are untouched, preserving VS Code Copilot chat compatibility.
- **Timeout backwards compatibility:** The existing `ResearchPhase` timeout value stays in config. If someone has customized it, the new `ResearchSubPhasePerPhase` will use its own default unless also customized.

### Trade-offs Accepted

- **Duplicated emoji parsing:** `ResearchPhaseParser` duplicates the `ParseStatusEmoji` helper from `PlanPhaseParser`. This is ~10 lines of code and acceptable for class independence.
- **No overall research time cap:** Like the coding loop, the research loop has no cumulative timeout. If a research document has many sub-phases, total time is N × 15 min. This matches the coding loop's design.
- **Planning produces plans without human input:** The autonomous planning prompt deliberately suppresses questions. If the research is thorough, this produces good plans. If research is thin, plan quality may suffer. The plan reviewer (`pch-plan-reviewer`) serves as the quality gate.

### Risks Acknowledged

- **pch-researcher CONTINUATION path reliability:** The biggest open risk. If the agent doesn't follow its CONTINUATION decision tree when given a continuation prompt, the research loop will still succeed (producing partial research) but quality may vary. Integration testing is the mitigation.
- **Timeout change for outline creation:** Reducing Research timeout from 30 min to 15 min also applies to the first outline-creation call. Outline creation is typically fast (<5 min), so 15 min provides ample headroom.

## Overview

### Problem Statement

1. **Research phase skips execution:** Hugh sends a single prompt to `pch-researcher`, which creates an outline and STOPs. Hugh interprets the outline as completed research because `SignalType.None` → `Success = true` and `File.Exists()` passes.
2. **Planning phase asks no questions:** The prompt says "Select Automatic Mode" (meaningless to `pch-planner`) and runs single-shot, causing the planner to generate plans without human input on key decisions.

### Objectives

- Execute all research sub-phases before advancing to Planning
- Create `ResearchPhaseParser` to detect incomplete research phases in the document
- Add `ExecuteResearchPhasesAsync` loop mirroring `ExecuteCodingPhasesAsync`
- Add completeness validation as a safety net after the research loop
- Clean up Research and Planning prompts to remove vestigial text and add Hugh-context instructions
- Preserve dual-environment compatibility (no agent prompt file changes)

## Requirements

### Functional

- FR-1: Research phase must execute all sub-phases in the research outline before marking Research as complete
- FR-2: Each research sub-phase must execute in a fresh SDK session (fresh context window)
- FR-3: If any research sub-phase fails, the Research phase must report failure (not silently skip)
- FR-4: After the research loop completes, a completeness validation must verify no `⏳ Not Started` phases remain
- FR-5: Planning prompt must instruct the LLM to make autonomous decisions and log rationale
- FR-6: Research prompt must provide proper Hugh CLI context (like Vision's autonomous override)

### Non-Functional

- NFR-1: No changes to agent prompt files in `agents/` (preserve VS Code Copilot chat compatibility)
- NFR-2: Research sub-phase loop must support resume via existing state persistence
- NFR-3: Per-sub-phase timeout must be configurable
- NFR-4: Research sub-phase loop must include a rate-limiting delay between iterations (like coding)

### Out of Scope

- Adding Planning to `InteractivePhases` for multi-turn Q&A
- Web fetching or external API integration for research
- Changes to `pch-researcher` agent prompt design
- Per-sub-phase retry for retryable errors (follow-up hardening)

## Technical Design

### Architecture

The fix adds a research sub-phase loop to `LoopOrchestrator` that mirrors the existing coding sub-phase loop. The key architectural change is routing `LoopPhase.Research` through `ExecuteResearchPhasesAsync` instead of `ExecuteSinglePhaseAsync` in `DispatchPhaseAsync`.

**Control flow (Research phase):**

```
DispatchPhaseAsync(Research)
  → ExecuteResearchPhasesAsync(researchDocPath, ct)
      → Iteration 0 (outline creation):
          PhaseExecutor.ExecuteAsync(Research, outlinePrompt, [visionDoc])
          → pch-researcher creates outline, returns outline path
          → ResearchPhaseParser.FindNextIncompletePhase(outlineContent) → Phase 1
      → Iteration 1..N (sub-phase execution):
          Read research doc from disk
          ResearchPhaseParser.FindNextIncompletePhase(content)
          If null → all phases complete → break
          Build continuation prompt with phase scope inlined
          PhaseExecutor.ExecuteAsync(Research, continuationPrompt, [researchDoc, visionDoc])
          Delay 2s between iterations
      → Post-loop: ResearchPhaseParser.HasIncompletePhases(content)
          If true → return failure (safety net)
          If false → return success with research doc path
```

**Modified dispatch routing:**

```
DispatchPhaseAsync
  ├── LoopPhase.Coding → ExecuteCodingPhasesAsync (existing)
  ├── LoopPhase.Research → ExecuteResearchPhasesAsync (NEW)
  ├── IsInteractivePhase(phase) → ExecuteInteractivePhaseAsync (existing)
  └── default → ExecuteSinglePhaseAsync (existing)
```

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: Sub-Phase Loop
    location: "Hugh/Orchestration/LoopOrchestrator.cs (ExecuteCodingPhasesAsync)"
    usage: Template for ExecuteResearchPhasesAsync — read document, find next incomplete phase, build prompt, execute, repeat
  - pattern: Phase Table Parser
    location: "Hugh/Detection/PlanPhaseParser.cs"
    usage: Template for ResearchPhaseParser — same emoji-status table format, same ParsePhases/FindNextIncompletePhase API
  - pattern: Manual DI Wiring
    location: "Hugh/Program.cs (BuildOrchestrator)"
    usage: Add ResearchPhaseParser instantiation and inject into LoopOrchestrator
  - pattern: Phase Prompt Building
    location: "Hugh/Orchestration/LoopOrchestrator.cs (BuildPromptForPhase)"
    usage: Rewrite Research and Planning prompts; add Hugh CLI context like the release-aware Vision prompt
  - pattern: State Checkpoint Updates
    location: "Hugh/Orchestration/LoopOrchestrator.cs (UpdatePhaseAsync)"
    usage: Track current research sub-phase number during loop execution
```

### Data Contracts

No data entities in scope — data contracts not applicable.

### Component Design

#### 1. `ResearchPhaseParser` (New — `Hugh/Detection/ResearchPhaseParser.cs`)

Parses the `## Research Phases` table in research documents to detect phase status.

**Research table format:**
```
| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Phase Name | ⏳ Not Started | Scope text | — |
| 2 | Phase Name | ✅ Complete | Scope text | 2026-02-23 |
```

**Record type:**
```csharp
public record ResearchPhase
{
    public int Number { get; init; }
    public string Name { get; init; } = "";
    public string StatusEmoji { get; init; } = "";
    public PhaseProgressStatus Status { get; init; }
    public string Scope { get; init; } = "";
}
```

Note: Reuses the existing `PhaseProgressStatus` enum from `PlanPhaseParser.cs` (already has `NotStarted`, `InProgress`, `Complete`, `Blocked`).

**Regex pattern:**
```csharp
// Matches: | 1 | Phase Name | ✅ Complete | Scope text | Session |
@"\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅|🔄|⏳|❌)\s*([^|]*)\|\s*([^|]*)\|\s*([^|]*)\|"
```

Groups: (1) phase number, (2) name, (3) status emoji, (4) status text (ignored), (5) scope, (6) session.

**Public API:**
```csharp
public partial class ResearchPhaseParser
{
    public IReadOnlyList<ResearchPhase> ParsePhases(string researchContent);
    public ResearchPhase? FindNextIncompletePhase(string researchContent);
    public bool HasIncompletePhases(string researchContent);
    public IReadOnlyList<int> GetIncompletePhaseNumbers(string researchContent);
}
```

#### 2. `ExecuteResearchPhasesAsync` (New method — `Hugh/Orchestration/LoopOrchestrator.cs`)

New private method in `LoopOrchestrator`, ~80 lines, modeled on `ExecuteCodingPhasesAsync`.

**Key differences from `ExecuteCodingPhasesAsync`:**
- First iteration uses the existing single-shot Research prompt (creates outline)
- Subsequent iterations build a continuation prompt with inlined phase scope
- Uses `ResearchPhaseParser.FindNextIncompletePhase()` instead of `PlanPhaseParser`
- Documents attached: research doc (after first iteration); vision doc also attached for sub-phase iterations to provide broader context
- No build verification between sub-phases (research doesn't produce code)
- Post-loop completeness validation as safety net

**Continuation prompt template:**
```
You are running inside the Hugh CLI — a self-operating development loop.
Continue the research document at `{researchDocPath}`.
Execute Phase {N}: {phaseName}.

Phase scope:
{scopeText}

The research document is attached for context.
Execute ONLY this phase, then update the research document with your findings.
Do not proceed to subsequent phases.
```

**State tracking:**
Uses existing `_stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress, subPhaseNumber, ct)` to track current research sub-phase, reusing the `CurrentCodingSubPhase` field (which is phase-agnostic in practice — it's an `int?` on `LoopState`).

#### 3. `DispatchPhaseAsync` Update (`Hugh/Orchestration/LoopOrchestrator.cs`)

Add Research routing before the `IsInteractivePhase` check:

```csharp
if (phase == LoopPhase.Research)
{
    var visionPath = state.Documents.Vision;
    return await ExecuteResearchPhasesAsync(visionPath, ct);
}
```

#### 4. `BuildPromptForPhase` Updates (`Hugh/Orchestration/LoopOrchestrator.cs`)

**Research prompt (standalone):**
```
You are running inside the Hugh CLI — a self-operating development loop.
Research the technical approach for implementing this feature.
The vision document is attached for context.
Create a research outline document with phased research topics.
Do not execute any research phases yet — create only the outline.
```

**Research prompt (release-aware):**
```
You are running inside the Hugh CLI — a self-operating development loop.
Research the technical approach for implementing Release {N}: {Name}.
Release Scope: {scope}
{completedInfo}
The clarified solution vision is attached for context.
Create a research outline document focused on this release's scope.
Do not execute any research phases yet — create only the outline.
```

**Planning prompt (standalone):**
```
You are running inside the Hugh CLI — a self-operating development loop.
Hugh orchestrates the full pipeline: Vision → Research → Planning → Review → Coding.
Create a detailed implementation plan for this feature.
The vision and research documents are attached for context.
Make reasonable implementation decisions based on the research findings.
Document your rationale for each decision in the plan's Planning Session Log.
Do not ask clarifying questions — decide autonomously based on available context.
```

**Planning prompt (release-aware):**
```
You are running inside the Hugh CLI — a self-operating development loop.
Create a detailed implementation plan for Release {N}: {Name}.
Release Scope: {scope}
{completedInfo}
The vision and research documents are attached for context.
Make reasonable implementation decisions based on the research findings.
Document your rationale in the plan's Planning Session Log.
Plan ONLY the work for this release. Do not plan items from other releases.
```

#### 5. `TimeoutConfig` Update (`Hugh/Configuration/HughConfig.cs`)

New property:
```csharp
public TimeSpan ResearchSubPhasePerPhase { get; set; } = TimeSpan.FromMinutes(15);
```

#### 6. `PhaseExecutor.GetPhaseTimeout` Update

The research sub-phase timeout needs to be used when `ExecuteResearchPhasesAsync` calls `PhaseExecutor.ExecuteAsync(Research, ...)` for continuation sub-phases. Two approaches:
- **Option A:** Pass a timeout override parameter to `ExecuteAsync`.
- **Option B:** Have `ExecuteResearchPhasesAsync` set the `LoopPhase` to a sentinel or use the existing timeout since `PhaseExecutor` already maps `Research` → `ResearchPhase` timeout.

For sub-phases, the existing `ResearchPhase` timeout (30 min) is adequate as a per-invocation timeout since each `ExecuteAsync` call is one sub-phase. The new `ResearchSubPhasePerPhase` (15 min) will be applied inside `ExecuteResearchPhasesAsync` by creating a per-sub-phase `CancellationTokenSource` that wraps the call to `PhaseExecutor.ExecuteAsync`. This mirrors how `PhaseExecutor` internally creates a `CancellationTokenSource.CreateLinkedTokenSource` with `CancelAfter`.

**However**, since `PhaseExecutor.ExecuteAsync` already creates its own timeout internally using `GetPhaseTimeout(phase)`, the simplest fix is to update `GetPhaseTimeout` to return `ResearchSubPhasePerPhase` when phase is `Research` — but this changes the timeout for the first outline-creation call too. Since 15 min is sufficient for both outline creation and sub-phase execution, this is acceptable.

**Decision:** Update `GetPhaseTimeout` to map `LoopPhase.Research → ResearchSubPhasePerPhase` (15 min default). The old `ResearchPhase` timeout (30 min) becomes unused but is retained in `TimeoutConfig` and `appsettings.json` for backward compatibility. A code comment should be added to `ResearchPhase` noting it is superseded by `ResearchSubPhasePerPhase`.

#### 7. `LoopOrchestrator` Constructor Update

Add `ResearchPhaseParser` as a required parameter:

```csharp
public LoopOrchestrator(
    PhaseExecutor phaseExecutor,
    LoopStateManager stateManager,
    PlanPhaseParser planPhaseParser,
    ResearchPhaseParser researchPhaseParser,  // NEW
    IOptions<HughConfig> options,
    ILogger<LoopOrchestrator> logger)
```

Private field: `private readonly ResearchPhaseParser _researchPhaseParser;`

#### 8. DI Wiring Updates

**`Hugh/Program.cs` — `BuildOrchestrator`:**
```csharp
var researchPhaseParser = new ResearchPhaseParser();
// ...
var orchestrator = new LoopOrchestrator(
    phaseExecutor, stateManager, planPhaseParser, researchPhaseParser, configOptions, ...);
```

**`Hugh/Orchestration/SolutionOrchestrator.cs` — `BuildReleaseOrchestrator`:**
Same pattern — add `new ResearchPhaseParser()` and pass to `LoopOrchestrator`.

#### 9. `appsettings.json` Update

Add under `Timeouts`:
```json
"ResearchSubPhasePerPhase": "00:15:00"
```

## Dependencies

- Research document 023: [023-hugh-research-phase-skip-failure.md](../research/023-hugh-research-phase-skip-failure.md) — root cause analysis and fix recommendations
- `PlanPhaseParser` at `Hugh/Detection/PlanPhaseParser.cs` — template for `ResearchPhaseParser`
- `ExecuteCodingPhasesAsync` at `Hugh/Orchestration/LoopOrchestrator.cs` — template for research loop
- Existing test infrastructure in `Hugh.Tests/` — xUnit + NSubstitute

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| pch-researcher doesn't follow CONTINUATION path when given continuation prompt | Medium | High | Integration test; fallback to retry with clearer prompt |
| Research sub-phases take longer than timeout | Low | Medium | Configurable per-sub-phase timeout; default 15 min |
| Research document table format varies across agent versions | Low | Medium | ResearchPhaseParser uses same flexible regex as PlanPhaseParser |
| LoopOrchestrator constructor change breaks existing tests | Low | Low | Required parameter (per D3); all 4 test subclass constructors updated in Phase 4 steps 4.2–4.4 |

## Execution Plan

### Progress Summary

| Phase | Name | Status | Tasks |
|-------|------|--------|-------|
| 1 | ResearchPhaseParser & Config | ✅ Complete | 7/7 |
| 2 | Research Sub-Phase Loop | ✅ Complete | 6/6 |
| 3 | Prompt Cleanup & Planning Fix | ✅ Complete | 4/4 |
| 4 | Test Updates & New Tests | ✅ Complete | 6/6 |

### Phase 1: ResearchPhaseParser & Config

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 4
**Prerequisites:** None
**Entry Point:** `Hugh/Detection/PlanPhaseParser.cs` (read for reference pattern)
**Verification:** `dotnet build` succeeds; new parser compiles

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 1.1 | Create `ResearchPhase` record and `ResearchPhaseParser` class | `Hugh/Detection/ResearchPhaseParser.cs` | New file with `ResearchPhase` record (Number, Name, StatusEmoji, Status, Scope), `ResearchPhaseParser` class with `[GeneratedRegex]` patterns matching research table format `\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅\|🔄\|⏳\|❌)\s*([^|]*)\|\s*([^|]*)\|\s*([^|]*)\|`. Public methods: `ParsePhases(string)`, `FindNextIncompletePhase(string)`, `HasIncompletePhases(string)`, `GetIncompletePhaseNumbers(string)`. Reuse existing `PhaseProgressStatus` enum. Follow `PlanPhaseParser` patterns exactly for emoji parsing. |
| 1.2 | Add `ResearchSubPhasePerPhase` to `TimeoutConfig` | `Hugh/Configuration/HughConfig.cs` | New property `public TimeSpan ResearchSubPhasePerPhase { get; set; } = TimeSpan.FromMinutes(15);` in `TimeoutConfig` class, with XML doc comment. |
| 1.3 | Add timeout to `appsettings.json` | `Hugh/appsettings.json` | Add `"ResearchSubPhasePerPhase": "00:15:00"` under `Timeouts` section. |
| 1.4 | Update `PhaseExecutor.GetPhaseTimeout` for Research | `Hugh/Orchestration/PhaseExecutor.cs` | Change `LoopPhase.Research => _config.Timeouts.ResearchPhase` to `LoopPhase.Research => _config.Timeouts.ResearchSubPhasePerPhase` in the `GetPhaseTimeout` switch expression. |
| 1.5 | Add deprecation comment to `ResearchPhase` timeout property | `Hugh/Configuration/HughConfig.cs` | Add XML doc comment to `TimeoutConfig.ResearchPhase` stating: "Superseded by ResearchSubPhasePerPhase. Retained for backward config compatibility." |
| 1.6 | Add clarifying comment to `CurrentCodingSubPhase` field | `Hugh/Models/LoopState.cs` | Update the comment on `CurrentCodingSubPhase` from `// For coding phases` to XML doc: "Tracks the current sub-phase number during coding or research phase loops. Named for coding but reused for research since both phases never execute simultaneously." |
| 1.7 | Verify build compiles | — | Run `dotnet build` and confirm no errors. The new parser is not yet wired into orchestration. |

### Phase 2: Research Sub-Phase Loop

**Status:** ✅ Complete
**Size:** Medium
**Files to Modify:** 3
**Prerequisites:** Phase 1 complete; `ResearchPhaseParser` class exists and compiles
**Entry Point:** `Hugh/Orchestration/LoopOrchestrator.cs`
**Verification:** `dotnet build` succeeds; `LoopOrchestrator` has new constructor parameter and `ExecuteResearchPhasesAsync` method

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 2.1 | Add `ResearchPhaseParser` field and constructor parameter to `LoopOrchestrator` | `Hugh/Orchestration/LoopOrchestrator.cs` | Add `private readonly ResearchPhaseParser _researchPhaseParser;` field. Add `ResearchPhaseParser researchPhaseParser` parameter after `planPhaseParser` in constructor. Assign in constructor body. Update `using Hugh.Detection;` if not already present. |
| 2.2 | Implement `ExecuteResearchPhasesAsync` method | `Hugh/Orchestration/LoopOrchestrator.cs` | New `private async Task<PhaseResult> ExecuteResearchPhasesAsync(string? visionPath, CancellationToken ct)` method. **First iteration:** Build outline prompt via existing `BuildPromptForPhase(Research, state)`, execute via `_phaseExecutor.ExecuteAsync`, extract research doc path from result. **Loop iterations:** Read research doc from disk, `_researchPhaseParser.FindNextIncompletePhase()`, if null break, build continuation prompt with inlined scope, attach both research doc and vision doc (if available), `_stateManager.UpdatePhaseAsync(Research, InProgress, subPhaseNumber, ct)`, execute, check success, 2s delay. **Post-loop:** Read final content, `_researchPhaseParser.HasIncompletePhases()` — if true, return failure. Return `PhaseResult { Success = true, OutputDocumentPath = researchDocPath }`. |
| 2.3 | Update `DispatchPhaseAsync` to route Research through new loop | `Hugh/Orchestration/LoopOrchestrator.cs` | Add `if (phase == LoopPhase.Research)` block before the `IsInteractivePhase` check, calling `ExecuteResearchPhasesAsync(state.Documents.Vision, ct)`. |
| 2.4 | Update DI wiring in `Program.cs` | `Hugh/Program.cs` | Add `var researchPhaseParser = new ResearchPhaseParser();` after `planPhaseParser`. Pass `researchPhaseParser` to `new LoopOrchestrator(...)`. |
| 2.5 | Update DI wiring in `SolutionOrchestrator.BuildReleaseOrchestrator` | `Hugh/Orchestration/SolutionOrchestrator.cs` | Add `var researchPhaseParser = new ResearchPhaseParser();` before `return new LoopOrchestrator(...)` in the `BuildReleaseOrchestrator` method (line ~601). Pass it to constructor. |
| 2.6 | Verify build compiles | — | Run `dotnet build` and confirm no errors. |

### Phase 3: Prompt Cleanup & Planning Fix

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 2 complete
**Entry Point:** `Hugh/Orchestration/LoopOrchestrator.cs` — `BuildPromptForPhase` and `BuildReleaseAwarePrompt`
**Verification:** `dotnet build` succeeds; prompts no longer contain "Automatic Mode" text

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 3.1 | Rewrite standalone Research prompt in `BuildPromptForPhase` | `Hugh/Orchestration/LoopOrchestrator.cs` | Replace the Research case in `BuildPromptForPhase` switch with Hugh CLI context prompt. Must include "create only the outline" instruction. Must NOT contain "Automatic Mode" or "select Automatic Mode". |
| 3.2 | Rewrite standalone Planning prompt in `BuildPromptForPhase` | `Hugh/Orchestration/LoopOrchestrator.cs` | Replace the Planning case with Hugh CLI context prompt. Must include "Make reasonable implementation decisions", "Document your rationale in the plan's Planning Session Log", "Do not ask clarifying questions". Must NOT contain "Automatic Mode". |
| 3.3 | Rewrite release-aware Research prompt in `BuildReleaseAwarePrompt` | `Hugh/Orchestration/LoopOrchestrator.cs` | Replace the Research case with Hugh CLI context + release scope + "create only the outline" instruction. Remove "Please select Automatic Mode and execute all research phases". |
| 3.4 | Rewrite release-aware Planning prompt in `BuildReleaseAwarePrompt` | `Hugh/Orchestration/LoopOrchestrator.cs` | Replace the Planning case with Hugh CLI context + release scope + autonomous decision-making instruction. Remove "Please select Automatic Mode and create the complete plan". |

### Phase 4: Test Updates & New Tests

**Status:** ✅ Complete
**Size:** Medium
**Files to Modify:** 5
**Prerequisites:** Phase 3 complete; all production code compiles
**Entry Point:** `Hugh.Tests/` directory
**Verification:** `dotnet test` passes with all existing + new tests green

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 4.1 | Create `ResearchPhaseParserTests.cs` | `Hugh.Tests/Detection/ResearchPhaseParserTests.cs` | New test file. Tests: (a) `ParsePhases_WithValidResearchTable_ReturnsAllPhases` — parses a 5-phase research table with mixed statuses. (b) `FindNextIncompletePhase_WithMixedStatuses_ReturnsFirstIncomplete` — returns first ⏳ phase when ✅ and ⏳ phases exist. (c) `FindNextIncompletePhase_AllComplete_ReturnsNull`. (d) `HasIncompletePhases_WithNotStartedPhases_ReturnsTrue`. (e) `HasIncompletePhases_AllComplete_ReturnsFalse`. (f) `ParsePhases_EmptyContent_ReturnsEmpty`. (g) `FindNextIncompletePhase_InProgressPhase_ReturnsInProgress` — ⏳ phases exist but a 🔄 phase should be returned first. (h) `ParsePhases_ExtractsScopeText` — verifies Scope column is captured. |
| 4.2 | Update `SubPhaseBuildVerificationTests.cs` constructor call | `Hugh.Tests/Orchestration/SubPhaseBuildVerificationTests.cs` | Update `BuildVerificationTestOrchestrator` constructor and its `base(...)` call to include `ResearchPhaseParser` parameter. Pass `new ResearchPhaseParser()` in test setup. |
| 4.3 | Update `PhaseRetryTests.cs` constructor call | `Hugh.Tests/Orchestration/PhaseRetryTests.cs` | Update `TestableLoopOrchestrator` constructor and its `base(...)` call to include `ResearchPhaseParser` parameter. Pass `new ResearchPhaseParser()` in test setup. |
| 4.4 | Update `PipelineTests.cs` constructor calls | `Hugh.Tests/E2E/PipelineTests.cs` | Update both `TestableLoopOrchestrator` and `HybridLoopOrchestrator` constructors and their `base(...)` calls to include `ResearchPhaseParser` parameter. Pass `new ResearchPhaseParser()` in test setup. |
| 4.5 | Create research loop integration test | `Hugh.Tests/Orchestration/ResearchLoopTests.cs` | New test file using a `ResearchTestOrchestrator` subclass (like `TestableLoopOrchestrator` in `PhaseRetryTests.cs`) that overrides `DispatchPhaseAsync` to control `PhaseResult` returns and captures dispatch calls. Tests invoke through `RunAsync` to exercise the full dispatch chain. At minimum: (a) Test that Research phase creates an outline then iterates sub-phases — provide a temp research file with ⏳ phases that the mock updates to ✅ on each dispatch, verify dispatch is called N+1 times (1 outline + N sub-phases). (b) Test completeness validation — create research doc where one phase stays ⏳ after all iterations, verify the run returns failure. |
| 4.6 | Run full test suite and verify | — | Run `dotnet test` and confirm all existing tests pass plus new tests pass. Zero regressions. |

## Standards

No organizational standards applicable to this plan.

## Review Summary

**Review Date:** 2026-02-23
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 7 (Critical: 1, Major: 2, Minor: 4)
- Clarifying Questions Asked: 1
- Sections Updated: Risks table, Codebase Patterns, Component Design §2/§6/§8, Execution Plan (Phases 1 & 2 & 4), Version History

### Key Improvements Made
1. Fixed wrong method name `BuildPerReleaseOrchestrator` → `BuildReleaseOrchestrator` (Critical)
2. Resolved Risks table contradiction: mitigation now says "Required parameter" consistent with D3
3. Fixed timeout default from "30 min" to "15 min" in Risks table to match D5 decision
4. Added missing tasks for `CurrentCodingSubPhase` clarifying comment (Step 1.6) and `ResearchPhase` deprecation comment (Step 1.5)
5. Clarified research sub-phase continuation attaches both research doc AND vision doc
6. Clarified test approach in Step 4.5 — uses test subclass via `RunAsync`, not direct private method invocation
7. Fixed misleading "Vision's autonomous override pattern" reference in Codebase Patterns

### Remaining Considerations
- pch-researcher CONTINUATION path reliability is the biggest open risk — integration testing is the primary mitigation
- The `ResearchPhase` timeout property (30 min) is now dead config; retained with deprecation comment for backward compatibility
- Phase 1 task count increased from 5 to 7 with the two new comment tasks

### Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | ~10 files across production + tests |
| New patterns introduced | 1 | Research loop mirrors existing coding loop |
| External dependencies | 1 | No new externals |
| Migration complexity | 1 | No data migration; backward-compatible config |
| Test coverage required | 3 | New parser tests + loop integration test + 4 constructor updates |
| **Overall Complexity** | **9/25** | **Low** |

### Sign-off
This plan has been reviewed and is **Ready for Implementation**

## Implementation Notes

### Plan Completion

**All phases completed:** 2026-02-23
**Total tasks completed:** 23
**Total files modified/created:** 10

### Phase 1 — ResearchPhaseParser & Config

**Completed:** 2026-02-23
**Execution Mode:** Automatic (Subagent)

**Files Created:**
- `Hugh/Detection/ResearchPhaseParser.cs`

**Files Modified:**
- `Hugh/Configuration/HughConfig.cs`
- `Hugh/appsettings.json`
- `Hugh/Orchestration/PhaseExecutor.cs`
- `Hugh/Models/LoopState.cs`

**Deviations from Plan:** None

### Phase 2 — Research Sub-Phase Loop

**Completed:** 2026-02-23
**Execution Mode:** Automatic (Subagent)

**Files Modified:**
- `Hugh/Orchestration/LoopOrchestrator.cs`
- `Hugh/Program.cs`
- `Hugh/Orchestration/SolutionOrchestrator.cs`
- `Hugh.Tests/E2E/PipelineTests.cs`
- `Hugh.Tests/Orchestration/SubPhaseBuildVerificationTests.cs`
- `Hugh.Tests/Orchestration/PhaseRetryTests.cs`

**Deviations from Plan:** Test files (3) required constructor updates for the new `ResearchPhaseParser` parameter — expected downstream of the constructor signature change. Steps 4.2-4.4 were completed early as part of Phase 2 to keep the build green.

### Phase 3 — Prompt Cleanup & Planning Fix

**Completed:** 2026-02-23
**Execution Mode:** Automatic (Subagent)

**Files Modified:**
- `Hugh/Orchestration/LoopOrchestrator.cs`

**Deviations from Plan:** None

### Phase 4 — Test Updates & New Tests

**Completed:** 2026-02-23
**Execution Mode:** Automatic (Subagent)

**Files Created:**
- `Hugh.Tests/Detection/ResearchPhaseParserTests.cs` (13 tests)
- `Hugh.Tests/Orchestration/ResearchLoopTests.cs` (5 tests)

**Deviations from Plan:** Steps 4.2-4.4 were already completed in Phase 2. Three additional `ResearchPhaseParser` tests added beyond the 8 specified for better coverage. Total new tests: 18. Full suite: 797 tests passing.

**Code Review:** Clean — no issues found.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2026-02-23 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2026-02-23 |
| Status | ✅ Complete |
| Next Agent | — |
| Plan Location | /docs/plans/026-hugh-research-phase-loop-and-planning-prompt-fix.md |
