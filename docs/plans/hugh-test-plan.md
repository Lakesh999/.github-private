# Hugh Test Plan

## Overview

This document describes the testing strategy for Hugh, the self-operating development loop CLI. It covers all test levels — unit, integration, and end-to-end — along with the process for running tests, evaluating results, and validating changes before release.

**Current baseline:** 705 tests, all passing (xUnit on .NET 9.0)

---

## Table of Contents

1. [Test Infrastructure](#1-test-infrastructure)
2. [Test Categories](#2-test-categories)
3. [Running Tests](#3-running-tests)
4. [Component Coverage Map](#4-component-coverage-map)
5. [Unit Test Details](#5-unit-test-details)
6. [Integration Test Details](#6-integration-test-details)
7. [Manual / E2E Verification](#7-manual--e2e-verification)
8. [Success Criteria](#8-success-criteria)
9. [Pre-Release Checklist](#9-pre-release-checklist)
10. [Adding New Tests](#10-adding-new-tests)

---

## 1. Test Infrastructure

| Item | Detail |
|------|--------|
| **Test framework** | xUnit 2.9.2 |
| **Mocking library** | NSubstitute 5.3.0 |
| **Code coverage** | coverlet.collector 6.0.2 |
| **Test project** | `Hugh.Tests/Hugh.Tests.csproj` (targets .NET 9.0) |
| **Main project** | `Hugh/Hugh.csproj` (.NET 8.0, exposes internals via `InternalsVisibleTo`) |
| **CI runner** | `windows-latest` in `.github/workflows/release.yml` |

### Key Design Principles

- **Offline by default** — Unit tests mock `ICopilotService` and `IProcessRunner` so they never call the GitHub Copilot SDK or shell out to external tools.
- **Temp directory isolation** — Every test that touches the filesystem creates a unique temp directory and cleans it up via `IDisposable`. No shared mutable state.
- **Regex permanence** — Signal classifier regexes are tested extensively because they serve dual environments (VS Code Copilot chat + Hugh CLI) and must not regress.

---

## 2. Test Categories

| Category | Trait | Purpose | Example Test Files |
|----------|-------|---------|--------------------|
| **Unit** | (default) | Tests a single class in isolation with mocked dependencies | `SignalClassifierTests.cs`, `SdkResponseProcessorTests.cs` |
| **Integration** | `[Trait("Category", "Integration")]` | Tests component interactions with real implementations (may depend on system state) | `SetupIntegrationTests.cs`, `IntegrationTests.cs` |
| **Orchestration** | (subfolder) | Tests workflow orchestration logic including state transitions and retry behavior | `SolutionOrchestratorTests.cs`, `PhaseRetryTests.cs`, `SubPhaseBuildVerificationTests.cs` |
| **Setup** | (subfolder) | Tests prerequisite detection, installer registry, and setup orchestration | `GitHubCliDetectorTests.cs`, `SetupOrchestratorTests.cs` |

---

## 3. Running Tests

### Run all tests

```bash
dotnet test Hugh.Tests/Hugh.Tests.csproj
```

### Run with verbose output

```bash
dotnet test Hugh.Tests/Hugh.Tests.csproj --verbosity normal
```

### Run a specific test class

```bash
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "FullyQualifiedName~SignalClassifierTests"
```

### Run only integration tests

```bash
dotnet test Hugh.Tests/Hugh.Tests.csproj --filter "Category=Integration"
```

### Run with code coverage

```bash
dotnet test Hugh.Tests/Hugh.Tests.csproj --collect:"XPlat Code Coverage"
```

### Successful outcome

```
Test Run Successful.
Total tests: 705
     Passed: 705
 Total time: ~11 seconds
```

All 705 tests must pass with zero failures and zero skipped. Any failure is a blocking issue.

---

## 4. Component Coverage Map

This table maps each Hugh component to its corresponding test file(s) and the areas covered.

| Component | Test File(s) | What Is Tested |
|-----------|-------------|----------------|
| **SignalClassifier** | `SignalClassifierTests.cs` | All signal types: PhaseComplete, WorkflowComplete, Blocker, QuestionWithRecommendation, ModeSelection, HandoffRequest. Regex pattern matching, edge cases, spacing variations. |
| **SdkResponseProcessor** | `SdkResponseProcessorTests.cs` | Tool-first priority with text fallback, event handling (ToolExecution, SessionCompaction), path extraction from agent output, metrics population, null-guard constructors. |
| **DocumentPathExtractor** | `DocumentPathExtractorTests.cs` | Extraction of document paths from various agent response formats. |
| **LoopStateManager** | `LoopStateManagerTests.cs` | State creation, persistence to disk, reload across process boundaries, phase transitions, failure recording, resume capability. |
| **SolutionOrchestrator** | `Orchestration/SolutionOrchestratorTests.cs` | Triage classification regex (FEATURE vs SOLUTION), state management delegation, `HasExistingSolution` routing. |
| **SolutionStateManager** | `Orchestration/SolutionStateManagerTests.cs` | Solution-level state persistence and loading. |
| **PhaseExecutor (retry)** | `Orchestration/PhaseRetryTests.cs` | Phase retry behavior, max retry limits, exponential backoff. |
| **SubPhase build verify** | `Orchestration/SubPhaseBuildVerificationTests.cs` | Build verification per sub-phase, skip when disabled, repair-on-failure, repair failure error path. |
| **CopilotService (retry)** | `CopilotServiceRetryTests.cs` | Retry logic with exponential backoff for transient failures. |
| **SessionErrorClassifier** | `SessionErrorClassifierTests.cs` | Error categorization into 6 types: authentication, rate limiting, context overflow, network, SDK, unknown. |
| **QuestionHandler** | `QuestionHandlerTests.cs` | Delegation pattern: text-detected questions converted to `UserInputRequest` and delegated to `SdkQuestionHandler`. |
| **SdkQuestionHandler** | `SdkQuestionHandlerTests.cs` | Auto-accept when recommendation present and `AcceptRecommendationsOnly` is true; pause for open-ended questions; `QuestionResponse` construction. |
| **QuestionResponse** | `QuestionResponseTests.cs` | Model behavior: `ShouldPause`, `Response`, `DecisionMade` properties. |
| **BlockerHandler** | `BlockerAutoSkipTests.cs` | Blocker auto-skip logic, retry/skip/stop decision routing. |
| **HughWorkflowTools** | `HughWorkflowToolsTests.cs` | Custom tool definitions (`hugh_notify_phase_complete`, `hugh_report_blocker`, `hugh_notify_workflow_complete`, `hugh_get_workspace_structure`) and their result models. |
| **FileSystemHooks** | `FileSystemHooksTests.cs` | Pre-tool-use directory auto-creation, post-tool-use logging, SDK session hook integration. |
| **ToolMetricsCollector** | `ToolMetricsCollectorTests.cs` | Tool invocation counting, timing, error rate tracking. |
| **PathResolver** | `PathResolverTests.cs` | Path resolution for dev vs installed scenarios (`HUGH_CONFIG_PATH` → assembly location → CWD). |
| **PhaseResult** | `Models/PhaseResultTests.cs` | Phase result model construction and properties. |
| **SolutionState** | `Models/SolutionStateTests.cs` | Solution state model serialization and properties. |
| **ReleaseRoadmapParser** | `Detection/ReleaseRoadmapParserTests.cs` | Parsing of release roadmap documents into structured data. |
| **BuiltInPromptLoader** | `Prompts/BuiltInPromptLoaderTests.cs` | Loading of embedded agent prompts from the built-in prompts directory. |
| **AgentDiscoveryDetector** | `Agents/Discovery/AgentDiscoveryDetectorTests.cs` | Agent discovery detection logic. |
| **GitHubAgentDiscoveryService** | `Agents/Discovery/GitHubAgentDiscoveryServiceTests.cs` | GitHub-based agent prompt discovery and sync. |
| **GitHubCliDetector** | `Setup/GitHubCliDetectorTests.cs` | `gh` CLI presence detection via `MockProcessRunner`. |
| **CopilotCliDetector** | `Setup/CopilotCliDetectorTests.cs` | `gh copilot` extension detection. |
| **GitHubAuthDetector** | `Setup/GitHubAuthDetectorTests.cs` | `gh auth status` detection. |
| **InstallerRegistry** | `Setup/InstallerRegistryTests.cs` | Installer mapping and priority ordering. |
| **SetupOrchestrator** | `Setup/SetupOrchestratorTests.cs` | Setup orchestration flow with detectors and installers. |
| **Integration (E2E sim)** | `IntegrationTests.cs` | Full workflow simulation using mock `CopilotService`: state transitions across all 5 phases, failure and resume, decision tracking, signal classification integration. |
| **Setup Integration** | `Setup/SetupIntegrationTests.cs` | Real system integration: actual `gh` CLI detection, prerequisite report rendering, setup orchestrator with all detectors. |

---

## 5. Unit Test Details

### 5.1 Signal Classification (`SignalClassifierTests.cs`)

**Purpose:** Validates that agent text output is correctly classified into signal types that drive the orchestration state machine.

**What success looks like:**
- Every signal type (`PhaseComplete`, `WorkflowComplete`, `Blocker`, `QuestionWithRecommendation`, `ModeSelection`, `HandoffRequest`, `Unknown`) is correctly identified.
- Phase numbers are extracted accurately from phase-complete signals.
- Regex patterns tolerate whitespace and formatting variations.
- Unknown or malformed input returns `SignalType.Unknown` (never throws).

### 5.2 SDK Response Processing (`SdkResponseProcessorTests.cs`)

**Purpose:** Validates the dual-signal processing pipeline — tool events take priority, text regex serves as fallback.

**What success looks like:**
- Tool execution events are processed before text analysis.
- `ToolExecution` events correctly populate `PhaseResult` fields.
- `SessionCompaction` events are handled gracefully.
- Document paths are extracted from both tool results and text output.
- Metrics collector is populated from tool execution events.
- Null dependencies throw `ArgumentNullException` at construction time.

### 5.3 State Management (`LoopStateManagerTests.cs`)

**Purpose:** Validates that run state is correctly persisted and can be resumed across process restarts.

**What success looks like:**
- New runs are created with `Phase = Vision`, `Status = NotStarted`.
- State survives serialization/deserialization to disk.
- Phase transitions follow the correct order (Vision → Research → Planning → Review → Coding).
- Failed states record failure reasons and can be resumed.
- State files are written to the `.orchestrator/` directory.

### 5.4 Question Handling (`SdkQuestionHandlerTests.cs`, `QuestionHandlerTests.cs`)

**Purpose:** Validates the autonomous decision-making pipeline where Hugh auto-accepts recommendations or pauses for human input.

**What success looks like:**
- When `AcceptRecommendationsOnly = true` and a recommendation is present, the handler auto-selects it and returns `ShouldPause = false`.
- When no recommendation is present, the handler returns `ShouldPause = true`.
- `QuestionHandler` (text-based) delegates all logic to `SdkQuestionHandler` (single source of truth).
- Autonomous decisions are recorded with full audit trail.

### 5.5 Orchestration (`Orchestration/` subfolder)

**Purpose:** Validates higher-level workflow logic including solution triage, phase retry, and build verification.

**What success looks like:**
- Triage regex correctly classifies `FEATURE` vs `SOLUTION` responses (case-insensitive).
- Phase retries respect `MaxPhaseRetries` configuration.
- Build verification runs `dotnet build` per sub-phase when enabled.
- Build failures trigger repair attempts.
- Retry exhaustion produces an error result (not an infinite loop).

### 5.6 Setup & Prerequisites (`Setup/` subfolder)

**Purpose:** Validates the prerequisite detection system that checks for `gh`, `gh copilot`, GitHub auth, agent files, and workspace structure.

**What success looks like:**
- Each detector returns a `PrerequisiteStatus` with the correct `PrerequisiteId`.
- Missing tools return `State = Missing` (not an exception).
- `MockProcessRunner` enables offline testing of process-dependent detectors.
- `InstallerRegistry` maps prerequisites to the correct installer with priority ordering.
- `SetupOrchestrator` runs all detectors and aggregates results.

---

## 6. Integration Test Details

### 6.1 Workflow Simulation (`IntegrationTests.cs`)

**Purpose:** Simulates the full Hugh pipeline using mocked `CopilotService` to verify phase transitions and state management work together.

**Key scenarios tested:**

| Scenario | What It Verifies |
|----------|-----------------|
| Vision → Research transition | State advances correctly after phase completion |
| Full 5-phase workflow | All phases complete in order; all appear in `CompletedPhases` |
| Failure and resume | Failed state is persisted; new process loads and resumes from failure point |
| Decision tracking | Multiple autonomous decisions are recorded and retrievable |
| Signal classification integration | Real `SignalClassifier` correctly classifies various agent outputs |
| Mock phase responses | Vision, Research, Planning phases return expected `PhaseResult` data |

**What success looks like:**
- State transitions follow the exact sequence: Vision → Research → Planning → Review → Coding → Complete.
- Failure states are recoverable — a new `LoopStateManager` instance in a separate test loads the persisted state and resumes.
- Decision objects accumulate correctly with all fields populated.

### 6.2 Setup Integration (`Setup/SetupIntegrationTests.cs`)

**Purpose:** Tests the prerequisite system against the real environment (actual `gh` CLI, auth status, etc.).

**Note:** These tests depend on system state. On a developer machine with `gh` installed and authenticated they will detect real versions. In CI they depend on the runner's toolchain.

**What success looks like:**
- Detectors run without throwing exceptions regardless of whether the tool is installed.
- Results include version information when tools are found.
- The setup orchestrator aggregates all detector results into a single report.
- Non-interactive UI renders reports without hanging on prompts.

---

## 7. Manual / E2E Verification

Some aspects of Hugh cannot be fully validated by automated tests due to external dependencies (GitHub Copilot SDK, live LLM sessions). These require manual verification.

### 7.1 Prerequisites Check

```bash
hugh doctor
```

**Expected outcome:**
- Table showing status of each prerequisite (github-cli, copilot-cli, github-auth, agents, workspace).
- All items show ✅ on a properly configured machine.
- Exit code `0` when all satisfied, `1` when any are missing.

### 7.2 New Feature Run (Smoke Test)

```bash
hugh "Add a hello world endpoint"
```

**Expected outcome:**
1. Prerequisite check passes.
2. Agent discovery syncs prompts (or falls back to bundled).
3. SolutionOrchestrator triages the request as `FEATURE`.
4. Pipeline executes: Vision → Research → Planning → Review → Coding.
5. Each phase shows a post-phase report in the terminal.
6. State is persisted to `.orchestrator/` after each phase.
7. Final message: "Implementation completed successfully."

### 7.3 Resume After Interruption

1. Start a run: `hugh "Add logging middleware"`
2. Press `Ctrl+C` during Research phase.
3. Resume: `hugh --resume`

**Expected outcome:**
- Graceful shutdown message appears on `Ctrl+C`.
- Resume picks up from the interrupted phase (not from the beginning).
- Run completes normally from the resume point.

### 7.4 Status Check

```bash
hugh --status
```

**Expected outcome:**
- Displays current run state including phase, status, and elapsed time.
- Shows "No active runs found" if no state file exists.

### 7.5 Verbose Mode

```bash
hugh --verbose "Add unit test"
```

**Expected outcome:**
- Debug-level log output appears in the console.
- Log file written to `~/.hugh/logs/` with full detail.

### 7.6 Timeout Override

```bash
hugh --timeout 00:05:00 "Quick feature"
```

**Expected outcome:**
- All phase timeouts are overridden to the specified value.
- Phases that exceed the timeout are cancelled gracefully.

### 7.7 Non-Interactive Mode

```bash
hugh --no-interactive "Add feature"
```

**Expected outcome:**
- No interactive prompts appear (Vision phase runs autonomously).
- Suitable for CI/CD usage where stdin is redirected.

---

## 8. Success Criteria

### Automated Tests

| Criterion | Threshold |
|-----------|-----------|
| All tests pass | 705/705 (zero failures) |
| No tests skipped | 0 skipped |
| Test execution time | < 30 seconds total |
| No test isolation leaks | Each test cleans up its temp directory |
| Build succeeds with zero warnings | `dotnet build` produces 0 warnings, 0 errors |

### Pre-Merge Validation

Before merging any PR:

1. **`dotnet build`** — zero errors, zero warnings.
2. **`dotnet test`** — all tests pass.
3. **No regressions in signal classification** — regex patterns must not change behavior for existing signal formats.
4. **Dual-environment compatibility** — agent prompts retain both tool-based and text-based signal formats.
5. **State serialization backward compatibility** — changes to `LoopState` or `SolutionState` models must not break deserialization of existing state files.

### Release Gate

The release workflow (`.github/workflows/release.yml`) runs `dotnet test` as a gate before publishing artifacts. A test failure blocks the release.

---

## 9. Pre-Release Checklist

| # | Check | Command / Action | Pass if... |
|---|-------|-----------------|------------|
| 1 | Build in Release mode | `dotnet build -c Release` | Zero errors, zero warnings |
| 2 | Run full test suite | `dotnet test Hugh.Tests/Hugh.Tests.csproj` | All tests pass |
| 3 | Verify prerequisite detection | `dotnet run --project Hugh -- doctor` | All prerequisites satisfied |
| 4 | Smoke test: new feature | `dotnet run --project Hugh -- "Add hello endpoint"` | Pipeline completes all phases |
| 5 | Smoke test: resume | Interrupt a run, then `dotnet run --project Hugh -- --resume` | Resumes from correct phase |
| 6 | Smoke test: status | `dotnet run --project Hugh -- --status` | Shows run state or "No active runs" |
| 7 | Verify log output | Check `~/.hugh/logs/` | Log file created with expected entries |
| 8 | Installer build | `.github/workflows/release.yml` (tag push) | Inno Setup builds without error |
| 9 | Portable ZIP | Same workflow | Self-contained binary zipped correctly |

---

## 10. Adding New Tests

### Conventions

1. **File naming:** `{ClassName}Tests.cs` — matches the class under test.
2. **Namespace:** `Hugh.Tests` for root-level tests; `Hugh.Tests.{Subfolder}` for organized tests.
3. **Test isolation:** Create a unique temp directory in the constructor, clean up in `Dispose()`.
4. **Mocking:** Use `NSubstitute` — call `Substitute.For<IInterface>()` for service mocks, `MockProcessRunner` for CLI process mocks.
5. **No shared state:** Tests must not depend on execution order or side effects of other tests.
6. **Theory data:** Use `[Theory]` with `[InlineData]` for parameterized tests covering multiple input variations.

### Template

```csharp
using NSubstitute;

namespace Hugh.Tests;

public class MyComponentTests : IDisposable
{
    private readonly string _testDir;

    public MyComponentTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), $"HughTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDir))
        {
            try { Directory.Delete(_testDir, recursive: true); }
            catch { }
        }
    }

    [Fact]
    public void MyMethod_WhenCondition_ExpectedOutcome()
    {
        // Arrange
        var dependency = Substitute.For<IMyDependency>();
        var sut = new MyComponent(dependency);

        // Act
        var result = sut.DoSomething();

        // Assert
        Assert.True(result.Success);
    }
}
```

### When to Add Tests

- **New class or method** — Add unit tests covering the happy path, error cases, and edge cases.
- **Bug fix** — Add a regression test that reproduces the bug before fixing it.
- **Regex or signal changes** — Add `[Theory]` cases covering all known input patterns.
- **State model changes** — Add serialization round-trip tests to ensure backward compatibility.
- **New agent tool** — Add tests in `HughWorkflowToolsTests.cs` for tool definitions and result models.
