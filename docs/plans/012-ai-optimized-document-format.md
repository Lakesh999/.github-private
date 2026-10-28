# Implementation Plan: AI-Optimized Document Format for PCH Agents

## Document Information

| Field | Value |
|-------|-------|
| Document ID | 012 |
| Feature | AI-Optimized Document Format |
| Created | 2026-02-08 |
| Last Updated | 2026-02-08 |
| Status | complete |
| Owner | pch-planner |
| Version | v3.0 |

## Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| v1.0 | 2026-02-08 | pch-planner | Initial plan creation |
| v1.1 | 2026-02-08 | pch-planner | Added 7 planning decisions |
| v2.0 | 2026-02-08 | pch-planner | Holistic review completed; all sections populated |
| v2.1 | 2026-02-08 | pch-plan-reviewer | Review: Moved copilot-instructions.md updates out of Phase 4; added Phase 5 for documentation updates (README.md + copilot-instructions.md) |
| v3.0 | 2026-02-08 | pch-coder | Implementation complete: All 5 phases executed successfully |

---

## Review Session Log

**Questions Pending:** 0 | **Questions Resolved:** 2 | **Last Updated:** 2026-02-08

| # | Issue/Question | Category | User Decision | Plan Update |
|---|----------------|----------|---------------|-------------|
| 1 | README.md contains emoji status references and document format descriptions needing updates — should this be a new phase or merged into Phase 4? | Completeness | Option A: Add Phase 5 for README + move copilot-instructions steps there | Steps 4.8-4.9 moved to new Phase 5; Phase 4 now focused on consumer agent prompts only; Phase 5 covers README.md + copilot-instructions.md |
| 2 | Hugh README and other READMEs — do they need updates? | Completeness | Option B: Add a sweep step to Phase 5 | Added step 5.8 to scan Hugh/README.md, installer/README.md, docs/presentations/README.md for emoji status references (verified: none currently exist, but step ensures consistency check at implementation time) |

---

## Planning Session Log

| Session | Date | Decision Point | Answer | Rationale |
|---------|------|----------------|--------|-----------|
| 1 | 2026-02-08 | YAML Schema Depth | B — Structured Top-Level, Freeform Content | Balances parseability with agent flexibility; top-level keys let downstream consumers reliably locate sections; freeform inner content lets agents maximize density without fighting a rigid schema |
| 2 | 2026-02-08 | Downstream Consumer Update Scope | A — Update All Consumers in This Plan | Coordinated cutover avoids broken handoffs; DocumentPathExtractor regexes target Markdown patterns that won't exist in YAML; end-to-end validation possible in one plan |
| 3 | 2026-02-08 | Existing Document Migration | B — No Migration — Coexist | All 25 existing docs are complete/historical; no agent will re-consume them; file extension difference (.yaml vs .md) makes format obvious; avoids large mechanical effort with no functional benefit |
| 4 | 2026-02-08 | File Extension | B — .md (Keep Current Extension) | Avoids cascading changes to DocumentPathExtractor regexes, document numbering instructions, copilot-instructions.md path references; agents and Hugh read content not extensions |
| 5 | 2026-02-08 | Introduction Field Scope | A — Brief (2-4 Sentences) | Maximizes token savings; enough for humans to grasp purpose; structured YAML keys below are still human-readable for detail; avoids recreating verbose prose |
| 6 | 2026-02-08 | Code Snippets in Research | A — Inline YAML Multiline Strings | Most token-efficient; YAML literal blocks preserve formatting natively; AI doesn't need Markdown fences or syntax hints; avoids cross-referencing overhead |
| 7 | 2026-02-08 | Progress Tracking | A — Plain Text Status Values | Most token-efficient; self-documenting strings (not-started, in-progress, complete, blocked); 1 token each vs 1-3 for emoji; easier to regex match; AI doesn't need visual emoji cues |

---

## Holistic Review Summary

### Decision Interactions

| Decisions | Interaction | Resolution |
|-----------|-------------|------------|
| D2 (All Consumers) + D4 (Keep .md Extension) | Keeping .md eliminates most DocumentPathExtractor regex changes; but consumer agent prompts still need updates to describe YAML content expectations | Agent prompts updated to say "YAML-formatted .md files"; DocumentPathExtractor regex patterns only need minor updates for YAML key-value path format (e.g., `path: docs/plans/012-foo.md`) |
| D1 (Structured Top-Level) + D6 (Inline YAML Code) | Freeform inner content allows code blocks via YAML `\|` without schema conflicts | No conflict — `\|` blocks are natural YAML values within freeform content sections |
| D3 (No Migration) + D4 (Keep .md Extension) | Old Markdown .md files coexist with new YAML .md files in same directories | Agents and Hugh read content, not format — no parsing ambiguity. The YAML documents will start with YAML structure (no `#` heading), making them distinguishable if needed |
| D5 (Brief Introduction) + D7 (Plain Text Status) | Both maximize token savings — consistent density philosophy | No conflict |
| D2 (All Consumers) + D7 (Plain Text Status) | pch-coder and pch-plan-reviewer prompts currently reference emoji status values; SignalClassifier may look for emoji patterns | Update all status references in consumer prompts from emoji to plain text; verify SignalClassifier doesn't hardcode emoji matching |
| D1 (Structured Top-Level) + D5 (Brief Intro) | `introduction` is a top-level key with freeform 2-4 sentence value | Clean fit — introduction is a simple multiline string at top level |

### Architectural Considerations Addressed

1. **Format Detection**: Since old (.md Markdown) and new (.md YAML) files coexist in the same directories, downstream consumers may encounter either format. AI agents handle this naturally (they parse content). Hugh's `DocumentPathExtractor` operates on agent response text (not document files), so it's unaffected by document content format. The `SdkResponseProcessor` and `SignalClassifier` operate on agent conversation output, not documents.

2. **Handoff Path Communication**: Agents currently communicate document paths using Markdown conventions (backtick-wrapped, bold markers). In YAML output mode, agents will still produce conversational responses in Markdown (the conversation is not YAML — only the *documents* are). Handoff messages remain Markdown. Therefore `DocumentPathExtractor` regexes for agent *response* parsing need no changes. Only the document *content* changes format.

3. **Plan Reviewer Reading YAML Plans**: `pch-plan-reviewer` will need to understand YAML plan structure to verify file paths, patterns, and phase details. Since AI agents naturally parse YAML, the main change is updating the reviewer's prompt to describe what keys to look for (e.g., `phases`, `requirements`, `decisions`).

4. **Incremental Document Building**: All three producer agents build documents incrementally (section by section). YAML is well-suited to incremental building — agents can add keys/sections via `replace_string_in_file` just as they do with Markdown sections. The agents' "one section at a time" rule still applies.

### Trade-offs Accepted

| Trade-off | Decision | Rationale |
|-----------|----------|-----------|
| Mixed formats in /docs/ directories | D3 (No Migration) | Historical docs are never re-consumed; cosmetic inconsistency is acceptable |
| YAML inside .md files (unconventional) | D4 (Keep .md) | Avoids cascading regex/path changes; content-based parsing makes extension irrelevant |
| No syntax highlighting for code in YAML | D6 (Inline YAML) | AI consumers don't benefit from syntax hints; pure token savings |
| Humans must read YAML structure for detail | D5 (Brief Intro) | YAML key-value pairs are human-readable, just denser; intro provides entry point |

### Risks Acknowledged

| Risk | Mitigation |
|------|------------|
| Agent may produce invalid YAML (indentation errors) | Include YAML formatting rules in agent prompts; AI models are proficient at YAML generation |
| Incremental YAML edits harder than Markdown (indentation-sensitive) | Agents use `replace_string_in_file` with exact string matching; indentation preserved in oldString/newString |
| Downstream agent confused by YAML document when prompt still describes Markdown | D2 ensures all consumers updated in same plan — coordinated cutover |

---

## Overview

### Feature Summary

Convert all three document-producing agents (`pch-visionary`, `pch-researcher`, `pch-planner`) from generating human-readable Markdown documents to generating AI-optimized YAML documents. The new format is the **default and only** output format — no mode toggle or format flag.

Each YAML document will include a prose `introduction` field providing human-readable context for the document's intent, but all structured data (decisions, requirements, phases, findings) will use compact YAML notation optimized for token efficiency.

### Motivation

Current documents average 65-75% token overhead from:
- Markdown table border characters (`|---|---|`)
- Decorative elements (horizontal rules, ASCII diagrams, status emoji tables)
- Prose wrappers around structured data
- Redundant cross-references and version history tables
- Section completion status tracking tables

Switching to YAML will reduce token consumption by ~65-75%, enabling:
- More context window room for actual code during `pch-coder` sessions
- Faster phase execution in Automatic Mode
- Fewer context window exhaustion failures in multi-phase Hugh runs
- More information per token for downstream AI consumers

### Objectives

1. Define the AI-optimized YAML document schema for all three document types (vision, research, plan)
2. Update `pch-visionary` prompt to generate YAML vision documents
3. Update `pch-researcher` prompt to generate YAML research documents
4. Update `pch-planner` prompt to generate YAML plan documents
5. Update downstream consumers (`pch-plan-reviewer`, `pch-coder`, Hugh/DocumentPathExtractor) to parse YAML
6. Update `copilot-instructions.md` to describe the new document format
7. Migrate or document approach for existing Markdown documents

---

## Requirements

### Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| FR-1 | Define YAML schema for vision documents with structured top-level keys and freeform inner content | P0 |
| FR-2 | Define YAML schema for research documents with structured top-level keys and freeform inner content | P0 |
| FR-3 | Define YAML schema for plan documents with structured top-level keys and freeform inner content | P0 |
| FR-4 | All schemas include a brief `introduction` field (2-4 sentences, human-readable prose) | P0 |
| FR-5 | All schemas use plain text status values: `not-started`, `in-progress`, `complete`, `blocked` | P0 |
| FR-6 | Code snippets embedded as inline YAML multiline strings (`\|` literal blocks) | P0 |
| FR-7 | Update `pch-visionary` prompt: replace Markdown document template with YAML schema, update all template/example sections | P0 |
| FR-8 | Update `pch-researcher` prompt: replace Markdown document template with YAML schema, update all template/example sections | P0 |
| FR-9 | Update `pch-planner` prompt: replace Markdown document template with YAML schema, update all template/example sections | P0 |
| FR-10 | Update `pch-plan-reviewer` prompt: describe YAML plan structure for review | P0 |
| FR-11 | Update `pch-coder` prompt: describe YAML plan structure for reading phases/tasks | P0 |
| FR-12 | Update `copilot-instructions.md`: document new YAML format, update document location descriptions | P1 |
| FR-13 | Documents retain `.md` file extension — no extension changes | P0 |
| FR-14 | Existing Markdown documents left as-is — no migration | P0 |
| FR-15 | Agent conversational responses (handoff messages, questions, status reports) remain Markdown — only document *files* become YAML | P0 |

### Non-Functional Requirements

| ID | Requirement | Priority |
|----|-------------|----------|
| NFR-1 | New YAML documents should use ~65-75% fewer tokens than equivalent Markdown documents | P0 |
| NFR-2 | YAML documents must be valid YAML (parseable by any YAML parser) | P1 |
| NFR-3 | Agent incremental building pattern preserved — agents can add/edit YAML sections one at a time | P0 |
| NFR-4 | All existing handoff flows between agents continue to work (path passing, status signals) | P0 |

### Out of Scope

- Migration of existing 25 Markdown documents to YAML
- File extension changes (`.yaml` or `.yml`)
- Format toggle/mode selection — YAML is the only format going forward
- Changes to Hugh's `SignalClassifier` or `SdkResponseProcessor` (they operate on agent conversation text, not document files)
- Changes to `DocumentPathExtractor` (it operates on agent response text which remains Markdown; document paths still end in `.md`)

---

## Technical Design

## Technical Design

### YAML Document Schemas

All three document types share common top-level keys (`id`, `type`, `status`, `created`, `introduction`) plus type-specific structured sections. Inner content within sections is freeform (strings, lists, nested maps as the agent sees fit).

#### Vision Document Schema

```yaml
id: "NNN"
type: vision
title: Short Feature Title
status: in-progress | complete
created: YYYY-MM-DD
owner: pch-visionary
version: v1.0

introduction: |
  2-4 sentence prose describing what this vision captures,
  the core problem being solved, and intended outcome.

decisions:
  - session: 1
    date: YYYY-MM-DD
    stage: 1
    topic: Problem Space
    question: What problem are we solving?
    answer: Description of the user's answer
    impact: How this shapes the vision

problem:
  context: Business context description
  current_state: What exists today
  desired_state: What we want to achieve
  success_metrics:
    - metric: Metric name
      target: Target value
      method: How measured

stakeholders:
  - role: Role name
    goals: What they want
    pain_points: Current frustrations
    proficiency: technical | non-technical

goals:
  primary:
    - id: G-1
      goal: Goal description
      priority: must-have | should-have | could-have
      success_indicator: How to measure
  non_goals:
    - Excluded item 1
    - Excluded item 2

requirements:
  functional:
    - id: FR-1
      requirement: Capability description
      priority: must-have | should-have | could-have
      phase: 1
  non_functional:
    - id: NFR-1
      requirement: Quality attribute
      priority: must-have
      type: performance | security | reliability | usability
  constraints:
    - type: technical | business | regulatory | resource
      description: Constraint detail
      impact: How it shapes the solution

architecture:
  style: web-app | api | cli | desktop | hybrid
  rationale: Why this style
  technology_decisions:
    - area: Platform | Language | Framework | Storage
      choice: Selected technology
      rationale: Why chosen
  integrations:
    - system: System name
      type: api | database | file | queue
      direction: read | write | both
      criticality: mvp | future
  security:
    - concern: Authentication | Authorization | Data sensitivity
      approach: How addressed

phasing:
  - phase: 1
    name: MVP
    scope:
      - Capability 1
      - Capability 2
    deferred:
      - item: Deferred capability
        target_phase: 2
    done_criteria:
      - Criteria 1

research_topics:
  - topic: Topic name
    priority: high | medium | low
    questions:
      - Question text
    target_phase: 1

risks:
  - risk: Risk description
    likelihood: high | medium | low
    impact: high | medium | low
    mitigation: Strategy

standards:
  - name: Standard name
    source: pch-standards-space
    relevance: Why it applies
    guidance: Specific guidance used

handoff:
  created_by: pch-visionary
  created_date: YYYY-MM-DD
  status: complete
  next_agent: pch-researcher | pch-planner
  path: /docs/vision/NNN-description.md
```

#### Research Document Schema

```yaml
id: "NNN"
type: research
title: Research Topic Title
status: in-progress | complete
created: YYYY-MM-DD
current_phase: N of M

introduction: |
  2-4 sentence prose describing what this research investigates,
  why it matters, and what questions it aims to answer.

objectives:
  - Question or objective 1
  - Question or objective 2

phases:
  - phase: 1
    name: Phase Name
    status: not-started | in-progress | complete | partial | blocked
    scope:
      - Scope item 1
      - Scope item 2
    session: YYYY-MM-DD | null
    findings: |
      Freeform findings content. Can include analysis,
      discovered patterns, relevant details.
      
      Code examples embedded as plain text:
      var client = new CopilotClient();
      await client.StartAsync();
    key_discoveries:
      - Discovery 1
      - Discovery 2
    relevant_files:
      - path: src/services/AuthService.ts
        note: Handles JWT validation
    external_sources:
      - url: https://docs.example.com
        note: Official SDK documentation
    gaps:
      - Gap description (or "none")
    assumptions:
      - Assumption with reasoning (or "none")

overview: |
  Synthesized summary across all phases. Written after all
  phases complete. Key findings, cross-cutting patterns,
  actionable conclusions, open questions.

key_findings:
  - Finding 1 synthesized from phases
  - Finding 2

actionable_conclusions:
  - Conclusion 1
  - Conclusion 2

open_questions:
  - Unresolved question

standards:
  - name: Standard name
    relevance: Why it applies
    guidance: Specific guidance used

handoff:
  created_by: pch-researcher
  created_date: YYYY-MM-DD
  status: complete
  current_phase: complete | N
  path: /docs/research/NNN-description.md
```

#### Plan Document Schema

```yaml
id: "NNN"
type: plan
title: Feature Name
status: in-progress | ready | implementing | complete
created: YYYY-MM-DD
updated: YYYY-MM-DD
owner: pch-planner
version: v1.0

introduction: |
  2-4 sentence prose describing what this plan implements,
  the business goal, and the high-level technical approach.

decisions:
  - session: 1
    date: YYYY-MM-DD
    question: Decision topic
    answer: Selected option
    rationale: Why chosen

review_log:
  questions_pending: 0
  questions_resolved: N
  entries:
    - issue: Issue description
      category: clarity | correctness | specificity
      decision: User's choice
      plan_update: What was changed

holistic_review:
  decision_interactions:
    - decisions: D1 + D2
      interaction: How they interact
      resolution: How resolved
  architectural_considerations:
    - Consideration description
  tradeoffs:
    - tradeoff: What was traded off
      decision: Related decision
      rationale: Why acceptable
  risks:
    - risk: Risk description
      mitigation: Strategy

overview:
  summary: Feature summary
  research_ref: /docs/research/NNN-description.md
  objectives:
    - Objective 1
    - Objective 2
  todos:
    - file: CopilotService.cs:65
      description: What to resolve
      resolution: How to resolve it

requirements:
  functional:
    - id: FR-1
      requirement: Requirement description
      priority: P0 | P1 | P2
  non_functional:
    - id: NFR-1
      requirement: Requirement description
      priority: P0 | P1 | P2
  out_of_scope:
    - Item description

technical_design:
  patterns:
    - pattern: Pattern name
      location: src/path/*.ts
      usage: How used in this plan
  architecture: |
    Freeform architecture description. Component relationships,
    data flow, disposal chains, etc.

dependencies:
  - dependency: What is needed
    type: internal | external
    status: available | needs-setup

risks:
  - risk: Risk description
    likelihood: high | medium | low
    impact: high | medium | low
    mitigation: Strategy

phases:
  - phase: 0
    name: Phase Name
    status: not-started | in-progress | complete | blocked
    size: small | medium | large
    files_to_modify: N
    prerequisites: Phase N-1 complete, specific artifacts
    entry_point: Start by reading src/file.ts
    verification: Confirm X exists from previous phase
    tasks:
      - step: "0.1"
        task: Task description
        files:
          - src/path/file.ts
        acceptance_criteria:
          - Criteria 1
          - Criteria 2

standards:
  - name: Standard name
    relevance: Why it applies
    guidance: Specific guidance used

handoff:
  created_by: pch-planner
  created_date: YYYY-MM-DD
  status: pending-review
  next_agent: pch-plan-reviewer
  path: /docs/plans/NNN-description.md
```

### Agent Prompt Changes

Each of the five agent prompts requires targeted modifications. The changes fall into these categories:

**Category A — Document Template Replacement (Producers: visionary, researcher, planner)**
- Replace the full Markdown document template with the corresponding YAML schema
- Update example documents/snippets throughout the prompt to show YAML format
- Replace emoji status references (⏳ 🔄 ✅ ❌) with plain text (`not-started`, `in-progress`, `complete`, `blocked`) in all template sections, example outputs, and status tracking instructions
- Update "incremental document building" instructions to describe YAML key-by-key building instead of Markdown section-by-section
- Update handoff message templates to reference YAML document structure (handoff messages themselves stay Markdown)

**Category B — Document Reading Instructions (Consumers: plan-reviewer, coder)**
- Add a note that plan documents are YAML-formatted
- Update references to plan sections from Markdown headings (e.g., "## Phase 1") to YAML keys (e.g., `phases[0]`)
- Update status value references from emoji to plain text
- Update any examples that show plan document content

**Category C — Shared Updates (All agents that reference status values)**
- Replace all status emoji references with plain text equivalents
- Update progress tracking instructions

### Specific Prompt Modifications by Agent

#### pch-visionary.md (1,648 lines)
1. **Lines ~870-1100**: Replace entire Vision Document Template with YAML schema
2. **Lines ~250-270**: Update "Section Completion Status" example table to YAML status tracking
3. **Lines ~155-180**: Update session log template from Markdown table to YAML `decisions` list
4. **Lines ~200-215**: Update "Section Status Interpretation" table — replace emoji with plain text
5. **Lines ~1100-1180**: Update Research Topics / Risk Assessment template sections to YAML
6. **All handoff message templates** (~lines 1400-1648): Keep as Markdown but update internal references to "YAML vision document"
7. **Throughout**: Replace all ⏳ 🔄 ✅ ❌ status references with `not-started`, `in-progress`, `complete`, `blocked`

#### pch-researcher.md (1,463 lines)
1. **Lines ~1050-1150**: Replace entire Research Document Structure template with YAML schema
2. **Lines ~420-500**: Update "Updating the Research Document" section — replace Markdown section examples with YAML key updates
3. **Lines ~500-560**: Update post-phase report template — keep as Markdown conversation, but reference YAML document structure
4. **Lines ~900-1000**: Update phase sizing, handoff messages — reference YAML keys
5. **Lines ~340-400**: Update subagent return format parsing — document section mapping references YAML keys
6. **Throughout**: Replace all emoji status references with plain text

#### pch-planner.md (416 lines)
1. **Lines ~300-350**: Replace "Plan Structure" section to describe YAML keys instead of Markdown headings
2. **Lines ~340-360**: Replace Phase Template from Markdown to YAML format
3. **Lines ~280-300**: Update "Pattern Documentation Format" from Markdown table to YAML
4. **Lines ~70-100**: Update question format — keep questions in Markdown (conversation), but note that decisions logged in YAML format
5. **Lines ~370-416**: Update handoff metadata format to YAML
6. **Throughout**: Replace emoji status references with plain text

#### pch-plan-reviewer.md (454 lines)
1. **Lines ~110-130**: Update "Codebase Verification" section to reference YAML structure
2. **Lines ~1-30**: Update description of what plan documents look like
3. **Add note**: "Plan documents are YAML-formatted .md files. Key sections to review: `requirements`, `technical_design`, `phases`, `decisions`"
4. **Throughout**: Replace emoji status references with plain text

#### pch-coder.md (1,034 lines)
1. **Lines ~200-250**: Update subagent invocation prompt template — describe YAML plan format
2. **Lines ~120-140**: Update plan reading instructions to reference YAML keys
3. **Lines ~250-320**: Update subagent result processing — reference YAML phase structure
4. **Add note**: "Plan documents are YAML-formatted .md files. Locate your assigned phase under the `phases` key"
5. **Throughout**: Replace emoji status references with plain text

### Downstream Consumer Updates

#### copilot-instructions.md
1. Update "Document Locations" section to note YAML format
2. Update progress status emoji section to plain text equivalents
3. Update session log table format description to YAML list
4. Update any example document snippets to YAML

#### No changes needed for:
- `Hugh/Agents/DocumentPathExtractor.cs` — operates on agent conversation text (Markdown), not document files
- `Hugh/Agents/SdkResponseProcessor.cs` — processes conversation events, not documents
- `Hugh/Detection/SignalClassifier.cs` — classifies conversation signals, not document content

### Codebase Patterns (Reference)

| Pattern | Location | Usage in This Plan |
|---------|----------|--------------------|
| Agent prompt files | `agents/*.md` | Modify document generation instructions |
| Vision document template | `pch-visionary.md` lines 870-1100 | Replace Markdown template with YAML schema |
| Research document template | `pch-researcher.md` lines 1050-1150 | Replace Markdown template with YAML schema |
| Plan document template | `pch-planner.md` (Plan Structure section) | Replace Markdown template with YAML schema |
| DocumentPathExtractor | `Hugh/Agents/DocumentPathExtractor.cs` | Update regex patterns for YAML path format |
| Plan reviewer | `agents/pch-plan-reviewer.md` | Update to read YAML plans |
| Coder | `agents/pch-coder.md` | Update to read YAML plans |
| Copilot instructions | `.github/copilot-instructions.md` | Update document format descriptions |

---

## Dependencies and Prerequisites

| Dependency | Type | Status |
|------------|------|--------|
| All 3 producer agent prompts (`pch-visionary`, `pch-researcher`, `pch-planner`) | Internal | Available |
| All 2 consumer agent prompts (`pch-plan-reviewer`, `pch-coder`) | Internal | Available |
| `copilot-instructions.md` | Internal | Available |
| Understanding of YAML syntax by AI model | External | Available (all major models support YAML generation) |

---

## Risk Assessment and Mitigation

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Agent produces invalid YAML (indentation errors) | Medium | Medium | Include explicit YAML formatting rules in prompt; AI models are proficient at YAML; incremental building reduces error scope |
| Incremental YAML edits break document structure | Low | High | Agents use `replace_string_in_file` with exact string matching; test incremental update patterns during implementation |
| Downstream agent confused by mixed format coexistence | Low | Medium | Consumer prompts explicitly state documents are YAML-formatted; old docs are historical and won't be consumed |
| Token savings less than projected 65-75% | Low | Low | Even 40-50% savings is valuable; YAML is inherently denser than decorated Markdown |
| Agent generates Markdown out of habit despite prompt changes | Medium | Medium | Prompt changes are comprehensive; new templates are YAML-only with no Markdown document examples remaining |

---

## Progress Summary

| Phase | Name | Status |
|-------|------|--------|
| 1 | Update pch-visionary.md | complete |
| 2 | Update pch-researcher.md | complete |
| 3 | Update pch-planner.md | complete |
| 4 | Update consumer agents (pch-plan-reviewer, pch-coder) | complete |
| 5 | Update documentation (README.md, copilot-instructions.md) | complete |

---

## Execution Plan

### Phase 1: Update pch-visionary.md — YAML Vision Document Format

**Estimated Session Size:** Medium
**Files to Modify:** 1 (`agents/pch-visionary.md`)
**Prerequisites:** None
**Entry Point:** Read `agents/pch-visionary.md` in full
**Verification:** N/A (first phase)

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 1.1 | Replace the Vision Document Template (lines ~870-1100) with the YAML vision schema defined in Technical Design. Remove all Markdown table templates, replace with YAML example showing all top-level keys. | `agents/pch-visionary.md` | Template section contains complete YAML schema; no Markdown tables remain in template |
| 1.2 | Update the "Section Completion Status" concept throughout the prompt. Replace Markdown status table template with a note that status is tracked via `status` fields on each YAML key. Replace all emoji status values (⏳ 🔄 ✅ ❌) with plain text (`not-started`, `in-progress`, `complete`, `blocked`). | `agents/pch-visionary.md` | No emoji status characters remain; all status references use plain text |
| 1.3 | Update the Vision Session Log references. Replace Markdown table format for session logging with instruction to append to the `decisions` list in the YAML document. Update the example session log entries. | `agents/pch-visionary.md` | Session log instructions reference YAML `decisions` key; example shows YAML list entry format |
| 1.4 | Update "Section Status Interpretation" table (lines ~200-215) to use plain text status values and describe YAML key-based status checking. | `agents/pch-visionary.md` | Table uses plain text values; references YAML status fields |
| 1.5 | Update Research Topics and Risk Assessment template sections (lines ~1100-1180) from Markdown tables to YAML `research_topics` and `risks` key format. | `agents/pch-visionary.md` | Template sections show YAML list format |
| 1.6 | Update all handoff message templates (lines ~1400-1648). Keep messages as Markdown conversation output but update references from "Markdown vision document" to "YAML-formatted vision document". Update any inline document content examples. | `agents/pch-visionary.md` | Handoff messages reference YAML format; conversation output remains Markdown |
| 1.7 | Update the "Incremental Document Building" instructions to describe YAML key-by-key building. Replace "section by section" language with "key by key". Note that `replace_string_in_file` works with YAML content using exact string matching. | `agents/pch-visionary.md` | Instructions describe YAML incremental building pattern |
| 1.8 | Update the Referenced Standards template section from Markdown table to YAML `standards` list. | `agents/pch-visionary.md` | Standards section uses YAML list format |
| 1.9 | Update the Handoff Information section template from Markdown to YAML `handoff` key. | `agents/pch-visionary.md` | Handoff metadata uses YAML map format |

### Phase 2: Update pch-researcher.md — YAML Research Document Format

**Estimated Session Size:** Medium
**Files to Modify:** 1 (`agents/pch-researcher.md`)
**Prerequisites:** Phase 1 complete (establishes the YAML pattern; researcher follows same conventions)
**Entry Point:** Read `agents/pch-researcher.md` in full
**Verification:** Confirm Phase 1 is complete — `pch-visionary.md` uses YAML templates

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 2.1 | Replace the Research Document Structure template (lines ~1050-1150) with the YAML research schema defined in Technical Design. Remove all Markdown heading/table templates. | `agents/pch-researcher.md` | Template section contains complete YAML research schema; no Markdown document templates remain |
| 2.2 | Replace all emoji status values (⏳ 🔄 ✅ ❌ ⚠️) throughout the entire prompt with plain text equivalents (`not-started`, `in-progress`, `complete`, `blocked`, `partial`). This includes the phase status table, progress tracking section, quality standards checklists, and all example outputs. | `agents/pch-researcher.md` | No emoji status characters remain anywhere in the prompt |
| 2.3 | Update "Updating the Research Document" section (lines ~420-500). Replace Markdown section update examples with YAML key update examples. Change "Insert findings into the phase section" to "Update the phase's `findings` key". Replace Markdown status table update examples with YAML `status` field updates. | `agents/pch-researcher.md` | All document update instructions reference YAML keys; examples show YAML updates |
| 2.4 | Update the subagent invocation prompt template (lines ~340-400). The subagent return format stays the same (it's conversation text, not a document). But the "Document Section Mapping" table needs to reference YAML keys instead of Markdown sections (e.g., `FINDINGS → phases[N].findings` instead of `Phase {N} Findings section`). | `agents/pch-researcher.md` | Document section mapping references YAML keys |
| 2.5 | Update post-phase report and final completion report templates (lines ~500-600). Keep as Markdown conversation output. Update any inline references to document structure from Markdown sections to YAML keys. | `agents/pch-researcher.md` | Report templates reference YAML document structure |
| 2.6 | Update session handoff messages (lines ~1170-1300). Keep as Markdown conversation output. Update references from "research document" to "YAML-formatted research document". | `agents/pch-researcher.md` | Handoff messages reference YAML format |
| 2.7 | Update the "Research Phases" progress tracking table format from Markdown table to instruction to update the `phases[N].status` and `phases[N].session` fields. | `agents/pch-researcher.md` | Progress tracking references YAML phase status fields |
| 2.8 | Update the "Code References" documentation standard from Markdown fenced blocks to YAML multiline string format (`\|` literal blocks). | `agents/pch-researcher.md` | Code reference example uses YAML literal block format |
| 2.9 | Update the Referenced Standards and Handoff Information template sections from Markdown to YAML `standards` and `handoff` keys. | `agents/pch-researcher.md` | Standards and handoff use YAML format |

### Phase 3: Update pch-planner.md — YAML Plan Document Format

**Estimated Session Size:** Small
**Files to Modify:** 1 (`agents/pch-planner.md`)
**Prerequisites:** Phases 1-2 complete (establishes consistent YAML pattern)
**Entry Point:** Read `agents/pch-planner.md` in full
**Verification:** Confirm Phases 1-2 complete — `pch-visionary.md` and `pch-researcher.md` use YAML templates

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 3.1 | Replace the "Plan Structure" section (lines ~300-350) to describe YAML top-level keys instead of Markdown headings. Replace the bulleted list of Markdown sections with a bulleted list of YAML keys matching the plan schema. | `agents/pch-planner.md` | Plan structure section lists YAML keys |
| 3.2 | Replace the Phase Template (lines ~340-360) from Markdown format to YAML format. Show a phase entry as a YAML list item with `phase`, `name`, `status`, `size`, `prerequisites`, `entry_point`, `verification`, and `tasks` keys. | `agents/pch-planner.md` | Phase template is YAML format |
| 3.3 | Update the "Pattern Documentation Format" (lines ~280-300) from Markdown table to YAML `patterns` list format. | `agents/pch-planner.md` | Pattern documentation uses YAML list |
| 3.4 | Replace all emoji status values (⏳ 🔄 ✅ ❌) throughout the prompt with plain text equivalents. This includes the progress tracking description and all example text. | `agents/pch-planner.md` | No emoji status characters remain |
| 3.5 | Update the "Incremental Document Creation" instructions to describe building YAML documents key by key. Replace "section by section" language with "key by key". | `agents/pch-planner.md` | Incremental building instructions reference YAML |
| 3.6 | Update the Handoff Metadata section (lines ~370-416) from Markdown to YAML `handoff` key format. Update the completion message template to reference "YAML-formatted plan document". | `agents/pch-planner.md` | Handoff metadata uses YAML format; completion message references YAML |
| 3.7 | Update the "Referenced Standards" documentation format from Markdown table to YAML `standards` list. | `agents/pch-planner.md` | Standards section uses YAML list format |

### Phase 4: Update Consumer Agents (pch-plan-reviewer, pch-coder)

**Estimated Session Size:** Small
**Files to Modify:** 2 (`agents/pch-plan-reviewer.md`, `agents/pch-coder.md`)
**Prerequisites:** Phases 1-3 complete (all producer agents updated)
**Entry Point:** Read `agents/pch-plan-reviewer.md`, then `agents/pch-coder.md`
**Verification:** Confirm Phases 1-3 complete — all three producer agents use YAML templates

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 4.1 | Add a YAML format awareness note to `pch-plan-reviewer.md` near the top (after Core Responsibilities). Insert: "Plan documents are YAML-formatted .md files. Key sections to review are found under these YAML keys: `requirements`, `technical_design`, `phases`, `decisions`, `holistic_review`. Status values use plain text: `not-started`, `in-progress`, `complete`, `blocked`." | `agents/pch-plan-reviewer.md` | Format awareness note present; describes YAML keys |
| 4.2 | Replace all emoji status values (⏳ 🔄 ✅ ❌) in `pch-plan-reviewer.md` with plain text equivalents. | `agents/pch-plan-reviewer.md` | No emoji status characters remain |
| 4.3 | Update the "Codebase Verification" section in `pch-plan-reviewer.md` to note that file paths in the plan are under YAML keys (e.g., `phases[N].tasks[M].files`) rather than Markdown table cells. | `agents/pch-plan-reviewer.md` | Codebase verification references YAML structure |
| 4.4 | Update references from Markdown plan sections to YAML keys throughout `pch-plan-reviewer.md`. For example, "Execution Plan section" → "`phases` key", "Requirements section" → "`requirements` key". | `agents/pch-plan-reviewer.md` | All plan section references use YAML key names |
| 4.5 | Add a YAML format awareness note to `pch-coder.md` in the Session Initialization section. Insert: "Plan documents are YAML-formatted .md files. Locate your assigned phase under the `phases` key. Each phase has `tasks`, `prerequisites`, `entry_point`, and `verification` fields. Status values use plain text: `not-started`, `in-progress`, `complete`, `blocked`." | `agents/pch-coder.md` | Format awareness note present; describes YAML keys |
| 4.6 | Replace all emoji status values (⏳ 🔄 ✅ ❌) in `pch-coder.md` with plain text equivalents. | `agents/pch-coder.md` | No emoji status characters remain |
| 4.7 | Update the subagent invocation prompt template in `pch-coder.md` (lines ~200-250). Add a note that the plan is YAML-formatted: "The plan below is YAML-formatted. Your assigned phase is under the `phases` key — find the entry where `phase: {N}`." | `agents/pch-coder.md` | Subagent prompt template includes YAML format note |

### Phase 5: Update Documentation (README.md, copilot-instructions.md)

**Estimated Session Size:** Small
**Files to Modify:** 2 (`README.md`, `.github/copilot-instructions.md`)
**Prerequisites:** Phases 1-4 complete (all agent prompts updated)
**Entry Point:** Read `README.md`, then `.github/copilot-instructions.md`
**Verification:** Confirm Phases 1-4 complete — all agent prompts use YAML templates and plain text status values

| Step | Task | File(s) | Acceptance Criteria |
|------|------|---------|---------------------|
| 5.1 | Update `copilot-instructions.md`: Replace the "Progress Status Emojis" section (lines ~149-154) with plain text status values (`not-started`, `in-progress`, `complete`, `blocked`). Rename the section header from "Progress Status Emojis" to "Progress Status Values". | `.github/copilot-instructions.md` | Status values are plain text; section header updated; no emoji status characters remain |
| 5.2 | Update `copilot-instructions.md`: Update the "Session Log Tables" description (lines ~156-161) from Markdown table format to YAML `decisions` list format. Show example YAML list entry instead of Markdown table row. | `.github/copilot-instructions.md` | Session log description references YAML `decisions` format |
| 5.3 | Update `copilot-instructions.md`: Update the "Phase Completion" section (line ~167) — replace `✅ status` with `complete status`. In the "Document Locations" section (lines ~138-142), add a note: "Documents created after 2026-02-08 use YAML format inside .md files. Older documents remain in Markdown format." | `.github/copilot-instructions.md` | Phase completion uses plain text; document format note present |
| 5.4 | Update `copilot-instructions.md`: Update the "Structured Interaction" description (line ~104) — replace emoji status references with plain text equivalents. | `.github/copilot-instructions.md` | No emoji status references remain in copilot-instructions.md |
| 5.5 | Update `README.md`: Replace the emoji status reference at line ~259 (`⏳ Not Started \| 🔄 In Progress \| ✅ Complete \| ❌ Blocked`) with plain text equivalents (`not-started \| in-progress \| complete \| blocked`). | `README.md` | No emoji status characters remain in README.md |
| 5.6 | Update `README.md`: In the "File Structure" section (lines ~261-279), add a note under the `docs/` folder entries: "Documents created after 2026-02-08 use YAML format inside .md files. Older documents remain in Markdown format." | `README.md` | YAML format note present in file structure description |
| 5.7 | Update `README.md`: In the "Structured Interaction" bullet list (lines ~254-259), verify all status references use plain text. In the "Key Features" section, ensure no emoji status references remain. | `README.md` | All status references in README.md use plain text |
| 5.8 | Sweep sub-project READMEs (`Hugh/README.md`, `installer/README.md`, `docs/presentations/README.md`) for any emoji status references (⏳ 🔄 ✅ ❌). Replace with plain text equivalents if found. If none exist, confirm no changes needed. | `Hugh/README.md`, `installer/README.md`, `docs/presentations/README.md` | No emoji status characters remain in any README; or confirmed none existed |

---

## Implementation Complexity

| Factor | Score (1-5) | Notes |
|--------|-------------|-------|
| Files to modify | 3 | 7 files across agents/, .github/, and root |
| New patterns introduced | 2 | YAML document format replaces Markdown; no new frameworks |
| External dependencies | 1 | None — YAML is natively supported by AI models |
| Migration complexity | 1 | No migration of existing documents (D3) |
| Test coverage required | 2 | Manual verification — generate a test document with each updated agent |
| **Overall Complexity** | **9/25** | **Low** — Changes are mechanical prompt edits across well-understood files |

---

## Implementation Notes for AI Agents

1. **Read the full agent prompt file before making changes** — these are large files (400-1,600 lines) with interconnected sections. Understand the full structure before editing.
2. **YAML template sections are the core change** — each producer agent has a document template section that is the primary target. Start there.
3. **Search-and-replace for emoji status** — use grep to find all instances of ⏳ 🔄 ✅ ❌ ⚠️ and replace systematically. Don't rely on finding them by reading.
4. **Handoff messages stay Markdown** — the conversation between agent and user is Markdown. Only the document *files* written to disk become YAML. Don't convert handoff templates to YAML.
5. **Preserve agent behavioral rules** — the YAML format change affects *what* documents look like, not *how* the agent behaves (one question at a time, save before stopping, phase boundaries, etc.). Don't alter behavioral instructions.
6. **The YAML schemas in Technical Design are the source of truth** — when building templates in agent prompts, reference the schemas defined in this plan.

---

## Review Summary

**Review Date:** 2026-02-08
**Reviewer:** pch-plan-reviewer
**Original Plan Version:** v2.0
**Reviewed Plan Version:** v2.1

### Review Metrics
- Issues Found: 2 (Critical: 0, Major: 1, Minor: 1)
- Clarifying Questions Asked: 2
- Sections Updated: Progress Summary, Execution Plan (Phases 4 & 5)

### Key Improvements Made
1. Separated Phase 4 into two phases: Phase 4 (consumer agent prompts only) and Phase 5 (documentation — README.md + copilot-instructions.md), keeping each phase focused and within comfortable context window limits
2. Added detailed steps for README.md updates (emoji status, file structure format note, structured interaction section)
3. Added sweep step for sub-project READMEs to ensure consistency

### Remaining Considerations
- Sub-project READMEs (Hugh, installer, presentations) were verified to contain no emoji status references — step 5.8 is a confirmation-only step
- Phase 5 steps reference specific line numbers in README.md and copilot-instructions.md — implementer should verify these are still accurate at execution time

### Sign-off
✅ This plan has been reviewed and is **Ready for Implementation**

---

## Implementation Notes

### Phase 1 - pch-visionary.md (Complete)
**Execution Mode:** Automatic
**Files Modified:** `agents/pch-visionary.md`
**Deviations from Plan:** None
**Notes:**
- Replaced Vision Document Template with complete YAML schema
- Updated all status emoji to plain text values throughout
- Updated session log references to use YAML `decisions` list
- Updated incremental building instructions to describe YAML key-by-key approach

### Phase 2 - pch-researcher.md (Complete)
**Execution Mode:** Automatic
**Files Modified:** `agents/pch-researcher.md`
**Deviations from Plan:** None
**Notes:**
- Replaced Research Document Structure template with YAML schema
- Updated all status emoji references to plain text
- Updated Progress Tracking examples to YAML format
- Preserved constraint list emoji (❌ forbidden, ✅ permitted) as these are UI indicators

### Phase 3 - pch-planner.md (Complete)
**Execution Mode:** Automatic
**Files Modified:** `agents/pch-planner.md`
**Deviations from Plan:** None
**Notes:**
- Updated Phase Template to YAML format
- Updated Codebase Patterns Reference to YAML format
- Updated Version History template to YAML format
- Updated Referenced Standards template to YAML format
- Updated Handoff Metadata to YAML format
- Replaced status emoji with plain text values

### Phase 4 - Consumer Agents (Complete)
**Execution Mode:** Automatic
**Files Modified:** `agents/pch-plan-reviewer.md`, `agents/pch-coder.md`
**Deviations from Plan:** None
**Notes:**
- Added YAML format awareness notes to both agents
- Updated Review Session Log to YAML format in pch-plan-reviewer.md
- Updated task status examples to YAML format in pch-coder.md
- Updated Deployment Notes format to YAML in pch-coder.md
- Replaced all status emoji with plain text values
- Preserved UI warning/error header emoji (⚠️, ❌ for error states)

### Phase 5 - Documentation (Complete)
**Execution Mode:** Automatic
**Files Modified:** `README.md`, `.github/copilot-instructions.md`
**Deviations from Plan:** None
**Notes:**
- Updated Progress Status section from "Emojis" to "Values" with plain text list
- Updated Session Log Tables to YAML format in copilot-instructions.md
- Updated progress tracking description in README.md
- Added "AI-Optimized Document Format" subsection to README.md Key Features
- Verified sub-project READMEs (step 5.8): No emoji status references found

---

## Referenced Standards

No organizational standards applicable to this plan.

---

## Handoff Information

**Created By:** pch-planner
**Created Date:** 2026-02-08
**Reviewed By:** pch-plan-reviewer
**Review Date:** 2026-02-08
**Implemented By:** pch-coder
**Implementation Date:** 2026-02-08
**Status:** Complete
**Plan Location:** /docs/plans/012-ai-optimized-document-format.md
