---
id: "024"
type: research
title: "Hugh Orchestration Gap Analysis — Vision-to-Implementation Pipeline Improvements"
status: ✅ Complete
created: "2026-02-27"
current_phase: "8 of 8"
---

## Introduction

Hugh is a self-operating development loop that autonomously implements software features via the GitHub Copilot SDK. It orchestrates a phased pipeline (Vision → Research → Planning → Review → Coding) supporting both single-feature and multi-release solution modes. This research evaluates how close Hugh's current architecture is to the target vision: a fully autonomous pipeline where a vision is decomposed into releases, each release is independently researched, research feeds into a multi-phase plan, and plan phases are implemented one at a time — with every unit of work (release, research item, plan phase, coding task) executing in its own context window session, and all results tracked in a living progress document.

## Objectives

- Assess the current pipeline against the target vision of fully session-isolated, document-tracked autonomous development
- Identify architectural gaps where Hugh falls short of per-unit-of-work context isolation
- Evaluate the progress tracking and observability story across releases, research, planning, and coding
- Identify inter-release learning gaps that limit quality in multi-release execution
- Evaluate review cycle completeness and plan-quality feedback loops
- Assess git strategy and rollback safety for multi-release execution
- Identify state model inconsistencies and prompt conflicts that cause behavioral issues
- Propose concrete improvements with priority ordering and estimated scope

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Current Architecture Capability Mapping | ✅ Complete | Map every current Hugh capability against the target vision; identify what already works, what partially works, and what is missing entirely | 2026-02-27 |
| 2 | Progress Tracking & Observability Gap | ✅ Complete | Evaluate how progress is currently tracked (JSON state files, emoji tables in docs); identify the absence of a consolidated human-readable progress document; define what a progress reporter should contain | 2026-02-27 |
| 3 | Planning Phase Context Window Risk | ✅ Complete | Analyze why planning runs as a single session while research and coding get sub-phase loops; assess context exhaustion risk for complex releases; evaluate the planning sub-phase loop pattern | 2026-02-27 |
| 4 | Task-Level Coding Granularity Gap | ✅ Complete | Analyze coding loop granularity (phase-level vs task-level); assess context exhaustion risk when a plan phase contains many tasks; evaluate task-level session isolation | 2026-02-27 |
| 5 | Inter-Release Learning & Context Propagation | ✅ Complete | Analyze what context flows between releases in multi-release mode; identify gaps where Release N+1 lacks knowledge of Release N's actual output; evaluate enrichment strategies | 2026-02-27 |
| 6 | Review Cycle Completeness | ✅ Complete | Analyze the review phase execution model (single-shot vs iterative); assess whether review findings are acted upon; evaluate review-revise loop patterns | 2026-02-27 |
| 7 | Git Strategy & Rollback Safety | ✅ Complete | Evaluate the absence of branch management; assess rollback risk when coding fails mid-release; evaluate branch-per-release strategies | 2026-02-27 |
| 8 | State Model & Prompt Consistency Issues | ✅ Complete | Catalog state model issues (shared sub-phase counter, hacky phase skipping); catalog prompt conflicts (autonomous vs interactive, vestigial "Automatic Mode"); evaluate fixes | 2026-02-27 |

## Phase 1: Current Architecture Capability Mapping

**Status:** ✅ Complete
**Session:** 2026-02-27

### Target Vision vs. Current State

| Target Capability | Current State | Gap Severity |
|---|---|---|
| Vision capture from user | ✅ Done — Interactive Q&A via pch-visionary, produces vision.md | None |
| Vision → releases decomposition | ✅ Done — SolutionOrchestrator: Triage → Clarification → Decomposition → roadmap.md | None |
| Per-release execution isolation | ✅ Done — Each release gets own LoopOrchestrator with isolated state in `.orchestrator/releases/release-N/` | None |
| Research topics in separate sessions | ✅ Done — `ExecuteResearchPhasesAsync` creates fresh session per research topic (fixed in plan 026) | None |
| Planning in separate sessions | ❌ Missing — Planning runs as single `ExecuteSinglePhaseAsync` call | High |
| Plan phases implemented one at a time | ✅ Done — `ExecuteCodingPhasesAsync` creates fresh session per plan phase | None |
| Individual tasks in separate sessions | ❌ Missing — All tasks within a plan phase execute in one session | Medium |
| Consolidated progress document | ❌ Missing — State tracked in JSON + emoji tables but no unified human-readable report | High |
| Inter-release learning | ⚠️ Partial — `ReleaseContext` passes release names but not actual outputs/artifacts | Medium |
| Iterative review cycle | ❌ Missing — Review runs once, no revision loop | Medium |
| Git branch strategy | ❌ Missing — No branch creation, no rollback capability | Medium |

### Architectural Strengths

1. **Document-driven continuity** — Each session re-reads documents from disk, enabling clean context isolation without shared memory
2. **Checkpoint-based recovery** — JSON state persisted after every transition; full resume from any failure point
3. **Dual-detection signal architecture** — Tool-based signals (primary) + text regex (fallback) ensures both Hugh CLI and VS Code Copilot compatibility
4. **Sub-phase loop pattern** — Proven in both research (`ExecuteResearchPhasesAsync`) and coding (`ExecuteCodingPhasesAsync`); reusable for planning
5. **Error classification with targeted retry** — 6 error categories enable intelligent retry (transient, rate-limited) vs fail-fast (auth, context exhaustion)
6. **Build/test verification with auto-repair** — Per-sub-phase build checks + post-coding verification with repair loops catch regressions early

### Key Architectural Pattern: The Sub-Phase Loop

The sub-phase loop pattern in `LoopOrchestrator` is the core mechanism that enables per-unit-of-work session isolation:

```
Session 0: Create outline/skeleton document with progress table
Loop:
  1. Re-read document from disk
  2. Parse progress table for next incomplete item (via PhaseParser)
  3. Create fresh SDK session with continuation prompt
  4. Execute single item
  5. Agent updates document on disk (marks item ✅)
  6. Update state checkpoint
  7. Repeat until all items complete
Post-loop: Validate all items marked complete
```

This pattern already exists for Research and Coding. The gap is that **Planning does not use it**.

**Key Discoveries:**
- Hugh delivers ~70% of the target vision — core pipeline, session isolation for research/coding, state persistence, resume capability
- The 30% gap concentrates in: planning granularity, task-level coding, progress visibility, inter-release learning, review iteration, and git safety
- The sub-phase loop pattern is the architectural foundation — extending it to planning and task-level coding closes the two largest granularity gaps
- Document-driven continuity is the right architecture for the target vision; no fundamental redesign needed

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | Core orchestration: sub-phase loops for research and coding, single-shot for planning |
| `Hugh/Orchestration/SolutionOrchestrator.cs` | Multi-release: triage, clarification, decomposition, per-release execution |
| `Hugh/Orchestration/LoopStateManager.cs` | Checkpoint persistence, resume logic |
| `Hugh/Detection/PlanPhaseParser.cs` | Coding sub-phase loop parser (template for planning) |
| `Hugh/Detection/ResearchPhaseParser.cs` | Research sub-phase loop parser |

**Gaps:** None
**Assumptions:** The sub-phase loop pattern scales to planning without architectural changes to the session or SDK layer.

---

## Phase 2: Progress Tracking & Observability Gap

**Status:** ✅ Complete
**Session:** 2026-02-27

### How Progress Is Currently Tracked

Hugh tracks progress through three disconnected mechanisms:

#### 1. JSON State Files (Machine-Readable)

```
.orchestrator/
├── solution-state.json          — SolutionState: mode, phase, releases[], currentReleaseIndex
└── releases/
    ├── release-1/loop-state.json — LoopState: currentPhase, completedPhases, documents, decisions
    └── release-N/loop-state.json
```

**Strengths:** Complete state for resume; serialized after every transition.
**Weakness:** Not human-readable. Requires JSON parsing to understand progress.

#### 2. Emoji Status Tables in Documents (Per-Document)

Research documents contain:
```markdown
| Phase | Name | Status | Scope | Session |
| 1 | API Design | ✅ Complete | ... | 2026-02-27 |
| 2 | Data Model | 🔄 In Progress | ... | — |
| 3 | Auth Strategy | ⏳ Not Started | ... | — |
```

Plan documents contain similar "Progress Summary" tables.

**Strengths:** Human-readable within each document; parseable by `ResearchPhaseParser`/`PlanPhaseParser`.
**Weakness:** Scattered across multiple files. No cross-document or cross-release aggregation.

#### 3. Console Output (Ephemeral)

`ConsoleOutput.WriteSection()`, `WriteSuccess()`, `WriteInfo()` provide real-time terminal output. Serilog logs to `~/.hugh/logs/`.

**Strengths:** Live feedback during execution.
**Weakness:** Ephemeral — lost when terminal closes. Log files are verbose diagnostic output, not progress summaries.

### What's Missing: The Consolidated Progress Document

There is **no single document** that answers: "What is the overall status of this solution?" A user returning to a long-running multi-release execution has to:

1. Read `solution-state.json` to find which release is active
2. Read each `release-N/loop-state.json` to find phase status
3. Open each research/plan document to check sub-phase completion
4. Manually aggregate this into a mental model

### Proposed Progress Reporter Design

A `ProgressReporter` component should generate and update a markdown progress document after every state transition:

**Output location:** `docs/progress/{solutionId}-progress.md` (or `docs/progress/progress.md` for single-feature)

**Content structure:**
```markdown
# Solution Progress: [Project Name]
**Started:** 2026-02-27 10:30 UTC | **Status:** 🔄 In Progress | **Duration:** 2h 15m

## Release Overview
| Release | Name | Status | Research | Plan | Review | Coding | Verification |
|---------|------|--------|----------|------|--------|--------|--------------|
| 1 | MVP | ✅ Complete | 5/5 ✅ | 8 phases ✅ | Approved ✅ | 8/8 ✅ | Build ✅ Tests ✅ |
| 2 | Advanced | 🔄 In Progress | 3/5 🔄 | Pending | Pending | Pending | Pending |
| 3 | Polish | ⏳ Pending | — | — | — | — | — |

## Release 2: Advanced Features — 🔄 In Progress

### Research (3/5 complete)
| # | Topic | Status | Duration |
|---|-------|--------|----------|
| 1 | API design patterns | ✅ Complete | 4m 12s |
| 2 | Authentication strategies | ✅ Complete | 3m 45s |
| 3 | Performance benchmarking | ✅ Complete | 5m 01s |
| 4 | Data migration approach | 🔄 In Progress | — |
| 5 | Integration testing strategy | ⏳ Not Started | — |

### Decisions Made
| Phase | Question | Decision | Rationale | Auto? |
|-------|----------|----------|-----------|-------|
| Research | Auth mechanism | JWT | Stateless, scalable | Yes |
| Planning | Database | PostgreSQL | Team familiarity | Yes |
```

### Integration Points

The `ProgressReporter` should hook into existing state transition methods:
- `LoopStateManager.UpdatePhaseAsync()` → update phase status row
- `LoopStateManager.MarkPhaseCompleteAsync()` → mark phase complete, record duration
- `SolutionStateManager.UpdateReleaseStatusAsync()` → update release overview table
- `SolutionStateManager.MarkCompleteAsync()` → mark solution complete

It should also parse documents for sub-phase detail:
- Read research document → `ResearchPhaseParser.ParsePhases()` → populate research sub-phase table
- Read plan document → `PlanPhaseParser.ParsePhases()` → populate plan phase table

**Key Discoveries:**
- Progress is tracked in three disconnected mechanisms (JSON, emoji tables, console) with no aggregation
- No single document answers "what is the overall status?" for a multi-release solution
- A `ProgressReporter` component can hook into existing state transition methods to generate a living progress document
- Sub-phase detail can be sourced from existing parsers (`ResearchPhaseParser`, `PlanPhaseParser`)
- The progress document should be markdown (human-readable, versionable, displayable in terminals)

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopStateManager.cs` | State transitions to hook into for progress updates |
| `Hugh/Orchestration/SolutionStateManager.cs` | Release-level state transitions |
| `Hugh/Detection/ResearchPhaseParser.cs` | Parse research sub-phase status for progress detail |
| `Hugh/Detection/PlanPhaseParser.cs` | Parse plan phase status for progress detail |
| `Hugh/Models/LoopState.cs` | Phase status, documents, decisions data |
| `Hugh/Models/SolutionState.cs` | Release definitions, release status data |

**Gaps:** None
**Assumptions:** Markdown format is sufficient for progress visibility (vs. a web dashboard or structured API).

---

## Phase 3: Planning Phase Context Window Risk

**Status:** ✅ Complete
**Session:** 2026-02-27

### The Problem

Planning runs as a single `ExecuteSinglePhaseAsync` call in `LoopOrchestrator.DispatchPhaseAsync` (line 373):

```csharp
return await ExecuteSinglePhaseAsync(phase, state, ct);
```

Unlike Research (`ExecuteResearchPhasesAsync`) and Coding (`ExecuteCodingPhasesAsync`), Planning has **no sub-phase loop**. The entire plan — potentially 8-15 phases with detailed tasks, acceptance criteria, and implementation notes — must be produced in a single context window.

### Context Window Risk Analysis

| Plan Complexity | Estimated Tokens | Risk |
|---|---|---|
| Simple (3-4 phases, 10 tasks) | ~15K output | Low — fits comfortably |
| Medium (6-8 phases, 25 tasks) | ~35K output | Medium — may trigger compaction |
| Complex (10-15 phases, 40+ tasks) | ~60K+ output | High — context exhaustion likely |

The risk is amplified because Planning also needs to:
- Read the vision document (attached)
- Read the research document (attached, potentially large after all sub-phases complete)
- Read the roadmap (attached, in multi-release mode)
- Generate the full plan with per-task detail

With all inputs attached, a complex release may consume 40-50% of context before the agent begins generating output.

### The Proven Pattern: Sub-Phase Planning Loop

Mirror the research and coding patterns:

**Session 0 — Plan Skeleton:**
- Agent receives vision + research + roadmap documents
- Creates plan document with: overview, phase table, phase headers (empty bodies)
- Returns plan document path

**Sessions 1..N — Per-Phase Detail:**
- Orchestrator re-reads plan from disk
- `PlanCreationPhaseParser` finds next phase with empty body
- Fresh session with continuation prompt: "Fill in Phase N: [Name]"
- Agent reads existing plan for context, generates detailed tasks for Phase N only
- Updates plan document on disk

**Post-Loop:**
- Validate all phases have content
- Proceed to Review

### Compatibility with pch-planner Agent

The `pch-planner` agent prompt already supports this pattern. Its instructions say:
- "Fill execution plans ONE PHASE AT A TIME (never all phases at once)"
- "Create plan document skeleton FIRST (section headers before content)"

These instructions were designed for the VS Code multi-session workflow. Hugh's single-shot execution was the component that violated this design.

### New Parser: `PlanCreationPhaseParser`

Distinct from `PlanPhaseParser` (which tracks coding progress via ✅/⏳ in the Progress Summary table), `PlanCreationPhaseParser` detects plan phases that lack implementation detail:

```
Phase has skeleton header but no task list → Incomplete
Phase has task list with acceptance criteria → Complete
```

### Prompt Design for Planning Continuation

```
You are running inside the Hugh CLI — a self-operating development loop.
Continue building the implementation plan at `{planPath}`.
Fill in Phase {N}: {Name}.

The plan document, vision, and research documents are attached for context.
Generate detailed tasks with acceptance criteria for ONLY Phase {N}.
Do not fill in subsequent phases.
Make reasonable implementation decisions based on the research findings.
Document your rationale in the plan's Planning Session Log.
```

**Key Discoveries:**
- Planning as single-shot risks context exhaustion for complex releases (10+ phases)
- The sub-phase loop pattern used by research and coding is directly applicable to planning
- `pch-planner` already supports this pattern ("fill execution plans ONE PHASE AT A TIME")
- A new `PlanCreationPhaseParser` is needed to distinguish skeleton headers from completed phase sections
- This is distinct from `PlanPhaseParser` which tracks coding progress
- Context window relief is significant: each planning session only needs to generate 1 phase instead of 10+

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | `DispatchPhaseAsync` routes planning as single-shot; needs `ExecutePlanningPhasesAsync` |
| `Hugh/Detection/PlanPhaseParser.cs` | Template for `PlanCreationPhaseParser`; different detection logic |
| `agents/pch-planner.md` | Agent prompt already supports per-phase filling pattern |

**Gaps:** None
**Assumptions:** LLM can reliably fill in one plan phase at a time when given the full plan skeleton as context. The plan skeleton provides sufficient structural framing.

---

## Phase 4: Task-Level Coding Granularity Gap

**Status:** ✅ Complete
**Session:** 2026-02-27

### The Problem

`ExecuteCodingPhasesAsync` iterates at the **phase** level. Each plan phase may contain 3-15 individual tasks. All tasks in a phase execute in a single context window session:

```csharp
var prompt = BuildCodingPhasePrompt(nextPhase);  // "Execute ONLY Phase N"
var result = await _phaseExecutor.ExecuteAsync(LoopPhase.Coding, prompt, new[] { planPath });
```

### Context Exhaustion Risk by Phase Size

| Tasks per Phase | Estimated Token Usage | Risk |
|---|---|---|
| 1-3 tasks | ~10-20K | Low |
| 4-7 tasks | ~25-40K | Medium — large file edits may trigger compaction |
| 8-15 tasks | ~50-80K+ | High — likely context exhaustion or quality degradation |

When a coding session runs long, quality degrades before context exhaustion:
- Later tasks get less attention (recency bias in attention)
- The agent may skip acceptance criteria verification for later tasks
- Error recovery for late-session failures is poor (no clean rollback point)

### Task-Level Session Isolation Design

Add an inner loop within `ExecuteCodingPhasesAsync`:

```
For each plan phase:
  Parse tasks within the phase
  For each task:
    Fresh session: "Implement Task N.M: [Description]"
    Agent reads plan, implements single task
    Agent updates task status in plan document (⏳ → ✅)
    Per-task build verification (optional)
```

### TaskParser Design

Extend `PlanPhaseParser` with task-level parsing:

Plan documents structure tasks within phases:
```markdown
## Phase 3: Authentication

### Task 3.1: Create auth middleware
- **Status:** ⏳ Not Started
- **Files:** src/middleware/auth.cs
- **Acceptance:** Middleware validates JWT tokens...

### Task 3.2: Add login endpoint
- **Status:** ⏳ Not Started
- **Files:** src/controllers/auth.cs
- **Acceptance:** POST /api/auth/login accepts...
```

`TaskParser.FindNextIncompleteTask(phaseNumber)` returns the next task with ⏳ status.

### Configuration

This should be opt-in to avoid breaking existing behavior:

```json
{
  "Hugh": {
    "Coding": {
      "Granularity": "phase",     // "phase" (default) or "task"
      "VerifyBuildPerTask": false  // Per-task build verification
    }
  }
}
```

### Trade-offs

| Aspect | Phase-Level (Current) | Task-Level (Proposed) |
|---|---|---|
| Context usage per session | Higher (all tasks in phase) | Lower (single task) |
| Session overhead | Lower (fewer sessions) | Higher (more sessions, more startup cost) |
| Failure granularity | Phase fails → retry all tasks | Task fails → retry single task |
| Progress visibility | Phase-level | Task-level |
| Plan document complexity | Simpler (phase status only) | More complex (per-task status) |
| Quality on large phases | Degrades | Consistent |

**Recommendation:** Default to phase-level (backward compatible), allow task-level via config for complex releases.

**Key Discoveries:**
- Coding operates at phase granularity; tasks within a phase all execute in one session
- Large phases (8+ tasks) risk context exhaustion and quality degradation
- Task-level session isolation follows the same sub-phase loop pattern
- Requires `TaskParser` extension to `PlanPhaseParser` for per-task status tracking
- Should be opt-in via `Coding.Granularity` config to preserve backward compatibility
- Per-task build verification adds safety but increases execution time significantly

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | `ExecuteCodingPhasesAsync` — needs inner task loop |
| `Hugh/Detection/PlanPhaseParser.cs` | Needs `FindNextIncompleteTask(phaseNumber)` method |
| `Hugh/Configuration/HughConfig.cs` | New `Coding.Granularity` config option |
| `agents/pch-coder.md` | Agent prompt says "execute EXACTLY ONE PHASE per subagent" — needs task-level variant |

**Gaps:** None
**Assumptions:** Plan documents will reliably format tasks with status markers that a `TaskParser` can detect. Agent prompt modifications for task-level execution are straightforward.

---

## Phase 5: Inter-Release Learning & Context Propagation

**Status:** ✅ Complete
**Session:** 2026-02-27

### The Problem

When Release 2 starts, its `ReleaseContext` carries:

```csharp
public record ReleaseContext
{
    public int ReleaseNumber { get; init; }
    public string ReleaseName { get; init; }
    public string ReleaseScope { get; init; }
    public string SolutionVisionPath { get; init; }
    public string RoadmapPath { get; init; }
    public List<string> CompletedReleases { get; init; }  // Just names!
}
```

`CompletedReleases` is `List<string>` containing only names like `"Release 1: MVP"`. Release 2 does **not** receive:
- What files Release 1 created or modified
- What APIs, interfaces, or data models Release 1 introduced
- What architectural patterns or conventions Release 1 established
- The actual research findings from Release 1
- The implementation plan from Release 1

### Impact on Quality

Without inter-release context, Release 2's research agent:
- May investigate patterns that Release 1 already established
- May propose architectural approaches that conflict with Release 1's implementation
- Cannot reference specific interfaces or APIs that Release 1 created for integration

Release 2's planner:
- Cannot create tasks that reference specific files from Release 1
- May duplicate work that Release 1 already completed
- Cannot plan integration tests against Release 1's actual implementation

### Proposed: Release Summary Document

After each release completes, generate a "release summary" capturing what was actually built:

```markdown
# Release 1 Summary: MVP

## Files Changed
| File | Action | Description |
|------|--------|-------------|
| src/auth/AuthService.cs | Created | JWT authentication service |
| src/models/User.cs | Created | User domain model |
| src/api/AuthController.cs | Created | Login/register endpoints |

## Public APIs Introduced
- POST /api/auth/login — Returns JWT token
- POST /api/auth/register — Creates user account
- GET /api/auth/me — Returns current user

## Architectural Patterns
- Repository pattern for data access
- JWT bearer token authentication
- Middleware-based authorization

## Key Decisions
| Decision | Choice | Rationale |
|----------|--------|-----------|
| Auth mechanism | JWT | Stateless, scalable |
| Password hashing | BCrypt | Industry standard |
```

### Implementation Approach

1. After release coding completes and verification passes, run `git diff` against the pre-release state to enumerate changed files
2. Generate summary from: git diff output + plan document decisions + research findings
3. Store at `docs/releases/release-{N}-summary.md`
4. Add `CompletedReleaseSummaries: List<string>` (file paths) to `ReleaseContext`
5. Attach summaries to Research and Planning sessions for subsequent releases

### Scope Consideration

This could be a simple git-diff-based file listing (low effort) or an LLM-generated architectural summary (medium effort). **Recommend starting with git-diff listing** and adding LLM summarization later if needed.

**Key Discoveries:**
- `ReleaseContext.CompletedReleases` carries only release names — no substance about what was built
- Subsequent releases cannot reference APIs, files, or patterns established by prior releases
- A "release summary" document generated after each release bridges this gap
- Git diff provides the file-level detail; plan decisions provide the rationale
- Should be attached to Research and Planning sessions for subsequent releases
- Start with git-diff-based listing; add LLM summarization as enhancement

| File | Relevance |
|------|-----------|
| `Hugh/Models/ReleaseContext.cs` | Needs `CompletedReleaseSummaries` field |
| `Hugh/Orchestration/SolutionOrchestrator.cs` | `ExecuteReleasesAsync` — needs post-release summary generation |
| `Hugh/Orchestration/LoopOrchestrator.cs` | `GetDocumentsForPhase` — needs to attach release summaries |

**Gaps:** None
**Assumptions:** `git diff` is available in the execution environment and provides sufficient detail for inter-release context.

---

## Phase 6: Review Cycle Completeness

**Status:** ✅ Complete
**Session:** 2026-02-27

### The Problem

The review phase runs exactly once via `ExecuteSinglePhaseAsync`. The reviewer (`pch-plan-reviewer`) produces findings, but there is **no mechanism** for the planner to revise the plan based on those findings and have the reviewer re-validate.

Current flow:
```
Review → (findings produced) → Plan marked "Ready for Implementation" or findings logged → Coding
```

Target flow:
```
Review → (findings produced) → Planner revises → Re-review → (no findings) → Coding
```

### How Review Currently Works

In `DispatchPhaseAsync`, Review is routed to `ExecuteSinglePhaseAsync` (line 373). The reviewer reads the plan, produces findings, and either:
- Marks the plan "Ready for Implementation" (happy path)
- Identifies issues and documents them in a "Review Session Log"

But there's **no feedback loop**. If the reviewer identifies issues:
1. The issues are logged in the plan document's Review Session Log
2. But nobody acts on them — the next step is Coding
3. The coder implements the plan as-is, including any issues the reviewer flagged

### Proposed: Review-Revise Cycle

Add `ExecuteReviewCycleAsync` to `LoopOrchestrator`:

```
Iteration 1:
  Reviewer reads plan → produces ReviewResult
  If ReviewResult.HasIssues:
    Planner revision session: "Fix these review findings: [issues]"
    Planner updates plan document
    Iteration 2:
      Reviewer re-reads plan → produces ReviewResult
      If ReviewResult.HasIssues AND attempts < MaxReviewCycles:
        Repeat
      Else:
        Proceed to Coding
  Else:
    Proceed to Coding
```

### Review Signal Detection

The reviewer needs to emit a structured signal indicating whether issues were found:
- `hugh_notify_phase_complete` with summary indicating approval or issues
- Text-based: "Ready for Implementation" (approved) vs "Issues Found" (needs revision)

Currently `SignalClassifier` can detect `PhaseComplete` and `WorkflowComplete` but has no concept of "approved with issues" vs "approved clean."

### Proposed: ReviewResult Extension

Add a `ReviewOutcome` to the phase result:
```csharp
public enum ReviewOutcome { Approved, ApprovedWithFindings, Rejected }
```

The review session's signal (or text parsing) determines the outcome. `ApprovedWithFindings` triggers a revision cycle; `Rejected` triggers revision; `Approved` proceeds directly.

### Configuration

```json
{
  "Hugh": {
    "Review": {
      "MaxReviewCycles": 2,
      "RequireCleanApproval": false
    }
  }
}
```

- `MaxReviewCycles: 2` — maximum revision iterations before forcing proceed
- `RequireCleanApproval: false` — when false, proceed after max cycles even with findings

**Key Discoveries:**
- Review runs exactly once with no revision feedback loop
- Reviewer findings are logged in the plan but never acted upon before coding begins
- A review-revise cycle (reviewer → planner revision → re-review) would catch plan quality issues
- Requires new `ReviewOutcome` concept in signal detection
- Should be bounded by `MaxReviewCycles` to prevent infinite revision loops
- `RequireCleanApproval` config controls whether to proceed after max cycles with outstanding findings

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/LoopOrchestrator.cs` | Needs `ExecuteReviewCycleAsync` replacing single-shot Review dispatch |
| `Hugh/Detection/SignalClassifier.cs` | May need review-specific signal patterns |
| `Hugh/Models/PhaseResult.cs` | Needs `ReviewOutcome` field |
| `agents/pch-plan-reviewer.md` | Already marks plans "Ready for Implementation" — needs structured outcome signal |
| `Hugh/Configuration/HughConfig.cs` | New `Review.MaxReviewCycles` config |

**Gaps:** None
**Assumptions:** The reviewer agent can reliably signal whether findings require plan revision vs. are informational only.

---

## Phase 7: Git Strategy & Rollback Safety

**Status:** ✅ Complete
**Session:** 2026-02-27

### The Problem

Hugh performs **no git branch management**. All coding happens on whatever branch the user is currently on. There is no:
- Branch-per-release isolation
- Rollback capability when coding fails mid-release
- PR creation for completed releases
- Protection against partial implementation corrupting the working branch

### Failure Scenario

```
Release 1: Coding Phase 5/8 fails
→ Phases 1-4 already wrote files to the working branch
→ No way to undo partial Release 1 changes
→ Working branch is in an inconsistent state
→ Manual git cleanup required
```

### Proposed: Branch Management Strategy

#### Per-Release Branches

```
main
  └── solution/{solutionId}  (base branch)
       ├── release/1-mvp
       ├── release/2-advanced
       └── release/3-polish
```

1. **Solution start:** Create `solution/{solutionId}` from current HEAD
2. **Release start:** Create `release/{N}-{name}` from solution branch
3. **Release coding:** All coding happens on the release branch
4. **Release success:** Merge release branch into solution branch
5. **Release failure:** Leave branch unmerged — clean rollback by deleting the branch
6. **Solution complete:** Optionally create PR from solution branch to main

#### Per-Coding-Phase Commits

Within a release branch, each coding sub-phase creates a commit:
```
release/1-mvp
  ├── commit: "Phase 1: Project setup and configuration"
  ├── commit: "Phase 2: Data models and migrations"
  ├── commit: "Phase 3: API endpoints"
  └── commit: "Phase 4: Authentication" (failed — easy to revert)
```

This enables:
- Per-phase rollback via `git revert` if a phase's code causes issues
- Clear commit history showing what each phase implemented
- Build verification per commit

### Implementation: `GitBranchManager`

```csharp
public class GitBranchManager
{
    Task<string> CreateSolutionBranchAsync(Guid solutionId);
    Task<string> CreateReleaseBranchAsync(int releaseNumber, string name);
    Task MergeReleaseBranchAsync(int releaseNumber);
    Task<string> GetCurrentBranchAsync();
    Task CommitPhaseAsync(int phaseNumber, string phaseName, string summary);
    Task<bool> HasUncommittedChangesAsync();
    Task RollbackReleaseBranchAsync(int releaseNumber);
}
```

### Integration Points

- `SolutionOrchestrator.ExecuteSolutionAsync()` → create solution branch before clarification
- `SolutionOrchestrator.ExecuteReleasesAsync()` → create release branch before each release, merge after success
- `LoopOrchestrator.ExecuteCodingPhasesAsync()` → commit after each successful coding sub-phase
- `LoopOrchestrator.RunPostCodingVerificationAsync()` → commit verification success

### Configuration

```json
{
  "Hugh": {
    "Git": {
      "BranchingEnabled": true,
      "CommitPerPhase": true,
      "CreatePullRequest": false,
      "BaseBranch": "main"
    }
  }
}
```

### Single-Feature Mode

For single-feature (non-multi-release) runs:
- Create `feature/{featureName}` branch from current HEAD
- Commit per coding sub-phase
- Optionally create PR on completion

**Key Discoveries:**
- Hugh performs no git branch management; all work happens on the current branch
- Partial release failures leave the working branch in an inconsistent state with no rollback
- Per-release branches provide isolation and clean rollback
- Per-phase commits within release branches provide granular history and revert capability
- `GitBranchManager` should integrate at SolutionOrchestrator (branches) and LoopOrchestrator (commits) levels
- Should be opt-in via `Git.BranchingEnabled` config to avoid changing behavior for users who manage branches manually

| File | Relevance |
|------|-----------|
| `Hugh/Orchestration/SolutionOrchestrator.cs` | Branch creation/merge at release boundaries |
| `Hugh/Orchestration/LoopOrchestrator.cs` | Per-phase commits in `ExecuteCodingPhasesAsync` |
| `Hugh/Configuration/HughConfig.cs` | New `Git` config section |

**Gaps:** None
**Assumptions:** Git is available in the execution environment. Users are comfortable with Hugh creating branches and commits (opt-in config).

---

## Phase 8: State Model & Prompt Consistency Issues

**Status:** ✅ Complete
**Session:** 2026-02-27

### Issue 1: Shared Sub-Phase Counter

`LoopState.CurrentCodingSubPhase` (type `int?`) is used for **both** Research and Coding sub-phases despite its name implying coding-only:

```csharp
// Used during Research sub-phase loop:
await _stateManager.UpdatePhaseAsync(LoopPhase.Research, PhaseStatus.InProgress, nextPhase.Number, ct);

// Used during Coding sub-phase loop:
await _stateManager.UpdatePhaseAsync(LoopPhase.Coding, PhaseStatus.InProgress, nextPhase.Number, ct);
```

**Risk:** If resume occurs, `CurrentCodingSubPhase = 3` is ambiguous — is it research sub-phase 3 or coding sub-phase 3? The `CurrentPhase` field disambiguates in practice, but the model is misleading and fragile.

**Fix:** Split into `CurrentResearchSubPhase` and `CurrentCodingSubPhase`. Add state migration for backward compatibility (deserialize old `currentCodingSubPhase` to whichever field matches `currentPhase`).

### Issue 2: Hacky Phase Skipping in RunForContextAsync

Lines 136-146 of `LoopOrchestrator.cs`:

```csharp
// Hack: we need to update the completed phases and documents in state manager
// by marking the skipped phases complete
if (startPhase != LoopPhase.Vision && releaseContext != null)
{
    foreach (var skippedPhase in state.CompletedPhases)
    {
        var docPath = skippedPhase == LoopPhase.Vision ? releaseContext.SolutionVisionPath : null;
        await _stateManager.MarkPhaseCompleteAsync(docPath, ct);
        await _stateManager.UpdatePhaseAsync(startPhase, PhaseStatus.NotStarted, null, ct);
    }
}
```

This iteratively calls `MarkPhaseCompleteAsync` (which advances the internal phase pointer) to "fast-forward" the state manager through skipped phases. It's brittle — if `MarkPhaseCompleteAsync` side effects change, this breaks.

**Fix:** Add `LoopStateManager.CreateRunFromPhaseAsync(featureRequest, startPhase, prePopulatedDocs)` that directly creates state at the desired phase without iterating through predecessors.

### Issue 3: Planning Prompt Conflicts

The `pch-planner` agent prompt (`agents/pch-planner.md`) is designed for interactive multi-turn Q&A:
- "Asks ONE clarifying question at a time"
- "NEVER answer own questions (even if recommendation seems obvious)"
- "STOP and yield to user after question"

But Hugh's planning prompt says:
- "Do not ask clarifying questions — decide autonomously based on available context"

These instructions **directly conflict**. The agent's system prompt says "always ask questions"; Hugh's user prompt says "never ask questions." LLM behavior under this conflict is unpredictable.

**Fix options:**
1. **Autonomous planning prompt override** — Add a built-in prompt (`Hugh/Prompts/BuiltIn/planning-autonomous.md`) that replaces the interactive Q&A instructions with autonomous decision-making instructions. Use this when `_config.Autonomous.Enabled`.
2. **Prompt mode flag** — Add a section to `pch-planner.md` that says "When running inside Hugh CLI, make decisions autonomously and log rationale in Session Log instead of asking questions." This is simpler but risks VS Code users seeing the autonomous behavior.

**Recommendation:** Option 1 (built-in override) — cleanest separation of concerns.

### Issue 4: Vestigial "Automatic Mode" in Prompts

Both Research and Planning prompts contained "Please select Automatic Mode" text (Research was fixed in plan 026; Planning still has it in the release-aware prompt at line 999). This text is meaningless to both agents.

**Fix:** Remove all "Automatic Mode" references from `BuildPromptForPhase` and `BuildReleaseAwarePrompt`. Replace with explicit Hugh CLI context instructions.

### Issue 5: BuildReleaseOrchestrator Uses NullLogger

`SolutionOrchestrator.BuildReleaseOrchestrator` (line 601-632) creates per-release orchestrators with `NullLoggerFactory`. This means per-release execution produces **no diagnostic logs** — a significant observability gap.

```csharp
var nullLoggerFactory = NullLoggerFactory.Instance;
```

**Fix:** Pass the actual logger factory through to per-release orchestrators. The logger should include release number context (e.g., `LogContext.PushProperty("ReleaseNumber", releaseNumber)`).

### Issue 6: Document Output Detection Fragility

`PhaseExecutor.ExtractOutputDocumentPath` uses a 4-level fallback:
1. SDK tool result → path from `hugh_notify_phase_complete`
2. Text regex extraction → `DocumentPathExtractor`
3. Expected path check → `{runId}-{phase}.md`
4. Filesystem scan → recently created `.md` files (30-minute window)

Level 3 frequently fails because agents name documents differently than the expected pattern. Level 4 is brittle due to the time window.

**Fix:** Standardize document naming by passing the expected output path in the agent prompt:
```
Create the research document at: {expectedPath}
```

This is already done for SolutionOrchestrator's clarification and decomposition prompts. Extend the pattern to all phases.

**Key Discoveries:**
- `CurrentCodingSubPhase` is shared between Research and Coding — ambiguous on resume
- Phase skipping in `RunForContextAsync` is self-described as a "hack" and is fragile
- Planning prompt conflicts with agent design (interactive Q&A vs autonomous decision-making)
- "Automatic Mode" text is vestigial and should be removed
- Per-release orchestrators use NullLogger, losing all diagnostic output
- Document output detection has a brittle 4-level fallback chain that can be simplified by passing expected paths

| File | Relevance |
|------|-----------|
| `Hugh/Models/LoopState.cs` | `CurrentCodingSubPhase` needs splitting |
| `Hugh/Orchestration/LoopOrchestrator.cs` | `RunForContextAsync` hack, prompt conflicts, document path passing |
| `Hugh/Orchestration/LoopStateManager.cs` | Needs `CreateRunFromPhaseAsync` method |
| `Hugh/Orchestration/SolutionOrchestrator.cs` | `BuildReleaseOrchestrator` NullLogger issue |
| `Hugh/Orchestration/PhaseExecutor.cs` | `ExtractOutputDocumentPath` fallback chain |
| `Hugh/Configuration/HughConfig.cs` | New config for autonomous planning |

**Gaps:** None
**Assumptions:** State migration for `CurrentCodingSubPhase` split can be handled via JSON deserialization fallback.

---

## Overview

Hugh is approximately **70% of the way** to the target vision of a fully autonomous, session-isolated, document-tracked development pipeline. The core architecture — document-driven continuity, checkpoint-based state persistence, sub-phase loop pattern, dual-detection signal architecture, and multi-release decomposition — is fundamentally sound. No architectural redesign is needed.

The **30% gap** concentrates in six areas:

1. **No consolidated progress document** (High) — Progress is scattered across JSON state files, emoji tables in individual documents, and ephemeral console output. No single document provides a complete picture of solution progress across releases, phases, and tasks.

2. **Planning lacks sub-phase isolation** (High) — Planning runs in a single session while Research and Coding get fresh sessions per sub-phase. Complex plans risk context exhaustion. The sub-phase loop pattern already proven in research and coding should be extended to planning.

3. **Coding doesn't go to task granularity** (Medium) — The coding loop operates at plan-phase level, but phases can contain many tasks. Large phases risk context exhaustion and quality degradation. Task-level session isolation (opt-in) would address this.

4. **No inter-release learning** (Medium) — `ReleaseContext` passes only release names to subsequent releases. No information about actual files, APIs, or patterns from prior releases flows forward. A release summary document bridges this gap.

5. **Review is non-iterative** (Medium) — Review runs once with no feedback loop. Review findings are logged but not acted upon before coding begins. A review-revise cycle would catch plan quality issues.

6. **No git branch strategy** (Medium) — No branch isolation between releases, no rollback on failure, no per-phase commits. Partial failures leave the working branch in an inconsistent state.

Additionally, several **state model and prompt consistency issues** create behavioral bugs: shared sub-phase counter, hacky phase skipping, conflicting planning prompts, vestigial "Automatic Mode" text, NullLogger in per-release orchestrators, and fragile document output detection.

## Key Findings

1. **The sub-phase loop pattern is Hugh's most important architectural concept** — it enables per-unit-of-work session isolation with document-driven continuity. It's already proven for Research and Coding. Extending it to Planning (and optionally to task-level Coding) closes the two largest granularity gaps.

2. **Document-driven continuity is the right architecture** — each session re-reads documents from disk, enabling clean context isolation. This scales to any number of sessions without shared memory or context carryover.

3. **A `ProgressReporter` component is the highest-impact improvement** — it transforms Hugh from a black box (check JSON files manually) into an observable system with a living progress document updated after every state transition.

4. **Planning as single-shot contradicts agent design** — `pch-planner` was designed for multi-session "one phase at a time" filling, but Hugh runs it in one shot. This is the same class of bug that caused the research phase skip failure (documented in research 023).

5. **Inter-release learning requires only a release summary document** — not a complex knowledge graph or shared memory system. Git diff output + plan decisions provide sufficient context for subsequent releases.

6. **Git branch management should be opt-in** — not all users want Hugh managing their branches. But for multi-release solutions, branch isolation is critical for rollback safety.

7. **Prompt conflicts between agent prompts and orchestrator prompts are a recurring issue** — the solution is built-in prompt overrides for autonomous mode, not modifying the shared agent prompts (which must remain VS Code compatible).

8. **State model issues are low-risk but should be cleaned up** — shared sub-phase counter, hacky phase skipping, and NullLogger are code quality issues that make the codebase harder to maintain and debug.

## Actionable Conclusions

### Priority 1 — High Impact, Foundational

1. **Build `ProgressReporter`** — Hook into state transition methods to generate/update a consolidated markdown progress document after every phase completion. Include release overview, research topic status, plan phase status, coding phase status, verification results, timing, and decisions.

2. **Add `ExecutePlanningPhasesAsync`** — Mirror the research sub-phase loop pattern. Session 0 creates plan skeleton; Sessions 1..N fill in one phase each. Add `PlanCreationPhaseParser` to detect incomplete plan sections.

3. **Fix prompt conflicts** — Create built-in autonomous planning prompt override. Remove all "Automatic Mode" references. Add explicit Hugh CLI context to all phase prompts.

### Priority 2 — Medium Impact, Quality Improvements

4. **Add review-revise cycle** — `ExecuteReviewCycleAsync` with reviewer → planner revision → re-review loop. Bounded by `MaxReviewCycles` config.

5. **Add inter-release context** — Generate release summary document after each release (git diff + decisions). Pass to subsequent releases via enriched `ReleaseContext`.

6. **Add git branch management** — `GitBranchManager` with per-release branches, per-phase commits, and rollback capability. Opt-in via `Git.BranchingEnabled` config.

### Priority 3 — Task-Level Granularity & Cleanup

7. **Add task-level coding sessions** (opt-in) — Inner task loop within `ExecuteCodingPhasesAsync`. `TaskParser` extension to `PlanPhaseParser`. Config: `Coding.Granularity: "task"`.

8. **Fix state model issues** — Split `CurrentCodingSubPhase`, add `CreateRunFromPhaseAsync`, fix NullLogger in per-release orchestrators, simplify document output detection by passing expected paths.

### Estimated Scope

| Improvement | New/Modified Files | Estimated Lines | Complexity |
|---|---|---|---|
| ProgressReporter | 2 new, 3 modified | ~400 | Medium |
| Planning sub-phase loop | 1 new parser, 1 modified orchestrator | ~250 | Medium |
| Prompt fixes | 1 new built-in prompt, 1 modified orchestrator | ~150 | Low |
| Review-revise cycle | 1 modified orchestrator, 1 modified config | ~200 | Medium |
| Inter-release context | 1 new component, 2 modified | ~250 | Medium |
| Git branch management | 1 new component, 2 modified orchestrators | ~350 | Medium-High |
| Task-level coding | 1 modified parser, 1 modified orchestrator, 1 config | ~200 | Medium |
| State model fixes | 3 modified files, state migration | ~200 | Low-Medium |
| **Total** | **~8 new/modified files** | **~2,000 lines** | **Medium** |

## Open Questions

- Should the `ProgressReporter` update on every sub-phase completion (more granular but more I/O) or only on phase completion (less noise)?
- Should the review-revise cycle use the same `pch-plan-reviewer` agent for re-review, or a lighter-weight validation pass?
- For git branch management, should Hugh auto-merge release branches or require user confirmation before merge?
- Should the planning sub-phase loop use the existing `pch-planner` agent prompt or a dedicated autonomous variant?
- What is the right default for `Coding.Granularity` — should new installations default to "task" or "phase"?

## Standards Applied

No organizational standards applicable to this research.

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher (manual analysis) |
| Created Date | 2026-02-27 |
| Status | ✅ Complete |
| Current Phase | ✅ 8 of 8 Complete |
| Path | /docs/research/024-hugh-orchestration-gap-analysis.md |
