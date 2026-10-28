# Implementation Plan: pch-visionary Agent

## Document Information

| Field | Value |
|-------|-------|
| Document | 005-pch-visionary-agent-implementation.md |
| Created | 2026-02-01 |
| Status | ✅ Implementation Complete |
| Owner | pch-planner |
| Version | v3.0 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-01 | pch-planner | Initial plan creation |
| v1.1 | 2026-02-01 | pch-planner | Completed all sections after Q&A |
| v2.0 | 2026-02-01 | pch-planner | Holistic review completed |
| v2.1 | 2026-02-01 | pch-plan-reviewer | Plan review completed — ready for implementation |
| v3.0 | 2026-02-01 | pch-coder | Implementation complete — all 5 phases executed via Automatic Mode |

---

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 5 | **Last Updated:** 2026-02-01

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | Agent file size discrepancy (1,100 vs 1,800-2,200 lines) | Specificity | Option D: Clarify which sections are trimmed | Technical Design updated with scope notes |
| 2 | Phase 1 lacks effort estimates | Specificity | Option C: Add line count estimates per task | Phase 1 tasks updated with line counts |
| 3 | Inconsistent copilot-instructions.md path references | Clarity | Option A: Use relative paths with leading slash | Technical Design paths standardized |
| 4 | Missing Section Completion Status in vision template | Completeness | Option A: Add as 14th section | Template description updated to 14 sections |
| 5 | Handoff labels/prompts differ from research | Correctness | Option A: Use research document versions | YAML frontmatter design updated |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-02-01 | Implementation Approach | A + Additional Phase | Follow research's 4-phase structure plus add Phase 5 for README/copilot-instructions updates |
| 1 | 2026-02-01 | Vision Document Location | A — New `/docs/vision/` | Clean separation, follows established pattern of each agent output having its own directory |
| 1 | 2026-02-01 | Agent File Size Strategy | B — Phased Section Building | Build incrementally within Phase 1: YAML → Core → Questions → Template → Session handling |
| 1 | 2026-02-01 | Standards Integration Timing | C — Query at Multiple Stages | Query at Problem Space (Stage 1), Requirements (Stage 4), and Architecture (Stage 5) for comprehensive coverage |
| 1 | 2026-02-01 | Testing Approach | C — Manual Testing After All Phases | Complete implementation first, then manually test 5 scenarios. Faster implementation flow |
| 1 | 2026-02-01 | Session Recovery Mechanism | C — Both Mechanisms | Self-handoff for discoverability + detection logic for document attachment. Covers both user behaviors |

---

## Holistic Review Summary

### Decision Interactions Analysis

| Decision 1 | Decision 2 | Interaction | Resolution |
|------------|------------|-------------|------------|
| Phased Section Building (agent file) | Manual Testing After Phases | Positive synergy — each section can be validated before testing full flow | No conflict |
| Standards at Multiple Stages | Both Recovery Mechanisms | Potential complexity — recovery must restore standards context | Standards cached in vision document; no issue |
| Follow Research Phases | Additional Documentation Phase | Compatible — Phase 5 adds cleanly after Phase 4 | No conflict |

### Architectural Considerations Addressed

1. **Context Window Management:** Phase 1 is large (12 tasks) but each task adds only 50-150 lines. The phased section building approach ensures no single task overwhelms context. ✅ Addressed

2. **Agent File Maintainability:** ~1,100 line file is substantial. Sections are clearly delineated with headers matching established patterns. Future maintenance should be manageable. ✅ Addressed

3. **Workflow Integration Complexity:** pch-visionary adds a new entry point to the workflow. Users may be confused about when to start with visionary vs. researcher. ✅ Addressed via pch-helper updates (Phase 2.4)

4. **Standards Query Overhead:** Three standards queries per vision session adds latency. Queries are at natural pause points (stage transitions), minimizing perceived delay. ✅ Acceptable trade-off

### Trade-offs Accepted

| Trade-off | Accepted Because |
|-----------|------------------|
| No Automatic Mode | Conversational Q&A requires continuous context; subagents would break this |
| Manual testing only | Faster implementation; 5 scenarios are straightforward to test manually |
| Large Phase 1 | Keeps related agent content together; easier to review as cohesive unit |

### Risks Acknowledged

| Risk | Status | Mitigation Confirmed |
|------|--------|---------------------|
| Session interruption | Mitigated | Dual recovery mechanisms (both handoff and attachment detection) |
| Standards unavailable | Mitigated | Graceful fallback documented in Phase 3 |
| User confusion on agent selection | Mitigated | pch-helper guidance added in Phase 2 |

---

## Overview

### Vision Reference

| Field | Value |
|-------|-------|
| Research Document | `/docs/research/005-pch-visionary-agent-design.md` |
| Research Status | ✅ Complete (6 of 6 phases) |

### Feature Summary

Create a new `pch-visionary` agent that serves as the **first agent** in the PCH workflow. The agent combines business analyst and solution architect capabilities to transform raw ideas into structured vision documents through guided Q&A.

### Key Design Decisions from Research

1. **Follows pch-planner's incremental pattern** — NOT subagent orchestration
2. **Six-stage question flow** — Problem Space → Stakeholders → Goals → Requirements → Architecture → Phasing
3. **Vision documents in `/docs/vision/`** — 13-section template
4. **Three handoff paths** — Self-continuation, pch-researcher (standard), pch-planner (skip research)
5. **Standards integration via pch-standards-space**

---

## Objectives

1. **Create the pch-visionary agent file** (`/agents/pch-visionary.md`) with complete Q&A workflow, question templates, and vision document template
2. **Establish the `/docs/vision/` directory** for vision document storage with proper numbering convention
3. **Integrate pch-visionary into the PCH workflow** by updating pch-helper with guidance and handoff paths
4. **Enable standards integration** via pch-standards-space queries at Problem Space, Requirements, and Architecture stages
5. **Update repository documentation** (README.md and copilot-instructions.md) to reflect the new 6-agent workflow

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority | Phase |
|----|-------------|----------|-------|
| FR-1 | Agent transforms raw ideas into structured vision documents through guided Q&A | Must Have | 1 |
| FR-2 | Six-stage question flow: Problem Space → Stakeholders → Goals → Requirements → Architecture → Phasing | Must Have | 1 |
| FR-3 | Three question types: Elicitation (open), Refinement (multiple choice), Validation (confirm) | Must Have | 1 |
| FR-4 | Vision documents stored in `/docs/vision/` with `{NNN}-{description}.md` naming | Must Have | 1 |
| FR-5 | 14-section vision document template embedded in agent | Must Have | 1 |
| FR-6 | Vision Session Log tracks all Q&A with immediate updates | Must Have | 1 |
| FR-7 | Three handoff paths: self-continuation, pch-researcher, pch-planner | Must Have | 2 |
| FR-8 | pch-helper updated with pch-visionary guidance and workflow diagram | Must Have | 2 |
| FR-9 | Standards queried at Problem Space (Stage 1), Requirements (Stage 4), Architecture (Stage 5) | Must Have | 3 |
| FR-10 | Session recovery via self-handoff AND document attachment detection | Must Have | 4 |
| FR-11 | Research Topics section serves as explicit handoff interface to pch-researcher | Must Have | 1 |

### Non-Functional Requirements

| ID | Requirement | Priority | Constraint Type |
|----|-------------|----------|-----------------|
| NFR-1 | Agent file follows established YAML frontmatter patterns | Must Have | Consistency |
| NFR-2 | Agent builds vision document incrementally (one section at a time) | Must Have | Context Management |
| NFR-3 | No subagent usage — continuous conversational context required | Must Have | Architecture |
| NFR-4 | Agent file ~1,100 lines, built in phased sections | Should Have | Maintainability |
| NFR-5 | Graceful fallback when pch-standards-space unavailable | Must Have | Reliability |

### Out of Scope

- Automatic Mode / Manual Mode selection (not applicable — single-session Q&A workflow)
- Subagent orchestration (research determined this breaks conversational context)
- Research enrichment subagents (identified as future enhancement in research)
- Modifications to pch-researcher or pch-coder agents
- Integration with external systems beyond pch-standards-space

---

## Technical Design

### Agent File Structure

**File:** `/agents/pch-visionary.md`
**Estimated Size:** ~1,100 lines (condensed from research estimate of ~1,800-2,200)

**Scope Clarification:** The plan targets a tighter implementation by:
- Condensing Stage-by-Stage Questions (research ~300 lines → plan ~200 lines) — fewer example variations
- Trimming Best Practices section (research ~50 lines → plan: folded into existing sections)
- Reducing Adaptive Question Flow examples (research ~40 lines → plan ~20 lines)
- The core six-stage framework, all question types, and full vision template are retained

The agent file will be structured in these sections (built incrementally):

| Section | Lines (Est.) | Content |
|---------|--------------|---------|
| YAML Frontmatter | ~30 | name, description, model, mcp-servers, handoffs (3) |
| Role Definition | ~20 | Identity statement, BA + architect dual role |
| Documentation Constraint | ~15 | Documentation-only rules (like other agents) |
| Core Responsibilities | ~25 | 5-7 bulleted responsibilities |
| Critical Rules | ~40 | Session initialization logic, one-question-at-a-time, immediate updates |
| Six-Stage Question Flow | ~80 | Stage definitions, completion criteria, flow diagram |
| Question Type Templates | ~150 | Elicitation, Refinement, Validation templates with examples |
| Stage-by-Stage Questions | ~300 | Complete question templates for all 6 stages |
| Standards Integration | ~50 | MCP query instructions, fallback behavior, documentation format |
| Vision Document Template | ~250 | Complete 13-section embedded template |
| Session Recovery | ~40 | Self-handoff detection, resume logic |
| Synthesis & Handoff | ~60 | Final validation, handoff message templates |
| Error Handling | ~40 | Edge cases, incomplete sessions, user redirects |

### YAML Frontmatter Design

```yaml
---
name: pch-visionary
description: Transforms raw ideas into structured vision documents through guided Q&A, combining business analyst and solution architect perspectives
model: Claude Opus 4.5
mcp-servers:
  - config: |
      {
        "github": {
          "type": "copilot",
          "toolsets": ["copilot_spaces"]
        }
      }
handoffs:
  - label: Continue Vision
    agent: pch-visionary
    prompt: Continue vision capture from the attached document
    send: false
  - label: Start Research
    agent: pch-researcher
    prompt: |
      Begin research based on the completed vision document.
      Focus on the Research Topics section which identifies key areas 
      requiring investigation before implementation planning.
    send: false
  - label: Skip Research - Start Planning
    agent: pch-planner
    prompt: |
      Create an implementation plan based on the completed vision document.
      Use the Requirements Overview and Product Phasing Strategy to 
      structure the implementation plan.
    send: false
---
```

### Vision Document Template

**Location:** Embedded in agent file (not a separate template file)
**Directory:** `/docs/vision/`
**Naming:** `{NNN}-{description}.md` (zero-padded 3-digit)

**14 Sections:**
1. Document Information (table)
2. Version History (table)
3. Vision Session Log (table)
4. Section Completion Status (progress tracking table)
5. Executive Summary (populated last)
6. Problem Statement (Business Context, Current/Future State, Success Metrics)
7. Stakeholders & Users (Primary Stakeholders table, User Personas)
8. Goals & Objectives (Primary Goals table, Non-Goals list)
9. Requirements Overview (Functional, Non-Functional, Constraints)
10. Architecture Vision (Solution Overview, Style table, Technology Decisions, Integration Points, Security)
11. Product Phasing Strategy (Phase Overview table, Phase 1 MVP details, Future Phases)
12. Research Topics (Topics table, Open Questions)
13. Risk Assessment (Vision-level risks table)
14. Handoff Information (metadata block)

### pch-helper Updates

**File:** `/agents/pch-helper.md`
**Changes Required:**

1. **Add pch-visionary to handoffs array** in YAML frontmatter
2. **Add agent description section** (~40 lines) under "The PCH Agents"
3. **Update workflow diagram** to show visionary as first agent
4. **Update "When to Start Where" table** with new entry for pch-visionary
5. **Add FAQ entry** explaining when to use visionary vs. starting with researcher/planner

### copilot-instructions.md Updates

**File:** `/.github/copilot-instructions.md`
**Changes Required:**

1. **Update Agent Architecture section** — Add pch-visionary to the 5-agent list (now 6)
2. **Update workflow diagram** — Show visionary → researcher → planner → reviewer → coder
3. **Update Document Locations** — Add `/docs/vision/` entry
4. **Minor text updates** — References to "five core agents" become "six core agents"

### README.md Updates

**File:** `/README.md`
**Changes Required:**

1. **Update agent count** in overview
2. **Add pch-visionary description** to agent list
3. **Update workflow explanation** if present

---

## Dependencies and Prerequisites

### Prerequisites

| Prerequisite | Description | Status |
|--------------|-------------|--------|
| Research Complete | Research document 005-pch-visionary-agent-design.md completed | ✅ Complete |
| Agent Patterns | Understanding of existing agent patterns (pch-planner, pch-researcher) | ✅ Available |
| Standards Space | pch-standards-space Copilot Space exists (optional, graceful fallback) | ⚠️ Optional |

### Dependencies Between Phases

| Phase | Depends On | Blocking? |
|-------|------------|-----------|
| Phase 2 (Workflow Integration) | Phase 1 (Agent File) | Yes — helper needs to reference working agent |
| Phase 3 (Standards Integration) | Phase 1 (Agent File) | Yes — standards logic added to existing agent |
| Phase 4 (Production Hardening) | Phases 1-3 | Yes — error handling for complete agent |
| Phase 5 (Documentation) | Phases 1-4 | Yes — docs describe final agent behavior |

### Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| YAML Frontmatter | All agents in `/agents/` | pch-visionary follows same structure |
| MCP Server Config | pch-planner.md, pch-researcher.md | Copied for standards integration |
| Handoff Structure | pch-helper.md | Add new handoff entry |
| Question Format | pch-planner.md (lines 55-80) | Adapted for Refinement question type |
| Incremental Building | pch-planner.md (lines 35-50) | Vision document built same way |
| Session Recovery | pch-researcher.md (lines 35-55) | Self-handoff pattern |

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation Strategy |
|------|------------|--------|---------------------|
| Agent file too large for single-session creation | Medium | Medium | Build incrementally across multiple tasks in Phase 1 (decision B) |
| Question flow too rigid for diverse use cases | Low | Medium | Include adaptive flow guidance, allow stage skipping when appropriate |
| Standards queries slow down Q&A experience | Medium | Low | Query at key stages only (3 queries), not every question |
| Session interruption loses context | Medium | Medium | Vision Session Log captures all progress; dual recovery mechanisms (decision C) |
| Users skip visionary and go straight to researcher | Low | Low | pch-helper guidance explains when to use visionary; workflow remains flexible |
| pch-standards-space unavailable | Low | Low | Graceful fallback with warning message (NFR-5) |

---

## Progress Summary

| Phase | Description | Status |
|-------|-------------|--------|
| 1 | Foundation — Agent File & Vision Template | ✅ Complete |
| 2 | Workflow Integration — pch-helper Updates | ✅ Complete |
| 3 | Enhancement — Standards Integration | ✅ Complete |
| 4 | Production Hardening — Error Handling & Edge Cases | ✅ Complete |
| 5 | Documentation — README & copilot-instructions.md | ✅ Complete |

---

## Execution Plan

### Phase 1: Foundation — Agent File & Vision Template

**Estimated Session Size:** Large (10-12 tasks)
**Files to Create:** `/agents/pch-visionary.md`, `/docs/vision/.gitkeep`
**Prerequisites:** None
**Entry Point:** Start by reviewing research document Phase 6 for content specifications

| Step | Task | File(s) | Lines | Acceptance Criteria |
|------|------|---------|-------|---------------------|
| 1.1 | Create `/docs/vision/` directory with `.gitkeep` | `/docs/vision/.gitkeep` | — | Directory exists and is tracked in git |
| 1.2 | Create pch-visionary.md with YAML frontmatter | `/agents/pch-visionary.md` | ~30 | Valid YAML with name, description, model, mcp-servers, 3 handoffs |
| 1.3 | Add role definition and documentation constraint | `/agents/pch-visionary.md` | ~35 | Clear identity statement, documentation-only rules matching other agents |
| 1.4 | Add core responsibilities section | `/agents/pch-visionary.md` | ~25 | 5-7 bulleted responsibilities covering Q&A workflow |
| 1.5 | Add critical rules with session initialization logic | `/agents/pch-visionary.md` | ~60 | Decision tree for new vs. continuation, one-question-at-a-time rule |
| 1.6 | Add six-stage question flow section | `/agents/pch-visionary.md` | ~80 | Stage definitions, completion criteria table, ASCII flow diagram |
| 1.7 | Add question type templates (Elicitation, Refinement, Validation) | `/agents/pch-visionary.md` | ~120 | Three distinct templates with format rules and examples |
| 1.8 | Add stage-by-stage question templates (Stages 1-3) | `/agents/pch-visionary.md` | ~100 | Problem Space, Stakeholders, Goals questions with templates |
| 1.9 | Add stage-by-stage question templates (Stages 4-6) | `/agents/pch-visionary.md` | ~100 | Requirements, Architecture, Phasing questions with templates |
| 1.10 | Add embedded vision document template (14 sections) | `/agents/pch-visionary.md` | ~250 | Complete template matching research Phase 2 design plus Section Completion Status |
| 1.11 | Add synthesis and handoff section | `/agents/pch-visionary.md` | ~60 | Final validation prompts, handoff message templates for all 3 paths |
| 1.12 | Verify agent file structure and completeness | `/agents/pch-visionary.md` | — | All sections present, follows patterns from pch-planner |

**Phase 1 Definition of Done:**
- [ ] `/docs/vision/` directory exists
- [ ] `/agents/pch-visionary.md` created with all core sections
- [ ] Agent can be invoked with `@pch-visionary`
- [ ] Agent creates vision document skeleton when given new idea

---

### Phase 2: Workflow Integration — pch-helper Updates

**Estimated Session Size:** Small (4 tasks)
**Files to Modify:** `/agents/pch-helper.md`
**Prerequisites:** Phase 1 complete
**Entry Point:** Read current pch-helper.md structure

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Add pch-visionary handoff to YAML frontmatter | `/agents/pch-helper.md` | New handoff entry with label, agent, prompt, send: false |
| 2.2 | Add pch-visionary agent description section | `/agents/pch-helper.md` | ~40 lines describing purpose, when to use, characteristics, output |
| 2.3 | Update workflow diagram to show visionary first | `/agents/pch-helper.md` | ASCII diagram: visionary → researcher → planner → reviewer → coder |
| 2.4 | Update "When to Start Where" table | `/agents/pch-helper.md` | New row for "Raw idea, unclear requirements" → pch-visionary |

**Phase 2 Definition of Done:**
- [ ] pch-helper includes pch-visionary in handoffs
- [ ] pch-visionary description matches format of other agents
- [ ] Workflow diagram shows 5-agent flow with visionary first
- [ ] User can ask pch-helper and get guidance to use pch-visionary

---

### Phase 3: Enhancement — Standards Integration

**Estimated Session Size:** Small (4 tasks)
**Files to Modify:** `/agents/pch-visionary.md`
**Prerequisites:** Phase 1 complete
**Entry Point:** Read standards integration sections from pch-planner.md and pch-researcher.md

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Add standards query instructions for Stage 1 (Problem Space) | `/agents/pch-visionary.md` | Query triggered after initial problem description, before questions |
| 3.2 | Add standards query instructions for Stage 4 (Requirements) | `/agents/pch-visionary.md` | Query for compliance/regulatory standards based on domain |
| 3.3 | Add standards query instructions for Stage 5 (Architecture) | `/agents/pch-visionary.md` | Query for technical standards based on solution style chosen |
| 3.4 | Add fallback behavior and Referenced Standards section format | `/agents/pch-visionary.md` | Warning message when unavailable, table format for documenting standards |

**Phase 3 Definition of Done:**
- [ ] Agent queries pch-standards-space at 3 defined stages
- [ ] Standards are documented in Referenced Standards section of vision document
- [ ] Graceful fallback when MCP or Space unavailable
- [ ] Query content is contextual based on vision captured so far

---

### Phase 4: Production Hardening — Error Handling & Edge Cases

**Estimated Session Size:** Small (5 tasks)
**Files to Modify:** `/agents/pch-visionary.md`
**Prerequisites:** Phases 1-3 complete
**Entry Point:** Review edge case scenarios from research Phase 6

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Add session recovery logic (self-handoff detection) | `/agents/pch-visionary.md` | Agent detects when continuing vs. new session via document analysis |
| 4.2 | Add session recovery logic (document attachment detection) | `/agents/pch-visionary.md` | Agent resumes from Vision Session Log when partial doc attached |
| 4.3 | Add user redirect handling | `/agents/pch-visionary.md` | When user jumps to implementation details, redirect to current stage |
| 4.4 | Add adaptive flow guidance | `/agents/pch-visionary.md` | Fast track for expert users, deep dive for exploratory users |
| 4.5 | Add incomplete session handling | `/agents/pch-visionary.md` | Clear instructions when user must leave before completion |

**Phase 4 Definition of Done:**
- [ ] Agent recovers gracefully from interrupted sessions
- [ ] Agent handles off-track user input appropriately
- [ ] Agent adapts question depth based on user expertise
- [ ] All error scenarios have defined behavior

---

### Phase 5: Documentation — README & copilot-instructions.md

**Estimated Session Size:** Small (4 tasks)
**Files to Modify:** `/README.md`, `/.github/copilot-instructions.md`
**Prerequisites:** Phases 1-4 complete
**Entry Point:** Read current README.md and copilot-instructions.md

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Update copilot-instructions.md Agent Architecture section | `/.github/copilot-instructions.md` | 6 agents listed, pch-visionary added with description |
| 5.2 | Update copilot-instructions.md workflow diagram | `/.github/copilot-instructions.md` | Diagram shows visionary → researcher → planner → reviewer → coder |
| 5.3 | Update copilot-instructions.md Document Locations | `/.github/copilot-instructions.md` | Add `/docs/vision/` entry |
| 5.4 | Update README.md with pch-visionary information | `/README.md` | Agent count updated, pch-visionary description added |

**Phase 5 Definition of Done:**
- [ ] copilot-instructions.md reflects 6-agent workflow
- [ ] Document locations include `/docs/vision/`
- [ ] README.md accurately describes the complete agent system
- [ ] All references to "five agents" updated to "six agents"

---

## Implementation Notes for AI Agents

### Critical Patterns to Follow

1. **YAML Frontmatter Format** — Copy exact structure from `/agents/pch-planner.md` lines 1-18, adapting content for pch-visionary

2. **MCP Server Configuration** — Use identical JSON structure:
   ```json
   {
     "github": {
       "type": "copilot",
       "toolsets": ["copilot_spaces"]
     }
   }
   ```

3. **Question Format** — Refinement questions must follow pch-planner's Options → Recommendation → Rationale structure (see pch-planner.md lines 55-80)

4. **Documentation Constraint Block** — Copy format from pch-planner.md lines 22-28, changing `/docs/plans/` to `/docs/vision/`

5. **Status Emoji Consistency** — Use only: ⏳ Not Started | 🔄 In Progress | ✅ Complete | ❌ Blocked

6. **Handoff Information Block** — Follow format from research document Phase 2 vision template

### Key Content Sources

| Content | Source Location |
|---------|-----------------|
| YAML frontmatter | Research Phase 5 (complete YAML design) |
| Vision document template | Research Phase 2 (13-section template) |
| Question templates | Research Phase 3 (stage-by-stage questions) |
| Session recovery | Research Phase 4 (why no subagents) + Phase 5 |
| Standards integration | Research Phase 5 + pch-planner.md patterns |

### File Size Management

The agent file will be ~1,100 lines. Build it incrementally:
- Create file with YAML frontmatter first (Step 1.2)
- Add sections in order (Steps 1.3-1.11)
- Each step should add 50-150 lines maximum
- Verify file structure after each major section

### Testing After Implementation

After all phases complete, manually test these 5 scenarios from research:

1. **New Vision Creation** — Start with "I have an idea for a customer portal" and complete full Q&A
2. **Session Recovery** — Interrupt mid-vision, start new session with document attached
3. **Handoff to Researcher** — Complete vision and verify handoff message and Research Topics
4. **Handoff to Planner** — Complete simple vision and use "Skip to Planning" path
5. **Standards Integration** — Verify standards are queried at Stages 1, 4, 5 when available

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 2 | 4 files: 1 new agent, 3 updates (helper, copilot-instructions, README) |
| New patterns introduced | 2 | Six-stage Q&A flow is new, but follows pch-planner incremental pattern |
| External dependencies | 1 | Only pch-standards-space (optional, graceful fallback) |
| Migration complexity | 1 | No migrations; additive changes only |
| Test coverage required | 2 | 5 manual test scenarios defined |
| **Overall Complexity** | **8/25** | **Low** — Additive feature with clear patterns to follow |

---

## Review Summary

**Review Date:** 2026-02-01
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 5 (Critical: 0, Major: 4, Minor: 1)
- Clarifying Questions Asked: 5
- Sections Updated: Technical Design, Phase 1 tasks, YAML frontmatter design

### Key Improvements Made
1. Added explicit scope clarification for agent file size (1,100 lines condensed from research's 1,800-2,200)
2. Added line count estimates to all Phase 1 tasks for context window management
3. Updated vision template from 13 to 14 sections (added Section Completion Status)
4. Aligned YAML frontmatter handoff labels/prompts with research document
5. Verified path consistency for copilot-instructions.md references

### Remaining Considerations
- Task 1.10 line estimate (~250) may increase slightly with 14th section
- "Skip to Planning" label in Testing section (line ~507) still uses old wording — implementer should use "Skip Research - Start Planning"

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Referenced Standards

| Standard | Relevance | Key Guidance Applied |
|----------|-----------|---------------------|
| PCH Agent Patterns | Agent file structure consistency | YAML frontmatter, handoff structure, documentation-only constraint |
| Incremental Document Creation | Context window management | One section at a time, immediate updates after each answer |
| Question Format (pch-planner) | User interaction consistency | Options → Recommendation → Rationale structure for Refinement questions |

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** 2026-02-01
**Reviewed By:** pch-plan-reviewer
**Review Date:** 2026-02-01
**Status:** ✅ Ready for Implementation
**Next Agent:** pch-coder
**Plan Location:** `/docs/plans/005-pch-visionary-agent-implementation.md`
