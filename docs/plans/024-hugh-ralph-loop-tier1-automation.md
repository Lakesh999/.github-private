---
id: "024"
type: plan
title: "Hugh Ralph Loop Tier 1 Automation"
status: ✅ Complete
created: "2025-02-23"
updated: "2025-02-23"
completed: "2025-02-23"
owner: pch-planner
version: v2.1
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-02-23 | pch-planner | Initial plan skeleton |
| v1.1 | 2025-02-23 | pch-planner | Per-phase retry scope (B) and retry count/behavior (D) decisions |
| v1.2 | 2025-02-23 | pch-planner | Per-sub-phase verification (D), blocker behavior (B), delay scope (B) decisions |
| v1.3 | 2025-02-23 | pch-planner | PhaseResult enrichment (C), blocker detection (A), testing strategy (C) decisions |
| v2.0 | 2025-02-23 | pch-planner | Requirements, Technical Design, Execution Plan, and Holistic Review completed |
| v2.1 | 2025-02-23 | pch-plan-reviewer | Review complete: 6 issues resolved; PhaseResult enrichment rules, dispatch extraction, constructor chain, appsettings completeness, line number caveat |

## Introduction

This plan implements the Tier 1 (Sprint 1) recommendations from research document `/docs/research/021-ralph-loop-agent-automation-for-hugh.md`. These are the five highest-ROI, lowest-risk automation improvements for Hugh's Ralph Wiggum loop orchestration. All changes are additive, config-gated, and backward-compatible — targeting a loop survival rate improvement from ~60% to ~85%.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| 1 | Per-phase retry scope | B — Non-interactive phases only | Interactive Vision phase has its own multi-turn resilience via `MaxInteractiveTurns`; retrying it would lose conversation state. Research, Planning, Review, and Coding are single-shot and clean retry candidates. |
| 2 | Per-phase retry count and behavior | D — Configurable max retries in `HughConfig` | Single `MaxPhaseRetries` setting (default: 2) keeps config surface small. Non-retryable errors (Authentication, ContextExhausted) fail immediately via `IsRetryable` check. Matches existing `Copilot.MaxRetryAttempts` pattern. |
| 3 | Per-sub-phase verification strategy | D — Build-only after each sub-phase, full verification unchanged | Build check after each sub-phase catches compilation cascades (most common failure) with minimal overhead. Full build+test verification remains post-all-phases. Preserves existing repair mechanism. |
| 4 | Auto-triage skippable blockers behavior | B — Log, record in `LoopState.Decisions`, and continue | Aligns with existing `DecisionLogger` pattern. Provides auditability via `--status` without prompt pollution or execution complexity. Skip budget (Option D) can be layered on later once we have data on blocker frequency. |
| 5 | Error-classified delay integration scope | B — Both per-phase retry and `SendWithRetryAsync` | Fixes GAP-D3 in both locations. `SendWithRetryAsync` already classifies errors but discards `SuggestedDelay` — fixing it ensures rate-limited errors get proper 60s cooldown instead of 2-4-8s backoff. Consistent delay strategy everywhere. |
| 6 | PhaseResult enrichment fields | C — All three plus `RetryAttemptsMade` | `CompactionOccurred`, `ToolMetrics`, `ErrorClassification`, and `RetryAttemptsMade` give full observability. Retry counter is free to add alongside per-phase retry and critical for tuning `MaxPhaseRetries`. |
| 7 | Blocker skippability detection mechanism | A — Tool-based only, text blockers always pause | Respects dual-environment design: Hugh CLI gets explicit `isSkippable` from `hugh_report_blocker` tool; VS Code text signals lack this metadata and already expect human interaction. Zero behavioral change for text path. |
| 8 | Testing strategy | C — Unit tests in existing files + new focused test files | Per-phase retry and blocker auto-skip are complex enough for dedicated test files. PhaseResult enrichment and classified delays fit in existing test files. Prioritizes unit coverage over integration tests. |

## Holistic Review

### Decision Interactions

1. **PhaseResult enrichment enables per-phase retry.** Decision 6 (add `ErrorClassification` to `PhaseResult`) is a hard prerequisite for Decision 1-2 (per-phase retry). The retry loop reads `result.ErrorClassification.IsRetryable` and `SuggestedDelay` — without enrichment, retry logic has no signal to act on. Execution order enforces this: Phase 1 before Phase 3.

2. **Classified delays and per-phase retry are synergistic.** Decision 5 (both `SendWithRetryAsync` and per-phase retry use classified delays) creates a two-tier retry system: in-session retries handle message-level transient errors with short delays, while per-phase retries handle session-level failures with longer delays. A rate-limited error gets 60s in both tiers — consistent and correct.

3. **Per-sub-phase build verification and per-phase retry are independent.** Decision 3 (build-only after each sub-phase) operates within the coding sub-phase loop, while Decision 1-2 (per-phase retry) operates at the outer phase loop. They don't interact directly, but together they address error cascading at two levels: within the coding phase (build check) and across phases (retry).

4. **Blocker auto-skip tool path and text path isolation.** Decision 7 (tool-based only) cleanly decouples from the text-based signal path. The `IsSkippable` property defaults to `false` on `SignalClassification`, so all text-based blockers get existing behavior. This means the dual-environment constraint is fully respected.

### Architectural Considerations

- **No circular dependencies.** Changes flow one direction: PhaseResult → CopilotService → LoopOrchestrator. No component references itself or creates feedback loops.
- **Config additive only.** Two new config flags (`MaxPhaseRetries`, `VerifyBuildPerSubPhase`) with sensible defaults. No existing config keys modified.
- **`HughWorkflowTools` state field.** The `LastBlockerIsSkippable` instance field relies on the fact that one agent session uses one `HughWorkflowTools` instance — no concurrent access. This is safe because `ExecutePhaseAsync` creates tools per-session, but should be documented.
- **`SdkResponseProcessor` constructor change.** Adding an optional `HughWorkflowTools?` parameter could break existing test code that constructs the processor. Mitigation: preserve existing constructor overload and add a new one.

### Trade-offs Accepted

1. **Build-only per-sub-phase, not full test suite.** Some test failures could still cascade — but build failures (the most common cascade) are caught. Full per-sub-phase testing can be evaluated later with timing data.
2. **Text-based blockers always pause.** This means VS Code Copilot chat users don't benefit from auto-skip. Acceptable because VS Code chat is inherently interactive.
3. **Fixed `MaxPhaseRetries` (not per-phase or per-error-category).** A single config value is simpler. Per-category retry counts (Decision 2, Option C) can be layered in Tier 2 if the fixed value proves insufficient.

### Risks Acknowledged

1. **Retry extends total run time.** A run with 3 retries on 2 phases could add 10+ minutes. Mitigated by `MaxPhaseRetries = 2` default and immediate fail on non-retryable errors.
2. **Per-sub-phase repair could consume verification retries.** If `RunRepairPhaseAsync` is called mid-loop and fails, the entire coding phase fails — the post-all-phases verification loop doesn't get a chance. This is intentional: an unfixable build failure mid-coding should not continue.

## Overview

### Feature Summary

Implement five Tier 1 automation improvements to Hugh's orchestration loop:

1. **PhaseResult Enrichment** (Recs 3+4) — Propagate `CompactionOccurred` and `ToolMetrics` from `ProcessedResponse` to `PhaseResult`, giving the orchestrator visibility into context pressure and tool behavior
2. **Per-Phase Retry with Error-Classified Delays** (Rec 1) — Replace the fail-fast pattern in `ExecuteLoopAsync` with retry logic that uses `SessionErrorClassifier` output for intelligent delay calculation
3. **Per-Sub-Phase Verification** (Rec 2) — Move build/test verification inside the coding sub-phase loop so errors are caught and repaired incrementally
4. **Auto-Triage Skippable Blockers** (Rec 5) — Modify `BlockerHandler` to auto-skip blockers marked `isSkippable: true` when autonomous mode is enabled

### Objectives

- Increase loop survival rate from ~60% to ~85% for transient failures
- Eliminate error cascading across coding sub-phases
- Reduce unnecessary manual pauses from skippable blockers
- Provide orchestrator observability into context compaction and tool metrics

### Research Reference

`/docs/research/021-ralph-loop-agent-automation-for-hugh.md` — Phases 1-5, specifically Tier 1 recommendations (Recs 1-5)

## Requirements

### Functional

1. **PhaseResult Enrichment** — `PhaseResult` record gains four new properties: `CompactionOccurred` (bool), `ToolMetrics` (ToolMetricsSummary?), `ErrorClassification` (ErrorClassification?), and `RetryAttemptsMade` (int). Values are populated from `ProcessedResponse` in all `ExecutePhaseAsync` return paths.
2. **Per-Phase Retry** — Non-interactive phases (Research, Planning, Review, Coding) retry on retryable errors up to `MaxPhaseRetries` (configurable, default 2). Non-retryable errors (Authentication, ContextExhausted) fail immediately. Delay between retries uses `ErrorClassification.SuggestedDelay`.
3. **Classified Delays in `SendWithRetryAsync`** — Replace the fixed `RetryBaseDelayMs * 2^attempt` formula with `ErrorClassification.SuggestedDelay` from `SessionErrorClassifier`. Rate-limited errors get 60s delay; transient errors get 2s base with exponential backoff.
4. **Per-Sub-Phase Build Verification** — After each coding sub-phase, run `BuildCommand` only. If build fails, trigger the repair loop for that sub-phase immediately. Full `BuildCommand` + `TestCommand` verification cycle remains unchanged after all sub-phases complete.
5. **Auto-Skip Skippable Blockers** — When `Autonomous.Enabled = true` and a blocker is reported via `hugh_report_blocker` with `isSkippable: true`, auto-skip it. Record the skip as a `Decision` in `LoopState.Decisions`. Text-based blocker signals always pause (unchanged).

### Non-Functional

1. **Backward Compatibility** — All changes are additive. No existing behavior changes when new config values are at defaults. No agent prompt changes.
2. **Config-Gated** — New behaviors behind configuration flags: `Copilot.MaxPhaseRetries`, `Verification.VerifyBuildPerSubPhase`.
3. **Observability** — All auto-skipped blockers and retry attempts are logged (Serilog) and recorded in `LoopState.Decisions`.
4. **Dual-Environment Safety** — Text-based signal paths are unchanged. Tool-based enhancements only affect Hugh CLI.
5. **Test Coverage** — All new logic has unit tests. New `PhaseRetryTests.cs` and `BlockerAutoSkipTests.cs` for complex behaviors.

### Out of Scope

- Tier 2 recommendations (Review→Planning feedback loop, reflexion, context carry-forward summaries)
- Tier 3 recommendations (dynamic replanning, context exhaustion recovery, `hugh_check_build` tool)
- Agent prompt changes (all changes are orchestrator-side)
- SolutionOrchestrator modifications (focus on LoopOrchestrator only)
- Integration tests (unit test coverage first; integration tests can follow)

## Technical Design

### Architecture Overview

All changes are orchestrator-side. No agent prompts are modified. The five Tier 1 improvements are layered in dependency order: PhaseResult enrichment first (keystone), then error-classified delays, per-phase retry, per-sub-phase build verification, and blocker auto-skip.

```yaml
codebase_patterns:
  - pattern: Record with init properties
    location: "Hugh/Models/PhaseResult.cs"
    usage: New properties follow existing init-only pattern
  - pattern: Configuration binding via HughConfig
    location: "Hugh/Configuration/HughConfig.cs"
    usage: New config flags in VerificationConfig and CopilotConfig
  - pattern: Decision audit logging
    location: "Hugh/Models/Decision.cs"
    usage: Blocker auto-skip recorded as Decision
  - pattern: Error classification
    location: "Hugh/Detection/SessionErrorClassifier.cs"
    usage: Full ErrorClassification surfaced to retry logic
  - pattern: Dual-detection signal architecture
    location: "Hugh/Agents/SdkResponseProcessor.cs"
    usage: Tool argument capture for isSkippable; text path unchanged
  - pattern: Tool instance state sharing
    location: "Hugh/Tools/HughWorkflowTools.cs"
    usage: ReportBlocker stores isSkippable on instance for orchestrator read-back
```

### 1. PhaseResult Enrichment (Recs 3+4)

**File:** `Hugh/Models/PhaseResult.cs`

Add four new init-only properties to the `PhaseResult` record:

```csharp
public record PhaseResult
{
    public bool Success { get; init; }
    public string? OutputDocumentPath { get; init; }
    public string? ErrorMessage { get; init; }
    public List<Decision> DecisionsMade { get; init; } = new();
    public TimeSpan Duration { get; init; }
    
    // NEW: Tier 1 enrichment
    public bool CompactionOccurred { get; init; }
    public ToolMetricsSummary? ToolMetrics { get; init; }
    public ErrorClassification? ErrorClassification { get; init; }
    public int RetryAttemptsMade { get; init; }
}
```

**Data flow fix in `CopilotService.ExecutePhaseAsync`:** Every `PhaseResult` return path must populate `CompactionOccurred` and `ToolMetrics` from `ProcessedResponse`:

```csharp
return new PhaseResult
{
    Success = true,
    OutputDocumentPath = processedResponse.OutputDocumentPath,
    DecisionsMade = decisions,
    Duration = stopwatch.Elapsed,
    CompactionOccurred = processedResponse.CompactionOccurred,
    ToolMetrics = processedResponse.ToolMetrics
};
```

Error paths set `ErrorClassification`:

```csharp
return new PhaseResult
{
    Success = false,
    ErrorMessage = ex.Message,
    Duration = stopwatch.Elapsed,
    ErrorClassification = _errorClassifier.Classify(ex)
};
```

### 2. Error-Classified Delays in `SendWithRetryAsync` (GAP-D3)

**File:** `Hugh/Agents/CopilotService.cs`, method `SendWithRetryAsync` (~line 752)

Replace the fixed delay formula with `SessionErrorClassifier.SuggestedDelay`:

**Current:**
```csharp
catch (Exception ex) when (IsTransientError(ex) && attempt < _config.Copilot.MaxRetryAttempts)
{
    attempt++;
    var delay = TimeSpan.FromMilliseconds(_config.Copilot.RetryBaseDelayMs * Math.Pow(2, attempt - 1));
    ...
}
```

**New:**
```csharp
catch (Exception ex)
{
    var classification = _errorClassifier.Classify(ex);
    if (!classification.IsRetryable || attempt >= _config.Copilot.MaxRetryAttempts)
        throw;
    
    attempt++;
    var baseDelay = classification.SuggestedDelay ?? TimeSpan.FromMilliseconds(_config.Copilot.RetryBaseDelayMs);
    var delay = classification.Category == ErrorCategory.RateLimited
        ? baseDelay  // Flat delay for rate limits (60s)
        : TimeSpan.FromTicks(baseDelay.Ticks * (long)Math.Pow(2, attempt - 1));  // Exponential for transient
    ...
}
```

**Rationale:** Rate-limited errors need a flat 60s cooldown (exponential backoff would produce 120s, 240s). Transient errors keep exponential backoff but use the classifier's 2s base instead of the config's generic `RetryBaseDelayMs`.

### 3. Per-Phase Retry in `ExecuteLoopAsync` (Rec 1)

**File:** `Hugh/Orchestration/LoopOrchestrator.cs`, method `ExecuteLoopAsync` (~line 213)

**New config:** `Copilot.MaxPhaseRetries` (int, default 2)

**File:** `Hugh/Configuration/HughConfig.cs`, class `CopilotConfig`

```csharp
public class CopilotConfig
{
    // ... existing properties ...
    
    /// <summary>
    /// Maximum number of retry attempts for a non-interactive phase when it fails
    /// with a retryable error. Non-retryable errors (Authentication, ContextExhausted) 
    /// fail immediately.
    /// </summary>
    public int MaxPhaseRetries { get; set; } = 2;
}
```

**Logic change in `ExecuteLoopAsync`:** Extract the existing dispatch block into a local function and wrap it in a retry loop for non-interactive phases:

```csharp
// Local function encapsulating existing dispatch logic (extracted, not duplicated)
async Task<PhaseResult> DispatchPhaseAsync(LoopPhase phase)
{
    if (phase == LoopPhase.Coding)
    {
        var planPath = state.Documents.Plan;
        // ... existing plan validation ...
        return await ExecuteCodingPhasesAsync(planPath, ct);
    }
    else if (IsInteractivePhase(phase))
    {
        return await ExecuteInteractivePhaseAsync(phase, state, ct);
    }
    else
    {
        return await ExecuteSinglePhaseAsync(phase, state, ct);
    }
}

// Execute with retry for non-interactive phases
var result = await DispatchPhaseAsync(phase);

if (!result.Success && !IsInteractivePhase(phase) 
    && result.ErrorClassification?.IsRetryable == true)
{
    var maxRetries = _config.Copilot.MaxPhaseRetries;
    for (var retryAttempt = 1; retryAttempt <= maxRetries; retryAttempt++)
    {
        var delay = result.ErrorClassification.SuggestedDelay 
            ?? TimeSpan.FromSeconds(5);
        _logger.LogWarning(
            "Phase {Phase} failed with retryable error ({Category}). " +
            "Retry {Attempt}/{Max} after {Delay}s",
            phase, result.ErrorClassification.Category,
            retryAttempt, maxRetries, delay.TotalSeconds);
        
        await Task.Delay(delay, ct);
        
        // Re-dispatch the phase via the same local function
        result = await DispatchPhaseAsync(phase);
        result = result with { RetryAttemptsMade = retryAttempt };
        
        if (result.Success) break;
        if (result.ErrorClassification?.IsRetryable != true) break;
    }
}
```

### 4. Per-Sub-Phase Build Verification (Rec 2)

**File:** `Hugh/Orchestration/LoopOrchestrator.cs`, method `ExecuteCodingPhasesAsync` (~line 379)

**New config:** `Verification.VerifyBuildPerSubPhase` (bool, default true)

**File:** `Hugh/Configuration/HughConfig.cs`, class `VerificationConfig`

```csharp
public class VerificationConfig
{
    // ... existing properties ...
    
    /// <summary>
    /// Whether to run build verification after each coding sub-phase.
    /// When true, runs BuildCommand after each sub-phase and triggers repair if it fails.
    /// Full build+test verification still runs after all sub-phases complete.
    /// </summary>
    public bool VerifyBuildPerSubPhase { get; set; } = true;
}
```

**Logic change in `ExecuteCodingPhasesAsync`:** After each sub-phase completes successfully, run build check:

```csharp
if (result.Success && _config.Verification.Enabled && _config.Verification.VerifyBuildPerSubPhase
    && !string.IsNullOrEmpty(_config.Verification.BuildCommand))
{
    _logger.LogInformation(
        "Running per-sub-phase build verification after coding sub-phase {SubPhase}",
        codingPhaseNumber);
    
    var buildResult = await RunShellCommandAsync(_config.Verification.BuildCommand, ct);
    if (!buildResult.Success)
    {
        _logger.LogWarning(
            "Build failed after coding sub-phase {SubPhase}: {Output}",
            codingPhaseNumber, buildResult.Output);
        
        // Trigger repair for this sub-phase
        var repairResult = await RunRepairPhaseAsync(
            planPath, "build", buildResult.Output, ct);
        totalDuration += repairResult.Duration;
        
        if (!repairResult.Success)
        {
            return new PhaseResult
            {
                Success = false,
                ErrorMessage = $"Build repair failed after coding sub-phase {codingPhaseNumber}: " +
                    $"{repairResult.ErrorMessage}",
                Duration = totalDuration
            };
        }
    }
}
```

This is inserted between the existing sub-phase success check and the `Task.Delay` between sub-phases.

### 5. Auto-Skip Skippable Blockers (Rec 5)

This requires changes across 4 files to thread `isSkippable` through the signal pipeline.

#### 5a. Capture `isSkippable` on `HughWorkflowTools` instance

**File:** `Hugh/Tools/HughWorkflowTools.cs`

Add an instance field to store the last blocker's skippability:

```csharp
public class HughWorkflowTools
{
    private readonly ILogger _logger;
    private readonly string _workspacePath;
    
    /// <summary>
    /// Stores the isSkippable value from the most recent ReportBlocker call.
    /// Read by the orchestrator after tool-based blocker signal detection.
    /// Thread-safe: only one agent session uses a HughWorkflowTools instance at a time.
    /// </summary>
    public bool LastBlockerIsSkippable { get; private set; }
    
    /// <summary>
    /// Stores the reason from the most recent ReportBlocker call.
    /// </summary>
    public string? LastBlockerReason { get; private set; }
```

Update `ReportBlocker` to store the value:

```csharp
public BlockerResult ReportBlocker(...)
{
    LastBlockerIsSkippable = isSkippable;
    LastBlockerReason = reason;
    // ... existing logic ...
}
```

#### 5b. Add `IsSkippable` to `SignalClassification`

**File:** `Hugh/Detection/SignalClassifier.cs`

```csharp
public record SignalClassification
{
    // ... existing properties ...
    
    /// <summary>
    /// For Blocker signals from tool calls: whether the blocker can be auto-skipped.
    /// Always false for text-based blocker signals (which lack this metadata).
    /// </summary>
    public bool IsSkippable { get; init; }
}
```

#### 5c. Thread `isSkippable` through `SdkResponseProcessor`

**File:** `Hugh/Agents/SdkResponseProcessor.cs`

Add a reference to `HughWorkflowTools` on `SdkResponseProcessor` (passed via constructor). After `DetectToolSignal("hugh_report_blocker")`, read `workflowTools.LastBlockerIsSkippable`:

```csharp
var toolSignal = DetectToolSignal(toolName);
if (toolSignal != null)
{
    // Enrich blocker signals with tool-provided metadata
    if (toolSignal.Type == SignalType.Blocker && _workflowTools != null)
    {
        toolSignal = toolSignal with 
        { 
            IsSkippable = _workflowTools.LastBlockerIsSkippable,
            RawMatch = _workflowTools.LastBlockerReason
        };
    }
    _toolBasedSignal = toolSignal;
}
```

#### 5d. Auto-skip in `CopilotService.HandleSignalAsync`

**File:** `Hugh/Agents/CopilotService.cs`, method `HandleSignalAsync` (~line 832)

```csharp
case SignalType.Blocker:
    // Check for auto-skip: tool-based skippable blockers in autonomous mode
    if (classification.Source == SignalSource.ToolCall 
        && classification.IsSkippable 
        && _config.Autonomous.Enabled)
    {
        _logger.Warning(
            "Auto-skipping skippable blocker in autonomous mode: {Description}",
            classification.RawMatch ?? "Unknown");
        
        // Record the skip as a Decision
        return new QuestionResponse
        {
            ShouldPause = false,
            Response = null,
            DecisionMade = new Decision
            {
                Phase = currentPhase,
                Question = $"Skippable blocker: {classification.RawMatch ?? "Unknown"}",
                Options = new List<string> { "Skip", "Pause" },
                Selected = "Skip",
                Rationale = "Auto-skipped: isSkippable=true in autonomous mode",
                Autonomous = true,
                Timestamp = DateTime.UtcNow
            }
        };
    }
    
    // Existing behavior for non-skippable or text-based blockers
    var blockerResponse = _blockerHandler.HandleBlocker(classification, currentPhase);
    ...
```

### Data Contracts

No data entities in scope — data contracts not applicable.

## Dependencies

### Internal Dependencies

| Dependency | Required By | Status |
|-----------|-------------|--------|
| `PhaseResult` enrichment | Per-phase retry (needs `ErrorClassification`) | Phase 1 of execution |
| `SessionErrorClassifier` | Both retry logic and classified delays | ✅ Exists, no changes needed |
| `HughWorkflowTools.ReportBlocker` | Blocker auto-skip (needs `isSkippable` capture) | Exists, needs state field |
| `LoopOrchestrator.RunShellCommandAsync` | Per-sub-phase build verification | ✅ Exists, reusable |
| `LoopOrchestrator.RunRepairPhaseAsync` | Per-sub-phase build repair | ✅ Exists, reusable |
| `Decision` record | Blocker auto-skip audit logging | ✅ Exists, no changes |

### External Dependencies

None. All changes use existing SDK features and .NET libraries.

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| Per-phase retry extends run time significantly | Medium | Low | Config-gated via `MaxPhaseRetries`; non-retryable errors fail immediately |
| Per-sub-phase build verification adds overhead | Low | Low | Config-gated via `VerifyBuildPerSubPhase`; build-only (~5-10s per sub-phase) |
| `HughWorkflowTools` state field race condition | Very Low | Medium | Single agent session per `HughWorkflowTools` instance; documented as not thread-safe |
| Rate limit retry delay (60s) blocks progress | Low | Low | Only for `ErrorCategory.RateLimited`; correct behavior per API rate limiting |
| `SdkResponseProcessor` constructor change breaks DI | Low | Medium | New parameter is optional (nullable); existing callers unchanged |

## Execution Plan

> **Note:** Line numbers in this plan are approximate and may drift as earlier phases modify files. Always navigate by **method name + anchor description** (e.g., "after the `if (!result.Success)` block in `ExecuteLoopAsync`") rather than relying on exact line numbers.

### Phase 1: PhaseResult Enrichment & Configuration

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 4
**Prerequisites:** None
**Entry Point:** `Hugh/Models/PhaseResult.cs`
**Verification:** `dotnet build` passes; existing tests pass

| Step | Task | Files | Status | Notes |
|------|------|-------|--------|-------|
| 1.1 | Add `CompactionOccurred`, `ToolMetrics`, `ErrorClassification`, `RetryAttemptsMade` properties to `PhaseResult` record | `Hugh/Models/PhaseResult.cs` | ✅ Complete | 4 init-only properties with XML doc comments added |
| 1.2 | Add `MaxPhaseRetries` property (int, default 2) to `CopilotConfig` class | `Hugh/Configuration/HughConfig.cs` | ✅ Complete | |
| 1.3 | Add `VerifyBuildPerSubPhase` property (bool, default true) to `VerificationConfig` class | `Hugh/Configuration/HughConfig.cs` | ✅ Complete | |
| 1.4 | Add `"MaxPhaseRetries": 2` to Copilot section and `"VerifyBuildPerSubPhase": true` to Verification section in appsettings.json | `Hugh/appsettings.json` | ✅ Complete | |
| 1.5 | Write unit tests verifying `PhaseResult` new properties can be set and defaulted | `Hugh.Tests/Models/PhaseResultTests.cs` (new file) | ✅ Complete | 10 tests covering defaults, with-expressions, combined enrichment |

**Phase 1 Implementation Notes:**
- **Completed:** 2025-02-23
- **Files Modified:** `Hugh/Models/PhaseResult.cs`, `Hugh/Configuration/HughConfig.cs`, `Hugh/appsettings.json`
- **Files Created:** `Hugh.Tests/Models/PhaseResultTests.cs`
- **Deviations:** Plan referenced `Description` property on ErrorClassification but actual property is `SuggestedAction`; tests use the correct property name
- **Verification:** All 670 tests pass (10 new + 660 existing)

### Phase 2: Error-Classified Delays & PhaseResult Data Flow

**Status:** ✅ Complete
**Size:** Medium
**Files to Modify:** 3
**Prerequisites:** Phase 1 complete
**Entry Point:** `Hugh/Agents/CopilotService.cs`
**Verification:** `dotnet build` passes; existing `SdkResponseProcessorTests` and `SessionErrorClassifierTests` pass; new tests pass

| Step | Task | Files | Status | Notes |
|------|------|-------|--------|-------|
| 2.1 | Refactor `SendWithRetryAsync` to use `SessionErrorClassifier.Classify(ex)` for delay calculation: flat delay for `RateLimited`, exponential for `Transient`; re-throw non-retryable | `Hugh/Agents/CopilotService.cs` | ✅ Complete | Removed superseded `IsTransientError` method |
| 2.2 | Populate `CompactionOccurred`, `ToolMetrics`, and `ErrorClassification` on all `PhaseResult` return paths in both `ExecutePhaseAsync` and `ExecuteInteractivePhaseAsync` using category-based rules | `Hugh/Agents/CopilotService.cs` | ✅ Complete | All 14 return paths enriched per 3-category rules |
| 2.3 | (Merged into 2.2) | — | ✅ Complete | — |
| 2.4 | Add unit tests for classified delay behavior in `SendWithRetryAsync` | `Hugh.Tests/CopilotServiceRetryTests.cs` (new) | ✅ Complete | 11 tests: 4 delay calculation, 4 classifier-driven, 3 end-to-end |

**Phase 2 Implementation Notes:**
- **Completed:** 2025-02-23
- **Files Modified:** `Hugh/Agents/CopilotService.cs`
- **Files Created:** `Hugh.Tests/CopilotServiceRetryTests.cs`
- **Deviations:** Extracted `CalculateRetryDelay` as internal static helper for testability. Removed superseded `IsTransientError` private method.
- **Verification:** All 681 tests pass (11 new + 670 existing)

### Phase 3: Per-Phase Retry in LoopOrchestrator

**Status:** ✅ Complete
**Size:** Medium
**Files to Modify:** 2
**Prerequisites:** Phase 2 complete (PhaseResult has ErrorClassification populated)
**Entry Point:** `Hugh/Orchestration/LoopOrchestrator.cs`
**Verification:** `dotnet build` passes; new `PhaseRetryTests.cs` passes

| Step | Task | Files | Status | Notes |
|------|------|-------|--------|-------|
| 3.1 | Extract dispatch block into `DispatchPhaseAsync` local function; add per-phase retry loop for non-interactive phases | `Hugh/Orchestration/LoopOrchestrator.cs` | ✅ Complete | Implemented as protected virtual method for testability |
| 3.2 | Ensure Coding phase retry works correctly with `FindNextIncompletePhase` | `Hugh/Orchestration/LoopOrchestrator.cs` | ✅ Complete | Plan-path validation moved into dispatch method |
| 3.3 | Log retry attempts with phase name, error category, attempt number, and delay | `Hugh/Orchestration/LoopOrchestrator.cs` | ✅ Complete | Warning-level per attempt; Error-level on exhaustion |
| 3.4 | Create `PhaseRetryTests.cs` with retry scenario tests | `Hugh.Tests/Orchestration/PhaseRetryTests.cs` (new) | ✅ Complete | 7 tests (5 required + 2 edge cases) |

**Phase 3 Implementation Notes:**
- **Completed:** 2025-02-23
- **Files Modified:** `Hugh/Orchestration/LoopOrchestrator.cs`
- **Files Created:** `Hugh.Tests/Orchestration/PhaseRetryTests.cs`
- **Deviations:** `DispatchPhaseAsync` as protected virtual method (not local function) for testability. `IsInteractivePhase` changed to protected virtual. `ExecuteLoopAsync` changed to internal.
- **Verification:** All 688 tests pass (7 new + 681 existing)

### Phase 4: Per-Sub-Phase Build Verification

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 2
**Prerequisites:** Phase 1 complete (VerifyBuildPerSubPhase config exists)
**Entry Point:** `Hugh/Orchestration/LoopOrchestrator.cs`
**Verification:** `dotnet build` passes; new tests pass

| Step | Task | Files | Status | Notes |
|------|------|-------|--------|-------|
| 4.1 | Add build-only verification after each successful coding sub-phase in `ExecuteCodingPhasesAsync` | `Hugh/Orchestration/LoopOrchestrator.cs` | ✅ Complete | Guarded with Verification.Enabled && VerifyBuildPerSubPhase && BuildCommand |
| 4.2 | Log per-sub-phase build verification: info on start, warning on failure, info on pass | `Hugh/Orchestration/LoopOrchestrator.cs` | ✅ Complete | Includes sub-phase number and truncated output |
| 4.3 | Add tests for build verification config gate, happy path, repair trigger, and repair failure | `Hugh.Tests/Orchestration/SubPhaseBuildVerificationTests.cs` (new) | ✅ Complete | 6 tests (4 required + 2 extra config gate tests) |

**Phase 4 Implementation Notes:**
- **Completed:** 2025-02-23
- **Files Modified:** `Hugh/Orchestration/LoopOrchestrator.cs`
- **Files Created:** `Hugh.Tests/Orchestration/SubPhaseBuildVerificationTests.cs`
- **Deviations:** Made `RunPostCodingVerificationAsync` protected virtual for test isolation. 6 tests instead of minimum 4.
- **Verification:** All 694 tests pass (6 new + 688 existing)

### Phase 5: Auto-Skip Skippable Blockers

**Status:** ✅ Complete
**Size:** Medium
**Files to Modify:** 5
**Prerequisites:** Phase 1 complete
**Entry Point:** `Hugh/Detection/SignalClassifier.cs`
**Verification:** `dotnet build` passes; new `BlockerAutoSkipTests.cs` passes; existing `SignalClassifierTests.cs` passes

| Step | Task | Files | Status | Notes |
|------|------|-------|--------|-------|
| 5.1 | Add `IsSkippable` property (bool, default false) to `SignalClassification` record | `Hugh/Detection/SignalClassifier.cs` | ✅ Complete | Backward-compatible default |
| 5.2 | Add `LastBlockerIsSkippable` and `LastBlockerReason` to `HughWorkflowTools`; set in `ReportBlocker` | `Hugh/Tools/HughWorkflowTools.cs` | ✅ Complete | Properties set at top of ReportBlocker |
| 5.3 | Add 5-param constructor to `SdkResponseProcessor`; enrich Blocker signals with IsSkippable | `Hugh/Agents/SdkResponseProcessor.cs` | ✅ Complete | 3-param→4-param→5-param chain |
| 5.4 | Pass `HughWorkflowTools` instance to `SdkResponseProcessor` in `CopilotService` | `Hugh/Agents/CopilotService.cs` | ✅ Complete | Via `_lastSessionWorkflowTools` field |
| 5.5 | Add auto-skip logic in `HandleSignalAsync` for skippable tool-based blockers | `Hugh/Agents/CopilotService.cs` | ✅ Complete | Decision recorded with autonomous=true |
| 5.6 | Create `BlockerAutoSkipTests.cs` with auto-skip scenario tests | `Hugh.Tests/BlockerAutoSkipTests.cs` (new) | ✅ Complete | 11 tests (5 required + 6 extra) |

**Phase 5 Implementation Notes:**
- **Completed:** 2025-02-23
- **Files Modified:** `Hugh/Detection/SignalClassifier.cs`, `Hugh/Tools/HughWorkflowTools.cs`, `Hugh/Agents/SdkResponseProcessor.cs`, `Hugh/Agents/CopilotService.cs`
- **Files Created:** `Hugh.Tests/BlockerAutoSkipTests.cs`
- **Deviations:** Used `_lastSessionWorkflowTools` field on CopilotService to thread workflow tools to processor.
- **Verification:** All 705 tests pass (11 new + 694 existing)

## Standards

No organizational standards applicable to this plan.

## Review Session Log

**Questions Pending:** 0  
**Questions Resolved:** 6  
**Last Updated:** 2025-02-23

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | PhaseResult enrichment missing from 14+ return paths in ExecutePhaseAsync | correctness | Option B: Categorize return paths into 3 groups with rules | Steps 2.2 and 2.3 updated with enrichment rules |
| 2 | Per-phase retry incorrectly placed in LoopOrchestrator | correctness | Option B: Keep plan approach with corrected dispatch via local function | Phase 3 Steps 3.1-3.2 updated |
| 3 | Auto-skip adds HandleSignalAsync blocker case to wrong class | correctness | Option A: Keep in HandleSignalAsync as planned | No plan change needed |
| 4 | Missing MaxPhaseRetries in appsettings.json | completeness | Option A: Add step to include MaxPhaseRetries in appsettings.json | Step 1.4 updated to add both keys |
| 5 | SdkResponseProcessor constructor backward compatibility | specificity | Option A: Specify exact 3-constructor chain | Step 5.3 updated with exact signatures |
| 6 | Line number references slightly off | clarity | Option B: Keep line numbers with "approximate, may drift" caveat | Caveat note added to Execution Plan |

### Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | 8 files across 3 directories (Models, Agents, Orchestration, Detection, Tools, Configuration) |
| New patterns introduced | 2 | Per-phase retry loop and dispatch extraction; follows existing retry patterns |
| External dependencies | 1 | None — all changes use existing SDK features and .NET libraries |
| Migration complexity | 1 | No migrations; additive config-gated changes only |
| Test coverage required | 3 | 3 new test files (PhaseResultTests, PhaseRetryTests, BlockerAutoSkipTests) + extensions to existing tests |
| **Overall Complexity** | **10/25** | **Low** — all changes are additive, config-gated, and follow established patterns |

## Review Summary

**Review Date:** 2025-02-23  
**Reviewer:** pch-plan-reviewer  
**Original Plan Version:** v2.0  
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 6 (Critical: 1, Major: 3, Minor: 2)
- Clarifying Questions Asked: 6
- Sections Updated: Technical Design (Sections 1, 3, 5), Execution Plan (Phases 1-3, 5), Handoff, Version History

### Key Improvements Made
1. **PhaseResult enrichment rules** — Merged Steps 2.2/2.3 and added a 3-category rules table covering all 14+ `PhaseResult` return paths across both `ExecutePhaseAsync` and `ExecuteInteractivePhaseAsync`
2. **Per-phase retry dispatch extraction** — Replaced duplicated switch expression with local `DispatchPhaseAsync` function that reuses the existing dispatch block
3. **SdkResponseProcessor constructor chain** — Specified exact 3-constructor overload chain (3-param → 4-param → 5-param) ensuring backward compatibility
4. **appsettings.json completeness** — Added `MaxPhaseRetries` to the Copilot config section alongside `VerifyBuildPerSubPhase`
5. **Line number drift caveat** — Added note to Execution Plan header directing pch-coder to navigate by method name + anchor description

### Remaining Considerations
- `HughWorkflowTools.LastBlockerIsSkippable` instance field is documented as not thread-safe (single session per instance) — if concurrency model ever changes, revisit
- `ExecuteInteractivePhaseAsync` also creates `PhaseResult` objects and needs the same enrichment rules applied (covered by the category-based rules in Step 2.2)
- Phase 3 and Phase 4 can execute in parallel since they modify different code paths in `LoopOrchestrator.cs` — but sequential execution is safer to avoid merge conflicts

### Sign-off
This plan has been reviewed and is **Ready for Implementation**

## Implementation Notes

### Plan Completion

**All phases completed:** 2025-02-23
**Total tasks completed:** 20
**Total files modified:** 8
**Total files created:** 5
**Total tests added:** 45 (705 total, up from 660)

**Code Review:** 2 findings — 1 fixed (unused using in BlockerAutoSkipTests.cs), 1 pre-existing (bare catch in TryExtractContentFromEvent reflection method, not in scope)

**All Modified Files:**
- `Hugh/Models/PhaseResult.cs`
- `Hugh/Configuration/HughConfig.cs`
- `Hugh/appsettings.json`
- `Hugh/Agents/CopilotService.cs`
- `Hugh/Orchestration/LoopOrchestrator.cs`
- `Hugh/Detection/SignalClassifier.cs`
- `Hugh/Tools/HughWorkflowTools.cs`
- `Hugh/Agents/SdkResponseProcessor.cs`

**All Created Files:**
- `Hugh.Tests/Models/PhaseResultTests.cs`
- `Hugh.Tests/CopilotServiceRetryTests.cs`
- `Hugh.Tests/Orchestration/PhaseRetryTests.cs`
- `Hugh.Tests/Orchestration/SubPhaseBuildVerificationTests.cs`
- `Hugh.Tests/BlockerAutoSkipTests.cs`

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2025-02-23 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2025-02-23 |
| Implemented By | pch-coder |
| Implementation Date | 2025-02-23 |
| Status | ✅ Complete |
| Plan Location | /docs/plans/024-hugh-ralph-loop-tier1-automation.md |
