# Implementation Plan: GitHub Copilot SDK Integration for Hugh

## Document Information

| Field | Value |
|-------|-------|
| Document ID | 009 |
| Feature | GitHub Copilot SDK Integration |
| Created | 2026-02-05 |
| Last Updated | 2026-02-05 |
| Status | ✅ Ready for Implementation |
| Owner | pch-planner |
| Version | v3.3 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-05 | pch-planner | Initial plan creation |
| v1.1 | 2026-02-05 | pch-planner | Added 6 planning decisions |
| v2.0 | 2026-02-05 | pch-planner | Holistic review completed |
| v2.1 | 2026-02-05 | pch-planner | Added Phase 8: Documentation Updates |
| v3.0 | 2026-02-05 | pch-plan-reviewer | Plan reviewed: 4 clarifying questions resolved; interface/disposal updates |
| v3.1 | 2026-02-05 | pch-plan-reviewer | Re-review: Fixed 6 consistency issues; updated architecture diagram; added line number specificity |
| v3.2 | 2026-02-05 | pch-planner | Added Phase 0: Project Rename (Huge → Hugh); updated all file paths and references |
| v3.3 | 2026-02-05 | pch-plan-reviewer | Re-review of Phase 0: corrected Agents file count (5, not 6) |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-02-05 | CopilotClient Lifecycle Strategy | C — Client Per-Orchestrator Run | Aligns with LoopOrchestrator lifecycle; fresh sessions per-phase for context isolation; minimizes CLI startup overhead; works with existing _globalCts shutdown pattern |
| 2 | 2026-02-05 | CopilotClientManager Ownership | B — Owned by CopilotService (Lazy Init) | Maintains ICopilotService abstraction boundary; SDK details fully encapsulated; follows existing pattern of CopilotService owning handlers; lazy init useful for testing |
| 3 | 2026-02-05 | Agent Prompt Loading Strategy | C — Extend ICopilotService for AgentDefinition | More semantic interface; CopilotService has full metadata to load prompts; better encapsulation of prompt loading within service |
| 4 | 2026-02-05 | Retry Logic Scope | B — Retry at Session Send Level Only | Preserves session context on transient failures; prevents single missed response from forcing phase restart; session creation failures rare and should propagate |
| 5 | 2026-02-05 | Test Strategy for SDK Integration | C — Integration Tests with Test Fixtures | Focus on integration tests with proper fixtures; skip unit tests for SDK code; pure logic classes (SdkResponseProcessor, DocumentPathExtractor) tested separately |
| 6 | 2026-02-05 | Timeout Override Format | A — Simple Duration (All Phases) | Single duration applies uniformly; per-phase config exists in appsettings.json; simple parsing; covers common "I need more time" use case |

---

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 4 | **Last Updated:** 2026-02-05

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | AgentPromptLoader duplication in PhaseExecutor and CopilotService | Clarity | Option B: PhaseExecutor loads prompts | Interface + Phase 4.4 updated |
| 2 | SessionConfig fate after CopilotService takes over session creation | Clarity | Option A: Remove entirely | Phase 5.2 updated |
| 3 | Timeout format consistency (30m vs HH:mm:ss) | Specificity | Option A: TimeSpan format only | Phase 6.1 updated |
| 4 | CopilotService disposal wiring specifics | Correctness | Option B: LoopOrchestrator disposes | Phase 5.5-5.6 updated |

---

## Holistic Review Summary

### Decision Interactions

| Decisions | Interaction | Resolution |
|-----------|-------------|------------|
| D1 (Client Per-Run) + D2 (CopilotService owns) | Client must be disposed at end of orchestrator run | LoopOrchestrator implements IAsyncDisposable; disposes CopilotService via PhaseExecutor (per Review Q4) |
| D2 (Lazy Init) + D3 (AgentDefinition param) | CopilotService receives prompt content from PhaseExecutor | PhaseExecutor loads prompts; passes agentPrompt string to CopilotService |
| D3 (AgentDefinition) + PhaseExecutor | Interface change from `string agentName` to `AgentDefinition` | PhaseExecutor is sole caller; update in Phase 5 |
| D4 (Retry at Send) + D2 (Client owned) | Session must remain valid for retry; client must remain started | SendWithRetryAsync retries only SendAsync; session stays open; client lazily started once |

### Architectural Considerations Addressed

1. **Disposal Chain**: `Program.cs` → `LoopOrchestrator.DisposeAsync()` → `PhaseExecutor` → `CopilotService.DisposeAsync()` → `CopilotClient.StopAsync()`. Phase 5.5-5.6 ensures proper wiring (per Review Q4).

2. **AgentPromptLoader Ownership**: PhaseExecutor continues to load and cache prompts via AgentPromptLoader (per Review Q1). CopilotService receives the prompt string as a parameter — no AgentPromptLoader injection needed.

3. **Error Classification**: `SendWithRetryAsync` needs to distinguish transient vs. permanent errors. Implementation note added with `IsTransientError(ex)` helper checking for network/rate-limit indicators.

4. **Context Compaction**: InfiniteSessionConfig with `CompactionThreshold = 0.8` prevents context exhaustion during long coding phases.

### Trade-offs Accepted

| Trade-off | Decision | Rationale |
|-----------|----------|-----------|
| No per-phase timeout via CLI | D6 (Simple Duration) | Complexity vs. utility; appsettings.json covers advanced cases |
| Integration tests only for SDK | D5 | SDK types difficult to mock; pure logic components tested separately |
| Breaking interface change | D3 | Sole caller is internal; cleaner long-term API |

### Risks Acknowledged

| Risk | Mitigation |
|------|------------|
| SDK breaking changes | Pin version 0.1.22; wrap in abstraction (CopilotService is the abstraction) |
| Disposal not called on crash | `_globalCts` cancellation triggers graceful shutdown; client has AutoRestart |

---

## Overview

### Feature Summary

Integrate the GitHub.Copilot.SDK (v0.1.22) into the Hugh self-operating development loop to replace the current stub implementation in `CopilotService.cs` with a fully functional SDK integration. This will enable actual agent execution rather than simulated operations.

### Referenced Research

- [009-github-copilot-sdk-integration.md](../research/009-github-copilot-sdk-integration.md) — Comprehensive SDK integration research

### Objectives

1. Add the GitHub.Copilot.SDK NuGet package to the Hugh project
2. Create `SdkResponseProcessor` class for event-based response handling
3. Create `DocumentPathExtractor` class for robust document path extraction
4. Replace stub implementation in `CopilotService.ExecutePhaseAsync` with full SDK integration
5. Implement timeout override functionality in `Program.cs`
6. Add retry logic with exponential backoff for transient errors
7. Ensure all existing tests pass and add new tests for SDK integration

### TODOs to Resolve

| File | Line | TODO Description | Resolution |
|------|------|------------------|------------|
| CopilotService.cs | 65 | Integrate with GitHub.Copilot.SDK | SDK client/session integration |
| CopilotService.cs | 79 | Parse response from SDK | `SdkResponseProcessor` |
| CopilotService.cs | 91 | Extract OutputDocumentPath | `DocumentPathExtractor` |
| CopilotService.cs | 130 | Replace SimulateAgentExecutionAsync | Full SDK implementation |
| PhaseExecutor.cs | 248 | Parse agent response for document path | Use `DocumentPathExtractor` |
| Program.cs | 206, 236 | Apply timeout override | `ApplyTimeoutOverride()` helper |

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | SDK integration creates CopilotClient and manages CLI lifecycle | P0 |
| FR-2 | Each phase execution creates a fresh CopilotSession with agent prompt | P0 |
| FR-3 | Agent prompts loaded via existing `AgentPromptLoader` | P0 |
| FR-4 | Documents attached using SDK's `UserMessageDataAttachmentsItem` | P0 |
| FR-5 | Response events accumulated and classified via existing `SignalClassifier` | P0 |
| FR-6 | Questions intercepted via `OnUserInputRequest` callback | P0 |
| FR-7 | Document paths extracted from agent responses | P0 |
| FR-8 | CLI timeout override supported via `--timeout` argument | P1 |
| FR-9 | Transient errors retried with exponential backoff | P1 |

### Non-Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-1 | SDK wrapped to allow future version updates | P1 |
| NFR-2 | InfiniteSessionConfig enabled to prevent context exhaustion | P1 |
| NFR-3 | All existing unit tests continue to pass | P0 |
| NFR-4 | New components have unit test coverage | P1 |

### Out of Scope

- Custom tool implementation via Microsoft.Extensions.AI (future enhancement)
- BYOK provider configuration
- Multi-model per-phase configuration

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                        Program.cs                               │
│  ├── ParseTimeoutOverride() → Apply uniform timeout to config   │
│  └── BuildOrchestrator() → Inject dependencies                  │
├─────────────────────────────────────────────────────────────────┤
│                     LoopOrchestrator : IAsyncDisposable         │
│  └── PhaseExecutor                                              │
│        ├── AgentPromptLoader → Loads agent prompts              │
│        ├── GetPhaseTimeout() → CreateLinkedTokenSource          │
│        ├── PrepareDocumentAttachments()                         │
│        └── ICopilotService.ExecutePhaseAsync(AgentDefinition,   │
│                             agentPrompt, prompt, docs, ct)      │
├─────────────────────────────────────────────────────────────────┤
│          CopilotService : ICopilotService, IAsyncDisposable     │
│  ├── CopilotClient (lazy init, reused across phases)            │
│  ├── HughConfig (retry/compaction settings)                     │
│  ├── CreateSessionAsync(agentPrompt) → New session per phase    │
│  │     ├── SessionConfig.SystemMessage → Agent prompt           │
│  │     ├── SessionConfig.OnUserInputRequest → Questions         │
│  │     └── SessionConfig.InfiniteSession → Auto-compaction      │
│  │                                                              │
│  ├── SendWithRetryAsync() → Retry at send level only            │
│  │                                                              │
│  ├── SdkResponseProcessor                                       │
│  │     ├── AssistantMessageDeltaEvent → Accumulate              │
│  │     ├── SessionIdleEvent → Classify + Extract path           │
│  │     └── SessionErrorEvent → Exception handling               │
│  │                                                              │
│  ├── DocumentPathExtractor → 4 regex patterns                   │
│  ├── SignalClassifier → Unchanged from existing                 │
│  └── QuestionHandler/BlockerHandler → Unchanged                 │
├─────────────────────────────────────────────────────────────────┤
│               GitHub.Copilot.SDK (v0.1.22)                      │
│  └── Copilot CLI (spawned process, AutoRestart=true)            │
│        └── LLM + MCP Tools                                      │
└─────────────────────────────────────────────────────────────────┘
```

### Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| Generated Regex | `SignalClassifier.cs`, `PhaseExecutor.cs` | `DocumentPathExtractor` will follow |
| Async/Await with CancellationToken | `PhaseExecutor.cs` | SDK integration maintains this pattern |
| Record types for DTOs | `PhaseResult.cs`, `FileAttachment` | New `ProcessedResponse` record |
| ILogger usage | Throughout Hugh | New classes follow same pattern |
| Dependency injection via constructor | `PhaseExecutor`, `CopilotService` | HughConfig injected into CopilotService (Q1: AgentPromptLoader stays in PhaseExecutor) |
| IAsyncDisposable | (new) | CopilotService + LoopOrchestrator for disposal chain |

### New Components

#### 1. SdkResponseProcessor (Hugh/Agents/SdkResponseProcessor.cs)

Processes SDK response events and integrates with existing `SignalClassifier`.

```csharp
public partial class SdkResponseProcessor
{
    // Dependencies
    private readonly SignalClassifier _signalClassifier;
    private readonly DocumentPathExtractor _pathExtractor;
    private readonly ILogger _logger;

    // Result record
    public record ProcessedResponse
    {
        public string Content { get; init; } = "";
        public SignalClassification Signal { get; init; }
        public string? OutputDocumentPath { get; init; }
        public bool IsComplete { get; init; }
        public Exception? Error { get; init; }
    }

    // Main method: processes session after SendAsync
    public async Task<ProcessedResponse> ProcessResponseAsync(
        CopilotSession session,
        CancellationToken ct);
}
```

#### 2. DocumentPathExtractor (Hugh/Agents/DocumentPathExtractor.cs)

Extracts document paths from agent responses using multiple regex patterns.

```csharp
public partial class DocumentPathExtractor
{
    // Pattern 1: "Created vision document at: docs/vision/001.md"
    [GeneratedRegex(@"(?:created|saved|wrote|generated|updated)\s+(?:vision|research|plan|document)?:?\s*(?:to|at)?:?\s*[`""']?([^\s`""'\n]+\.md)[`""']?", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExplicitCreationRegex();

    // Pattern 2: "**Document:** docs/vision/001.md"
    [GeneratedRegex(@"\*\*Document(?:\s+Location)?:\*\*\s*[`""']?([^\s`""'\n]+\.md)[`""']?", 
        RegexOptions.Compiled)]
    private static partial Regex HandoffDocumentRegex();

    // Pattern 3: "[001-feature.md](docs/plans/001-feature.md)"
    [GeneratedRegex(@"\[([^\]]+\.md)\]\(([^)]+\.md)\)", RegexOptions.Compiled)]
    private static partial Regex MarkdownLinkRegex();

    // Pattern 4: "created at: `docs/research/001.md`"
    [GeneratedRegex(@"(?:created|saved|updated|written)\s+(?:at|to)?:?\s*`([^`]+\.md)`", 
        RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex BacktickPathRegex();

    public string? ExtractDocumentPath(string agentResponse);
    public IReadOnlyList<string> ExtractAllDocumentPaths(string agentResponse);
}
```

### API Changes

#### ICopilotService Interface

**Before:**
```csharp
public interface ICopilotService
{
    Task<PhaseResult> ExecutePhaseAsync(
        string agentName,
        string prompt,
        IEnumerable<string> documentPaths,
        CancellationToken ct);
}
```

**After:**
```csharp
public interface ICopilotService : IAsyncDisposable
{
    Task<PhaseResult> ExecutePhaseAsync(
        AgentDefinition agentDefinition,
        string agentPrompt,  // System message loaded by PhaseExecutor
        string prompt,       // User prompt
        IEnumerable<string> documentPaths,
        CancellationToken ct);
}
```

#### CopilotService Constructor

**Before:**
```csharp
public CopilotService(
    DecisionLogger decisionLogger,
    ILogger? logger = null)
```

**After:**
```csharp
public CopilotService(
    HughConfig config,
    DecisionLogger decisionLogger,
    ILogger? logger = null)
```

> **Review Update (Q1):** AgentPromptLoader removed from CopilotService. PhaseExecutor loads prompts and passes `agentPrompt` string to `ExecutePhaseAsync`.

#### PhaseExecutor.ExecuteWithTokenAsync

**Before:**
```csharp
return await _copilotService.ExecutePhaseAsync(
    agentDefinition.Name,
    prompt,
    documentPaths,
    ct);
```

**After:**
```csharp
return await _copilotService.ExecutePhaseAsync(
    agentDefinition,
    agentPrompt,  // Loaded by PhaseExecutor via AgentPromptLoader
    prompt,
    documentPaths,
    ct);
```

### Configuration Additions

#### CopilotConfig (HughConfig.cs)

Add SDK-specific settings:

```csharp
public class CopilotConfig
{
    public string Model { get; set; } = "claude-sonnet-4-5";
    
    // NEW: SDK retry settings
    public int MaxRetryAttempts { get; set; } = 3;
    public int RetryBaseDelayMs { get; set; } = 2000;
    
    // NEW: Context compaction threshold
    public double CompactionThreshold { get; set; } = 0.8;
}
```

### Data Model Changes

None required. Existing `PhaseResult`, `Decision`, `FileAttachment`, and `AgentDefinition` records are sufficient.

### Error Handling Strategy

| Error Type | Source | Recovery |
|------------|--------|----------|
| Session creation failure | `CreateSessionAsync` | Propagate immediately (fatal) |
| Send transient error | `SendAsync` | Retry with exponential backoff (2s, 4s, 8s) |
| SessionErrorEvent | Event stream | Check IsTransient; retry send if transient |
| Timeout | `CancelAfter` | Return timeout result; mark phase failed |
| User cancellation | `_globalCts` | AbortAsync + propagate for shutdown |
| SignalType.Blocker | Agent response | Pause execution; require human input |
| SignalType.QuestionOpen | Agent response | AbortAsync; pause for user |

---

## Dependencies and Prerequisites

| Dependency | Type | Status | Notes |
|------------|------|--------|-------|
| GitHub.Copilot.SDK v0.1.22 | NuGet Package | Not Installed | Must be added to Hugh.csproj |
| Copilot CLI | External Tool | Assumed Installed | Must be in PATH |
| GitHub Authentication | Runtime | Assumed Configured | Via GitHub CLI login |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| SDK breaking changes (technical preview) | Medium | High | Pin version, wrap in abstraction |
| CLI process instability | Low | Medium | AutoRestart enabled by default |
| Context window exhaustion | Medium | Medium | InfiniteSessionConfig with compaction |
| Test environment without CLI | High | Medium | Conditional test execution / mocking |

---

## Progress Summary

| Phase | Description | Status |
|-------|-------------|--------|
| 0 | Project Rename (Huge → Hugh) | ✅ Complete |
| 1 | Package & Configuration Setup | ✅ Complete |
| 2 | DocumentPathExtractor Component | ✅ Complete |
| 3 | SdkResponseProcessor Component | ✅ Complete |
| 4 | CopilotService SDK Integration | ✅ Complete |
| 5 | PhaseExecutor & Interface Updates | ✅ Complete |
| 6 | Program.cs Timeout Override | ✅ Complete |
| 7 | Testing & Validation | ✅ Complete |
| 8 | Documentation Updates | ✅ Complete |
---

## Execution Plan

---

### Phase 0: Project Rename (Huge → Hugh)

**Estimated Session Size:** Large
**Files to Modify:** 25+
**Prerequisites:** None (must complete before all other phases)
**Entry Point:** Start by renaming folders, then project files, then update all references
**Verification:** `dotnet build` succeeds; `dotnet test` passes; all namespaces updated

> **Context:** The project name "Huge" was a misspelling of "Hugh", the Borg character from Star Trek: The Next Generation. This phase corrects all references to use the proper spelling.

#### Scope of Changes

| Category | Before | After |
|----------|--------|-------|
| **Folders** | `Hugh/`, `Hugh.Tests/` | `Hugh/`, `Hugh.Tests/` |
| **Project Files** | `Huge.csproj`, `Huge.Tests.csproj` | `Hugh.csproj`, `Hugh.Tests.csproj` |
| **Config Class** | `HughConfig` | `HughConfig` |
| **Namespaces** | `namespace Huge.*`, `using Huge.*` | `namespace Hugh.*`, `using Hugh.*` |
| **Config Binding** | `"Hugh":` section | `"Hugh":` section |
| **CLI Command** | `huge` | `hugh` |
| **Log Files** | `logs/huge-.log` | `logs/hugh-.log` |

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 0.1 | Rename `Hugh/` folder to `Hugh/` | Folder rename | Folder exists at `Hugh/` |
| 0.2 | Rename `Hugh.Tests/` folder to `Hugh.Tests/` | Folder rename | Folder exists at `Hugh.Tests/` |
| 0.3 | Rename `Huge.csproj` to `Hugh.csproj` | `Hugh/Huge.csproj` → `Hugh/Hugh.csproj` | Project file renamed |
| 0.4 | Rename `HughConfig.cs` to `HughConfig.cs` | `Hugh/Configuration/HughConfig.cs` → `Hugh/Configuration/HughConfig.cs` | Config file renamed |
| 0.5 | Rename `Huge.Tests.csproj` to `Hugh.Tests.csproj` | `Hugh.Tests/Huge.Tests.csproj` → `Hugh.Tests/Hugh.Tests.csproj` | Test project file renamed |
| 0.6 | Update project reference in test project | `Hugh.Tests/Hugh.Tests.csproj` | `<ProjectReference Include="..\Hugh\Hugh.csproj" />` |
| 0.7 | Update namespace in `HughConfig.cs` | `Hugh/Configuration/HughConfig.cs` | `namespace Hugh.Configuration;`, class renamed to `HughConfig` |
| 0.8 | Update all `namespace Huge` declarations | All `.cs` files in `Hugh/` | All namespaces changed to `namespace Hugh.*` |
| 0.9 | Update all `using Huge.*` statements | All `.cs` files in `Hugh/` | All using statements changed to `using Hugh.*` |
| 0.10 | Update `namespace Huge.Tests` declarations | All `.cs` files in `Hugh.Tests/` | All namespaces changed to `namespace Hugh.Tests` |
| 0.11 | Update all `using Huge.*` statements in tests | All `.cs` files in `Hugh.Tests/` | All using statements changed to `using Hugh.*` |
| 0.12 | Update `HughConfig` class reference to `HughConfig` | All `.cs` files referencing `HughConfig` | `HughConfig` used everywhere |
| 0.13 | Update `HughConfig` variable names to `hughConfig` | `Hugh/Program.cs`, `Hugh/Orchestration/*.cs` | All variable names updated |
| 0.14 | Update config section binding in Program.cs | `Hugh/Program.cs` | `configuration.GetSection("Hugh").Bind(hughConfig);` |
| 0.15 | Update appsettings.json config section | `Hugh/appsettings.json` | `"Hugh": {` instead of `"Hugh": {` |
| 0.16 | Update CLI command description | `Hugh/Program.cs` | `"Hugh - Self-Operating Development Loop"` |
| 0.17 | Update log file paths | `Hugh/Program.cs` | `path: "logs/hugh-.log"` (two occurrences) |
| 0.18 | Update CLI example hints | `Hugh/Program.cs` | `hugh "..."`, `hugh --status`, `hugh --resume` |
| 0.19 | Update console banner | `Hugh/ConsoleOutput.cs` | `"Hugh - Self-Operating Dev Loop"` |
| 0.20 | Update status display header | `Hugh/ConsoleOutput.cs` | `"Hugh Status"` |
| 0.21 | Update log messages | `Hugh/Program.cs` | `"Hugh starting..."`, `"Hugh terminated unexpectedly"` |
| 0.22 | Update test workspace temp folder names | `Hugh.Tests/LoopStateManagerTests.cs`, `Hugh.Tests/IntegrationTests.cs` | `HughTests_`, `HughIntegrationTests_` |
| 0.23 | Update integration test comments | `Hugh.Tests/IntegrationTests.cs` | `Integration test harness for the Hugh development loop.` |
| 0.24 | Fix image reference in README | `Hugh/README.md` | `![Hugh](assets/hugh.png)` (asset file already correctly named) |
| 0.25 | Update README title and all references | `Hugh/README.md` | `# Hugh`, all CLI examples use `hugh`, config shows `"Hugh":` |
| 0.26 | Update prerequisite checker comment | `Hugh/PrerequisiteChecker.cs` | `Validates prerequisites required for Hugh to run.` |
| 0.27 | Update doc comments throughout | All `.cs` files | Replace "Huge" with "Hugh" in all XML doc comments |
| 0.28 | Run `dotnet restore` | — | Restore succeeds with renamed projects |
| 0.29 | Run `dotnet build` | — | Build succeeds with no errors |
| 0.30 | Run `dotnet test` | — | All existing tests pass |

#### Documentation Updates (Part of Phase 0)

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 0.31 | Update research document title and references | `docs/research/009-github-copilot-sdk-integration.md` | All `Huge` → `Hugh`, all file paths updated |
| 0.32 | Update this plan document file paths and references | `docs/plans/009-github-copilot-sdk-integration.md` | All `Hugh/` paths → `Hugh/`, `HughConfig` → `HughConfig` |

#### Files Affected Summary

**Source Files (Hugh/):**
- `Program.cs` — Namespaces, config binding, log paths, CLI hints, log messages
- `ConsoleOutput.cs` — Banner, status header
- `PrerequisiteChecker.cs` — Namespace, doc comment
- `Configuration/HughConfig.cs` — File rename, namespace, class name
- `Agents/*.cs` — All namespaces and using statements (5 files)
- `Detection/*.cs` — All namespaces (3 files)
- `Documents/DocumentManager.cs` — Namespace
- `Handlers/*.cs` — All namespaces and using statements (2 files)
- `Logging/DecisionLogger.cs` — Namespace and using
- `Models/*.cs` — All namespaces (4 files)
- `Orchestration/*.cs` — All namespaces, using, and HughConfig references (3 files)
- `appsettings.json` — Config section name
- `README.md` — Title, examples, structure

**Test Files (Hugh.Tests/):**
- `IntegrationTests.cs` — Namespaces, using, temp folder names, doc comments
- `LoopStateManagerTests.cs` — Namespaces, using, temp folder names
- `SignalClassifierTests.cs` — Namespaces, using
- `Hugh.Tests.csproj` — Project reference path

**Documentation:**
- `docs/research/009-github-copilot-sdk-integration.md` — All Huge references
- `docs/plans/009-github-copilot-sdk-integration.md` — All file paths and type references

---

### Phase 1: Package & Configuration Setup

**Estimated Session Size:** Small
**Files to Modify:** 2
**Prerequisites:** Phase 0 complete
**Entry Point:** Start by reading `Hugh/Hugh.csproj`
**Verification:** `dotnet build` succeeds with new package

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Add GitHub.Copilot.SDK package reference | `Hugh/Hugh.csproj` | `<PackageReference Include="GitHub.Copilot.SDK" Version="0.1.22" />` present |
| 1.2 | Add SDK retry configuration to CopilotConfig | `Hugh/Configuration/HughConfig.cs` | `MaxRetryAttempts`, `RetryBaseDelayMs`, `CompactionThreshold` properties added |
| 1.3 | Update appsettings.json with default SDK settings | `Hugh/appsettings.json` | New Copilot section values present with defaults |
| 1.4 | Run `dotnet restore` and `dotnet build` | — | Build succeeds with no errors |

---

### Phase 2: DocumentPathExtractor Component

**Estimated Session Size:** Small
**Files to Modify:** 1 (new)
**Prerequisites:** Phase 1 complete
**Entry Point:** Review `Hugh/Orchestration/PhaseExecutor.cs` lines 299-304 for existing `DocumentPathRegex()` pattern
**Verification:** Unit tests for DocumentPathExtractor pass

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Create DocumentPathExtractor class | `Hugh/Agents/DocumentPathExtractor.cs` | Class created with 4 generated regex patterns |
| 2.2 | Implement ExtractDocumentPath method | `Hugh/Agents/DocumentPathExtractor.cs` | Returns first matched path using priority order |
| 2.3 | Implement ExtractAllDocumentPaths method | `Hugh/Agents/DocumentPathExtractor.cs` | Returns all unique paths found |
| 2.4 | Add NormalizePath helper method | `Hugh/Agents/DocumentPathExtractor.cs` | Strips quotes, backticks; normalizes separators |
| 2.5 | Create unit tests for DocumentPathExtractor | `Hugh.Tests/DocumentPathExtractorTests.cs` | Tests for each pattern + edge cases |

---

### Phase 3: SdkResponseProcessor Component

**Estimated Session Size:** Medium
**Files to Modify:** 1 (new)
**Prerequisites:** Phase 2 complete (DocumentPathExtractor)
**Entry Point:** Review `Hugh/Detection/SignalClassifier.cs` for Classify() method
**Verification:** Unit tests for SdkResponseProcessor pass

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Create ProcessedResponse record | `Hugh/Agents/SdkResponseProcessor.cs` | Record with Content, Signal, OutputDocumentPath, IsComplete, Error |
| 3.2 | Create SdkResponseProcessor class with dependencies | `Hugh/Agents/SdkResponseProcessor.cs` | Constructor takes SignalClassifier, DocumentPathExtractor, ILogger |
| 3.3 | Implement ProcessResponseAsync method | `Hugh/Agents/SdkResponseProcessor.cs` | Event subscription, response accumulation, signal classification |
| 3.4 | Add helper methods IsCompletionSignal, IsPauseSignal | `Hugh/Agents/SdkResponseProcessor.cs` | Static helpers for signal type checking |
| 3.5 | Create unit tests for SdkResponseProcessor | `Hugh.Tests/SdkResponseProcessorTests.cs` | Mock event stream; verify classification; verify path extraction |

---

### Phase 4: CopilotService SDK Integration

**Estimated Session Size:** Large
**Files to Modify:** 2
**Prerequisites:** Phases 1-3 complete
**Entry Point:** Read full `Hugh/Agents/CopilotService.cs` and `Hugh/Agents/ICopilotService.cs`
**Verification:** Build succeeds; existing tests still pass

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Update ICopilotService to extend IAsyncDisposable | `Hugh/Agents/ICopilotService.cs` | Interface extends IAsyncDisposable |
| 4.2 | Change ExecutePhaseAsync signature to accept AgentDefinition + agentPrompt | `Hugh/Agents/ICopilotService.cs` | Params: AgentDefinition, string agentPrompt, string prompt, documentPaths, ct |
| 4.3 | Add using statements for SDK namespaces | `Hugh/Agents/CopilotService.cs` | `using GitHub.Copilot.SDK;` at top |
| 4.4 | Update CopilotService constructor | `Hugh/Agents/CopilotService.cs` | Add `HughConfig config` as first parameter; store in `_config` field for retry/compaction settings. No AgentPromptLoader (PhaseExecutor handles prompt loading per Q1) |
| 4.5 | Add private CopilotClient field with lazy initialization | `Hugh/Agents/CopilotService.cs` | `_client` field; `EnsureClientStartedAsync()` method |
| 4.6 | Implement IAsyncDisposable.DisposeAsync | `Hugh/Agents/CopilotService.cs` | Disposes _client if initialized |
| 4.7 | Implement CreateSessionAsync helper | `Hugh/Agents/CopilotService.cs` | Creates session with agent prompt, OnUserInputRequest, InfiniteSession |
| 4.8 | Implement SendWithRetryAsync helper | `Hugh/Agents/CopilotService.cs` | Retry at send level with exponential backoff (3 attempts) |
| 4.9 | Rewrite ExecutePhaseAsync with full SDK integration | `Hugh/Agents/CopilotService.cs` | Replace stub; use received agentPrompt; create session; send; process response |
| 4.10 | Remove SimulateAgentExecutionAsync method | `Hugh/Agents/CopilotService.cs` | Stub method deleted |
| 4.11 | Wire OnUserInputRequest to existing handlers | `Hugh/Agents/CopilotService.cs` | Route to SignalClassifier → QuestionHandler/BlockerHandler |

---

### Phase 5: PhaseExecutor & Interface Updates

**Estimated Session Size:** Medium
**Files to Modify:** 4
**Prerequisites:** Phase 4 complete
**Entry Point:** Read `Hugh/Orchestration/PhaseExecutor.cs`, `Hugh/Orchestration/LoopOrchestrator.cs`, and `Hugh/Program.cs`
**Verification:** Build succeeds; integration compiles

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Update ExecuteWithTokenAsync signature and implementation | `Hugh/Orchestration/PhaseExecutor.cs` | Remove `SessionConfig sessionConfig` parameter; pass `agentPrompt` string (loaded earlier) to `_copilotService.ExecutePhaseAsync(agentDefinition, agentPrompt, prompt, ...)` |
| 5.2 | Remove SessionConfig record, BuildSessionConfig(), and call site | `Hugh/Orchestration/PhaseExecutor.cs` | Delete `SessionConfig` record (lines 307-340), `BuildSessionConfig()` method (lines 168-179), and the call in `ExecuteAsync` (lines 77-78); remove `sessionConfig` variable from `ExecuteWithTokenAsync` call (line 88) |
| 5.3 | Update ExtractOutputDocumentPath to use result path | `Hugh/Orchestration/PhaseExecutor.cs` | Trust result.OutputDocumentPath from SDK; fallback pattern retained |
| 5.4 | Update BuildOrchestrator to inject new CopilotService dependencies | `Hugh/Program.cs` | Pass HughConfig to CopilotService constructor (no AgentPromptLoader per Q1) |
| 5.5 | Make LoopOrchestrator implement IAsyncDisposable | `Hugh/Orchestration/LoopOrchestrator.cs` | LoopOrchestrator disposes PhaseExecutor → CopilotService chain (per Q4) |
| 5.6 | Add DisposeAsync call for LoopOrchestrator at shutdown | `Hugh/Program.cs` | Call `_orchestrator.DisposeAsync()` in finally block |

---

### Phase 6: Program.cs Timeout Override

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 5 complete
**Entry Point:** Read `Hugh/Program.cs` lines 203-247 (HandleResumeCommand, HandleNewFeatureCommand)
**Verification:** `hugh new "test" --timeout 00:30:00` applies timeout correctly

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 6.1 | Create ParseTimeoutOverride helper method | `Hugh/Program.cs` | Uses TimeSpan.TryParse for HH:mm:ss format; returns TimeSpan? |
| 6.2 | Create ApplyTimeoutOverride helper method | `Hugh/Program.cs` | Sets all TimeoutConfig properties to override value |
| 6.3 | Replace TODO in HandleResumeCommand | `Hugh/Program.cs` | Calls ParseTimeoutOverride → ApplyTimeoutOverride |
| 6.4 | Replace TODO in HandleNewFeatureCommand | `Hugh/Program.cs` | Calls ParseTimeoutOverride → ApplyTimeoutOverride |
| 6.5 | Add warning log if timeout format is invalid | `Hugh/Program.cs` | Logs warning and continues with default if parse fails |

---

### Phase 7: Testing & Validation

**Estimated Session Size:** Medium
**Files to Modify:** 2-3
**Prerequisites:** Phases 1-6 complete
**Entry Point:** Run `dotnet test` to verify existing tests pass
**Verification:** All tests pass; manual integration test succeeds

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 7.1 | Run existing unit tests | — | All existing tests pass |
| 7.2 | Add DocumentPathExtractor tests if not created in Phase 2 | `Hugh.Tests/DocumentPathExtractorTests.cs` | Tests for all 4 patterns |
| 7.3 | Add SdkResponseProcessor tests if not created in Phase 3 | `Hugh.Tests/SdkResponseProcessorTests.cs` | Tests for signal detection, path extraction |
| 7.4 | Update any existing tests that mock ICopilotService | `Hugh.Tests/*.cs` | Update method signatures to match new interface |
| 7.5 | Create integration test with [Category("Integration")] | `Hugh.Tests/IntegrationTests.cs` | Add test that invokes real SDK (skipped without CLI) |
| 7.6 | Manual end-to-end test | — | Run `hugh new "test feature"` and verify agent executes |

---

### Phase 8: Documentation Updates

**Estimated Session Size:** Small
**Files to Modify:** 3
**Prerequisites:** Phases 1-7 complete
**Entry Point:** Read existing `Hugh/README.md` and `.github/copilot-instructions.md`
**Verification:** Documentation accurately reflects SDK integration

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 8.1 | Update Hugh README with SDK prerequisites | `Hugh/README.md` | Copilot CLI installation instructions; GitHub auth requirements |
| 8.2 | Document new configuration options | `Hugh/README.md` | CopilotConfig section: Model, MaxRetryAttempts, CompactionThreshold |
| 8.3 | Update CLI usage examples | `Hugh/README.md` | `--timeout` flag documented with format examples |
| 8.4 | Update architecture section | `Hugh/README.md` | SDK integration diagram or description added |
| 8.5 | Update copilot-instructions.md for Hugh project | `.github/copilot-instructions.md` | Add section on Hugh SDK integration patterns; document CopilotService usage |
| 8.6 | Add troubleshooting section | `Hugh/README.md` | Common SDK errors; CLI not found; auth failures |

---

## Implementation Notes for AI Agents

### Key SDK Patterns

1. **CopilotClient lifecycle**: Create once, reuse across sessions. Use `await using` for automatic disposal.
   ```csharp
   await using var client = new CopilotClient();
   await client.StartAsync();
   ```

2. **Session creation with agent prompt**:
   ```csharp
   await using var session = await client.CreateSessionAsync(new SessionConfig
   {
       Model = _config.Copilot.Model,
       SystemMessage = new SystemMessageConfig
       {
           Mode = SystemMessageMode.Append,
           Content = agentPrompt
       },
       InfiniteSession = new InfiniteSessionConfig
       {
           Enabled = true,
           CompactionThreshold = _config.Copilot.CompactionThreshold
       }
   });
   ```

3. **OnUserInputRequest callback for autonomous mode**:
   ```csharp
   OnUserInputRequest = async (request, invocation) =>
   {
       var signal = _signalClassifier.Classify(request.Message);
       if (signal.Type == SignalType.QuestionWithRecommendation)
       {
           // Auto-respond with recommendation
           return new UserInputResponse { Answer = signal.RecommendationLetter };
       }
       // Pause for blockers/open questions
       await session.AbortAsync();
       return new UserInputResponse { ShouldAbort = true };
   }
   ```

4. **Event handling pattern**:
   ```csharp
   var responseBuilder = new StringBuilder();
   var completionSource = new TaskCompletionSource();
   
   session.On(evt =>
   {
       switch (evt)
       {
           case AssistantMessageDeltaEvent delta:
               responseBuilder.Append(delta.Content);
               break;
           case SessionIdleEvent:
               completionSource.TrySetResult();
               break;
           case SessionErrorEvent error:
               completionSource.TrySetException(new Exception(error.Message));
               break;
       }
   });
   
   await session.SendAsync(new MessageOptions { Prompt = prompt, Attachments = attachments });
   await completionSource.Task;
   ```

5. **Retry at send level**:
   ```csharp
   private async Task SendWithRetryAsync(CopilotSession session, MessageOptions options, CancellationToken ct)
   {
       int attempt = 0;
       while (true)
       {
           try
           {
               await session.SendAsync(options);
               return;
           }
           catch (Exception ex) when (IsTransientError(ex) && attempt < _config.Copilot.MaxRetryAttempts)
           {
               attempt++;
               var delay = TimeSpan.FromMilliseconds(_config.Copilot.RetryBaseDelayMs * Math.Pow(2, attempt - 1));
               await Task.Delay(delay, ct);
           }
       }
   }
   ```

### Critical Implementation Details

- **Lazy client initialization**: Only start CopilotClient when first phase executes, not in constructor
- **Abort on pause signals**: When QuestionOpen or Blocker detected, call `session.AbortAsync()` before returning
- **Preserve existing handlers**: `QuestionHandler` and `BlockerHandler` remain unchanged; route signals through them
- **Generated regex in DocumentPathExtractor**: Use `[GeneratedRegex]` attribute with `RegexOptions.Compiled` for performance
- **Cancellation integration**: Register cancellation token with TaskCompletionSource via `ct.Register()`

### Testing Notes

- Mock `ICopilotService` at the interface boundary for unit tests
- `DocumentPathExtractor` and `SdkResponseProcessor` are pure logic — test without SDK mocks
- Integration tests require Copilot CLI installed; use `[Category("Integration")]` attribute to skip in CI

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | 8 files across 3 directories |
| New patterns introduced | 2 | SDK client/session pattern; IAsyncDisposable chain |
| External dependencies | 3 | GitHub.Copilot.SDK, Copilot CLI |
| Migration complexity | 1 | No data migration; interface change internal only |
| Test coverage required | 3 | Unit (2 new classes) + Integration tests |
| **Overall Complexity** | 12/25 | **Medium** |

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

*No organizational standards applicable to this plan.*

---

## Review Summary

**Review Date:** 2026-02-05
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.1
**Reviewed Plan Version:** v3.3

### Review Metrics
- Initial Review Issues Found: 10 (Critical: 0, Major: 5, Minor: 5)
- Re-Review Issues Found: 6 (Critical: 0, Major: 2, Minor: 4)
- Phase 0 Review Issues Found: 1 (Critical: 0, Major: 0, Minor: 1)
- Clarifying Questions Asked: 4 (initial) + 0 (re-review) + 0 (Phase 0)
- Sections Updated: Interface, Phases 4-5, Holistic Review Summary, Architecture Diagram, Files Affected Summary

### Key Improvements Made (Initial Review)
1. **Interface clarification (Q1):** PhaseExecutor loads prompts; CopilotService receives `agentPrompt` string parameter instead of AgentPromptLoader injection
2. **Dead code removal (Q2):** SessionConfig record and BuildSessionConfig() marked for removal from PhaseExecutor
3. **Format consistency (Q3):** Timeout CLI uses standard TimeSpan format (HH:mm:ss) consistent with appsettings.json
4. **Disposal chain (Q4):** LoopOrchestrator implements IAsyncDisposable and owns CopilotService disposal

### Re-Review Corrections (v3.0 → v3.1)
1. **Review Session Log cleanup:** Removed duplicate entries for questions 3-4 that appeared both resolved and pending
2. **Phase 5.1-5.2 specificity:** Added exact line numbers for SessionConfig removal; documented ExecuteWithTokenAsync signature change
3. **Architecture diagram fix:** Updated to show AgentPromptLoader in PhaseExecutor (not CopilotService); shows LoopOrchestrator as IAsyncDisposable
4. **Codebase patterns table:** Corrected dependency injection pattern description
5. **PhaseExecutor.ExecuteWithTokenAsync example:** Added missing `agentPrompt` parameter to "After" code
6. **Progress Summary table:** Fixed formatting (missing pipe character)

### Phase 0 Review (v3.2 → v3.3)
1. **Codebase verification:** All 32 steps verified against actual file paths, namespaces, and patterns
2. **Files count correction:** Agents folder contains 5 files (not 6 as originally stated)
3. **Step ordering validated:** Folder renames before file renames is correct — plan file paths reflect intermediate state

### Codebase Verification
- ✅ All file paths verified to exist
- ✅ TODO line numbers confirmed accurate (65, 79, 91, 130 in CopilotService; 248 in PhaseExecutor; 206, 236 in Program)
- ✅ SessionConfig location verified: lines 307-340 in PhaseExecutor.cs
- ✅ BuildSessionConfig location verified: lines 168-179 in PhaseExecutor.cs
- ✅ Existing patterns (GeneratedRegex, ILogger, record types) validated
- ✅ AgentPromptLoader and SignalClassifier integration points verified
- ✅ Phase 0 rename targets verified: 21 namespace declarations, 20+ using statements, 5 Agents files

### Remaining Considerations
- Integration tests require Copilot CLI in PATH — document in README
- SDK v0.1.22 is a technical preview — monitor for breaking changes
- Phase 0 folder renames should be executed via `git mv` for proper history tracking

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** 2026-02-05
**Reviewed By:** pch-plan-reviewer
**Review Date:** 2026-02-05
**Status:** ✅ Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** /docs/plans/009-github-copilot-sdk-integration.md
