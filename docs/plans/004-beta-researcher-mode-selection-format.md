# Implementation Plan: Beta Researcher Mode Selection Format Alignment

## Document Information

| Field | Value |
|-------|-------|
| **Plan Number** | 004 |
| **Version** | v1.0 |
| **Date** | January 16, 2026 |
| **Status** | 🔄 In Progress |
| **Owner** | pch-planner |
| **Feature** | Align beta-pch-researcher mode selection format with beta-pch-coder |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-01-16 | pch-planner | Initial plan creation |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-01-16 | Initial request | Align mode selection format | User prefers beta-pch-coder's format style |

---

## Holistic Review Summary

*To be completed after all questions are answered.*

---

## Overview and Objectives

### Background

The user wants to update the `beta-pch-researcher` agent to use the same mode selection format style as `beta-pch-coder`. Upon analysis, both agents currently have very similar mode selection prompts, but there may be subtle differences in wording, structure, or presentation that the user wants aligned.

### Objectives

1. Ensure `beta-pch-researcher` mode selection format matches `beta-pch-coder` exactly
2. Maintain context-appropriate wording (research vs. implementation)
3. Preserve semantic meaning while achieving visual/structural consistency

---

## Requirements

### Functional Requirements

- [ ] Mode selection prompt in beta-pch-researcher must follow the same structure as beta-pch-coder
- [ ] Mode option descriptions should use consistent formatting patterns
- [ ] Parenthetical notes should use similar phrasing style

### Non-Functional Requirements

- [ ] Changes should be minimal and targeted
- [ ] Research-specific context should be preserved where appropriate

### Out of Scope

- Changes to the actual mode selection logic
- Changes to other sections of the agent files
- Changes to beta-pch-coder

---

## Technical Design

### Current State Analysis

**beta-pch-coder Mode Selection Prompt (Lines 35-48):**
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

**beta-pch-researcher Mode Selection Prompt (Lines 73-86):**
```
🔄 **Execution Mode Selection**

I'm ready to conduct research as documented at `[research doc path]`.

**How would you like to proceed?**

1️⃣ **Manual Mode** (Recommended for first-time research)
   I'll execute one phase at a time. After each phase, you'll start a new session to continue.
   This gives you full control and the ability to review findings between phases.

2️⃣ **Automatic Mode** (For well-defined research documents)
   I'll orchestrate all phases automatically using isolated subagents.
   Each phase runs in fresh context. I'll report findings after each phase completes.
   ⚠️ Requires VS Code setting: `chat.customAgentInSubagent.enabled: true`

**Enter 1 or 2** (or say "manual" / "automatic"):
```

### Differences Identified

| Aspect | beta-pch-coder | beta-pch-researcher | Difference Type |
|--------|----------------|---------------------|-----------------|
| Context line | "implement the plan at" | "conduct research as documented at" | Appropriate variation |
| Path placeholder | `[plan path]` | `[research doc path]` | Appropriate variation |
| Manual parenthetical | "first-time plans" | "first-time research" | Appropriate variation |
| Manual description | "review between phases" | "review findings between phases" | Minor wording difference |
| Auto parenthetical | "trusted, reviewed plans" | "well-defined research documents" | **Key difference** |
| Auto description | "report results" | "report findings" | Appropriate variation |

### Proposed Changes

The key formatting difference is in the **Automatic Mode parenthetical note**:
- beta-pch-coder: `(For trusted, reviewed plans)`
- beta-pch-researcher: `(For well-defined research documents)`

To achieve consistency while preserving context appropriateness, update beta-pch-researcher to use:
- `(For trusted, well-structured research)` — mirrors the "trusted, reviewed" pattern from coder

Additionally, the "Choosing Between Modes" table should be reviewed for consistency.

---

## Dependencies and Prerequisites

### Prerequisites

- Access to `/agents/beta-pch-researcher.md`

### Dependencies

- None

---

## Risk Assessment and Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Breaks existing functionality | Low | Low | Changes are cosmetic only |
| Inconsistent wording | Low | Low | Careful review of context-appropriate terms |

---

## Progress Summary

| Phase | Description | Status |
|-------|-------------|--------|
| Phase 1 | Update mode selection format | ⏳ Not Started |

---

## Execution Plan

### Phase 1: Update Mode Selection Format

**Estimated Session Size:** Small
**Files to Modify:** 1
**Prerequisites:** None
**Entry Point:** Start by reading `agents/beta-pch-researcher.md`
**Verification:** Visual comparison of mode selection section with beta-pch-coder

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Update Automatic Mode parenthetical | `agents/beta-pch-researcher.md` | Parenthetical matches style of beta-pch-coder: uses "trusted" phrasing |
| 1.2 | Verify mode selection prompt structure | `agents/beta-pch-researcher.md` | Format visually matches beta-pch-coder |
| 1.3 | Review "Choosing Between Modes" table | `agents/beta-pch-researcher.md` | Table structure and style consistent |

---

## Implementation Notes for AI Agents

### Pattern Reference

Follow the format established in [beta-pch-coder.md](../agents/beta-pch-coder.md) lines 28-70 for the mode selection section.

### Key Changes

1. Line ~79: Change `(For well-defined research documents)` to `(For trusted, well-structured research)`
2. Verify all other formatting matches

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** January 16, 2026
**Status:** Pending Review
**Next Agent:** pch-plan-reviewer
**Plan Location:** `/docs/plans/004-beta-researcher-mode-selection-format.md`
