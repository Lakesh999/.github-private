---
id: "029"
type: plan
title: "Coding Phase Infinite Loop Fix — Zero Tasks Bug"
status: ✅ Complete
created: "2025-05-20"
updated: "2025-05-20"
owner: pch-planner
version: v2.0
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-05-20 | pch-planner | Initial plan skeleton |
| v2.0 | 2025-05-20 | pch-planner | All decisions made; holistic review completed; execution plan finalized |
| v2.1 | 2025-05-20 | pch-plan-reviewer | Review complete: corrected guard placement (Task 2.2), fixed Risks table inconsistency, added decisions #5-6 |

## Introduction

Hugh's coding phase enters an infinite loop when `PlanPhaseParser.FindNextIncompletePhase` finds an incomplete phase but `TaskParser.FindNextIncompleteTask` returns null (0 tasks parsed). This plan addresses the root cause (colon-only regex) and adds safety guards to prevent infinite loops from any future parse failure.

Based on research document `/docs/research/027-coding-phase-infinite-loop-zero-tasks.md`.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| 1 | Same-phase repetition threshold | Hardcoded constant = 2 | Safety guard for a situation that should never occur normally; 0-task phases will never self-resolve, so fast abort saves API budget; no config pollution for a non-tunable knob |
| 2 | Scope of fix | A + B + C only (~37 LOC) | Sufficient defense-in-depth: root cause fix + safety net + observability; Strategy D deferred since loop guard already prevents budget waste; minimal change reduces risk |
| 3 | Error behavior when guard triggers | Return PhaseResult failure with actionable error message | Clean failure with format guidance; no document mutation on abort path; user can fix plan and --resume; simplest and safest |
| 4 | Tolerant regex character class | `[:–—\-]` — colon, en-dash, em-dash, hyphen-minus | Matches all 4 separator chars found in existing plans; en-dash and em-dash both confirmed present; no over-engineering with obscure Unicode dashes |
| 5 | Loop guard placement in while loop | After `codingPhaseNumber++` but before `UpdatePhaseAsync` (line 985) | Avoids writing a spurious state checkpoint for a phase about to abort; counter increment is harmless local variable; clean abort before any side-effects |
| 6 | Loop guard attempt semantics | Keep as-is: MaxSamePhaseAttempts = 2, meaning 3 total iterations (initial + 2 repeats) before abort | Internally consistent; provides small safety margin for slow file writes; ~4s worst case is negligible vs infinite loop |

## Holistic Review

### Decision Interactions

- **Tolerant regex + loop guard** work as defense-in-depth: the regex fixes the root cause for known separator variants, while the guard catches any unknown future parse failures (missing task tables, entirely new formats, corrupted documents).
- **Hardcoded threshold (2) + no document mutation** means the system fails fast and cleanly — the user sees the error, fixes the plan, and `--resume`s. No risk of partial writes corrupting the plan.
- **Diagnostic logging + loop guard** together ensure that when the guard triggers, the preceding warning log already explains WHY (0 parseable tasks with format hint), making diagnosis trivial.

### Architectural Considerations

- No new types, no new files, no behavior changes for correctly-formatted plans
- The regex change is backward-compatible (colon is still the first match in `[:–—\-]`)
- The loop guard adds ~15 LOC to a 180-line method — minimal cognitive overhead
- `PlanCreationPhaseParser` alignment ensures the planning sub-phase loop is also protected

### Trade-offs Accepted

- The loop guard uses a hardcoded constant rather than config — accepted because this is an error path, not a tunable behavior
- Strategy D (pre-coding validation) is deferred — accepted because the loop guard + error message achieves the same outcome (no wasted budget, actionable guidance)
- No automatic fallback to phase-level granularity — accepted because masking plan defects is worse than surfacing them

### Risks Acknowledged

- If an LLM produces an entirely novel separator (e.g., `→`), the regex won't match — but the loop guard will abort with a clear error instead of looping infinitely
- The 2-second delay between loop iterations means worst case is ~4 seconds wasted before the guard triggers (2 iterations × 2 seconds)

## Overview

### Problem Statement

The coding phase loop has no progress guard. When `TaskParser` cannot parse tasks (due to non-colon phase header separators), it returns an empty list, which `ExecuteTasksInPhaseAsync` treats as "success with 0 tasks." The outer loop re-reads the unchanged plan, finds the same incomplete phase, and repeats indefinitely — burning API budget at ~30 iterations/minute.

### Objectives

1. Eliminate the infinite loop for all failure modes (format mismatch, missing task tables, etc.)
2. Fix the root cause: make phase header parsing tolerant of dash and em-dash separators
3. Add observability so format mismatches are immediately diagnosable from logs
4. Align `PlanCreationPhaseParser` regex for consistency

### Research Reference

- Research: `/docs/research/027-coding-phase-infinite-loop-zero-tasks.md`
- Recommended strategies: A (tolerant regex) + B (loop-break guard) + C (diagnostic logging)

## Requirements

### Functional

- `TaskParser.PhaseHeaderRegex` must match `### Phase N: Name`, `### Phase N - Name`, and `### Phase N — Name`
- `PlanCreationPhaseParser.PhaseHeaderRegex` must match the same formats
- `ExecuteCodingPhasesAsync` must abort with an actionable error when the same phase is attempted multiple times without progress
- `ExecuteTasksInPhaseAsync` must log a warning when `taskCount == 0`

### Non-Functional

- Total code change ~37 LOC across 3 files
- No new model classes required for the minimum fix
- All existing tests must continue to pass
- New unit tests must cover dash and em-dash separator formats

### Out of Scope

- Pre-coding plan format validation (Strategy D — deferred)
- Automatic fallback from task-level to phase-level granularity
- Progress Summary table auto-updating

## Technical Design

### Architecture

No architectural changes. This is a targeted bug fix across 3 existing files:
- `Hugh/Detection/TaskParser.cs` — regex update
- `Hugh/Detection/PlanCreationPhaseParser.cs` — regex update
- `Hugh/Orchestration/LoopOrchestrator.cs` — loop guard + logging

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: GeneratedRegex attributes
    location: "Hugh/Detection/TaskParser.cs, Hugh/Detection/PlanCreationPhaseParser.cs"
    usage: Update regex pattern string in existing GeneratedRegex attribute
  - pattern: Loop safety guards
    location: "Hugh/Orchestration/LoopOrchestrator.cs (research/planning loops)"
    usage: Follow post-loop safety net pattern from research loop
  - pattern: Structured logging
    location: "Hugh/Orchestration/LoopOrchestrator.cs"
    usage: Use existing _logger.LogWarning/LogError pattern with structured parameters
```

### Data Contracts

No data entities in scope — data contracts not applicable.

### Component Changes

#### Strategy A: Tolerant Parser Regex

**File:** `Hugh/Detection/TaskParser.cs` (line 57)

Change `PhaseHeaderRegex` from:
```csharp
[GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*:\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
```
To:
```csharp
[GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*[:–—\-]\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
```

**File:** `Hugh/Detection/PlanCreationPhaseParser.cs` (line 44)

Same change — identical regex, same update:
```csharp
[GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*[:–—\-]\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
```

#### Strategy B: Same-Phase Repetition Guard

**File:** `Hugh/Orchestration/LoopOrchestrator.cs` — in `ExecuteCodingPhasesAsync` method (around line 958)

Add tracking variables after `var useTaskGranularity`:
```csharp
int? lastAttemptedPhaseNumber = null;
int samePhaseAttempts = 0;
const int MaxSamePhaseAttempts = 2;
```

Inside the `while` loop, after `nextPhase` is found (after the null check), add:
```csharp
if (nextPhase.Number == lastAttemptedPhaseNumber)
{
    samePhaseAttempts++;
    if (samePhaseAttempts >= MaxSamePhaseAttempts)
    {
        _logger.LogError(
            "Phase {PlanPhase} ({PhaseName}) attempted {Count} times without progress. " +
            "Aborting coding loop. Check that phase headers use '### Phase N: Name' format " +
            "and task tables use '| N.M | Task | Files | Criteria |' format.",
            nextPhase.Number, nextPhase.Name, samePhaseAttempts + 1);
        return new PhaseResult
        {
            Success = false,
            ErrorMessage = $"Phase {nextPhase.Number} stuck: attempted {samePhaseAttempts + 1} times " +
                "without progress. Likely cause: task table format mismatch in plan document.",
            Duration = totalDuration
        };
    }
}
else
{
    lastAttemptedPhaseNumber = nextPhase.Number;
    samePhaseAttempts = 0;
}
```

#### Strategy C: Diagnostic Logging

**File:** `Hugh/Orchestration/LoopOrchestrator.cs` — in `ExecuteTasksInPhaseAsync` method (around line 1537)

Replace the existing log message when `nextTask == null` with a conditional:
```csharp
if (nextTask == null)
{
    if (taskCount == 0)
    {
        _logger.LogWarning(
            "Phase {PlanPhase} ({PhaseName}) has 0 parseable tasks. " +
            "Possible format mismatch in plan document. " +
            "Expected header format: '### Phase {PlanPhase}: Name' with task rows '| N.M | ... |'",
            phase.Number, phase.Name, phase.Number);
    }
    else
    {
        _logger.LogInformation(
            "All tasks in Phase {PlanPhase} complete ({TaskCount} tasks executed)",
            phase.Number, taskCount);
    }
    break;
}
```

## Dependencies

- Research document 027 (complete)
- No external dependencies

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Tolerant regex matches unintended content | Low | Low | Character class `[:–—\-]` is specific; tests validate |
| Same-phase threshold too aggressive | Low | Medium | Threshold of 2 aborts quickly; 0-task phases never self-resolve so fast abort is correct |
| Existing tests rely on strict colon-only behavior | Low | Low | No tests validate that dash format FAILS — they only test valid colon format |

## Execution Plan

### Progress Summary

| # | Phase | Status | Tasks |
|---|-------|--------|-------|
| 1 | Tolerant Parser Regex | ✅ Complete | 2/2 |
| 2 | Loop Safety Guard & Diagnostic Logging | ✅ Complete | 3/3 |
| 3 | Unit Tests | ✅ Complete | 4/4 |

### Phase 1: Tolerant Parser Regex

**Status:** ✅ Complete  
**Size:** Small  
**Files to Modify:** 2  
**Prerequisites:** None  
**Entry Point:** `Hugh/Detection/TaskParser.cs`  
**Verification:** `dotnet build` succeeds

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 1.1 | Update `PhaseHeaderRegex` in `TaskParser` to use character class `[:–—\-]` instead of `:` | `Hugh/Detection/TaskParser.cs` | ✅ Complete |
| 1.2 | Update `PhaseHeaderRegex` in `PlanCreationPhaseParser` to use identical character class `[:–—\-]` | `Hugh/Detection/PlanCreationPhaseParser.cs` | ✅ Complete |

### Phase 2: Loop Safety Guard & Diagnostic Logging

**Status:** ✅ Complete  
**Size:** Small  
**Files to Modify:** 1  
**Prerequisites:** Phase 1 complete (build must pass)  
**Entry Point:** `Hugh/Orchestration/LoopOrchestrator.cs`  
**Verification:** `dotnet build` succeeds

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 2.1 | Add same-phase tracking variables (`lastAttemptedPhaseNumber`, `samePhaseAttempts`, `MaxSamePhaseAttempts = 2`) at top of `ExecuteCodingPhasesAsync` after `useTaskGranularity` | `Hugh/Orchestration/LoopOrchestrator.cs` | Variables declared; const = 2 |
| 2.2 | Add same-phase guard logic inside the `while` loop, after `codingPhaseNumber++` (line 984) but before `UpdatePhaseAsync` (line 986). This ensures the guard fires before any state checkpoint is written. | `Hugh/Orchestration/LoopOrchestrator.cs` | When same phase attempted ≥2 times, returns `PhaseResult { Success = false }` with actionable error message mentioning expected format |
| 2.3 | Replace existing log in `ExecuteTasksInPhaseAsync` when `nextTask == null` with conditional: `LogWarning` when `taskCount == 0`, `LogInformation` otherwise | `Hugh/Orchestration/LoopOrchestrator.cs` | Zero-task case logs at Warning level with format hint; normal completion logs at Information level as before |

### Phase 3: Unit Tests

**Status:** ✅ Complete  
**Size:** Small  
**Files to Modify:** 2  
**Prerequisites:** Phase 2 complete (all implementation done)  
**Entry Point:** `Hugh.Tests/TaskParserTests.cs`  
**Verification:** `dotnet test` passes

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 3.1 | Add test: `ParseTasks_WithDashSeparator_ReturnsTasks` — verifies `### Phase 3 - Name` format parses correctly | `Hugh.Tests/TaskParserTests.cs` | Test passes; returns same task list as colon format |
| 3.2 | Add test: `ParseTasks_WithEmDashSeparator_ReturnsTasks` — verifies `### Phase 3 — Name` format parses correctly | `Hugh.Tests/TaskParserTests.cs` | Test passes; returns same task list as colon format |
| 3.3 | Add test: `ParseTasks_WithEnDashSeparator_ReturnsTasks` — verifies `### Phase 3 – Name` format parses correctly | `Hugh.Tests/TaskParserTests.cs` | Test passes; returns same task list as colon format |
| 3.4 | Add test: `ParsePhases_WithDashSeparator_ReturnsPhases` — verifies `PlanCreationPhaseParser` handles dash separators | `Hugh.Tests/PlanCreationPhaseParserTests.cs` | Test passes; returns phases regardless of separator format |

## Standards

No organizational standards applicable to this plan.

## Review Session Log

**Questions Pending:** 0  
**Questions Resolved:** 2  
**Last Updated:** 2025-05-20

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | Loop guard placement ambiguity in Task 2.2 | specificity | Option B: After `codingPhaseNumber++` but before `UpdatePhaseAsync` | Task 2.2 rewritten with exact line references |
| 2 | Guard attempt counting semantics | correctness | Option C: Keep as-is (3 total attempts, MaxSamePhaseAttempts = 2) | Decision #6 added to Planning Session Log |

## Review Summary

**Review Date:** 2025-05-20  
**Reviewer:** pch-plan-reviewer  
**Original Plan Version:** v2.0  
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 2 (Critical: 0, Major: 1, Minor: 1)
- Clarifying Questions Asked: 2
- Sections Updated: Task 2.2, Planning Session Log, Risks table, Version History

### Key Improvements Made
1. Task 2.2 now specifies exact placement: after `codingPhaseNumber++` (line 984), before `UpdatePhaseAsync` (line 986) — avoids spurious state checkpoint writes
2. Fixed Risks table inconsistency (was "threshold of 3", now correctly states threshold of 2)
3. Added decisions #5 and #6 documenting reviewer-resolved placement and semantics choices

### Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 1 | 3 source files + 2 test files |
| New patterns introduced | 1 | None — uses existing GeneratedRegex, logging, PhaseResult patterns |
| External dependencies | 1 | None |
| Migration complexity | 1 | No migrations, fully backward-compatible |
| Test coverage required | 2 | 4 new unit tests (separator variants + parser alignment) |
| **Overall Complexity** | **6/25** | **Low** |

### Remaining Considerations
- Strategy D (pre-coding plan format validation) is intentionally deferred — the loop guard provides equivalent protection
- If novel separators emerge beyond the 4 covered (colon, hyphen, en-dash, em-dash), the loop guard will still abort cleanly with an actionable error

### Sign-off
This plan has been reviewed and is **Ready for Implementation**.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2025-05-20 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2025-05-20 |
| Status | ✅ Ready for Implementation |
| Next Agent | pch-coder |
| Plan Location | /docs/plans/029-coding-phase-infinite-loop-fix.md |
