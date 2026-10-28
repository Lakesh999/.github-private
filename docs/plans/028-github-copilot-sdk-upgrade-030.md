---
id: "028"
type: plan
title: "GitHub Copilot SDK Upgrade: 0.1.22 → 0.3.0"
status: ✅ Complete
completed: 2025-05-19
created: 2025-05-19
updated: 2025-05-19
owner: pch-planner
version: v2.1
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-05-19 | pch-planner | Initial plan creation || v1.1 | 2025-05-19 | pch-planner | Completed 6 planning decisions (scope D, resume B, observability D, detector A, attachments A, auth A) |
| v2.0 | 2025-05-19 | pch-planner | Holistic review completed; full execution plan with 5 phases |
| v2.1 | 2025-05-19 | pch-plan-reviewer | Review complete: 5 issues resolved (1 Critical, 4 Major); Phase 4 expanded for SolutionOrchestrator; Steps 2.5, 3.4, 3.7, 4.2, 5.2, 5.9 updated |
## Introduction

This plan covers upgrading Hugh's `GitHub.Copilot.SDK` dependency from version 0.1.22 to 0.3.0. The upgrade involves 4 hard compilation fixes, elimination of 4 reflection hacks with native API calls, wiring a previously-unwired SDK callback, and adding hang protection. It also fixes a critical production bug where InfiniteSession was never actually enabled due to a property name typo in reflection code.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| 1 | Upgrade scope | D — Full Adoption (Maximum) | User wants all required, recommended, and optional items: session resume, `GetAuthStatusAsync`, subagent events, token tracking, `MessageOptions.Attachments` |
| 2 | Session resume integration depth | B — Resume on Explicit Pause/Resume Only | Fresh context per phase is by design; resume only helps the `--resume` CLI scenario. Nullable `LastSessionId` on `LoopState` is trivially backward-compatible. Fallback to fresh session ensures reliability. |
| 3 | Event monitoring & token tracking | D — Full Observability Dashboard Model | Log subagent + usage events, add `UsageMetrics` and `SubagentMetrics` on `PhaseResult`, extend `ToolMetricsCollector`, create `SessionReport` aggregate, print summary table at workflow completion |
| 4 | CopilotCliDetector role change | A — Soft Diagnostic (Warning Only) | Keep detector but change to non-blocking warning. `hugh doctor` reports it; doesn't gate execution. `--fix` can still install as optional. Standalone binary useful for debugging. |
| 5 | MessageOptions.Attachments adoption | A — Replace `BuildPromptWithDocuments` Entirely | Use SDK Attachments API for all document context passing. Removes prompt-concatenation workaround. Clean prompt + proper file boundaries. Full adoption scope warrants full modernization. |
| 6 | GetAuthStatusAsync migration | A — Replace GitHubAuthDetector Implementation | Swap `GitHubAuthDetector` internals from `gh auth status` shell to SDK `GetAuthStatusAsync()`. Keeps `IPrerequisiteDetector` interface. Requires injecting client/factory into detector. |

## Review Session Log

**Questions Pending:** 0  
**Questions Resolved:** 5  
**Last Updated:** 2025-05-19

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | SolutionOrchestrator calls BuildPromptWithDocuments at 3 sites — not in Phase 4 scope | correctness | Option A: Migrate SolutionOrchestrator to Attachments API too | Phase 4 updated: added SolutionOrchestrator.cs to file list, added Step 4.4b |
| 2 | PrerequisiteState.Warning enum value doesn't exist | correctness | Option B: Return `Satisfied` with informational message | Step 5.2 updated |
| 3 | Session ID persistence: no path from CopilotService to LoopStateManager | specificity | Option B: Return session ID on `PhaseResult` — orchestrator persists | Steps 4.2 updated; `PhaseResult.SessionId` added to Step 3.4 scope |
| 4 | ProcessedResponse type modification unspecified | specificity | Option A: Clarify target type — add `UsageMetrics?` and `SubagentMetrics?` to `ProcessedResponse` | Step 3.7 clarified |
| 5 | Naming collision: Hugh's OnUserInputRequest vs SDK's OnUserInputRequest | correctness | Option A: Rename Hugh's property to `OnQuestionDetected` | Step 2.5 expanded to include rename |

## Holistic Review

### Decision Interactions

1. **Attachments + Session Resume** — Using `MessageOptions.Attachments` means document content is not embedded in the prompt text. If `ResumeSessionAsync` restores a prior session, the resumed context already has the attachments from the prior turn. This is safe — the `--resume` scenario won't re-send documents unless a new message is sent.

2. **Full Observability (D) + Core Upgrade** — Adding `UsageMetrics`, `SubagentMetrics`, `SessionReport` increases the model surface, but these are all new additive types with no coupling to the 4 required compilation fixes. Phase 3 (observability) is independent of Phase 1 (core upgrade), so a failure in observability doesn't block the SDK upgrade itself.

3. **GitHubAuthDetector SDK-based (A) + Client Initialization Changes** — The auth detector now depends on `CopilotClient` being startable. But `hugh doctor` runs *before* normal execution. The detector must handle `CopilotClient` startup failure gracefully. **Mitigation:** Implement try/catch with fallback to shell-based `gh auth status` — documented in Phase 5, Step 5.1.

4. **CopilotCliDetector Warning (A) + SDK Bundled CLI** — If the standalone `copilot` binary is absent but the SDK bundles its own, `hugh doctor` will show a non-blocking warning. This is correct behavior — the warning helps users who want the standalone tool for debugging without blocking execution.

5. **InfiniteSession Fix + Token Tracking** — Enabling context compaction (which was broken before) will change session behavior: longer sessions, more compaction events, potentially different token usage patterns. `AssistantUsageEvent` tracking (Phase 3) will provide visibility into this behavioral change — the two features complement each other well.

### Architectural Considerations

- **`PhaseResult` growth** — Adding `UsageMetrics?` and `SubagentMetrics?` is the third and fourth nullable property added to this record. Still reasonable given it's the primary phase-output container. No explosion risk.
- **`SdkResponseProcessor` complexity** — Adding 4+ new event type cases to the switch statement. The switch is already the core routing mechanism; this is natural growth. Consider extracting a `HandleObservabilityEvent` helper if it exceeds ~15 cases.
- **`CopilotService` method count** — Adding `ResumeSessionAsync` wrapper and removing `TryEnableInfiniteSession` + `BuildPromptWithDocuments` is net neutral. File size stays manageable.

### Trade-offs Accepted

- **Full scope (D) means ~5 phases** — Larger implementation surface, but each phase is independently verifiable and the features are well-isolated. Acceptable given the user's explicit choice.
- **`GetAuthStatusAsync` chicken-and-egg** — Requiring a `CopilotClient` for auth detection creates a dependency cycle for `hugh doctor`. Mitigated by fallback, but adds branching complexity to the detector.
- **`BuildPromptWithDocuments` removal** — If `MessageOptions.Attachments` doesn't pass file content effectively (e.g., the SDK only passes paths, not content), agent behavior may degrade. Mitigated by testing with representative documents and keeping the old implementation in git history.

### Risks Acknowledged

- InfiniteSession was NEVER enabled — enabling it for the first time may surface previously-unseen compaction behavior. Watchpoint for live testing.
- Subagent events (`SubagentStartedEvent`, etc.) may never fire if Hugh doesn't trigger SDK-level subagents. The code handles them defensively (counters stay at zero) with no waste.
- `SessionReport` summary table adds console output that may be noisy in CI. Print only with `--verbose` or interactive TTY.

## Overview

### Feature Summary

Upgrade the GitHub Copilot SDK package from 0.1.22 to 0.3.0, applying all required API changes, eliminating reflection-based workarounds in favor of native typed APIs, and optionally adopting new SDK capabilities that improve reliability.

### Objectives

1. Achieve clean compilation with SDK 0.3.0
2. Fix the InfiniteSession production bug (context compaction was never enabled)
3. Eliminate all 4 reflection hacks with direct API calls
4. Wire the native `OnUserInputRequest` SDK callback
5. Add `ForceStopAsync` hang protection to disposal
6. Update configuration to expose new SDK properties
7. Maintain all existing tests (100+ passing unchanged)

### Research Reference

Based on: `/docs/research/026-github-copilot-sdk-upgrade-0122-to-030.md`

## Requirements

### Functional

- Hugh compiles and passes all tests with SDK 0.3.0
- InfiniteSession (context compaction) is actually enabled in production
- SDK `OnUserInputRequest` callback fires for question handling (with text-fallback preserved)
- Permission handler approves all tool calls (matching existing security model)
- `CopilotClient` initializes with proper `CopilotClientOptions`
- Disposal uses `ForceStopAsync` as hang protection

### Non-Functional

- Zero behavior regression for existing agent orchestration
- Dual-environment compatibility preserved (VS Code + Hugh CLI text signals)
- Test impact limited to 2 tests (InfiniteSession reflection tests)

### Out of Scope

- None — full adoption scope selected

## Technical Design

### Architecture Impact

The upgrade touches the SDK boundary layer and observability infrastructure. No core orchestration redesign needed. Key architectural changes:

1. **Client initialization** — `CopilotClient` constructor changes from parameterless to `CopilotClientOptions`
2. **Session config** — `OnPermissionRequest` required; `InfiniteSessions` replaces reflection hack
3. **Event processing** — 3 reflection-based handlers become proper type-matched switch cases; new event types added
4. **Question handling** — SDK `OnUserInputRequest` callback wired natively; reflection in `FromSdkRequest` eliminated
5. **Observability** — New `UsageMetrics`, `SubagentMetrics`, `SessionReport` models; `ToolMetricsCollector` extended
6. **Document passing** — `BuildPromptWithDocuments` → `MessageOptions.Attachments`
7. **Session resume** — `LoopState` gains nullable `LastSessionId`; `--resume` tries `ResumeSessionAsync` first
8. **Setup/Detection** — `GitHubAuthDetector` uses SDK `GetAuthStatusAsync`; `CopilotCliDetector` becomes non-blocking warning

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: SDK Integration
    location: "Hugh/Agents/CopilotService.cs"
    usage: Central point for all SDK interaction changes
  - pattern: Response Processing
    location: "Hugh/Agents/SdkResponseProcessor.cs"
    usage: Event handling switch statement — add proper type cases + new event subscriptions
  - pattern: Configuration Binding
    location: "Hugh/Configuration/HughConfig.cs"
    usage: Add new CopilotConfig properties, bind from appsettings.json
  - pattern: Question Handling
    location: "Hugh/Handlers/SdkQuestionHandler.cs"
    usage: Replace reflection with direct property access via HughUserInputRequest
  - pattern: Metrics Collection
    location: "Hugh/Metrics/ToolMetricsCollector.cs"
    usage: Extend for subagent + usage metrics; follow existing ToolStats pattern
  - pattern: Models
    location: "Hugh/Models/PhaseResult.cs"
    usage: Add UsageMetrics + SubagentMetrics records; follow existing ToolMetricsSummary pattern
  - pattern: Setup Detectors
    location: "Hugh/Setup/Detection/"
    usage: Modify GitHubAuthDetector + CopilotCliDetector; follow IPrerequisiteDetector interface
  - pattern: Live SDK Tests
    location: "Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs"
    usage: Rewrite 2 InfiniteSession tests; add new tests for permission, resume, events
```

### Key Type Changes

| Current (0.1.22) | New (0.3.0) | Location |
|---|---|---|
| `new CopilotClient()` | `new CopilotClient(CopilotClientOptions)` | `CopilotService.cs:107` |
| *(not set)* | `OnPermissionRequest = PermissionHandler.ApproveAll` | `CopilotService.cs:695` |
| `TryEnableInfiniteSession()` (reflection) | `InfiniteSessions = new InfiniteSessionConfig { ... }` | `CopilotService.cs:713-758` → delete |
| `BuildPromptWithDocuments()` string concat | `MessageOptions { Attachments = [...] }` | `CopilotService.cs:847-855` |
| Reflection `FromSdkRequest` | Direct `request.Question` / `.Choices` / `.AllowFreeform` | `SdkQuestionHandler.cs:84-100` |
| String name check `"AssistantMessageEvent"` | `case AssistantMessageEvent msg:` | `SdkResponseProcessor.cs:250` |
| String name check `"SessionCompaction*"` | `case SessionCompactionStartEvent:` / `case SessionCompactionCompleteEvent:` | `SdkResponseProcessor.cs:257-265` |
| `PermissionHandler` (was a delegate) | `PermissionHandler` (static class with `ApproveAll`) | SDK namespace change |

### New Models

```csharp
// Hugh/Models/UsageMetrics.cs
public sealed record UsageMetrics
{
    public int InputTokens { get; init; }
    public int OutputTokens { get; init; }
    public int ReasoningTokens { get; init; }
    public TimeSpan Duration { get; init; }
    public string? Model { get; init; }
}

// Hugh/Models/SubagentMetrics.cs
public sealed record SubagentMetrics
{
    public int Started { get; init; }
    public int Completed { get; init; }
    public int Failed { get; init; }
}

// Hugh/Models/SessionReport.cs
public sealed record SessionReport
{
    public Guid RunId { get; init; }
    public List<PhaseMetricsEntry> Phases { get; init; } = new();
    public UsageMetrics TotalUsage { get; init; } = new();
    public SubagentMetrics TotalSubagents { get; init; } = new();
    public int CompactionEvents { get; init; }
    public TimeSpan TotalDuration { get; init; }
}

public sealed record PhaseMetricsEntry
{
    public string PhaseName { get; init; } = "";
    public UsageMetrics? Usage { get; init; }
    public SubagentMetrics? Subagents { get; init; }
    public ToolMetricsSummary? Tools { get; init; }
    public TimeSpan Duration { get; init; }
}
```

### LoopState Model Extension

```csharp
// Add nullable field — backward compatible via JSON deserialization defaulting to null
public record LoopState
{
    // ... existing fields ...
    public string? LastSessionId { get; init; }  // NEW: for --resume session continuity
}
```

### Configuration Extensions

```json
// appsettings.json additions to Hugh:Copilot section:
{
  "BufferExhaustionThreshold": 0.95,
  "CliPath": null,
  "AutoRestart": true
}
```

```csharp
// CopilotConfig additions:
public double BufferExhaustionThreshold { get; set; } = 0.95;
public string? CliPath { get; set; }
public bool AutoRestart { get; set; } = true;
```

### Data Contracts

No data entities in scope — data contracts not applicable.

## Dependencies

| Dependency | Type | Impact |
|------------|------|--------|
| `GitHub.Copilot.SDK` 0.3.0 NuGet package | External | Must be available on NuGet feed |
| Existing test infrastructure | Internal | `SdkTestBase`, `TestCategory.LiveSdk` unchanged |
| `appsettings.json` schema | Internal | Additive changes only — backward compatible |
| `LoopState` JSON serialization | Internal | New nullable field — backward compatible deserialization |
| `IPrerequisiteDetector` interface | Internal | No interface changes — only implementation swap |
| `PhaseResult` consumers | Internal | New nullable properties — non-breaking for existing code |
| `ConsoleOutput` (Spectre.Console) | Internal | Used for `SessionReport` summary table rendering |

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|-----------|
| SDK 0.3.0 has undocumented breaking changes beyond research findings | Low | High | Run full test suite after package update; research was comprehensive |
| `PermissionHandler.ApproveAll` not in expected namespace | Low | Medium | Fall back to custom lambda: `(req, inv) => Task.FromResult(PermissionRequestResult.Allow)` |
| InfiniteSession behavioral change when actually enabled (was never working before) | Medium | Medium | Monitor session logs for compaction events; verify via live SDK tests |
| `MessageOptions.Attachments` behaves differently than prompt concatenation for agent context | Medium | High | Test with representative documents; compare agent responses; keep `BuildPromptWithDocuments` in git history for rollback reference |
| `ResumeSessionAsync` fails for expired/stale sessions | Medium | Low | Graceful fallback to `CreateSessionAsync` already in design |
| `GetAuthStatusAsync` requires running client — chicken-and-egg for `hugh doctor` | Medium | Medium | Implement fallback: try SDK-based check, catch startup failure → fall back to shell `gh auth status` with warning |
| `SessionReport` summary table adds noise to CI output | Low | Low | Only print when `--verbose` or when a TTY is detected |
| Backward-compat: existing `.orchestrator/loop-state.json` files missing `LastSessionId` | Low | Low | Nullable property defaults to null on deserialization — no migration needed |

## Execution Plan

### Phase 1: Core SDK Upgrade (Required Compilation Fixes)

**Status:** ✅ Complete  
**Size:** Small  
**Files to Modify:** 5  
**Prerequisites:** None  
**Entry Point:** `Hugh/Hugh.csproj`  
**Verification:** `dotnet build` succeeds with zero errors

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 1.1 | Update package reference from 0.1.22 to 0.3.0 | `Hugh/Hugh.csproj` | `<PackageReference Include="GitHub.Copilot.SDK" Version="0.3.0" />` |
| 1.2 | Replace `new CopilotClient()` with `new CopilotClient(new CopilotClientOptions { Cwd = _workspacePath, Logger = _loggerFactory.CreateLogger("CopilotSdk"), UseLoggedInUser = true, AutoRestart = _config.Copilot.AutoRestart })` | `Hugh/Agents/CopilotService.cs` (L107) | Client created with options; no parameterless constructor call |
| 1.3 | Add `OnPermissionRequest = PermissionHandler.ApproveAll` to `SessionConfig` in `CreateSessionAsync` | `Hugh/Agents/CopilotService.cs` (L693) | Property set on every session config creation |
| 1.4 | Delete entire `TryEnableInfiniteSession` method (L713-758). Replace with direct property in `CreateSessionAsync`: `InfiniteSessions = new InfiniteSessionConfig { Enabled = true, BackgroundCompactionThreshold = _config.Copilot.CompactionThreshold, BufferExhaustionThreshold = _config.Copilot.BufferExhaustionThreshold }` | `Hugh/Agents/CopilotService.cs` | Reflection method deleted; direct property set; no `TryEnableInfiniteSession` call |
| 1.5 | Add `BufferExhaustionThreshold`, `CliPath`, `AutoRestart` properties to `CopilotConfig` class | `Hugh/Configuration/HughConfig.cs` | Properties exist with correct defaults (0.95, null, true) |
| 1.6 | Add corresponding JSON entries to `appsettings.json` under `Hugh:Copilot` | `Hugh/appsettings.json` | New keys present and bindable |
| 1.7 | Run `dotnet build` — verify zero compilation errors | — | Clean build |

### Phase 2: Reflection Elimination & Native API Adoption

**Status:** ✅ Complete  
**Size:** Medium  
**Files to Modify:** 4  
**Prerequisites:** Phase 1 complete (clean build on 0.3.0)  
**Entry Point:** `Hugh/Agents/SdkResponseProcessor.cs`  
**Verification:** `dotnet test` — all existing tests pass

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 2.1 | Replace reflection-based event handling in `SdkResponseProcessor`: convert `"AssistantMessageEvent"` string check to `case AssistantMessageEvent msg:` with `responseBuilder.Append(msg.Data.Content)` | `Hugh/Agents/SdkResponseProcessor.cs` (L249-253) | Proper type-matched case; no string name check for this event |
| 2.2 | Replace `"SessionCompactionStartEvent"` and `"SessionCompactionCompleteEvent"` string checks with typed switch cases | `Hugh/Agents/SdkResponseProcessor.cs` (L255-265) | Proper type-matched cases; no string name checks for compaction |
| 2.3 | Delete `TryExtractContentFromEvent` method entirely (no longer needed) | `Hugh/Agents/SdkResponseProcessor.cs` (L480-530) | Method removed; no references |
| 2.4 | Replace reflection in `HughUserInputRequest.FromSdkRequest` — use `sdkRequest.Question`, `sdkRequest.Choices`, `sdkRequest.AllowFreeform` directly | `Hugh/Handlers/SdkQuestionHandler.cs` (L66-105) | Direct property access; `TryGetPromptFromSdkRequest` deleted; no reflection |
| 2.5 | Rename Hugh's `CopilotService.OnUserInputRequest` property to `OnQuestionDetected` (declaration L61, assignment L82, usage L877/879/881). This distinguishes Hugh's orchestrator-facing callback from the SDK's `SessionConfig.OnUserInputRequest`. Then wire `SessionConfig.OnUserInputRequest` callback in `CreateSessionAsync`: create lambda that converts `UserInputRequest` → `HughUserInputRequest.FromSdkRequest()` → calls `_sdkQuestionHandler.HandleAsync()` → returns `SDK.UserInputResponse` | `Hugh/Agents/CopilotService.cs` (L61, L82, L693, L877-881) | Hugh property renamed; SDK callback wired; fires when model requests user input |
| 2.6 | Add `ForceStopAsync` hang protection in `DisposeAsync`: wrap `_client.StopAsync()` in `WaitAsync(5s)` try/catch, call `ForceStopAsync()` on timeout | `Hugh/Agents/CopilotService.cs` (L155-163) | Disposal doesn't hang; `ForceStopAsync` called on timeout |
| 2.7 | Run `dotnet test` — all 100+ existing tests pass | — | Zero test failures |

### Phase 3: Observability & Metrics Models

**Status:** ✅ Complete  
**Size:** Medium  
**Files to Modify:** 7  
**Prerequisites:** Phase 2 complete (reflection eliminated, event handling updated)  
**Entry Point:** `Hugh/Models/`  
**Verification:** `dotnet build` succeeds; new model tests pass

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 3.1 | Create `UsageMetrics` record: `InputTokens`, `OutputTokens`, `ReasoningTokens`, `Duration`, `Model` | `Hugh/Models/UsageMetrics.cs` (new) | Record compiles; serializable |
| 3.2 | Create `SubagentMetrics` record: `Started`, `Completed`, `Failed` | `Hugh/Models/SubagentMetrics.cs` (new) | Record compiles; serializable |
| 3.3 | Create `SessionReport` record with `PhaseMetricsEntry` list, `TotalUsage`, `TotalSubagents`, `CompactionEvents`, `TotalDuration` | `Hugh/Models/SessionReport.cs` (new) | Record compiles; can aggregate PhaseMetricsEntry items |
| 3.4 | Add `UsageMetrics? Usage`, `SubagentMetrics? Subagents`, and `string? SessionId` properties to `PhaseResult` | `Hugh/Models/PhaseResult.cs` | Properties exist; nullable; backward compatible |
| 3.5 | Add subagent event subscriptions in `SdkResponseProcessor` switch: `SubagentStartedEvent`, `SubagentCompletedEvent`, `SubagentFailedEvent` → increment counters | `Hugh/Agents/SdkResponseProcessor.cs` | Events logged (Info); counters tracked in local state |
| 3.6 | Add `AssistantUsageEvent` subscription → capture `InputTokens`, `OutputTokens`, `ReasoningTokens`, `Duration`, `Model` into `UsageMetrics` | `Hugh/Agents/SdkResponseProcessor.cs` | Token data captured; available on `ProcessedResponse` result |
| 3.7 | Add `UsageMetrics? Usage` and `SubagentMetrics? Subagents` properties to `ProcessedResponse` record (nested in `SdkResponseProcessor`, L141). Accumulate values in local variables within `ProcessResponseAsync` (same pattern as `compactionOccurred`). In `CopilotService.ExecutePhaseAsync`, read from `processedResponse.Usage` / `.Subagents` and assign to `PhaseResult.Usage` / `.Subagents` | `Hugh/Agents/SdkResponseProcessor.cs` (L141 + ProcessResponseAsync body), `Hugh/Agents/CopilotService.cs` | `PhaseResult.Usage` and `PhaseResult.Subagents` populated from session events |
| 3.8 | Create `SessionReport` accumulator in `LoopOrchestrator` — aggregate `PhaseResult` metrics per phase; print summary table at workflow end via `ConsoleOutput` | `Hugh/Orchestration/LoopOrchestrator.cs`, `Hugh/ConsoleOutput.cs` | Summary table printed showing per-phase tokens, duration, tool counts |
| 3.9 | Run `dotnet build` — verify compilation | — | Clean build |

### Phase 4: Session Resume & Document Attachments

**Status:** ✅ Complete  
**Size:** Medium  
**Files to Modify:** 7  
**Prerequisites:** Phase 1 complete (SDK 0.3.0 compiles)  
**Entry Point:** `Hugh/Models/LoopState.cs`  
**Verification:** `dotnet test` passes; `--resume` with persisted session ID attempts resume

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 4.1 | Add `string? LastSessionId` property to `LoopState` record | `Hugh/Models/LoopState.cs` | Property exists; JSON deserialization of existing state files defaults to null |
| 4.2 | Capture `session.SessionId` in `ProcessedResponse` (new `string? SessionId` property). In `ExecutePhaseAsync`, propagate to `PhaseResult` (new `string? SessionId` property). In `LoopOrchestrator`/`PhaseExecutor`, after receiving `PhaseResult`, persist `SessionId` to `LoopState.LastSessionId` via `LoopStateManager.UpdateAsync(...)` | `Hugh/Agents/SdkResponseProcessor.cs`, `Hugh/Agents/CopilotService.cs`, `Hugh/Orchestration/LoopOrchestrator.cs` or `Hugh/Orchestration/PhaseExecutor.cs` | Session ID flows: session → ProcessedResponse → PhaseResult → LoopState; persisted after each phase |
| 4.3 | In `--resume` path: if `LoopState.LastSessionId` is non-null, attempt `_client.ResumeSessionAsync(lastSessionId)`. Catch exceptions → fall back to `CreateSessionAsync` | `Hugh/Agents/CopilotService.cs` | Resume attempted; graceful fallback on failure; logged |
| 4.4 | Replace `BuildPromptWithDocuments` in `CopilotService` with `MessageOptions.Attachments`: create an internal helper `CreateAttachments(IEnumerable<string> documentPaths)` that reads each path and returns attachment objects. Update `SendWithRetryAsync` to accept optional attachments and use `MessageOptions { Prompt = prompt, Attachments = attachments }` | `Hugh/Agents/CopilotService.cs` (L207, L457, L847-855) | Documents passed as attachments in both `ExecutePhaseAsync` and `ExecuteInteractivePhaseAsync`; old string-concat removed |
| 4.4b | Migrate `SolutionOrchestrator` Attachments: replace 3 call sites (L264, L328, L467) that call `CopilotService.BuildPromptWithDocuments(...)` to use the new internal `CreateAttachments` helper + `SendWithRetryAsync` with attachments parameter. After all callers are migrated, delete `BuildPromptWithDocuments` static method entirely | `Hugh/Orchestration/SolutionOrchestrator.cs`, `Hugh/Agents/CopilotService.cs` | All `BuildPromptWithDocuments` references removed; `SolutionOrchestrator` uses attachments; clean build |
| 4.5 | Update `ICopilotService.ExecutePhaseAsync` / `ExecuteInteractivePhaseAsync` — `documentPaths` parameter now feeds attachments instead of prompt concatenation | `Hugh/Agents/ICopilotService.cs`, `Hugh/Agents/CopilotService.cs` | Interface unchanged; implementation uses attachments internally |
| 4.6 | Run `dotnet test` — all tests pass (tests that mock `BuildPromptWithDocuments` may need update) | — | Zero failures |

### Phase 5: Setup Detectors & Test Updates

**Status:** ✅ Complete  
**Size:** Medium  
**Files to Modify:** 8  
**Prerequisites:** Phase 1 complete (SDK 0.3.0 available)  
**Entry Point:** `Hugh/Setup/Detection/GitHubAuthDetector.cs`  
**Verification:** `dotnet test` — all tests pass including new ones

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 5.1 | Modify `GitHubAuthDetector`: replace `gh auth status` shell call with `CopilotClient.GetAuthStatusAsync()`. Accept `Func<CopilotClient>` or a lightweight client factory in constructor. Handle case where client can't start (fallback to shell-based check with warning) | `Hugh/Setup/Detection/GitHubAuthDetector.cs` | SDK-based auth check used; `IProcessRunner` dependency optional/fallback |
| 5.2 | Modify `CopilotCliDetector`: when standalone binary is missing, return `PrerequisiteState.Satisfied` with `Message = "Standalone Copilot CLI not found — SDK bundles CLI internally (optional for debugging)"`. No enum changes needed. | `Hugh/Setup/Detection/CopilotCliDetector.cs` | Missing binary returns `Satisfied` with informational message; `hugh doctor` displays message but doesn't block |
| 5.3 | Update `SetupOrchestrator` or `Program.cs` DI wiring to pass client factory to `GitHubAuthDetector` | `Hugh/Program.cs` or `Hugh/Setup/SetupOrchestrator.cs` | Detector receives client access; DI wiring compiles |
| 5.4 | Rewrite test 2.6 `InfiniteSession_ConfiguredViaReflection_Succeeds` → `InfiniteSession_ConfiguredDirectly_Succeeds`: assert `sessionConfig.InfiniteSessions` is not null after creation | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Test passes; verifies direct property set |
| 5.5 | Rewrite test 2.7 `InfiniteSession_CompactionThreshold_Applied` → assert `sessionConfig.InfiniteSessions.BackgroundCompactionThreshold == 0.8` without reflection | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Test passes; verifies threshold value |
| 5.6 | Add test for `OnPermissionRequest` presence on session config | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` (new test) | Test verifies permission handler is wired |
| 5.7 | Add test for `UsageMetrics` and `SubagentMetrics` on `PhaseResult` | `Hugh.Tests/Models/` (new file) | Model tests pass; records serialize/deserialize |
| 5.8 | Update `GitHubAuthDetectorTests` for new SDK-based implementation | `Hugh.Tests/Setup/GitHubAuthDetectorTests.cs` | Tests pass with mocked client factory |
| 5.9 | Update `CopilotCliDetectorTests` — verify `Satisfied` state with informational message returned when binary missing | `Hugh.Tests/Setup/CopilotCliDetectorTests.cs` | Assertion checks `State == Satisfied` and message contains "optional" |
| 5.10 | Run full `dotnet test` — all tests green | — | Zero failures across entire test suite |

## Standards

No organizational standards applicable to this plan.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2025-05-19 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2025-05-19 |
| Status | ✅ Ready for Implementation |
| Next Agent | pch-coder |
| Plan Location | /docs/plans/028-github-copilot-sdk-upgrade-030.md |

## Review Summary

**Review Date:** 2025-05-19  
**Reviewer:** pch-plan-reviewer  
**Original Plan Version:** v2.0  
**Reviewed Plan Version:** v2.1  

### Review Metrics
- Issues Found: 5 (Critical: 1, Major: 4, Minor: 3)
- Clarifying Questions Asked: 5
- Sections Updated: Steps 2.5, 3.4, 3.7, 4.2, 4.4b (new), 5.2, 5.9

### Key Improvements Made
1. **Phase 4 expanded** — Added Step 4.4b to migrate `SolutionOrchestrator`'s 3 call sites to Attachments API (was a compile-breaking omission)
2. **CopilotCliDetector corrected** — Returns `Satisfied` with message instead of non-existent `Warning` enum value
3. **Session ID propagation specified** — Full flow defined: session → `ProcessedResponse.SessionId` → `PhaseResult.SessionId` → `LoopState.LastSessionId` via orchestrator
4. **`ProcessedResponse` modification clarified** — Explicit target type named with accumulation pattern
5. **Naming collision resolved** — Hugh's `OnUserInputRequest` renamed to `OnQuestionDetected` before wiring SDK's same-named callback

### Remaining Considerations
- Line references are approximate (±5 lines) — coder should use symbol search rather than exact line numbers
- `MessageOptions.Attachments` is untested with this codebase — if agent behavior degrades, the old `BuildPromptWithDocuments` pattern is in git history
- InfiniteSession enabling for the first time (it was broken before) may surface new compaction behavior — monitor logs in initial runs

### Sign-off
This plan has been reviewed and is **Ready for Implementation**
