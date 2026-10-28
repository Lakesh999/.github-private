---
id: "020"
type: plan
title: "AI-Efficient Markdown Document Format for PCH Agents"
status: complete
created: "2025-02-11"
updated: "2025-02-11"
owner: pch-planner
version: v2.1
---

## Review Session Log

**Questions Pending:** 0
**Questions Resolved:** 3
**Last Updated:** 2025-02-11

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | Missing YAML references in pch-coder.md | completeness | Option A: Add Tasks 4.5 and 4.6 | Phase 4 tasks + Progress Summary + Codebase Patterns updated |
| 2 | Task 1.5 line reference incorrect; overlap with 1.6 | completeness | Option C: Expand 1.5 range + 1.6 as verification | Tasks 1.5, 1.6 updated |
| 3 | Task 5.6 lacks specific acceptance criteria | specificity | Option A: Add explicit line references and exclusion list | Task 5.6 updated |

## Introduction

This plan converts all PCH agent document templates from YAML format to a compact, AI-efficient Markdown format. Research 018 found that YAML's deep nesting (up to 4 levels), indentation sensitivity, and mixed array styles cause inconsistent AI generation. The proposed Markdown format uses max 2-level headers, Markdown tables for structured data, and emoji status values that align with Hugh's existing PlanPhaseParser — requiring zero code changes to Hugh. Token usage is ~74% of verbose Markdown (7% more than YAML), an acceptable trade-off for reliable generation.

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2025-02-11 | pch-planner | Initial plan creation |
| v1.1 | 2025-02-11 | pch-planner | Technical design and execution phases added per user decisions |
| v2.0 | 2025-02-11 | pch-planner | Holistic review completed; plan ready for review |
| v2.1 | 2025-02-11 | pch-plan-reviewer | Review complete: added Tasks 4.5/4.6, clarified Task 1.5/1.6 boundaries, refined Task 5.6 criteria |

## Decisions Log

| # | Topic | Question | Decision | Rationale |
|---|-------|----------|----------|-----------|
| 1 | Metadata Block Format | YAML front matter vs plain text vs inline labels | A — YAML Front Matter (triple-dash fenced) | Flat key-value scalars are 100% reliable for AI; universally recognized by Markdown tooling |
| 2 | Status Value Format | Emoji vs plain text vs hybrid | D — Emoji Everywhere with Plain Text Suffix | Matches Hugh PlanPhaseParser regex exactly; human-readable; no Hugh changes needed |
| 3 | Incremental Building Instructions | YAML-specific vs format-agnostic vs Markdown-specific with tips | A — Replace with Markdown-Specific Instructions | Keeps explicit format-aware guidance; references Markdown constructs (## sections, table rows, replace_string_in_file with Markdown matching) |
| 4 | Migration Scope | New format only vs dual-format vs migration phase | A — New Format Only, No Backward Compatibility | Simpler prompts; agents can interpret YAML docs without explicit instructions; avoids prompt bloat |
| 5 | Plan Task Structure | Task table vs expandable details vs bullet lists | A — Task Table Per Phase | Flattest structure; eliminates nesting; semicolon-separated criteria in cells; preserves step ID pattern for pch-coder |
| 6 | pch-planner Self-Reference | Update planner first vs last vs split | A — Update pch-planner Last (Phase 5) | Plan document is authoritative spec; pch-coder reads plan not planner prompt; other agents converted first reduces risk |

## Overview

**Feature:** Replace YAML document templates with compact Markdown templates across all PCH agent prompts (pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer, pch-coder) and update copilot-instructions.md.

**Objectives:**
- Eliminate YAML generation inconsistencies (indentation errors, schema drift, mixed array styles)
- Align document format with Hugh's PlanPhaseParser (already Markdown tables + emoji)
- Maintain token efficiency (~74% of verbose Markdown)
- Standardize shared conventions across all document types
- Preserve all existing document content/structure in equivalent Markdown form

**Research Source:** [docs/research/018-markdown-document-format-for-pch-agents.md](docs/research/018-markdown-document-format-for-pch-agents.md)

## Requirements

### Functional

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Replace YAML vision document template in pch-visionary with Markdown template | Must-have |
| FR-2 | Replace YAML research document template in pch-researcher with Markdown template | Must-have |
| FR-3 | Replace YAML plan document template in pch-planner with Markdown template | Must-have |
| FR-4 | Update pch-plan-reviewer navigation instructions and output templates | Must-have |
| FR-5 | Update pch-coder navigation instructions and task status format | Must-have |
| FR-6 | Update copilot-instructions.md YAML references | Must-have |
| FR-7 | Define shared conventions (metadata block, headers, tables, status values) | Must-have |
| FR-8 | Standardize pch-plan-reviewer output structures (review_session_log, implementation_complexity, review_summary) | Must-have |

### Non-Functional

| ID | Requirement | Priority | Type |
|----|-------------|----------|------|
| NFR-1 | New format must be parseable by Hugh PlanPhaseParser without code changes | Must-have | Compatibility |
| NFR-2 | Token usage of new templates ≤80% of original verbose Markdown | Should-have | Efficiency |
| NFR-3 | Max 2 levels of header hierarchy (## and ###) | Must-have | Simplicity |
| NFR-4 | Existing documents remain valid (no migration required) | Must-have | Backward compat |

### Out of Scope

- Migration of existing YAML documents to new format
- Changes to Hugh codebase (PlanPhaseParser, SignalClassifier, SdkResponseProcessor)
- Changes to subagent text-based return format
- Changes to inter-agent communication protocols

## Technical Design

### Shared Conventions

**Metadata Block:** Triple-dash fenced block at document top with simple `key: value` lines. Required keys: id, type, title, status, created.

**Status Values (emoji, matching Hugh PlanPhaseParser):**
- ⏳ Not Started
- 🔄 In Progress
- ✅ Complete
- ❌ Blocked
- ⚠️ Partial

**Header Hierarchy:** Max 2 levels — `##` for major sections, `###` for subsections. No `####`.

**Structured Data Rules:**
- Tabular data (3+ columns) → Markdown table
- Status tracking with progress → Markdown table
- Simple lists → Bullet list
- Short arrays (2-5 items) → Inline with bold label
- Freeform prose → Direct text under header
- Code examples → Fenced code blocks

### Codebase Patterns

| Pattern | Location | Usage |
|---------|----------|-------|
| YAML document templates | `agents/pch-visionary.md` lines 1086-1230 | Replace with Markdown template |
| YAML document templates | `agents/pch-researcher.md` lines 1395-1488 | Replace with Markdown template |
| YAML plan structure | `agents/pch-planner.md` lines 358-375 | Replace with Markdown section spec |
| YAML navigation note | `agents/pch-coder.md` line 14 | Remove YAML reference |
| YAML navigation note | `agents/pch-plan-reviewer.md` line 22 | Remove YAML reference |
| YAML incremental building | `agents/pch-researcher.md` lines 106-110 | Replace YAML refs with Markdown |
| YAML incremental building | `agents/pch-visionary.md` lines 296-300 | Replace YAML refs with Markdown |
| YAML incremental building | `agents/pch-planner.md` lines 114-119 | Replace YAML refs with Markdown |
| YAML version history | `agents/pch-planner.md` lines 387-397 | Replace with Markdown table |
| YAML phase template | `agents/pch-planner.md` lines 444-456 | Replace with Markdown table |
| YAML handoff metadata | `agents/pch-planner.md` lines 561-567 | Replace with Markdown table |
| YAML review_session_log | `agents/pch-plan-reviewer.md` lines 377-399 | Replace with Markdown table |
| YAML handoff section | `agents/pch-plan-reviewer.md` lines 562-570 | Replace with Markdown table |
| YAML task status updates | `agents/pch-coder.md` lines 344-365 | Replace with Markdown table rows |
| YAML progress_summary | `agents/pch-coder.md` lines 380-385 | Replace with Markdown table |
| YAML progress tracking (subagent) | `agents/pch-coder.md` lines 703-715 | Replace with Markdown table format |
| YAML deployment_checkpoint | `agents/pch-coder.md` lines 1102-1115 | Replace with Markdown table/bold-label format |
| PlanPhaseParser (Markdown) | `Hugh/Detection/PlanPhaseParser.cs` | Already parses Markdown tables + emoji — no changes |
| README document format | `README.md` lines 262-265 | Update YAML references |

### Component Specifications

Each agent prompt requires specific modifications organized by the type of change:

**Change Type 1: Template Replacement (Producers)**
Replace the full YAML document template with an equivalent Markdown template using the shared conventions (--- metadata, ## headers, ### subsections, Markdown tables, emoji status).

| Agent | Template Location | Complexity | Key Structures to Convert |
|-------|------------------|------------|--------------------------|
| pch-researcher | lines 1395-1488 | Medium | phases[] → ### Phase N sections; relevant_files[] → tables |
| pch-visionary | lines 1086-1230 | High | phasing.phases[] → tables + ### subsections; decisions[] → table; stakeholders[] → table |
| pch-planner | lines 358-456 | High | phases[].tasks[] → tables under ### Phase N; versions[] → table; handoff{} → table |

**Change Type 2: Incremental Building Instructions**
Replace YAML-specific incremental instructions with Markdown equivalents.

| Agent | Location | Change |
|-------|----------|--------|
| pch-researcher | lines 106-110 | "YAML section" → "## section"; "YAML key(s)" → "section under the appropriate ## header"; "YAML content" → "Markdown content" |
| pch-visionary | lines 296-300 | Same pattern as researcher |
| pch-planner | lines 114-119 | Same pattern as researcher |

**Change Type 3: Navigation Instructions (Consumers)**
Replace "navigate by YAML keys" with Markdown header navigation.

| Agent | Location | Old Text | New Text |
|-------|----------|----------|----------|
| pch-coder | line 14 | "Navigate plans by YAML keys (e.g., `phases`, `tasks`)" | "Navigate plans by `##` section headers (e.g., `## Execution Plan`, `### Phase N`)" |
| pch-plan-reviewer | line 22 | "navigate by YAML keys (e.g., `phases`, `tasks`, `technical_design`)" | "navigate by `##` section headers (e.g., `## Technical Design`, `### Phase N`)" |

**Change Type 4: Status Value Updates**
Replace "plain text status values" references with emoji status format.

| Agent | Location | Change |
|-------|----------|--------|
| pch-visionary | lines 1237-1242 | Replace plain text status list with emoji status list |
| pch-researcher | lines 1493-1498 | Replace plain text status list with emoji status list |
| pch-planner (mode) | various | Update status references in mode instructions |

**Change Type 5: Output Format Updates (Reviewer)**
Replace YAML output templates with Markdown equivalents.

| Structure | Current Format | New Format |
|-----------|---------------|------------|
| review_session_log | YAML with entries[] array | `## Review Session Log` table: # / Issue / Category / Decision / Plan Update |
| review_summary | Mixed YAML + Markdown | `## Review Summary` with bold labels + bullet lists (already partially Markdown) |
| handoff section | YAML key-value | `## Handoff` table: Field / Value |
| version history append | YAML array entry | Append row to `## Version History` Markdown table |

**Change Type 6: Task Status Updates (Coder)**
Replace YAML task status format with Markdown.

| Structure | Current YAML | New Markdown |
|-----------|-------------|--------------|
| Task status per step | `- step: "3.1"\n  task: ...\n  status: complete` | Update task table rows under `### Phase N` |
| Progress summary | `progress_summary:\n  - phase: 3\n    status: complete` | Update rows in `## Progress Summary` table |

## Dependencies

| Dependency | Type | Status | Notes |
|------------|------|--------|-------|
| Research 018 | Research | ✅ Complete | Design specification for Markdown format |
| Hugh PlanPhaseParser | Runtime | ✅ Compatible | Already parses Markdown tables with emoji |
| Existing agent prompts | Files | Available | 5 agent files + copilot-instructions.md |

## Risks

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| AI generates inconsistent Markdown tables | Low | Medium | Provide explicit table templates with example rows |
| Downstream agents fail to parse new format | Medium | High | Test with real plan documents before full rollout |
| Token usage exceeds budget | Low | Low | Measured at ~74% — well within limits |
| Loss of information during template conversion | Low | High | Side-by-side comparison of YAML vs Markdown templates |

## Progress Summary

| Phase | Name | Status | Tasks |
|-------|------|--------|-------|
| 1 | pch-researcher Template Conversion | ✅ Complete | 6/6 |
| 2 | pch-visionary Template Conversion | ✅ Complete | 6/6 |
| 3 | pch-plan-reviewer Output & Navigation | ✅ Complete | 5/5 |
| 4 | pch-coder Navigation & Status Updates | ✅ Complete | 6/6 |
| 5 | pch-planner Template & copilot-instructions | ✅ Complete | 7/7 |

## Execution Plan

### Phase 1: pch-researcher Template Conversion

**Size:** Medium (6 tasks, 1 file)  
**Prerequisites:** None  
**Entry Point:** `agents/pch-researcher.md`  
**Verification:** N/A (first phase)

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|--------------------|
| 1.1 | Replace "AI-Optimized Format" section header and description (lines 1391-1393) | `agents/pch-researcher.md` | Section titled "## Research Document Structure" / "### Document Format"; description references compact Markdown format instead of YAML |
| 1.2 | Replace YAML research document template (lines 1395-1488) with Markdown template | `agents/pch-researcher.md` | Template uses --- metadata block; ## sections for Introduction, Objectives, Research Phases (table), ## Phase N subsections with prose findings, Key Discoveries (bullets), Files Analyzed (table), Gaps, Assumptions; ## Overview; ## Key Findings; ## Handoff (table) |
| 1.3 | Replace Status Values section (lines 1493-1498) with emoji status values | `agents/pch-researcher.md` | Lists: ⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked, ⚠️ Partial |
| 1.4 | Update Incremental Document Building instructions (lines 106-110) | `agents/pch-researcher.md` | "YAML section" → "## section"; "YAML key(s)" → "section under the appropriate ## header"; "YAML content" → "Markdown content" |
| 1.5 | Update subagent prompt template and orchestrator document update instructions to reference Markdown format (~lines 422-670) | `agents/pch-researcher.md` | Subagent instructions reference ## sections and Markdown tables instead of YAML keys; orchestrator "Updating the Research Document" examples (lines 628-656) use Markdown sections/tables instead of YAML blocks |
| 1.6 | Verify no remaining YAML document format references in pch-researcher.md | `agents/pch-researcher.md` | grep for "YAML" returns only: agent file YAML front matter references, *.yaml in excluded file types, and YAML front matter parsing mentions — no document template/update references |

### Phase 2: pch-visionary Template Conversion

**Size:** Medium (6 tasks, 1 file)  
**Prerequisites:** Phase 1 complete (validates the approach)  
**Entry Point:** `agents/pch-visionary.md`  
**Verification:** Phase 1 completed successfully; researcher template is Markdown

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 2.1 | Replace "AI-Optimized Format" section header and description (lines 1082-1086) | `agents/pch-visionary.md` | Section titled "### Document Format"; description references compact Markdown format instead of YAML |
| 2.2 | Replace YAML vision document template (lines 1088-1230) with Markdown template | `agents/pch-visionary.md` | Template uses --- metadata block; ## sections for Introduction, Problem Space, Stakeholders (table), Goals (bullets with IDs), Requirements (tables for functional/non-functional/constraints), Architecture Vision (tables for tech decisions, integrations, security), Product Phasing (summary table + ### Phase N subsections with scope/research topics/done criteria as bullets), Research Topics (table), Risks (table), Decisions Log (table), Handoff (table) |
| 2.3 | Replace Status Values section (lines 1237-1242) with emoji status values | `agents/pch-visionary.md` | Lists: ⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked |
| 2.4 | Update Incremental Document Building instructions (lines 296-300) | `agents/pch-visionary.md` | "YAML section" → "## section"; "YAML key(s)" → "section under the appropriate ## header"; "YAML content" → "Markdown content" |
| 2.5 | Update "decisions list in the YAML document" reference (line 228, 290) | `agents/pch-visionary.md` | References "## Decisions Log table" instead of "decisions list in the YAML document" |
| 2.6 | Verify no remaining YAML document format references exist | `agents/pch-visionary.md` | grep for "YAML" returns only: agent file YAML front matter references, *.yaml in excluded file types, and YAML front matter parsing mentions — no document template references |

### Phase 3: pch-plan-reviewer Output & Navigation

**Size:** Small (5 tasks, 1 file)  
**Prerequisites:** Phase 2 complete  
**Entry Point:** `agents/pch-plan-reviewer.md`  
**Verification:** Phases 1-2 completed; researcher and visionary templates are Markdown

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 3.1 | Replace YAML navigation note (line 22) with Markdown header navigation | `agents/pch-plan-reviewer.md` | Note reads: "Implementation plans use Markdown format with ## section headers. When reviewing plans, navigate by section headers (e.g., `## Technical Design`, `### Phase N`) rather than searching for keys." |
| 3.2 | Replace YAML review_session_log format (lines 377-399) with Markdown table | `agents/pch-plan-reviewer.md` | Format shows `## Review Session Log` with table: # / Issue / Category / Decision / Plan Update; includes bold labels for questions_pending, questions_resolved, last_updated above the table |
| 3.3 | Replace YAML handoff section format (lines 562-570) with Markdown table | `agents/pch-plan-reviewer.md` | Handoff uses `## Handoff` table: Field / Value rows for created_by, created_date, reviewed_by, review_date, status, next_agent, plan_location |
| 3.4 | Update version history append format to show Markdown table row | `agents/pch-plan-reviewer.md` | Version history example shows appending a row to existing Markdown table (pipe-delimited) instead of YAML array entry |
| 3.5 | Verify no remaining YAML document format references | `agents/pch-plan-reviewer.md` | grep for "YAML" returns only agent file YAML front matter references — no document format references |

### Phase 4: pch-coder Navigation & Status Updates

**Size:** Small (4 tasks, 1 file)  
**Prerequisites:** Phase 3 complete  
**Entry Point:** `agents/pch-coder.md`  
**Verification:** Phases 1-3 completed; reviewer updated

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 4.1 | Replace YAML navigation note (line 14) with Markdown header navigation | `agents/pch-coder.md` | Note reads: "Implementation plans use Markdown format with ## section headers. Navigate plans by section headers (e.g., `## Execution Plan`, `### Phase N`) and use emoji status values (⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked)." |
| 4.2 | Replace YAML task status update format (lines 344-357) with Markdown table row updates | `agents/pch-coder.md` | Shows updating task rows within Markdown tables under `### Phase N`: changing status column value; preserving step IDs (e.g., "3.1") |
| 4.3 | Replace YAML progress_summary update format (lines 380-385) with Markdown table row update | `agents/pch-coder.md` | Shows updating rows in `## Progress Summary` Markdown table; uses emoji status values |
| 4.4 | Verify no remaining YAML document format references | `agents/pch-coder.md` | grep for "YAML" returns only agent file YAML front matter references — no document format references |
| 4.5 | Replace YAML progress tracking format (lines 703-715) with Markdown table format | `agents/pch-coder.md` | Subagent progress tracking section uses Markdown table rows under `### Phase N` instead of YAML task list; status values use emoji |
| 4.6 | Replace YAML deployment notes format (lines 1102-1115) with Markdown table/bold-label format | `agents/pch-coder.md` | deployment_checkpoint uses Markdown table or bold-label format instead of YAML block; verification steps use table rows |

### Phase 5: pch-planner Template, copilot-instructions & README

**Size:** Medium (7 tasks, 3 files)  
**Prerequisites:** Phases 1-4 complete (all other agents updated)  
**Entry Point:** `agents/pch-planner.md`  
**Verification:** Phases 1-4 completed; all other agents use Markdown format

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 5.1 | Replace "AI-Optimized Format" section and YAML key list (lines 358-375) with Markdown section spec | `agents/pch-planner.md` | Section header "### Document Format"; description references Markdown format; lists required ## sections instead of YAML keys |
| 5.2 | Replace YAML version history example (lines 387-397) with Markdown table example | `agents/pch-planner.md` | Shows `## Version History` Markdown table: Version / Date / Author / Changes |
| 5.3 | Replace YAML phase template (lines 444-456) with Markdown phase template | `agents/pch-planner.md` | Template shows `### Phase N` with metadata as bold labels + task table: Step / Task / Files / Acceptance Criteria; uses emoji status |
| 5.4 | Replace YAML handoff metadata (lines 561-567) with Markdown handoff table | `agents/pch-planner.md` | Handoff uses `## Handoff` table: Field / Value rows |
| 5.5 | Update Incremental Document Building instructions (lines 114-119) | `agents/pch-planner.md` | "YAML section" → "## section"; "YAML key(s)" → "section under the appropriate ## header"; "YAML content" → "Markdown content" |
| 5.6 | Update copilot-instructions.md: session log structure and status value references | `.github/copilot-instructions.md` | Lines 159-163: Progress Status Values updated to include emoji format (⏳ Not Started, 🔄 In Progress, ✅ Complete, ❌ Blocked); Lines 168-180: Session log YAML code block replaced with Markdown table example (# / Decision Point / Answer / Rationale); Do NOT modify YAML front matter references at lines 35-71, 110-118, 150 — those describe agent file structure, not document format |
| 5.7 | Update README.md: AI-Optimized Document Format section (lines 262-265) | `README.md` | References "compact Markdown format" instead of "YAML format"; status values list includes emoji; removes "top-level keys provide machine-readable navigation" |

## Holistic Review

### Decision Interactions

1. **Metadata (D1) + Status (D2):** YAML front matter metadata block uses flat key-value (reliable), while emoji status values appear inside Markdown tables. These are complementary — metadata `status:` field uses emoji text (e.g., `status: 🔄 In Progress`), and table cells use the same emoji. No conflict.

2. **Markdown-Specific Instructions (D3) + No Backward Compat (D4):** By not adding dual-format instructions, the Markdown-specific incremental building guidance can be fully committed to Markdown constructs without hedging. This makes instructions clearer and shorter. Risk: agents reading old YAML docs won't have format hints, but AI models handle both formats naturally.

3. **Task Table (D5) + planner-last (D6):** The task table format (Step / Task / Files / Acceptance Criteria) is used in this very plan document. Since pch-planner is updated last, the coder implementing this plan reads YAML-format planner instructions but follows this plan's Markdown task tables — the plan document is the authoritative spec, so this is safe.

### Architectural Considerations

- **Hugh PlanPhaseParser compatibility:** Confirmed compatible. The parser uses regex `\|\s*(\d+)\s*\|\s*([^|]+)\|\s*(✅|🔄|⏳|❌)\s*([^|]*)\|\s*(\d+)/(\d+)\s*\|` which matches the `## Progress Summary` table format specified in this plan. Zero code changes needed.

- **Subagent return format:** Text-based (STATUS:, TASKS_COMPLETED:, etc.) — completely independent of document format. No changes needed.

- **Agent file YAML front matter:** Agent `.md` files use YAML front matter (`---` delimited) for agent metadata (name, description, model, handoffs). This is unrelated to document format and must NOT be changed.

### Trade-offs Accepted

- **7% token increase vs YAML:** Accepted. Markdown tables use slightly more tokens than equivalent YAML, but elimination of YAML malformation errors makes this worthwhile.
- **No backward compatibility notes:** Accepted. Simpler prompts at the cost of agents not having explicit guidance when reading legacy YAML documents. AI models interpret both formats well.
- **Concise task table cells:** Accepted. Semicolon-separated acceptance criteria in table cells are less readable than bullet lists but maintain the 2-level header maximum.

### Risks Acknowledged

- **Self-referential pch-planner update (Phase 5):** Mitigated by processing planner last and having the plan document serve as the authoritative format specification.
- **Inconsistent table generation:** Mitigated by providing explicit template examples with populated sample rows in each agent prompt.
- **copilot-instructions.md scope:** The copilot-instructions.md file is large and contains many YAML examples for session logs and status values. Task 5.6 must carefully update only document-format references, not agent-file YAML front matter references.

## Standards

No organizational standards applicable to this plan.

## Implementation Notes for Coder

The following minor items should be kept in mind during implementation:

1. **pch-researcher.md lines 217 and 1303/1670:** Additional ```` ```yaml ```` code blocks appear in the structured communication / subagent return format sections. These are inter-agent communication formats (not document templates) and are out of scope — do NOT change them. The Task 1.6 verification step will confirm these are correctly excluded.
2. **pch-coder.md lines 703 and 1102:** These were not in the original codebase patterns table but are now covered by Tasks 4.5 and 4.6. Read the surrounding context (±20 lines) before editing to understand the section structure.
3. **Emoji rendering:** Ensure emoji characters (⏳🔄✅❌⚠️) are inserted as actual Unicode characters, not as `:emoji_name:` shortcodes, to match PlanPhaseParser regex expectations.

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 2 | 7 files across agents + config |
| New patterns introduced | 1 | Markdown tables replace YAML — no new patterns |
| External dependencies | 1 | None |
| Migration complexity | 1 | No migration; new format only |
| Test coverage required | 2 | Manual validation of agent behavior; PlanPhaseParser already tested |
| **Overall Complexity** | **7/25** | **Low** |

## Review Summary

**Review Date:** 2025-02-11
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 6 (Critical: 0, Major: 3, Minor: 3)
- Clarifying Questions Asked: 3
- Sections Updated: Phase 1 (Tasks 1.5, 1.6), Phase 4 (Tasks 4.5, 4.6 added), Phase 5 (Task 5.6), Codebase Patterns, Progress Summary

### Key Improvements Made
1. Added Tasks 4.5 and 4.6 to Phase 4 covering two missed YAML blocks in pch-coder.md (Progress Tracking Format and Deployment Notes Format)
2. Expanded Task 1.5 line range from ~422-486 to ~422-670 to cover orchestrator document update YAML examples; converted Task 1.6 to verification step for consistency across phases
3. Refined Task 5.6 acceptance criteria with explicit line references and exclusion list to prevent accidental modification of legitimate YAML front matter instructions in copilot-instructions.md

### Remaining Considerations
- Minor items documented in "Implementation Notes for Coder" section above
- All phases remain appropriately sized (3-7 tasks each) after additions

### Sign-off
This plan has been reviewed and is **Ready for Implementation**

## Handoff

| Field | Value |
|-------|-------|
| Created by | pch-planner |
| Created date | 2025-02-11 |
| Reviewed by | pch-plan-reviewer |
| Review date | 2025-02-11 |
| Status | ready-for-implementation |
| Next agent | pch-coder |
| Plan location | /docs/plans/020-markdown-document-format-for-pch-agents.md |
