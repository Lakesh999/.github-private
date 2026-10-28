# Implementation Plan: beta-pch-researcher Agent Prompt

## Document Information

| Field | Value |
|-------|-------|
| Document | 002-beta-pch-researcher-agent.md |
| Created | January 16, 2026 |
| Status | ✅ Implementation Complete |
| Owner | pch-planner |
| Version | v1.1 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | January 16, 2026 | pch-planner | Initial plan creation |
| v1.1 | January 16, 2026 | pch-plan-reviewer | Review updates: clarified subagent invocation, blocker options, return format, clarification triggers |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | January 16, 2026 | Initial Context | Research document 002 provided | Comprehensive research already completed |

---

## Review Session Log

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Subagent agentName for invocation | Completeness | Option A: Use `agentName: "beta-pch-researcher"` | Added to Phase 3.3 acceptance criteria |
| 2 | 5-option blocker menu specifics | Clarity | Option A: Continue, Skip, Stop, Retry, Provide Clarification | Added to Phase 3.7 acceptance criteria |
| 3 | Return format field name (FILES_ANALYZED vs RELEVANT_FILES) | Correctness | Option B: Use `RELEVANT_FILES` | Updated Subagent Return Format section |
| 4 | Pre-flight clarification triggers | Specificity | Option B: Text markers + structural checks | Added to Phase 3.2 acceptance criteria |

---

## Overview

Create a new `beta-pch-researcher.md` agent that extends the existing `pch-researcher.md` with automatic subagent orchestration capability. This follows the pattern established by `beta-pch-coder.md`, allowing users to choose between Manual Mode (existing behavior) and Automatic Mode (subagent-driven phase execution).

### Objectives

1. **Preserve existing functionality** — Manual mode must work identically to current `pch-researcher.md`
2. **Add automatic mode** — Enable subagent orchestration for research phases
3. **Handle research-specific needs** — Implement clarification checks, partial findings handling, and overview synthesis
4. **Maintain consistency** — Follow patterns from `beta-pch-coder.md` for familiar UX

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Mode selection prompt at research start (Manual vs Automatic) | High |
| FR-2 | Manual mode preserves exact pch-researcher behavior | High |
| FR-3 | Automatic mode orchestrates phases via subagents | High |
| FR-4 | Pre-flight clarification check before subagent invocation | High |
| FR-5 | Research-specific return format (FINDINGS, KEY_DISCOVERIES, GAPS_FOUND) | High |
| FR-6 | Three-level status handling (success, partial, blocked) | High |
| FR-7 | Orchestrator generates Overview section after all phases | Medium |
| FR-8 | Session handoff messages updated for mode awareness | Medium |

### Non-Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-1 | Agent prompt should be ~1020 lines (pch-researcher 675 + ~345 new) | Low |
| NFR-2 | Clear section organization mirroring beta-pch-coder structure | Medium |
| NFR-3 | Comprehensive inline documentation for maintainability | Medium |

### Out of Scope

- Modifying the original `pch-researcher.md` file
- Creating new tooling or VS Code extensions
- Changes to other agents in the workflow

---

## Technical Design

### Source Files

| File | Purpose | Lines (approx) |
|------|---------|----------------|
| `agents/pch-researcher.md` | Base agent to copy from | 675 |
| `agents/beta-pch-coder.md` | Pattern reference for automatic mode | 961 |

### Target File

| File | Purpose | Lines (approx) |
|------|---------|----------------|
| `agents/beta-pch-researcher.md` | New agent with automatic mode | ~1020 |

### Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| Mode Selection Prompt | `beta-pch-coder.md` lines 35-75 | Adapt for research context |
| Prerequisites Section | `beta-pch-coder.md` lines 77-95 | Copy with minor adjustments |
| Orchestration Loop | `beta-pch-coder.md` lines 97-150 | Adapt for research phases |
| Subagent Invocation | `beta-pch-coder.md` lines 152-185 | Modify return format |
| Blocker Handling | `beta-pch-coder.md` lines 400-510 | Add Option 5 (Provide Clarification) |
| Result Processing | `beta-pch-coder.md` lines 220-280 | Adapt for research fields |

### Document Structure

The new agent prompt will have this organization:

```
1. YAML Frontmatter (updated name, description)
2. Agent Introduction
3. 🚨 CRITICAL RULES - READ FIRST 🚨 (preserved)
4. Core Responsibilities (preserved)
5. Execution Mode Selection (NEW - ~60 lines)
6. Prerequisites for Automatic Mode (NEW - ~30 lines)
7. Automatic Mode Workflow (NEW - ~250 lines)
   - Orchestration Loop
   - Pre-Phase Clarification Check
   - Subagent Invocation Template
   - Processing Subagent Results
   - Updating the Research Document
   - Post-Phase Report
   - Final Completion (Overview Synthesis)
   - Handling Partial/Blocked Phases
   - Subagent Invocation Failure
8. One Phase Per Session - Manual Mode (existing, with mode context note)
9. Why Phased Research (preserved)
10. Document Numbering (preserved)
11. How to Break Down Research (preserved)
12. Workflow Process (preserved, minor mode context)
13. Research Document Structure (preserved)
14. Phase Sizing Guidelines (preserved)
15. Session Handoff Messages (updated for mode awareness)
16. Research Best Practices (preserved)
17. Handling Incomplete Phases (preserved)
18. Progress Tracking (preserved)
19. Quality Standards (preserved)
```

### YAML Frontmatter Changes

**From:**
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

**To:**
```yaml
---
name: beta-pch-researcher
description: Conducts comprehensive, phased research with optional automatic subagent orchestration
model: Claude Opus 4.5
handoffs:
  - label: Continue researching
    agent: beta-pch-researcher
    prompt: Continue phased research
    send: false
---
```

### Subagent Return Format

Research subagents will use this structured return format:

```
PHASE: {N}
PHASE_NAME: {Phase Name}
STATUS: success | partial | blocked
SCOPE_COMPLETED: [brief description]
FINDINGS: [comprehensive markdown content]
KEY_DISCOVERIES: [bulleted list]
RELEVANT_FILES: [list of file paths analyzed with notes]
EXTERNAL_SOURCES: [URLs or "none"]
GAPS_FOUND: [incomplete areas or "none"]
ASSUMPTIONS_MADE: [with reasoning or "none"]
FOLLOW_UP_NEEDED: [suggestions or "none"]
NOTES: [orchestrator context]
```

### Key Adaptations from beta-pch-coder

| beta-pch-coder Feature | beta-pch-researcher Adaptation |
|------------------------|-------------------------------|
| Deployment checkpoint | Pre-flight clarification check |
| FILES_MODIFIED, FILES_CREATED | RELEVANT_FILES, EXTERNAL_SOURCES |
| TASKS_COMPLETED, TASKS_BLOCKED | SCOPE_COMPLETED, GAPS_FOUND |
| Binary status (success/blocked) | Three-level status (success/partial/blocked) |
| Offer git commit/push | Generate Overview section |
| 4 blocker options | 5 blocker options: 1) Continue, 2) Skip to Phase M, 3) Stop, 4) Retry, 5) Provide Clarification |

---

## Dependencies and Prerequisites

### Prerequisites

| ID | Prerequisite | Status |
|----|--------------|--------|
| P-1 | Research document 002 complete | ✅ Complete |
| P-2 | Access to pch-researcher.md source | ✅ Available |
| P-3 | Access to beta-pch-coder.md for patterns | ✅ Available |

### Dependencies

| ID | Dependency | Impact |
|----|------------|--------|
| D-1 | VS Code subagent feature (chat.customAgentInSubagent.enabled) | Required for automatic mode |
| D-2 | runSubagent tool availability | Required for automatic mode |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Large file edit errors | Medium | Medium | Use incremental edits; build section by section |
| Inconsistent patterns with beta-pch-coder | Low | Medium | Follow research doc closely; reference beta-pch-coder directly |
| Missing edge cases | Low | Low | Research doc Phase 3 covers edge cases comprehensively |

---

## Progress Summary

| Phase | Description | Status | Tasks |
|-------|-------------|--------|-------|
| 1 | Create Base Document Structure | ✅ Complete | 5 tasks |
| 2 | Add Mode Selection and Prerequisites | ✅ Complete | 3 tasks |
| 3 | Implement Automatic Mode Workflow | ✅ Complete | 7 tasks |
| 4 | Update Session Handoff Messages | ✅ Complete | 2 tasks |
| 5 | Final Review and Validation | ✅ Complete | 3 tasks |

---

## Execution Plan

### Phase 1: Create Base Document Structure

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** None
**Entry Point:** Read `agents/pch-researcher.md`
**Verification:** File exists at `agents/beta-pch-researcher.md`

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Copy pch-researcher.md to beta-pch-researcher.md | `agents/beta-pch-researcher.md` | File created with full content |
| 1.2 | Update YAML frontmatter | `agents/beta-pch-researcher.md` | name: beta-pch-researcher, updated description, updated handoff |
| 1.3 | Update agent introduction paragraph | `agents/beta-pch-researcher.md` | Mentions optional automatic subagent orchestration |
| 1.4 | Add placeholder for new sections after Core Responsibilities | `agents/beta-pch-researcher.md` | Placeholder comments for Execution Mode Selection, Prerequisites, Automatic Mode Workflow |
| 1.5 | Verify document structure intact | `agents/beta-pch-researcher.md` | All original sections preserved, no syntax errors |

---

### Phase 2: Add Mode Selection and Prerequisites

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 1 complete
**Entry Point:** Read `agents/beta-pch-researcher.md` from Phase 1
**Verification:** Mode selection prompt appears after Core Responsibilities

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Add "Execution Mode Selection" section (~60 lines) | `agents/beta-pch-researcher.md` | Mode selection prompt with research-specific wording; Manual/Automatic options; Choosing Between Modes table |
| 2.2 | Add "Prerequisites for Automatic Mode" section (~30 lines) | `agents/beta-pch-researcher.md` | VS Code setting requirement; Plan completeness check; No interactive steps note |
| 2.3 | Add mode context note to "One Phase Per Session" section | `agents/beta-pch-researcher.md` | Note that this section applies to Manual Mode; reference to Automatic Mode Workflow |

---

### Phase 3: Implement Automatic Mode Workflow

**Estimated Session Size:** Large
**Files to Modify:** 1
**Prerequisites:** Phase 2 complete
**Entry Point:** Read `agents/beta-pch-researcher.md` placeholder section
**Verification:** Complete Automatic Mode Workflow section with all subsections

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Add Orchestration Loop section | `agents/beta-pch-researcher.md` | 3-step loop (load, iterate phases, finalize); Research-specific adaptations |
| 3.2 | Add Pre-Phase Clarification Check section | `agents/beta-pch-researcher.md` | Ambiguity triggers: text markers ("TBD", "unclear", "to be determined", "❓") AND structural checks (empty scope, missing research tasks); Clarification pause prompt template |
| 3.3 | Add Subagent Invocation section with prompt template | `agents/beta-pch-researcher.md` | Full prompt template using `agentName: "beta-pch-researcher"`; Research-specific instructions; Return format with RELEVANT_FILES field |
| 3.4 | Add Processing Subagent Results section | `agents/beta-pch-researcher.md` | Field-by-field parsing table; Three-level status interpretation |
| 3.5 | Add Updating the Research Document section | `agents/beta-pch-researcher.md` | Document section mapping; Update sequence; Orchestrator ownership note |
| 3.6 | Add Post-Phase Report and Final Completion sections | `agents/beta-pch-researcher.md` | Phase report template; Overview synthesis instructions; Final summary template |
| 3.7 | Add Handling Partial/Blocked and Invocation Failure sections | `agents/beta-pch-researcher.md` | 5-option blocker menu: 1) Continue to Next Phase, 2) Skip to Phase M, 3) Stop Automatic Execution, 4) Retry Phase N, 5) Provide Clarification; Invocation failure handling with 3 recovery options |

---

### Phase 4: Update Session Handoff Messages

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 3 complete
**Entry Point:** Locate Session Handoff Messages section
**Verification:** Handoff messages include mode context

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Update "After Creating Initial Outline" message | `agents/beta-pch-researcher.md` | Mode-aware wording; Mentions both modes available in next session |
| 4.2 | Update "After Completing a Phase" message | `agents/beta-pch-researcher.md` | Only shown in Manual Mode; Adds clarifying note about mode |

---

### Phase 5: Final Review and Validation

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 4 complete
**Entry Point:** Read complete `agents/beta-pch-researcher.md`
**Verification:** Document passes all quality checks

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Verify all sections are complete and properly formatted | `agents/beta-pch-researcher.md` | No placeholder text remaining; All markdown renders correctly |
| 5.2 | Verify consistency with beta-pch-coder patterns | `agents/beta-pch-researcher.md` | Similar structure; Consistent terminology; Research-specific adaptations documented |
| 5.3 | Verify YAML frontmatter is valid | `agents/beta-pch-researcher.md` | Frontmatter parses correctly; All required fields present |

---

## Implementation Notes

### Phase 3 - Implement Automatic Mode Workflow

**Completed:** January 16, 2026
**Execution Mode:** Subagent (pch-coder)

**Files Modified:**
- `agents/beta-pch-researcher.md`

**Tasks Completed:**
- 3.1: Added Orchestration Loop section with 3-step loop adapted for research context
- 3.2: Added Pre-Phase Clarification Check section with all triggers (TBD, unclear, to be determined, ❓, empty scope, missing tasks, vague language, placeholder text)
- 3.3: Added Subagent Invocation section with full prompt template using `agentName: "beta-pch-researcher"` and research-specific return format
- 3.4: Added Processing Subagent Results section with field-by-field parsing table and three-level status interpretation
- 3.5: Added Updating the Research Document section with document section mapping, update sequence, and orchestrator ownership note
- 3.6: Added Post-Phase Report and Final Completion sections with overview synthesis instructions
- 3.7: Added Handling Partial/Blocked Phases section with 5 options and Subagent Invocation Failure section with 3 recovery options

**Deviations from Plan:**
- None

**Notes:**
- Section is approximately 270 lines (exceeds estimate of ~250 lines slightly due to comprehensive coverage)
- Added additional ambiguity triggers: "vague language" and "placeholder text" patterns for more robust detection
- Included clarification examples for common scenarios

### Phase 4 - Update Session Handoff Messages

**Completed:** January 16, 2026
**Execution Mode:** Subagent (pch-coder)

**Files Modified:**
- `agents/beta-pch-researcher.md`

**Tasks Completed:**
- 4.1: Updated "After Creating Initial Outline" message with mode-aware wording (mentions both modes available in next session)
- 4.2: Updated "After Completing a Phase" message with Manual Mode clarifying note

**Deviations from Plan:**
- None

**Notes:**
- Session handoff messages now clearly indicate mode selection will occur in the new session
- Manual Mode note added as a blockquote for visibility

### Phase 5 - Final Review and Validation

**Completed:** January 16, 2026
**Execution Mode:** Subagent (pch-coder)

**Files Modified:**
- `agents/beta-pch-researcher.md`

**Tasks Completed:**
- 5.1: Verified all sections complete — no placeholder text, TODO comments, or incomplete sections found
- 5.2: Verified consistency with beta-pch-coder patterns — structure matches, terminology consistent, research-specific adaptations properly documented
- 5.3: Verified YAML frontmatter — valid syntax, all required fields present (name, description, model, handoffs)

**Validation Results:**
- **Placeholder Check:** ✅ PASSED — All matches for "TODO", "TBD", "placeholder" are within example/documentation text, not actual placeholders
- **Markdown Lint:** ✅ PASSED — No errors detected by VS Code diagnostics
- **YAML Frontmatter:** ✅ PASSED — Correctly formatted with `name: beta-pch-researcher`, updated description, and self-referencing handoff
- **Pattern Consistency:** ✅ PASSED — Mode Selection, Prerequisites, Orchestration Loop, Subagent Invocation, Post-Phase Report, Blocker Handling, and Invocation Failure sections all follow beta-pch-coder patterns with research-specific adaptations
- **Document Length:** 1259 lines (exceeds estimate of ~1020 lines due to comprehensive documentation)

**Deviations from Plan:**
- None

**Notes:**
- Document is production-ready
- All acceptance criteria met for Phase 5

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** January 16, 2026
**Reviewed By:** pch-plan-reviewer
**Review Date:** January 16, 2026
**Status:** ✅ Implementation Complete
**Completed By:** pch-coder
**Completion Date:** January 16, 2026
**Plan Location:** `/docs/plans/002-beta-pch-researcher-agent.md`

---

## Review Summary

**Review Date:** January 16, 2026
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v1.0
**Reviewed Plan Version:** v1.1

### Review Metrics
- Issues Found: 8 (Critical: 0, Major: 3, Minor: 5)
- Clarifying Questions Asked: 4
- Sections Updated: Technical Design, Phase 3 Acceptance Criteria

### Key Improvements Made
1. Specified `agentName: "beta-pch-researcher"` for subagent invocations (consistency with beta-pch-coder pattern)
2. Enumerated all 5 blocker options explicitly: Continue, Skip to Phase M, Stop, Retry, Provide Clarification
3. Changed `FILES_ANALYZED` to `RELEVANT_FILES` to match pch-researcher's existing section naming
4. Added specific clarification triggers: text markers ("TBD", "unclear", "to be determined", "❓") and structural checks (empty scope, missing research tasks)
5. Updated Key Adaptations table with explicit blocker option list

### Remaining Considerations
- Line references in Codebase Patterns table are approximate — implementer should use section names for navigation
- NFR-1 line count (~1020) is an estimate; actual may vary ±50 lines based on formatting
- Session handoff message examples not provided — implementer should reference beta-pch-coder patterns

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**
