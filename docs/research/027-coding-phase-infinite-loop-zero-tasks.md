---
id: "027"
type: research
title: "Coding Phase Infinite Loop — '0 Tasks Executed' Bug"
status: ✅ Complete
created: "2025-05-20"
current_phase: "4 of 4"
---

## Introduction

Hugh's coding phase enters an infinite loop when `PlanPhaseParser.FindNextIncompletePhase` keeps finding an incomplete phase but `TaskParser.FindNextIncompleteTask` immediately returns null (0 tasks parsed). The planning phase successfully produces tasks in the plan document, but the coding orchestrator's `TaskParser` cannot find them — causing the loop to report "0 tasks executed" and repeat indefinitely without making progress.

## Objectives

- Identify the exact code path that produces the "0 tasks executed" infinite loop
- Determine why `TaskParser` fails to parse tasks that the planning phase generated
- Document the format mismatch between what the planner produces and what `TaskParser` expects
- Identify how `PlanPhaseParser` and `TaskParser` interact and where the disconnect occurs
- Propose targeted fixes to break the infinite loop and/or align the parsers

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Coding Loop Control Flow | ✅ Complete | Trace `ExecuteCodingPhasesAsync` and `ExecuteTasksInPhaseAsync` to document the exact infinite loop path; identify the exit conditions and where "0 tasks executed" is logged | 2025-05-20 |
| 2 | TaskParser Regex & Format Requirements | ✅ Complete | Analyze `TaskParser.ExtractPhaseSection` and `TaskRowRegex`; document the exact Markdown format expected (header format `### Phase N:`, table row regex `\|\s*(\d+\.\d+)\s*\|`); identify what format variations would cause parse failure | 2025-05-20 |
| 3 | PlanPhaseParser vs TaskParser Format Mismatch | ✅ Complete | Compare what `PlanPhaseParser` uses to detect incomplete phases (Progress Summary table) vs what `TaskParser` uses to find tasks (phase section headers + task rows); identify the specific scenario where one succeeds and the other fails | 2025-05-20 |
| 4 | Fix Strategies & Loop Safety | ✅ Complete | Document concrete fix options: (a) align parsers, (b) add loop-break safety when 0 tasks found, (c) add diagnostic logging, (d) validate plan format after planning phase completes; assess trade-offs | 2025-05-20 |

## Phase 1: Coding Loop Control Flow

**Status:** ✅ Complete  
**Session:** 2025-05-20

### Executive Summary

The infinite loop occurs because `ExecuteTasksInPhaseAsync` returns `Success = true` even when **zero tasks are found**, and the outer loop (`ExecuteCodingPhasesAsync`) has **no check for progress** — it simply loops back, re-reads the plan, finds the same incomplete phase again, and repeats infinitely.

### Entry Point

The coding phase is dispatched at line 438 of `LoopOrchestrator.cs`:

```csharp
return await ExecuteCodingPhasesAsync(planPath, ct);
```

### Outer Loop: `ExecuteCodingPhasesAsync` (line 948)

This method contains a `while (!ct.IsCancellationRequested)` loop (line 959) with the following structure:

```csharp
private async Task<PhaseResult> ExecuteCodingPhasesAsync(string planPath, CancellationToken ct)
{
    var codingPhaseNumber = 0;
    var totalDuration = TimeSpan.Zero;
    var useTaskGranularity = string.Equals(
        _config.Coding.Granularity, "task", StringComparison.OrdinalIgnoreCase);

    while (!ct.IsCancellationRequested)
    {
        // Re-read plan each iteration
        var planContent = await File.ReadAllTextAsync(planPath, ct);
        
        // Find next incomplete phase
        var nextPhase = _planPhaseParser.FindNextIncompletePhase(planContent);
        if (nextPhase == null)
        {
            _logger.LogInformation("All coding phases complete");
            break;  // ← ONLY exit condition (all phases ✅)
        }

        codingPhaseNumber++;

        if (useTaskGranularity)
        {
            var taskResult = await ExecuteTasksInPhaseAsync(
                planPath, nextPhase, codingPhaseNumber, ct);
            totalDuration += taskResult.Duration;

            if (!taskResult.Success)
                return taskResult;  // ← Exit on failure
            
            // ⚠️ NO CHECK: Was any progress made? Were any tasks found?
            // Immediately loops back to while()
        }
        else { /* phase-level granularity */ }

        // Build verification, git commit, delay...
        await Task.Delay(TimeSpan.FromSeconds(2), ct);
    }
    // Returns success after break
}
```

**Exit conditions for the outer loop:**
1. `CancellationToken` is triggered (external timeout/cancel)
2. `FindNextIncompletePhase` returns `null` (all phases ✅ Complete)
3. A sub-phase execution returns `Success = false` (early return with error)

**Missing exit condition:** There is NO check for "zero progress made in this iteration." If `ExecuteTasksInPhaseAsync` returns `Success = true` but `taskCount == 0`, the loop continues.

### Inner Loop: `ExecuteTasksInPhaseAsync` (line 1503)

```csharp
private async Task<PhaseResult> ExecuteTasksInPhaseAsync(
    string planPath, PlanPhase phase, int codingPhaseNumber, CancellationToken ct)
{
    var taskDuration = TimeSpan.Zero;
    var taskCount = 0;

    while (!ct.IsCancellationRequested)
    {
        var planContent = await File.ReadAllTextAsync(planPath, ct);
        
        var nextTask = _taskParser.FindNextIncompleteTask(planContent, phase.Number);
        if (nextTask == null)
        {
            _logger.LogInformation(
                "All tasks in Phase {PlanPhase} complete ({TaskCount} tasks executed)",
                phase.Number, taskCount);
            break;  // ← This is where "0 tasks executed" appears in logs
        }

        taskCount++;
        // ... execute the task ...
    }

    return new PhaseResult
    {
        Success = true,      // ← Always returns success, even if taskCount == 0
        Duration = taskDuration
    };
}
```

**Critical behavior when 0 tasks parsed:**
1. `FindNextIncompleteTask` returns `null` on the **first iteration**
2. The log message fires: `"All tasks in Phase {N} complete (0 tasks executed)"`
3. The inner loop `break`s immediately
4. Method returns `PhaseResult { Success = true, Duration = 0 }`

### The Infinite Loop Path (Complete Trace)

```
1. ExecuteCodingPhasesAsync starts
2. while loop iteration N:
   a. Re-read plan file
   b. PlanPhaseParser.FindNextIncompletePhase(planContent)
      → Returns Phase X (status ⏳ or 🔄 in Progress Summary table)
   c. useTaskGranularity == true (default config: "task")
   d. ExecuteTasksInPhaseAsync(planPath, phaseX, ...)
      i.  Re-read plan file
      ii. TaskParser.FindNextIncompleteTask(planContent, X)
          → Returns null (cannot find/parse tasks for Phase X)
      iii. Logs: "All tasks in Phase X complete (0 tasks executed)"
      iv.  Returns PhaseResult { Success = true }
   e. taskResult.Success == true → does NOT return early
   f. Build verification runs (or doesn't, depending on config)
   g. Git commit attempt (nothing changed, likely no-op)
   h. await Task.Delay(2 seconds)
3. while loop iteration N+1:
   a. Re-read plan file (UNCHANGED — no work was done)
   b. PlanPhaseParser finds SAME Phase X still incomplete
   c. ... repeat forever ...
```

### Why PlanPhaseParser Still Finds the Phase

`PlanPhaseParser.FindNextIncompletePhase` looks at the **Progress Summary table** (a high-level table with status emojis):

```markdown
| 1 | Phase Name | ⏳ Not Started | 0/5 |
```

This table is NOT updated by `ExecuteTasksInPhaseAsync` — it only gets updated by the coding agent as part of its work. Since no coding session actually executes (0 tasks found), the Progress Summary table never changes.

### No Safety Guards Exist

There is:
- ❌ No `MaxIterations` cap on the outer loop
- ❌ No "no progress" detection (e.g., tracking `codingPhaseNumber` vs phases completed)
- ❌ No check that `taskCount > 0` after `ExecuteTasksInPhaseAsync` returns
- ❌ No "same phase returned twice in a row without progress" detection
- ❌ No fallback from task-level to phase-level granularity when 0 tasks found

The only things that can stop the loop are:
- Cancellation token (external timeout)
- Plan file deletion (error)
- Phase-level executor failure (error return)

**Key Discoveries:**
- The infinite loop occurs because `ExecuteTasksInPhaseAsync` returns `Success = true` even when `taskCount == 0`, and the outer loop has no progress-detection guard
- The outer `while` loop has only ONE normal exit: `FindNextIncompletePhase` returning null (all phases marked ✅). There is no iteration cap or no-progress detection.
- The "0 tasks executed" message is logged when `FindNextIncompleteTask` returns null on the very first iteration of the inner loop
- `PlanPhaseParser` reads the Progress Summary table (status emojis), while `TaskParser` reads phase section headers + task table rows. These are completely independent parsing mechanisms that can disagree.
- The default granularity is `"task"`, making this the default code path for all Hugh coding executions
- No safety mechanisms exist: no MaxIterations, no same-phase-twice detection, no fallback to phase-level granularity, no progress tracking between iterations
- The 2-second `Task.Delay` between iterations means the loop burns API calls/tokens at ~30 iterations/minute indefinitely

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` (lines 948-1127) | Contains `ExecuteCodingPhasesAsync` outer loop, the infinite loop source |
| `Hugh/Orchestration/LoopOrchestrator.cs` (lines 1503-1627) | Contains `ExecuteTasksInPhaseAsync` inner loop, returns success with 0 tasks |
| `Hugh/Detection/PlanPhaseParser.cs` | Parses Progress Summary table to find incomplete phases |
| `Hugh/Detection/TaskParser.cs` | Parses phase sections and task table rows; returns null when format doesn't match |
| `Hugh/Configuration/HughConfig.cs` (lines 320-350) | `CodingConfig` with default `Granularity = "task"` |

**Gaps:** None — control flow fully traced  
**Assumptions:** The bug manifests when `Granularity == "task"` (the default). The phase-level branch could theoretically also loop if the coding agent's session doesn't update the Progress Summary table, but that's a different scenario.

## Phase 2: TaskParser Regex & Format Requirements

**Status:** ✅ Complete  
**Session:** 2025-05-20

### TaskParser Architecture

`TaskParser` (`Hugh/Detection/TaskParser.cs`) uses a two-stage parsing approach:

1. **Stage 1 — `ExtractPhaseSection`**: Finds the phase header, then extracts all text between it and the next phase header (or end of document)
2. **Stage 2 — `TaskRowRegex`**: Scans the extracted section body for pipe-delimited task rows with N.M step IDs

### PhaseHeaderRegex — Exact Requirements

```csharp
[GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*:\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
private static partial Regex PhaseHeaderRegex();
```

**This regex requires ALL of the following:**
- Line starts with `###` (exactly H3 level)
- Followed by one or more spaces
- The word `Phase` (case-sensitive)
- One or more digits (captured as group 1 — the phase number)
- Optional whitespace
- A **colon** (`:`) — THIS IS THE CRITICAL CONSTRAINT
- Optional whitespace
- One or more characters (captured as group 2 — the phase name)

**Valid format:** `### Phase 5: Task-Level Coding`

### TaskRowRegex — Exact Requirements

```csharp
[GeneratedRegex(@"\|\s*(\d+\.\d+)\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|", RegexOptions.Compiled)]
private static partial Regex TaskRowRegex();
```

**This regex requires:**
- A pipe `|` followed by optional whitespace
- A step ID in `N.M` format (digits.digits) — captured as group 1
- Optional whitespace and a pipe `|`
- Any non-pipe characters (task description) — captured as group 2
- A pipe, optional whitespace, non-pipe characters (files column) — captured as group 3
- A pipe, optional whitespace, non-pipe characters (acceptance criteria) — captured as group 4
- A final pipe `|`

**Critical constraint:** Requires **exactly 4 content columns**: `| StepId | Description | Files | Acceptance Criteria |`

**Completion detection:** After matching, the parser checks `fullRow.Contains("✅")` — the ✅ can appear ANYWHERE in the matched row text.

### ExtractPhaseSection Logic

```csharp
private static string? ExtractPhaseSection(string planContent, int phaseNumber)
{
    var matches = PhaseHeaderRegex().Matches(planContent);
    for (var i = 0; i < matches.Count; i++)
    {
        var match = matches[i];
        if (!int.TryParse(match.Groups[1].Value, out var num) || num != phaseNumber)
            continue;
        var sectionStart = match.Index + match.Length;
        var sectionEnd = i + 1 < matches.Count
            ? matches[i + 1].Index
            : planContent.Length;
        return planContent[sectionStart..sectionEnd];
    }
    return null;
}
```

**Key behavior:** Returns `null` if no matching `### Phase N:` header is found → 0 tasks → infinite loop.

### Format Variations That CAUSE Parse Failure

| # | Variation | Example | Why It Fails |
|---|-----------|---------|--------------|
| 1 | Dash separator instead of colon | `### Phase 1 - Setup Project` | Regex requires `:` not `-` |
| 2 | Em-dash separator instead of colon | `### Phase 1 — Setup Project` | Regex requires `:` not `—` |
| 3 | No separator | `### Phase 1 Setup Project` | Regex requires `:` after the number |
| 4 | Different heading level | `## Phase 1: Setup` or `#### Phase 1: Setup` | Regex requires exactly `###` |
| 5 | Extra text before Phase | `### Execution Phase 1: Setup` | Regex requires `Phase` immediately after `### ` |
| 6 | Missing step ID in N.M format | `\| 1 \| Task description \| file.cs \| criteria \|` | TaskRowRegex needs `\d+\.\d+` format |
| 7 | Step ID without decimal | `\| Phase 1 \| Task \| files \| criteria \|` | Not `N.M` format |
| 8 | Fewer than 4 columns | `\| 1.1 \| Task description \| file.cs \|` | Regex expects 4 pipe-separated content cells |

### Three Separator Formats in Existing Plans

Examining all plan documents in `/docs/plans/`, THREE different phase header formats are used:

1. **Colon format** (matches TaskParser): `### Phase N: Name` — Used in Execution Plan sections
2. **Dash format** (DOES NOT match): `### Phase N - Name` — Used in Implementation Notes sections
3. **Em-dash format** (DOES NOT match): `### Phase N — Name` — Used in some older plans' Implementation Notes

The built-in `planning-continuation.md` prompt correctly instructs colon format, but LLM non-compliance can still produce dash format.

### The coding.md Template Inconsistency

The `coding.md` prompt's "Implementation Notes" section template uses:
```markdown
### Phase {phaseNumber} - {phaseName}
```
This dash format doesn't directly cause the bug (Implementation Notes is a different section), but it means completed plans contain BOTH formats in the same document, creating confusion for format expectations.

### Test Coverage Gaps

- ❌ **NOT tested: dash-format headers** — No test verifies that dash/em-dash formats return empty
- ❌ **NOT tested: extra columns (5+ pipes)**
- ❌ **NOT tested: missing columns (fewer than 4)**

**Key Discoveries:**
- `PhaseHeaderRegex` requires EXACTLY `### Phase N: Name` with a colon separator — dash and em-dash silently fail
- The repository contains plans using ALL THREE separator formats (`:`, `-`, `—`), but only `:` is parseable
- `ExtractPhaseSection` returning null → empty task list → infinite loop (the exact failure mode)
- `TaskRowRegex` requires exactly 4 content columns — any deviation causes no matches
- The coding.md prompt uses dash format in its template, creating format inconsistency within the same document
- No explicit tests verify the known limitation with dash/em-dash formats

| File | Relevance |
|------|-----------|
| `Hugh/Detection/TaskParser.cs` | Parser with PhaseHeaderRegex and TaskRowRegex definitions |
| `Hugh.Tests/TaskParserTests.cs` | 449 lines of unit tests covering valid formats only |
| `Hugh/Detection/PlanCreationPhaseParser.cs` | Uses identical colon-only `PhaseHeaderRegex` |
| `Hugh/Prompts/BuiltIn/planning-continuation.md` | Instructs planner to produce colon format |
| `Hugh/Prompts/BuiltIn/coding.md` | Contains dash format in Implementation Notes template |

**Gaps:** None  
**Assumptions:** None

## Phase 3: PlanPhaseParser vs TaskParser Format Mismatch

**Status:** ✅ Complete  
**Session:** 2025-05-20

### Two Independent Parsing Mechanisms

The coding phase loop uses **two completely independent parsers** that read **different sections** of the same plan document:

| Parser | Document Section | Regex Pattern | Purpose |
|--------|-----------------|---------------|---------|
| `PlanPhaseParser` | Progress Summary TABLE (near top) | `\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅\|🔄\|⏳\|❌)\s*([^|]*)\|\s*(\d+)/(\d+)\s*\|` | Determines WHICH phase is next |
| `TaskParser` | Phase section HEADERS + task table rows (Execution Plan body) | `^###\s+Phase\s+(\d+)\s*:\s*(.+)$` for headers; `\|\s*(\d+\.\d+)\s*\|...` for tasks | Determines WHICH task within that phase |

### PlanPhaseParser — What It Does

Located in `Hugh/Detection/PlanPhaseParser.cs`:

1. **Primary regex** (`PlanPhaseRowRegex`): Matches rows with N/M task counts:
   ```
   | 1 | Phase Name | ⏳ Not Started | 0/5 |
   ```

2. **Fallback regex** (`PlanPhaseRowSimpleRegex`): Matches simpler tables without task counts:
   ```
   | 1 | Phase Name | ⏳ Not Started |
   ```

3. **`FindNextIncompletePhase()`**: Returns any 🔄 In Progress phase first; then the first ⏳ Not Started phase.

### Concrete Mismatch Scenarios

#### Scenario A: Phase Header Separator Mismatch (Most Common)

Progress Summary (PlanPhaseParser succeeds):
```markdown
| 3 | API Layer | ⏳ Not Started | 0/5 |
```

Phase section header (TaskParser fails):
```markdown
### Phase 3 - API Layer        ← dash separator: FAILS
### Phase 3 — API Layer        ← em-dash separator: FAILS
```

#### Scenario B: Missing Task Table (Partial Planning)

Phase header matches (colon format), but NO task table rows exist:
```markdown
### Phase 3: API Layer

**Status:** ⏳ Not Started
**Size:** Medium
{Phase description — no task table rows exist}
```

TaskParser finds the section but `TaskRowRegex` finds no `| N.M |` rows → empty task list.

#### Scenario C: Wrong Task Table Format

```markdown
| # | Task | File |           ← 3 columns instead of 4
| 1 | Create service | src/ |  ← "1" instead of "3.1" step ID
```

### Execution Flow Trace (Infinite Loop)

```
OUTER LOOP (ExecuteCodingPhasesAsync):
  1. Read plan file
  2. _planPhaseParser.FindNextIncompletePhase(planContent)
     → Reads Progress Summary table → finds Phase 3 (⏳)
  3. ExecuteTasksInPhaseAsync(planPath, phase3, ...)

INNER LOOP (ExecuteTasksInPhaseAsync):
  4. _taskParser.FindNextIncompleteTask(planContent, 3)
     → ExtractPhaseSection(content, 3) → null (dash separator)
     → ParseTasks returns empty list → null
  5. "All tasks in Phase 3 complete (0 tasks executed)" → break
  6. Return PhaseResult { Success = true }

BACK IN OUTER LOOP:
  7. taskResult.Success == true → continue
  8. await Task.Delay(2 seconds)
  9. GOTO step 1 → same phase found again → INFINITE LOOP
```

### Why the Disagreement Is Permanent

1. **Neither parser modifies the document** — They are read-only
2. **No coding agent is invoked** — 0 tasks → no session started → nobody updates anything
3. **Nobody updates the Progress Summary** — Only the coding agent does this, and it never runs
4. **No maximum iteration guard** — The outer `while` loop only exits on cancellation or all-phases-complete
5. **Global timeout is the only safety net** — By then the entire coding budget is exhausted with zero work

### Real Plan Documents With Non-Colon Separators

**Plans with dash (`-`) separators (would trigger bug):**
- `001-pch-coder-subagent-orchestration.md`
- `002-beta-pch-researcher-agent.md`
- `006-pch-agent-presentation.md`
- `010-hugh-global-cli-installation.md`
- `012-ai-optimized-document-format.md`
- `022-hugh-mvp-deployment-gap-fixes.md`

**Plans with em-dash (`—`) separators (would trigger bug):**
- `003-pch-agents-standards-integration.md`
- `013-subagent-question-passthrough.md`
- `021-data-contract-management-across-pch-agents.md`
- `026-hugh-research-phase-loop-and-planning-prompt-fix.md`

### PlanCreationPhaseParser Shares Same Vulnerability

`PlanCreationPhaseParser` uses the **identical** colon-only `PhaseHeaderRegex`, meaning the planning sub-phase loop has the same vulnerability — if the initial planning session generates dash-format headers, the continuation planning loop also cannot find phase sections.

**Key Discoveries:**
- PlanPhaseParser and TaskParser read completely different document sections — they can permanently disagree
- 12+ existing plan documents use non-colon format that would trigger the infinite loop bug
- The disagreement is irresolvable without intervention: neither parser writes, no agent runs, no one updates the Progress Summary
- Even with colon-format headers, a missing task table (partial planning) produces the same zero-task infinite loop
- `PlanCreationPhaseParser` shares the identical colon-only regex vulnerability
- The only protection is the global timeout, which means the entire coding time budget is wasted

| File | Relevance |
|------|-----------|
| `Hugh/Detection/PlanPhaseParser.cs` | Progress Summary table parser; uses emoji-based status detection |
| `Hugh/Detection/TaskParser.cs` | Phase section parser; colon-only PhaseHeaderRegex |
| `Hugh/Detection/PlanCreationPhaseParser.cs` | Planning loop parser; shares identical colon-only regex |
| `Hugh/Orchestration/LoopOrchestrator.cs` | Both loops with no progress guard |
| `Hugh/Prompts/BuiltIn/planning.md` | Template shows colon format |

**Gaps:** None  
**Assumptions:** The `ct` CancellationToken eventually triggers a timeout from Program.cs or the global CLI timeout.

## Phase 4: Fix Strategies & Loop Safety

**Status:** ✅ Complete  
**Session:** 2025-05-20

### Existing Safety Patterns in Hugh

The research loop (`ExecuteResearchSubPhasesAsync`) and planning loop (`ExecutePlanningSubPhasesAsync`) both have a **post-loop safety net** that the coding loop lacks:

```csharp
// Post-loop safety net: verify all phases are complete
var finalContent = await File.ReadAllTextAsync(researchDocPath, ct);
if (_researchPhaseParser.HasIncompletePhases(finalContent))
{
    return new PhaseResult { Success = false, ErrorMessage = "..." };
}
```

The coding loop has **no equivalent safety net** and no progress-detection guard.

---

### Strategy A: Tolerant Parser — Align `TaskParser.PhaseHeaderRegex`

**Description:** Make `PhaseHeaderRegex` accept multiple separator formats.

**Code Change:** `Hugh/Detection/TaskParser.cs` + `Hugh/Detection/PlanCreationPhaseParser.cs`

```csharp
// BEFORE:
[GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*:\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]

// AFTER:
[GeneratedRegex(@"^###\s+Phase\s+(\d+)\s*[:–—\-]\s*(.+)$", RegexOptions.Multiline | RegexOptions.Compiled)]
```

| Pros | Cons |
|------|------|
| Fixes ROOT CAUSE for 12+ existing plans | Doesn't protect against missing task tables |
| 1-line regex change | Slightly looser matching |
| Backward-compatible | — |

**Complexity:** Very Low  
**Prevents infinite loop alone?** Only for header format mismatch. NOT for missing task tables.

---

### Strategy B: Loop-Break Safety Guard (Same-Phase Repetition)

**Description:** Detect when the same phase is attempted multiple times without progress and abort.

**Code Change:** `Hugh/Orchestration/LoopOrchestrator.cs` in `ExecuteCodingPhasesAsync`

```csharp
var codingPhaseNumber = 0;
int? lastPhaseNumber = null;
int samePhaseCount = 0;

while (!ct.IsCancellationRequested)
{
    var planContent = await File.ReadAllTextAsync(planPath, ct);
    var nextPhase = _planPhaseParser.FindNextIncompletePhase(planContent);
    if (nextPhase == null) { break; }

    // Same-phase repetition guard
    if (nextPhase.Number == lastPhaseNumber)
    {
        samePhaseCount++;
        if (samePhaseCount >= 2)
        {
            _logger.LogError(
                "Phase {PlanPhase} attempted {Count} times without completion. Aborting.",
                nextPhase.Number, samePhaseCount + 1);
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Phase {nextPhase.Number} stuck: attempted {samePhaseCount + 1} times without progress. " +
                    "Check that phase headers use '### Phase N: Name' and task tables use '| N.M | ...' format.",
                Duration = totalDuration
            };
        }
    }
    else
    {
        lastPhaseNumber = nextPhase.Number;
        samePhaseCount = 0;
    }
    // ... rest of loop ...
}
```

| Pros | Cons |
|------|------|
| Prevents infinite loop for ALL failure modes | Doesn't fix root cause (plans still fail) |
| ~15 lines, zero model changes | Requires choosing a threshold |
| Catches unknown future failures | — |
| Produces actionable error message | — |

**Complexity:** Very Low  
**Prevents infinite loop alone?** YES — this is the MINIMUM required fix.

---

### Strategy C: Diagnostic Logging

**Description:** Add logging when `taskCount == 0` to make diagnosis instant.

**Code Change:** `Hugh/Orchestration/LoopOrchestrator.cs` in `ExecuteTasksInPhaseAsync`

```csharp
if (nextTask == null)
{
    if (taskCount == 0)
    {
        _logger.LogWarning(
            "Phase {PlanPhase} ({PhaseName}) has 0 parseable tasks. " +
            "Possible format mismatch in plan document.",
            phase.Number, phase.Name);
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

| Pros | Cons |
|------|------|
| Makes diagnosis instant from logs | Does NOT prevent the infinite loop |
| Zero behavior change risk | Only helps after the bug occurs |
| Essential for production | — |

**Complexity:** Very Low  
**Prevents infinite loop alone?** NO — logging doesn't stop the loop.

---

### Strategy D: Post-Planning Format Validation

**Description:** After planning completes, verify `TaskParser` can parse tasks from each phase before coding begins.

**Code Change:** New validation method in `LoopOrchestrator.cs`, called before `ExecuteCodingPhasesAsync`

```csharp
private PlanValidationResult ValidatePlanForCoding(string planPath)
{
    var planContent = File.ReadAllText(planPath);
    var phases = _planPhaseParser.ParsePhases(planContent);
    var emptyPhases = phases
        .Where(p => _taskParser.ParseTasks(planContent, p.Number).Count == 0)
        .Select(p => p.Number)
        .ToList();

    if (emptyPhases.Count > 0)
        return PlanValidationResult.Failed(
            $"Phases [{string.Join(", ", emptyPhases)}] have 0 parseable tasks.");

    return PlanValidationResult.Success();
}
```

| Pros | Cons |
|------|------|
| Catches problems BEFORE wasting API budget | Higher complexity (new class/method) |
| Proactive, clear error | Only catches pre-existing issues, not mid-loop degradation |

**Complexity:** Medium  
**Prevents infinite loop alone?** Yes for pre-existing format issues. Not for mid-loop issues.

---

### Recommended Approach: Defense-in-Depth (A + B + C)

| Priority | Strategy | Effort | Impact |
|----------|----------|--------|--------|
| P0 (Critical) | B — Same-phase repetition guard | ~15 LOC | Eliminates infinite loop for ALL causes |
| P0 (Critical) | A — Tolerant parser regex | ~2 LOC + tests | Fixes root cause for existing plans |
| P1 (Important) | C — Diagnostic logging | ~20 LOC | Enables fast diagnosis |
| P2 (Nice to have) | D — Pre-coding validation | ~40 LOC + class | Proactive catch, saves API budget |

**Minimum Viable Fix:** Strategy B alone (~15 LOC) prevents the infinite loop for all scenarios.

**Recommended Fix:** A + B + C (~37 LOC total) provides root cause fix + safety net + observability.

**Key Discoveries:**
- Strategy B (same-phase repetition guard) is the MINIMUM fix — ~15 lines, prevents infinite loop for ALL failure modes
- Strategy A (tolerant regex) fixes the ROOT CAUSE — `[:–—\-]` makes parsers match all 12+ existing plans
- Research and planning loops have post-loop safety nets the coding loop lacks — a consistency gap
- `ExecuteTasksInPhaseAsync` already tracks `taskCount` locally — checking it is trivial
- The recommended approach is A + B + C (defense-in-depth): root cause fix + safety net + observability
- The simplest same-phase guard needs ZERO model changes — just a counter and condition

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` (lines 948-1111, 1503-1632) | Primary target for Strategy B and C |
| `Hugh/Detection/TaskParser.cs` (line 57) | Primary target for Strategy A regex |
| `Hugh/Detection/PlanCreationPhaseParser.cs` (line 44) | Same regex, should be aligned |
| `Hugh/Models/PhaseResult.cs` | Optional TasksExecuted addition |
| `Hugh/Configuration/HughConfig.cs` | Could add MaxSamePhaseAttempts config |

**Gaps:** None  
**Assumptions:** Same-phase threshold of 2 retries (3 total) is reasonable for an error that should never occur normally.

## Overview

Hugh's coding phase infinite loop is caused by a **parser disagreement**: `PlanPhaseParser` reads the Progress Summary table (emoji-based status) and finds an incomplete phase, while `TaskParser` reads phase section headers (requires `### Phase N: Name` with colon) and fails to find any tasks. `ExecuteTasksInPhaseAsync` treats "0 tasks found" as success, and the outer loop has no progress guard — creating a permanent infinite loop that burns API budget at ~30 iterations/minute until the global timeout cancels it.

The root cause is `TaskParser.PhaseHeaderRegex` requiring a colon separator (`:`) while real plan documents (12+ in the repository) use dash (`-`) or em-dash (`—`) separators. A secondary trigger is missing task tables from partial/interrupted planning sessions. No safety mechanisms exist in the coding loop — unlike the research and planning loops which have post-loop safety nets.

## Key Findings

1. **The infinite loop path:** `PlanPhaseParser.FindNextIncompletePhase` finds a phase (via Progress Summary table) → `TaskParser.FindNextIncompleteTask` returns null (can't parse section headers) → `ExecuteTasksInPhaseAsync` returns `Success = true` with 0 tasks → outer loop re-reads same plan → same phase found → repeat forever
2. **Root cause:** `TaskParser.PhaseHeaderRegex` requires EXACTLY `### Phase N: Name` with colon — dash (`-`) and em-dash (`—`) separators silently fail by returning null from `ExtractPhaseSection`
3. **12+ real plan documents** in `/docs/plans/` use non-colon separators and would trigger this bug under task-granularity mode
4. **No safety guards exist:** No MaxIterations, no same-phase-twice detection, no zero-progress abort, no fallback to phase-level granularity
5. **The two parsers are completely independent:** `PlanPhaseParser` reads the Progress Summary table (top of document); `TaskParser` reads phase section headers + task rows (body). They share no common parsing logic.
6. **Research and planning loops have post-loop safety nets** that the coding loop lacks — a consistency gap
7. **The bug is the DEFAULT code path:** `Granularity = "task"` is the default, making every Hugh coding execution vulnerable

## Actionable Conclusions

1. **Minimum fix (P0):** Add same-phase repetition guard in `ExecuteCodingPhasesAsync` (~15 LOC, zero model changes). Aborts after 3 attempts at the same phase with actionable error message.
2. **Root cause fix (P0):** Change `PhaseHeaderRegex` from `:\s*` to `[:–—\-]\s*` in both `TaskParser.cs` and `PlanCreationPhaseParser.cs` (~2 LOC). Makes parsers match all existing plans.
3. **Observability (P1):** Add diagnostic logging in `ExecuteTasksInPhaseAsync` when `taskCount == 0` (~20 LOC). Distinguishes "all tasks complete" from "no tasks found."
4. **Recommended implementation order:** B (safety net) → A (tolerant regex) → C (logging). Total: ~37 LOC across 3 files.

## Open Questions

- Should the same-phase threshold be configurable (via `HughConfig`) or hardcoded at 3 attempts?
- Should Strategy D (pre-coding validation) be included in the initial fix or deferred?
- Should the phase-level granularity fallback be implemented (automatically switch to phase-level when task-level parsing fails)?

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2025-05-20 |
| Status | ✅ Complete |
| Current Phase | ✅ Complete |
| Path | /docs/research/027-coding-phase-infinite-loop-zero-tasks.md |
