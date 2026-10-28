---
id: "025"
type: plan
title: "Hugh SDK Test Automation Expansion"
status: ✅ Complete
created: "2025-02-23"
updated: "2025-02-23"
owner: pch-planner
version: v2.1
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|--------|
| v1.0 | 2025-02-23 | pch-planner | Initial plan skeleton |
| v1.1 | 2025-02-23 | pch-planner | Scope decision: full implementation |
| v1.2 | 2025-02-23 | pch-planner | Trait taxonomy, skip migration, bug fix approach |
| v1.3 | 2025-02-23 | pch-planner | CI workflow, testability, token budget, E2E approach |
| v1.4 | 2025-02-23 | pch-planner | Requirements, technical design, dependencies, risks |
| v1.5 | 2025-02-23 | pch-planner | All 6 execution phases defined |
| v2.0 | 2025-02-23 | pch-planner | Holistic review completed |
| v2.1 | 2025-02-23 | pch-plan-reviewer | Review complete: consolidated --no-interactive fix (3 locations), added TryEnableInfiniteSession access modifier change, updated phase dependencies |

## Introduction

This plan expands Hugh's test suite beyond mock-based unit tests to include live SDK integration tests, CLI smoke tests, and end-to-end pipeline validation. Based on research document 022, it addresses ~104-114 new automated tests across five categories, along with shared test infrastructure, CI workflow updates, and bug fixes discovered during research.

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|----------------|--------|-----------|
| 1 | Implementation Scope | A — Full Implementation — All 5 Categories | Research mapped all gaps with specific tests; infrastructure is prerequisite for all categories; incremental effort is modest once foundation exists; avoids re-reading research across multiple planning cycles |
| 2 | Test Trait Taxonomy & CI Filtering | B — Tag New Tests Only + Default-Include Filter | Exclusion filter (`Category!=LiveSdk&Category!=E2E`) achieves primary goal without touching existing files; ~705 existing tests should run in every CI build anyway; minimal churn |
| 3 | Silent Skip Migration | C — Migrate Existing + Deprecate `IsCopilotCliAvailable()` | Centralizes SDK detection in `SdkAvailability.IsSdkAvailable()`; replaces silent-pass with proper `Assert.Skip()`; removes duplicated detection logic; low-risk change |
| 4 | `--no-interactive` Bug Fix | A — Pass `noInteractive` Parameter Through Handler Signature | Mirrors working `doctor` handler pattern; explicit parameter passing; no new static state; straightforward `SetHandler` binding update |
| 5 | Nightly SDK Test CI Workflow | D — No Separate Workflow — Document Manual Testing | SDK tests are developer-local only; avoids daily token burn; no Copilot auth secrets needed in CI; simplest approach; manual `dotnet test --filter` documented |
| 6 | `ParseTimeoutOverride`/`ApplyTimeoutOverride` Testability | A — Make Both Methods `internal static` | Lowest friction; `InternalsVisibleTo` already configured; pure functions ideal for direct unit testing; 2-line access modifier change |
| 7 | Token Budget Controls | A — Include `TokenBudgetTracker` Infrastructure | Provides safety guardrails during SDK test development; `HUGH_TEST_TOKEN_BUDGET` env var for limit; `Assert.Skip()` when budget exceeded; prevents runaway costs |
| 8 | E2E Test Approach | B — Hybrid Real+Mock Approach | `TestableLoopOrchestrator` pattern already proven; real SDK for Vision/Research, mock for Planning/Review/Coding; validates SDK integration without 5x real LLM cost; state transitions deterministic once `PhaseResult` controlled |

## Review Session Log

**Questions Pending:** 0  
**Questions Resolved:** 2  
**Last Updated:** 2025-02-23

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | Missing `--no-interactive` fix for `--check` path (line 180) — same bug as resume/feature paths | correctness | Option B: Merge all three fix locations into a single Step 5.2 | Steps 5.2+5.3 consolidated; Req 7 updated |
| 2 | `TryEnableInfiniteSession` is `private` — Steps 2.6/2.7 cannot test it directly | specificity | Option A: Change `TryEnableInfiniteSession` to `internal` | Step 5.1 updated; Steps 2.6/2.7 clarified |

## Holistic Review

### Decision Interactions

1. **Scope (Q1: Full) × Trait Strategy (Q2: Tag New Only)**: These work well together. Full implementation means many new test files that all get proper traits, while the exclusion filter (`Category!=LiveSdk&Category!=E2E`) naturally handles the new categories without touching existing tests. No conflict.

2. **Skip Migration (Q3: Centralize) × Infrastructure (Q1)**: The `SdkAvailability.IsSdkAvailable()` centralizer is both an infrastructure component (Phase 1) and a migration target for existing code (`IntegrationTests.cs`). The migration is included in Phase 1 to establish the centralized pattern before new test classes reference it. Good sequencing.

3. **Bug Fix (Q4: Pass-through) × CLI Smoke Tests (Q1)**: The `--no-interactive` bug fix is a prerequisite for Phase 4 CLI smoke tests — specifically tests 4.10/4.11 that validate non-interactive behavior. Phase 5 (bug fixes) must execute before Phase 4. This dependency is documented in the phase dependency table.

4. **No CI Workflow (Q5) × Token Budget (Q7: Include)**: Without a scheduled CI workflow, token budget tracking is developer-local only. The `TokenBudgetTracker` still has value as a safety guardrail during local development — preventing a developer from accidentally running a full E2E suite multiple times without realizing the cost. The two decisions are complementary.

5. **Hybrid E2E (Q8) × SDK Lifecycle (Phase 2)**: The hybrid E2E approach requires real `CopilotService` for Vision/Research phases, which depends on SDK lifecycle tests (Phase 2) confirming that real client start/stop/session works correctly. Phase 6 correctly lists Phase 2 as a prerequisite.

6. **Internal Visibility (Q6) × CLI Smoke Tests (Phase 4)**: Making `ParseTimeoutOverride`/`ApplyTimeoutOverride` `internal` enables direct unit testing in Phase 4 CLI smoke tests. Phase 5 step 5.1 must execute before Phase 4 steps 4.1-4.3.

### Architectural Considerations

- **No performance concern**: SDK tests are excluded from CI gate and run locally on demand. The `release.yml` filter change (Phase 5, step 5.4) is critical — without it, new `LiveSdk`/`E2E` tests would fail every CI build. This is the single most important step to get right.
- **Test isolation is robust**: Every test class creates its own temp workspace. Parallel xUnit execution is safe. No `[Collection]` serialization needed unless rate limiting occurs.
- **`SdkTestRetry` reuses production code**: By delegating to `SessionErrorClassifier` and `CalculateRetryDelay`, the retry wrapper inherits all production error classification logic. Changes to error handling are automatically reflected in test retries.

### Trade-offs Accepted

- **No retroactive tagging**: ~705 existing tests remain untagged. This means `dotnet test --filter "Category=Unit"` returns 0 results (not useful). Acceptable because the primary use case is exclusion (`Category!=LiveSdk`), not inclusion.
- **No scheduled CI**: SDK regression detection depends on developers running tests locally. Risk: SDK breakage could go undetected between releases. Mitigated by documenting test commands and encouraging pre-release SDK test runs.
- **Token budget is estimate-based**: `TokenBudgetTracker` uses developer-supplied estimates, not actual token counts from SDK. Acceptable as a guardrail; precision is not critical.
- **Hybrid E2E loses full-pipeline confidence**: By mocking Planning/Review/Coding, E2E tests don't verify those phases work with real SDK. Acceptable because (a) SDK lifecycle tests verify the connection works, and (b) the state machine logic is deterministic once `PhaseResult` is provided.

### Risks Acknowledged

- **LLM non-determinism**: Even with `SdkTestRetry`, some SDK tests may be flaky. Tests assert structural properties (file exists, non-empty content, positive duration) rather than exact content. Strong system prompts help but can't guarantee behavior.
- **`TryEnableInfiniteSession` reflection**: The most fragile production code. Steps 2.6/2.7 specifically target this — if SDK v0.1.23+ changes the reflection target, these tests will catch it immediately.

## Overview

### Feature Summary

Hugh's current ~705-test suite is entirely mock-based. The only integration test touching the real Copilot SDK (`CopilotService_WithRealCLI_ExecutesPrompt`) is silently skipped when the SDK isn't available. This plan creates:

1. **Shared test infrastructure** — Base classes, traits, retry logic, skip detection, token budgets
2. **SDK session lifecycle tests** — Real `CopilotClient` start/stop, `SessionConfig` wiring, `DisposeAsync` cleanup
3. **Workflow tool & signal detection tests** — Real SDK tool invocation, dual-detection priority
4. **CLI smoke tests** — Entry-point validation for `doctor`, `--status`, `--resume`, `--verbose`, `--timeout`, `--no-interactive`
5. **End-to-end pipeline tests** — Single-phase and multi-phase execution with real SDK
6. **CI/CD updates** — Filter SDK tests from PR gate, create nightly SDK test workflow
7. **Bug fixes** — `--no-interactive` propagation, silent skip pattern migration

### Objectives

- Close critical gaps between mock-based assertions and real SDK behavior
- Catch SDK version breakage (especially `TryEnableInfiniteSession` reflection)
- Validate `hugh_*` workflow tools are actually invoked by the SDK
- Test `ProcessResponseAsync` dual-detection priority through the actual event path
- Improve CI reliability by filtering expensive SDK tests from PR gate
- Fix the `--no-interactive` propagation bug in resume/feature handlers

## Requirements

### Functional

1. **Test Infrastructure**: Create `Hugh.Tests/Infrastructure/` directory with shared utilities: `TestCategory.cs`, `SdkAvailability.cs`, `SdkTestBase.cs`, `SdkTestRetry.cs`, `TestTimeouts.cs`, `TokenBudgetTracker.cs`
2. **SDK Lifecycle Tests**: ~18-20 tests covering `CopilotClient` start/stop, `SessionConfig` wiring, `DisposeAsync` cleanup, compaction configuration, and auth/network error handling
3. **Workflow Tools & Signal Tests**: ~25-33 tests covering `hugh_*` tool registration/invocation in real sessions, signal classification against real LLM output, dual-detection priority, and `FileSystemHooks` via SDK
4. **CLI Smoke Tests**: ~35 tests covering `doctor` exit codes, `--status`, `--resume`, `--verbose`, `--timeout`, `--no-interactive` entry points
5. **E2E Pipeline Tests**: ~26 tests covering single-phase execution, multi-phase state transitions (hybrid real+mock), cancellation/timeout, and `PhaseResult` field population
6. **Silent Skip Migration**: Replace `IsCopilotCliAvailable()` + `return` with `SdkAvailability.IsSdkAvailable()` + `Assert.Skip()` in `IntegrationTests.cs`; delete `IsCopilotCliAvailable()` method
7. **Bug Fix**: Propagate `noInteractive` parameter through `HandleResumeCommand`, `HandleNewFeatureCommand`, and the `--check` path in the root handler in `Program.cs`
8. **Testability**: Change `ParseTimeoutOverride` and `ApplyTimeoutOverride` from `private static` to `internal static` in `Program.cs`; change `TryEnableInfiniteSession` from `private` to `internal` in `CopilotService.cs`
9. **CI Update**: Add `--filter "Category!=LiveSdk&Category!=E2E"` to `release.yml` test step
10. **Documentation**: Update `Hugh/README.md` with SDK test execution commands and trait category definitions

### Non-Functional

1. **Test Isolation**: Every test creates its own temp workspace; cleaned up via `IDisposable`/`IAsyncLifetime`
2. **SDK Test Skip**: All `LiveSdk`/`E2E` tests skip gracefully via `Assert.Skip()` when SDK is unavailable
3. **Timeout Enforcement**: All SDK tests have environment-aware timeouts (local vs CI)
4. **Token Budget**: `TokenBudgetTracker` enforces `HUGH_TEST_TOKEN_BUDGET` env var limit (default 50K)
5. **No CI Breakage**: Existing ~705 tests continue passing; new `LiveSdk`/`E2E` tests excluded from `release.yml`
6. **Backward Compatibility**: No changes to public APIs; `internal` visibility changes only

### Out of Scope

1. Separate `sdk-tests.yml` CI workflow (decision: document manual testing only)
2. Retroactive `[Trait]` tagging of existing ~705 tests
3. Custom xUnit `ITraitDiscoverer` or assembly-level defaults
4. Nightly/scheduled SDK test automation
5. Token usage monitoring dashboards

## Technical Design

### Architecture

The test expansion adds 6 infrastructure files and 5 new test classes following existing codebase patterns:

```
Hugh.Tests/
├── Infrastructure/                    # NEW — shared test utilities
│   ├── TestCategory.cs                # String constants for trait categories
│   ├── SdkAvailability.cs             # Centralized SDK detection + caching
│   ├── SdkTestBase.cs                 # Base class with workspace setup, IAsyncLifetime
│   ├── SdkTestRetry.cs                # Retry wrapper using SessionErrorClassifier
│   ├── TestTimeouts.cs                # Environment-aware timeout values
│   └── TokenBudgetTracker.cs          # Token budget enforcement via env var
├── Sdk/                               # NEW — live SDK test classes
│   ├── SdkSessionLifecycleTests.cs    # Phase 1: CopilotClient, SessionConfig, Dispose
│   ├── LiveWorkflowToolTests.cs       # Phase 2: hugh_* tools, signal detection
│   └── LiveDualDetectionTests.cs      # Phase 2: tool-first vs text-fallback priority
├── Cli/                               # NEW — CLI smoke test classes
│   └── CliSmokeTests.cs               # Phase 3: doctor, --status, --resume, --timeout, etc.
├── E2E/                               # NEW — end-to-end pipeline tests
│   └── PipelineTests.cs               # Phase 4: single-phase, multi-phase, cancellation
├── IntegrationTests.cs                # MODIFIED — migrate IsCopilotCliAvailable
└── ... (existing files unchanged)
```

### Key Design Decisions

**`SdkTestBase` Base Class**: Provides unique temp workspace with `agents/`, `docs/vision/`, `docs/research/`, `docs/plans/`, `.orchestrator/` subdirectories; minimal agent stubs for all 5 agents; pre-configured `HughConfig` with test-appropriate settings (short timeouts, test model override via `HUGH_SDK_TEST_MODEL` env var); `IAsyncLifetime` for async setup + `IDisposable` for cleanup.

**`SdkTestRetry` Wrapper**: Reuses Hugh's own `SessionErrorClassifier` to classify exceptions. Retries only retryable errors (Transient, RateLimited). Uses `CopilotService.CalculateRetryDelay` for exponential backoff. Default: 2 retries, 2000ms base delay.

**`SdkAvailability` Centralized Checker**: Caches result per process. Checks: (1) `gh` CLI available, (2) `copilot` extension available, (3) `gh auth status` succeeds. Environment variable `HUGH_SKIP_SDK_TESTS=true` for explicit opt-out.

**`TokenBudgetTracker`**: Thread-safe static counter. Reads `HUGH_TEST_TOKEN_BUDGET` env var (default 50K). Tests call `TokenBudgetTracker.RecordUsage(estimatedTokens)` after SDK calls. Returns `false` if budget exceeded; callers use `Assert.Skip("Token budget exceeded")`.

**Hybrid E2E Pattern**: E2E tests use `TestableLoopOrchestrator` (from `PhaseRetryTests.cs`) for Planning/Review/Coding phases. Vision and Research phases use real `CopilotService`. State transitions verified through `DispatchCalls` recording + `LoopStateManager` persistence.

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: IDisposable + Temp Directory
    location: "Hugh.Tests/LoopStateManagerTests.cs, IntegrationTests.cs"
    usage: All new test classes follow this pattern for workspace isolation
  - pattern: TestableLoopOrchestrator Subclass
    location: "Hugh.Tests/Orchestration/PhaseRetryTests.cs"
    usage: E2E tests override DispatchPhaseAsync for hybrid real+mock testing
  - pattern: NSubstitute Mock Service
    location: "Hugh.Tests/IntegrationTests.cs"
    usage: ICopilotService mocking for offline test paths
  - pattern: InternalsVisibleTo
    location: "Hugh/Hugh.csproj"
    usage: Test project accesses internal members (CreateSessionAsync, EnsureClientStartedAsync, ParseTimeoutOverride, ApplyTimeoutOverride)
  - pattern: Trait-Based Categorization
    location: "Hugh.Tests/Setup/SetupIntegrationTests.cs"
    usage: Only one [Trait("Category", "Integration")] usage currently — expanding to full taxonomy for new tests
  - pattern: PhaseResult Factory Methods
    location: "Hugh.Tests/Orchestration/PhaseRetryTests.cs"
    usage: RetryableFailure(), NonRetryableFailure(), SuccessResult() helpers for controlled test results
```

### Data Contracts

No data entities in scope — data contracts not applicable.

### Component Specifications

#### `TestCategory.cs`

```csharp
namespace Hugh.Tests.Infrastructure;

public static class TestCategory
{
    public const string Unit = "Unit";
    public const string Integration = "Integration";
    public const string Smoke = "Smoke";
    public const string LiveSdk = "LiveSdk";
    public const string E2E = "E2E";
}
```

#### `SdkAvailability.cs`

- Static class with `IsSdkAvailable()` method returning `bool`
- Calls `gh copilot --version` via `Process.Start` (same pattern as existing `IsCopilotCliAvailable()`)
- Also checks `gh auth status` for authenticated session
- Caches result in `static bool? _cachedResult`
- Checks `HUGH_SKIP_SDK_TESTS` env var first — if `"true"`, returns false immediately
- 5-second timeout on process execution

#### `SdkTestBase.cs`

- Abstract class implementing `IAsyncLifetime`, `IDisposable`
- `InitializeAsync()`: creates temp workspace with directory structure, writes minimal agent stubs, constructs `HughConfig` with test settings
- `DisposeAsync()` + `Dispose()`: deletes temp workspace
- Protected properties: `TestWorkspace`, `TestConfig`, `ConfigOptions`, `AgentsPath`, `DocsPath`
- Protected method: `CreateCopilotService()` → returns real `CopilotService` instance with test config
- Protected method: `SkipIfSdkUnavailable()` → calls `SdkAvailability.IsSdkAvailable()`, uses `Assert.Skip()`

#### `SdkTestRetry.cs`

- Static class with `WithRetryAsync(Func<Task> testBody, int maxRetries = 2, int baseDelayMs = 2000)`
- Catches exceptions, classifies via `SessionErrorClassifier`
- Retries only if `classification.IsRetryable` and attempts remain
- Uses `CopilotService.CalculateRetryDelay` for backoff (requires method to be `internal` — already accessible via `InternalsVisibleTo`)

#### `TestTimeouts.cs`

- Static class with timeout properties per test category
- Detects CI via `Environment.GetEnvironmentVariable("CI")` or `"GITHUB_ACTIONS"`
- Categories: Unit (5s/10s), Integration (15s/30s), Smoke (30s/60s), LiveSdk (120s/180s), E2E (300s/600s)
- Returns `CancellationTokenSource` via `CreateTimeout(string category)` factory method

#### `TokenBudgetTracker.cs`

- Static class with thread-safe `Interlocked` counter
- `RecordUsage(int tokens)`: increments counter, returns `true` if under budget
- `IsWithinBudget()`: checks `_totalUsed < _budget`
- `_budget` read from `HUGH_TEST_TOKEN_BUDGET` env var (default 50000)
- `Reset()`: for test isolation between test runs

## Dependencies

### Prerequisites

| Dependency | Type | Required For | Notes |
|------------|------|-------------|-------|
| GitHub Copilot SDK v0.1.22 | Package | All SDK tests | Already in `Hugh.csproj` |
| `gh` CLI | Runtime | SDK availability check | Checked by `SdkAvailability` |
| `gh copilot` extension | Runtime | SDK availability check | Checked by `SdkAvailability` |
| `gh auth` session | Runtime | Live SDK tests | Required for real CopilotClient |
| xUnit 2.9.2 | Package | `Assert.Skip()` | Already in `Hugh.Tests.csproj` |
| NSubstitute 5.3.0 | Package | Mock tests in hybrid E2E | Already in `Hugh.Tests.csproj` |
| `InternalsVisibleTo` | Config | Internal method access | Already in `Hugh.csproj` |

### Phase Dependencies

| Phase | Depends On |
|-------|------------|
| Phase 2 (SDK Lifecycle Tests) | Phase 1 (Infrastructure) + Phase 5 Step 5.1 (access modifiers) |
| Phase 3 (Workflow & Signal Tests) | Phase 1 (Infrastructure) |
| Phase 4 (CLI Smoke Tests) | Phase 1 (Infrastructure) + Phase 5 (Bug Fixes) |
| Phase 5 (Bug Fixes + CI) | None — independent |
| Phase 6 (E2E Pipeline Tests) | Phase 1 (Infrastructure) + Phase 2 (SDK Lifecycle) |

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| `TryEnableInfiniteSession` reflection breaks on SDK update | Medium | High | SDK lifecycle tests will catch this immediately |
| Non-deterministic LLM output causes flaky SDK tests | High | Medium | Use `SdkTestRetry` wrapper; assert structural properties not exact content; use strong system prompts |
| SDK tests fail in CI due to missing auth | High | Low | Tests use `Assert.Skip()` + exclusion filter in `release.yml`; no CI breakage |
| `System.CommandLine` beta4 exit code behavior for void handlers | Low | Medium | CLI smoke tests will verify; if `InvokeAsync` returns 0 for void handlers, document workaround |
| `CalculateRetryDelay` method visibility | Low | Low | Already `internal` or can be made so; `InternalsVisibleTo` covers access |
| Token budget tracking accuracy | Medium | Low | Estimates only; budget is a guardrail not a billing mechanism |
| Test project .NET 9.0 vs Hugh .NET 8.0 compatibility | Low | Low | Already working for ~705 tests; `ProjectReference` handles cross-targeting |

## Execution Plan

### Phase 1: Test Infrastructure

**Status:** ✅ Complete
**Size:** Medium
**Files to Create:** 6
**Prerequisites:** None
**Entry Point:** `Hugh.Tests/Infrastructure/` (new directory)
**Verification:** `dotnet build Hugh.Tests/Hugh.Tests.csproj` succeeds; existing tests still pass

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 1.1 | Create `TestCategory.cs` with string constants for all 5 trait categories (`Unit`, `Integration`, `Smoke`, `LiveSdk`, `E2E`) | `Hugh.Tests/Infrastructure/TestCategory.cs` | Constants defined; compiles without error |
| 1.2 | Create `SdkAvailability.cs` with centralized SDK detection: check `gh copilot --version`, `gh auth status`, `HUGH_SKIP_SDK_TESTS` env var, cached `bool?` result, 5s process timeout. Follow same `Process.Start` pattern as `IsCopilotCliAvailable()` in `IntegrationTests.cs` | `Hugh.Tests/Infrastructure/SdkAvailability.cs` | Static `IsSdkAvailable()` returns `false` when SDK not installed; caches result; respects env var |
| 1.3 | Create `TestTimeouts.cs` with environment-aware timeouts. Detect CI via `CI` or `GITHUB_ACTIONS` env vars. Provide `CreateTimeout(string category)` returning `CancellationTokenSource`. Timeout values: Unit=5s/10s, Integration=15s/30s, Smoke=30s/60s, LiveSdk=120s/180s, E2E=300s/600s (local/CI) | `Hugh.Tests/Infrastructure/TestTimeouts.cs` | Correct timeouts returned for each category; CI detection works |
| 1.4 | Create `SdkTestRetry.cs` with `WithRetryAsync(Func<Task>, int maxRetries=2, int baseDelayMs=2000)`. Use `SessionErrorClassifier.Classify(ex)` for retry decisions. Use `CopilotService.CalculateRetryDelay` for backoff. Only retry if `classification.IsRetryable` and attempts remain | `Hugh.Tests/Infrastructure/SdkTestRetry.cs` | Retries transient errors; throws non-retryable immediately; respects max retries |
| 1.5 | Create `TokenBudgetTracker.cs` with thread-safe `Interlocked` counter. Read `HUGH_TEST_TOKEN_BUDGET` env var (default 50000). `RecordUsage(int tokens)` returns `true` if under budget. `IsWithinBudget()` check. `Reset()` for test isolation | `Hugh.Tests/Infrastructure/TokenBudgetTracker.cs` | Counter increments correctly; respects budget limit; env var override works |
| 1.6 | Create `SdkTestBase.cs` abstract class implementing `IAsyncLifetime`, `IDisposable`. `InitializeAsync()` creates temp workspace with `agents/`, `docs/vision/`, `docs/research/`, `docs/plans/`, `.orchestrator/` dirs; writes 5 minimal agent stubs. Protected properties for workspace paths and config. `SkipIfSdkUnavailable()` helper using `Assert.Skip()` | `Hugh.Tests/Infrastructure/SdkTestBase.cs` | Temp workspace created with all subdirs; agent stubs written; cleanup on dispose; skip works |
| 1.7 | Migrate `IntegrationTests.cs`: replace all `IsCopilotCliAvailable()` + `return` call sites with `SdkAvailability.IsSdkAvailable()` + `Assert.Skip("Copilot SDK not available")`. Delete the `IsCopilotCliAvailable()` private method and its `#region Helper Methods` wrapper | `Hugh.Tests/IntegrationTests.cs` | No references to `IsCopilotCliAvailable` remain; tests report as "skipped" not "passed" when SDK unavailable |

### Phase 2: SDK Session Lifecycle Tests

**Status:** ✅ Complete
**Size:** Medium
**Files to Create:** 1
**Prerequisites:** Phase 1 complete (Infrastructure) + Phase 5 Step 5.1 complete (access modifier changes for `TryEnableInfiniteSession`)
**Entry Point:** `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` (new file)
**Verification:** `dotnet test --filter "Category=LiveSdk"` runs tests (or skips if SDK unavailable); existing tests pass

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 2.1 | Create `SdkSessionLifecycleTests.cs` class extending `SdkTestBase` with `[Trait("Category", TestCategory.LiveSdk)]`. Add `CopilotClient_Start_Succeeds` test: instantiate `CopilotClient`, call `StartAsync()`, assert no exception. Uses `SdkTestRetry.WithRetryAsync` wrapper | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Test creates real client, starts successfully, or skips if SDK unavailable |
| 2.2 | Add `CopilotClient_Start_Idempotent` test: call `EnsureClientStartedAsync` twice on same `CopilotService` instance, verify single client created (no exception on second call) | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Second call is no-op; no double-start error |
| 2.3 | Add `CopilotClient_Dispose_CleansUp` test: start client, call `DisposeAsync()`, verify `_clientStarted` resets. Add `Dispose_CalledTwice_Idempotent` and `Dispose_WithoutStart_Succeeds` tests | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Dispose is idempotent; disposing unstarted service doesn't throw |
| 2.4 | Add `CreateSession_WithFullConfig_Succeeds` test: create `CopilotService`, call `CreateSessionAsync` with model, system message, tools (via `HughWorkflowTools.CreateToolSet()`), and hooks (via `FileSystemHooks.CreateHooks()`). Verify session returned, use `await using` for cleanup | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Real session created with full `SessionConfig`; no exception |
| 2.5 | Add `CreateSession_ToolsRegistered_FourTools` test: verify `HughWorkflowTools.CreateToolSet()` returns 4 `AIFunction` instances and session accepts them without error | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | 4 tools registered in session config |
| 2.6 | Add `InfiniteSession_ConfiguredViaReflection_Succeeds` test: call `CopilotService.TryEnableInfiniteSession` directly (accessible via `internal` + `InternalsVisibleTo`, changed in Step 5.1) on a real `SdkSessionConfig`, verify reflection sets `InfiniteSessionConfig.CompactionThreshold` to 0.8 without throwing | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Reflection path succeeds; threshold value correctly set; catches SDK version breakage |
| 2.7 | Add `InfiniteSession_CompactionThreshold_Applied` test: call `TryEnableInfiniteSession` directly on a `SdkSessionConfig`, then use reflection to read back the `InfiniteSession.CompactionThreshold` property and assert equality to 0.8 | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | `CompactionThreshold == 0.8` after `TryEnableInfiniteSession` |
| 2.8 | Add `PostDispose_ExecutePhase_ThrowsOrFails` test: after `DisposeAsync`, call `ExecutePhaseAsync` and verify graceful failure (exception or failed `PhaseResult`) | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Post-dispose operations fail gracefully |
| 2.9 | Add `Session_ReceivesEvents_OnPrompt` test: create session, send simple prompt ("Reply with: OK"), verify response events are received (non-empty content). Assert `Duration > TimeSpan.Zero`. Uses `TestTimeouts.CreateTimeout(TestCategory.LiveSdk)` | `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Real SDK roundtrip completes; events received; timeout enforced |

### Phase 3: Workflow Tools & Signal Detection Tests

**Status:** ✅ Complete
**Size:** Large
**Files to Create:** 2
**Prerequisites:** Phase 1 complete (Infrastructure); Phase 2 recommended (SDK lifecycle verified)
**Entry Point:** `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` (new file)
**Verification:** `dotnet test --filter "Category=LiveSdk"` runs tests (or skips if SDK unavailable)

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 3.1 | Create `LiveWorkflowToolTests.cs` class extending `SdkTestBase` with `[Trait("Category", TestCategory.LiveSdk)]`. Add `Session_RegistersWorkflowTools_ToolsVisibleInSession` test: create session with `HughWorkflowTools.CreateToolSet()`, verify all 4 tools registered without error | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | 4 `hugh_*` tools registered in real session |
| 3.2 | Add `Session_AgentCanCall_NotifyPhaseComplete` test: prompt agent with system instruction to call `hugh_notify_phase_complete` tool with specific parameters. Verify `ToolExecutionCompleteEvent` fires. Use `SdkResponseProcessor` to process events and verify signal detection | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | Agent invokes tool; event fires; signal classified as `PhaseComplete` |
| 3.3 | Add `Session_AgentCanCall_ReportBlocker` test: prompt agent to call `hugh_report_blocker(reason="test blocker", severity="low", isSkippable=true)`. Verify `LastBlockerIsSkippable` and `LastBlockerReason` metadata populated | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | Blocker tool invoked; metadata enrichment works |
| 3.4 | Add `Session_AgentCanCall_GetWorkspaceStructure` test: prompt agent to call `hugh_get_workspace_structure`. Verify result includes directory tree from test workspace | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | Workspace structure returned; includes `agents/` and `docs/` dirs |
| 3.5 | Add `Session_MultipleToolCalls_MetricsCollected` test: prompt agent to call multiple tools. Verify `ToolMetricsCollector` records all invocations with timing data | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | `TotalExecutions > 1`; timing data positive |
| 3.6 | Add `RealAgent_ProducesDetectablePhaseCompleteSignal` test: use agent prompt that instructs text-based signal format. Verify `SignalClassifier.Classify()` detects `PhaseComplete` from real LLM output | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | Real LLM text matches signal regex |
| 3.7 | Add `RealAgent_LongResponse_SignalStillDetected` test: prompt agent to produce analysis text (1000+ chars) followed by phase complete signal. Verify signal detected despite long prefix | `Hugh.Tests/Sdk/LiveWorkflowToolTests.cs` | Signal detection works in long responses |
| 3.8 | Create `LiveDualDetectionTests.cs` class extending `SdkTestBase` with `[Trait("Category", TestCategory.LiveSdk)]`. Add `DualDetection_ToolFirstPriority_RealSession` test: create session with tools, prompt agent to produce both text signal AND call `hugh_notify_phase_complete`. Verify `Source == ToolCall` (tool-first priority) | `Hugh.Tests/Sdk/LiveDualDetectionTests.cs` | `SignalSource.ToolCall` takes priority over `TextRegex` |
| 3.9 | Add `DualDetection_TextFallback_NoToolsRegistered` test: create session WITHOUT tools, prompt agent to produce text-based phase complete signal. Verify `Source == TextRegex` | `Hugh.Tests/Sdk/LiveDualDetectionTests.cs` | Text fallback used when tools not available |
| 3.10 | Add `DualDetection_NoSignal_WhenAgentDoesntSignal` test: prompt agent with regular analysis request (no signal instructions). Verify `Signal.Type == None` | `Hugh.Tests/Sdk/LiveDualDetectionTests.cs` | No false positive signal detection |
| 3.11 | Add `DualDetection_MetricsAccumulated_AcrossToolCalls` test: multiple tool calls in session, verify only `hugh_*` tools produce signals while other tools are just tracked in metrics | `Hugh.Tests/Sdk/LiveDualDetectionTests.cs` | Metrics include all tool calls; signals only from `hugh_*` tools |

### Phase 4: CLI Smoke Tests & Bug Fixes

**Status:** ✅ Complete
**Size:** Large
**Files to Create:** 1; Files to Modify: 1
**Prerequisites:** Phase 1 complete (Infrastructure); Phase 5 complete (bug fixes in `Program.cs`)
**Entry Point:** `Hugh.Tests/Cli/CliSmokeTests.cs` (new file)
**Verification:** `dotnet test --filter "Category=Smoke"` passes; `dotnet test` (all) still passes

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 4.1 | Create `CliSmokeTests.cs` class with `[Trait("Category", TestCategory.Smoke)]`. Implement `IDisposable` with temp workspace. Add `ParseTimeoutOverride_ValidFormat_ReturnsTimeSpan` test: call `Program.ParseTimeoutOverride("01:00:00")`, assert returns `TimeSpan.FromHours(1)` | `Hugh.Tests/Cli/CliSmokeTests.cs` | Pure function test; direct call succeeds |
| 4.2 | Add `ParseTimeoutOverride_InvalidFormat_ReturnsNull` test: `"xyz"` → null. Add `ParseTimeoutOverride_EmptyString_ReturnsNull` and `ParseTimeoutOverride_Null_ReturnsNull` tests | `Hugh.Tests/Cli/CliSmokeTests.cs` | Invalid/empty/null inputs return null |
| 4.3 | Add `ParseTimeoutOverride_MinutesFormat_Works` test: `"00:30:00"` → 30 min. Add `ApplyTimeoutOverride_SetsAllPhases` test: call `Program.ApplyTimeoutOverride(config, TimeSpan.FromHours(1))`, verify all 5 timeout fields set to 1 hour | `Hugh.Tests/Cli/CliSmokeTests.cs` | All 5 phase timeouts updated identically |
| 4.4 | Add `Doctor_AllSatisfied_ExitCode0` test: create `SetupOrchestrator` with all-satisfied mock detectors, call `RunAsync`, verify `result.AllSatisfied == true`. Follow pattern in `SetupOrchestratorTests.cs` | `Hugh.Tests/Cli/CliSmokeTests.cs` | All detectors pass → AllSatisfied |
| 4.5 | Add `Doctor_OneMissing_ExitCode1` test: create `SetupOrchestrator` with one failing detector, call `RunAsync`, verify `result.AllSatisfied == false` | `Hugh.Tests/Cli/CliSmokeTests.cs` | One failure → not satisfied |
| 4.6 | Add `Status_NoStateFile_ShowsNoActiveRuns` test: create `LoopOrchestrator` with empty workspace, call `GetStatusAsync()`, verify returns null | `Hugh.Tests/Cli/CliSmokeTests.cs` | No state file → null status |
| 4.7 | Add `Status_WithStateFile_ShowsRunStatus` test: create state via `LoopStateManager.CreateRunAsync`, then call `GetStatusAsync()`, verify returns populated `LoopState` | `Hugh.Tests/Cli/CliSmokeTests.cs` | State file exists → returns state |
| 4.8 | Add `Resume_NoState_ReturnsFalse` test: call `ResumeAsync(null)` with empty workspace, verify returns false | `Hugh.Tests/Cli/CliSmokeTests.cs` | No state → resume fails gracefully |
| 4.9 | Add `Resume_CompletedRun_ReturnsTrue` test: create state, mark all phases complete, call `ResumeAsync`, verify returns true (already complete) | `Hugh.Tests/Cli/CliSmokeTests.cs` | Completed run → returns true |
| 4.10 | Add `NoInteractive_ClearsInteractivePhases` test: create `HughConfig` with interactive phases, simulate `--no-interactive` by calling `InteractivePhases.Clear()`, verify empty | `Hugh.Tests/Cli/CliSmokeTests.cs` | Interactive phases cleared |
| 4.11 | Add `NoInteractive_Doctor_SetsNonInteractiveTrue` test: create `SetupOptions` with `NonInteractive = true`, use `NonInteractiveSetupUI`, verify all prompts return `Skip` decisions | `Hugh.Tests/Cli/CliSmokeTests.cs` | Non-interactive UI skips all prompts |

### Phase 5: Bug Fixes & CI Update

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 3
**Prerequisites:** None — independent of other phases
**Entry Point:** `Hugh/Program.cs`, `Hugh/Agents/CopilotService.cs`
**Verification:** `dotnet build`; `dotnet test` passes; `release.yml` includes filter

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 5.1 | Change `ParseTimeoutOverride` from `private static` to `internal static` in `Program.cs` (line ~514). Change `ApplyTimeoutOverride` from `private static` to `internal static` (line ~531). Change `TryEnableInfiniteSession` from `private` to `internal` in `CopilotService.cs` (line ~706). No logic changes — access modifier only | `Hugh/Program.cs`, `Hugh/Agents/CopilotService.cs` | Methods accessible from `Hugh.Tests` via `InternalsVisibleTo`; `dotnet build` succeeds |
| 5.2 | Fix `--no-interactive` propagation in all three `SetupOptions` creation sites: (a) root handler `--check` path (line ~180): change `NonInteractive = Console.IsInputRedirected` to `NonInteractive = noInteractive \|\| Console.IsInputRedirected` (the `noInteractive` variable is already in scope from the handler lambda); (b) `HandleResumeCommand` (line ~260): add `bool noInteractive` parameter, change to `NonInteractive = noInteractive \|\| Console.IsInputRedirected`, update `rootCommand.SetHandler` lambda to pass `noInteractive`; (c) `HandleNewFeatureCommand` (line ~329): same as (b). All three sites should match the working `doctor` handler pattern at line ~126 | `Hugh/Program.cs` | `--no-interactive --check`, `--no-interactive --resume`, and `--no-interactive "feature"` all properly set `NonInteractive = true` |
| 5.3 | Update `release.yml` test step: change `dotnet test Hugh.Tests/Hugh.Tests.csproj --configuration Release --no-restore` to `dotnet test Hugh.Tests/Hugh.Tests.csproj --configuration Release --no-restore --filter "Category!=LiveSdk&Category!=E2E"` | `.github/workflows/release.yml` | SDK/E2E tests excluded from CI gate; existing tests still run |
| 5.4 | Add SDK test execution documentation to `Hugh/README.md`: add a "Running SDK Tests" section with `dotnet test --filter "Category=LiveSdk"`, `dotnet test --filter "Category=E2E"`, `dotnet test --filter "Category=Smoke"` commands. Document `HUGH_SKIP_SDK_TESTS`, `HUGH_TEST_TOKEN_BUDGET`, `HUGH_SDK_TEST_MODEL` env vars | `Hugh/README.md` | Documentation includes all test commands and env var descriptions |

### Phase 6: E2E Pipeline Tests

**Status:** ✅ Complete
**Size:** Large
**Files to Create:** 1
**Prerequisites:** Phase 1 (Infrastructure) + Phase 2 (SDK Lifecycle) complete
**Entry Point:** `Hugh.Tests/E2E/PipelineTests.cs` (new file)
**Verification:** `dotnet test --filter "Category=E2E"` runs tests (or skips if SDK unavailable); existing tests pass

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 6.1 | Create `PipelineTests.cs` class extending `SdkTestBase` with `[Trait("Category", TestCategory.E2E)]`. Create `HybridLoopOrchestrator` inner class extending `LoopOrchestrator` — overrides `DispatchPhaseAsync` to use real `CopilotService` for Vision/Research and return controlled `PhaseResult` for Planning/Review/Coding. Follow `TestableLoopOrchestrator` pattern from `PhaseRetryTests.cs` | `Hugh.Tests/E2E/PipelineTests.cs` | Hybrid orchestrator compiles; real SDK for Vision/Research, mock for others |
| 6.2 | Add `Vision_SinglePhase_ProducesDocument` test: execute Vision phase with real `CopilotService`, verify `PhaseResult.Success == true`, `OutputDocumentPath` exists, file has markdown content. Assert structural properties (file exists, non-empty) not exact content | `Hugh.Tests/E2E/PipelineTests.cs` | Vision document created; `PhaseResult` fields populated |
| 6.3 | Add `Vision_Phase_PopulatesToolMetrics` test: execute Vision, verify `ToolMetrics != null` and `TotalExecutions > 0` (or assert not null if SDK version emits events) | `Hugh.Tests/E2E/PipelineTests.cs` | ToolMetrics populated from real SDK events |
| 6.4 | Add `PhaseResult_Duration_IsPositive` test: execute any real phase, assert `Duration > TimeSpan.Zero` and `Duration < timeout`. Add `PhaseResult_Success_TrueOnNormalCompletion` — verify `ErrorMessage` is null on success | `Hugh.Tests/E2E/PipelineTests.cs` | Duration and success fields correctly populated |
| 6.5 | Add `MultiPhase_VisionThenResearch_StateTransitionsCorrectly` test: use `HybridLoopOrchestrator` with real Vision, real Research, mock Planning/Review/Coding. Verify `state.CurrentPhase` advances, `CompletedPhases` updated, `Documents.VisionPath` and `Documents.ResearchPath` populated | `Hugh.Tests/E2E/PipelineTests.cs` | State machine transitions correctly with real SDK output |
| 6.6 | Add `MultiPhase_DocumentOutputValidation_FailsWhenMissing` test: use mock orchestrator where Vision returns `OutputDocumentPath = null`. Verify loop fails with appropriate error. Follow existing pattern from `PhaseRetryTests.cs` | `Hugh.Tests/E2E/PipelineTests.cs` | Missing document path causes loop failure |
| 6.7 | Add `Cancellation_GlobalCts_PropagatesGracefully` test: start Vision phase with real SDK, cancel after 2s via `CancellationTokenSource.Cancel()`. Verify `OperationCanceledException` or failed `PhaseResult` | `Hugh.Tests/E2E/PipelineTests.cs` | Cancellation propagates; no hanging |
| 6.8 | Add `Cancellation_PerPhaseTimeout_ReturnsFailedResult` test: set Vision timeout to 1s (artificially short). Verify `PhaseResult.Success == false` with "timed out" in `ErrorMessage` | `Hugh.Tests/E2E/PipelineTests.cs` | Per-phase timeout produces failed result |
| 6.9 | Add `PhaseResult_CompactionOccurred_FalseForShortSessions` test: short Vision session, verify `CompactionOccurred == false` (context not exhausted) | `Hugh.Tests/E2E/PipelineTests.cs` | Compaction tracking accurate for short sessions |
| 6.10 | Add `PhaseResult_RetryAttemptsMade_ZeroOnFirstSuccess` test: successful first attempt, verify `RetryAttemptsMade == 0` | `Hugh.Tests/E2E/PipelineTests.cs` | Retry counter accurate |
| 6.11 | Add `MultiPhase_ResumeFromFailedPhase_ContinuesCorrectly` test: create state with Vision complete, Research failed. Resume with hybrid orchestrator. Verify Research re-attempted (not Vision) | `Hugh.Tests/E2E/PipelineTests.cs` | Resume skips completed phases; retries failed phase |

## Standards

No organizational standards applicable to this plan.

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | 3 existing files (Program.cs, CopilotService.cs, release.yml) + migration of IntegrationTests.cs |
| New patterns introduced | 2 | `SdkTestBase`/`SdkTestRetry`/`TokenBudgetTracker` are new but follow established IDisposable + temp dir patterns |
| External dependencies | 2 | Real Copilot SDK + gh CLI for LiveSdk/E2E tests; graceful skip when unavailable |
| Migration complexity | 1 | Silent skip migration is low-risk; access modifier changes are trivial |
| Test coverage required | 4 | ~104-114 new tests across 5 test classes + infrastructure |
| **Overall Complexity** | **12/25** | **Medium** — well-scoped with clear phases and existing patterns to follow |

## Review Summary

**Review Date:** 2025-02-23  
**Reviewer:** pch-plan-reviewer  
**Original Plan Version:** v2.0  
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 3 (Critical: 0, Major: 2, Minor: 1)
- Clarifying Questions Asked: 2
- Sections Updated: Phase 2 prerequisites, Phase 5 Steps 5.1-5.2, Phase Dependencies table, Requirements 7 and 8

### Key Improvements Made
1. **Consolidated `--no-interactive` bug fix** — Identified a third missing fix location (`--check` path, line 180) and merged all three locations into a single Step 5.2
2. **Resolved `TryEnableInfiniteSession` testability** — Steps 2.6/2.7 referenced a `private` method; added access modifier change to Step 5.1 and updated Phase 2 prerequisites to reflect the dependency
3. **Updated phase dependency graph** — Phase 2 now correctly depends on Phase 5 Step 5.1 for `TryEnableInfiniteSession` access

### Remaining Considerations
- Phase execution order is non-intuitive (Phase 5 should execute before Phases 2-4 due to access modifier changes); implementers should follow the Phase Dependencies table, not sequential numbering
- `TryEnableInfiniteSession` is an instance method (not static) — tests in Steps 2.6/2.7 will need a `CopilotService` instance with test config to invoke it
- The minor issue of phase ordering is documented but not restructured, to preserve the original plan’s logical grouping (infrastructure → tests → fixes → E2E)

### Sign-off
This plan has been reviewed and is **Ready for Implementation**

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2025-02-23 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2025-02-23 |
| Status | ✅ Complete |
| Next Agent | — (implementation complete) |
| Plan Location | /docs/plans/025-hugh-sdk-test-automation-expansion.md |
## Implementation Notes

### Plan Completion

**All phases completed:** 2025-07-17
**Execution Order:** Phase 5 → Phase 1 → Phase 2 → Phase 3 → Phase 4 → Phase 6 (following dependency graph)
**Total tasks completed:** 53
**Total files created:** 10
**Total files modified:** 5

### Phase Execution Summary

| Phase | Name | Files Created | Files Modified | Tests Added |
|-------|------|---------------|----------------|-------------|
| 5 | Bug Fixes & CI Update | 0 | 4 (Program.cs, CopilotService.cs, release.yml, README.md) | 0 |
| 1 | Test Infrastructure | 6 | 2 (IntegrationTests.cs, Hugh.Tests.csproj) | 0 |
| 2 | SDK Lifecycle Tests | 1 (SdkSessionLifecycleTests.cs) | 0 | 11 |
| 3 | Workflow & Signal Tests | 2 (LiveWorkflowToolTests.cs, LiveDualDetectionTests.cs) | 0 | 11 |
| 4 | CLI Smoke Tests | 1 (CliSmokeTests.cs) | 0 | 14 |
| 6 | E2E Pipeline Tests | 1 (PipelineTests.cs) | 0 | 11 |

### Deviations from Plan

1. **xUnit v2 Skip API**: Plan specified `Assert.Skip()` (xUnit v3 only). Implemented with `Xunit.SkippableFact` v1.4.13 package — all skippable tests use `[SkippableFact]` attribute + `Skip.IfNot()` instead. This is the correct approach for xUnit 2.9.2.
2. **`SdkSessionConfig` alias**: Used `using SdkSessionConfig = GitHub.Copilot.SDK.SessionConfig` to disambiguate from Hugh's internal types.
3. **`SendAsync` API**: `CopilotSession.SendAsync` requires `new MessageOptions { Prompt = "..." }` not raw string.

### Test Suite Summary

**Final test count:** 752 total (725 passed, 27 skipped, 0 failures)
- Previous baseline: ~705 tests
- New tests added: 47
- SDK-dependent tests (skip without SDK): 27

### Code Review

Post-implementation code review: clean — no issues found.