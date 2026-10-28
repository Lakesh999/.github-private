# Implementation Plan: Huge — Self-Operating Development Loop

## Document Information

| Field | Value |
|-------|-------|
| Document | 008-self-operating-development-loop.md |
| Version | v3.0 |
| Created | 2026-02-05 |
| Status | ✅ Implementation Complete |
| Owner | pch-planner |
| Vision Source | /docs/vision/002-self-operating-development-loop.md |
| Research Source | /docs/research/008-self-operating-development-loop.md |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-05 | pch-planner | Initial plan creation |
| v2.0 | 2026-02-05 | pch-planner | All planning questions answered; holistic review complete |
| v2.1 | 2026-02-05 | pch-plan-reviewer | Review complete: added Phase 0 for agent format standardization; linked token cancellation strategy |
| v2.2 | 2026-02-05 | pch-plan-reviewer | Renamed project to "Huge" (Star Trek Hugh reference); added README and logo tasks to Phase 1 |
| v2.3 | 2026-02-05 | pch-plan-reviewer | Follow-up review: added ModeSelection detection task (4.5b); verified agent format claims |
| v3.0 | 2026-02-05 | pch-coder | Implementation complete: all 8 phases (51 tasks) completed via automatic mode |

---

## Planning Session Log

| Session | Date | Decision Point | Options Considered | Answer | Rationale |
|---------|------|----------------|-------------------|--------|-----------|
| 1 | 2026-02-05 | Project Structure | A) Single Console, B) Layered Solution, C) Single Project w/ Folders | C | Balance of simplicity and organization for ~3000-5000 LOC MVP |
| 2 | 2026-02-05 | Autonomous Answering | A) Always Autonomous, B) Confidence Threshold, C) Category Rules, D) Accept Recommendations | D | Leverages existing PCH agent recommendation format; minimal new logic |
| 3 | 2026-02-05 | Error Handling | A) Fail Fast, B) Retry w/ Backoff, C) Retry Once + Skip, D) Checkpoint & Resume | D | Documents serve as natural checkpoints; --resume allows controlled recovery |
| 4 | 2026-02-05 | Agent Invocation | A) SDK Direct, B) CLI Process, C) CLI + ACP, D) Hybrid | A | Native .NET API with CreateSessionAsync, OnUserInputRequest, SessionIdleEvent |
| 5 | 2026-02-05 | Document Passing | A) Full Embed, B) SDK Attachments, C) Selective Embed, D) @file Reference | B | Cleaner separation; SDK handles reading; simpler implementation |
| 6 | 2026-02-05 | Coder Multi-Phase | A) Single Phase (Auto), B) External Orchestration, C) Hybrid | B | External orchestration with document phase tracking; unified checkpoint model |
| 7 | 2026-02-05 | Logging | A) Serilog, B) Console + JSON, C) Console Only, D) OpenTelemetry | A | Structured logging, rolling files, de facto .NET standard |
| 8 | 2026-02-05 | Configuration | A) appsettings.json + CLI, B) Env Vars, C) CLI Only, D) YAML | A | Standard .NET pattern; defaults + overrides; familiar to users |

---

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 3 | **Last Updated:** 2026-02-05

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Non-standard recommendation format handling | Correctness | Update agents to enforce consistent format | Phase 0 added; agent format standardization |
| 2 | Cancellation token propagation | Correctness | Option B: Linked Tokens | Task 3.5 updated; Error Handling Patterns updated |
| 3 | ModeSelection signal type missing detection task | Completeness | Add detection task for mode selection prompts | Task 4.5b added to Phase 4 |

---

## Holistic Review Summary

### Decision Interaction Analysis

The 8 planning decisions form a coherent architecture:

| Decision Pair | Interaction | Assessment |
|---------------|-------------|------------|
| SDK Direct (D4) + SDK Attachments (D5) | Both rely on GitHub.Copilot.SDK APIs | ✅ Consistent — same SDK surface |
| Checkpoint/Resume (D3) + External Coder Orchestration (D6) | Checkpoint model must track coding sub-phases | ✅ Compatible — state file supports `currentCodingSubPhase` |
| Accept Recommendations (D2) + Serilog Logging (D7) | Autonomous decisions need audit trail | ✅ Complementary — decisions.jsonl + structured logs |
| Single Project (D1) + Folder Organization | Project size ~3000-5000 LOC | ✅ Appropriate — not over-architected |
| appsettings.json (D8) + Timeouts | Per-phase timeouts configurable | ✅ Natural fit |

### Architectural Considerations Addressed

1. **SDK Preview Instability**: Mitigated by `ICopilotService` abstraction layer. If SDK changes, only one class needs updating.

2. **Context Window Exhaustion**: SDK Attachments (D5) may attach large documents. Monitored risk — can fall back to selective embedding if needed. InfiniteSessions SDK feature available as backup.

3. **Coding Phase Granularity**: External orchestration (D6) reads phase status from plan document. Requires consistent plan format from pch-planner. ✅ PCH agents already use standardized Progress Summary tables.

4. **Autonomous Decision Quality**: "Accept Recommendations Only" (D2) relies on agents providing recommendations. pch-planner and pch-visionary always provide structured recommendations. pch-researcher may have open-ended questions → these correctly escalate to human.

5. **Error Recovery Flow**: Checkpoint/Resume (D3) + logging (D7) ensure failures are recoverable:
   - State persisted before each phase
   - Logs capture what happened
   - `--resume` continues from last checkpoint
   - Human can investigate logs, fix issues, resume

### Trade-offs Accepted

| Trade-off | Accepted Risk | Mitigation |
|-----------|---------------|------------|
| SDK is Technical Preview | API may change | Abstraction layer isolates changes |
| Full document attachment | May waste context tokens | Monitor; can switch to selective embedding |
| External coder orchestration | More complex than single-phase | Unified checkpoint model; consistent with other agents |
| Accept recommendations blindly | May auto-accept poor recommendations | Audit log; agent recommendations have been reliable |

### Gaps Identified and Resolved

1. **Question without recommendation**: What happens if an agent asks a question without a `**Recommendation:**` pattern?
   - **Resolution**: Pause execution, save checkpoint, wait for human. Log as Warning level.

2. **Plan document format varies**: What if plan doesn't have expected Progress Summary table?
   - **Resolution**: Fail gracefully with clear error message. Plans from pch-planner always have this table.

3. **SDK startup failure**: What if Copilot CLI isn't installed?
   - **Resolution**: Check prerequisites at startup. Display clear error with installation instructions.

---

## Overview and Objectives

### Feature Overview

**Huge** — A .NET Core CLI application that orchestrates PCH agents in an autonomous development loop. The system enables fire-and-forget feature development by automatically progressing through vision → research → planning → review → code phases, answering its own clarifying questions, and passing documents between sessions with fresh context per phase.

#### Name Origin

The name "Huge" is derived from [Hugh](https://memory-alpha.fandom.com/wiki/Hugh), a Borg drone from *Star Trek: The Next Generation* who regained his individuality after being severed from the Collective. Like Hugh, this tool operates autonomously — freed from the need for constant human interaction — while still maintaining connection to its purpose.

**Project Logo:** An image of Hugh is available at `C:\Users\vincent.miceli\OneDrive - pointchealth.com\Pictures\d9aec884805e216b126e828ddd67a5aa.jpg` and should be copied to the project folder for the README.

### Key Objectives

1. **Autonomous Agent Orchestration**: Automatically invoke and sequence pch-visionary → pch-researcher → pch-planner → pch-plan-reviewer → pch-coder
2. **Document Continuity**: Pass vision/research/plan documents between phases without manual intervention
3. **Fresh Context Per Phase**: Implement Ralph Wiggum pattern using `CreateSessionAsync()` for each phase
4. **Autonomous Question Answering**: Model answers its own clarifying questions using meta-prompting
5. **Completion Detection**: Detect phase completion via SDK events and content pattern matching

### Success Criteria

- System runs 4+ hours unattended on a typical feature
- 90%+ phase transitions occur without human intervention
- 80%+ questions answered autonomously

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority | Source |
|----|-------------|----------|--------|
| FR-1 | Orchestrate agent phases (vision → research → planning → review → code) | Must Have | Vision |
| FR-2 | Pass documents between phases automatically | Must Have | Vision |
| FR-3 | Fresh context per phase (Ralph Wiggum pattern) | Must Have | Vision |
| FR-4 | Autonomous question answering when enabled | Must Have | Vision |
| FR-5 | Completion detection for phase handoffs | Must Have | Vision |

### Non-Functional Requirements

| ID | Requirement | Priority | Source |
|----|-------------|----------|--------|
| NFR-1 | Reliability: Recover from failures, retry failed phases | Must Have | Vision |
| NFR-2 | Observability: Comprehensive logging | Must Have | Vision |
| NFR-3 | Resilience: Handle crashes, timeouts, malformed outputs | Must Have | Vision |

### Out of Scope

- Multi-user/team support
- GUI/Dashboard
- Deployment automation
- Custom agent development
- Cross-repository operation
- External notifications (Phase 2 — FR-6)

---

## Technical Design

### Codebase Patterns (Reference)

*New project — establishing patterns.*

### Project Structure

```
Huge/
├── Huge.csproj
├── Program.cs                    # CLI entry point, argument parsing
├── appsettings.json              # Configuration
├── /Orchestration
│   ├── LoopOrchestrator.cs       # Main loop logic
│   └── PhaseExecutor.cs          # Execute individual phases
├── /Agents
│   ├── AgentDefinitions.cs       # PCH agent configuration
│   └── AgentSession.cs           # Session wrapper
├── /Detection
│   ├── CompletionDetector.cs     # Phase completion detection
│   └── SignalClassifier.cs       # Output classification
├── /Handlers
│   ├── QuestionHandler.cs        # Autonomous answering
│   └── BlockerHandler.cs         # Handle blockers/failures
├── /Documents
│   ├── DocumentManager.cs        # Read/write docs
│   └── DocumentContext.cs        # Document state tracking
└── /Logging
    └── LoopLogger.cs             # Structured logging
```

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     .NET Core CLI ("huge")                          │
│                                                                         │
│   CopilotClient ──▶ GitHub.Copilot.SDK                                 │
│        │                                                                │
│   ┌────┴────────────────────────────────────────────────────────┐      │
│   │          Session Management (CreateSessionAsync)            │      │
│   │                                                              │      │
│   │   PhaseExecutor ──▶ Runs one phase per fresh session        │      │
│   │   QuestionHandler ──▶ OnUserInputRequest callback           │      │
│   │   CompletionDetector ──▶ SessionIdleEvent + parsing         │      │
│   └──────────────────────────────────────────────────────────────┘      │
│                           │                                             │
│                    PCH Agents (/agents/*.md)                            │
└─────────────────────────────────────────────────────────────────────────┘
```

### Key Components

| Component | Purpose | Key Responsibilities |
|-----------|---------|---------------------|
| `Program.cs` | CLI entry point | Argument parsing, orchestration startup |
| `LoopOrchestrator` | Main orchestration logic | Phase sequencing, document passing, error handling |
| `PhaseExecutor` | Execute individual phases | Session creation, prompt construction, result parsing |
| `QuestionHandler` | Autonomous answering | Detect `**Recommendation:**` pattern, auto-accept recommendations, escalate unstructured questions |
| `CompletionDetector` | Detect phase completion | Pattern matching, signal classification |
| `DocumentManager` | Document I/O | Read/write vision/research/plan docs |

### Autonomous Answering Strategy

**Approach:** Accept Recommended Answers Only

**Prerequisite:** All PCH agents must follow the standardized recommendation format (see Phase 0). This ensures reliable autonomous parsing.

When a PCH agent asks a question:
1. Parse output for `**Recommendation:** [Letter] — [Option Name]` pattern
2. If found → auto-accept the recommendation; log decision with agent's rationale
3. If not found → pause execution; wait for human input (future: escalate via notifications)

**Detection Regex:**
```regex
\*\*Recommendation:\*\*\s+([A-E])\s+[—–-]\s+(.+)
```

**Audit Logging:** Each auto-accepted decision is logged with:
- Question topic
- Options presented
- Recommendation selected
- Agent's rationale

### Error Handling & Recovery

**Approach:** Checkpoint and Resume

**Checkpoint Mechanism:**
- State file: `.orchestrator/loop-state.json` tracks current run
- Each completed phase updates the state file before proceeding
- Document artifacts (vision/research/plan) serve as durable checkpoints

**State File Schema:**
```json
{
  "runId": "uuid",
  "startedAt": "ISO8601",
  "featureRequest": "original prompt",
  "currentPhase": "research",
  "completedPhases": ["vision"],
  "documents": {
    "vision": "/docs/vision/003-feature.md",
    "research": null,
    "plan": null
  },
  "decisions": [
    { "phase": "vision", "question": "...", "answer": "A", "auto": true }
  ],
  "status": "in-progress | paused | completed | failed"
}
```

**CLI Commands:**
- `huge "feature request"` — Start new run
- `huge --resume` — Resume most recent paused/failed run
- `huge --resume <runId>` — Resume specific run
- `huge --status` — Show current run state

**Failure Handling:**
1. On error → log details, update state to "failed", exit cleanly
2. User investigates, fixes if needed
3. `huge --resume` continues from last successful phase

### Agent Invocation via SDK

**Package:** `GitHub.Copilot.SDK` (NuGet)

**Core SDK Usage Pattern:**
```csharp
await using var client = new CopilotClient();
await client.StartAsync();

// Fresh session per phase (Ralph Wiggum pattern)
await using var session = await client.CreateSessionAsync(new SessionConfig
{
    Model = "claude-sonnet-4-5",
    SystemMessage = new SystemMessageConfig
    {
        Content = agentPrompt,  // PCH agent .md content
        Mode = SystemMessageMode.Append
    }
});

// Intercept questions for autonomous answering
session.OnUserInputRequest += (sender, args) =>
{
    var recommendation = ExtractRecommendation(args.Prompt);
    if (recommendation != null)
    {
        args.Response = recommendation.Letter;  // Auto-accept
        LogDecision(recommendation);
    }
    else
    {
        // No recommendation — pause for human
        PauseForUserInput(args);
    }
};

// Send prompt with document attachments
await session.SendAsync(new MessageOptions
{
    Prompt = prompt,
    Attachments = new[] { new FileAttachment(documentPath) }
});

// Detect completion
session.On(evt =>
{
    if (evt is SessionIdleEvent idle)
    {
        // Phase complete
    }
});
```

**Abstraction Layer:**
To isolate from SDK preview changes, wrap in `ICopilotService`:
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

### Document Passing via SDK Attachments

**Approach:** Use `MessageOptions.Attachments` to pass document file paths

**Implementation:**
```csharp
await session.SendAsync(new MessageOptions
{
    Prompt = $"Continue implementing the plan — start Phase {phaseNumber}",
    Attachments = new[]
    {
        new FileAttachment(visionDocPath),
        new FileAttachment(researchDocPath),
        new FileAttachment(planDocPath)
    }
});
```

**Document Flow by Phase:**

| Phase | Agent | Documents Attached |
|-------|-------|--------------------|
| Vision | pch-visionary | (none — starts from feature request) |
| Research | pch-researcher | vision |
| Planning | pch-planner | vision, research |
| Review | pch-plan-reviewer | vision, research, plan |
| Coding | pch-coder | plan (vision/research embedded in plan) |

**Note:** If context limits become an issue with large documents, can revisit with selective embedding strategy.

### pch-coder Multi-Phase Orchestration

**Approach:** Orchestrate Each Coding Phase Externally with Document Phase Tracking

huge invokes pch-coder in "Manual Mode" once per coding phase, reading phase progress from the plan document. This provides:
- Unified checkpoint model across all agents
- Fresh context per coding phase (Ralph Wiggum at coding level)
- Consistent error handling and recovery

**Phase Detection from Plan Document:**

Plans have a Progress Summary table:
```markdown
| Phase | Name | Status | Tasks Complete |
|-------|------|--------|----------------|
| 1 | Project Setup | ✅ Complete | 5/5 |
| 2 | Core Infrastructure | 🔄 In Progress | 2/8 |
| 3 | API Layer | ⏳ Not Started | 0/6 |
```

**Orchestration Logic:**
```csharp
public async Task ExecuteCodingPhasesAsync(string planPath)
{
    while (true)
    {
        var nextPhase = ParseNextIncompletePhase(planPath);
        if (nextPhase == null) break;  // All phases complete
        
        // Fresh session for each coding phase
        var result = await ExecutePhaseAsync(
            agent: "pch-coder",
            prompt: $"Continue implementing the plan — start Phase {nextPhase.Number}",
            documents: new[] { planPath }
        );
        
        // pch-coder updates the plan document with ✅ status
        // Loop continues to next phase
        
        UpdateCheckpoint(currentPhase: $"coding-{nextPhase.Number}");
    }
}
```

**Phase Status Detection Regex:**
```regex
\|\s*(\d+)\s*\|[^|]+\|\s*(⏳|🔄)\s*[^|]+\|
```
Matches phases with "⏳ Not Started" or "🔄 In Progress" status.

**Benefits:**
- Plan document is source of truth for coding progress
- Each coding phase gets fresh context (same as other agents)
- Failure in Phase 3 doesn't lose Phase 1-2 progress
- `--resume` works at coding-phase granularity

### Logging & Observability

**Approach:** Console + File with Serilog

**Packages:**
- `Serilog`
- `Serilog.Sinks.Console`
- `Serilog.Sinks.File`

**Configuration:**
```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: ".orchestrator/logs/huge-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7)
    .CreateLogger();
```

**Log Categories:**

| Category | Level | Example |
|----------|-------|---------|
| Phase start/end | Information | "Starting phase: research" |
| Document operations | Information | "Attached document: /docs/vision/001.md" |
| Autonomous decisions | Information | "Auto-accepted recommendation: A — JWT Authentication" |
| Agent output (summary) | Information | "Phase complete. Output: 2500 chars" |
| Agent output (full) | Debug | Full agent response text |
| Questions requiring human | Warning | "Question requires human input: [topic]" |
| Errors/failures | Error | "Phase failed: timeout after 5m" |
| Checkpoint updates | Information | "Checkpoint saved: research complete" |

**Decision Audit Log:**
In addition to Serilog, autonomous decisions are recorded in `.orchestrator/decisions.jsonl`:
```json
{"timestamp":"...","phase":"planning","question":"Auth Approach","options":["A","B","C"],"selected":"A","rationale":"...","auto":true}
```

### Configuration

**Approach:** appsettings.json + CLI Overrides

**Packages:**
- `Microsoft.Extensions.Configuration`
- `Microsoft.Extensions.Configuration.Json`
- `Microsoft.Extensions.Configuration.CommandLine`

**appsettings.json:**
```json
{
  "Huge": {
    "AgentsPath": "./agents",
    "DocsPath": "./docs",
    "OrchestratorPath": "./.orchestrator",
    "Timeouts": {
      "VisionPhase": "00:10:00",
      "ResearchPhase": "00:30:00",
      "PlanningPhase": "00:20:00",
      "ReviewPhase": "00:15:00",
      "CodingPhasePerPhase": "00:20:00"
    },
    "Autonomous": {
      "Enabled": true,
      "AcceptRecommendationsOnly": true
    },
    "Copilot": {
      "Model": "claude-sonnet-4-5"
    }
  },
  "Serilog": {
    "MinimumLevel": "Information"
  }
}
```

**CLI Argument Overrides:**
```
huge "feature request" --timeout 30m --no-autonomous --model gpt-5
huge --resume --verbose
huge --status
```

### Data Models

```csharp
// Core domain models

public enum LoopPhase
{
    Vision,
    Research,
    Planning,
    Review,
    Coding  // Contains sub-phases (coding-1, coding-2, ...)
}

public enum PhaseStatus
{
    NotStarted,
    InProgress,
    Complete,
    Failed,
    Paused  // Waiting for human input
}

public record LoopState
{
    public Guid RunId { get; init; }
    public DateTime StartedAt { get; init; }
    public string FeatureRequest { get; init; } = "";
    public LoopPhase CurrentPhase { get; init; }
    public int? CurrentCodingSubPhase { get; init; }  // For coding phases
    public List<LoopPhase> CompletedPhases { get; init; } = new();
    public DocumentPaths Documents { get; init; } = new();
    public List<Decision> Decisions { get; init; } = new();
    public PhaseStatus Status { get; init; }
    public string? FailureReason { get; init; }
}

public record DocumentPaths
{
    public string? Vision { get; init; }
    public string? Research { get; init; }
    public string? Plan { get; init; }
}

public record Decision
{
    public string Phase { get; init; } = "";
    public string Question { get; init; } = "";
    public List<string> Options { get; init; } = new();
    public string Selected { get; init; } = "";
    public string Rationale { get; init; } = "";
    public bool Autonomous { get; init; }
    public DateTime Timestamp { get; init; }
}

public record PhaseResult
{
    public bool Success { get; init; }
    public string? OutputDocumentPath { get; init; }
    public string? ErrorMessage { get; init; }
    public List<Decision> DecisionsMade { get; init; } = new();
    public TimeSpan Duration { get; init; }
}

public record AgentDefinition
{
    public string Name { get; init; } = "";
    public string PromptPath { get; init; } = "";
    public LoopPhase Phase { get; init; }
    public TimeSpan DefaultTimeout { get; init; }
}
```

### API Contracts

*Not applicable — CLI application, no external API.*

---

## Dependencies and Prerequisites

### Technical Dependencies

| Dependency | Version | Purpose |
|------------|---------|---------|
| .NET 8.0+ | 8.0 | Runtime |
| GitHub.Copilot.SDK | Latest (preview) | Agent invocation |
| Copilot CLI | Latest | Underlying agent runtime |

### Infrastructure Prerequisites

- GitHub Copilot subscription or BYOK
- Copilot CLI installed and in PATH
- PCH agents available in `/agents/`
- GitHub token with "Copilot Requests" permission

---

## Risk Assessment and Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| SDK is Technical Preview — breaking changes | Medium | High | Abstraction layer, monitor releases |
| Large documents exceed context | Low | Medium | Selective embedding, InfiniteSessions |
| Autonomous answers may be wrong | Medium | Medium | Confidence thresholds, decision audit log |
| Agent completion detection unreliable | Low | High | Redundant detection (SDK events + patterns) |
| Copilot CLI unavailable | Low | Critical | SDK manages CLI lifecycle automatically |

---

## Progress Summary

| Phase | Name | Status | Tasks Complete |
|-------|------|--------|----------------|
| 0 | Agent Format Standardization | ✅ Complete | 3/3 |
| 1 | Project Setup & Configuration | ✅ Complete | 8/8 |
| 2 | Core Infrastructure (SDK, Logging, State) | ✅ Complete | 8/8 |
| 3 | Phase Execution Engine | ✅ Complete | 7/7 |
| 4 | Completion Detection & Signal Classification | ✅ Complete | 7/7 |
| 5 | Autonomous Question Handling | ✅ Complete | 5/5 |
| 6 | Loop Orchestration | ✅ Complete | 7/7 |
| 7 | End-to-End Testing & Polish | ✅ Complete | 6/6 |

---

## Execution Plan

### Phase 0: Agent Format Standardization

**Estimated Session Size:** Small
**Files to Modify:** 3
**Prerequisites:** None
**Entry Point:** Read existing agent files in `/agents/`
**Verification:** All PCH agents that ask questions use the standardized recommendation format

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 0.1 | Standardize pch-visionary recommendation format | `agents/pch-visionary.md` | All questions use `**Recommendation:** [A-E] — [Option Name]` format; remove freeform recommendation patterns |
| 0.2 | Verify pch-planner format compliance | `agents/pch-planner.md` | Confirm all question examples use standardized format (already compliant per verification) |
| 0.3 | Verify pch-researcher format compliance | `agents/pch-researcher.md` | Confirm questions requiring choices use standardized format; open-ended research questions are acceptable (correctly escalate) |

**Phase 0 Deliverables:**
- All PCH agents use consistent `**Recommendation:** [Letter] — [Option Name]` format
- Autonomous answering can reliably parse recommendations
- Non-standard patterns removed or updated

---

### Phase 1: Project Setup & Configuration

**Estimated Session Size:** Small
**Files to Create:** 8
**Prerequisites:** Phase 0 complete; .NET 8 SDK installed, GitHub.Copilot.SDK accessible
**Entry Point:** Create new project from scratch
**Verification:** `dotnet build` succeeds; `dotnet run -- --help` shows usage

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Create .NET 8 console project | `Huge/Huge.csproj` | Project file with .NET 8 target, nullable enabled |
| 1.2 | Add NuGet package references | `Huge/Huge.csproj` | References: GitHub.Copilot.SDK, Serilog, Serilog.Sinks.Console, Serilog.Sinks.File, Microsoft.Extensions.Configuration.*, System.CommandLine |
| 1.3 | Create folder structure | `Huge/` folders | Create: /Orchestration, /Agents, /Detection, /Handlers, /Documents, /Logging |
| 1.4 | Create appsettings.json | `Huge/appsettings.json` | Configuration structure per Technical Design; copy to output enabled |
| 1.5 | Create Program.cs with CLI parsing | `Huge/Program.cs` | Parse commands: `<feature>`, `--resume [runId]`, `--status`, `--verbose`, `--timeout` |
| 1.6 | Configure Serilog in Program.cs | `Huge/Program.cs` | Console + rolling file sinks; log at startup "Huge starting..." |
| 1.7 | Copy project logo image | `Huge/assets/hugh.jpg` | Copy image from `C:\Users\vincent.miceli\OneDrive - pointchealth.com\Pictures\d9aec884805e216b126e828ddd67a5aa.jpg` to project assets folder |
| 1.8 | Create README.md with project branding | `Huge/README.md` | Include project name, Hugh image reference `![Hugh](assets/hugh.jpg)`, description, usage examples, name origin |

**Phase 1 Deliverables:**
- Runnable project skeleton
- CLI argument parsing working
- Logging infrastructure in place
- Configuration loading from appsettings.json
- Project README with Hugh branding

---

### Phase 2: Core Infrastructure (SDK, Logging, State)

**Estimated Session Size:** Medium
**Files to Create/Modify:** 8
**Prerequisites:** Phase 1 complete
**Entry Point:** Read `Huge/Program.cs`
**Verification:** Unit tests pass for state management; SDK client connects

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Create data models | `Huge/Models/LoopState.cs`, `Huge/Models/PhaseResult.cs`, `Huge/Models/Decision.cs` | Record types matching Technical Design Data Models section |
| 2.2 | Create AgentDefinition model | `Huge/Agents/AgentDefinition.cs` | Enum `LoopPhase`, record `AgentDefinition` with Name, PromptPath, Phase, Timeout |
| 2.3 | Create ICopilotService interface | `Huge/Agents/ICopilotService.cs` | Interface with `ExecutePhaseAsync()` signature per design |
| 2.4 | Implement CopilotService (SDK wrapper) | `Huge/Agents/CopilotService.cs` | Connect to SDK, create sessions, send prompts with attachments |
| 2.5 | Create LoopStateManager | `Huge/Orchestration/LoopStateManager.cs` | Load/save state to `.orchestrator/loop-state.json`; create run, update phase, mark complete/failed |
| 2.6 | Create DocumentManager | `Huge/Documents/DocumentManager.cs` | Read document content, find latest document by type in `/docs/` folders |
| 2.7 | Create DecisionLogger | `Huge/Logging/DecisionLogger.cs` | Append decisions to `.orchestrator/decisions.jsonl` |
| 2.8 | Add Models folder and namespace | `Huge/Models/` | Folder for domain models |

**Phase 2 Deliverables:**
- SDK abstraction layer (ICopilotService/CopilotService)
- State persistence working
- Document reading working
- Decision audit logging working

---

### Phase 3: Phase Execution Engine

**Estimated Session Size:** Medium
**Files to Create/Modify:** 7
**Prerequisites:** Phase 2 complete; CopilotService functional
**Entry Point:** Read `Huge/Agents/CopilotService.cs`
**Verification:** Can execute a single agent phase; document attachment works

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Create PhaseExecutor class | `Huge/Orchestration/PhaseExecutor.cs` | Class with `ExecuteAsync(LoopPhase, string prompt, string[] docs)` |
| 3.2 | Implement agent prompt loading | `Huge/Agents/AgentPromptLoader.cs` | Read agent .md files from configured path; cache in memory |
| 3.3 | Implement session configuration | `Huge/Orchestration/PhaseExecutor.cs` | Build SessionConfig with SystemMessage from agent prompt |
| 3.4 | Implement document attachment | `Huge/Orchestration/PhaseExecutor.cs` | Create FileAttachment for each document path; add to MessageOptions |
| 3.5 | Implement linked token cancellation | `Huge/Orchestration/PhaseExecutor.cs` | Create `CancellationTokenSource.CreateLinkedTokenSource(globalToken)` per phase; set phase-specific timeout from config; pass linked token to all SDK calls |
| 3.6 | Implement result extraction | `Huge/Orchestration/PhaseExecutor.cs` | Extract output document path from agent response; return PhaseResult |
| 3.7 | Create AgentRegistry | `Huge/Agents/AgentRegistry.cs` | Map LoopPhase → AgentDefinition (name, prompt path, default timeout) |

**Phase 3 Deliverables:**
- PhaseExecutor can run any PCH agent
- Agent prompts loaded from /agents/
- Documents attached to sessions
- Timeout handling working

---

### Phase 4: Completion Detection & Signal Classification

**Estimated Session Size:** Medium
**Files to Create/Modify:** 6
**Prerequisites:** Phase 3 complete
**Entry Point:** Read `Huge/Orchestration/PhaseExecutor.cs`
**Verification:** All signal patterns correctly classified in unit tests

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Create SignalType enum | `Huge/Detection/SignalType.cs` | Enum: PhaseComplete, WorkflowComplete, QuestionWithRecommendation, QuestionOpen, Blocker, ModeSelection |
| 4.2 | Create SignalClassifier | `Huge/Detection/SignalClassifier.cs` | Class with `Classify(string agentOutput)` → returns SignalType + extracted data |
| 4.3 | Implement phase completion regex | `Huge/Detection/SignalClassifier.cs` | Regex: `✅\s*\*\*Phase\s+\[?\d+\]?\s+(Complete|Completed)` |
| 4.4 | Implement workflow completion regex | `Huge/Detection/SignalClassifier.cs` | Regex: `(Research Complete\|All Phases Complete\|Plan Complete)` |
| 4.5 | Implement recommendation extraction | `Huge/Detection/SignalClassifier.cs` | Regex: `\*\*Recommendation:\*\*\s+([A-E])\s+[—–-]\s+(.+)` ; extract letter + option name |
| 4.5b | Implement mode selection detection | `Huge/Detection/SignalClassifier.cs` | Regex: `🔄\s*\*\*Execution Mode Selection` ; auto-respond with "1" (Manual) for external orchestration |
| 4.6 | Implement coding phase status parser | `Huge/Detection/PlanPhaseParser.cs` | Parse Progress Summary table from plan; find next incomplete phase |

**Phase 4 Deliverables:**
- Signal classification for all output types
- Completion detection working
- Recommendation extraction working
- Plan phase parsing working

---

### Phase 5: Autonomous Question Handling

**Estimated Session Size:** Small
**Files to Create/Modify:** 5
**Prerequisites:** Phase 4 complete
**Entry Point:** Read `Huge/Detection/SignalClassifier.cs`
**Verification:** Recommendations auto-accepted; open questions pause execution

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Create QuestionHandler class | `Huge/Handlers/QuestionHandler.cs` | Class with `HandleQuestion(SignalClassification)` → returns response or null (pause) |
| 5.2 | Implement recommendation acceptance | `Huge/Handlers/QuestionHandler.cs` | If SignalType is QuestionWithRecommendation → return recommendation letter |
| 5.3 | Implement pause-for-human logic | `Huge/Handlers/QuestionHandler.cs` | If SignalType is QuestionOpen → return null; log Warning |
| 5.4 | Integrate with SDK OnUserInputRequest | `Huge/Agents/CopilotService.cs` | Wire up OnUserInputRequest callback to QuestionHandler |
| 5.5 | Create BlockerHandler | `Huge/Handlers/BlockerHandler.cs` | Handle Blocker signals; log, update state, pause execution |

**Phase 5 Deliverables:**
- Autonomous answering of recommendations
- Graceful pause on open questions
- Blocker handling
- Decision audit logging per answer

---

### Phase 6: Loop Orchestration

**Estimated Session Size:** Medium
**Files to Create/Modify:** 7
**Prerequisites:** Phases 3-5 complete
**Entry Point:** Read `Huge/Orchestration/PhaseExecutor.cs`, `Huge/Orchestration/LoopStateManager.cs`
**Verification:** Full loop runs vision → research → planning → review → coding (mocked or real)

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 6.1 | Create LoopOrchestrator class | `Huge/Orchestration/LoopOrchestrator.cs` | Main orchestration class with `RunAsync(string featureRequest)` |
| 6.2 | Implement phase sequencing | `Huge/Orchestration/LoopOrchestrator.cs` | Execute phases in order: Vision → Research → Planning → Review → Coding |
| 6.3 | Implement document flow | `Huge/Orchestration/LoopOrchestrator.cs` | Track output document paths; pass to subsequent phases |
| 6.4 | Implement checkpoint saves | `Huge/Orchestration/LoopOrchestrator.cs` | Update LoopState before each phase; save to disk |
| 6.5 | Implement coding sub-phase loop | `Huge/Orchestration/LoopOrchestrator.cs` | Read plan, find next incomplete phase, execute, repeat until all complete |
| 6.6 | Implement resume logic | `Huge/Orchestration/LoopOrchestrator.cs` | Load state from file; resume from currentPhase |
| 6.7 | Wire orchestrator to Program.cs | `Huge/Program.cs` | `huge "request"` → LoopOrchestrator.RunAsync(); `--resume` → load state + resume |

**Phase 6 Deliverables:**
- Complete loop orchestration
- Phase sequencing working
- Document flow between phases
- Resume from checkpoint working
- Coding sub-phase iteration working

---

### Phase 7: End-to-End Testing & Polish

**Estimated Session Size:** Medium
**Files to Create/Modify:** 6
**Prerequisites:** Phase 6 complete
**Entry Point:** Run full loop end-to-end
**Verification:** Successful autonomous run through all phases on a sample feature

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 7.1 | Create integration test harness | `Huge.Tests/IntegrationTests.cs` | Test project with xUnit; mock CopilotService for offline testing |
| 7.2 | Test signal classification | `Huge.Tests/SignalClassifierTests.cs` | Unit tests for all SignalType detection patterns |
| 7.3 | Test state persistence | `Huge.Tests/LoopStateManagerTests.cs` | Save, load, update, resume scenarios |
| 7.4 | Add prerequisite checks | `Huge/Program.cs` | Check Copilot CLI installed; check agents exist; friendly error messages |
| 7.5 | Add --status command | `Huge/Program.cs` | Display current run state in human-readable format |
| 7.6 | Polish CLI output | `Huge/Program.cs`, `Huge/Logging/` | Progress indicators; phase summaries; clear status messages |

**Phase 7 Deliverables:**
- Test coverage for critical paths
- Prerequisite validation
- Status command working
- Polished user experience

---

## Implementation Notes for AI Agents

### General Guidelines

1. **Use .NET 8 features**: File-scoped namespaces, records, pattern matching, nullable reference types
2. **Follow single-responsibility**: Each class has one clear purpose
3. **Prefer async/await**: All I/O operations should be async
4. **Use ILogger abstraction**: Inject `ILogger<T>` via constructor
5. **Configuration via IOptions**: Use `IOptions<HugeConfig>` pattern

### SDK Integration Notes

The `GitHub.Copilot.SDK` is in Technical Preview. Key APIs:
- `CopilotClient` — Main client, manages CLI lifecycle
- `CreateSessionAsync(SessionConfig)` — Fresh session per phase
- `SendAsync(MessageOptions)` — Send prompt with attachments
- `OnUserInputRequest` — Callback for question interception
- `SessionIdleEvent` — Completion detection

If SDK APIs change, update only `CopilotService.cs`.

### Regex Patterns Reference

```csharp
// Phase completion
private static readonly Regex PhaseCompleteRegex = 
    new(@"✅\s*\*\*Phase\s+\[?\d+\]?\s+(Complete|Completed)", RegexOptions.Compiled);

// Workflow completion
private static readonly Regex WorkflowCompleteRegex = 
    new(@"(Research Complete|All Phases Complete|Plan Complete)", RegexOptions.Compiled);

// Recommendation extraction
private static readonly Regex RecommendationRegex = 
    new(@"\*\*Recommendation:\*\*\s+([A-E])\s+[—–-]\s+(.+)", RegexOptions.Compiled);

// Plan phase status (find incomplete)
private static readonly Regex PlanPhaseRegex = 
    new(@"\|\s*(\d+)\s*\|[^|]+\|\s*(⏳|🔄)\s*[^|]+\|", RegexOptions.Compiled);
```

### Cancellation Token Strategy

**Approach:** Linked Token Sources

Each phase uses a linked cancellation token that combines:
1. **Global shutdown token** — triggered by Ctrl+C or `--abort` command
2. **Phase-specific timeout** — from `appsettings.json` (e.g., VisionPhase: 10m)

```csharp
// In LoopOrchestrator - create global token
private readonly CancellationTokenSource _globalCts = new();

// In PhaseExecutor - create linked token per phase
public async Task<PhaseResult> ExecuteAsync(LoopPhase phase, ...)
{
    var phaseTimeout = _config.Timeouts[phase];
    using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(_globalCts.Token);
    phaseCts.CancelAfter(phaseTimeout);
    
    try
    {
        return await ExecuteWithTokenAsync(phaseCts.Token);
    }
    catch (OperationCanceledException) when (_globalCts.IsCancellationRequested)
    {
        Log.Warning("Phase {Phase} cancelled by user", phase);
        throw; // Propagate for graceful shutdown
    }
    catch (OperationCanceledException)
    {
        Log.Warning("Phase {Phase} timed out after {Timeout}", phase, phaseTimeout);
        await stateManager.MarkFailedAsync($"Timeout after {phaseTimeout}");
        throw;
    }
}
```

### Error Handling Patterns

```csharp
try
{
    await ExecutePhaseAsync(phase, linkedToken);
}
catch (OperationCanceledException)
{
    // Handled in PhaseExecutor with proper logging
    await stateManager.MarkFailedAsync("Cancelled or timed out");
}
catch (Exception ex)
{
    Log.Error(ex, "Phase {Phase} failed unexpectedly", phase);
    await stateManager.MarkFailedAsync(ex.Message);
    throw;
}
```

### Testing Strategy

- **Unit tests**: SignalClassifier, LoopStateManager, QuestionHandler
- **Integration tests**: Mock CopilotService, test full orchestration flow
- **Manual E2E**: Run against real agents with a simple feature request

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|

> ⚠️ Could not access organizational standards from pch-standards-space. Proceeding without standards context.

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | ~25 new files across Huge project + 3 agent updates |
| New patterns introduced | 2 | SDK integration is new; rest uses standard .NET patterns |
| External dependencies | 4 | GitHub.Copilot.SDK (preview), Copilot CLI, Serilog |
| Migration complexity | 1 | Greenfield project — no migration required |
| Test coverage required | 3 | Unit tests for signal classification, state management; integration tests for orchestration |
| **Overall Complexity** | 13/25 | Medium — Standard .NET project, preview SDK is main risk |

---

## Review Summary

**Review Date:** 2026-02-05
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.3

### Review Metrics
- Issues Found: 3 (Critical: 0, Major: 2, Minor: 1)
- Clarifying Questions Asked: 2
- Sections Updated: Phase 0 added, Task 3.5 updated, Task 4.5b added, Cancellation Token Strategy added, Error Handling Patterns updated, Project renamed to Huge

### Key Improvements Made
1. **Agent Format Standardization (Phase 0)**: Added prerequisite phase to ensure all PCH agents use consistent `**Recommendation:** [Letter] — [Option]` format for reliable autonomous answering
2. **Linked Token Cancellation**: Updated timeout handling to use linked cancellation token sources, enabling both per-phase timeouts and global shutdown control
3. **Project Naming & Branding**: Renamed project to "Huge" (inspired by Star Trek's Hugh); added tasks for logo image and README with branding
4. **ModeSelection Detection (v2.3)**: Added Task 4.5b for detecting pch-coder/pch-researcher mode selection prompts and auto-responding with Manual mode

### Follow-up Review (v2.3)
Verified:
- ✅ pch-planner uses compliant recommendation format (Task 0.2 will confirm)
- ✅ pch-visionary has non-standard patterns that Task 0.1 will fix (lines 605, 728)
- ✅ pch-researcher uses `**Recommendations:**` (plural) for research findings — acceptable
- ✅ SDK APIs match research findings (CreateSessionAsync, OnUserInputRequest, etc.)
- ✅ All phase task counts match Progress Summary
- ✅ Linked token cancellation properly documented

### Remaining Considerations
- SDK is Technical Preview — `CopilotService.cs` abstraction layer isolates changes
- Monitor agent output for non-standard patterns during initial runs
- Consider adding unit tests for cancellation scenarios in Phase 7

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** 2026-02-05
**Reviewed By:** pch-plan-reviewer
**Review Date:** 2026-02-05
**Status:** Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** `/docs/plans/008-self-operating-development-loop.md`
