# Research: Subagent Integration for beta-pch-researcher Agent

## Document Information

| Field | Value |
|-------|-------|
| Document | 002-beta-pch-researcher-subagent-integration.md |
| Created | January 16, 2026 |
| Status | ✅ Complete |
| Current Phase | 5 of 5 (Complete) |

## Overview

This research investigated how to create a `beta-pch-researcher` agent that leverages subagent orchestration for automatic research phase execution, based on patterns established in `beta-pch-coder`. The investigation spanned five phases covering workflow comparison, orchestration design, research-specific considerations, return format design, and implementation recommendations.

### Key Findings

**Structural Compatibility:** The pch-coder and pch-researcher workflows share high structural similarity — both use phased approaches, document-based persistence, status tracking, and session handoffs. This makes the automatic mode pattern from beta-pch-coder directly transferable to research with targeted adaptations.

**Research-Specific Adaptations Required:**
1. **Clarification handling** — Research often needs user input to clarify scope. Since subagents cannot ask questions, the orchestrator must perform pre-flight checks for ambiguity and pause before invocation when needed.
2. **Three-level status** — Research uses `success`, `partial`, and `blocked` statuses (vs. binary task completion) reflecting the spectrum nature of research findings.
3. **No deployment equivalent** — Research doesn't deploy; remove deployment checkpoint logic entirely.
4. **Overview synthesis** — The orchestrator generates the final Overview section after all phases, rather than delegating to a subagent.

**Return Format Design:** Research subagents use a structured format with FINDINGS (markdown content), KEY_DISCOVERIES (insights), GAPS_FOUND (incomplete areas), FILES_ANALYZED, EXTERNAL_SOURCES, and ASSUMPTIONS_MADE fields. The orchestrator owns all document writes to ensure consistency.

**Implementation Scope:** The beta-pch-researcher agent adds approximately 345 lines to the 675-line pch-researcher base, primarily for mode selection, prerequisites, and the automatic mode workflow section.

### Top Discoveries

1. **Mode selection pattern transfers directly** — Same Manual/Automatic choice structure works for research
2. **Subagent return format needs research-specific fields** — FINDINGS replaces FILES_MODIFIED; GAPS_FOUND is new
3. **Pre-flight clarification is essential** — Must detect ambiguous scopes before subagent invocation
4. **External tools (fetch_webpage, MCP) are inherited** — Subagents have same tool access as orchestrator
5. **Partial status is a valid, common outcome** — Research frequently produces incomplete but valuable findings

### Recommendations

1. **Create beta-pch-researcher.md** by copying pch-researcher.md and adding automatic mode sections
2. **Preserve manual mode entirely** — Add mode context notes but keep existing workflow intact
3. **Implement pre-flight clarification checks** — Scan for ambiguity markers before subagent invocation
4. **Design robust return format parsing** — Handle all three status levels with appropriate user options
5. **Test with simple research documents first** — Validate orchestration loop before complex scenarios

---

## Research Objectives

Investigate how to create a `beta-pch-researcher` agent that uses subagents to execute research phases automatically, based on the existing `pch-researcher` prompt and the patterns established in `beta-pch-coder`.

### Key Questions to Answer

1. **What parallels exist between pch-coder and pch-researcher workflows?** — How do their phased approaches compare, and what patterns from beta-pch-coder can be reused?
2. **How should subagent orchestration work for research phases?** — What is the optimal subagent invocation pattern for research (vs. implementation)?
3. **What unique considerations exist for research subagents?** — How do research-specific aspects (external sources, web fetching, synthesis) affect subagent design?
4. **What return format should research subagents use?** — How should findings be structured for the orchestrator to process?
5. **What are the differences in mode selection, blockers, and completion?** — How should automatic mode differ for research compared to implementation?

---

## Research Phases

| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 1 | Workflow Comparison: pch-coder vs pch-researcher | ✅ Complete | January 16, 2026 |
| 2 | Subagent Orchestration Design for Research | ✅ Complete | January 16, 2026 |
| 3 | Research-Specific Considerations and Adaptations | ✅ Complete | January 16, 2026 |
| 4 | Return Format and Document Update Patterns | ✅ Complete | January 16, 2026 |
| 5 | Implementation Recommendations and Template | ✅ Complete | January 16, 2026 |

---

## Phase 1: Workflow Comparison: pch-coder vs pch-researcher

**Status:** ✅ Complete
**Scope:** Analyze both pch-coder and pch-researcher workflows in detail. Identify structural similarities and differences. Map pch-coder concepts to pch-researcher equivalents. Understand what made beta-pch-coder's automatic mode effective and what can transfer directly.

### Research Tasks

- ✅ Compare YAML frontmatter structure between both agents
- ✅ Compare phase/task structure (implementation phases vs research phases)
- ✅ Compare document update patterns
- ✅ Compare handoff and completion messages
- ✅ Identify reusable patterns from beta-pch-coder
- ✅ Identify pch-researcher-specific patterns that need adaptation

### Findings

#### 1. YAML Frontmatter Comparison

| Field | pch-coder | pch-researcher | Notes |
|-------|-----------|----------------|-------|
| `name` | `pch-coder` | `pch-researcher` | Direct equivalent |
| `description` | Implementation specialist | Research specialist | Different domain |
| `model` | Claude Opus 4.5 | Claude Opus 4.5 | Same |
| `handoffs` | To `pch-planner` | To self (`pch-researcher`) | Different targets |

**Key Observation:** The handoff structure differs significantly:
- **pch-coder** hands off to `pch-planner` when plan updates are needed
- **pch-researcher** hands off to itself for continuing research phases
- **beta-pch-coder** keeps the same handoff but adds the mode selection workflow

#### 2. Phase/Task Structure Comparison

| Aspect | pch-coder (Implementation) | pch-researcher (Research) |
|--------|---------------------------|---------------------------|
| **Work Unit** | Tasks within phases | Research phases |
| **Granularity** | Multiple tasks per phase | One thematic phase at a time |
| **Progress Marker** | Task-level (Step 1.1, 1.2, etc.) | Phase-level (Phase 1, 2, etc.) |
| **Document Type** | Implementation Plan (`/docs/plans/`) | Research Document (`/docs/research/`) |
| **Phase Size** | Based on task complexity | Based on files to analyze (2-15) |

**Structural Parallels:**

```
pch-coder                          pch-researcher
---------                          --------------
Phase N                      ≈     Phase N
  ├── Task N.1               ≈     Scope (what to research)
  ├── Task N.2               ≈     Research Tasks (checklist)
  ├── Task N.3               ≈     Findings (documented output)
  └── Acceptance Criteria    ≈     Key Discoveries
```

#### 3. Document Update Patterns

**pch-coder Document Updates:**
1. Update task status to 🔄 In Progress
2. Execute the task (write code, modify files)
3. Update task status to ✅ Complete
4. Add Implementation Notes section with details
5. Update Progress Tracking table

**pch-researcher Document Updates:**
1. Update phase status to 🔄 In Progress (in phases table)
2. Execute the research (search, read, analyze)
3. Write findings into the phase section
4. List Key Discoveries bullet points
5. List Relevant Files/Resources
6. Update phase status to ✅ Complete

**Pattern Alignment:**
| pch-coder Concept | pch-researcher Equivalent |
|-------------------|---------------------------|
| Task Status Update | Phase Status Update |
| Implementation Notes | Findings Section |
| Deviations from Plan | Gaps/Uncertainties |
| Files Modified/Created | Relevant Files/Resources |
| Acceptance Criteria Met | Key Discoveries Documented |

#### 4. Handoff and Completion Messages

**Manual Mode Session Handoff Messages:**

Both agents have nearly identical handoff message structures:

| Element | pch-coder | pch-researcher |
|---------|-----------|----------------|
| Completion indicator | `✅ Phase [N] Complete — Session End` | `✅ Phase [N] Complete` |
| Summary of completed | Tasks completed list | Key Findings list |
| Files affected | Files Modified/Created | Relevant Files/Resources |
| Next phase preview | Phase [N+1] with task count | Phase [N+1] with phase name |
| Session request | "Start a new chat session" | "Start a new chat session" |
| Why needed | Context capacity explanation | Context capacity explanation |

**Final Completion Messages:**

| Aspect | pch-coder | pch-researcher |
|--------|-----------|----------------|
| All phases complete | "Offer to commit and push" | "Write Overview section, mark complete" |
| Final output | Implementation summary | Research Summary with recommendations |
| Post-completion | Optional git operations | No further action |

#### 5. Reusable Patterns from beta-pch-coder

The following patterns from beta-pch-coder can be **directly adapted** for beta-pch-researcher:

**A. Mode Selection Prompt Pattern**
```
Location: After Core Responsibilities, before workflow
Pattern: Present 2 modes (Manual vs Automatic)
Default: Manual Mode for backward compatibility
Persistence: Session-level only (not saved to document)
```

**B. Prerequisites for Automatic Mode**
```
1. VS Code setting: chat.customAgentInSubagent.enabled: true
2. Document is complete (outline exists with all phases defined)
3. No interactive steps (can't ask clarifying questions during subagent execution)
```

**C. Orchestration Loop Structure**
```
1. Load the document
2. For each phase marked ⏳ Not Started:
   a. Check for special phase types (deployment → research equivalent TBD)
   b. Invoke subagent for the phase
   c. Process subagent result (parse structured return format)
   d. Update document with findings
   e. Display phase report to user
   f. Determine next action (continue, pause, stop)
3. After all phases complete: Display final report
```

**D. Subagent Return Format Concept**
```
beta-pch-coder uses:
- PHASE, STATUS, TASKS_COMPLETED, TASKS_BLOCKED
- FILES_MODIFIED, FILES_CREATED
- DEVIATIONS, NOTES, VERIFICATION

For research, this maps to:
- PHASE, STATUS, FINDINGS_COMPLETE (yes/no)
- KEY_DISCOVERIES, GAPS_FOUND
- FILES_ANALYZED, RESOURCES_REFERENCED
- NOTES, FOLLOW_UP_NEEDED
```

**E. Blocker Handling Pattern**
```
Pause automatic execution when:
- Status is 'partial' or 'blocked'
- Present user with options: Continue, Skip, Stop, Retry
- Document the blocker in the research document
```

**F. Post-Phase Report Structure**
```
Phase completion report with:
- Phase number and name
- Status indicator
- What was completed
- Any issues encountered
- Proceeding to next phase announcement
```

#### 6. pch-researcher-Specific Patterns Needing Adaptation

**A. No Deployment Equivalent (Different)**
- pch-coder has deployment checkpoints (push, CI/CD, etc.)
- pch-researcher has **no equivalent** — research doesn't deploy anything
- **Recommendation:** Remove deployment handling entirely OR replace with "Synthesis Checkpoint" for phases requiring integration

**B. External Source Access (New for Research)**
- pch-researcher explicitly uses: `fetch_webpage`, browser MCP, external docs
- pch-coder doesn't typically access external sources
- **Consideration:** Subagents may need explicit permission/guidance for external fetches

**C. Clarifying Questions (Research Constraint)**
- pch-researcher can ask clarifying questions ONE AT A TIME
- Subagents CANNOT ask questions (no user interaction)
- **Critical Adaptation:** 
  - Pre-flight check before invoking subagent
  - If phase scope is unclear, pause and ask BEFORE subagent invocation
  - Or: Include "if unclear, document uncertainty and continue" instruction in subagent prompt

**D. Findings vs Implementation**
- pch-coder produces code (discrete, verifiable outputs)
- pch-researcher produces findings (text, analysis, synthesis)
- **Document Update Difference:**
  - pch-coder: Subagent modifies files, returns file list
  - pch-researcher: Subagent returns findings text, orchestrator writes to document

**E. Final Overview Synthesis**
- pch-researcher has a special final step: Create Overview section
- This is a synthesis of all phases (not a phase itself)
- **Consideration:** This could be automatic (done by orchestrator after all phases) or a special final subagent invocation

**F. Partial Status (Research-Specific)**
- pch-researcher has `⚠️ Partial` status for incomplete phases
- pch-coder doesn't have a partial task status
- **Include in return format:** `STATUS: success | partial | blocked`

### Key Discoveries

- **Structural similarity is high:** Both agents use phased approaches with document-based persistence, status tracking, and session handoffs
- **Mode selection pattern is directly transferable:** The same Manual/Automatic choice structure works for research
- **Subagent return format needs research-specific fields:** Replace task-based fields with findings-based fields (FINDINGS, DISCOVERIES, GAPS, FILES_ANALYZED)
- **No deployment equivalent for research:** Remove or repurpose deployment checkpoint logic
- **External sources are research-unique:** Need explicit handling for web fetch and browser MCP in subagent prompts
- **Clarifying questions are incompatible with subagents:** Must handle before invocation or instruct subagent to document uncertainty
- **Overview synthesis is a special final step:** Could be handled by orchestrator or as a special subagent invocation

### Relevant Files/Resources

- [agents/pch-coder.md](agents/pch-coder.md) — Original implementation agent (479 lines)
- [agents/pch-researcher.md](agents/pch-researcher.md) — Original research agent (675 lines)
- [agents/beta-pch-coder.md](agents/beta-pch-coder.md) — Automatic mode implementation (961 lines)
- [docs/research/001-subagent-integration-for-pch-coder.md](docs/research/001-subagent-integration-for-pch-coder.md) — Prior research on subagent integration

---

## Phase 2: Subagent Orchestration Design for Research

**Status:** ✅ Complete
**Scope:** Design the subagent orchestration loop for research phases. Determine how the orchestrator should invoke subagents for each research phase. Define the subagent prompt template for research work.

### Research Tasks

- ✅ Adapt beta-pch-coder's orchestration loop for research context
- ✅ Design the subagent prompt template for research phases
- ✅ Determine what context needs to be passed to research subagents
- ✅ Define the orchestration decision points (continue, pause, stop)
- ✅ Consider how to handle multi-phase research dependencies

### Findings

#### 1. Adapted Orchestration Loop for Research

The beta-pch-coder orchestration loop can be adapted for research with the following modifications:

**Original beta-pch-coder Loop:**
```
1. Load the implementation plan document
2. For each phase marked ⏳ Not Started:
   a. Check if phase is a deployment checkpoint → Pause
   b. Invoke subagent for the phase
   c. Process subagent result
   d. Update plan document
   e. Display phase report to user
   f. Determine next action (continue, pause, stop)
3. After all phases complete: Display final report
```

**Adapted beta-pch-researcher Loop:**
```
1. Load the research document
2. For each phase marked ⏳ Not Started:
   a. Check if phase requires clarification → Pause and ask user (NEW)
   b. Check if phase is a synthesis checkpoint → Special handling (NEW)
   c. Invoke subagent for the phase
   d. Process subagent result (parse research-specific format)
   e. Update research document with findings
   f. Display phase report to user
   g. Determine next action (continue, pause, stop)
3. After all phases complete:
   a. Generate Overview section (orchestrator or subagent)
   b. Display final research summary
```

**Key Differences:**

| Aspect | beta-pch-coder | beta-pch-researcher |
|--------|----------------|---------------------|
| **Checkpoint Type** | Deployment checkpoint | Clarification checkpoint |
| **Document Update** | Task statuses + Implementation Notes | Phase findings + Key Discoveries |
| **Special Final Step** | Offer git commit/push | Generate Overview synthesis |
| **Result Format** | Files modified/created | Findings documented |
| **Partial Handling** | Some tasks blocked | Some findings incomplete |

#### 2. Subagent Prompt Template for Research Phases

Designed to parallel the beta-pch-coder template but adapted for research:

````markdown
You are pch-researcher executing Phase {N} of a research document.

**Your Assignment:** Execute ONLY Phase {N}: {Phase Name}
**Research Document Path:** {document_path}

---

## FULL RESEARCH DOCUMENT

{entire_research_document_content}

---

## YOUR INSTRUCTIONS

1. Locate Phase {N} in the Research Phases section above
2. Execute ALL research tasks listed in Phase {N}
3. Use available tools to conduct research:
   - `semantic_search` for codebase exploration
   - `read_file` to analyze specific files
   - `grep_search` for pattern matching
   - `fetch_webpage` for external documentation (if needed)
   - Browser MCP tools for web research (if available)
4. Document your findings comprehensively
5. Identify key discoveries and relevant files
6. Report any gaps or areas requiring follow-up

**IMPORTANT:** Research ONLY Phase {N}. Do not proceed to subsequent phases.

**CRITICAL:** If you encounter areas where user clarification would normally be needed, document the uncertainty and make reasonable assumptions. Note assumptions in your findings.

**Return Format (REQUIRED):**
```
PHASE: {N}
STATUS: success | partial | blocked
SCOPE_COMPLETED: [brief description of what was researched]
FINDINGS: [comprehensive findings text — will be inserted into document]
KEY_DISCOVERIES: [bulleted list of major discoveries]
RELEVANT_FILES: [list of file paths analyzed]
EXTERNAL_SOURCES: [list of URLs/docs referenced, if any]
GAPS_FOUND: [areas that couldn't be fully researched, or "none"]
ASSUMPTIONS_MADE: [any assumptions due to lack of clarification, or "none"]
FOLLOW_UP_NEEDED: [suggestions for additional research, or "none"]
NOTES: [any additional context for the orchestrator]
```
````

#### 3. Context Requirements for Research Subagents

**Required Context (Always Include):**

| Context Element | Purpose | Source |
|-----------------|---------|--------|
| Full research document | Phase scope, objectives, prior findings | Document file |
| Phase number | Which phase to execute | Orchestrator |
| Phase name/topic | What to research | Research document |
| Research scope | Specific scope for this phase | Research document |
| Research tasks checklist | What needs to be done | Research document |

**Optional Context (Include When Relevant):**

| Context Element | When to Include | Purpose |
|-----------------|-----------------|---------|
| Prior phase findings | When phases have dependencies | Inform current research |
| User preferences | If user provided direction | Guide research approach |
| Tool availability | If specific tools needed | Subagent awareness |

**Context Size Considerations:**

For research documents, context requirements are generally smaller than implementation plans because:
- Research documents contain findings text (verbose) rather than code specs
- But findings grow as phases complete
- Recommendation: For documents with 5+ completed phases, summarize prior findings to reduce context

**Summarization Strategy for Large Documents:**
```markdown
## PRIOR RESEARCH SUMMARY (Phases 1-4)

[2-3 sentence summary of key findings from each completed phase]

## FULL CONTENT FOR CURRENT PHASE

[Full Phase N section including scope, tasks, and empty findings]
```

#### 4. Orchestration Decision Points

**Decision Point 1: Pre-Phase Check**

Before invoking subagent, the orchestrator checks:

| Check | Condition | Action |
|-------|-----------|--------|
| **Clarification Needed** | Phase scope mentions "TBD", "unclear", or marked with ❓ | Pause and ask user for clarification |
| **External Access Required** | Phase requires web fetch or browser (noted in scope) | Confirm tools available; proceed or warn |
| **Dependency Check** | Phase depends on prior phase findings | Verify prior phase is ✅ Complete |
| **Synthesis Phase** | Phase is "Final Overview" or "Synthesis" | Use special synthesis handling |

**Decision Point 2: Post-Subagent Processing**

After receiving subagent result:

| Status | Interpretation | Action |
|--------|----------------|--------|
| `success` | All research tasks completed | Update document, proceed to next phase |
| `partial` | Some findings obtained, some gaps | Update document with findings, present user options |
| `blocked` | Could not conduct research | Present blocker details, ask user how to proceed |

**Decision Point 3: Phase Transition**

After updating document, determine next action:

| Condition | Action |
|-----------|--------|
| More phases remain + `success` | Continue to next phase |
| More phases remain + `partial` | Present options: Continue, Stop, Retry |
| More phases remain + `blocked` | Present options: Skip, Stop, Retry |
| All phases complete | Trigger Overview synthesis |

#### 5. Handling Multi-Phase Research Dependencies

Research phases may have dependencies where later phases build on earlier findings.

**Dependency Types:**

| Type | Example | Handling |
|------|---------|----------|
| **Informational** | Phase 3 uses terminology defined in Phase 1 | Include prior findings in subagent context |
| **Discovery-Based** | Phase 2 discovered 5 modules; Phase 3 researches each | Pass module list to Phase 3 subagent prompt |
| **Cumulative** | Each phase adds to understanding | Summarize prior phases for context |

**Dependency Handling Strategy:**

1. **Read prior phase findings** before invoking subagent
2. **Include relevant context** in the subagent prompt:
   ```markdown
   ## RELEVANT PRIOR FINDINGS
   
   From Phase 2:
   - Discovered 5 API modules: UserAPI, AuthAPI, ProductAPI, OrderAPI, NotificationAPI
   - Each module follows the repository pattern
   
   ## YOUR PHASE 3 ASSIGNMENT
   
   Research the UserAPI module in depth...
   ```
3. **Handle cascading failures**: If Phase N finds less than expected, Phase N+1 may need scope adjustment → Present to user

**Dependency Detection:**

Orchestrator should scan phase scope for dependency indicators:
- "Based on Phase N findings..."
- "Using the modules discovered in Phase N..."
- "Following up on..."
- References to prior phase numbers

When detected, orchestrator explicitly includes prior findings in subagent context.

#### 6. User Interaction Points in Automatic Mode

Unlike manual mode (which asks questions one at a time), automatic mode has specific pause points:

| Pause Point | Trigger | User Prompt |
|-------------|---------|-------------|
| **Pre-Research Clarification** | Phase scope is ambiguous | "Phase N scope is unclear. Please clarify: [specific question]" |
| **Partial Findings** | Subagent returns `partial` status | "Phase N completed with gaps. Continue, Retry, or Stop?" |
| **Blocked Phase** | Subagent returns `blocked` status | "Phase N could not proceed. Skip, Retry, or Stop?" |
| **External Access Warning** | Phase needs web access but tool unavailable | "Phase N requires web access. Proceed without, or Stop?" |
| **All Phases Complete** | Final phase finished | "All research complete. Generate Overview now?" |

#### 7. Research-Specific Blocker Handling

Adapted from beta-pch-coder's blocker handling:

```
⚠️ **Phase {N} Completed with Gaps**

**Status:** partial
**Scope Completed:** {what was researched}
**Gaps Found:** {what couldn't be determined}

**Gap Details:**
{gap_information_from_subagent}

---

**How would you like to proceed?**

1️⃣ **Continue to Next Phase**
   Proceed with Phase {N+1}. Gaps are documented for future follow-up.

2️⃣ **Skip to Phase {M}**
   Jump to a specific phase. Say: "Skip to Phase {M}"

3️⃣ **Stop Automatic Research**
   End automation here. Review gaps and restart when ready.

4️⃣ **Retry Phase {N}**
   Re-run with a fresh subagent. Useful if you can provide additional context.

5️⃣ **Provide Clarification** (NEW for research)
   Provide additional context and retry. Say: "Retry with context: [your input]"

**Enter 1, 2, 3, 4, or 5:**
```

**Option 5 (Research-Specific):** Allows user to provide clarifying information that was missing, then retries the phase with that context included in the subagent prompt.

### Key Discoveries

- **Orchestration loop adapts well from beta-pch-coder** — Core structure (load → iterate phases → process results → update document) transfers directly
- **Deployment checkpoints become clarification checkpoints** — Research doesn't deploy, but may need user input before certain phases
- **Subagent prompt template requires research-specific return format** — FINDINGS, KEY_DISCOVERIES, GAPS_FOUND replace FILES_MODIFIED, TASKS_COMPLETED
- **Dependencies between research phases need explicit handling** — Orchestrator must pass relevant prior findings to dependent phases
- **User clarification can happen pre-phase OR be deferred** — Pre-phase pause for ambiguity, or instruct subagent to document assumptions
- **Option 5 "Provide Clarification" is research-specific** — Allows user to add context before retry, unlike implementation which has clearer requirements

### Relevant Files/Resources

- [agents/beta-pch-coder.md](agents/beta-pch-coder.md) — Lines 85-300: Orchestration loop, subagent invocation, result processing
- [docs/research/001-subagent-integration-for-pch-coder.md](docs/research/001-subagent-integration-for-pch-coder.md) — Prior research on subagent patterns
- [agents/pch-researcher.md](agents/pch-researcher.md) — Original research workflow for comparison

---

## Phase 3: Research-Specific Considerations and Adaptations

**Status:** ✅ Complete
**Scope:** Identify aspects unique to research that require special handling. Consider web fetching, external source access, synthesis work, and the iterative nature of research discovery.

### Research Tasks

- ✅ Analyze how external source access (web fetch, browser MCP) works in subagents
- ✅ Determine how to handle research that requires user clarification (since subagents can't ask questions)
- ✅ Consider partial/incomplete phase scenarios unique to research
- ✅ Evaluate synthesis phase handling (final overview creation)
- ✅ Consider research document structure updates during automation

### Findings

#### 1. External Source Access in Subagents

Research uniquely requires access to external sources. From [pch-researcher.md](agents/pch-researcher.md#L545-L575):

**Available External Tools:**
| Tool | Purpose | Subagent Availability |
|------|---------|----------------------|
| `fetch_webpage` | Retrieve documentation, articles, API references | ✅ Available (standard tool) |
| Browser MCP | Navigate and extract information from web pages | ⚠️ Conditional (depends on MCP server configuration) |
| MCP servers | Specialized data access (context-dependent) | ⚠️ Conditional (depends on user setup) |

**Subagent Tool Inheritance:**

When a subagent is invoked via `runSubagent`, it inherits the same tool set as the orchestrator, including:
- All standard VS Code tools (`semantic_search`, `read_file`, `grep_search`, etc.)
- `fetch_webpage` for web access
- Any MCP tools that are configured for the agent

**Key Consideration:** The orchestrator cannot guarantee MCP tool availability for the subagent. The beta-pch-researcher prompt should include:

```markdown
**EXTERNAL SOURCES (if needed):**
- Use `fetch_webpage` for web documentation (always available)
- Browser MCP tools may be available if configured
- If external access fails, document what was attempted and continue with codebase research
- Never block a phase solely due to external source unavailability
```

**Source Priority for Subagents:**

Instruct subagents to follow the same priority order from pch-researcher:
1. Existing codebase (source of truth)
2. Project documentation (README, docs folder)
3. Configuration files
4. Official external documentation (via fetch_webpage)
5. External web resources (if accessible)
6. Community resources (with accuracy verification)

#### 2. Handling Clarifying Questions (Critical Adaptation)

**The Problem:**

In manual mode, pch-researcher explicitly asks clarifying questions ONE AT A TIME:

> *"If the research scope is unclear or you need user guidance, ask one specific question. Wait for the user's response before proceeding."* — [pch-researcher.md, line 293](agents/pch-researcher.md#L293)

Subagents **cannot ask questions** — they run to completion without user interaction.

**Adaptation Strategies:**

| Strategy | When to Use | Implementation |
|----------|-------------|----------------|
| **Pre-flight Clarification** | Scope contains "TBD", "unclear", or ambiguous language | Orchestrator pauses BEFORE subagent invocation; asks user; includes answer in subagent prompt |
| **Documented Assumptions** | Minor ambiguity that subagent can work around | Subagent documents assumptions in `ASSUMPTIONS_MADE` return field |
| **Partial Completion** | Significant ambiguity blocking progress | Subagent returns `partial` status with gaps documented; orchestrator presents to user |

**Pre-flight Clarification Check:**

Before invoking a subagent, the orchestrator should scan the phase scope for:
- Explicit markers: `❓`, `TBD`, `unclear`, `needs clarification`
- Implicit indicators: vague scope ("research the system"), missing specifics
- User-dependent decisions: "decide between X and Y approach"

**Orchestrator Pre-flight Prompt:**

```
⏸️ **Clarification Needed Before Phase {N}**

The scope for Phase {N} requires clarification:

**Phase:** {Phase Name}
**Scope:** {Phase Scope}
**Question:** {Specific question extracted or generated}

Please provide direction before I proceed.

After you answer, say: "Continue with Phase {N}"
```

**Subagent Instruction for Assumptions:**

```markdown
**HANDLING AMBIGUITY:**
- If scope is slightly unclear, make reasonable assumptions and document them
- Prefix assumptions with: "ASSUMED: ..."
- Include all assumptions in the ASSUMPTIONS_MADE return field
- If ambiguity significantly blocks research, return STATUS: partial with GAPS_FOUND explaining what couldn't be researched
```

#### 3. Partial/Incomplete Phase Scenarios

Research is inherently more prone to partial completion than implementation:

| Scenario | Cause | Handling |
|----------|-------|----------|
| **Missing source files** | Expected code doesn't exist | Document gap; suggest where to look; STATUS: partial |
| **Unclear patterns** | Inconsistent or unusual implementations | Document what was found; note ambiguity; STATUS: success (with notes) |
| **External source unreachable** | Web fetch fails, MCP unavailable | Fall back to codebase; note external source gap; STATUS: success |
| **Scope too broad** | Phase covers too much for context | Complete what fits; document remaining scope; STATUS: partial |
| **Conflicting information** | Codebase contradicts documentation | Document conflict; flag for resolution; STATUS: success (with notes) |

**Partial Status (`⚠️ Partial`) in Research:**

Unlike implementation where tasks are discrete (done or not done), research findings exist on a spectrum:

```
Comprehensive ← ─ ─ ─ ─ ─ ─ ─ → Incomplete
    ✅                ⚠️ Partial              ❌ Blocked
```

**When to use each status:**

| Status | Criteria | Next Action |
|--------|----------|-------------|
| `success` | All research tasks addressed; findings documented | Proceed to next phase |
| `partial` | Some tasks completed; significant gaps remain | Present options; may proceed or retry |
| `blocked` | Cannot make meaningful progress | Must resolve before continuing |

**Partial Findings Template:**

```markdown
**Status:** ⚠️ Partial
**Completed Research:**
- [What was successfully researched]

**Gaps Found:**
- [What couldn't be determined and why]

**Assumptions Made:**
- ASSUMED: [assumption 1 and reasoning]
- ASSUMED: [assumption 2 and reasoning]

**Follow-up Recommendations:**
- [Suggestion for filling gaps in future research or manual investigation]
```

#### 4. Synthesis Phase Handling (Final Overview)

pch-researcher has a unique final step: **creating the Overview section** after all phases complete.

> *"Create a comprehensive overview at the top of the document that synthesizes key findings across all phases, highlights the most important discoveries, provides actionable conclusions or recommendations, identifies any gaps or areas for future research."* — [pch-researcher.md, lines 335-340](agents/pch-researcher.md#L335)

**Options for Automation:**

| Approach | Pros | Cons |
|----------|------|------|
| **Orchestrator generates Overview** | Full context of all phases; consistent formatting | May exceed context if document is large |
| **Final subagent for synthesis** | Fresh context for synthesis work | Requires embedding entire document with all findings |
| **Hybrid: Orchestrator with summarization** | Can summarize phases if too large | More complex logic |

**Recommended Approach: Orchestrator Generates Overview**

After the final phase completes successfully:

1. **Read the complete research document** (all phase findings)
2. **Generate the Overview section** by synthesizing:
   - Key findings across all phases
   - Most important discoveries (pick top 3-5)
   - Actionable conclusions/recommendations
   - Remaining gaps or future research areas
3. **Update document:** Replace the Overview placeholder with synthesized content
4. **Update document:** Mark Status as ✅ Complete
5. **Display final research summary** to user

**If document is too large for orchestrator context:**

Use a synthesis subagent with a condensed prompt:

```markdown
You are pch-researcher completing the final synthesis for a research document.

**Document Path:** {path}
**Total Phases:** {N}

## PHASE SUMMARIES

{For each phase, include only: Phase name, Key Discoveries section, 2-3 sentence summary}

## YOUR TASK

Generate a comprehensive Overview section that:
1. Synthesizes key findings across all {N} phases
2. Highlights the 3-5 most important discoveries
3. Provides actionable conclusions
4. Notes any remaining gaps

**Return Format:**
```
OVERVIEW_CONTENT: [The markdown content for the Overview section]
CONCLUSIONS_CONTENT: [The markdown content for the Conclusions section]
KEY_DISCOVERIES: [Top 5 discoveries as a bulleted list]
RECOMMENDATIONS: [Top 3-5 recommendations]
GAPS_REMAINING: [Any gaps or future research needs, or "none"]
```
```

#### 5. Document Structure Updates During Automation

**What Needs to Be Updated:**

| Document Section | Update Trigger | Updated By |
|------------------|----------------|------------|
| Document Information → Status | Phase completion, all phases done | Orchestrator |
| Document Information → Current Phase | Phase transition | Orchestrator |
| Research Phases table (status) | Phase start/completion | Orchestrator |
| Phase N → Status line | Phase start/completion | Orchestrator |
| Phase N → Findings | Research complete | Subagent result (content) → Orchestrator (writes) |
| Phase N → Key Discoveries | Research complete | Subagent result → Orchestrator |
| Phase N → Relevant Files | Research complete | Subagent result → Orchestrator |
| Overview section | All phases complete | Orchestrator |
| Conclusions section | All phases complete | Orchestrator |

**Update Sequence for Each Phase:**

```
1. BEFORE subagent invocation:
   - Update Phase N status to 🔄 In Progress
   - Update Current Phase to N
   - Save document

2. AFTER subagent completion:
   - Parse subagent return format
   - Write Findings content to Phase N section
   - Write Key Discoveries bullets
   - Write Relevant Files list
   - Update Phase N status to ✅ Complete (or ⚠️ Partial)
   - Update Research Phases table status
   - Save document
```

**Document Integrity Rules:**

- Orchestrator owns all document updates (subagent only returns content)
- Single writer prevents formatting conflicts
- Each update is saved before proceeding
- Status updates are atomic (always reflect true state)

**Handling Large Research Documents:**

As findings accumulate, the document grows. For 5+ completed phases:

1. **Read selectively:** Only read phases relevant to current work
2. **Summarize in subagent prompts:** Condense prior findings to key points
3. **Keep Overview placeholder compact:** Don't expand until final synthesis
4. **Archive verbose findings:** Consider summary + detailed subsections

#### 6. Iterative Research Discovery

Research often reveals new questions or areas to explore. Unlike implementation (where scope is fixed by the plan), research may need scope adjustment.

**Handling Scope Expansion:**

| Scenario | Manual Mode | Automatic Mode |
|----------|-------------|----------------|
| Phase finds more than expected | Document findings; note for user | Include in findings; note in NOTES field |
| Phase suggests new research areas | Can ask user if should expand | Document suggestion; continue with original scope |
| Prior phase assumptions proven wrong | Can backtrack and adjust | Return `partial` with reason; user decides |

**Automatic Mode Principle:** Stay within defined scope; document suggestions for expansion but don't expand automatically.

**Subagent Instruction:**

```markdown
**SCOPE MANAGEMENT:**
- Research ONLY what is defined in Phase {N} scope
- If you discover topics outside scope, note them in NOTES field as "Suggested additional research: ..."
- Do NOT expand research beyond the defined scope
- If prior findings affect this phase, reference them but stay on topic
```

### Key Discoveries

- **External tools (fetch_webpage, MCP) are available to subagents** — Subagents inherit the orchestrator's tool set, but MCP availability depends on user configuration
- **Clarifying questions require pre-flight checks** — Orchestrator must scan for ambiguity markers before invocation and pause if needed; subagents document assumptions instead of asking
- **Partial status is more common in research than implementation** — Research findings exist on a spectrum; `⚠️ Partial` is a valid, usable outcome that should be handled gracefully
- **Overview synthesis should be done by the orchestrator** — After all phases complete, orchestrator has full context to synthesize; fallback to synthesis subagent for very large documents
- **Orchestrator owns all document updates** — Subagents return structured content; orchestrator writes to document to ensure consistency and prevent conflicts
- **Scope expansion should be noted but not executed** — Automatic mode maintains defined scope; unexpected findings are documented for user review

### Relevant Files/Resources

- [agents/pch-researcher.md](agents/pch-researcher.md) — Lines 293-300 (clarifying questions), Lines 545-575 (external sources), Lines 590-608 (incomplete phases)
- [agents/beta-pch-coder.md](agents/beta-pch-coder.md) — Lines 246-290 (subagent result processing), Lines 400-510 (blocker handling)

---

## Phase 4: Return Format and Document Update Patterns

**Status:** ✅ Complete
**Scope:** Define the structured return format for research subagents. Design how the orchestrator processes results and updates the research document.

### Research Tasks

- ✅ Design return format for research subagents (STATUS, FINDINGS, DISCOVERIES, etc.)
- ✅ Define how findings are written to the research document
- ✅ Define how phase status updates work
- ✅ Consider handling of partial findings vs complete phases
- ✅ Design error/blocker reporting for research phases

### Findings

#### 1. Subagent Return Format Design

Based on the beta-pch-coder return format and adapted for research-specific needs:

**beta-pch-coder Return Format (for reference):**
```
PHASE: {N}
STATUS: success | partial | blocked
TASKS_COMPLETED: [list of task IDs]
TASKS_BLOCKED: [list of task IDs with reasons]
FILES_MODIFIED: [list of file paths]
FILES_CREATED: [list of file paths]
DEVIATIONS: [changes from plan, or "none"]
NOTES: [implementation details]
VERIFICATION: [how to verify]
```

**Proposed beta-pch-researcher Return Format:**

```
PHASE: {N}
PHASE_NAME: {Phase Name}
STATUS: success | partial | blocked

SCOPE_COMPLETED: [Brief description of research scope covered]

FINDINGS:
[Comprehensive research findings in markdown format. This content
will be inserted into the phase's Findings section verbatim.
Include subsections with headers (####), code examples, tables,
and analysis as appropriate for the research topic.]

KEY_DISCOVERIES:
- [Major discovery 1 — concise, actionable insight]
- [Major discovery 2 — concise, actionable insight]
- [Major discovery 3 — concise, actionable insight]

FILES_ANALYZED:
- [path/to/file1.ext] — [brief note on relevance]
- [path/to/file2.ext] — [brief note on relevance]

EXTERNAL_SOURCES:
- [URL or documentation reference] — [what was learned]
- "none" if no external sources were consulted

GAPS_FOUND:
- [Gap 1: what couldn't be determined and why]
- [Gap 2: what couldn't be determined and why]
- "none" if research was fully complete

ASSUMPTIONS_MADE:
- ASSUMED: [assumption] — BECAUSE: [reasoning]
- "none" if no assumptions were necessary

FOLLOW_UP_NEEDED:
- [Suggested additional research topic 1]
- [Suggested additional research topic 2]
- "none" if no follow-up needed

NOTES:
[Any additional context for the orchestrator, such as:
- Unexpected discoveries outside scope
- Suggestions for scope refinement
- Observations about document structure
- Warnings about conflicting information found]
```

#### 2. Field-by-Field Specification

| Field | Required | Type | Purpose |
|-------|----------|------|---------|
| `PHASE` | Yes | Integer | Phase number for verification |
| `PHASE_NAME` | Yes | String | Human-readable phase identifier |
| `STATUS` | Yes | Enum | `success`, `partial`, or `blocked` |
| `SCOPE_COMPLETED` | Yes | String | Brief description of work done |
| `FINDINGS` | Yes | Markdown | Full findings content for document |
| `KEY_DISCOVERIES` | Yes | List | Bulleted major insights (3-7 items) |
| `FILES_ANALYZED` | Yes | List | File paths with notes |
| `EXTERNAL_SOURCES` | No | List | URLs/docs referenced, or "none" |
| `GAPS_FOUND` | No | List | Incomplete areas, or "none" |
| `ASSUMPTIONS_MADE` | No | List | Documented assumptions, or "none" |
| `FOLLOW_UP_NEEDED` | No | List | Future research suggestions, or "none" |
| `NOTES` | No | String | Free-form orchestrator context |

#### 3. Status Interpretation for Research

Unlike implementation where tasks are binary (done/not done), research has nuanced completion states:

| Status | Criteria | Orchestrator Action |
|--------|----------|---------------------|
| `success` | All scope items researched; findings comprehensive; no critical gaps | Update document; proceed to next phase |
| `partial` | Some scope items researched; significant gaps remain; findings are usable but incomplete | Update document with findings; present user with options |
| `blocked` | Cannot make meaningful progress; fundamental barrier encountered | Document blocker; require user intervention |

**Detailed Status Conditions:**

**`success` — All of these must be true:**
- All research tasks in the phase scope were addressed
- `KEY_DISCOVERIES` contains at least 2 substantive insights
- `GAPS_FOUND` is "none" or contains only minor gaps
- `FINDINGS` section is comprehensive and well-structured

**`partial` — Any of these conditions:**
- Some scope items could not be researched (e.g., files don't exist)
- `GAPS_FOUND` contains significant unresolved questions
- External sources were needed but unavailable
- Prior phase findings were required but insufficient
- `FINDINGS` is incomplete but usable

**`blocked` — Any of these conditions:**
- Fundamental ambiguity prevents any meaningful research
- Required access (files, tools, external sources) is unavailable
- Scope definition is unclear or contradictory
- Prerequisites from prior phases are missing or wrong

#### 4. Document Update Patterns

The orchestrator is responsible for ALL document modifications. Subagents return content; orchestrator writes.

**Update Sequence (per phase):**

```
┌─────────────────────────────────────────────────────────────┐
│ BEFORE SUBAGENT INVOCATION                                  │
├─────────────────────────────────────────────────────────────┤
│ 1. Update Document Information → Current Phase: N           │
│ 2. Update Research Phases table → Phase N: 🔄 In Progress   │
│ 3. Update Phase N section → Status: 🔄 In Progress          │
│ 4. SAVE document to disk                                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                    [Invoke Subagent]
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│ AFTER SUBAGENT COMPLETION                                   │
├─────────────────────────────────────────────────────────────┤
│ 1. Parse return format fields                                │
│ 2. Write FINDINGS content to Phase N → Findings section     │
│ 3. Write KEY_DISCOVERIES to Phase N → Key Discoveries        │
│ 4. Write FILES_ANALYZED + EXTERNAL_SOURCES to                │
│    Phase N → Relevant Files/Resources                        │
│ 5. If GAPS_FOUND ≠ "none": Add "Gaps and Limitations"        │
│    subsection to Phase N                                     │
│ 6. If ASSUMPTIONS_MADE ≠ "none": Add "Assumptions"           │
│    subsection to Phase N                                     │
│ 7. Update Phase N section → Status: ✅ Complete              │
│    (or ⚠️ Partial if status was partial)                     │
│ 8. Update Research Phases table → Phase N: ✅ Complete       │
│    (with session date)                                       │
│ 9. SAVE document to disk                                     │
└─────────────────────────────────────────────────────────────┘
```

#### 5. Document Section Mapping

How subagent return fields map to research document sections:

| Return Field | Document Location | Format |
|--------------|-------------------|--------|
| `FINDINGS` | `## Phase N` → `### Findings` | Verbatim markdown insertion |
| `KEY_DISCOVERIES` | `## Phase N` → `### Key Discoveries` | Bulleted list |
| `FILES_ANALYZED` | `## Phase N` → `### Relevant Files/Resources` | Bulleted list with notes |
| `EXTERNAL_SOURCES` | `## Phase N` → `### Relevant Files/Resources` | Appended after files |
| `GAPS_FOUND` | `## Phase N` → `### Gaps and Limitations` (new) | Bulleted list |
| `ASSUMPTIONS_MADE` | `## Phase N` → `### Assumptions` (new) | Bulleted list with reasoning |
| `FOLLOW_UP_NEEDED` | `## Phase N` → `### Follow-up Research` (new) | Bulleted list |
| `NOTES` | Not written to document | Orchestrator internal use only |

**Example Document Update:**

Before subagent:
```markdown
## Phase 3: Authentication Patterns

**Status:** 🔄 In Progress
**Scope:** Research authentication implementations across the codebase

### Findings

[Research findings will be documented here]

### Key Discoveries

- [Discovery 1]
- [Discovery 2]

### Relevant Files/Resources

- [File or resource references]
```

After subagent (orchestrator writes):
```markdown
## Phase 3: Authentication Patterns

**Status:** ✅ Complete
**Scope:** Research authentication implementations across the codebase

### Findings

The authentication system uses a multi-strategy approach with JWT tokens
as the primary mechanism. The implementation spans three main modules:

#### Core Authentication Flow

1. User credentials are validated against the UserService
2. JwtService generates access and refresh tokens
3. Tokens are stored in HTTP-only cookies with secure flags

[... additional findings content from subagent ...]

### Key Discoveries

- JWT tokens use RS256 algorithm with rotating keys stored in `/config/keys/`
- Session management is stateless by default but can opt-in to Redis sessions
- Password hashing uses bcrypt with configurable work factor (currently 12)

### Relevant Files/Resources

- [src/auth/auth.service.ts](src/auth/auth.service.ts) — Core authentication logic
- [src/auth/strategies/jwt.strategy.ts](src/auth/strategies/jwt.strategy.ts) — Passport JWT configuration
- [src/config/auth.config.ts](src/config/auth.config.ts) — Authentication settings
- https://jwt.io/introduction — JWT standard reference

### Gaps and Limitations

- OAuth2/OIDC integration patterns were not found in current codebase
- Rate limiting for auth endpoints was not researched (outside scope)
```

#### 6. Handling Partial Findings

When subagent returns `STATUS: partial`, the orchestrator must:

1. **Write what was found** — Document available findings
2. **Clearly mark as partial** — Use `⚠️ Partial` status
3. **Document gaps** — Include GAPS_FOUND in the document
4. **Present user options** — See Phase 2 findings for option menu

**Partial Status in Document:**

```markdown
## Phase N: {Topic}

**Status:** ⚠️ Partial
**Scope:** {Original scope}

### Findings

{Partial findings that were obtained}

### Key Discoveries

- {Discoveries made, even if incomplete}

### Gaps and Limitations

- {Gap 1 — what couldn't be determined}
- {Gap 2 — why it couldn't be determined}

### Relevant Files/Resources

{Files that were analyzed, even if more were expected}
```

**Research Phases Table for Partial:**

```markdown
| Phase | Topic | Status | Session |
|-------|-------|--------|---------|
| 3 | Authentication Patterns | ⚠️ Partial | January 16, 2026 |
```

#### 7. Error and Blocker Reporting

When research cannot proceed, the return format should include detailed blocker information:

**Blocked Return Format:**

```
PHASE: 4
PHASE_NAME: API Integration Analysis
STATUS: blocked

SCOPE_COMPLETED: Unable to begin research

FINDINGS:
Research could not be conducted for this phase.

KEY_DISCOVERIES:
- none

FILES_ANALYZED:
- none

EXTERNAL_SOURCES:
- none

GAPS_FOUND:
- The expected API module at `/src/api/` does not exist
- No alternative API implementation patterns were found
- This phase depends on Phase 3 findings which are incomplete

ASSUMPTIONS_MADE:
- none

FOLLOW_UP_NEEDED:
- Verify correct directory structure with project owner
- Determine if API functionality exists under different path
- Consider if this research topic is still relevant

NOTES:
BLOCKER: Cannot proceed without access to API implementation.
The project structure differs significantly from expected.
Recommend reviewing Phase 3 gaps before retrying this phase.
```

**Orchestrator Blocker Handling:**

1. Parse `STATUS: blocked`
2. Do NOT update Findings (nothing substantive to add)
3. Update document with blocker information:
   ```markdown
   ## Phase 4: API Integration Analysis
   
   **Status:** ❌ Blocked
   **Scope:** {Original scope}
   
   ### Blocker Information
   
   **Issue:** Cannot access required codebase areas
   **Details:**
   - The expected API module at `/src/api/` does not exist
   - No alternative API implementation patterns were found
   
   **Impact:** Phase cannot proceed until resolved
   
   **Suggested Resolution:**
   - Verify correct directory structure with project owner
   - Determine if API functionality exists under different path
   ```
4. Present blocker options to user (Continue to next, Skip, Stop, Retry)

#### 8. NOTES Field Usage

The `NOTES` field is for orchestrator-only information that should NOT be written to the document:

**Appropriate NOTES content:**
- Suggestions for orchestrator action: "Consider condensing prior phases for Phase 5"
- Warnings: "Conflicting information found; findings reflect majority pattern"
- Meta-observations: "This phase took longer due to scattered implementations"
- Scope feedback: "Scope was too broad; recommend splitting in future research"

**NOTES parsing by orchestrator:**
- Check for `BLOCKER:` prefix → Treat as critical, present to user
- Check for `WARNING:` prefix → Display to user before continuing
- Check for `SUGGESTION:` prefix → Consider for future phases
- Otherwise → Log internally, do not display

#### 9. Comparison with beta-pch-coder Patterns

| Aspect | beta-pch-coder | beta-pch-researcher |
|--------|----------------|---------------------|
| **Work unit** | Task (discrete) | Phase (thematic) |
| **Output type** | Files modified/created | Findings documented |
| **Completion clarity** | Binary (done/blocked) | Spectrum (complete → partial → blocked) |
| **Content field** | NOTES (brief) | FINDINGS (comprehensive markdown) |
| **Discovery tracking** | Implicit in files | Explicit KEY_DISCOVERIES field |
| **Gap handling** | Not applicable | GAPS_FOUND required field |
| **Verification** | VERIFICATION field | Not applicable (no execution) |
| **External sources** | Rare | EXTERNAL_SOURCES explicit |

### Key Discoveries

- **Return format needs research-specific fields** — FINDINGS (markdown content), KEY_DISCOVERIES (insights), GAPS_FOUND (incomplete areas), and EXTERNAL_SOURCES are unique to research workflow
- **Findings are inserted verbatim** — The FINDINGS field should be well-structured markdown that the orchestrator inserts directly into the document
- **Three-level status is essential** — `success`/`partial`/`blocked` allows nuanced handling of research completion states
- **NOTES field is orchestrator-private** — Use for meta-information, warnings, and suggestions; never write to document
- **Partial status is a valid outcome** — Unlike implementation, research commonly produces partial results that are still valuable
- **Document update sequence is strict** — Mark in-progress before invocation, update findings after, save at each step

### Relevant Files/Resources

- [agents/beta-pch-coder.md](agents/beta-pch-coder.md#L220-L280) — Return format parsing and document update logic
- [agents/pch-researcher.md](agents/pch-researcher.md#L350-L430) — Research document structure template
- [docs/research/001-subagent-integration-for-pch-coder.md](docs/research/001-subagent-integration-for-pch-coder.md#L550-L600) — Prior research on document ownership patterns

---

## Phase 5: Implementation Recommendations and Template

**Status:** ✅ Complete
**Scope:** Synthesize all findings into concrete recommendations. Produce a template/outline for the beta-pch-researcher agent prompt. Identify required changes and additions.

### Research Tasks

- ✅ Create beta-pch-researcher agent prompt outline
- ✅ Document required YAML frontmatter changes
- ✅ Define mode selection prompt for research
- ✅ Define automatic mode workflow section
- ✅ Define manual mode (existing behavior) preservation
- ✅ Create comparison table of changes from pch-researcher
- ✅ Identify implementation effort and risks

### Findings

#### 1. beta-pch-researcher Agent Prompt Outline

The beta-pch-researcher agent should be structured as follows (mirroring beta-pch-coder's organization):

```
1. YAML Frontmatter
2. Agent Introduction (identity and purpose)
3. 🚨 CRITICAL RULES - READ FIRST 🚨 (preserved from pch-researcher)
4. Core Responsibilities
5. Execution Mode Selection (NEW)
   - Mode Selection Prompt
   - Mode Selection Logic
   - Choosing Between Modes (table)
6. Prerequisites for Automatic Mode (NEW)
7. Automatic Mode Workflow (NEW)
   - Orchestration Loop
   - Subagent Invocation
   - Processing Subagent Results
   - Updating the Research Document
   - Post-Phase Report
   - Final Completion Report (with Overview synthesis)
   - Handling Blockers in Automatic Mode
   - Clarification Pause Handling (research-specific)
   - Subagent Invocation Failure
8. One Phase Per Session (Manual Mode)
   - Why This Matters
   - (Existing pch-researcher workflow preserved)
9. Workflow Process (adapted for both modes)
   - Phase 0: Create Initial Research Document
   - Phase 1-N: Execute Research Phases
   - Final Phase: Create Overview
10. Research Document Structure (unchanged)
11. Phase Sizing Guidelines (unchanged)
12. Session Handoff Messages (updated for mode awareness)
13. Research Best Practices (unchanged)
14. Handling Incomplete Phases (unchanged)
15. Progress Tracking (unchanged)
16. Quality Standards (unchanged)
```

**Key Structural Changes from pch-researcher:**

| Section | pch-researcher | beta-pch-researcher |
|---------|----------------|---------------------|
| Line ~45 | Core Responsibilities | Core Responsibilities (unchanged) |
| Line ~50 | (none) | **NEW: Execution Mode Selection** |
| Line ~85 | (none) | **NEW: Prerequisites for Automatic Mode** |
| Line ~100 | (none) | **NEW: Automatic Mode Workflow** (~250 lines) |
| Line ~350 | Workflow Process | Workflow Process (minor updates for mode context) |
| Line ~450 | Session Handoff Messages | Session Handoff Messages (mode-aware) |

#### 2. Required YAML Frontmatter Changes

**Current pch-researcher frontmatter:**
```yaml
---
name: pch-researcher
description: Researches and outlines multi-step plans
model: Claude Opus 4.5
handoffs:
  - label: Continue researching
    agent: pch-researcher
    prompt: Researches and outlines multi-step plans
    send: false
---
```

**Proposed beta-pch-researcher frontmatter:**
```yaml
---
name: beta-pch-researcher
description: Conducts comprehensive, phased research with optional automatic subagent orchestration
model: Claude Opus 4.5
handoffs:
  - label: Continue researching
    agent: beta-pch-researcher
    prompt: Continue research with phased approach
    send: false
---
```

**Changes:**
| Field | Change | Reason |
|-------|--------|--------|
| `name` | `pch-researcher` → `beta-pch-researcher` | New agent identity |
| `description` | Updated to mention subagent orchestration | Reflects new capability |
| `handoffs[0].agent` | `pch-researcher` → `beta-pch-researcher` | Self-reference updated |
| `handoffs[0].prompt` | Simplified | Cleaner handoff text |

**Note:** No new handoffs are needed. Unlike beta-pch-coder (which hands off to pch-planner), the researcher is self-contained.

#### 3. Mode Selection Prompt for Research

```markdown
## Execution Mode Selection

Before beginning research execution, ask the user which execution mode they prefer.

### Mode Selection Prompt

At the start of research execution (after loading an existing document), present this choice:

\`\`\`
🔬 **Research Execution Mode Selection**

I'm ready to continue research at \`[document path]\`.

**How would you like to proceed?**

1️⃣ **Manual Mode** (Recommended for exploratory research)
   I'll execute one phase at a time. After each phase, you'll start a new session to continue.
   This allows you to review findings and adjust direction between phases.

2️⃣ **Automatic Mode** (For well-defined research)
   I'll orchestrate all phases automatically using isolated subagents.
   Each phase runs in fresh context. I'll report findings after each phase completes.
   ⚠️ Requires VS Code setting: \`chat.customAgentInSubagent.enabled: true\`
   ⚠️ Cannot ask clarifying questions during execution

**Enter 1 or 2** (or say "manual" / "automatic"):
\`\`\`

### Mode Selection Logic

After the user responds:

- **If user chooses 1 or "manual":** Proceed with the standard one-phase-per-session behavior
- **If user chooses 2 or "automatic":** Proceed to Automatic Mode Workflow
- **If response is unclear:** Default to **Manual Mode** (preserves exploratory research capability)

### Choosing Between Modes

| Scenario | Recommended Mode | Reason |
|----------|------------------|--------|
| Exploratory research (unknown scope) | Manual | May need to adjust phases based on findings |
| Well-structured research plan | Automatic | Phases are defined; can run without intervention |
| Research needing external sources | Manual or Automatic | Both support fetch_webpage; browser MCP availability varies |
| Research with unclear phase scopes | Manual | Can ask clarifying questions |
| Time-constrained research | Automatic | Reduces session overhead |
| Learning a new codebase | Manual | See each phase in detail; absorb findings |
```

#### 4. Automatic Mode Workflow Section

The following section should be added after "Prerequisites for Automatic Mode":

````markdown
## Automatic Mode Workflow

When the user selects Automatic Mode (option 2), follow this orchestration workflow.

### Orchestration Loop

Execute research phases automatically using subagent delegation:

1. **Load the research document**
   - Read the full research document
   - Parse all phases and their current status
   - Identify phases marked as ⏳ Not Started

2. **For each phase marked ⏳ Not Started:**

   a. **Check if phase requires clarification** (research-specific)
      - Scan phase scope for: "TBD", "unclear", "❓", vague language
      - If YES: Pause and ask user for clarification before invoking subagent
      - If NO: Continue to subagent invocation

   b. **Invoke subagent for the phase**
      - Use the `runSubagent` tool with `agentName: "beta-pch-researcher"`
      - Include the full research document in the prompt
      - Specify which phase to execute
      - Wait for subagent completion

   c. **Process subagent result**
      - Parse the structured return format (STATUS, FINDINGS, DISCOVERIES, etc.)
      - Interpret status: `success`, `partial`, or `blocked`

   d. **Update research document**
      - Write FINDINGS content to the phase section
      - Add KEY_DISCOVERIES bullet points
      - Add RELEVANT_FILES and EXTERNAL_SOURCES
      - Add GAPS_FOUND and ASSUMPTIONS_MADE if applicable
      - Update phase status (✅ Complete or ⚠️ Partial)

   e. **Display phase report to user**
      - Show full phase completion report

   f. **Determine next action**
      - If `success`: Continue to next phase
      - If `partial` or `blocked`: Present user with options

3. **After all phases complete:**
   - Generate the Overview section (orchestrator synthesizes findings)
   - Generate the Conclusions section
   - Mark research as ✅ Complete
   - Display final research summary

### Subagent Invocation

When invoking a subagent for a research phase, use this prompt template:

\`\`\`\`markdown
You are pch-researcher executing Phase {N} of a research document.

**Your Assignment:** Execute ONLY Phase {N}: {Phase Name}
**Research Document Path:** {document_path}

---

## FULL RESEARCH DOCUMENT

{entire_research_document_content}

---

## YOUR INSTRUCTIONS

1. Locate Phase {N} in the Research Phases section above
2. Execute ALL research tasks listed in Phase {N}
3. Use available tools to conduct research:
   - `semantic_search` for codebase exploration
   - `read_file` to analyze specific files
   - `grep_search` for pattern matching
   - `fetch_webpage` for external documentation (if needed)
4. Document your findings comprehensively
5. Identify key discoveries and relevant files
6. Report any gaps or areas requiring follow-up

**IMPORTANT:** Research ONLY Phase {N}. Do not proceed to subsequent phases.

**HANDLING AMBIGUITY:**
- If scope is slightly unclear, make reasonable assumptions and document them
- Prefix assumptions with: "ASSUMED: ..."
- If ambiguity significantly blocks research, return STATUS: partial

**Return Format (REQUIRED):**
\`\`\`
PHASE: {N}
PHASE_NAME: {Phase Name}
STATUS: success | partial | blocked
SCOPE_COMPLETED: [brief description of what was researched]
FINDINGS: [comprehensive findings text — will be inserted into document]
KEY_DISCOVERIES: [bulleted list of major discoveries]
FILES_ANALYZED: [list of file paths with notes]
EXTERNAL_SOURCES: [URLs/docs referenced, or "none"]
GAPS_FOUND: [areas that couldn't be fully researched, or "none"]
ASSUMPTIONS_MADE: [assumptions with reasoning, or "none"]
FOLLOW_UP_NEEDED: [suggestions for additional research, or "none"]
NOTES: [additional context for orchestrator]
\`\`\`
\`\`\`\`

### Pre-Phase Clarification Check (Research-Specific)

Before invoking a subagent, scan the phase scope for ambiguity indicators:

**Ambiguity Triggers:**
- Explicit markers: `❓`, `TBD`, `unclear`, `needs clarification`
- Vague scope: "research the system", "look into X"
- User-dependent decisions: "decide between X and Y"
- Missing specifics: no files, modules, or areas identified

**Clarification Pause Prompt:**

\`\`\`
⏸️ **Clarification Needed Before Phase {N}**

The scope for Phase {N} requires clarification:

**Phase:** {Phase Name}
**Scope:** {Phase Scope}
**Question:** {Specific question}

Please provide direction before I proceed.

After you answer, say: "Continue with Phase {N}"
\`\`\`

### Processing Subagent Results

Parse the structured return format to determine next actions:

| Field | How to Parse | Action |
|-------|--------------|--------|
| `PHASE` | Integer | Verify matches invoked phase |
| `PHASE_NAME` | String | Include in phase report |
| `STATUS` | Enum | Determines flow: continue, pause, or stop |
| `SCOPE_COMPLETED` | String | Brief description for report |
| `FINDINGS` | Markdown | Insert verbatim into phase Findings section |
| `KEY_DISCOVERIES` | List | Add as bullets to Key Discoveries section |
| `FILES_ANALYZED` | List | Add to Relevant Files/Resources section |
| `EXTERNAL_SOURCES` | List | Append to Relevant Files/Resources |
| `GAPS_FOUND` | List | Create Gaps subsection if not "none" |
| `ASSUMPTIONS_MADE` | List | Create Assumptions subsection if not "none" |
| `FOLLOW_UP_NEEDED` | List | Create Follow-up subsection if not "none" |
| `NOTES` | String | Orchestrator internal use only |

**Status Interpretation:**

- **`success`**: All scope items researched; comprehensive findings. Proceed to next phase.
- **`partial`**: Some findings obtained; significant gaps remain. Pause and present options.
- **`blocked`**: Cannot make meaningful progress. Stop and present blocker details.

### Post-Phase Report

After processing results and updating the document:

\`\`\`
✅ **Phase {N} Complete** ({current}/{total} phases)

**Phase:** {N}: {Phase Name}
**Status:** {success | partial | blocked}
**Execution Mode:** Automatic (Subagent)

**Scope Completed:**
{scope_completed_from_subagent}

**Key Discoveries:**
- {discovery_1}
- {discovery_2}
- {discovery_3}

**Files Analyzed:**
- \`{file_path}\` — {note}
- \`{file_path}\` — {note}

**External Sources:** (if any)
- {url} — {what was learned}

**Gaps Found:** (if any)
- {gap_description}

---

➡️ **Proceeding to Phase {N+1}:** {Next Phase Name}...
\`\`\`

### Handling Blockers in Automatic Mode

When subagent returns `partial` or `blocked` status:

\`\`\`
⚠️ **Phase {N} Completed with Issues**

**Status:** {partial | blocked}
**Scope Completed:** {what was researched}
**Gaps Found:** {gaps_from_subagent}

**Issue Details:**
{details_from_subagent}

---

**How would you like to proceed?**

1️⃣ **Continue to Next Phase**
   Proceed with Phase {N+1}. Gaps are documented for future follow-up.

2️⃣ **Skip to Phase {M}**
   Jump to a specific phase. Say: "Skip to Phase {M}"

3️⃣ **Stop Automatic Research**
   End automation here. Review findings and restart when ready.

4️⃣ **Retry Phase {N}**
   Re-run with a fresh subagent.

5️⃣ **Provide Clarification and Retry** (research-specific)
   Add context and retry. Say: "Retry with context: [your input]"

**Enter 1, 2, 3, 4, or 5:**
\`\`\`

### Final Overview Synthesis

After all phases complete, the orchestrator generates the Overview and Conclusions:

1. **Read all phase findings** from the document
2. **Synthesize the Overview section:**
   - Key findings across all phases
   - Top 3-5 most important discoveries
   - Actionable conclusions
   - Remaining gaps or future research areas
3. **Generate Conclusions section:**
   - Answer the Key Questions from Research Objectives
   - Provide recommendations
4. **Update document:**
   - Replace Overview placeholder with synthesized content
   - Replace Conclusions placeholder with findings
   - Update Document Information → Status: ✅ Complete
5. **Display final summary to user**

**Final Completion Report:**

\`\`\`
🎉 **Research Complete**

**Document:** \`{document_path}\`
**Total Phases:** {N}
**Execution Mode:** Automatic (Subagent Orchestration)

**Summary by Phase:**
| Phase | Name | Status | Key Finding |
|-------|------|--------|-------------|
| 1 | {name} | ✅ | {brief finding} |
| 2 | {name} | ✅ | {brief finding} |
| ... | ... | ... | ... |

**Top Discoveries:**
1. {major_discovery_1}
2. {major_discovery_2}
3. {major_discovery_3}

**Recommendations:**
- {recommendation_1}
- {recommendation_2}

**Remaining Gaps:** {count or "None"}

**Research document fully updated at:** \`{document_path}\`

---

✅ **Research is now complete.** The document includes a comprehensive Overview synthesizing all findings.
\`\`\`
````

#### 5. Manual Mode Preservation

The existing pch-researcher behavior should be preserved entirely under a "Manual Mode" context. This requires:

**A. Section Heading Update:**
```markdown
## One Phase Per Session (Manual Mode)

*This section applies when using Manual Mode. For Automatic Mode, see [Automatic Mode Workflow](#automatic-mode-workflow).*
```

**B. Critical Rules Update:**
Add mode context to the critical rules section:

```markdown
## 🚨 CRITICAL RULES - READ FIRST 🚨

**These rules apply to MANUAL MODE. In Automatic Mode, the orchestrator handles session management.**

[existing rules remain unchanged]
```

**C. Workflow Process Update:**
Add mode context at the beginning:

```markdown
## Workflow Process

*The workflow below applies to Manual Mode. In Automatic Mode, phases are executed via subagent orchestration — see [Automatic Mode Workflow](#automatic-mode-workflow).*
```

**D. Session Handoff Messages Update:**
Add conditional note:

```markdown
## Session Handoff Messages

*These messages apply to Manual Mode. In Automatic Mode, the orchestrator displays phase reports instead of handoff messages.*
```

#### 6. Comparison Table: pch-researcher vs beta-pch-researcher

| Aspect | pch-researcher | beta-pch-researcher |
|--------|----------------|---------------------|
| **Agent Name** | `pch-researcher` | `beta-pch-researcher` |
| **Execution Modes** | Manual only | Manual + Automatic |
| **Session Management** | User-initiated new sessions | Manual: User-initiated<br>Automatic: Orchestrator-managed |
| **Subagent Usage** | None | Yes (in automatic mode) |
| **Clarifying Questions** | Can ask one at a time | Manual: Can ask<br>Automatic: Pre-flight check or assumptions |
| **Phase Execution** | One per session | Manual: One per session<br>Automatic: All phases sequentially |
| **Document Updates** | Agent writes directly | Manual: Agent writes<br>Automatic: Orchestrator writes |
| **Overview Synthesis** | Agent does in final session | Manual: Agent<br>Automatic: Orchestrator |
| **Partial Status** | `⚠️ Partial` supported | Same, with blocker options in automatic |
| **External Sources** | fetch_webpage, browser MCP | Same (inherited by subagents) |
| **Return Format** | N/A (no subagents) | Structured format for subagent results |
| **Document Structure** | Unchanged | Unchanged |
| **YAML Handoffs** | To self | To self (updated name) |

**Lines Added (Estimated):**

| Section | Approximate Lines |
|---------|-------------------|
| Execution Mode Selection | ~50 |
| Prerequisites for Automatic Mode | ~25 |
| Automatic Mode Workflow | ~250 |
| Manual Mode context notes | ~20 |
| **Total New Content** | **~345 lines** |

**pch-researcher baseline:** 675 lines  
**Estimated beta-pch-researcher:** ~1020 lines

#### 7. Implementation Effort and Risks

**Implementation Effort:**

| Task | Effort | Complexity |
|------|--------|------------|
| Copy pch-researcher.md to beta-pch-researcher.md | Low | Trivial |
| Update YAML frontmatter | Low | Trivial |
| Add Execution Mode Selection section | Medium | Moderate |
| Add Prerequisites section | Low | Low |
| Add Automatic Mode Workflow section (~250 lines) | High | High |
| Add mode context to existing sections | Low | Low |
| Update session handoff messages | Low | Low |
| Testing and iteration | High | Variable |

**Total Estimated Effort:** 4-6 hours for initial implementation + testing

**Risks and Mitigations:**

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Subagent tool unavailable | Medium | High | Clear error messages; fallback to manual mode |
| Large documents exceed context | Medium | Medium | Summarization strategy for prior phases |
| Subagent doesn't follow return format | Low | Medium | Clear format specification; parsing fallbacks |
| Findings quality degradation | Low | Medium | Same prompt quality as manual mode |
| User confusion about modes | Low | Low | Clear mode selection prompt; default to manual |
| Clarification needs missed | Medium | Medium | Pre-flight checks; assumption documentation |
| External source access failures | Low | Low | Fallback to codebase-only research |

**Critical Success Factors:**

1. **Return format compliance** — Subagents must consistently return the structured format
2. **Document ownership clarity** — Orchestrator must own all document writes
3. **Pre-flight clarification checks** — Must detect ambiguous scopes before invocation
4. **Graceful degradation** — Manual mode must remain fully functional
5. **User communication** — Clear phase reports and progress indicators

**Testing Recommendations:**

1. Test with simple 3-phase research document first
2. Verify return format parsing with success, partial, and blocked statuses
3. Test pre-flight clarification detection
4. Test document update patterns (findings, discoveries, gaps)
5. Test overview synthesis after all phases complete
6. Test blocker handling options (continue, skip, stop, retry, clarify)
7. Test subagent invocation failure and recovery

### Key Discoveries

- **Agent structure mirrors beta-pch-coder closely** — Same mode selection, prerequisites, and orchestration loop structure with research-specific adaptations
- **~345 new lines required** — Automatic mode adds significant content but most is templated from beta-pch-coder
- **No new handoffs needed** — Research workflow is self-contained; no handoff to other agents
- **Pre-flight clarification is research-unique** — Must scan for ambiguity before subagent invocation
- **Overview synthesis by orchestrator** — After all phases, orchestrator generates Overview instead of delegating to subagent
- **Manual mode preserved intact** — Existing pch-researcher behavior remains available; mode selection defaults to manual
- **Implementation risk is moderate** — Subagent reliability and return format compliance are the main concerns

### Relevant Files/Resources

- [agents/beta-pch-coder.md](agents/beta-pch-coder.md) — Template for automatic mode sections
- [agents/pch-researcher.md](agents/pch-researcher.md) — Base agent to extend
- [docs/research/001-subagent-integration-for-pch-coder.md](docs/research/001-subagent-integration-for-pch-coder.md) — Prior subagent research

---

## Conclusions

This research confirms that creating a `beta-pch-researcher` agent with automatic subagent orchestration is both **feasible and well-structured** based on the patterns established in `beta-pch-coder`.

### Answers to Key Questions

1. **What parallels exist between pch-coder and pch-researcher workflows?**
   
   High structural similarity: both use phased approaches, document-based persistence, status tracking (with emoji indicators), and session handoffs. The mode selection pattern, orchestration loop structure, and subagent invocation model transfer directly.

2. **How should subagent orchestration work for research phases?**
   
   The orchestrator loads the research document, iterates through phases marked ⏳ Not Started, invokes subagents with the full document context, parses structured return formats, updates the document with findings, and reports progress. Research adds pre-flight clarification checks before invocation.

3. **What unique considerations exist for research subagents?**
   
   - Cannot ask clarifying questions (must use pre-flight checks or document assumptions)
   - Produce findings (text) rather than file changes (code)
   - Use `partial` status more frequently due to research's exploratory nature
   - Have access to external tools (fetch_webpage, browser MCP) for documentation research
   - Require overview synthesis as a post-completion orchestrator step

4. **What return format should research subagents use?**
   
   A structured format with: PHASE, PHASE_NAME, STATUS, SCOPE_COMPLETED, FINDINGS (markdown), KEY_DISCOVERIES, FILES_ANALYZED, EXTERNAL_SOURCES, GAPS_FOUND, ASSUMPTIONS_MADE, FOLLOW_UP_NEEDED, and NOTES.

5. **What are the differences in mode selection, blockers, and completion?**
   
   - Mode selection defaults to Manual for exploratory research
   - Blocker options include "Provide Clarification and Retry" (research-specific)
   - Completion includes orchestrator-driven Overview synthesis
   - No deployment checkpoints; replaced with clarification checkpoints

### Final Recommendations

| Priority | Recommendation |
|----------|----------------|
| **High** | Create beta-pch-researcher.md using the outlined structure |
| **High** | Implement pre-flight clarification detection before subagent invocation |
| **High** | Design robust return format parsing with fallbacks |
| **Medium** | Test with 3-phase research document before complex scenarios |
| **Medium** | Implement document summarization for large documents (5+ phases) |
| **Low** | Consider synthesis subagent fallback for very large documents |

### Implementation Path

1. Copy `pch-researcher.md` to `beta-pch-researcher.md`
2. Update YAML frontmatter (name, description, handoff agent reference)
3. Add mode selection section after Core Responsibilities
4. Add prerequisites section
5. Add Automatic Mode Workflow section (~250 lines)
6. Add mode context notes to existing sections
7. Test with simple research document
8. Iterate based on subagent behavior

The estimated implementation effort is **4-6 hours** with moderate complexity. The primary risks are subagent tool availability and return format compliance, both mitigatable with clear error messages and manual mode fallback.

---

## Handoff Information

**Created By:** pch-researcher
**Created Date:** January 16, 2026
**Status:** ✅ Complete
**Current Phase:** Complete (5 of 5)
**Document Location:** `/docs/research/002-beta-pch-researcher-subagent-integration.md`
