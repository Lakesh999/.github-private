# Implementation Plan: pch-coder Subagent Orchestration

## Document Information

| Field | Value |
|-------|-------|
| Version | v3.0 |
| Created | January 15, 2026 |
| Status | ✅ Implementation Complete |
| Owner | pch-planner |
| Plan Number | 001 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | January 15, 2026 | pch-planner | Initial plan creation |
| v1.1 | January 15, 2026 | pch-planner | Added execution mode selection (Decision 1) |
| v1.2 | January 15, 2026 | pch-planner | Added subagent prompt strategy (Decision 2) |
| v1.3 | January 15, 2026 | pch-planner | Added deployment handling (Decision 3) |
| v1.4 | January 15, 2026 | pch-planner | Added error handling (Decision 4) |
| v1.5 | January 15, 2026 | pch-planner | Added progress reporting (Decision 5) |
| v1.6 | January 15, 2026 | pch-planner | Added YAML config decision (Decision 6) |
| v2.0 | January 15, 2026 | pch-planner | Holistic review completed; execution plan finalized |
| v2.1 | January 15, 2026 | pch-plan-reviewer | Review complete; location anchors updated; verification task added |
| v2.2 | January 15, 2026 | pch-planner | Added Phase 0 prerequisite to work on beta-pch-coder.md copy |
| v2.3 | January 15, 2026 | pch-plan-reviewer | Added exact anchors, explicit content refs, Phase 0 verification |
| v3.0 | January 15, 2026 | pch-coder | Implementation complete; all phases executed successfully |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-01-15 | Execution Mode Selection Approach | A — Upfront Single Question | Simplest UX; user answers once at start, mode applies to entire plan |
| 1 | 2026-01-15 | Subagent Prompt Strategy | A — Full Plan Embedding | Maximum context for subagent; subagent has complete picture without extra file reads |
| 1 | 2026-01-15 | Deployment Phase Handling | B — Pause Before Deployment Phase | Preserves automation for implementation while requiring user control for deployment |
| 1 | 2026-01-15 | Subagent Error/Blocker Handling | D — User Choice at Blocker | Gives user control over error handling while maintaining automation flow |
| 1 | 2026-01-15 | Progress Reporting Format | B — Full Phase Report | Keeps user fully informed after each phase; matches manual mode visibility |
| 1 | 2026-01-15 | YAML Frontmatter Tool Config | B — Rely on Default Inheritance | Simpler; runSubagent in default toolset; avoid maintenance burden |
| 1 | 2026-01-15 | Plan Document Updates | A — Orchestrator Updates | Subagent returns results; orchestrator parses and updates plan document |

---

## Overview

### Feature Summary

Update the `pch-coder` agent to support subagent-based phase orchestration. The enhanced agent will:

1. **Ask the user for execution preference** at the start of each plan execution:
   - **Manual Mode**: Execute phases one at a time with user control between phases (current behavior)
   - **Automatic Mode**: Orchestrate all phases automatically using subagent calls, one phase per subagent

2. **Execute phases via subagents** (in automatic mode):
   - Invoke a subagent for each phase
   - Wait for subagent completion before proceeding
   - Process results and update the plan document
   - Handle errors/blockers gracefully

3. **Maintain user visibility** throughout automatic execution:
   - Report progress after each phase
   - Allow user to pause or cancel automation
   - Surface any blockers immediately

### Research Foundation

This plan is based on the research document: [001-subagent-integration-for-pch-coder.md](../research/001-subagent-integration-for-pch-coder.md)

Key research findings incorporated:
- Subagents provide fresh context per invocation (solving context exhaustion)
- `runSubagent` tool with structured prompts enables reliable delegation
- Plan document ownership must stay with the main orchestrator agent
- Hybrid approach recommended (option for both manual and automatic modes)

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| FR-1 | Ask user for execution mode (manual vs automatic) before starting | Must Have | ⏳ |
| FR-2 | Support manual mode (current behavior - one phase per session) | Must Have | ⏳ |
| FR-3 | Support automatic mode (subagent orchestration across phases) | Must Have | ⏳ |
| FR-4 | Invoke one subagent per phase in automatic mode | Must Have | ⏳ |
| FR-5 | Process subagent results and update plan document | Must Have | ⏳ |
| FR-6 | Handle subagent errors/blockers gracefully | Must Have | ⏳ |
| FR-7 | Report progress to user after each phase in automatic mode | Must Have | ⏳ |
| FR-8 | Allow user to pause/cancel automatic execution | Should Have | ⏳ |
| FR-9 | Skip deployment phases in automatic mode (require manual handling) | Must Have | ⏳ |

### Non-Functional Requirements

| ID | Requirement | Priority | Status |
|----|-------------|----------|--------|
| NFR-1 | Preserve existing pch-coder behavior when manual mode selected | Must Have | ⏳ |
| NFR-2 | Document VS Code setting prerequisite (chat.customAgentInSubagent.enabled) | Must Have | ⏳ |
| NFR-3 | Provide clear error messages when subagent invocation fails | Must Have | ⏳ |

### Out of Scope

- Parallel phase execution (phases must run sequentially)
- Nested subagent calls (subagents cannot spawn their own subagents)
- Automatic retry of failed phases (user must decide on retry)
- Task-level subagent delegation within a phase (research identified this as optional)

---

## Technical Design

### Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│                    pch-coder (Orchestrator)                             │
├─────────────────────────────────────────────────────────────────────────┤
│  1. Ask user: Manual or Automatic mode?                                 │
│                                                                         │
│  IF MANUAL MODE:                                                        │
│    └── Execute current behavior (one phase per session)                 │
│                                                                         │
│  IF AUTOMATIC MODE:                                                     │
│    └── For each phase in plan:                                          │
│        ├── Check if phase is deployment checkpoint                      │
│        │   └── YES: Pause, present deployment options, wait for user    │
│        │   └── NO: Continue to subagent invocation                      │
│        ├── Invoke subagent with full plan context                       │
│        ├── Wait for subagent completion                                 │
│        ├── Parse result (STATUS, FILES, NOTES, etc.)                    │
│        ├── Update plan document with results                            │
│        ├── Report progress to user                                      │
│        └── If blocked: Stop and report; else: continue to next phase   │
│                                                                         │
│  2. Final summary when all phases complete                              │
└─────────────────────────────────────────────────────────────────────────┘
```

**Deployment Phase Detection:**

A phase is considered a deployment checkpoint if:
- Phase name contains "deploy", "deployment", or "release"
- Phase description mentions deployment, git push, or CI/CD pipeline
- Plan document has explicit deployment checkpoint marker after the phase

**Pause Behavior for Deployment:**

When a deployment phase is detected in automatic mode:

```
⏸️ **Automatic Execution Paused — Deployment Required**

**Completed Phases:** {N-1} of {total}
**Next Phase:** Phase {N}: {Phase Name} (Deployment Checkpoint)

Deployment phases require manual handling. Please:
1. Review the changes made so far
2. Handle deployment as needed (commit, push, deploy)
3. Resume automation when ready

**To continue automatic execution after deployment:**
Say: "Continue automatic execution from Phase {N+1}"

**To switch to manual mode:**
Say: "Switch to manual mode"
```

### YAML Frontmatter Changes

**Approach:** Rely on Default Inheritance

The YAML frontmatter will remain minimal. No `tools` field will be added since `runSubagent` is part of the default tool set. The only change is documentation.

**Current Frontmatter (unchanged):**

```yaml
---
name: pch-coder
description: Executes implementation plan steps with precise, production-ready code following established patterns
model: Claude Opus 4.5
handoffs:
  - label: Request Plan Update
    agent: pch-planner
    prompt: Update the implementation plan based on issues discovered during implementation
    send: false
---
```

**Prerequisite Documentation:**

The agent instructions must document that automatic mode requires:
- VS Code setting: `chat.customAgentInSubagent.enabled: true`
- This setting enables custom agents to invoke subagents

### Execution Mode Selection

**Approach:** Upfront Single Question

At the start of plan execution (before Step 1: Load the Plan), the agent will ask:

```
🔄 **Execution Mode Selection**

I'm ready to implement the plan at `[plan path]`.

**How would you like to proceed?**

1️⃣ **Manual Mode** (Recommended for first-time plans)
   I'll execute one phase at a time. After each phase, you'll start a new session to continue.
   This gives you full control and the ability to review between phases.

2️⃣ **Automatic Mode** (For trusted, reviewed plans)
   I'll orchestrate all phases automatically using isolated subagents.
   Each phase runs in fresh context. I'll report results after each phase completes.
   ⚠️ Requires VS Code setting: `chat.customAgentInSubagent.enabled: true`

**Enter 1 or 2** (or say "manual" / "automatic"):
```

**Mode Persistence:** Once selected, the mode applies to the entire plan execution session. The mode is NOT persisted to the plan document — it's a session-level choice.

**Default Behavior:** If the user provides a plan path without explicitly choosing a mode, default to **Manual Mode** to preserve backward compatibility with existing workflows.

### Subagent Invocation Pattern

**Approach:** Full Plan Embedding

When invoking a subagent for a phase, embed the entire implementation plan document into the prompt. This provides maximum context so the subagent has the complete picture including:
- All codebase patterns and conventions
- Implementation notes from previous phases
- Full requirements and acceptance criteria
- Dependencies and prerequisites

**Subagent Prompt Template:**

```markdown
You are pch-coder executing Phase {N} of an implementation plan.

**Your Assignment:** Execute ONLY Phase {N}: {Phase Name}
**Plan Document Path:** {plan_path}

---

## FULL IMPLEMENTATION PLAN

{entire_plan_document_content}

---

## YOUR INSTRUCTIONS

1. Locate Phase {N} in the Execution Plan section above
2. Execute ALL tasks in Phase {N} sequentially
3. Follow all code quality standards and patterns documented in the plan
4. Update task statuses in your response (do NOT modify the plan file directly)
5. Report any blockers immediately

**IMPORTANT:** Execute ONLY Phase {N}. Do not proceed to subsequent phases.

**Return Format (REQUIRED):**
```
PHASE: {N}
STATUS: success | partial | blocked
TASKS_COMPLETED: [list of task IDs marked complete]
TASKS_BLOCKED: [list of task IDs that could not complete, with reasons]
FILES_MODIFIED: [list of file paths]
FILES_CREATED: [list of file paths]
DEVIATIONS: [any changes from plan, or "none"]
NOTES: [implementation details for plan update]
VERIFICATION: [how to verify this phase works]
```
```

**Context Window Consideration:** Full plan embedding uses more tokens but eliminates ambiguity and extra file reads. For very large plans (>5000 lines), consider chunking or summarizing completed phases.

### Result Processing

**Approach:** Full Phase Report

After each subagent completes, display a complete session summary similar to manual mode. This keeps the user fully informed and provides immediate visibility into what changed.

**Post-Phase Report Template:**

```
✅ **Phase {N} Complete** ({current}/{total} phases)

**Phase:** {N}: {Phase Name}
**Status:** {success | partial | blocked}
**Duration:** {estimated time or "N/A"}

**Tasks Completed:**
- [{task_id}]: {task_description} ✅
- [{task_id}]: {task_description} ✅

**Files Modified:**
- `{file_path}`
- `{file_path}`

**Files Created:**
- `{file_path}`

**Deviations from Plan:**
{deviations_or_"None"}

**Implementation Notes:**
{notes_from_subagent}

**Verification:**
{verification_steps}

---

➡️ **Proceeding to Phase {N+1}:** {Next Phase Name}...
```

**Plan Document Updates:**

After processing each subagent result, the orchestrator updates the plan document:

1. Mark completed tasks as ✅ Complete
2. Mark blocked tasks as ❌ Blocked with reason
3. Add implementation notes to the "Implementation Notes" section
4. Update the Progress Summary table
5. Record any deviations discovered

**Plan Document Ownership (CRITICAL):**

- **Subagents do NOT modify the plan document** — They only return structured results
- **Orchestrator owns all plan updates** — Ensures consistent formatting and single source of truth
- **Subagent prompt explicitly prohibits plan modification** — "do NOT modify the plan file directly"

This design ensures:
- No conflicting updates from multiple sources
- Orchestrator can verify/validate before committing changes
- Clear audit trail of what was updated and when

**Final Completion Report:**

After all phases complete:

```
🎉 **All Phases Complete — Implementation Finished**

**Plan:** {plan_path}
**Total Phases:** {N}
**Total Tasks:** {count}
**Execution Mode:** Automatic (Subagent Orchestration)

**Summary by Phase:**
| Phase | Name | Status | Tasks | Files Changed |
|-------|------|--------|-------|---------------|
| 1 | {name} | ✅ | {n}/{n} | {count} |
| 2 | {name} | ✅ | {n}/{n} | {count} |
| ... | ... | ... | ... | ... |

**All Files Modified:**
- `{file_path}`
- `{file_path}`
- ...

**Next Steps:**
- Review the changes
- Run tests: `{test_command}`
- Commit when satisfied: `git add . && git commit -m "{message}"`

**Plan document updated at:** `{plan_path}`
```

### Error Handling

**Approach:** User Choice at Blocker

When a subagent returns a `blocked` or `partial` status, pause automatic execution and present the user with options:

```
⚠️ **Phase {N} Completed with Issues**

**Status:** {partial | blocked}
**Tasks Completed:** {list}
**Tasks Blocked:** {list with reasons}

**Blocker Details:**
{blocker_information_from_subagent}

---

**How would you like to proceed?**

1️⃣ **Continue to Next Phase**
   Proceed with Phase {N+1} despite the issues. Use if blocked tasks are non-critical
   or independent from remaining phases.

2️⃣ **Skip to Phase {M}** (if applicable)
   Jump to a specific phase that doesn't depend on blocked tasks.
   Say: "Skip to Phase {M}"

3️⃣ **Stop Automatic Execution**
   End automation here. Investigate the blocker and restart when resolved.
   Progress has been saved to the plan document.

4️⃣ **Retry Phase {N}**
   Re-run the same phase with a fresh subagent. Use if the failure
   might be transient or you've made manual fixes.

**Enter 1, 2, 3, or 4:**
```

**Subagent Failure (Tool Error):**

If the subagent invocation itself fails (not a task blocker, but a tool error):

```
❌ **Subagent Invocation Failed**

**Phase:** {N}: {Phase Name}
**Error:** {error_message}

This may indicate:
- VS Code setting `chat.customAgentInSubagent.enabled` is not enabled
- Context window exceeded
- Transient service error

**Options:**
1️⃣ **Retry** — Try invoking the subagent again
2️⃣ **Switch to Manual Mode** — Execute this phase directly without subagent
3️⃣ **Stop** — End execution and investigate

**Enter 1, 2, or 3:**
```

---

## Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| Agent YAML Frontmatter | `agents/pch-coder.md` → `agents/beta-pch-coder.md` | Copy to beta file; update name field |
| Execution Workflow | `agents/beta-pch-coder.md` — section "## Execution Workflow" | Insert mode selection before Step 1 |
| Phase Completion | `agents/beta-pch-coder.md` — section "## Phase Completion and Session Handoff" | Modify for automatic mode continuation |
| Session Handoff | `agents/beta-pch-coder.md` — subsection "### Session End Message" | Conditional based on execution mode |
| Deployment Handling | `agents/beta-pch-coder.md` — section "## Deployment Handling" | Cross-reference from automatic mode deployment pause |

**Note:** All modifications are made to `beta-pch-coder.md`, a copy of the original `pch-coder.md`. The original file remains unchanged until the beta version is validated.

---

## Dependencies and Prerequisites

| Dependency | Type | Status | Notes |
|------------|------|--------|-------|
| VS Code Setting | Configuration | Required | `chat.customAgentInSubagent.enabled` must be `true` |
| runSubagent Tool | Built-in Tool | Available | Part of VS Code Copilot default toolset |
| Research Document | Documentation | ✅ Complete | [001-subagent-integration-for-pch-coder.md](../research/001-subagent-integration-for-pch-coder.md) |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Subagent feature is experimental | Medium | Medium | Document as optional enhancement; preserve manual mode |
| Subagent cannot ask clarifying questions | High | Medium | Ensure plans are detailed; surface blockers immediately |
| Context lost between subagent calls | Low | High | Use file-based state (plan document) for persistence |
| Deployment steps run automatically | Medium | High | Explicitly exclude deployment from automatic mode |

---

## Progress Summary

| Phase | Description | Status | Tasks |
|-------|-------------|--------|-------|
| Phase 0 | Create Beta Copy of pch-coder.md | ✅ Complete | 2 tasks |
| Phase 1 | Add Execution Mode Selection | ✅ Complete | 4 tasks |
| Phase 2 | Implement Subagent Orchestration Loop | ✅ Complete | 5 tasks |
| Phase 3 | Add Result Processing and Plan Updates | ✅ Complete | 4 tasks |
| Phase 4 | Add Error Handling and User Interaction | ✅ Complete | 4 tasks |
| Phase 5 | Documentation and Prerequisites | ✅ Complete | 4 tasks |

---

## Execution Plan

### Phase 0: Create Beta Copy of pch-coder.md

**Estimated Session Size:** Small
**Files to Modify:** 0 (creates new file)
**Prerequisites:** None
**Entry Point:** Read `agents/pch-coder.md` to verify it exists
**Verification:** `agents/beta-pch-coder.md` exists and contains identical content to `agents/pch-coder.md`

| Step | Task | Status | Acceptance Criteria |
|------|------|--------|---------------------|
| 0.1 | Create copy of `agents/pch-coder.md` as `agents/beta-pch-coder.md` | ✅ Complete | New file exists with identical content |
| 0.2 | Update YAML frontmatter name field to `beta-pch-coder` | ✅ Complete | `name: beta-pch-coder` in frontmatter |

**Detailed Task Specifications:**

#### Task 0.1: Create beta copy

**Action:** Copy the entire contents of `agents/pch-coder.md` to a new file `agents/beta-pch-coder.md`

**Rationale:** All changes in this plan will be made to the beta copy, allowing the original `pch-coder.md` to remain stable until the new features are validated.

#### Task 0.2: Update YAML frontmatter name

**Location:** `agents/beta-pch-coder.md` — YAML frontmatter block

**Find:**
```yaml
name: pch-coder
```

**Replace with:**
```yaml
name: beta-pch-coder
```

---

### Phase 1: Add Execution Mode Selection

**Estimated Session Size:** Small
**Files to Modify:** 1 (`agents/beta-pch-coder.md`)
**Prerequisites:** Phase 0 complete — verify `agents/beta-pch-coder.md` exists and contains `name: beta-pch-coder` in YAML frontmatter
**Entry Point:** Read `agents/beta-pch-coder.md` lines 1-50
**Verification:** Review the new section appears after Core Responsibilities

| Step | Task | Status | Acceptance Criteria |
|------|------|--------|---------------------|
| 1.1 | Add "Execution Mode" section after Core Responsibilities | ✅ | New section exists with mode selection prompt template |
| 1.2 | Define the upfront question format with options 1 and 2 | ✅ | Question includes Manual and Automatic options with descriptions |
| 1.3 | Document VS Code setting prerequisite in Automatic option | ✅ | `chat.customAgentInSubagent.enabled` mentioned with warning |
| 1.4 | Add mode selection logic description | ✅ | Instructions for how agent should handle each mode choice |

**Detailed Task Specifications:**

#### Task 1.1: Add "Execution Mode" section

**Location:** After the "## Core Responsibilities" section and before the "## One Phase Per Session (CRITICAL)" section

**Anchor Text (exact match for insertion):**
```markdown
- **End the session after completing the phase** and request a new session for the next phase

## One Phase Per Session (CRITICAL)
```

**Action:** Replace the above anchor with:
```markdown
- **End the session after completing the phase** and request a new session for the next phase

## Execution Mode Selection

Before beginning implementation, ask the user which execution mode they prefer. This determines how phases will be processed.

## One Phase Per Session (CRITICAL)
```

**Content to add:**
```markdown
## Execution Mode Selection

Before beginning implementation, ask the user which execution mode they prefer. This determines how phases will be processed.
```

#### Task 1.2: Define upfront question format

**Location:** Within the new "Execution Mode Selection" section

**Content:** Copy verbatim the full mode selection prompt from **Technical Design > Execution Mode Selection** section of this plan document (the block starting with "🔄 **Execution Mode Selection**" through the "Enter 1 or 2" line)

#### Task 1.3: Document VS Code setting prerequisite

**Location:** Within the Automatic mode option description

**Content:** Warning about experimental feature and required setting

#### Task 1.4: Add mode selection logic

**Location:** After the question template

**Content:** Instructions describing:
- If user chooses 1 or "manual": proceed with current one-phase-per-session behavior
- If user chooses 2 or "automatic": proceed to subagent orchestration workflow
- Default to Manual if unclear response

---

### Phase 2: Implement Subagent Orchestration Loop

**Estimated Session Size:** Medium
**Files to Modify:** 1 (`agents/beta-pch-coder.md`)
**Prerequisites:** Phase 1 complete
**Entry Point:** Read `agents/beta-pch-coder.md` new Execution Mode section
**Verification:** Automatic Mode Workflow section exists with complete loop logic

| Step | Task | Status | Acceptance Criteria |
|------|------|--------|---------------------|
| 2.1 | Add "Automatic Mode Workflow" section | ✅ Complete | New major section for subagent orchestration |
| 2.2 | Document the orchestration loop logic | ✅ Complete | Step-by-step loop: load plan, iterate phases, invoke subagent |
| 2.3 | Add subagent prompt template | ✅ Complete | Full template with plan embedding and return format |
| 2.4 | Add deployment phase detection logic | ✅ Complete | How to detect deployment phases and pause |
| 2.5 | Add deployment pause prompt template | ✅ Complete | User-facing message when deployment detected |

**Detailed Task Specifications:**

#### Task 2.1: Add "Automatic Mode Workflow" section

**Location:** After the Execution Mode Selection section (new major section)

**Content structure:**
```markdown
## Automatic Mode Workflow

When the user selects Automatic Mode (option 2), follow this orchestration workflow instead of the standard one-phase-per-session approach.
```

#### Task 2.2: Document orchestration loop logic

**Location:** Within Automatic Mode Workflow section

**Content:** Numbered steps:
1. Load the implementation plan document
2. Identify all phases and their status
3. For each phase marked ⏳ Not Started:
   a. Check if phase is a deployment checkpoint
   b. If deployment: pause and present options
   c. If not deployment: invoke subagent
   d. Process subagent result
   e. Update plan document
   f. Display phase report
   g. Continue to next phase or handle blocker
4. After all phases: display final completion report

#### Task 2.3: Add subagent prompt template

**Location:** Within Automatic Mode Workflow, under "Subagent Invocation"

**Content:** Full template as specified in Technical Design > Subagent Invocation Pattern

#### Task 2.4: Add deployment phase detection logic

**Location:** Within Automatic Mode Workflow

**Content:** Rules for detecting deployment phases:
- Phase name contains "deploy", "deployment", "release"
- Phase has deployment checkpoint marker
- Description mentions git push, CI/CD, pipeline

#### Task 2.5: Add deployment pause prompt template

**Location:** After deployment detection logic

**Content:** Pause message as specified in Technical Design > Architecture Overview > Pause Behavior for Deployment

---

### Phase 3: Add Result Processing and Plan Updates

**Estimated Session Size:** Medium
**Files to Modify:** 1 (`agents/beta-pch-coder.md`)
**Prerequisites:** Phase 2 complete
**Entry Point:** Read the Automatic Mode Workflow section in `agents/beta-pch-coder.md`
**Verification:** Result processing and reporting sections exist with complete templates

| Step | Task | Status | Acceptance Criteria |
|------|------|--------|---------------------|
| 3.1 | Add "Processing Subagent Results" subsection | ✅ | Describes how to parse the structured return format |
| 3.2 | Add plan document update instructions | ✅ | How to update task status, add notes, record deviations |
| 3.3 | Add post-phase report template | ✅ | Full phase report matching Technical Design > Result Processing |
| 3.4 | Add final completion report template | ✅ | Summary report for when all phases complete |

**Detailed Task Specifications:**

#### Task 3.1: Add "Processing Subagent Results" subsection

**Location:** Within Automatic Mode Workflow, after subagent invocation

**Content:**
- Expected return format (STATUS, FILES_MODIFIED, etc.)
- How to parse each field
- Status interpretation (success, partial, blocked)

#### Task 3.2: Add plan document update instructions

**Location:** After result processing

**Content:**
- Update task statuses based on TASKS_COMPLETED
- Mark blocked tasks with reasons from TASKS_BLOCKED
- Add implementation notes from NOTES field
- Record deviations from DEVIATIONS field

#### Task 3.3: Add post-phase report template

**Location:** After plan update instructions

**Content:** Full template as specified in Technical Design > Result Processing > Post-Phase Report Template

#### Task 3.4: Add final completion report template

**Location:** After post-phase report

**Content:** Final summary as specified in Technical Design > Result Processing > Final Completion Report

---

### Phase 4: Add Error Handling and User Interaction

**Estimated Session Size:** Medium
**Files to Modify:** 1 (`agents/beta-pch-coder.md`)
**Prerequisites:** Phase 3 complete
**Entry Point:** Read the Automatic Mode Workflow section in `agents/beta-pch-coder.md`
**Verification:** Error handling section exists with blocker dialog and failure recovery

| Step | Task | Status | Acceptance Criteria |
|------|------|--------|---------------------|
| 4.1 | Add "Handling Blockers in Automatic Mode" subsection | ✅ Complete | User choice dialog for blocked/partial status |
| 4.2 | Add handler logic for each user choice | ✅ Complete | Continue, Skip, Stop, Retry options documented |
| 4.3 | Add "Subagent Invocation Failure" subsection | ✅ Complete | Tool error handling separate from task blockers |
| 4.4 | Add recovery options for invocation failures | ✅ Complete | Retry, Switch to Manual, Stop options |

**Detailed Task Specifications:**

#### Task 4.1: Add "Handling Blockers in Automatic Mode" subsection

**Location:** Within Automatic Mode Workflow, after result processing

**Content:** Blocker dialog as specified in Technical Design > Error Handling

#### Task 4.2: Add handler logic for each user choice

**Location:** After blocker dialog template

**Content:**
- Option 1 (Continue): Proceed to next phase, note that issues exist
- Option 2 (Skip): Jump to specified phase, update skipped phases as ⏭️ Skipped
- Option 3 (Stop): End automatic execution, display current progress
- Option 4 (Retry): Re-invoke subagent for same phase with fresh context

#### Task 4.3: Add "Subagent Invocation Failure" subsection

**Location:** After blocker handling

**Content:** Failure dialog as specified in Technical Design > Error Handling > Subagent Failure

#### Task 4.4: Add recovery options for invocation failures

**Location:** After failure dialog

**Content:**
- Option 1 (Retry): Attempt subagent invocation again
- Option 2 (Switch to Manual): Execute phase directly in current context
- Option 3 (Stop): End execution for investigation

---

### Phase 5: Documentation and Prerequisites

**Estimated Session Size:** Small
**Files to Modify:** 1 (`agents/beta-pch-coder.md`)
**Prerequisites:** Phase 4 complete
**Entry Point:** Read full `agents/beta-pch-coder.md`
**Verification:** Prerequisites section exists; manual mode sections updated with cross-references

| Step | Task | Status | Acceptance Criteria |
|------|------|--------|---------------------|
| 5.1 | Add "Prerequisites for Automatic Mode" section | ✅ Complete | VS Code setting documented prominently |
| 5.2 | Update "One Phase Per Session" section | ✅ Complete | Clarify this applies to Manual Mode; add cross-reference |
| 5.3 | Add "Choosing Between Modes" guidance | ✅ Complete | When to use Manual vs Automatic with recommendations |
| 5.4 | End-to-end verification of Automatic Mode | ⏳ User Task | Test complete workflow with sample plan |

**Detailed Task Specifications:**

#### Task 5.1: Add "Prerequisites for Automatic Mode" section

**Location:** After Execution Mode Selection, before Automatic Mode Workflow

**Content:**
```markdown
### Prerequisites for Automatic Mode

Before using Automatic Mode, ensure:

1. **VS Code Setting Enabled:**
   ```json
   {
     "chat.customAgentInSubagent.enabled": true
   }
   ```
   This setting enables custom agents to invoke subagents. Without it, automatic mode will fail.

2. **Plan Document is Complete:**
   The implementation plan should be reviewed and approved by `pch-plan-reviewer`. Automatic mode cannot ask clarifying questions during execution.

3. **No Interactive Steps:**
   Phases requiring user input (beyond deployment) should be handled in manual mode.

⚠️ **Note:** Subagent orchestration is an experimental feature. If issues occur, switch to Manual Mode.
```

#### Task 5.2: Update "One Phase Per Session" section

**Location:** The existing "## One Phase Per Session (CRITICAL)" section header

**Change:** Rename the section and add clarification that this applies to Manual Mode:

**Find this header:**
```markdown
## One Phase Per Session (CRITICAL)
```

**Replace with:**
```markdown
## One Phase Per Session (Manual Mode)

*This section applies when using Manual Mode. For Automatic Mode, see [Automatic Mode Workflow](#automatic-mode-workflow).*
```

**Also add cross-reference to Deployment Handling:** In the new Automatic Mode deployment pause section (Task 2.5), include:
```markdown
For full deployment handling options including `deployment.prompt.md` discovery and failure recovery, see [Deployment Handling](#deployment-handling).
```

#### Task 5.3: Add "Choosing Between Modes" guidance

**Location:** Within Execution Mode Selection section

**Content:**
```markdown
### Choosing Between Modes

| Scenario | Recommended Mode | Reason |
|----------|-----------------|--------|
| First time running a new plan | Manual | Review each phase before proceeding |
| Trusted, well-tested plan | Automatic | Efficiency gains from orchestration |
| Plan with deployment steps | Manual or Hybrid | Deployment needs manual attention |
| Complex or risky changes | Manual | Better control and review points |
| Repetitive/mechanical tasks | Automatic | Less user overhead |
| Learning the codebase | Manual | See each phase in detail |
```

#### Task 5.4: End-to-end verification of Automatic Mode

**Location:** N/A — Manual verification step

**⚠️ Note:** This task is completed by the user/operator after implementation, not by pch-coder. The coder agent should mark this task as ⏳ Not Started and include it in the handoff summary for user follow-up.

**Verification Steps:**
1. Create or identify a small test plan with 2-3 phases
2. Invoke `pch-coder` with the test plan
3. Select Automatic Mode (option 2) when prompted
4. Verify the following work correctly:
   - Mode selection prompt appears with both options
   - VS Code setting prerequisite warning is visible
   - Subagent invocation prompt is properly formatted with full plan embedding
   - Progress reporting shows phase completion details
   - Error handling dialogs appear correctly (if simulating a blocker)
5. Document any issues found for follow-up

**Acceptance Criteria:**
- Automatic mode workflow executes without errors
- All templates and prompts render correctly
- User can successfully complete a multi-phase plan using automatic mode

---

## Review Session Log

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Line number references in Codebase Patterns table are inaccurate and will become stale | Specificity | B — Use section header anchors | Updated Codebase Patterns table |
| 2 | Inconsistency: table says "Add tools field" but Decision 6 says no YAML changes | Clarity | A — Remove tools field reference | Updated Codebase Patterns table |
| 3 | Missing cross-reference from Automatic Mode deployment pause to existing Deployment Handling section | Completeness | A — Add cross-reference | Updated Task 5.2 |
| 4 | No end-to-end verification task for Automatic Mode | Completeness | C — Add Task 5.4 | Added Task 5.4 to Phase 5 |
| 5 | Task 1.1 insertion anchor is ambiguous — coder needs exact text to match | Specificity | A — Add exact anchor text | Updated Task 1.1 with anchor |
| 6 | Tasks 1.2-1.4 reference Technical Design without inline content | Specificity | A — Add explicit copy instruction | Updated Task 1.2 |
| 7 | Task 5.4 is manual verification mixed with coder tasks | Clarity | A — Add note that user completes this | Updated Task 5.4 |
| 8 | Phase 1 prerequisites don't specify how to verify Phase 0 | Completeness | A — Add verification instruction | Updated Phase 1 prerequisites |

---

## Review Summary

**Review Date:** January 15, 2026
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.1
**Reviewed Plan Version:** v2.3

### Review Metrics
- Issues Found: 8 (Critical: 0, Major: 1, Minor: 7)
- Clarifying Questions Asked: 2
- Sections Updated: Task 1.1, Task 1.2, Task 5.4, Phase 1 prerequisites, Review Session Log

### Key Improvements Made (This Review Session)
1. Added exact anchor text to Task 1.1 for reliable insertion during implementation
2. Added explicit copy instruction to Task 1.2 for verbatim content from Technical Design
3. Clarified Task 5.4 is completed by user/operator, not pch-coder agent
4. Added Phase 0 verification requirement to Phase 1 prerequisites

### Previous Review Improvements (v2.0 → v2.1)
1. Replaced fragile line number references with section header anchors for maintainability
2. Corrected inconsistency between Codebase Patterns table and Decision 6 (no YAML changes needed)
3. Added cross-reference from Automatic Mode deployment pause to existing Deployment Handling section
4. Added Task 5.4 for end-to-end verification of the complete Automatic Mode workflow

### Remaining Considerations
- The subagent feature (`chat.customAgentInSubagent.enabled`) is experimental; monitor for API changes
- For very large plans (>5000 lines), full plan embedding may approach context limits — document this limitation during implementation
- The first implementation should use a small test plan to validate the workflow before using on production plans

### Codebase Verification Results
- ✅ `agents/pch-coder.md` exists (479 lines)
- ✅ All referenced sections exist (Core Responsibilities, Execution Workflow, Phase Completion, Deployment Handling)
- ✅ Research document verified at expected location
- ✅ YAML frontmatter structure verified

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Implementation Notes

### Phase 0 - Create Beta Copy of pch-coder.md

**Completed:** January 15, 2026

**Files Created:**

- `agents/beta-pch-coder.md` (created)

**Deviations from Plan:**

- None

**Discoveries:**

- Original `pch-coder.md` contains 479 lines
- File structure verified: YAML frontmatter followed by markdown content

**Verification:**

- ✅ `agents/beta-pch-coder.md` exists with identical content structure
- ✅ YAML frontmatter contains `name: beta-pch-coder`
- ✅ No markdown lint errors in created file

### Phase 1 - Add Execution Mode Selection

**Completed:** January 15, 2026

**Files Modified:**

- `agents/beta-pch-coder.md`

**Deviations from Plan:**

- None

**Changes Made:**

- Added new "## Execution Mode Selection" section after Core Responsibilities
- Included "### Mode Selection Prompt" with full dialog template
- Added "### Mode Selection Logic" with handling instructions for each choice
- Added "### Choosing Between Modes" guidance table
- Renamed "## One Phase Per Session (CRITICAL)" to "## One Phase Per Session (Manual Mode)"
- Added cross-reference note linking to Automatic Mode Workflow

**Verification:**

- ✅ New section appears at line 24 after Core Responsibilities
- ✅ Mode selection prompt includes Manual and Automatic options
- ✅ VS Code setting `chat.customAgentInSubagent.enabled` documented with warning
- ✅ Mode selection logic covers all three cases (manual, automatic, unclear)
- ✅ No markdown lint errors in modified file

### Phase 2 - Implement Subagent Orchestration Loop

**Completed:** January 15, 2026

**Files Modified:**

- `agents/beta-pch-coder.md`

**Deviations from Plan:**

- None

**Changes Made:**

- Added "## Prerequisites for Automatic Mode" section with VS Code setting documentation
- Added "## Automatic Mode Workflow" major section
- Added "### Orchestration Loop" with complete step-by-step workflow
- Added "### Subagent Invocation" with full prompt template and return format
- Added "### Deployment Phase Detection" with detection rules
- Added "### Deployment Pause in Automatic Mode" with pause dialog template
- Added cross-reference to existing Deployment Handling section

**Verification:**

- ✅ Automatic Mode Workflow section exists after Execution Mode Selection
- ✅ Orchestration loop documents all steps: load plan, iterate phases, invoke subagent
- ✅ Subagent prompt template includes full plan embedding and structured return format
- ✅ Deployment phase detection rules are documented
- ✅ Deployment pause prompt template matches Technical Design specification
- ✅ No markdown lint errors in modified file

### Phase 3 - Add Result Processing and Plan Updates

**Completed:** January 15, 2026

**Files Modified:**

- `agents/beta-pch-coder.md`

**Deviations from Plan:**

- None

**Changes Made:**

- Added "### Processing Subagent Results" subsection with expected return format and parsing table
- Added "### Updating the Plan Document" subsection with update sequence and ownership rules
- Added "### Post-Phase Report" subsection with full phase completion report template
- Added "### Final Completion Report" subsection with comprehensive summary template

**Verification:**

- ✅ Processing Subagent Results section describes all return format fields with parsing guidance
- ✅ Plan document update instructions include task status updates, implementation notes, and ownership rules
- ✅ Post-phase report template matches Technical Design specification
- ✅ Final completion report template matches Technical Design specification
- ✅ No markdown lint errors in modified file

### Phase 4 - Add Error Handling and User Interaction

**Completed:** January 15, 2026

**Files Modified:**

- `agents/beta-pch-coder.md`

**Deviations from Plan:**

- None

**Changes Made:**

- Added "### Handling Blockers in Automatic Mode" subsection with user choice dialog for partial/blocked status
- Added handler logic table documenting all four options (Continue, Skip, Stop, Retry)
- Added detailed behavior descriptions for each option
- Added "### Subagent Invocation Failure" subsection with tool error dialog
- Added recovery options table for invocation failures (Retry, Switch to Manual, Stop)
- Added detailed behavior descriptions for each recovery option

**Verification:**

- ✅ Blocker handling section includes complete user choice dialog
- ✅ All four handler options (Continue, Skip, Stop, Retry) documented with logic
- ✅ Subagent invocation failure section separate from task blockers
- ✅ Recovery options include Retry, Switch to Manual, and Stop
- ✅ No markdown lint errors in modified file

### Phase 5 - Documentation and Prerequisites

**Completed:** January 15, 2026

**Files Modified:**

- None (verification phase - all content was proactively added in earlier phases)

**Deviations from Plan:**

- Tasks 5.1, 5.2, and 5.3 were completed proactively during Phases 1 and 2 rather than in Phase 5
- Task 5.4 (end-to-end verification) is marked as a user task to be completed after implementation

**Verification Results:**

- ✅ **Task 5.1**: "Prerequisites for Automatic Mode" section exists at lines 75-93 with VS Code setting documented
- ✅ **Task 5.2**: "One Phase Per Session" section renamed to "One Phase Per Session (Manual Mode)" with cross-reference at lines 504-508
- ✅ **Task 5.3**: "Choosing Between Modes" guidance table exists at lines 64-73
- ⏳ **Task 5.4**: End-to-end verification pending user testing

**Notes:**

- All Phase 5 documentation requirements were satisfied by content added in earlier phases
- The proactive approach during Phases 1-2 resulted in a more cohesive document structure
- No additional modifications needed to complete Phase 5

---

## Holistic Review Summary

### Decision Interactions Analysis

The six decisions made form a coherent design:

| Decision | Interacts With | Interaction Effect |
|----------|---------------|-------------------|
| Upfront Mode Selection | All others | Sets the execution path; automatic mode enables all subagent logic |
| Full Plan Embedding | Error Handling, Progress Reporting | Subagent has full context to provide detailed error info and notes for reports |
| Pause at Deployment | Error Handling | Both create user interaction points; consistent "pause and ask" pattern |
| User Choice at Blocker | Progress Reporting | Blocker dialog includes full context because we show full phase reports |
| Full Phase Report | Full Plan Embedding | Subagent returns rich data; orchestrator surfaces all of it |
| Default Tool Inheritance | All | Simplifies implementation; no YAML changes needed |

### Architectural Considerations

**Strengths:**
- **Backward compatible**: Manual mode is the default; existing behavior unchanged
- **User control preserved**: Multiple interaction points (mode selection, deployment, blockers)
- **Full visibility**: User sees complete reports matching manual mode experience
- **Simple implementation**: No YAML changes; additive documentation changes only

**Trade-offs Accepted:**
- **Verbosity**: Full phase reports in automatic mode create more output (user explicitly chose this)
- **User interaction during automation**: Blocker handling requires user input (user explicitly chose this)
- **Token usage**: Full plan embedding uses more context per subagent (user explicitly chose this for maximum context)

### Potential Concerns Identified

1. **Large Plan Handling**: Full plan embedding may exceed context limits for very large plans (>5000 lines). Mitigation: Document this limitation; recommend chunking for large plans.

2. **Interaction Frequency**: With deployment pauses + blocker choices + full reports, automatic mode may feel less "automatic". This is acceptable per user's choices prioritizing visibility and control.

3. **Subagent Feature Stability**: The `chat.customAgentInSubagent.enabled` setting is experimental. Mitigation: Document prominently; preserve manual mode as fallback.

### Risks Acknowledged

| Risk | User's Mitigation Choice |
|------|-------------------------|
| Cascading failures from blockers | User chose interactive blocker handling (D) over automatic stop |
| Lost visibility in automation | User chose full phase reports (B) over progressive summary |
| Deployment executed accidentally | Pause before deployment (B) prevents this |

### Final Validation

✅ All decisions are internally consistent
✅ No architectural conflicts identified
✅ User control and visibility prioritized throughout
✅ Backward compatibility maintained
✅ Implementation scope is manageable (additive changes only)

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** January 15, 2026
**Reviewed By:** pch-plan-reviewer
**Review Date:** January 15, 2026
**Status:** Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** `/docs/plans/001-pch-coder-subagent-orchestration.md`
