---
id: "022"
type: research
title: "Hugh SDK Test Automation Expansion"
status: ✅ Complete
created: "2025-02-23"
current_phase: "5 of 5"
---

## Introduction

With SDK access available in the test environment, Hugh's test suite can be expanded beyond mock-based unit tests to include live SDK integration tests. This research investigates what new automated tests should be added — covering real CopilotClient/Session lifecycle, workflow tool integration, signal detection with actual LLM responses, CLI entry-point smoke tests, and end-to-end pipeline validation using the live Copilot SDK.

## Objectives

- Identify gaps in the current 705-test suite that SDK access would close
- Determine which manual verification scenarios (test plan Section 7) can become automated SDK tests
- Define test patterns for real SDK session lifecycle (client start, session create/dispose, compaction)
- Evaluate how to test workflow tools (`hugh_*`) with real SDK sessions
- Assess signal classification accuracy against actual LLM output (not just regex patterns)
- Design tests for CLI entry-point paths (`--status`, `--resume`, `--check`, `--verbose`, `--timeout`)
- Determine appropriate timeouts, retry strategies, and flakiness mitigation for live SDK tests

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | SDK Session Lifecycle Tests | ✅ Complete | CopilotClient start/stop; session creation with SessionConfig; DisposeAsync cleanup; compaction trigger behavior; error handling for auth/network failures | 2025-02-23 |
| 2 | Workflow Tools & Signal Detection with Live SDK | ✅ Complete | `hugh_*` workflow tools registered in real sessions; signal classification against actual LLM responses; tool-first vs text-fallback detection accuracy; FileSystemHooks callbacks in real sessions | 2025-02-23 |
| 3 | CLI Entry-Point Smoke Tests | ✅ Complete | `hugh doctor` exit codes and output; `--status` with/without state file; `--resume` from persisted state; `--verbose` log file creation; `--timeout` override behavior; `--no-interactive` mode | 2025-02-23 |
| 4 | End-to-End Pipeline Tests | ✅ Complete | Single-phase execution with real agents (Vision, Research); multi-phase state transitions with real SDK; cancellation/timeout mid-phase; PhaseResult field population from real responses | 2025-02-23 |
| 5 | Test Infrastructure & Flakiness Mitigation | ✅ Complete | SDK test trait/category conventions; timeout tuning for CI vs local; retry strategies for transient SDK failures; test isolation patterns for real SDK; conditional skip vs hard requirement; cost/token budget controls | 2025-02-23 |

## Phase 1: SDK Session Lifecycle Tests

**Status:** ✅ Complete  
**Session:** 2025-02-23

### 1. CopilotClient Start/Stop Lifecycle

`CopilotService` manages the `CopilotClient` lifecycle via lazy initialization in `EnsureClientStartedAsync` (lines 94–145). Key behaviors to test with real SDK:

- `CopilotClient()` constructor succeeds when the Copilot standalone binary is installed
- `StartAsync()` establishes connection to the Copilot process
- Idempotency: calling `EnsureClientStartedAsync` twice doesn't create two clients
- Error paths: missing binary → `InvalidOperationException` with install guidance, auth failure → message with `gh auth login` guidance, timeout → network message

**Current gap:** Zero tests verify real `CopilotClient` instantiation. The existing `CopilotService_WithRealCLI_ExecutesPrompt` in `IntegrationTests.cs` skips if CLI unavailable and tests `ExecutePhaseAsync` end-to-end — it doesn't isolate client start/stop.

**Proposed tests (~3):**
- `CopilotClient_Start_Succeeds` — Verify `new CopilotClient()` + `StartAsync()` completes
- `CopilotClient_Start_Idempotent` — Call `EnsureClientStartedAsync` twice, verify single client
- `CopilotClient_Dispose_CleansUp` — Start then dispose, verify `_clientStarted` resets

### 2. Session Creation with SessionConfig

Session creation in `CreateSessionAsync` (lines 658–694) builds a `SdkSessionConfig` with Model, SystemMessage (Append mode), Hooks (`FileSystemHooks.CreateHooks()`), Tools (`HughWorkflowTools.CreateToolSet()` — 4 `AIFunction` instances), and InfiniteSession (reflection-based).

**Current gap:** No tests create a real `CopilotSession` with a populated `SessionConfig`.

**Proposed tests (~4):**
- `CreateSession_WithFullConfig_Succeeds` — Create session with model, system message, tools, hooks
- `CreateSession_ToolsRegistered_FourTools` — Verify `CreateToolSet()` returns 4 AIFunction instances accepted by session
- `CreateSession_HooksAttached_NoError` — Verify hooks wire correctly
- `Session_ReceivesEvents_OnPrompt` — Send simple prompt, verify `SessionIdleEvent` fires

### 3. DisposeAsync Cleanup

`DisposeAsync` (lines 147–161) is idempotent (`_disposed` guard), calls `CopilotClient.DisposeAsync()`, resets state (`_client = null`, `_clientStarted = false`), and disposes `_loggerFactory`. Sessions use `await using` pattern in `ExecutePhaseAsync`.

**Current gap:** No tests verify `DisposeAsync` on real `CopilotService` instances.

**Proposed tests (~5):**
- `Dispose_AfterStart_CleansUpClient` — Start client, dispose, verify clean state
- `Dispose_CalledTwice_Idempotent` — Double-dispose doesn't throw
- `Dispose_WithoutStart_Succeeds` — Dispose on never-started service doesn't throw
- `Session_DisposeViaAwaitUsing_NoLeaks` — Create session in `await using`, verify cleanup
- `PostDispose_ExecutePhase_ThrowsOrFails` — After disposal, operations should fail gracefully

### 4. Compaction Trigger Behavior

`TryEnableInfiniteSession` (lines 699–752) uses reflection to set `InfiniteSessionConfig.CompactionThreshold` on SDK v0.1.22's `SessionConfig`. Compaction events are tracked in `SdkResponseProcessor.ProcessResponseAsync` (lines 262–275) via `SessionCompactionStartEvent`/`SessionCompactionCompleteEvent` type name checks, flowing into `PhaseResult.CompactionOccurred`.

**Current gap:** No tests verify the reflection-based setup succeeds on real SDK, nor that compaction events are correctly tracked.

**Proposed tests (~3):**
- `InfiniteSession_ConfiguredViaReflection_Succeeds` — Reflection setup doesn't throw, sets property
- `InfiniteSession_CompactionThreshold_Applied` — Threshold value (0.8) correctly set
- `Session_CompactionOccurred_TrackedInResult` — Would need very long context (expensive, potentially flaky)

**Practical note:** Actual compaction triggering requires filling ~80% context window — expensive in tokens. Most practical tests verify the reflection path and config wiring.

### 5. Error Handling for Auth/Network Failures

Client startup error handling (lines 109–144) has 4 distinct exception wrapping paths based on message content analysis. Post-connection errors use `SendWithRetryAsync` (lines 788–826) with `SessionErrorClassifier` (6 categories: Authentication, RateLimited, Transient, ContextExhausted, SdkError, Unknown).

**Current coverage:** `SessionErrorClassifierTests.cs` (370 lines) thoroughly tests classifier patterns. `CopilotServiceRetryTests.cs` tests `CalculateRetryDelay`. Both are mock-based.

**Current gap:** No test verifies real SDK auth/network failures produce classifiable errors.

**Proposed tests (~2-3):**
- `EnsureClientStarted_WithInvalidAuth_ThrowsAuthError` — Intentionally misconfigure credentials
- `Session_ErrorEvent_ProducesClassifiableError` — Verify real SDK errors match classifier patterns

### Test Gap Matrix Summary

| Lifecycle Area | Current Tests | Proposed New Tests |
|---|---|---|
| CopilotClient start | 0 unit, 1 integration (skippable) | 3 |
| SessionConfig creation | 0 | 4 |
| DisposeAsync cleanup | 0 | 5 |
| Compaction configuration | 0 | 3 |
| Auth error handling | Unit tests for classifier only | 2-3 |
| Network error handling | Unit tests for retry math only | 1-2 |
| **Total** | | **~18-20** |

**Key Discoveries:**
- The existing `CopilotService_WithRealCLI_ExecutesPrompt` is the *only* test touching the real SDK, using `return;` as skip (appears "passed" not "skipped")
- `CopilotService.CreateSessionAsync` is `internal`, accessible via `InternalsVisibleTo` — key entry point for isolated tests
- `TryEnableInfiniteSession` uses reflection for `InfiniteSessionConfig` — fragile to SDK version changes, SDK test would catch breakage
- `HughWorkflowTools.CreateToolSet()` returns 4 `AIFunction` instances via `AIFunctionFactory.Create`
- `FileSystemHooks.CreateHooks()` produces `PreToolUseHookOutput` callbacks with `"allow"` permission
- Test project targets .NET 9.0 while Hugh targets .NET 8.0 — SDK tests need compatibility verification

| File | Relevance |
|------|----------|
| `Hugh/Agents/CopilotService.cs` | Primary SDK lifecycle manager (1031 lines) |
| `Hugh/Agents/SdkResponseProcessor.cs` | Event processing, compaction tracking (553 lines) |
| `Hugh/Configuration/HughConfig.cs` | Config model with CopilotConfig (240 lines) |
| `Hugh/Hooks/FileSystemHooks.cs` | Pre/post tool use hooks (362 lines) |
| `Hugh/Tools/HughWorkflowTools.cs` | 4 workflow tools via AIFunctionFactory (228 lines) |
| `Hugh/Detection/SessionErrorClassifier.cs` | 6-category error classification (255 lines) |
| `Hugh.Tests/IntegrationTests.cs` | Existing integration tests (636 lines) |
| `Hugh.Tests/SessionErrorClassifierTests.cs` | Error classification unit tests (370 lines) |
| `Hugh.Tests/CopilotServiceRetryTests.cs` | Retry delay tests (202 lines) |

**Gaps:** None  
**Assumptions:** Copilot standalone binary is installed on the test machine; `InfiniteSessionConfig` property exists in SDK v0.1.22

## Phase 2: Workflow Tools & Signal Detection with Live SDK

**Status:** ✅ Complete  
**Session:** 2025-02-23

### 1. `hugh_*` Workflow Tools in Real Sessions

`CopilotService.CreateSessionAsync` registers all 4 workflow tools via `HughWorkflowTools.CreateToolSet()` into `SessionConfig.Tools`. The 4 tools (`hugh_notify_phase_complete`, `hugh_notify_workflow_complete`, `hugh_report_blocker`, `hugh_get_workspace_structure`) are created via `AIFunctionFactory.Create()`. Existing mock tests in `HughWorkflowToolsTests.cs` (299 lines, ~25 tests) validate C# method return values and tool metadata, but **no tests verify the SDK actually discovers and invokes these tools**.

**Proposed tests (~8-10) — `LiveWorkflowToolRegistrationTests`:**
- `Session_RegistersWorkflowTools_ToolsVisibleInSession` — Real session with tools, verify all 4 registered
- `Session_AgentCanCall_NotifyPhaseComplete` — Prompt agent to call tool, verify `ToolExecutionCompleteEvent`
- `Session_AgentCanCall_ReportBlocker` — Same for blocker reporting
- `Session_AgentCanCall_GetWorkspaceStructure` — Verify result includes directory tree
- `Session_AgentCanCall_NotifyWorkflowComplete` — Same for workflow completion
- `Session_ToolCallSetsBlockerMetadata` — After `ReportBlocker(isSkippable: true)`, verify `LastBlockerIsSkippable` and `LastBlockerReason`
- `Session_MultipleToolCalls_MetricsCollected` — Multiple tool calls, verify `ToolMetricsCollector` records all
- `Session_ToolNotRegistered_AgentCannotCallIt` — Session WITHOUT tools, agent can't invoke `hugh_*`

Timeout: Each tool-invocation test requires LLM round trip (~5-30s). Set per-test timeout to 120s.

### 2. Signal Classification Against Actual LLM Responses

`SignalClassifierTests.cs` (462 lines) tests all regex patterns against synthetic strings. Gaps: real LLM output variations (extra whitespace, markdown), signals embedded in long responses, multi-signal responses (text + tool call), partial/garbled patterns.

**Proposed tests (~6-8) — `LiveSignalDetectionTests`:**
- `RealAgent_ProducesDetectablePhaseCompleteSignal` — Agent produces text signal, `SignalClassifier` detects it
- `RealAgent_ToolSignalDetected_PhaseComplete` — Agent calls `hugh_notify_phase_complete`, `SdkResponseProcessor` detects via event
- `RealAgent_ToolSignalPriority_OverText` — Both text AND tool signal, verify `Source = ToolCall`
- `RealAgent_BlockerSignalDetected_ToolBased` — Agent calls `hugh_report_blocker`, verify signal type
- `RealAgent_TextFallbackUsed_WhenNoTools` — Session without tools, text-based detection used
- `RealAgent_LongResponse_SignalStillDetected` — 1000+ chars of analysis preceding signal
- `RealAgent_NeedsUserInput_DetectedFromSubagentFormat` — `STATUS: needs_user_input` + `QUESTION_FOR_USER:` format

Challenges: Non-deterministic LLM output; use strong system prompts + retry-on-flake.

### 3. Tool-First vs Text-Fallback Detection Accuracy

Dual-detection priority in `SdkResponseProcessor.ProcessResponseAsync` (lines 260–290): checks `_toolBasedSignal` first (set by `HandleToolComplete` for `hugh_*` tools), falls back to `SignalClassifier.Classify(content)`. The `SignalSource` enum (`TextRegex` vs `ToolCall`) differentiates sources.

**Current gap:** `ProcessResponseAsync` priority logic has ZERO test coverage through the actual event processing path. Tests validate `DetectToolSignal()` and `Classify()` independently but never exercise the combined priority.

**Proposed tests (~5-7) — `LiveDualDetectionTests`:**
- `DualDetection_ToolFirstPriority_RealSession` — Agent produces both signals, verify `Source == ToolCall`
- `DualDetection_TextFallback_NoToolsRegistered` — Session without tools, text detection used
- `DualDetection_BlockerEnriched_WithToolMetadata` — `IsSkippable` and `RawMatch` populated from tool metadata
- `DualDetection_MetricsAccumulated_AcrossToolCalls` — Multiple tools, only `hugh_*` produces signal
- `DualDetection_NoSignal_WhenAgentDoesntSignal` — Regular analysis text, `Signal.Type == None`

### 4. FileSystemHooks Callbacks in Real Sessions

`FileSystemHooks.cs` provides `HandlePreToolUseAsync` (auto-creates directories, checks expected paths, always returns `"allow"`) and `HandlePostToolUseAsync` (logs tool completion). Existing tests (399 lines) cover utility methods directly but **bypass the SDK’s hook invocation mechanism entirely** — they never go through real `PreToolUseHookInput` objects.

**Proposed tests (~6-8) — `LiveFileSystemHooksTests`:**
- `Hooks_PreToolUse_CalledWhenAgentCreatesFile` — Verify callback via directory creation side-effect
- `Hooks_DirectoryAutoCreated_WhenAgentWritesToNewPath` — Non-existent parent dirs created before tool
- `Hooks_UnexpectedPathLogsWarning_ForConstrainedAgent` — pch-researcher writes outside `docs/research/`
- `Hooks_PostToolUse_CalledAfterToolCompletes` — Callback fires after tool execution
- `Hooks_MultipleToolCalls_BothHooksFire` — Multiple tools, hooks fired for each
- `Hooks_NonFileWriteTool_SkipsDirectoryCreation` — `read_file`/`grep_search` don't trigger directory creation

Recommended approach: Side-effect verification (directory creation) is most reliable.

### Test Gap Matrix (Phase 2)

| Area | Current Tests | Proposed New Tests |
|---|---|---|
| Workflow tool SDK registration | Mock-only (25 tests) | 8-10 |
| Signal detection on real LLM output | Synthetic regex tests (462 lines) | 6-8 |
| Dual-detection priority in ProcessResponseAsync | 0 through actual path | 5-7 |
| FileSystemHooks via SDK invocation | Direct utility tests only | 6-8 |
| **Total** | | **~25-33** |

**Key Discoveries:**
- All 4 `hugh_*` tools registered in every session but no test verifies SDK actually invokes them
- `ProcessResponseAsync` dual-detection priority logic has zero coverage through actual event processing
- `FileSystemHooks` tests bypass SDK hook invocation mechanism entirely
- `_toolBasedSignal` reset pattern ensures test isolation per processing cycle
- `SignalSource` enum (`TextRegex` vs `ToolCall`) never verified in integration context
- `LastBlockerIsSkippable`/`LastBlockerReason` metadata enrichment has mock-only coverage
- ~25-33 new live SDK tests proposed across 4 test classes

| File | Relevance |
|------|----------|
| `Hugh/Tools/HughWorkflowTools.cs` | 4 workflow tools and `CreateToolSet()` |
| `Hugh/Agents/SdkResponseProcessor.cs` | Dual-detection priority, tool event handling |
| `Hugh/Detection/SignalClassifier.cs` | All regex patterns for text-based detection |
| `Hugh/Hooks/FileSystemHooks.cs` | Pre/post tool-use hooks |
| `Hugh/Metrics/ToolMetricsCollector.cs` | Tool execution metrics |
| `Hugh.Tests/HughWorkflowToolsTests.cs` | Existing mock tests (299 lines) |
| `Hugh.Tests/SdkResponseProcessorTests.cs` | Existing signal tests (673 lines) |
| `Hugh.Tests/SignalClassifierTests.cs` | Existing regex tests (462 lines) |
| `Hugh.Tests/FileSystemHooksTests.cs` | Existing hook utility tests (399 lines) |

**Gaps:** None  
**Assumptions:** SDK `ToolExecutionStartEvent`/`ToolExecutionCompleteEvent` reliably fire for custom `AIFunction` tools; `PreToolUseHookInput.ToolName` and `ToolArgs` populated consistently by SDK

## Phase 3: CLI Entry-Point Smoke Tests

**Status:** ✅ Complete  
**Session:** 2025-02-23

### 1. CLI Architecture

The CLI is defined in `Hugh/Program.cs` using `System.CommandLine` v2.0.0-beta4. `Program.Main(string[] args)` returns `int` (exit code) via `rootCommand.InvokeAsync(args)`. Handler paths delegate to `HandleStatusCommand()`, `HandleResumeCommand()`, `HandleNewFeatureCommand()`, and the `doctor` subcommand. Several handler paths use `Environment.ExitCode = 1` (global state) instead of returning from the handler, creating a dual exit-code mechanism.

**Key testability challenge:** `Program.Main` is static with all dependencies constructed internally (no DI). True CLI smoke tests require process-level invocation or refactoring.

### 2. `hugh doctor` Exit Codes and Output

Doctor command (lines 115–134) calls `SetupOrchestrator.RunAsync()`, sets `Environment.ExitCode = result.AllSatisfied ? 0 : 1`. Runs 5 detectors in priority order: AgentDiscovery, Workspace, GitHubCli, CopilotCli, GitHubAuth.

**Proposed tests (~6):**
- `Doctor_AllSatisfied_ExitCode0` — All detectors satisfied → exit code 0
- `Doctor_OneMissing_ExitCode1` — One detector missing → exit code 1
- `Doctor_WithFix_NonInteractive_AutoInstalls` — Auto-installs without prompts
- `Doctor_WithFix_Interactive_PromptsUser` — Prompts user for fix decisions
- `Doctor_Output_ShowsPrerequisiteReport` — Captures and verifies report formatting
- `Doctor_FixAbort_StopsProcessing` — User aborts → `AllSatisfied=false`

Note: `SetupOrchestratorTests.cs` (571 lines) covers orchestrator logic; CLI tests focus on Program.cs wiring.

### 3. `--status` With/Without State File

Delegates to `LoopOrchestrator.GetStatusAsync()` which reads `.orchestrator/loop-state.json`. Returns `null` if no file exists → "No active runs found." Otherwise displays via `ConsoleOutput.WriteRunStatus(state)`.

**Proposed tests (~5):**
- `Status_NoStateFile_ShowsNoActiveRuns`
- `Status_WithStateFile_ShowsRunStatus`
- `Status_WithFailedRun_ShowsFailureReason`
- `Status_WithPausedRun_ShowsPausedStatus`
- `Status_WithCodingPhase_ShowsSubPhase`

### 4. `--resume` From Persisted State

Resume (lines 255–320) first validates prerequisites, discovers agents, then routes to `SolutionOrchestrator.ResumeAsync` or `LoopOrchestrator.ResumeAsync`. `ResumeAsync` loads state by GUID or most recent, returns `false` if no state found, `true` if already complete, otherwise calls `ExecuteLoopAsync`.

**Proposed tests (~8):**
- `Resume_NoState_ReturnsFalse`
- `Resume_CompletedRun_ReturnsTrue`
- `Resume_WithRunId_LoadsSpecificRun`
- `Resume_WithWrongRunId_ReturnsFalse`
- `Resume_PrereqsFail_ExitCode1`
- `Resume_FailedRun_RetriesFromCurrentPhase`
- `Resume_PausedRun_ContinuesFromCurrentPhase`
- `Resume_MostRecent_WhenNoGuidProvided`

### 5. `--verbose` Log File Creation

`--verbose` reconfigures Serilog's global `Log.Logger` to Debug level with file sink to `~/.hugh/logs/hugh-{date}.log` (via `PathResolver.GetLogPath()`). Default (non-verbose) writes at Information level.

**Proposed tests (~4):**
- `Verbose_CreatesLogFile` — Log file created in `~/.hugh/logs/`
- `Verbose_LogsDebugMessages` — Debug-level entries present
- `NonVerbose_LogsInfoOnly` — No Debug entries without flag
- `LogPath_DirectoryCreated` — Already covered in PathResolverTests

### 6. `--timeout` Override Behavior

`ParseTimeoutOverride` (lines 517–530) uses `TimeSpan.TryParse`, returns `null` for invalid. `ApplyTimeoutOverride` (lines 536–545) sets all 5 phase timeouts (Vision, Research, Planning, Review, CodingPerPhase) to the override value. Both are `private static` — need to be made `internal` for direct testing.

**Proposed tests (~6):**
- `Timeout_ValidFormat_AppliesOverride` — `01:00:00` → all timeouts = 1 hour
- `Timeout_InvalidFormat_LogsWarning` — `xyz` → null, defaults unchanged
- `Timeout_EmptyString_ReturnsNull`
- `Timeout_NullInput_ReturnsNull`
- `Timeout_AppliesAllPhases` — All 5 fields set identically
- `Timeout_MinutesFormat_Works` — `00:30:00` → 30 minutes

### 7. `--no-interactive` Mode

In root handler (line 153): clears `HughConfig.Autonomous.InteractivePhases`. In doctor (line 126): sets `SetupOptions.NonInteractive = true` (also true when `Console.IsInputRedirected`). `NonInteractiveSetupUI` always returns `FixDecision.Skip` and `AuthDecision.Skip`.

**⚠️ Potential Bug Found:** In `HandleResumeCommand` (line 260) and `HandleNewFeatureCommand` (line 329), `SetupOptions.NonInteractive` is set from `Console.IsInputRedirected` only — the `--no-interactive` flag does NOT propagate. This means `--no-interactive --resume` still uses `InteractiveSetupUI` for prerequisite checks unless stdin is redirected.

**Proposed tests (~6):**
- `NoInteractive_ClearsInteractivePhases`
- `NoInteractive_Doctor_SetsNonInteractiveTrue`
- `NoInteractive_Doctor_WithFix_SkipsPrompts`
- `NonInteractive_UI_SkipsAllPrompts`
- `NoInteractive_Resume_NotPropagated` — Verifies/documents the potential bug
- `StdinRedirected_ImpliesNonInteractive`

### Test Strategy Summary

**Tier 1 — Pure Unit Tests:** Test extracted helper methods (`ParseTimeoutOverride`, `ApplyTimeoutOverride` made `internal`), `SetupOrchestrator.RunAsync` with mocks, `LoopStateManager` scenarios.

**Tier 2 — Process-Level Smoke Tests:** Invoke `Program.Main` or the compiled binary, validate exit codes and output.

**Testability blockers:** Static state (`_orchestrator`, `_globalCts`, `Log.Logger`, `PathResolver._installationPath`), no DI, `Environment.ExitCode` global state. Recommended: extract `ParseTimeoutOverride`/`ApplyTimeoutOverride` as `internal`, consider `CliCommandHandler` extraction.

| Area | Proposed Tests |
|---|---|
| `doctor` exit codes | 6 |
| `--status` | 5 |
| `--resume` | 8 |
| `--verbose` | 4 |
| `--timeout` | 6 |
| `--no-interactive` | 6 |
| **Total** | **~35** |

**Key Discoveries:**
- Dual exit-code mechanism: `Environment.ExitCode` vs `rootCommand.InvokeAsync` return value
- `ParseTimeoutOverride`/`ApplyTimeoutOverride` are `private static` — need `internal` for testing
- **Potential bug:** `--no-interactive` doesn't propagate to `SetupOptions.NonInteractive` in resume/feature handlers
- `doctor` exit code uses `Environment.ExitCode` (handler returns void), so `InvokeAsync` always returns 0
- All static dependencies make in-process `Program.Main` testing require careful state cleanup

| File | Relevance |
|------|----------|
| `Hugh/Program.cs` | CLI entry point, all handlers (542 lines) |
| `Hugh/Setup/SetupOrchestrator.cs` | Doctor command logic (306 lines) |
| `Hugh/Orchestration/LoopOrchestrator.cs` | Status/Resume logic (991 lines) |
| `Hugh/Orchestration/LoopStateManager.cs` | State persistence (320 lines) |
| `Hugh/Configuration/HughConfig.cs` | Config model with TimeoutConfig (240 lines) |
| `Hugh/Configuration/PathResolver.cs` | Log path resolution (84 lines) |
| `Hugh/ConsoleOutput.cs` | Console output formatting (373 lines) |
| `Hugh/Setup/UserInterface/NonInteractiveSetupUI.cs` | Non-interactive UI (156 lines) |

**Gaps:** `System.CommandLine` beta4 behavior for `Environment.ExitCode` vs `InvokeAsync` return value needs verification  
**Assumptions:** `System.CommandLine` handler with void return always returns 0 from `InvokeAsync`, so `doctor` actual exit relies on `Environment.ExitCode`

## Phase 4: End-to-End Pipeline Tests

**Status:** ✅ Complete  
**Session:** 2025-02-23

### 1. Single-Phase Execution with Real Agents

`PhaseExecutor` has two execution paths: `ExecuteAsync` (single-shot) and `ExecuteInteractiveAsync` (multi-turn Q&A for Vision). `LoopOrchestrator.DispatchPhaseAsync` routes based on phase type. Agent prompts are loaded via `AgentPromptLoader`, document attachments prepared by `PrepareDocumentAttachments`, and output paths extracted through a 3-tier fallback chain (result path → expected path → recent file scan within 30 minutes).

**Proposed tests (~5):**
- `Vision_SinglePhase_ProducesDocument` — Execute Vision, verify `PhaseResult.Success`, `OutputDocumentPath` exists, document has markdown structure
- `Vision_InteractivePhase_HandlesAutoAccept` — `AcceptRecommendationsOnly=true`, verify decisions auto-accepted
- `Vision_Phase_PopulatesToolMetrics` — Verify `ToolMetrics != null` and `TotalExecutions > 0`
- `Research_SinglePhase_ProducesDocument` — With vision doc prerequisite, verify output in `docs/research/`
- `Research_WithDocumentContext_ReceivesAttachments` — Execute with docs attached, verify completion

**Testability notes:** Non-deterministic LLM output — assert structural properties (file exists, has content) not exact content. Each real SDK call takes ~30-120s and incurs API costs.

### 2. Multi-Phase State Transitions with Real SDK

State machine in `LoopOrchestrator.ExecuteLoopAsync`: for each phase, `UpdatePhaseAsync(InProgress)` → `DispatchPhaseAsync` → retry if retryable → validate document exists → `MarkPhaseCompleteAsync`. State persisted to `.orchestrator/loop-state.json`. Document paths propagate forward: Research gets Vision doc, Planning gets Vision+Research, etc.

**Proposed tests (~5):**
- `MultiPhase_VisionThenResearch_StateTransitionsCorrectly` — State.CurrentPhase advances, CompletedPhases updated, Documents populated
- `MultiPhase_FullPipeline_VisionThroughReview` — 4-phase pipeline with state persistence and document propagation
- `MultiPhase_DocumentOutputValidation_FailsWhenMissing` — Empty OutputDocumentPath → loop fails
- `MultiPhase_ResumeFromFailedPhase_ContinuesCorrectly` — Fail Research, resume, Research re-attempted (not Vision)
- `MultiPhase_PerPhaseRetry_WithRealSDK` — Transient error triggers retry per `MaxPhaseRetries`

**Recommended hybrid approach:** Use real `CopilotService` for 1-2 phases, testable subclass (`TestableLoopOrchestrator` pattern from `PhaseRetryTests`) for the rest. Verifies SDK integration while managing costs.

### 3. Cancellation/Timeout Mid-Phase

Dual cancellation: global CTS (Ctrl+C → `OperationCanceledException` for graceful shutdown) and per-phase timeout CTS (linked, returns failed `PhaseResult` with "timed out" message). Timeouts: Vision=10m, Research=30m, Planning=20m, Review=15m, Coding=20m per sub-phase. Interactive Vision=30m.

**Proposed tests (~6):**
- `Cancellation_GlobalCts_PropagatesGracefully` — Cancel after 2s, verify OCE thrown, state=Failed
- `Cancellation_PerPhaseTimeout_ReturnsFailedResult` — 1s timeout, verify `Success=false`, "timed out" message
- `Cancellation_MidPhase_StateMarkedFailed` — Cancel during Vision, verify state persisted as Failed
- `Cancellation_LinkedToken_RespondsToEitherSource` — Cancel phase-specific CTS, verify no propagation upward
- `Timeout_InteractivePhase_UsesLongerTimeout` — Artificially short interactive timeout, verify trigger
- `Cancellation_DuringRetry_StopsImmediately` — Cancel during retry delay, no more retries

**Implementation:** Use artificially short timeouts (500ms-2s). Real SDK startup exceeds these naturally.

### 4. PhaseResult Field Population from Real Responses

`PhaseResult` has 9 fields populated from 3 layers: `CopilotService` (Success, Duration, ErrorMessage, ErrorClassification), `SdkResponseProcessor` (CompactionOccurred, ToolMetrics, OutputDocumentPath), `LoopOrchestrator` (RetryAttemptsMade). `DecisionsMade` populated by `HandleSignalAsync` when auto-accepting recommendations.

**Proposed tests (~10):**
- `PhaseResult_Duration_IsPositive` — `Duration > TimeSpan.Zero` and `< timeout`
- `PhaseResult_Success_TrueOnNormalCompletion` — Vision completes, `ErrorMessage` is null
- `PhaseResult_OutputDocumentPath_PopulatedForDocumentPhases` — Not null, file exists, ends .md
- `PhaseResult_ToolMetrics_ContainsExecutionData` — Soft assertion (not null OR expected shape)
- `PhaseResult_ErrorClassification_PopulatedOnFailure` — Force failure, verify category
- `PhaseResult_CompactionOccurred_FalseForShortSessions` — Short session, false
- `PhaseResult_DecisionsMade_PopulatedWhenAutoAccepting` — Auto-accept, check Decision records
- `PhaseResult_RetryAttemptsMade_ZeroOnFirstSuccess` — No retries needed
- `PhaseResult_FailedPhase_HasErrorMessage` — Timeout phase, non-empty error
- `PhaseResult_InteractivePhase_PopulatesAllFields` — Interactive Vision, all fields populated

### Test Summary

| Area | Proposed Tests |
|---|---|
| Single-phase execution | 5 |
| Multi-phase state transitions | 5 |
| Cancellation/timeout | 6 |
| PhaseResult field population | 10 |
| **Total** | **~26** |

**Key Discoveries:**
- Dual cancellation architecture: global CTS propagates as exception, per-phase timeout returns failed `PhaseResult`
- `ExtractOutputDocumentPath` 3-tier fallback chain (result → expected → recent file scan within 30 min) — tests should use `File.Exists()` not exact path matching
- `ExecuteLoopAsync` validates document-producing phases created output files; fails immediately if missing
- Existing testable subclass pattern (`TestableLoopOrchestrator`, `BuildVerificationTestOrchestrator`) enables hybrid real+mock E2E tests
- `PhaseResult` 9 fields populated from 3 layers — ToolMetrics/CompactionOccurred from SDK events
- Interactive phase flow fundamentally different (multi-turn, empty answer escalation, phase transition detection)

| File | Relevance |
|------|----------|
| `Hugh/Orchestration/PhaseExecutor.cs` | Phase execution, timeout/cancellation, document path extraction |
| `Hugh/Orchestration/LoopOrchestrator.cs` | Main loop state machine, retry, document validation |
| `Hugh/Orchestration/LoopStateManager.cs` | State persistence, resume |
| `Hugh/Agents/CopilotService.cs` | SDK wrapper, signal handling, interactive Q&A |
| `Hugh/Agents/SdkResponseProcessor.cs` | Event processing, metrics, signal classification |
| `Hugh/Models/PhaseResult.cs` | 9-field result record |
| `Hugh.Tests/Orchestration/PhaseRetryTests.cs` | TestableLoopOrchestrator pattern |
| `Hugh.Tests/Orchestration/SubPhaseBuildVerificationTests.cs` | BuildVerificationTestOrchestrator pattern |

**Gaps:** None  
**Assumptions:** Real SDK E2E tests need `gh auth`/`GH_TOKEN`; ToolMetrics population depends on SDK version emitting execution events

## Phase 5: Test Infrastructure & Flakiness Mitigation

**Status:** ✅ Complete  
**Session:** 2025-02-23

### 1. SDK Test Trait/Category Conventions

Currently only one trait (`Category=Integration`) is used across ~584 test methods. No `xunit.runner.json`, `[Collection]`, `IClassFixture`, or `ICollectionFixture` exists.

**Proposed trait taxonomy:**

| Trait Value | Purpose | When to Run | Estimated Count |
|---|---|---|---|
| `Unit` | Pure logic tests | Always | ~570 existing |
| `Integration` | Filesystem/process tests | Always | ~15 existing |
| `Smoke` | CLI entry-point tests | Pre-merge CI | ~35 proposed |
| `LiveSdk` | Real Copilot SDK session | On-demand, nightly | ~18-20 proposed |
| `E2E` | Full pipeline with real SDK | On-demand, weekly | ~26 proposed |

Implement via `TestCategory` constants class. CI filter: `dotnet test --filter "Category!=LiveSdk&Category!=E2E"`. **Must update `release.yml`** which currently runs `dotnet test` unfiltered.

### 2. Timeout Tuning for CI vs Local

No test-level timeouts exist. Recommended: environment-aware `TestTimeouts` class with `CancellationTokenSource` pattern (already used in codebase).

| Category | Local | CI |
|---|---|---|
| Unit | 5s | 10s |
| Integration | 15s | 30s |
| Smoke | 30s | 60s |
| LiveSdk | 120s | 180s |
| E2E | 300s | 600s |

Detect CI via `Environment.GetEnvironmentVariable("CI")` or `"GITHUB_ACTIONS"`.

### 3. Retry Strategies for Transient SDK Failures

Hugh's production retry infrastructure (`SendWithRetryAsync`, `CalculateRetryDelay`, `SessionErrorClassifier`) can be directly reused for test retries.

**Recommended: `SdkTestRetry.WithRetryAsync()` wrapper** that classifies exceptions via `SessionErrorClassifier`, retries only retryable errors, uses exponential backoff via `CalculateRetryDelay`. Simpler than custom xUnit discoverers.

```csharp
public static class SdkTestRetry
{
    public static async Task WithRetryAsync(
        Func<Task> testBody, int maxRetries = 2, int baseDelayMs = 2000)
    {
        for (int attempt = 1; attempt <= maxRetries + 1; attempt++)
        {
            try { await testBody(); return; }
            catch (Exception ex) when (attempt <= maxRetries)
            {
                var classification = new SessionErrorClassifier().Classify(ex);
                if (!classification.IsRetryable) throw;
                await Task.Delay(CopilotService.CalculateRetryDelay(
                    classification, attempt, baseDelayMs));
            }
        }
    }
}
```

### 4. Test Isolation Patterns for Real SDK

Existing pattern (IDisposable + temp directory with `Guid.NewGuid():N`) is strong. Proposed `SdkTestBase` base class providing:
- Unique temp workspace with `agents/`, `docs/` subdirectories, `.orchestrator/`
- Minimal agent stubs for all 5 agents
- Pre-configured `HughConfig` with test-appropriate settings
- `IAsyncLifetime` for async setup + `IDisposable` for cleanup
- `await using var service = new CopilotService(TestConfig)` pattern ensures session cleanup on failure

**Parallel execution:** Safe since each test creates its own workspace. No `[Collection]` needed unless rate limiting occurs, in which case group via `[Collection("SdkTests")]`.

### 5. Conditional Skip vs Hard Requirement

Current `IsCopilotCliAvailable()` + `return` marks skipped tests as **passed** (silent skip). **xUnit 2.9.2 supports `Assert.Skip(reason)`** — properly reports as "skipped" in test results.

**Recommended: `SdkAvailability.IsSdkAvailable()` centralized checker** with cached results, checking gh CLI, copilot extension, and auth status. Environment variable `HUGH_SKIP_SDK_TESTS=true` for explicit opt-out.

```csharp
if (!SdkAvailability.IsSdkAvailable())
    Assert.Skip("Copilot SDK not available");
```

Migrate existing `IsCopilotCliAvailable()` + `return` to `Assert.Skip()` pattern.

### 6. Cost/Token Budget Controls

**Estimated full suite cost:** ~70K tokens ≈ $0.21/run with minimal prompts.

**Strategies:**
1. **Minimal prompts** — `"Reply with exactly: OK"` (~50-100 tokens per test)
2. **Token budget tracker** — Optional `TokenBudgetTracker` with `HUGH_TEST_TOKEN_BUDGET` env var (default 50K)
3. **Test tiering** — Run Tier 0-1 always, Tier 2 on PR merge, Tier 3 nightly, Tier 4 weekly
4. **Model override** — `HUGH_SDK_TEST_MODEL` env var for cheaper models in CI
5. **Nightly CI workflow** — Separate `sdk-tests.yml` with schedule trigger and tier selection

### Infrastructure Files to Create

| File | Purpose |
|---|---|
| `Hugh.Tests/Infrastructure/TestCategory.cs` | String constants for trait categories |
| `Hugh.Tests/Infrastructure/SdkAvailability.cs` | Centralized SDK detection + caching |
| `Hugh.Tests/Infrastructure/SdkTestBase.cs` | Base class with workspace setup, timeout, IDisposable |
| `Hugh.Tests/Infrastructure/SdkTestRetry.cs` | Retry wrapper using SessionErrorClassifier |
| `Hugh.Tests/Infrastructure/TestTimeouts.cs` | Environment-aware timeout values |
| `Hugh.Tests/Infrastructure/TokenBudgetTracker.cs` | Optional token budget enforcement |

### Migration Path

1. Tag existing tests with proper traits
2. Replace `IsCopilotCliAvailable()` + `return` with `Assert.Skip()`
3. Update `release.yml` to exclude `LiveSdk` and `E2E` categories
4. Create nightly CI workflow for SDK tests

### Test Execution Matrix

| Environment | Command | What Runs |
|---|---|---|
| Developer local (fast) | `dotnet test` | Unit + Integration |
| Developer local (SDK) | `dotnet test --filter "Category=LiveSdk"` | Live SDK tests |
| CI (PR gate) | `dotnet test --filter "Category!=LiveSdk&Category!=E2E"` | Unit + Integration + Smoke |
| CI (nightly) | `dotnet test` | All tests |
| CI (weekly E2E) | `dotnet test --filter "Category=E2E"` | Full pipeline |

**Key Discoveries:**
- Only 1 trait used across ~584 test methods; vast majority uncategorized
- `Assert.Skip()` available in xUnit 2.9.2 but unused; current skip pattern shows passed not skipped
- No test-level timeouts configured; xUnit default is infinite
- `SessionErrorClassifier`/`CalculateRetryDelay` directly reusable for test retries
- `release.yml` runs `dotnet test` unfiltered — new SDK tests would immediately fail in CI
- Estimated full SDK test suite: ~70K tokens ≈ $0.21/run

| File | Relevance |
|------|----------|
| `Hugh.Tests/IntegrationTests.cs` | Only real CLI test, `IsCopilotCliAvailable()` pattern |
| `Hugh.Tests/Hugh.Tests.csproj` | xUnit 2.9.2, NSubstitute 5.3.0, .NET 9.0 |
| `Hugh.Tests/CopilotServiceRetryTests.cs` | Retry infrastructure patterns to reuse |
| `Hugh.Tests/LoopStateManagerTests.cs` | Exemplar IDisposable + temp dir pattern |
| `Hugh/Detection/SessionErrorClassifier.cs` | Error classification for retry decisions |
| `.github/workflows/release.yml` | CI workflow needs `--filter` update |

**Gaps:** None  
**Assumptions:** xUnit 2.9.2 includes `Assert.Skip()`; GitHub Actions runners lack Copilot CLI auth

## Overview

Hugh's current 705-test suite is entirely mock-based and unit-focused, with only a single integration test (`CopilotService_WithRealCLI_ExecutesPrompt`) touching the real Copilot SDK — and that test is silently skipped when the SDK isn't available. This research identified **~104-114 new automated tests** across five categories that would close critical gaps between mock-based assertions and real SDK behavior.

The highest-value gaps are in **workflow tool registration** (no test verifies the SDK actually invokes `hugh_*` tools), **dual-detection priority** (`ProcessResponseAsync`'s tool-first/text-fallback logic has zero coverage through the actual event processing path), and **SDK session lifecycle** (zero tests for client start/stop, session creation with full `SessionConfig`, or `DisposeAsync` cleanup). The `TryEnableInfiniteSession` reflection-based configuration is particularly fragile — an SDK version change would silently break it with no test to catch it.

CLI smoke testing revealed a **potential bug**: the `--no-interactive` flag doesn't propagate to `SetupOptions.NonInteractive` in the `HandleResumeCommand` and `HandleNewFeatureCommand` code paths, and a **dual exit-code mechanism** where `doctor` uses `Environment.ExitCode` while the `Main` method returns `rootCommand.InvokeAsync()`.

The research recommends a **tiered test infrastructure** using xUnit traits (`Unit`, `Integration`, `Smoke`, `LiveSdk`, `E2E`), environment-aware timeouts, a retry wrapper reusing Hugh's own `SessionErrorClassifier`, `Assert.Skip()` for proper skip reporting (replacing the current silent-pass pattern), and a token budget system estimating ~$0.21/full run.

## Key Findings

1. **Zero SDK lifecycle test coverage** — No tests verify real `CopilotClient` start/stop, `SessionConfig` wiring, `DisposeAsync` cleanup, or compaction configuration. ~18-20 tests proposed.
2. **Workflow tool invocation untested end-to-end** — `HughWorkflowTools.CreateToolSet()` is mock-tested but no test verifies the SDK discovers and invokes the 4 `hugh_*` tools in a real session.
3. **Dual-detection priority untested through actual path** — `SdkResponseProcessor.ProcessResponseAsync` tool-first/text-fallback logic has zero coverage through the event processing pipeline. Tests validate `DetectToolSignal()` and `Classify()` independently.
4. **`TryEnableInfiniteSession` uses fragile reflection** — Sets `InfiniteSessionConfig.CompactionThreshold` via reflection on SDK v0.1.22. No test catches SDK version breakage.
5. **Silent test skips** — `IsCopilotCliAvailable()` + `return` marks skipped tests as passed. `Assert.Skip()` is available in xUnit 2.9.2 but unused.
6. **`--no-interactive` propagation bug** — Flag doesn't propagate to `SetupOptions.NonInteractive` in resume/feature handlers (lines 260, 329 of `Program.cs`).
7. **`release.yml` needs `--filter` update** — CI runs `dotnet test` unfiltered; new SDK/E2E tests would fail immediately.
8. **`Program.cs` testability limited by static state** — `_orchestrator`, `_globalCts`, `Log.Logger`, `PathResolver._installationPath` are all static.
9. **Existing patterns enable infrastructure** — `IDisposable` + temp directory isolation, `InternalsVisibleTo`, testable subclass pattern (`TestableLoopOrchestrator`) provide strong foundations.
10. **Cost estimate: ~70K tokens ($0.21) per full SDK test run** — Manageable with tiered execution and minimal prompts.

## Actionable Conclusions

1. **Create `Hugh.Tests/Infrastructure/` directory** with 6 shared files: `TestCategory.cs`, `SdkAvailability.cs`, `SdkTestBase.cs`, `SdkTestRetry.cs`, `TestTimeouts.cs`, `TokenBudgetTracker.cs`
2. **Migrate existing skip pattern** from `IsCopilotCliAvailable()` + `return` to `Assert.Skip()` with centralized `SdkAvailability` checker
3. **Update `release.yml`** to add `--filter "Category!=LiveSdk&Category!=E2E"` preventing SDK tests from running in CI without credentials
4. **Create nightly CI workflow** (`sdk-tests.yml`) with tiered execution: Smoke → LiveSdk Basic → LiveSdk Full → E2E
5. **Make `ParseTimeoutOverride`/`ApplyTimeoutOverride` internal** in `Program.cs` for direct unit testability
6. **Fix the `--no-interactive` propagation** — pass `noInteractive` parameter to `SetupOptions.NonInteractive` in resume/feature handlers
7. **Implement tests in priority order**: Phase 1 lifecycle tests (catches SDK breakage) → Phase 2 workflow tools (validates core communication) → Phase 3 CLI smoke (improves confidence in CLI wiring) → Phase 4 E2E (validates full pipeline)

## Open Questions

- Does `System.CommandLine` beta4 `InvokeAsync` return 0 for void handlers even when `Environment.ExitCode` is set to 1? This affects how `doctor` exit codes should be tested.
- Is the `--no-interactive` not propagating to resume/feature handlers intentional or a bug?
- Should `[Collection("SdkTests")]` be used to serialize SDK tests if rate limiting occurs, or keep parallel execution?
- What's the token budget threshold for CI — should there be a hard limit or just monitoring?

## Standards Applied

| Standard | Relevance | Guidance |
|----------|-----------|----------|
| No organizational standards applicable to this research. | — | — |

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2025-02-23 |
| Status | ✅ Complete |
| Current Phase | ✅ Complete |
| Path | /docs/research/022-hugh-sdk-test-automation-expansion.md |
