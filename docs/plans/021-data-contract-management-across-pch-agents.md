---
id: "021"
type: plan
title: "Data Contract Management Across PCH Agents"
status: ✅ Complete
created: "2026-02-17"
updated: "2026-02-17"
owner: "pch-planner"
version: "2.3"
---

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-17 | pch-planner | Initial plan skeleton |
| v1.1 | 2026-02-17 | pch-planner | Decision 1: no directory scaffolding phase; create-if-not-exists guard added to pch-visionary |
| v2.0 | 2026-02-17 | pch-planner | Holistic review completed; phases renumbered 1-5; architectural considerations documented |
| v2.1 | 2026-02-17 | pch-planner | Decision 2: pch-coder updates contracts when user accepts drift; Step 5.6 added to Phase 5 |
| v2.2 | 2026-02-17 | pch-plan-reviewer | Added Review Session Log; resolved naming convention inconsistency (dc-NNN canonical); added Architectural Considerations item 5; updated Step 1.2–1.3 acceptance criteria with naming overrides and table column correction |
| v2.3 | 2026-02-17 | pch-plan-reviewer | Added Phase 0 pre-read step; Execution Plan sequencing note; Implementation Complexity table; Review Summary; Handoff updated to Ready for Implementation |

## Introduction

This plan implements the data contract management system across all five PCH agents (pch-visionary, pch-researcher, pch-planner, pch-plan-reviewer, pch-coder) based on the findings of Research 019. The implementation consists entirely of additions and modifications to the five agent prompt Markdown files in `/agents/`, plus creation of the `/docs/data-contracts/` directory with a README. All decisions, exact insertion text, and lifecycle design have been pre-resolved by Research 019 — this plan translates those findings directly into executable implementation tasks.

**Research Source:** [docs/research/019-data-contract-management-across-pch-agents.md](../research/019-data-contract-management-across-pch-agents.md)

## Planning Session Log

| # | Decision Point | Answer | Rationale |
|---|---------------|--------|-----------|
| 1 | `/docs/data-contracts/` initialization | C — No placeholder file; add "create if not exists" guard to pch-visionary | Avoids an extra scaffolding phase. The directory creates itself on first use when pch-visionary runs its contract creation step. Agent instructions stay self-contained. |
| 2 | Who updates contracts after code changes | B — pch-coder updates contracts when the user accepts drift at the post-sweep decision point | Contracts should always reflect the as-built state after an implementation run ends. Routing every deviation back through pch-planner adds friction for a mechanical update. pch-coder already has the code and contract in context. The update is still gated on an explicit user decision (not automatic), preventing silent schema drift. |

## Review Session Log

**Questions Pending:** 0
**Questions Resolved:** 2
**Last Updated:** 2026-02-17

| # | Issue | Category | Decision | Plan Update |
|---|-------|----------|----------|-------------|
| 1 | Naming convention inconsistency (dc-NNN vs NNN) between Research 019 Phase 2 and Phase 3 | Correctness | Option C — note in Architectural Considerations + override in Phase 1 steps 1.2 and 1.3 | Architectural Considerations item 5 added; Step 1.2 and 1.3 acceptance criteria updated |
| 2 | Where to add Research 019 read step | Specificity | Option A — dedicated Phase 0 pre-read step before all phases | Phase 0 added to Execution Plan |

## Holistic Review

### Decision Interactions

| Decision | Impact on Other Decisions |
|----------|--------------------------|
| C — No scaffolding file; create-if-not-exists guard | Shifts directory creation responsibility from a dedicated phase to pch-visionary's instruction set. pch-planner Step 3.2 must also gracefully handle a missing `/docs/data-contracts/` directory (added to acceptance criteria). No other agent is affected since only pch-visionary ever creates contracts initially. |
| B — pch-coder updates contracts on accepted drift | pch-coder now has a limited write role on contract files, breaking the "only visionary and planner write contracts" rule from Research 019. Scope is tightly bounded: coder may only update field rows, version, Version History, Breaking Change Log, and Handoff fields — triggered exclusively by the user choosing "Accept drift" at the post-sweep decision point. The create-if-not-exists guard (Decision 1) means the contract file is guaranteed to exist before coder ever runs. |

### Architectural Considerations

1. **Sequential ownership (updated):** The contract lifecycle is create → read → activate → validate → enforce → update-on-acceptance. pch-coder now has a bounded write role, but only after all other agents have completed. The workflow is still strictly sequential; no concurrent writes are possible. pch-coder's write scope is intentionally narrow: field rows for accepted drift, version bump, Version History row, Breaking Change Log row, and Handoff update.

2. **Pseudo-template notation in subagent prompts:** Research 019 uses `{if_data_contracts_exist}` pseudo-template syntax for illustration. The actual pch-researcher and pch-coder subagent prompts use prose instructions, not template engines. Acceptance criteria in Phases 2 and 5 specify "conditional" and "only when relevant contracts exist" in prose — pch-coder should implement as an `if` instruction rather than a template macro.

3. **Token budget (pch-coder):** Phase 5 Step 5.2 specifies embedding contracts only for entities touched by the current phase. This is critical for large contracts or many-entity plans. The acceptance criteria explicitly includes this constraint.

4. **Fallback coverage:** All phases that interact with `/docs/data-contracts/` include graceful fallbacks (skip and note) for the case where the directory doesn't exist or contains no relevant contracts. This prevents any new instruction from blocking an existing workflow that doesn't use data contracts.

5. **Filename convention — authoritative format is `dc-NNN-{domain}.md`:** Research 019 is internally inconsistent. Phase 2 (template design) uses `dc-NNN-{domain}.md` (e.g., `dc-001-order-domain.md`). Phase 3 (agent integration) uses bare `NNN-{domain}.md` (e.g., `NNN-*.md`, `NNN-{domain-name}.md`). **The `dc-NNN-{domain}.md` form is canonical.** The bare `NNN-{domain}` form in Research 019 Phase 3 is an inconsistency in the research document and must be ignored. All phases that create, reference, or embed contract file paths must use `dc-NNN-{domain}.md`.

### Trade-offs Accepted

- **No README in `/docs/data-contracts/`:** The template lives only in Research 019. Developers must know to consult the research document to understand the directory's format. Accepted because the research is the canonical source of truth and duplication creates divergence risk.
- **C# type mapping only:** Non-C# language drift detection is out of scope. Accepted per Research 019 Phase 4 scope definition.
- **pch-coder writes contracts only on explicit user acceptance:** Automatic contract overwriting (Option D) was rejected because it would defeat the purpose of drift detection. The "Accept drift" gate ensures a human decision precedes every coder-initiated contract change.

### Risks Acknowledged

- If pch-visionary's tool environment doesn't support creating nested directories automatically, the create-if-not-exists guard may fail silently. Mitigation: the guard instruction explicitly states to create the directory if absent.
- Per-phase drift detection requires the subagent to receive the contract in its prompt context. If contracts are large (many entities), this adds tokens. Mitigation: embed only relevant entities per phase.

## Overview

### Feature Summary

Inject data contract awareness into each PCH agent at its appropriate lifecycle stage so that:
- Named data entities declared during vision Q&A are captured in a persistent, machine-readable Markdown contract document
- Research subagents use declared entity schemas instead of discovering them independently
- Plans reference contracts as the authoritative source of schema truth
- Plan reviews verify contract references are complete and authoritative
- Coder subagents detect and report drift between declared contracts and actual code

### Objectives

- Create `/docs/data-contracts/` as a new artifact directory in the PCH document hierarchy
- Define a standard Markdown data contract template for the directory (per Research 019 Phase 2)
- Modify pch-visionary to create draft data contracts after Stage 5 Architecture Q&A
- Modify pch-researcher to read and embed contract context in subagent prompts
- Modify pch-planner to discover, reference, and promote contracts to `active` status
- Modify pch-plan-reviewer to validate contract references in plans
- Modify pch-coder to perform per-phase and post-implementation drift detection

### Success Criteria

- A vision session that identifies data entities produces a contract file in `/docs/data-contracts/` with status `draft`
- A plan document for an entity-bearing feature contains a `### Data Contracts` table in `## Technical Design`
- All contracts referenced in a plan have `status: active` and a populated `source_file` before pch-coder handoff
- pch-coder subagents report drift findings in the `DEVIATIONS` field using `[drift-high]`/`[drift-medium]` prefixes
- The post-implementation code review includes a 4th category for contract drift

## Requirements

### Functional

- pch-visionary creates `/docs/data-contracts/dc-NNN-{domain}.md` after Stage 5 when persistent data entities are present
- pch-visionary records created contracts in the vision document's `## Data Contracts Created` table
- pch-researcher reads vision doc's `## Data Contracts Created` table during Orchestration Loop Step 1
- pch-researcher embeds relevant contract content in subagent prompts under `## DATA CONTRACT CONTEXT`
- pch-planner lists `/docs/data-contracts/` and reads relevant contracts during Context Gathering Step 4.5
- pch-planner populates `### Data Contracts` table in `## Technical Design` section
- pch-planner updates contract `source_file` and promotes status `draft → active` during Workflow Step 4.5
- pch-plan-reviewer checks contract completeness, status, and source_file population
- pch-coder orchestrator embeds contract content in per-phase subagent prompts
- pch-coder subagents compare code against contract and report drift in `DEVIATIONS` field
- pch-coder post-implementation review sweeps all contracts as 4th review category
- HIGH-severity drift findings pause the workflow and require user decision before final completion

### Non-Functional

- All agent prompt modifications follow the existing single-section-per-response rule (incremental)
- Contract files use the same YAML front matter + Markdown body structure as all other PCH artifacts
- Status values use the shared emoji vocabulary (⏳/🔄/✅/❌/⚠️)
- Agent modifications do not change or break any existing agent behavior — only additive changes
- All new instructions include fallback behavior for cases where no data contracts exist

### Out of Scope

- Automated schema validation tooling (CI pipelines, schema registries, ODCS CLI tools)
- Non-C# language type mapping (C# is the assumed target language per Research 019 Phase 4)
- Contract-to-code migration scripts or automated drift fixing
- Modifications to pch-helper.md or hugh.md
- Contract versioning beyond what is documented in the contract file itself

## Technical Design

### Architecture

The data contract system is a cross-cutting overlay on the existing PCH agent workflow. It introduces one new artifact type (`data-contract`) and one new directory (`/docs/data-contracts/`), and adds instructions to five existing agent prompt files. No new agent files are created. No tooling is added.

```
/docs/
  data-contracts/      ← NEW: one file per domain (dc-NNN-{domain}.md)
                          (created by pch-visionary on first use; no placeholder file)
/agents/
  pch-visionary.md     ← MODIFIED: Stage 5 contract creation + create-if-not-exists guard
  pch-researcher.md    ← MODIFIED: Orchestration Loop + Subagent Invocation
  pch-planner.md       ← MODIFIED: Context Gathering + Plan Structure + Workflow
  pch-plan-reviewer.md ← MODIFIED: Review Checklist + Common Defects
  pch-coder.md         ← MODIFIED: Subagent Invocation + Post-Impl Review + Contract update on accepted drift
```

### Data Contract File Format

Contract files follow the PCH Markdown artifact convention (YAML front matter + Markdown body). Key structural elements:

**Front matter fields:**
- `id`: `"dc-NNN"` matching filename prefix
- `type`: `data-contract`
- `title`: Human-readable name
- `status`: Lifecycle emoji (⏳/🔄/✅)
- `created`, `updated`, `version`: Standard PCH fields
- `domain`: Business/technical domain
- `owner`: Accountable team
- `steward`: Responsible individual

**Body sections:**
- `## Overview` — contract purpose, consumers, vision source link
- `## Breaking Change Policy` — SemVer rules (MAJOR/MINOR/PATCH)
- `## Entities` — one `### Entity: Name` subsection per entity
- Each entity section: physical name, source_file, status, field table
- Field table columns: Field | Type | Physical Type | Required | Unique | Description | Constraints | Example
- `## Version History` — version table
- `## Breaking Change Log` — audit trail
- `## Standards Applied` — universal PCH footer
- `## Handoff` — universal PCH footer

**File naming:** `dc-NNN-{domain-name}.md` where NNN uses the `max + 1` pattern from listing `/docs/data-contracts/`.

### Codebase Patterns

```yaml
codebase_patterns:
  - pattern: YAML Front Matter + Markdown Body
    location: "agents/*.md, docs/vision/*.md, docs/research/*.md, docs/plans/*.md"
    usage: All new contract files follow this convention identically
  - pattern: Zero-padded 3-digit sequential numbering
    location: "docs/vision/, docs/research/, docs/plans/"
    usage: /docs/data-contracts/ uses same max+1 pattern; dc-NNN prefix distinguishes from other directories
  - pattern: Full document embedding in subagent prompts
    location: "agents/pch-researcher.md, agents/pch-coder.md"
    usage: Contract content embedded in ## DATA CONTRACT CONTEXT block, parallel to existing ## VISION DOCUMENT CONTEXT
  - pattern: Mandatory pre-phase document read
    location: "agents/pch-researcher.md (Orchestration Loop step 1), agents/pch-coder.md"
    usage: Contract loading is added as a sub-step of the existing mandatory load step
  - pattern: Fallback behavior for unavailable external resources
    location: "agents/pch-planner.md (pch-standards-space fallback)"
    usage: All new contract instructions include "skip and note" fallback when no contracts exist
```

### Data Contracts

_This plan modifies agent prompt files (no data entities). Data contracts not applicable._

No data entities in scope — data contracts not applicable.

### Agent Integration Points (Per Research 019, Phase 3)

#### pch-visionary.md Integration Points

| Location | Change Type | Description |
|----------|-------------|-------------|
| `## Six-Stage Question Flow` → after Stage 5 questions | INSERT new subsection | `### Data Contract Creation (Stage 5: Post-Architecture)` with trigger conditions, creation procedure, and fallback |
| `## Vision Completion Checklist` → Required Sections | INSERT checklist item | Require `## Data Contracts Created` section when entities identified |
| `## Vision Document Template` | INSERT new section | Add `## Data Contracts Created` table template |

#### pch-researcher.md Integration Points

| Location | Change Type | Description |
|----------|-------------|-------------|
| `## Automatic Mode Workflow` → `### Orchestration Loop` step 1 | INSERT sub-step | Load vision doc `## Data Contracts Created`; read and embed contract files |
| `### Subagent Invocation` prompt template | INSERT optional block | `## DATA CONTRACT CONTEXT` block after `{end_if_vision_source}` |

#### pch-planner.md Integration Points

| Location | Change Type | Description |
|----------|-------------|-------------|
| `## Context Gathering (Step 0)` after step 4 | INSERT new step | Step 4.5: Contract Discovery |
| `## Plan Structure` → `## Technical Design` description | INSERT guidance | `### Data Contracts` table format and rules |
| `## Workflow Process` after Step 4 | INSERT new step | Step 4.5: Update contract source_file and promote to active |

#### pch-plan-reviewer.md Integration Points

| Location | Change Type | Description |
|----------|-------------|-------------|
| `## Review Checklist` → `### Correctness` | INSERT 3 checklist items | Contract completeness, active status, source_file checks |
| `## Review Checklist` → `### Completeness` | INSERT 2 checklist items | Data Contracts table presence, contract path validity |
| `## Common Plan Defects to Watch For` | INSERT new subsection | `### Data Contract Issues` with 5 defect types + severities |

#### pch-coder.md Integration Points

| Location | Change Type | Description |
|----------|-------------|-------------|
| `### Subagent Invocation` → `## YOUR INSTRUCTIONS` block | INSERT new section | `**Data Contract Drift Detection (if applicable):**` block |
| `## Post-Implementation Code Review` subagent prompt | INSERT 4th category | Contract drift final sweep category |
| `## Post-Implementation Code Review` output format | EXTEND | Add `CONTRACT_DRIFT_SUMMARY` field and `[drift-high]`/`[drift-medium]` FINDINGS categories |

### Drift Detection Design

**5 drift categories (all pch-coder subagents check these):**

| # | Category | Severity | Detection Method |
|---|----------|----------|-----------------|
| 1 | Field in contract, missing from code | HIGH | Property absent from class |
| 2 | Field in code, missing from contract | MEDIUM | Extra property not in field table |
| 3 | Type mismatch | HIGH | `Physical Type` differs from C# declared type |
| 4 | Nullable mismatch | HIGH | `Required: Yes` but `?` suffix present (or vice versa) |
| 5 | Constraint declared, not enforced | MEDIUM | Data annotation or EF config absent |

**Name matching:** Case-insensitive (`totalAmount` matches `TotalAmount`).
**Audit field exemption:** `CreatedAt`, `UpdatedAt`, `IsDeleted`, `RowVersion` exempt from undocumented-field category.
**Partial classes:** Also check `{ClassName}.*.cs` files in same directory.
**Inheritance:** Read one level up in inheritance chain.

**Blocking behavior:**
- Per-phase: never block; report in `DEVIATIONS` field
- Post-sweep: HIGH findings pause before final completion; user chooses one of:
  - **Fix** — pch-coder corrects the code; no contract change
  - **Accept drift** — pch-coder updates the contract to match the as-built code (bumps version, logs in Breaking Change Log if MAJOR, updates Handoff)
  - **Update contract via planner** — defers to a new pch-planner session (for cases requiring broader replanning)
- MEDIUM findings (undocumented fields, missing constraints): offered as a secondary confirmation after HIGH resolution; "Accept" → pch-coder adds missing field rows or constraint notes to contract with MINOR version bump

## Dependencies

| Dependency | Type | Status | Notes |
|------------|------|--------|-------|
| Research 019 (`docs/research/019-data-contract-management-across-pch-agents.md`) | Document | ✅ Complete | All design decisions resolved; provides exact insertion text |
| `/docs/data-contracts/` directory | Infrastructure | ⏳ Not Created | Created automatically by pch-visionary on first contract creation (create-if-not-exists guard in Phase 1 of this plan) |
| `agents/pch-visionary.md` | File | ✅ Exists | Must be read before modification |
| `agents/pch-researcher.md` | File | ✅ Exists | Must be read before modification |
| `agents/pch-planner.md` | File | ✅ Exists | Must be read before modification |
| `agents/pch-plan-reviewer.md` | File | ✅ Exists | Must be read before modification |
| `agents/pch-coder.md` | File | ✅ Exists | Must be read before modification |

## Risks

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Agent prompt additions cause large response errors | Low | Medium | Each insertion is a single focused block; research has pre-written exact text |
| Insertion point text not found exactly (edit fails) | Medium | Low | Read each file immediately before editing; use surrounding context for matching |
| New instructions conflict with existing behavior | Low | Medium | All changes are additive; fallback behavior included for cases with no contracts |
| pch-visionary creates contracts for non-entity features | Low | Low | Clear trigger conditions defined; "no entities" fallback documented |

## Execution Plan

> **Note on phase sequencing:** Phases 1–5 each modify a different, independent agent file and could logically run in any order. They are sequenced 1→5 to match the contract lifecycle (create → read → plan → review → implement), making the implementation order self-documenting and reducing cognitive load. pch-coder should complete one phase fully before starting the next to avoid large response errors.

### Phase 0: Pre-Read Research 019

**Status:** ✅ Complete
**Size:** Minimal
**Files to Modify:** None
**Prerequisites:** None
**Purpose:** Load the pre-written insertion text from Research 019 into context before making any edits. This ensures pch-coder uses the researched text rather than generating new text from the acceptance criteria alone.

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 0.1 | Read `docs/research/019-data-contract-management-across-pch-agents.md` in full | `docs/research/019-data-contract-management-across-pch-agents.md` | Key insertion blocks identified for all five agents: Stage 5 contract creation block (Phase 1), Orchestration Loop sub-step and DATA CONTRACT CONTEXT block (Phase 2), Context Gathering step and Plan Structure guidance (Phase 3), Review Checklist items and defect subsection (Phase 4), Subagent drift detection block and Post-Impl review category (Phase 5). **Apply Architectural Consideration #5:** wherever Research 019 uses bare `NNN-{domain}.md`, substitute `dc-NNN-{domain}.md` in the final inserted text. |

### Phase 1: Modify pch-visionary.md

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 0 complete
**Entry Point:** `agents/pch-visionary.md` — `## Six-Stage Question Flow`
**Verification:** File contains `### Data Contract Creation (Stage 5: Post-Architecture)` section with create-if-not-exists guard; Vision Document Template contains `## Data Contracts Created` table; Completion Checklist updated

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 1.1 | Read `agents/pch-visionary.md` fully to locate all insertion points | `agents/pch-visionary.md` | Exact insertion point text identified for Stage 5 and both template locations |
| 1.2 | Insert `### Data Contract Creation (Stage 5: Post-Architecture)` subsection after all Stage 5 question templates and before `### Stage 6` | `agents/pch-visionary.md` | Section present with: trigger conditions (named domain objects, API bodies, DTOs); **create-if-not-exists guard** ("If `/docs/data-contracts/` does not exist, create it before creating the contract file"); 7-step creation procedure; `source_file: TBD` instruction; user validation question requirement; "no contracts needed" fallback; unknown-entity-structure fallback. **⚠️ Naming override:** Use Research 019 Phase 2 text (`dc-NNN-{domain}.md`) for all filename references; the bare `NNN-{domain}` form in Research 019 Phase 3 is incorrect and must not be used. |
| 1.3 | Add `## Data Contracts Created` table to `## Vision Document Template` | `agents/pch-visionary.md` | Template section includes the **4-column** table format (Contract File, Entities, Status, Vision Phase) matching Research 019 Phase 2 — column "Notes" is omitted (not present in Research 019). **⚠️ Naming override:** Contract File column values must use `dc-NNN-{domain}.md` format; not the bare `NNN-domain.md` shown in Research 019 Phase 3. Includes explicit "no entities" fallback prose. |
| 1.4 | Add `## Data Contracts Created` requirement to `## Vision Completion Checklist` | `agents/pch-visionary.md` | Checklist item reads: "Data Contracts Created section present (or explicit 'not required' note)" |

### Phase 2: Modify pch-researcher.md

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 1 complete
**Entry Point:** `agents/pch-researcher.md` — `## Automatic Mode Workflow` → `### Orchestration Loop`
**Verification:** Orchestration Loop step 1 includes contract loading sub-step; Subagent Invocation template contains `## DATA CONTRACT CONTEXT` block

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 2.1 | Read `agents/pch-researcher.md` fully to locate Orchestration Loop step 1 and Subagent Invocation template | `agents/pch-researcher.md` | Exact text of step 1 and template boundary identified |
| 2.2 | Add contract loading sub-step to Orchestration Loop step 1 | `agents/pch-researcher.md` | Step 1 includes: check vision doc `## Data Contracts Created` table; read each referenced contract from `/docs/data-contracts/`; note which are relevant to the current phase |
| 2.3 | Add `## DATA CONTRACT CONTEXT` optional block to Subagent Invocation prompt template | `agents/pch-researcher.md` | Block is conditional (only included when relevant contracts exist); parallel to existing `## VISION DOCUMENT CONTEXT` pattern; instructs subagent to use declared field names and flag existing code that diverges from declared entity structure |

### Phase 3: Modify pch-planner.md

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 2 complete
**Entry Point:** `agents/pch-planner.md` — `## Context Gathering (Step 0)`
**Verification:** Context Gathering has Step 4.5; Plan Structure Technical Design guidance includes `### Data Contracts` table format; Workflow Process has Step 4.5 for contract promotion

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 3.1 | Read `agents/pch-planner.md` fully to locate the three insertion points | `agents/pch-planner.md` | Exact text surrounding Step 0 step 4, Technical Design description, and Workflow Process Step 4 identified |
| 3.2 | Insert Step 4.5 in `## Context Gathering (Step 0)` between existing step 4 (Summarize findings) and step 5 (Query Standards) | `agents/pch-planner.md` | Step 4.5 titled "Check for Data Contracts"; includes: list `/docs/data-contracts/` (gracefully handle missing directory); check vision doc `## Data Contracts Created`; read relevant contracts; record draft vs. active status and TBD source_file |
| 3.3 | Add `### Data Contracts` table guidance to `## Plan Structure` → `## Technical Design` description | `agents/pch-planner.md` | Guidance specifies exact 5-column table format (Contract File, Entity, Status, Source File, Contract Path); one row per entity; includes "no entities" fallback text |
| 3.4 | Insert Step 4.5 after Step 4 (Holistic Review) in `## Workflow Process` | `agents/pch-planner.md` | Step 4.5 titled "Update Data Contract Source Files"; includes: update `source_file` from TBD to actual path; change draft → active; update contract `## Handoff` table; create new contracts if plan introduces uncontracted entities |

### Phase 4: Modify pch-plan-reviewer.md

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 3 complete
**Entry Point:** `agents/pch-plan-reviewer.md` — `## Review Checklist`
**Verification:** Correctness checklist has 3 contract items; Completeness checklist has 2 contract items; Common Plan Defects has `### Data Contract Issues` subsection

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 4.1 | Read `agents/pch-plan-reviewer.md` fully to locate insertion points | `agents/pch-plan-reviewer.md` | `### Correctness`, `### Completeness`, and `## Common Plan Defects to Watch For` sections located with exact surrounding text |
| 4.2 | Add 3 contract checks to `## Review Checklist` → `### Correctness` | `agents/pch-plan-reviewer.md` | Three checkboxes: (1) all data entities in Technical Design have contract rows; (2) referenced contracts have `status: active`; (3) all `source_file` fields populated (not TBD) |
| 4.3 | Add 2 contract checks to `## Review Checklist` → `### Completeness` | `agents/pch-plan-reviewer.md` | Two checkboxes: (1) `### Data Contracts` table present if plan involves persistent data entities; (2) all Contract Path values point to existing files |
| 4.4 | Add `### Data Contract Issues` subsection to `## Common Plan Defects to Watch For` | `agents/pch-plan-reviewer.md` | Subsection lists 5 defect types with severity: Missing table (Major), Draft contract (Major), TBD source_file (Major), Broken contract path (Critical), Uncovered entity (Minor) |

### Phase 5: Modify pch-coder.md

**Status:** ✅ Complete
**Size:** Small
**Files to Modify:** 1
**Prerequisites:** Phase 4 complete
**Entry Point:** `agents/pch-coder.md` — `### Subagent Invocation` section
**Verification:** Orchestrator section embeds contract context; subagent `## YOUR INSTRUCTIONS` block contains drift detection section; Post-Implementation Code Review has 4th category; output format includes `CONTRACT_DRIFT_SUMMARY`

| Step | Task | Files | Acceptance Criteria |
|------|------|-------|---------------------|
| 5.1 | Read `agents/pch-coder.md` fully to locate all insertion points | `agents/pch-coder.md` | Orchestrator loop, `### Subagent Invocation` `## YOUR INSTRUCTIONS` block, `## Post-Implementation Code Review` subagent prompt, and output format section identified |
| 5.2 | Insert orchestrator contract-embedding instruction in the Orchestration Loop / `### Subagent Invocation` orchestration logic | `agents/pch-coder.md` | Orchestrator reads plan's `### Data Contracts` table; reads each referenced contract from `/docs/data-contracts/`; embeds contract content under `## DATA CONTRACT CONTEXT` for phases touching those entities only (token budget management) |
| 5.3 | Add `**Data Contract Drift Detection (if applicable):**` block to the subagent `## YOUR INSTRUCTIONS` | `agents/pch-coder.md` | Block includes: 6-step procedure; all 5 drift categories with `[drift-high]`/`[drift-medium]` labels; case-insensitive name matching; partial class and inheritance handling; "report only, do not fix" instruction; skip conditions (TBD source_file, no Data Contracts table, phase doesn't touch contract entities) |
| 5.4 | Add 4th review category to `## Post-Implementation Code Review` subagent prompt | `agents/pch-coder.md` | Category titled "Data contract drift (final sweep)"; covers all 5 drift categories; includes "no contracts declared" note |
| 5.5 | Extend Post-Implementation Code Review output format with `CONTRACT_DRIFT_SUMMARY` and HIGH-severity pause behavior | `agents/pch-coder.md` | Output format includes `CONTRACT_DRIFT_SUMMARY: N high-severity; N medium-severity`; FINDINGS include `[drift-high]`/`[drift-medium]` entries; when HIGH findings present, orchestrator pauses and presents Fix / Accept drift / Update via planner options before final completion |
| 5.6 | Add contract update procedure to orchestrator for when user chooses "Accept drift" | `agents/pch-coder.md` | On "Accept drift": orchestrator updates the affected entity's field rows in the contract to match as-built code; applies SemVer bump (MAJOR for type/nullable/removal changes; MINOR for additive); appends row to contract `## Breaking Change Log` (MAJOR only); appends row to contract `## Version History`; updates contract front matter `version` and `updated` fields; updates contract `## Handoff` table with pch-coder as last agent. On "Accept" for MEDIUM findings: adds undocumented field rows and constraint notes with MINOR bump. Scope boundary: orchestrator only updates the specific fields reported in drift findings — it does not rewrite the full entity section. |

## Implementation Complexity

| Factor | Score (1–5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | 5 agent prompt files in `/agents/` |
| New patterns introduced | 3 | Data contract lifecycle; per-phase drift detection in subagent prompts |
| External dependencies | 1 | Research 019 (local file); no APIs or services |
| Migration complexity | 1 | No data migration; fully reversible (edit prompt files) |
| Test coverage required | 1 | Agent prompt files; no automated tests applicable |
| **Overall Complexity** | **9/25** | **Low (≤10)** |

## Review Summary

**Review Date:** 2026-02-17
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.1
**Reviewed Plan Version:** v2.3

### Review Metrics

- Issues Found: 6 (Critical: 0, Major: 2, Minor: 4)
- Clarifying Questions Asked: 2
- Sections Updated: Architectural Considerations, Phase 0 (new), Phase 1 Steps 1.2–1.3, Review Session Log, Version History, front matter

### Key Improvements Made

1. **Naming convention clarified:** Research 019's internal inconsistency (`dc-NNN` vs bare `NNN`) documented in Architectural Considerations item 5; Phase 1 acceptance criteria updated with explicit overrides so pch-coder uses the correct `dc-NNN-{domain}.md` form
2. **Phase 0 added:** Dedicated pre-read step ensures pch-coder loads Research 019 insertion text before making any edits, preserving the value of the pre-researched design
3. **Table column count corrected:** Step 1.3 acceptance criteria corrected from 5-column to 4-column table matching Research 019 Phase 2
4. **Phase sequencing explained:** Preamble note added to Execution Plan documenting why phases are sequential despite targeting independent files
5. **Front matter corrected:** Version (`2.0` → `2.3`) and status (`🔄 In Progress` → `⏳ Pending Review`) aligned

### Remaining Considerations

- Research 019 itself remains inconsistent (Phase 2 vs Phase 3 naming). The plan overrides this, but a future cleanup of Research 019 would remove the ambiguity entirely.
- No automated test coverage is possible for agent prompt changes; correctness must be verified by running a vision session end-to-end after implementation.

### Sign-off

This plan has been reviewed and is **Ready for Implementation**

## Implementation Notes

### Plan Completion
**All phases completed:** 2026-02-17
**Total tasks completed:** 20 (0.1, 1.1–1.4, 2.1–2.3, 3.1–3.4, 4.1–4.4, 5.1–5.6)
**Total files modified:** 5

### Phase 1 — pch-visionary.md
**Files Modified:** `agents/pch-visionary.md`
**Deviations:** Added `/docs/data-contracts/` to Permitted Actions (Hard Constraints section) — necessary for the create-if-not-exists guard to function without contradicting the agent's own constraints. Post-review fix: also updated the "Before ANY Tool Call — Self-Check" gate to accept `/docs/data-contracts/` alongside `/docs/vision/`.

### Phase 2 — pch-researcher.md
**Files Modified:** `agents/pch-researcher.md`
**Deviations:** None

### Phase 3 — pch-planner.md
**Files Modified:** `agents/pch-planner.md`
**Deviations:** Post-review fix: expanded the "Documentation Only" constraint to also permit `/docs/data-contracts/` — necessary because Step 4.5 updates contract files.

### Phase 4 — pch-plan-reviewer.md
**Files Modified:** `agents/pch-plan-reviewer.md`
**Deviations:** Post-review fix: moved two orphaned bullets ("No error response formats defined", "Unclear ownership of shared concerns") back under `### Implementation Ambiguity` — they had been displaced by the `### Data Contract Issues` insertion.

### Phase 5 — pch-coder.md
**Files Modified:** `agents/pch-coder.md`
**Deviations:** Re-lettered Orchestration Loop sub-steps (b→c, c→d, d→e, e→f, f→g) after inserting new step b (Load data contracts). This is a formatting-only change that does not alter behavior.

## Standards

_No organizational standards applicable — pch-standards-space unavailable in current context. Proceeding without standards context._

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-planner |
| Created Date | 2026-02-17 |
| Reviewed By | pch-plan-reviewer |
| Review Date | 2026-02-17 |
| Status | ✅ Complete |
| Implemented By | pch-coder |
| Implementation Date | 2026-02-17 |
| Next Agent | — (implementation complete) |
| Plan Location | /docs/plans/021-data-contract-management-across-pch-agents.md |
