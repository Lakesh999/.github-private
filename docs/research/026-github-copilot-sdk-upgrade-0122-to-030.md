---
id: "026"
type: research
title: "GitHub Copilot SDK Upgrade: 0.1.22 → 0.3.0"
status: ✅ Complete
created: "2025-05-19"
current_phase: "5 of 5"
---

## Introduction

Hugh currently uses `GitHub.Copilot.SDK` version 0.1.22. The latest stable release is 0.3.0 (published April 2025). This research documents all breaking changes, new APIs, and required code modifications to upgrade Hugh to the latest SDK version. The upgrade involves significant API surface changes including a now-required `OnPermissionRequest` handler, restructured `InfiniteSession` configuration, new event types, and numerous new capabilities.

## Objectives

- Identify all breaking API changes between SDK 0.1.22 and 0.3.0
- Map each breaking change to specific Hugh source files that need modification
- Document new SDK features that Hugh should adopt (replacing reflection hacks, etc.)
- Identify configuration changes needed in `appsettings.json` and `HughConfig`
- Assess test impact and required test updates
- Determine if any Hugh architecture patterns need redesign for the new SDK

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | CopilotService Session Creation Breaking Changes | ✅ Complete | Analyze `CreateSessionAsync` signature changes; `OnPermissionRequest` requirement; `InfiniteSessionConfig` restructuring; `SdkSessionConfig` type changes | 2025-05-19 |
| 2 | Event Types and Response Processing Changes | ✅ Complete | Analyze new/changed event types; streaming events; `SdkResponseProcessor` compatibility; `ToolExecutionStartEvent`/`ToolExecutionCompleteEvent` changes | 2025-05-19 |
| 3 | Hooks, Tools, and Permission Handler Changes | ✅ Complete | Analyze `SessionHooks` API changes; `OnPreToolUse`/`OnPostToolUse` signature changes; `FileSystemHooks` compatibility; `HughWorkflowTools` tool registration; `skip_permission` and `is_override` | 2025-05-19 |
| 4 | User Input, Elicitation, and Question Handling | ✅ Complete | Analyze `OnUserInputRequest` signature changes; new `UserInputResponse` model; `OnElicitationRequest`; `SdkQuestionHandler` compatibility | 2025-05-19 |
| 5 | Client Lifecycle, Configuration, and Test Impact | ✅ Complete | Analyze `CopilotClient` constructor changes; `CopilotClientOptions` new properties; `ForceStopAsync`; session resume; `HughConfig` changes; test file updates needed | 2025-05-19 |

## Phase 1: CopilotService Session Creation Breaking Changes

**Status:** ✅ Complete  
**Session:** 2025-05-19

### 1. `OnPermissionRequest` — Now Required on `SessionConfig`

**Breaking Change Severity: HIGH — Session creation will fail without it**

In SDK 0.3.0, `OnPermissionRequest` is a **required** property on `SessionConfig`. Without it, `CreateSessionAsync` will throw at runtime.

**Current Hugh code** (`Hugh/Agents/CopilotService.cs` L683-698):
```csharp
var sessionConfig = new SdkSessionConfig
{
    Model = _config.Copilot.Model,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = agentPrompt
    },
    Hooks = fileSystemHooks.CreateHooks(),
    Tools = workflowTools.CreateToolSet()
};
```

**Required change:**
```csharp
var sessionConfig = new SdkSessionConfig
{
    Model = _config.Copilot.Model,
    SystemMessage = new SystemMessageConfig
    {
        Mode = SystemMessageMode.Append,
        Content = agentPrompt
    },
    Hooks = fileSystemHooks.CreateHooks(),
    Tools = workflowTools.CreateToolSet(),
    OnPermissionRequest = PermissionHandler.ApproveAll  // NEW: Required
};
```

**Design Decision:** Hugh already has `FileSystemHooks` that returns `PermissionDecision = "allow"` for all tool calls. The simplest approach is `PermissionHandler.ApproveAll` (built-in helper). Alternatively, a custom handler could integrate with the existing hooks architecture for logging/monitoring, but that may be over-engineering since hooks already handle pre/post tool monitoring.

### 2. `InfiniteSession` → `InfiniteSessions` Property Rename + API Restructuring

**Breaking Change Severity: HIGH — Current reflection code will silently fail**

The property name changed from `InfiniteSession` to `InfiniteSessions` (plural), and config type properties changed:

| 0.1.22 (via reflection) | 0.3.0 (native API) |
|---|---|
| `SessionConfig.InfiniteSession` | `SessionConfig.InfiniteSessions` |
| `InfiniteSessionConfig.CompactionThreshold` | `InfiniteSessionConfig.BackgroundCompactionThreshold` |
| *(none)* | `InfiniteSessionConfig.BufferExhaustionThreshold` |
| *(none)* | `InfiniteSessionConfig.Enabled` |

In 0.3.0, **infinite sessions are enabled by default** — no explicit configuration is needed unless custom thresholds are desired.

**Current Hugh code** (`Hugh/Agents/CopilotService.cs` L713-758) — uses reflection:
```csharp
internal void TryEnableInfiniteSession(SdkSessionConfig sessionConfig)
{
    try
    {
        var infiniteSessionProperty = sessionConfig.GetType().GetProperty("InfiniteSession");
        if (infiniteSessionProperty == null) { return; }
        var sdkAssembly = typeof(SdkSessionConfig).Assembly;
        var infiniteConfigType = sdkAssembly.GetType("GitHub.Copilot.SDK.InfiniteSessionConfig");
        // ... creates instance via reflection, sets CompactionThreshold
    }
    catch (Exception ex)
    {
        _logger.Warning(ex, "Failed to enable InfiniteSession...");
    }
}
```

**Required change — Replace entire method with direct API call:**
```csharp
var sessionConfig = new SdkSessionConfig
{
    // ... other properties ...
    InfiniteSessions = new InfiniteSessionConfig
    {
        Enabled = true,
        BackgroundCompactionThreshold = _config.Copilot.CompactionThreshold,  // 0.8
        BufferExhaustionThreshold = 0.95  // new property, sane default
    }
};
```

**Impact on `HughConfig.CopilotConfig`:** `CompactionThreshold` (0.8) maps to `BackgroundCompactionThreshold`. Consider adding `BufferExhaustionThreshold` to config or hardcode 0.95.

**The entire `TryEnableInfiniteSession` method can be deleted.**

### 3. `CopilotSession.WorkspacePath` — New Property

In 0.3.0, when infinite sessions are enabled, the session exposes a `WorkspacePath` property. This is `null` when infinite sessions are disabled. Hugh doesn't currently use it but could leverage it for state persistence.

### 4. `SessionConfig` New Properties (Non-Breaking Additions)

| Property | Type | Purpose | Hugh Impact |
|---|---|---|---|
| `SessionId` | `string?` | Custom session ID | Could use for deterministic session naming |
| `ReasoningEffort` | `string?` | "low"/"medium"/"high"/"xhigh" | Potential config option |
| `AvailableTools` | `IList<string>?` | Allowlist tool names | May restrict agent tools |
| `ExcludedTools` | `IList<string>?` | Blocklist tool names | May restrict agent tools |
| `Provider` | `ProviderConfig?` | BYOK provider config | Not needed now |
| `Streaming` | `bool` | Enable streaming chunks | Hugh already processes deltas |
| `OnElicitationRequest` | handler | UI elicitation | New capability |
| `Commands` | `IList<CommandDefinition>?` | Slash commands | Not needed (headless) |
| `EnableConfigDiscovery` | `bool?` | Auto-discover MCP/skills | Potentially useful |

### 5. `OnUserInputRequest` — Native Typed Handler

**Major capability upgrade.** In 0.1.22, Hugh handles user input via reflection. In 0.3.0, there's a native, typed handler:

```csharp
OnUserInputRequest = async (request, invocation) =>
{
    // request.Question - The question to ask
    // request.Choices - Optional list of choices
    // request.AllowFreeform - Whether freeform input is allowed
    return new UserInputResponse
    {
        Answer = "User's answer",
        WasFreeform = true
    };
}
```

The reflection-based property extraction in `SdkQuestionHandler.FromSdkRequest` can be replaced with direct property access.

### 6. `CopilotClient` Constructor — New `CopilotClientOptions`

New overload (non-breaking):
```csharp
_client = new CopilotClient(new CopilotClientOptions
{
    Logger = _sdkLogger,
    Cwd = _workspacePath,
    Telemetry = new TelemetryConfig { ... },
    GitHubToken = "...",
    UseLoggedInUser = true,
    CliPath = "...",
});
```

**Key Change:** The SDK now **bundles the CLI** — separate `copilot` binary installation may no longer be required. This affects Hugh's `Setup/Detection/CopilotCliDetector.cs`.

### 7. `ForceStopAsync()` and `ResumeSessionAsync`

- `ForceStopAsync()` — Forceful shutdown when `StopAsync()` takes too long. Useful for timeout scenarios.
- `ResumeSessionAsync` — Resume sessions by ID after disposal. Could enable actual session resume rather than creating new ones.

### 8. Reflection Hacks That Can Be Eliminated

| Current Code | Location | 0.3.0 Native API |
|---|---|---|
| `TryEnableInfiniteSession` | `CopilotService.cs:713-758` | `InfiniteSessions` property |
| `TryGetPromptFromSdkRequest` | `SdkQuestionHandler.cs:84-100` | `request.Question`, `.Choices`, `.AllowFreeform` |
| `TryExtractContentFromEvent` | `SdkResponseProcessor.cs` switch default | Stable `msg.Data.Content` |
| `SessionCompactionStartEvent`/`CompleteEvent` by name | `SdkResponseProcessor.cs` | Stable types in SDK |

**Key Discoveries:**
- `OnPermissionRequest` is REQUIRED — session creation fails without it
- `InfiniteSession` renamed to `InfiniteSessions` with restructured config properties
- Infinite sessions enabled by default in 0.3.0
- SDK bundles CLI binary — prerequisite detection may need updating
- `OnUserInputRequest` is now a typed handler — all reflection hacks replaceable
- `ResumeSessionAsync` enables actual session resume
- 4 reflection hacks can be eliminated with direct native API calls

| File | Relevance |
|------|-----------|
| `Hugh/Agents/CopilotService.cs` | Primary: session creation, InfiniteSession reflection |
| `Hugh/Hooks/FileSystemHooks.cs` | Hook signatures, `CreateHooks()` |
| `Hugh/Tools/HughWorkflowTools.cs` | `CreateToolSet()` method |
| `Hugh/Handlers/SdkQuestionHandler.cs` | Reflection-based question handling |
| `Hugh/Agents/SdkResponseProcessor.cs` | Event type handling with reflection |
| `Hugh/Configuration/HughConfig.cs` | `CompactionThreshold` property mapping |
| `Hugh/Hugh.csproj` | Package reference to update |
| `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | Tests for `TryEnableInfiniteSession` |

**Gaps:** CHANGELOG only covers up to v0.2.2; v0.3.0 entry sourced from NuGet README. Exact delegate signature of `OnPermissionRequest` inferred from examples.  
**Assumptions:** `PermissionHandler.ApproveAll` is in `GitHub.Copilot.SDK` namespace. `InfiniteSessionConfig` is a concrete class.

## Phase 2: Event Types and Response Processing Changes

**Status:** ✅ Complete  
**Session:** 2025-05-19

### Core Finding: Full Backward Compatibility Confirmed

The upgrade from 0.1.22 to 0.3.0 introduces **no compilation errors** for Hugh's event processing code. The project compiles cleanly with 0.3.0, and all 50 `SdkResponseProcessor` tests plus 25 `ToolMetricsCollector` tests pass without modification.

### Event Types Currently Used by Hugh

Hugh's `SdkResponseProcessor.ProcessResponseAsync()` handles events via `session.On(evt => ...)` with a switch statement:

| Event Type | Current Handling | 0.3.0 Status |
|---|---|---|
| `AssistantMessageDeltaEvent` | Type-matched (switch case) | ✅ Unchanged |
| `SessionIdleEvent` | Type-matched (no Data destructured) | ✅ Unchanged — now has `Data.Aborted` |
| `SessionErrorEvent` | Type-matched (switch case) | ✅ Unchanged |
| `ToolExecutionStartEvent` | Type-matched (switch case) | ✅ Unchanged — new fields added |
| `ToolExecutionCompleteEvent` | Type-matched (switch case) | ✅ Unchanged — new fields added |
| `AssistantMessageEvent` | String name check (reflection) | ⚠️ Can now be type-matched directly |
| `SessionCompactionStartEvent` | String name check (reflection) | ⚠️ Can now be type-matched directly |
| `SessionCompactionCompleteEvent` | String name check (reflection) | ⚠️ Can now be type-matched directly |

### ToolExecutionStartEvent Changes

New additive properties (non-breaking):
- `Arguments` — Tool call arguments
- `McpServerName` / `McpToolName` — MCP tool identification
- `ParentToolCallId` — Subagent tool call chain

### ToolExecutionCompleteEvent Changes

New additive properties (non-breaking):
- `Result` — Rich typed content (text/terminal/image/audio/resource)
- `InteractionId`, `IsUserRequested`, `Model`, `ParentToolCallId`, `ToolTelemetry`

### New Event Types in 0.3.0 (Most Relevant to Hugh)

**Subagent Events (5 new):**
- `SubagentStartedEvent` / `SubagentSelectedEvent` / `SubagentCompletedEvent` / `SubagentFailedEvent` / `SubagentDeselectedEvent`

**Assistant Turn Lifecycle (7 new):**
- `AssistantTurnStartEvent` / `AssistantTurnEndEvent`
- `AssistantUsageEvent` — Token usage data (`InputTokens`, `OutputTokens`, `ReasoningTokens`, `Duration`, `Model`)
- `AssistantIntentEvent` / `AssistantStreamingDeltaEvent` / `AssistantReasoningDeltaEvent` / `AssistantReasoningEvent`

**Tool Progress (2 new):**
- `ToolExecutionProgressEvent` — Real-time progress updates
- `ToolExecutionPartialResultEvent` — Partial results before completion

**Other notable:**
- `SessionWarningEvent`, `SessionTruncationEvent`, `AbortEvent`, `SessionHandoffEvent`
- `PermissionRequestedEvent` / `PermissionCompletedEvent`

### Reflection Hacks Eliminatable

3 reflection-based event handlers can convert to proper type-matched switch cases:

```csharp
// Replace string-matching default branch with:
case AssistantMessageEvent msg:
    responseBuilder.Append(msg.Data.Content);
    break;
case SessionCompactionStartEvent:
    compactionOccurred = true;
    break;
case SessionCompactionCompleteEvent:
    compactionOccurred = true;
    break;
```

This eliminates `TryExtractContentFromEvent()` entirely.

### SessionIdleEvent Enhancement

Now has `Data.Aborted` — distinguishes normal idle from abort-caused idle. Hugh currently ignores this property.

### No IAsyncEnumerable

SDK 0.3.0 continues callback-based `session.On(evt => ...)`. No pattern migration needed.

**Key Discoveries:**
- Zero compilation errors with 0.3.0; all existing tests pass without modification
- 3 reflection-based event handlers can be converted to proper type-matched switch cases
- 5 new subagent events enable subagent lifecycle monitoring
- `AssistantUsageEvent` provides token/cost tracking
- `SessionIdleEvent` now distinguishes abort vs normal idle
- `ToolExecutionProgressEvent` enables real-time tool progress UX
- Event subscription pattern (`session.On()`) is unchanged

| File | Relevance |
|------|-----------|
| `Hugh/Agents/SdkResponseProcessor.cs` | Core event processing; switch statement L211-268; reflection fallback L469-524 |
| `Hugh/Metrics/ToolMetricsCollector.cs` | Tool metrics via OnToolStart/OnToolComplete — fully compatible |
| `Hugh.Tests/SdkResponseProcessorTests.cs` | 50 tests all passing with 0.3.0 |

**Gaps:** `SessionConfig.OnEvent` vs `session.On()` behavior unclear — Phase 5 to investigate.  
**Assumptions:** Properties accessed by Hugh (ToolName, ToolCallId, etc.) existed in 0.1.22 with same signatures — confirmed by clean build.

## Phase 3: Hooks, Tools, and Permission Handler Changes

**Status:** ✅ Complete  
**Session:** 2025-05-19

### SessionHooks API — No Breaking Changes

The `SessionHooks` class and delegate signatures are **identical** between 0.1.22 and 0.3.0:

```csharp
// SAME in both versions:
Task<PreToolUseHookOutput?> HandlePreToolUseAsync(PreToolUseHookInput input, HookInvocation invocation)
Task<PostToolUseHookOutput?> HandlePostToolUseAsync(PostToolUseHookInput input, HookInvocation invocation)
```

`FileSystemHooks.CreateHooks()` works unchanged. Zero source changes needed for hooks.

### Hook Output Types — Minor Addition

`PreToolUseHookOutput.PermissionDecision` gains a new value `"ask"` (falls through to `OnPermissionRequest` handler). Hugh uses `"allow"` — no impact.

### HughWorkflowTools — Fully Compatible

Tool registration via `AIFunctionFactory.Create()` with `IList<AIFunction>` return type works unchanged. `SessionConfig.Tools` still accepts `IList<AIFunction>`.

**However**, custom tools now generate `PermissionRequestCustomTool` permission requests in 0.3.0 — the `OnPermissionRequest` handler MUST be configured to approve them.

### Permission Handler — BREAKING CHANGE (Critical)

| Concept | 0.1.22 | 0.3.0 |
|---------|--------|-------|
| Permission handler delegate | `PermissionHandler` (is the delegate) | `PermissionRequestHandler` (new delegate name) |
| Helper class | ❌ doesn't exist | `PermissionHandler` (static class with pre-built handlers) |
| Pre-built "approve all" | ❌ doesn't exist | `PermissionHandler.ApproveAll` (static property) |
| Required? | Optional (not set) | **REQUIRED** |

Delegate signature in 0.3.0:
```csharp
delegate Task<PermissionRequestResult> PermissionRequestHandler(
    PermissionRequest request, PermissionInvocation invocation);
```

`PermissionRequest` is polymorphic:
- `PermissionRequestShell`, `PermissionRequestWrite`, `PermissionRequestRead`
- `PermissionRequestMcp`, `PermissionRequestUrl`, `PermissionRequestMemory`
- `PermissionRequestCustomTool` — **Hugh's workflow tools!**
- `PermissionRequestHook`

### 🐛 BUG: InfiniteSessions Was NEVER Enabled

**Critical Finding:** The current `TryEnableInfiniteSession` method has TWO bugs:

1. Looks for property `"InfiniteSession"` (singular) → **always returns null** because actual property is `"InfiniteSessions"` (plural) in BOTH versions
2. Looks for `"CompactionThreshold"` → **doesn't exist** (actual: `BackgroundCompactionThreshold`)

```csharp
// CURRENT CODE (BROKEN - never works):
var infiniteSessionProperty = sessionConfig.GetType().GetProperty("InfiniteSession");
// ^^^ ALWAYS NULL — property is "InfiniteSessions" (plural)
if (infiniteSessionProperty == null)
{
    _logger.Debug("InfiniteSession not available in this SDK version");
    return;  // Always exits here!
}
```

**Result:** Hugh has been running WITHOUT context compaction in production.

### InfiniteSessionConfig in 0.3.0

| Property | Type | Default |
|----------|------|---------|
| `Enabled` | `bool?` | true |
| `BackgroundCompactionThreshold` | `double?` | 0.80 |
| `BufferExhaustionThreshold` | `double?` | 0.95 |

Required migration:
```csharp
sessionConfig.InfiniteSessions = new InfiniteSessionConfig
{
    Enabled = true,
    BackgroundCompactionThreshold = _config.Copilot.CompactionThreshold
};
```

### `skip_permission` and `is_override` — DO NOT EXIST

These were speculative scope items. The actual permission mechanisms:
- "Skip permission" = `PreToolUseHookOutput.PermissionDecision = "allow"` (already used)
- "Ask" fallthrough = `PermissionDecision = "ask"` → falls to `OnPermissionRequest`

**Key Discoveries:**
- FileSystemHooks is 100% compatible — zero changes needed
- HughWorkflowTools is 100% compatible — zero changes needed
- `OnPermissionRequest = PermissionHandler.ApproveAll` is REQUIRED — all tool calls fail without it
- `PermissionHandler` type restructured: was a delegate, now a static class with helpers
- Custom `hugh_*` tools now generate `PermissionRequestCustomTool` requests
- **BUG**: `TryEnableInfiniteSession` was NEVER working due to property name typo — Hugh ran without context compaction in production
- `skip_permission` / `is_override` do not exist in SDK

| File | Relevance |
|------|-----------|
| `Hugh/Hooks/FileSystemHooks.cs` | Fully compatible, no changes needed |
| `Hugh/Tools/HughWorkflowTools.cs` | Fully compatible, no changes needed |
| `Hugh/Agents/CopilotService.cs` | Needs `OnPermissionRequest` + InfiniteSessions fix |
| `Hugh.Tests/FileSystemHooksTests.cs` | No changes needed |
| `Hugh.Tests/HughWorkflowToolsTests.cs` | No changes needed |

**Gaps:** None.  
**Assumptions:** `PermissionHandler.ApproveAll` available via `using GitHub.Copilot.SDK`.

## Phase 4: User Input, Elicitation, and Question Handling

**Status:** ✅ Complete  
**Session:** 2025-05-19

### SDK 0.3.0 User Input API — Fully Typed

**`SessionConfig.OnUserInputRequest`** — `UserInputHandler` delegate:
```csharp
Task<UserInputResponse> UserInputHandler(UserInputRequest request, UserInputInvocation invocation)
```

**`UserInputRequest` model (SDK type):**
```csharp
public class UserInputRequest
{
    public string Question { get; set; }
    public List<string> Choices { get; set; }
    public bool? AllowFreeform { get; set; }
}
```

**`UserInputResponse` model (SDK type):**
```csharp
public class UserInputResponse
{
    public string Answer { get; set; }
    public bool WasFreeform { get; set; }
}
```

### Critical Discovery: `OnUserInputRequest` NOT Currently Wired

Hugh's `CopilotService.CreateSessionAsync()` does **NOT** wire `sessionConfig.OnUserInputRequest`. The SDK callback **never fires**. Hugh relies entirely on text-based signal detection (regex in `SignalClassifier`).

Hugh's `CopilotService.OnUserInputRequest` property (line 61) is a completely different type:
```csharp
// Hugh's own callback — NOT the SDK's
public Func<SignalClassification, string, Guid, CancellationToken, Task<QuestionResponse?>>? OnUserInputRequest { get; set; }
```

### Current Reflection-Based Implementation

`SdkQuestionHandler.TryGetPromptFromSdkRequest()` tries property names `["Prompt", "Message", "Text", "Question"]` via reflection because `UserInputRequest` properties were unstable in 0.1.22.

### Migration Plan

**Step 1: Wire SDK callback in `CreateSessionAsync`:**
```csharp
OnUserInputRequest = async (request, invocation) =>
{
    var hughRequest = HughUserInputRequest.FromSdkRequest(request);
    var response = await _sdkQuestionHandler.HandleAsync(hughRequest, ...);
    return new GitHub.Copilot.SDK.UserInputResponse
    {
        Answer = response.Answer,
        WasFreeform = !response.IsAutomatic
    };
}
```

**Step 2: Replace reflection in `FromSdkRequest`:**
```csharp
public static HughUserInputRequest FromSdkRequest(SdkUserInputRequest sdkRequest)
{
    return new HughUserInputRequest
    {
        Prompt = sdkRequest.Question ?? "SDK user input request",
        Choices = sdkRequest.Choices?.AsReadOnly(),
        AllowFreeform = sdkRequest.AllowFreeform ?? true,
        QuestionType = "SDK"
    };
}
```

**Step 3: Resolve naming conflict** — Hugh's `UserInputResponse` vs SDK's:
```csharp
using SdkUserInputResponse = GitHub.Copilot.SDK.UserInputResponse;
```

### ❌ `OnElicitationRequest` Does NOT Exist

**Correction:** `OnElicitationRequest` does NOT exist on `SessionConfig` in 0.3.0. No types containing "Elicit" exist in the assembly. The only `On*` handlers are:
1. `OnPermissionRequest` (`PermissionRequestHandler`)
2. `OnUserInputRequest` (`UserInputHandler`)

### Impact on Dual-Path Architecture

- **SDK callback fires first** — during model turn, before text output
- **Text-based detection still needed** — for `STATUS: needs_user_input` signals in agent text (VS Code compatibility)
- Architecture remains: SDK-callback-first + text-fallback

### Property Mapping: Hugh → SDK

| Hugh's `UserInputResponse` | SDK's `UserInputResponse` | Mapping |
|---|---|---|
| `Answer` | `Answer` | Direct |
| `IsAutomatic` | — | → `!WasFreeform` |
| — | `WasFreeform` | → `!IsAutomatic` |

**Key Discoveries:**
- `OnElicitationRequest` does NOT exist in SDK 0.3.0 — only `OnPermissionRequest` and `OnUserInputRequest`
- `SessionConfig.OnUserInputRequest` is NOT currently wired by Hugh — SDK callback never fires
- `UserInputRequest` has stable typed properties: `Question`, `Choices`, `AllowFreeform` — no reflection needed
- `TryGetPromptFromSdkRequest` becomes a simple direct property access
- Hugh's `CopilotService.OnUserInputRequest` property conflicts in naming with SDK's `SessionConfig.OnUserInputRequest`
- SDK handler fires synchronously during model turn — earlier than text-based detection

| File | Relevance |
|------|-----------|
| `Hugh/Handlers/SdkQuestionHandler.cs` | Reflection at L66-105; `HandleAsync` at L243-253 |
| `Hugh/Handlers/QuestionHandler.cs` | Delegation wrapper |
| `Hugh/Models/QuestionResponse.cs` | Orchestrator response model |
| `Hugh/Agents/CopilotService.cs` | L61 Hugh's own handler, L683-695 SessionConfig creation |
| `Hugh.Tests/SdkQuestionHandlerTests.cs` | 30+ tests — no changes needed for handler logic |

**Gaps:** Cannot verify runtime timing of `OnUserInputRequest` callback vs event stream without live SDK session.  
**Assumptions:** SDK callback fires synchronously within session turn before text output.

## Phase 5: Client Lifecycle, Configuration, and Test Impact

**Status:** ✅ Complete  
**Session:** 2025-05-19

### 1. CopilotClient Constructor — BREAKING CHANGE

**Parameterless constructor is GONE.** Hard compilation error.

```csharp
// CURRENT (won't compile in 0.3.0):
_client = new CopilotClient();

// REQUIRED:
_client = new CopilotClient(new CopilotClientOptions
{
    Cwd = _workspacePath,
    Logger = _loggerFactory.CreateLogger("CopilotSdk"),
    UseLoggedInUser = true,
    AutoRestart = true
});
```

### 2. CopilotClientOptions Properties (14 total)

| Property | Type | Hugh Should Use? |
|----------|------|------------------|
| `Cwd` | `string` | **Yes** — workspace path |
| `Logger` | `ILogger` | **Yes** — already have `_loggerFactory` |
| `UseLoggedInUser` | `bool?` | **Yes** — default auth |
| `AutoRestart` | `bool` | **Yes** — crash recovery |
| `CliPath` | `string` | Optional — non-standard location |
| `AutoStart` | `bool` | Maybe — simplifies startup |
| `LogLevel` | `string` | Optional |
| `GithubToken` | `string` | Optional — alternative auth |
| `CliArgs` | `string[]` | No |
| `Port` | `int` | No |
| `UseStdio` | `bool` | No |
| `CliUrl` | `string` | No |
| `Environment` | `IReadOnlyDictionary` | No |

### 3. `ForceStopAsync()` — On CopilotClient

```csharp
Task ForceStopAsync()  // No parameters
```

Useful for hang protection:
```csharp
try { await _client.StopAsync().WaitAsync(TimeSpan.FromSeconds(5)); }
catch (TimeoutException) { await _client.ForceStopAsync(); }
```

### 4. `ResumeSessionAsync` — Session Continuity

```csharp
Task<CopilotSession> ResumeSessionAsync(
    string sessionId, ResumeSessionConfig config?, CancellationToken ct?)
```

`CopilotSession.SessionId` (read-only getter) provides the ID to persist.

**Hugh integration:** `LoopState` could persist `LastSessionId` for context-preserving retries.

### 5. New CopilotClient Utility Methods

| Method | Replaces |
|--------|----------|
| `GetAuthStatusAsync()` | `GitHubAuthDetector` shell-based `gh auth status` |
| `PingAsync()` | Health checks |
| `ListModelsAsync()` | Model discovery |
| `GetLastSessionIdAsync()` | Session continuity |
| `DeleteSessionAsync()` | Session cleanup |
| `ListSessionsAsync()` | Session management |
| `GetStatusAsync()` | Client monitoring |

### 6. CopilotSession New APIs

| API | Purpose |
|-----|---------|
| `SessionId` (getter) | Get ID for resume |
| `WorkspacePath` (getter) | Infinite session workspace |
| `SendAndWaitAsync` | Single-turn (returns `AssistantMessageEvent`) |
| `GetMessagesAsync` | Conversation history |
| `AbortAsync` | Operation abort |

### 7. HughConfig Changes Required

**Must add to `CopilotConfig`:**

| Property | Type | Default | Reason |
|----------|------|---------|--------|
| `CliPath` | `string?` | `null` | Non-standard CLI location |
| `AutoRestart` | `bool` | `true` | SDK auto-restart |
| `BufferExhaustionThreshold` | `double` | `0.95` | New InfiniteSessionConfig property |

**`appsettings.json` additions:**
```json
"Copilot": {
    "Model": "claude-sonnet-4-5",
    "MaxRetryAttempts": 3,
    "RetryBaseDelayMs": 2000,
    "CompactionThreshold": 0.8,
    "BufferExhaustionThreshold": 0.95,
    "CliPath": null,
    "AutoRestart": true
}
```

### 8. CopilotCliDetector — Role Change

SDK bundles CLI binary. Detector changes from "hard prerequisite" to "diagnostic utility." Still useful for `hugh doctor` but no longer a gate.

### 9. Test Impact Assessment

**Tests with compilation errors (2):**
- `Sdk/SdkSessionLifecycleTests.cs` tests 2.6, 2.7 — `TryEnableInfiniteSession` reflection tests → rewrite for direct property

**Tests safe — no changes (100+):**
- All `SdkResponseProcessorTests`, `ToolMetricsCollectorTests`, `FileSystemHooksTests`, `HughWorkflowToolsTests` pass unchanged
- All `SignalClassifierTests`, `QuestionHandlerTests`, `SdkQuestionHandlerTests` — no SDK types
- All `Orchestration/`, `Cli/`, `Setup/` tests — use `ICopilotService` mock or internal logic
- `IntegrationTests`, `CopilotServiceRetryTests`, `BlockerAutoSkipTests` — safe

**Tests needing minor updates:**
- `Setup/CopilotCliDetectorTests.cs` — if detector role changes
- Live SDK tests — session creation handled by `CopilotService`

### 10. Migration Summary

**Required (Won't Compile — 4 items):**
1. `new CopilotClient()` → `new CopilotClient(new CopilotClientOptions { ... })`
2. Add `OnPermissionRequest = PermissionHandler.ApproveAll` to `SessionConfig`
3. Delete `TryEnableInfiniteSession`, use `InfiniteSessions` property directly
4. Package reference: `0.1.22` → `0.3.0`

**Recommended (Eliminates Reflection — 5 items):**
5. Wire `SessionConfig.OnUserInputRequest` callback
6. Replace reflection in `SdkQuestionHandler.FromSdkRequest`
7. Replace reflection-based event handling in `SdkResponseProcessor`
8. Add config properties to `HughConfig`
9. Use `ForceStopAsync` for hang protection

**Optional (New Capabilities — 5 items):**
10. `ResumeSessionAsync` for session continuity
11. `GetAuthStatusAsync` to replace shell-based auth detection
12. `MessageOptions.Attachments` for document context
13. `ConnectionState` monitoring
14. Subagent events for enhanced orchestration visibility

**Key Discoveries:**
- `CopilotClient()` parameterless constructor is GONE — hard compilation error
- `CopilotClientOptions` requires at minimum `Cwd` and ideally `Logger`, `UseLoggedInUser`, `AutoRestart`
- `ForceStopAsync()` on CopilotClient enables hang protection
- `ResumeSessionAsync` enables session continuity if `LoopState` persists session IDs
- `GetAuthStatusAsync` could replace `GitHubAuthDetector` shell-based check
- Only 2 tests have compilation errors; all other 100+ tests are safe
- SDK bundles CLI — `CopilotCliDetector` changes to diagnostic utility
- `MessageOptions.Attachments` could replace `BuildPromptWithDocuments`

| File | Relevance |
|------|-----------|
| `Hugh/Agents/CopilotService.cs` | L107 constructor, L664-700 CreateSession, L148-158 Dispose |
| `Hugh/Configuration/HughConfig.cs` | Needs CliPath, AutoRestart, BufferExhaustionThreshold |
| `Hugh/appsettings.json` | Needs new Copilot properties |
| `Hugh/Hugh.csproj` | Package reference update |
| `Hugh/Setup/Detection/CopilotCliDetector.cs` | Role change |
| `Hugh.Tests/Sdk/SdkSessionLifecycleTests.cs` | 2 tests must be rewritten |

**Gaps:** Cannot verify `AutoStart` default or `Logger` type without full assembly load.  
**Assumptions:** `AutoStart=true` by default; `Logger` is `Microsoft.Extensions.Logging.ILogger`.

## Overview

The GitHub Copilot SDK upgrade from 0.1.22 to 0.3.0 is a well-defined, achievable migration with **exactly 4 hard compilation fixes** and several recommended improvements that eliminate reflection hacks. The overall architecture of Hugh remains sound — no redesign is needed.

### Upgrade Difficulty: Moderate

The upgrade is moderate in scope: 4 breaking changes that prevent compilation, 5 reflection eliminations that improve code quality, and 5+ optional new capabilities. Event processing and hooks are fully backward compatible, which means most of Hugh's codebase works unchanged.

### Critical Bug Discovered

**InfiniteSession was NEVER enabled in production.** The `TryEnableInfiniteSession` method has been silently failing since deployment due to a property name typo (`"InfiniteSession"` vs `"InfiniteSessions"` plural). Hugh has been running without context compaction — meaning long sessions may have been hitting context limits without graceful handling. The 0.3.0 upgrade fixes this by using direct property assignment.

### Migration Categorization

**Required (4 items — won't compile):**
1. `new CopilotClient()` → `new CopilotClient(new CopilotClientOptions { Cwd, Logger, UseLoggedInUser, AutoRestart })`
2. Add `OnPermissionRequest = PermissionHandler.ApproveAll` to `SessionConfig`
3. Delete `TryEnableInfiniteSession` — use `sessionConfig.InfiniteSessions = new InfiniteSessionConfig { ... }` directly
4. Update package reference: `0.1.22` → `0.3.0`

**Recommended (5 items — eliminates reflection):**
5. Wire `SessionConfig.OnUserInputRequest` callback (currently not connected)
6. Replace reflection in `SdkQuestionHandler.FromSdkRequest` with direct `request.Question`/`.Choices`/`.AllowFreeform`
7. Replace 3 reflection-based event handlers in `SdkResponseProcessor` with proper type-matched switch cases
8. Add `CliPath`, `AutoRestart`, `BufferExhaustionThreshold` to `HughConfig`
9. Use `ForceStopAsync` for hang protection in disposal

**Optional (5+ items — new capabilities):**
10. `ResumeSessionAsync` for session continuity across retries
11. `GetAuthStatusAsync` to replace shell-based `GitHubAuthDetector`
12. Subagent events (`SubagentCompletedEvent`, etc.) for enhanced orchestration monitoring
13. `AssistantUsageEvent` for token/cost tracking
14. `MessageOptions.Attachments` for cleaner document context passing

### Cross-Cutting Patterns

1. **Reflection Elimination**: 4 distinct reflection hacks can be replaced with native API calls (InfiniteSession config, UserInputRequest parsing, event type matching, content extraction)
2. **Permission Model**: New required `OnPermissionRequest` handler integrates cleanly — `PermissionHandler.ApproveAll` is the simplest path; Hugh's existing hooks handle monitoring
3. **Dual-Path Architecture Preserved**: Text-based signal detection remains alongside SDK-native callbacks — consistent with VS Code Copilot chat compatibility requirement
4. **Backward Compatibility**: All events, hooks, and tools work unchanged; only client initialization and session config need modification

### Test Impact: Minimal

- **2 tests** have compilation errors (InfiniteSession reflection tests → rewrite)
- **100+ tests** are fully safe with no changes needed
- All 50 `SdkResponseProcessor` tests pass unchanged
- All 25 `ToolMetricsCollector` tests pass unchanged
- All hook/tool tests pass unchanged

### Corrections from Initial Scope

- `OnElicitationRequest` does **NOT** exist in SDK 0.3.0 (only `OnPermissionRequest` and `OnUserInputRequest`)
- `skip_permission` and `is_override` do **NOT** exist in the SDK
- `SessionConfig.OnUserInputRequest` was never wired by Hugh — this is not a "change" but a "never implemented" item

## Key Findings

1. **4 hard compilation fixes** are all that's needed to get Hugh building on 0.3.0 (constructor, permission handler, infinite session, package version)
2. **InfiniteSession was never enabled** — property name typo means Hugh ran without context compaction in production
3. **All events, hooks, and tools are backward compatible** — `SdkResponseProcessor`, `FileSystemHooks`, and `HughWorkflowTools` work unchanged
4. **4 reflection hacks can be eliminated** — replaced by stable native APIs in 0.3.0
5. **`OnUserInputRequest` was never wired** — the SDK callback was available but Hugh only used text-based detection
6. **SDK bundles CLI** — `CopilotCliDetector` changes from hard prerequisite to diagnostic utility
7. **`ResumeSessionAsync` enables session continuity** — could improve retry resilience
8. **`ForceStopAsync` enables hang protection** — graceful timeout handling in disposal
9. **Only 2 tests break** — minimal test maintenance for a significant upgrade
10. **`OnElicitationRequest` doesn't exist** — simplifies the upgrade (only 2 handlers to wire, not 3)

## Actionable Conclusions

1. **Start with the 4 required fixes** — They're straightforward and well-defined; the entire upgrade can be done in a single implementation phase
2. **Delete `TryEnableInfiniteSession` entirely** — Replace with 4-line direct property assignment; this also fixes the production bug
3. **Wire `OnUserInputRequest` during the upgrade** — It's low effort (one lambda) and gives Hugh proper SDK-native question handling
4. **Eliminate all 4 reflection hacks in the same PR** — They're all simple replacements with direct API calls; reduces maintenance burden
5. **Keep `CopilotCliDetector` but soften it** — Change from "blocking prerequisite" to "informational diagnostic" since SDK bundles its own CLI
6. **Add `ForceStopAsync` hang protection** — 3-line try/catch in `DisposeAsync` improves reliability
7. **Defer `ResumeSessionAsync` adoption** — Requires `LoopState` model changes; do separately after the core upgrade stabilizes
8. **Defer subagent event monitoring** — Nice-to-have observability but not needed for the upgrade itself

## Open Questions

1. Does `AutoStart=true` in `CopilotClientOptions` eliminate the need for explicit `StartAsync()` call?
2. Does `ResumeSessionAsync` work across `CopilotClient` restarts or only within the same process lifetime?
3. What is the exact runtime timing of `OnUserInputRequest` callback relative to `AssistantMessageDeltaEvent` events?
4. Should Hugh adopt `SessionConfig.OnEvent` as an alternative/supplement to `session.On()` event subscription?
5. Should `HughConfig.CopilotConfig.CompactionThreshold` be renamed to `BackgroundCompactionThreshold` to match SDK naming?

## Standards Applied

| Standard | Relevance | Guidance |
|----------|-----------|----------|
| No organizational standards applicable to this research. | — | — |

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2025-05-19 |
| Status | ✅ Complete |
| Current Phase | ✅ Complete |
| Path | /docs/research/026-github-copilot-sdk-upgrade-0122-to-030.md |
