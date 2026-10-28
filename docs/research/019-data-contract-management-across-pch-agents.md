---
id: "019"
type: research
title: "Data Contract Management Across PCH Agents"
status: ✅ Complete
created: "2026-02-17"
current_phase: "5 of 5"
---

## Introduction

AI agents working on a project can stray from established data contracts when focused on a specific problem set, leading to schema drift, broken integrations, and inconsistent models. This research investigates how the PCH agent workflow (visionary → researcher → planner → reviewer → coder) can systematically create, reference, maintain, and validate data contracts stored as Markdown documents under `/docs/data-contracts/`. The goal is to determine where in each agent's lifecycle data contract awareness should be injected, how contracts should be structured, and how model drift should be detected during implementation phases.

## Objectives

- Understand how existing cross-cutting artifacts (e.g., standards, vision documents) are currently managed across the PCH agent lifecycle
- Identify industry best practices for data contract structure, content, and governance in software projects
- Determine the precise integration points in each PCH agent prompt where data contract clauses should be added
- Design a model drift detection approach for pch-coder to verify implementation against contracts
- Define the end-to-end data contract lifecycle across the full agent workflow

## Research Phases

| Phase | Name | Status | Scope | Session |
|-------|------|--------|-------|---------|
| 1 | Current Cross-Cutting Artifact Patterns | ✅ Complete | Analyze how PCH agents currently handle shared artifacts (standards, vision docs, research docs); identify existing patterns for artifact creation, referencing, and handoff that data contracts could follow | 2026-02-17 |
| 2 | Data Contract Formats and Best Practices | ✅ Complete | Research industry data contract standards (structure, content, governance); evaluate Markdown-based contract templates; identify what fields/sections a data contract should contain for AI agent consumption | 2026-02-17 |
| 3 | Agent-Specific Integration Points | ✅ Complete | Map each PCH agent's prompt structure to identify exact sections where data contract awareness should be injected; determine per-agent responsibilities (create, reference, validate, enforce) | 2026-02-17 |
| 4 | Model Drift Detection in Code Review | ✅ Complete | Research patterns for comparing data contracts against implementation; design pch-coder's drift detection workflow; define what "drift" means (field changes, type mismatches, missing contracts) and how to surface it | 2026-02-17 |
| 5 | End-to-End Lifecycle Design | ✅ Complete | Synthesize findings into a cohesive lifecycle design: contract creation (visionary/planner), referencing (researcher/planner), validation (reviewer), enforcement (coder); define contract-passing conventions across agent handoffs | 2026-02-17 |

## Phase 1: Current Cross-Cutting Artifact Patterns

**Status:** ✅ Complete
**Session:** 2026-02-17

### Artifact Directory Structure

The PCH agent system organizes all cross-cutting artifacts under `/docs/` with type-segregated subdirectories:

```
/docs/
  vision/       # pch-visionary output — numbered NNN-description.md
  research/     # pch-researcher output — numbered NNN-description.md
  plans/        # pch-planner output — numbered NNN-description.md
  presentations/ # (ad hoc)
/agents/
  pch-visionary.md
  pch-researcher.md
  pch-planner.md
  pch-plan-reviewer.md
  pch-coder.md
  pch-helper.md
  hugh.md
```

All document directories use zero-padded 3-digit sequential numbers as prefixes (e.g., `001`, `018`, `019`). Each agent is responsible for finding the next available number by listing its target directory before creating a new file, to avoid collisions in non-sequential numbering.

### Document Metadata Convention (YAML Front Matter)

Every artifact type uses a YAML front matter block at the top of the Markdown file. The front matter is enclosed in triple-dash delimiters and contains flat key-value scalar fields:

```markdown
---
id: "019"
type: research
title: "Data Contract Management Across PCH Agents"
status: 🔄 In Progress
created: "2026-02-17"
current_phase: "1 of 5"
---
```

The `id`, `type`, `title`, `status`, and `created` fields are shared across all three artifact types. Agent-specific fields are added on top:
- Vision: `owner`, `version`
- Research: `current_phase`, `vision_source` (optional — links to vision doc)
- Plan: `updated`, `owner`, `version`

The `vision_source` field in research documents is the primary cross-document link that connects a research document to its originating vision document. This field is machine-readable and allows orchestrators to automatically load vision context for subagents.

### Status Value System (Emoji + Text)

All artifact types share the same emoji-prefixed status vocabulary:

| Emoji | Status | Meaning |
|-------|--------|---------|
| ⏳ | Not Started | Phase/section not begun |
| 🔄 | In Progress | Phase/section actively being worked |
| ✅ | Complete | Phase/section fully done |
| ❌ | Blocked | Phase/section cannot proceed |
| ⚠️ | Partial | Phase/section has gaps or incomplete areas |

These emoji values align with Hugh's PlanPhaseParser regex so the CLI can read document status without code changes. This was a deliberate design decision recorded in Research 018 and Plan 020.

### How Each Agent Creates Artifacts

**pch-visionary** creates vision documents under `/docs/vision/`:
- Document skeleton created immediately after the first user response
- Sections populated one at a time after each Q&A exchange
- Each update uses `replace_string_in_file` with exact Markdown matching
- Saves progress after every interaction — document is the single source of truth across sessions
- Queries `pch-standards-space` at three stages (domain, compliance, technical) and records results in a `## Standards Applied` table

**pch-researcher** creates research documents under `/docs/research/`:
- On a new request: does minimal discovery (2-5 tool calls), creates a phased outline document, then stops immediately
- The outline document is the handoff artifact — it is shared with a new session to begin phase execution
- On continuation: the full research document is embedded in each subagent's prompt verbatim so the subagent has complete context without needing to read prior session history
- Also queries `pch-standards-space` before beginning research

**pch-planner** creates plan documents under `/docs/plans/`:
- Analyzes the codebase and queries `pch-standards-space` during Step 0 (context gathering) before creating the plan document
- Creates a plan skeleton first, then populates sections one at a time as Q&A answers arrive
- Documents decisions in a `## Planning Session Log` table
- Includes a `## Handoff` table at the end pointing to `pch-plan-reviewer`
- References the research source document with a relative markdown link (e.g., `[docs/research/018-...](docs/research/018-...)`)

### How Each Agent References Artifacts from Upstream Agents

**pch-researcher** referencing vision documents:
- Reads the full vision document when a `vision_source` is provided or when a user provides a vision document path
- Embeds the entire vision document content in each subagent prompt alongside the research document:
  ```
  ## VISION DOCUMENT CONTEXT
  **Vision Document Path:** {vision_doc_path}
  {entire_vision_document_content}
  ```
- The `vision_source` metadata field in the research document acts as the persistent link back to the originating vision
- Subagents receive both documents and are instructed to focus on the research phase but use the vision for broader context on dependencies and constraints

**pch-planner** referencing vision and research documents:
- Receives the vision document as a handoff input (attached or referenced by path)
- User-directed: planner is told "Plan Phase 1 from the attached vision document"
- References the research source in the plan's Overview section using a markdown hyperlink
- No formal `research_source` metadata field exists; the reference is embedded in the plan prose

**pch-plan-reviewer** referencing plan documents:
- Receives the plan document as its primary input (via the `send: true` handoff from pch-planner)
- Reads the plan, verifies file path claims against the codebase, and updates the plan document in-place
- Adds a `## Review Session Log` table to the plan and updates the `## Handoff` section to point to pch-coder
- Queries `pch-standards-space` to verify the plan aligns with organizational standards

**pch-coder** referencing plan documents:
- Receives the plan document as its primary input
- Embeds the entire plan document in each subagent's prompt verbatim:
  ```
  ## FULL IMPLEMENTATION PLAN
  {entire_plan_document_content}
  ```
- Does NOT directly reference vision or research documents — those artifacts are fully upstream and their context has been distilled into the plan

### Handoff Mechanisms

#### Inline Handoff Table (Structured Footer)

Every artifact type ends with a `## Handoff` table that captures provenance and next-agent metadata:

```markdown
## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2026-02-17 |
| Status | 🔄 In Progress |
| Next Agent | pch-planner |
| Path | /docs/research/019-... |
```

For plan documents, the reviewer appends their own review fields:
```markdown
| Reviewed By | pch-plan-reviewer |
| Review Date | 2026-02-17 |
| Status | ✅ Ready for Implementation |
| Next Agent | pch-coder |
```

This table is the formal handoff record and is the target of the agent UI handoff buttons.

#### YAML Front Matter Handoff Buttons

The agent file's YAML front matter defines UI-level handoff buttons under the `handoffs:` key:

```yaml
handoffs:
  - label: Start Research
    agent: pch-researcher
    prompt: |
      Begin research based on the completed vision document.
    send: false
  - label: Start Plan Review
    agent: pch-plan-reviewer
    prompt: Review the implementation plan...
    send: true  # auto-sends vs requires user to click Send
```

The `send: true` setting (used for visionary→researcher and planner→reviewer handoffs) causes the prompt to be automatically sent. The handoff prompt text instructs the receiving agent what to do and implicitly carries the artifact path or content via the attached document.

#### Full Document Embedding (Subagent Pattern)

For pch-researcher and pch-coder — both of which use subagent orchestration — the handoff mechanism within a multi-phase workflow is full document embedding rather than path references. The orchestrator re-reads the document and embeds its entire content in the next subagent's prompt. This ensures each phase subagent has complete context without relying on conversation history or file system reads during the subagent's isolated execution.

### Standards Integration Pattern (pch-standards-space)

All four documentation agents (visionary, researcher, planner, reviewer) query the `pch-standards-space` GitHub Copilot Space at specific lifecycle points:

| Agent | When Queried | Purpose |
|-------|-------------|---------|
| pch-visionary | Stage 1, 4, 5 of Q&A | Domain standards, compliance, technical standards |
| pch-researcher | Before beginning research | Research topic context |
| pch-planner | Step 0 (context gathering) | Feature/topic standards |
| pch-plan-reviewer | Before review begins | Verify plan aligns with org standards |

The query mechanism uses the GitHub MCP server's `copilot_spaces` toolset:
```
list_copilot_spaces → confirm space exists
get_copilot_space(space_name: "pch-standards-space", query: "...") → retrieve relevant standards
```

Query results are documented in a `## Standards Applied` section of each artifact using a consistent table format:

```markdown
## Standards Applied

| Standard | Source | Relevance | Guidance |
|----------|--------|-----------|----------|
| [Standard Name] | pch-standards-space | [Why it applies] | [Specific guidance used] |
```

All agents have fallback behavior: if `pch-standards-space` is unavailable, they log the failure in the Standards Applied table and continue without blocking the workflow.

### Cross-Agent Referencing Conventions Summary

| Convention | Implementation |
|------------|---------------|
| Document numbering | Zero-padded 3-digit sequential prefix in filename |
| Inter-document links | `vision_source` metadata field (machine-readable); markdown hyperlinks in prose (human-readable) |
| Status tracking | Shared emoji status vocabulary in both metadata and phase tables |
| Context passing | Full document embedding in subagent prompts; YAML front matter for persistent metadata |
| Handoff record | `## Handoff` table at end of every artifact |
| Standards traceability | `## Standards Applied` table in every artifact |
| Session continuity | Document-as-truth principle: all state saved to file; session history not relied upon |

### Existing Enforcement Mechanisms

1. **Mandatory pre-session read**: Both pch-researcher and pch-coder read (or re-read) the full target artifact document at the start of every session. This is described as mandatory in both agent prompts.

2. **Document-before-question rule**: All interactive agents (visionary, planner, reviewer) must update the artifact document BEFORE asking the next question. This prevents decisions from being lost between sessions.

3. **Single-section-per-response rule**: All document-writing agents are constrained to write no more than one major `##` section per response to avoid context overflow.

4. **Standards queries at fixed lifecycle points**: The three-query pattern (domain, compliance, technical) is specified at exact lifecycle moments in the visionary's prompt, not left to agent judgment.

5. **Git initialization check**: pch-coder checks for git before coding begins, ensuring all implementation artifacts are version-controlled.

6. **Post-implementation code review**: pch-coder invokes a code review subagent after all phases complete, creating a mandatory quality gate before the final completion report.

**Key Discoveries:**
- All PCH artifacts share a consistent triple-dash YAML front matter + Markdown body structure
- The `vision_source` metadata field is the only machine-readable inter-document link currently used
- Cross-agent context transfer relies on full document embedding (not path references) for subagent isolation
- The `pch-standards-space` integration is the primary cross-cutting "read before acting" enforcement pattern
- The `## Handoff` table is the universal artifact footer — present in all three document types (vision, research, plan)
- Standards Applied tables provide a traceability trail but are populated by agents from queries, not pulled automatically from the document chain
- No agent currently reads upstream artifacts automatically without explicit user instruction or document embedding

| File | Relevance |
|------|-----------|
| `/c/repos/.github-private/agents/pch-visionary.md` | Vision artifact creation, standards query stages, handoff button spec |
| `/c/repos/.github-private/agents/pch-researcher.md` | Research artifact creation, vision-driven research, subagent embedding pattern |
| `/c/repos/.github-private/agents/pch-planner.md` | Plan artifact creation, research source referencing, context gathering |
| `/c/repos/.github-private/agents/pch-plan-reviewer.md` | Review session log pattern, in-place plan updating, standards verification |
| `/c/repos/.github-private/agents/pch-coder.md` | Plan embedding pattern, post-implementation review, git initialization |
| `/c/repos/.github-private/docs/vision/004-hugh-copilot-agent.md` | Reference vision document showing full YAML front matter + phasing structure |
| `/c/repos/.github-private/docs/research/010-hugh-global-cli-installation.md` | Reference research document showing vision cross-reference in prose |
| `/c/repos/.github-private/docs/plans/020-markdown-document-format-for-pch-agents.md` | Reference plan document showing research source link and handoff table |
| `/c/repos/.github-private/docs/research/018-markdown-document-format-for-pch-agents.md` | Research on document format design; context for current Markdown conventions |
| `/c/repos/.github-private/.github/copilot-instructions.md` | Agent architecture overview and agent file format specification |

**Gaps:** None for this phase scope.
**Assumptions:** None.

## Phase 2: Data Contract Formats and Best Practices

**Status:** ✅ Complete
**Session:** 2026-02-17

### Industry Data Contract Standards Overview

Three major open standards define the current industry landscape for data contracts. All three use YAML as the primary machine-readable serialization format and share common structural patterns.

#### ODCS — Open Data Contract Standard (Bitol / LF AI & Data Foundation)

The Open Data Contract Standard (ODCS) v3.1.0 is the most comprehensive specification. Released under Apache 2.0 and maintained by the Linux Foundation, it defines executable contracts between data producers and consumers.

**ODCS v3.1.0 Top-Level Sections:**

| Section | Required | Purpose |
|---------|----------|---------|
| `apiVersion` | Yes | Spec version (e.g., `v3.1.0`) |
| `kind` | Yes | Always `DataContract` |
| `id` | Yes | Globally unique identifier (UUID/URN/slug) |
| `name` | Yes | Human-readable contract name |
| `version` | Yes | Semantic version of the contract itself |
| `domain` | No | Business domain this contract belongs to |
| `status` | No | `active`, `deprecated`, `retired`, `draft` |
| `schema` | Yes | Dataset/table/column definitions (the core) |
| `dataQuality` | No | Data quality rules and thresholds |
| `sla` | No | Service-level agreements (freshness, availability, retention) |
| `pricing` | No | Cost/billing metadata for internal chargeback |
| `stakeholders` | No | Owner, steward, consumers with history |
| `roles` | No | Access roles and permissions |
| `customProperties` | No | Extension fields for org-specific metadata |

**ODCS Schema Section Structure (per entity/table):**
```yaml
schema:
  - name: orders
    physicalName: public.orders
    description: One record per customer order
    properties:
      - name: order_id
        logicalType: string
        physicalType: uuid
        primaryKey: true
        required: true
        unique: true
        description: Unique identifier for the order
      - name: customer_id
        logicalType: string
        physicalType: varchar(36)
        required: true
        description: Foreign key to the customers entity
      - name: total_amount
        logicalType: number
        physicalType: decimal(10,2)
        required: true
        minimum: 0
        description: Order total in USD
        example: "149.99"
      - name: created_at
        logicalType: timestamp
        physicalType: timestamp with time zone
        required: true
```

**Breaking Change Classification (ODCS):**
- Removing an existing field → MAJOR version bump required
- Changing a field's logical type → MAJOR version bump required
- Adding a non-nullable field → MAJOR version bump required
- Adding a nullable/optional field → MINOR version bump (backward compatible)
- Fixing field description/metadata → PATCH version bump

#### Data Contract Specification (datacontract.com)

Inspired by OpenAPI/AsyncAPI conventions. Deprecated in favor of ODCS v3.1.0 but still in wide use until end of 2026.

**Key structural elements:**
- `dataContractSpecification`: Spec version
- `id`: Unique identifier
- `info`: Metadata block (title, version, description, owner, contact)
- `servers`: Data platform connections (BigQuery, S3, Postgres, etc.)
- `terms`: Usage terms (allowed use cases, billing, limitations)
- `models`: Logical data model (tables with fields, types, constraints)
- `servicelevels`: Availability, freshness, latency, retention SLAs
- `quality`: Quality rules attached to models/fields

**Model + Field Level:**
```yaml
models:
  orders:
    description: One record per customer order
    fields:
      order_id:
        type: string
        required: true
        unique: true
        primaryKey: true
        description: Unique order identifier
      customer_id:
        type: string
        required: true
        references: customers.customer_id
        description: Links to the customer entity
      total_amount:
        type: number
        required: true
        minimum: 0
        description: Order total in USD
```

#### dbt Model Contracts

dbt (data build tool) uses YAML-based contracts at the model level within `schema.yml` files. Key features relevant to the PCH context:

- `contract.enforced: true` triggers compile-time schema validation
- Each column must declare `name` and `data_type` explicitly
- Constraints include `not_null`, `unique`, `primary_key`, `foreign_key`, `check`
- **Breaking changes** that dbt detects automatically: removing a column, changing a column's `data_type`, removing or modifying constraints
- Breaking change detection runs at PR time — not just at runtime

**dbt contract example:**
```yaml
models:
  - name: orders
    config:
      contract:
        enforced: true
    columns:
      - name: order_id
        data_type: varchar
        constraints:
          - type: not_null
          - type: primary_key
      - name: total_amount
        data_type: decimal
        constraints:
          - type: not_null
```

### Core Field Set: What a Data Contract Must Define

Synthesizing across ODCS, datacontract.com, dbt, and PayPal's template, the following fields represent the universal minimum for a software-project data contract:

#### Document-Level (Identity & Governance)

| Field | Purpose | Example |
|-------|---------|---------|
| `id` | Unique identifier for the contract | `"dc-001"` |
| `version` | SemVer of the contract itself | `"1.0.0"` |
| `status` | Lifecycle state | `draft`, `active`, `deprecated`, `retired` |
| `domain` | Business/technical domain | `"orders"`, `"users"` |
| `owner` | Team or person accountable for the contract | `"backend-team"` |
| `steward` | Person responsible for day-to-day maintenance | `"@jane"` |
| `created` | Date contract was created | `"2026-02-17"` |
| `updated` | Date contract was last changed | `"2026-02-17"` |
| `description` | What this contract covers | prose |
| `breaking_change_policy` | What triggers a version bump and what the notice period is | prose or table |
| `consumers` | List of agents/services/teams that depend on this contract | list |

#### Entity-Level (What is being defined)

| Field | Purpose |
|-------|---------|
| `entity_name` | Logical name of the data entity (e.g., `Order`, `User`) |
| `physical_name` | Actual class/table/collection name in code |
| `description` | What this entity represents |
| `source_file` | Path to the canonical implementation file (for drift detection) |

#### Field-Level (Per property/column/attribute)

| Field | Purpose | Example |
|-------|---------|---------|
| `name` | Field identifier (exact as used in code) | `order_id` |
| `type` | Logical type | `string`, `number`, `boolean`, `timestamp`, `uuid`, `enum` |
| `physical_type` | Code/language-specific type | `string`, `int`, `DateTime`, `Guid` |
| `required` | Whether null is allowed | `true` / `false` |
| `unique` | Whether values must be unique | `true` / `false` |
| `primary_key` | Is this the entity identifier | `true` / `false` |
| `foreign_key` | Reference to another entity/field | `customers.id` |
| `description` | Business meaning of the field | prose |
| `example` | Sample value | `"ord-12345"` |
| `constraints` | Validation rules | `min: 0`, `max: 100`, `pattern: "^ord-"`, `enum: [active, cancelled]` |
| `deprecated` | Whether this field is being phased out | `true` / `false` |
| `added_in` | Contract version when field was added | `"1.0.0"` |
| `removed_in` | Contract version when field was/will be removed | `"2.0.0"` |

### Governance Best Practices for Data Contracts

#### Versioning Policy (Semantic Versioning)

Industry consensus follows semantic versioning (SemVer: MAJOR.MINOR.PATCH) for contract documents:

| Change Type | Version Bump | Example |
|-------------|-------------|---------|
| Remove field | MAJOR | `1.2.0 → 2.0.0` |
| Rename field | MAJOR | `1.2.0 → 2.0.0` |
| Change field type | MAJOR | `1.2.0 → 2.0.0` |
| Change nullable→required | MAJOR | `1.2.0 → 2.0.0` |
| Add non-nullable field | MAJOR | `1.2.0 → 2.0.0` |
| Add nullable/optional field | MINOR | `1.2.0 → 1.3.0` |
| Add/update description, example | PATCH | `1.2.0 → 1.2.1` |
| Fix typo in constraint | PATCH | `1.2.0 → 1.2.1` |

**Notice period best practice:** Minimum 30-day advance notice for MINOR changes; 90-day notice for MAJOR (breaking) changes. Breaking changes require a deprecation period where both old and new contract versions remain documented.

#### Lifecycle States

| State | Meaning | Consumer Behavior |
|-------|---------|------------------|
| `draft` | Contract is being designed; subject to change | Do not implement against this |
| `active` | Approved and in use | Implement against this; treat as authoritative |
| `deprecated` | Will be removed; a newer version exists | Migrate to newer version; limited support |
| `retired` | No longer supported; no consumers should use this | Do not use |

#### Ownership Model

Three roles are common across ODCS, PayPal template, and industry guidance:

- **Owner**: The team accountable for the data entity and its contract. Approves breaking changes and signs off on new versions. Typically the producing team.
- **Steward**: The individual responsible for keeping the contract accurate, up-to-date, and complete. Responds to consumer questions.
- **Consumers**: The agents, services, or teams that depend on the contract's guarantee. Must be notified of breaking changes.

### Single Document vs. Multiple Entities Per Document

Industry standards disagree slightly but the consensus for software projects is:

| Approach | When to Use | Pros | Cons |
|----------|-------------|------|------|
| One entity per document | Independent entities with separate owners | Clear ownership, easy versioning, granular deprecation | More files, higher cross-reference overhead |
| Multiple entities per document | Tightly coupled entities (e.g., OrderHeader + OrderLine), same domain, same owner | Fewer files, visible relationships | Mixed versioning complexity, harder to deprecate one entity |

**Recommendation for PCH:** One document per entity for entities owned by different teams or with independent lifecycles. One document per domain module (e.g., `dc-orders.md` covering `Order`, `OrderLine`, `OrderStatus`) for tightly coupled aggregates managed by the same team.

### AI-Agent-Specific Considerations for Data Contract Consumption

Based on research into spec-driven development, LLM schema enforcement, and the PCH document-as-truth principle:

#### What pch-visionary Needs to CREATE a Contract

- Entity name(s), domain, and description
- Field names, types, and business meaning (from Q&A with the user)
- Constraints and validation rules
- Owner/steward assignment
- Initial version (`1.0.0`) and status (`draft`)

#### What pch-planner Needs to REFERENCE a Contract

- The full field list with types and required flags — to design database migrations, DTOs, and API payloads
- The `source_file` path — to know which files in the codebase are expected to implement this contract
- The `version` and `status` — to know if this contract is authoritative (`active`) or still being designed (`draft`)
- The `breaking_change_policy` — to know when to flag a planned change as a MAJOR version bump

#### What pch-coder Needs to VERIFY a Contract

For drift detection at implementation time, pch-coder needs:
- Entity `physical_name` → compare against class/interface/type definition in code
- Field `name` → compare against property/column names in the implementation
- Field `type` / `physical_type` → compare against declared types in code
- Field `required` → compare against nullable annotations or NOT NULL constraints
- Field `constraints` → compare against validation attributes or database constraints
- `source_file` path → tells the coder exactly which file to inspect for drift

**Drift categories** (what counts as drift):
1. Field present in contract but missing from code
2. Field present in code but missing from contract (undocumented field)
3. Type mismatch between contract and code
4. Required field nullable in code (or vice versa)
5. Constraint defined in contract but not implemented in code

#### Machine-Parseability Requirements for AI Agents

The PCH system embeds full documents in subagent prompts. For a data contract document to be reliably parsed by an LLM agent:

1. **Flat structure preferred** — LLMs parse Markdown tables more reliably than deeply nested YAML structures (per Research 018 findings)
2. **One table per entity** — Field definitions in a single table are easier for an agent to scan than YAML arrays
3. **Explicit labels** — Every field should have a `**Field:**` or column header, not rely on position
4. **Code blocks for examples** — Example values in fenced code blocks prevent misinterpretation
5. **Consistent column order** — Name → Type → Required → Description → Constraints → Example (consistent order helps LLM pattern matching)

### Recommended PCH Data Contract Markdown Template

Based on Phase 1 findings (PCH Markdown conventions) and industry standards research, the following template is designed for the `/docs/data-contracts/` directory.

**Key design decisions:**
- Follows the same YAML front matter + Markdown body pattern as all other PCH artifacts
- Uses the shared emoji status vocabulary (⏳/🔄/✅/❌/⚠️)
- Field table uses Markdown tables (not YAML arrays) for LLM parseability
- Supports multiple entities per document (domain-level grouping) with one `### Entity` subsection per entity
- Includes `source_file` for drift detection by pch-coder
- Includes `## Breaking Change Log` for governance traceability
- Follows `## Standards Applied` and `## Handoff` footer convention

```markdown
---
id: "dc-001"
type: data-contract
title: "Order Domain Data Contracts"
status: 🔄 In Progress
created: "2026-02-17"
updated: "2026-02-17"
version: "1.0.0"
domain: "orders"
owner: "backend-team"
steward: "@developer"
---

## Overview

Brief description of what this contract covers — which entities, which domain, and the business context.

**Contract Status:** active | draft | deprecated | retired
**Consumers:** [list of services/agents/teams that depend on this contract]
**Vision Source:** [link to originating vision document if applicable]

## Breaking Change Policy

Follows semantic versioning (MAJOR.MINOR.PATCH):
- **MAJOR bump required:** Remove field, rename field, change type, change nullable→required, add required field
- **MINOR bump required:** Add optional/nullable field, add new entity
- **PATCH bump required:** Update description, example, or metadata only
- **Notice period:** 90 days for MAJOR changes; 30 days for MINOR changes
- **Deprecation:** Deprecated fields remain in contract for one MAJOR version before removal

## Entities

### Entity: Order

**Physical Name:** `Order` (class/table/collection name in code)
**Source File:** `src/Models/Order.cs`
**Description:** Represents a customer purchase order. One record per order placed.
**Status:** ✅ Active

| Field | Type | Physical Type | Required | Unique | Description | Constraints | Example |
|-------|------|---------------|----------|--------|-------------|-------------|---------|
| `id` | uuid | string (Guid) | Yes | Yes | Unique order identifier | — | `"ord-abc123"` |
| `customerId` | uuid | string (Guid) | Yes | No | Reference to the owning Customer | foreign_key: Customer.id | `"cust-xyz789"` |
| `status` | enum | string | Yes | No | Current order lifecycle state | enum: [pending, active, completed, cancelled] | `"active"` |
| `totalAmount` | number | decimal | Yes | No | Order total in USD | min: 0, precision: 2 | `149.99` |
| `createdAt` | timestamp | DateTime | Yes | No | When the order was placed | — | `"2026-02-17T10:00:00Z"` |
| `notes` | string | string? | No | No | Optional customer notes | maxLength: 500 | `"Please leave at door"` |

**Relationships:**
- `customerId` → `Customer.id` (many-to-one)
- `id` ← `OrderLine.orderId` (one-to-many)

**Validation Notes:** `status` field transitions follow the state machine: `pending → active → completed | cancelled`. Direct `pending → completed` is not valid.

### Entity: OrderLine

**Physical Name:** `OrderLine`
**Source File:** `src/Models/OrderLine.cs`
**Description:** Individual line item within an order. One record per product in an order.
**Status:** ✅ Active

| Field | Type | Physical Type | Required | Unique | Description | Constraints | Example |
|-------|------|---------------|----------|--------|-------------|-------------|---------|
| `id` | uuid | string (Guid) | Yes | Yes | Unique line item identifier | — | `"line-001"` |
| `orderId` | uuid | string (Guid) | Yes | No | Parent order | foreign_key: Order.id | `"ord-abc123"` |
| `productId` | uuid | string (Guid) | Yes | No | Product being ordered | foreign_key: Product.id | `"prod-456"` |
| `quantity` | integer | int | Yes | No | Number of units ordered | min: 1, max: 999 | `2` |
| `unitPrice` | number | decimal | Yes | No | Price per unit at time of order | min: 0, precision: 2 | `74.99` |

## Version History

| Version | Date | Author | Change Type | Description |
|---------|------|--------|-------------|-------------|
| 1.0.0 | 2026-02-17 | pch-visionary | Initial | Initial contract for Order domain |

## Breaking Change Log

| Version | Date | Field | Change | Consumers Notified | Deprecated Since | Removed In |
|---------|------|-------|--------|-------------------|-----------------|------------|
| — | — | — | — | — | — | — |

## Standards Applied

| Standard | Source | Relevance | Guidance |
|----------|--------|-----------|----------|
| [Standard Name] | pch-standards-space | [Why it applies] | [Guidance used] |

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-visionary |
| Created Date | 2026-02-17 |
| Status | 🔄 In Progress |
| Path | /docs/data-contracts/dc-001-order-domain.md |
```

### Template Design Rationale

| Decision | Choice | Reason |
|----------|--------|--------|
| YAML front matter keys | Same as other PCH artifacts (id, type, title, status, created) | Consistency; agents apply same patterns |
| Status values | Emoji vocabulary (✅/⏳/etc.) | Matches Hugh PlanPhaseParser; consistent with all other artifacts |
| Field definition format | Markdown table (not YAML array) | More LLM-reliable; flatter; matches Research 018 recommendation |
| Multiple entities per document | Yes, one `### Entity` section per entity | Reduces file proliferation for tightly coupled domain entities |
| `source_file` in each entity | Yes, path to implementation file | Enables pch-coder drift detection without searching |
| `domain` in front matter | Yes | Aligns with ODCS; enables grouping/querying by domain |
| `version` in front matter | Yes (contract version, not document version) | Necessary for breaking change tracking |
| Breaking Change Log table | Yes | Permanent audit trail; consumer traceability |
| Version History table | Yes | Same pattern as plan documents; familiar to all PCH agents |
| `## Standards Applied` | Yes | Universal PCH footer; required for standards traceability |
| `## Handoff` | Yes | Universal PCH footer; required for agent routing |

### Evaluation: Tooling-Compatible vs. PCH-Native Format

The ODCS and datacontract.com YAML formats are designed for tooling integration (schema registries, CI/CD validation pipelines, data catalogs). The PCH system does not currently have such tooling. Key evaluation:

| Criterion | Industry YAML (ODCS/datacontract.com) | PCH Markdown Template |
|-----------|--------------------------------------|-----------------------|
| AI agent parseability | Medium — LLMs are prone to YAML generation errors (per Research 018) | High — Markdown tables are LLM-reliable |
| Human readability | Medium — verbose YAML nesting | High — tables are scannable |
| Tooling integration | High — CLI tools, validators, registries | None (not needed for current PCH scope) |
| PCH convention alignment | Low — different format from all other PCH artifacts | High — same patterns as vision/research/plan docs |
| Schema drift detection | High (automated) | Medium (LLM-assisted, not automated) |
| Token efficiency when embedded | Low (YAML verbose) | Medium-High (Markdown tables compact) |

**Conclusion:** The PCH Markdown template is the correct choice for the current PCH system scope. If the project later adopts a data catalog or CI schema validation pipeline, the Markdown contracts could be transpiled to ODCS YAML by a tool without changing the source-of-truth document format.

### Key Discoveries from Phase 2

- The ODCS v3.1.0 (Bitol/Linux Foundation) is the authoritative industry standard; datacontract.com spec is deprecated in favor of ODCS by end of 2026
- Industry contracts universally require: identity (id, version, status), governance (owner, steward, consumers), schema (entities + fields + types + constraints), versioning policy, and a breaking change log
- Breaking changes follow semantic versioning: field removal/rename/type-change = MAJOR; adding optional fields = MINOR; metadata updates = PATCH
- 90-day notice for breaking changes is the industry-recommended minimum notice period
- One contract per entity is the canonical best practice; multiple related entities in one document is acceptable for tightly coupled domain aggregates with the same owner
- For AI agent consumption, Markdown tables are more reliably parsed than YAML arrays (consistent with Research 018 findings)
- pch-coder needs `source_file`, field `name`, `type`, `required`, and `constraints` to perform drift detection — these are the minimum fields for a drift-checkable contract
- pch-visionary needs entity name, domain, field business meaning, and owner to CREATE a contract from Q&A
- pch-planner needs `source_file`, full field list with types, and contract `version`/`status` to reference a contract in a plan
- The `draft` → `active` lifecycle transition should be the trigger for the contract becoming authoritative (before `active`, it is informational only)

| File | Relevance |
|------|-----------|
| `/c/repos/.github-private/docs/research/018-markdown-document-format-for-pch-agents.md` | Source of Markdown format conventions used in template design; LLM table reliability findings |
| `/c/repos/.github-private/docs/plans/020-markdown-document-format-for-pch-agents.md` | Reference implementation of Markdown template conventions |
| `/c/repos/.github-private/docs/vision/001-pch-agent-presentation.md` | Reference vision document showing Document Information table header style |
| `/c/repos/.github-private/agents/pch-planner.md` | Shows Technical Design section needs (data models, API contracts) that data contract should feed |
| `/c/repos/.github-private/agents/pch-coder.md` | Consumer of contracts; no existing contract-reading behavior — gap identified |
| `/c/repos/.github-private/agents/pch-visionary.md` | Potential contract creator; currently no contract creation instructions |

## Phase 3: Agent-Specific Integration Points

**Status:** ✅ Complete
**Session:** 2026-02-17

### Summary

Every PCH agent has a distinct, non-overlapping role in the data contract lifecycle. The integration points are determined by reading the existing section structure of each agent prompt and identifying the exact insertion locations. No agent currently has any data contract awareness — all clauses must be added as new instructions, following the established `pch-standards-space` query pattern as the closest analogue.

### Agent Role Matrix

| Agent | Primary Role | Lifecycle Action | Contract Operation |
|-------|-------------|------------------|--------------------|
| pch-visionary | Creator | Stage 5: Architecture Q&A | CREATE new `/docs/data-contracts/NNN-*.md` file |
| pch-researcher | Reader | Before beginning research | READ existing contracts as context |
| pch-planner | Referencer | Technical Design section | REFERENCE contracts, link in plan |
| pch-plan-reviewer | Validator | Review checklist | VALIDATE plan references contracts with `active` status |
| pch-coder | Enforcer | Per-phase implementation | DETECT drift between contract and code |

---

### Agent 1: pch-visionary

#### Primary Role: CREATE

pch-visionary is the only agent that creates data contracts. Its six-stage Q&A flow gathers all the information needed to draft a contract by Stage 5 (Architecture). By Stage 5, the following data entity information has already been collected from the user through Stages 1–4:
- Domain and business context (Stage 1: Problem Space)
- Goals and success metrics that imply data entities (Stage 3)
- Functional requirements that reference data objects (Stage 4)
- Technology/storage decisions (Stage 5: Architecture)

**When the agent learns about data entities:** Entities emerge progressively across Stages 1–4 but are most precisely defined during Stage 4 (Requirements — functional requirements reference specific data models) and Stage 5 (Architecture — storage decisions and data models are discussed).

**Trigger condition:** pch-visionary should create a data contract when the solution involves a **persistent data entity** (i.e., a named class, record type, database table, or API request/response body that will be stored or transmitted). It should do so proactively — without waiting for the user to mention "data model" explicitly — whenever the functional requirements imply a named data structure.

**Decision — proactive vs. on-demand:** Proactive creation is correct. The vision document is the earliest point in the lifecycle where data entities are named; waiting until the planner stage means the contract arrives too late to inform the research phase.

#### Exact Insertion Point in pch-visionary.md

The agent's prompt has a `## Six-Stage Question Flow` section with a `### Stage 5: Architecture Vision` subsection. Within Stage 5, the existing questions are:
1. Solution Approach (solution style)
2. Key Technology Decisions
3. Integration Points
4. Security Considerations

The data contract creation instruction should be inserted **after all Stage 5 questions are answered** and **before the `### Stage 6: Product Phasing Strategy`** section. This mirrors the standards query pattern: Query #3 fires at Stage 5, so contract creation is also anchored at Stage 5.

Additionally, a new entry should be added to the **`## Vision Completion Checklist`** under "Required Sections" to require a `## Data Contracts Created` section when data entities were identified.

The **`## Vision Document Template`** should also gain a conditional section:

```markdown
## Data Contracts Created

| Contract File | Entities | Status | Vision Phase |
|---------------|----------|--------|--------------|
| `/docs/data-contracts/NNN-domain.md` | EntityName | draft | Phase 2 |
```

#### What Instructions to Inject (Draft Clause for pch-visionary)

Insert after the Stage 5 question templates and before Stage 6:

```markdown
### Data Contract Creation (Stage 5: Post-Architecture)

After completing all Stage 5 questions (solution style, technology decisions, integrations, security), identify whether the solution involves persistent data entities.

**Trigger:** If the solution has any of the following, create data contracts:
- Named domain objects that will be stored in a database
- API request or response bodies that define a schema
- Named DTOs, models, records, or value objects referenced in the requirements

**How to Create a Contract:**

1. **List `/docs/data-contracts/`** to find the next available number (same `max + 1` pattern as other artifact directories)
2. **Create `/docs/data-contracts/NNN-{domain-name}.md`** using the PCH data contract template (see Phase 2 research: `## Recommended PCH Data Contract Markdown Template`)
3. **Set status to `draft`** — the contract is not authoritative until the planner confirms it
4. **Populate entities** from what was captured in Stages 1–5:
   - Entity names from functional requirements (Stage 4)
   - Field names and types from technology decisions and integration points (Stage 5)
   - Domain from problem space (Stage 1)
   - Owner from stakeholders (Stage 2)
5. **Record `source_file` as TBD** — the actual implementation path is not known until the planner assigns it
6. **Add to the vision document** under `## Data Contracts Created` table with the contract file path, entity names, status, and which vision phase the entities belong to
7. **Ask the user to confirm entity names and fields** before marking the contract `active` (use a Type C validation question)

**When No Contracts Are Needed:**
If the solution is documentation-only, configuration-only, or has no named persistent data entities, skip this step and note "No data contracts required" in the `## Data Contracts Created` section.

**Fallback:** If you cannot determine entity structure from the conversation so far, note the gap in the `## Data Contracts Created` section and list it as a research topic in the relevant phase.
```

#### Document Updates pch-visionary Should Perform

1. Create `/docs/data-contracts/NNN-{domain}.md` with status `draft`
2. Update the vision document's `## Data Contracts Created` table
3. Log the contract creation decision in the `## Decisions Log` table (Stage 5 entry)

#### New Section vs. Existing Section

- **Modified:** `## Six-Stage Question Flow` → `### Stage 5: Architecture Vision` — add post-Stage-5 data contract creation procedure
- **Modified:** `## Vision Completion Checklist` → add `Data Contracts Created` to required sections checklist
- **Modified:** `## Vision Document Template` → add `## Data Contracts Created` section to template

---

### Agent 2: pch-researcher

#### Primary Role: READ (Context Consumer)

pch-researcher's role is not to create or validate contracts but to **read existing contracts as input context** when research is relevant to a domain that has declared contracts. This prevents research findings from being inconsistent with declared entity structures.

**When:** Before beginning research on a phase that involves data entities or implementation of domain models, pch-researcher should check whether a data contract exists for that domain and read it to inform research findings.

**How the agent currently discovers related artifacts:** The researcher already reads the `vision_source` document when one exists. The same mechanism applies: if the vision document has a `## Data Contracts Created` table, the researcher should read each referenced contract before starting the phase.

#### Exact Insertion Point in pch-researcher.md

The researcher has a `## Automatic Mode Workflow` section with an `### Orchestration Loop` that begins:

> "1. **Load the research document** — Read the full research document; parse all phases and their current status; identify phases with `not-started`. If the document references a vision document (`vision_source`), read it for context — the full vision is passed to subagents alongside the research document."

This step 1 is the correct injection point. The data contract loading should be added as a sub-step of step 1.

Additionally, the **`### Subagent Invocation`** section's prompt template should be extended to optionally include a `## DATA CONTRACT CONTEXT` block (parallel to the existing `## VISION DOCUMENT CONTEXT` block).

#### What Instructions to Inject (Draft Clause for pch-researcher)

In the `### Orchestration Loop`, modify step 1 to add:

```markdown
   - If the vision document has a `## Data Contracts Created` table, read each referenced contract file from `/docs/data-contracts/`. Pass any contracts relevant to this research phase to the subagent in the prompt under a `## DATA CONTRACT CONTEXT` section.
```

In the **`### Subagent Invocation`** prompt template, add after the `{end_if_vision_source}` block:

```markdown
{if_data_contracts_exist}
## DATA CONTRACT CONTEXT

The following data contracts have been declared for this project. When researching implementation patterns, ensure your findings are consistent with these declared entity structures. If you discover that existing code already diverges from these contracts, note the drift in your findings.

{for_each_relevant_contract}
**Contract:** {contract_file_path}
{contract_document_content}
{end_for_each_contract}
{end_if_data_contracts_exist}
```

#### What Subagents Should Do with Contracts

Subagents that receive data contract context should:
1. Use the declared field names and types when describing implementation patterns
2. Flag any research findings where existing code diverges from the declared contract (this is pre-coder drift detection)
3. Note if a contract is still `draft` — this means fields may change and the researcher should flag this as a risk

#### Document Updates pch-researcher Should Perform

- If a research finding reveals that an existing data contract is incorrect or incomplete, the subagent should include a `**Contract Gap:**` note in its findings, and the orchestrator should surface this to the user
- The researcher does NOT update the contract file directly — that is pch-visionary's (or pch-planner's) responsibility

#### New Section vs. Existing Section

- **Modified:** `## Automatic Mode Workflow` → `### Orchestration Loop` step 1 — add contract loading sub-step
- **Modified:** `### Subagent Invocation` prompt template — add `## DATA CONTRACT CONTEXT` optional block

---

### Agent 3: pch-planner

#### Primary Role: REFERENCE and PROMOTE

pch-planner has two contract responsibilities:
1. **Reference** existing contracts in the `## Technical Design` section of the plan — specifically in the data models and API contracts subsections
2. **Promote** contracts from `draft` to `active` — when the planner finalizes the data model for a phase, it should update the referenced contract's status from `draft` to `active` and fill in the `source_file` field

**How the planner currently handles data model definitions:** The plan structure includes a `## Technical Design` section with subsections for data models, API contracts, and component specifications. This is exactly where contract references belong.

#### Exact Insertion Point in pch-planner.md

The agent has a `## Plan Structure` section that lists the required `##` sections including `## Technical Design`. The subsection format is not specified in the agent prompt — it uses `### Codebase Patterns` as a sub-section example.

The injection points are:

1. **`## Context Gathering (Step 0)`** — Add a step to check `/docs/data-contracts/` for existing contracts before beginning planning.
2. **`## Plan Structure` → `## Technical Design`** — Add guidance that data model definitions should reference contracts rather than re-declare field lists.
3. **`## Workflow Process` → Step 1 (Create Initial Plan Document)** — Add a step to populate the `## Technical Design` section with contract references.
4. **After holistic review (Step 4)** — Add a step to update contract `source_file` fields and promote contracts from `draft` to `active`.

#### What Instructions to Inject (Draft Clause for pch-planner)

In `## Context Gathering (Step 0)`, after the existing step 4 (Summarize findings) and before step 5 (Query Standards), add:

```markdown
**Step 4.5 — Check for Data Contracts:**

Before beginning detailed planning, check whether data contracts exist for the entities involved in this plan.

1. **List `/docs/data-contracts/`** — Find any contracts whose domain matches the feature being planned
2. **If the vision document is available**, check its `## Data Contracts Created` table for contract file paths
3. **Read each relevant contract** — Note the entities, their `source_file` fields (may be `TBD`), and the contract `status`
4. **Record findings** — Note which contracts are `draft` vs `active` and which `source_file` paths are still `TBD`

These contracts become the authoritative schema reference for the plan's Technical Design section.
```

In the `## Plan Structure` description of `## Technical Design`, add:

```markdown
**Data Contract References (required when data entities are involved):**

When the plan involves named data entities that have corresponding data contracts in `/docs/data-contracts/`, the Technical Design section must reference them:

```markdown
### Data Contracts

| Contract | Entity | Status | Source File | Contract Path |
|----------|--------|--------|-------------|---------------|
| `dc-001-order-domain.md` | Order | active | `src/Models/Order.cs` | `/docs/data-contracts/001-order-domain.md` |
| `dc-001-order-domain.md` | OrderLine | active | `src/Models/OrderLine.cs` | `/docs/data-contracts/001-order-domain.md` |
```

Each implementation task that creates or modifies a data entity must reference the corresponding contract entry.
```

After Step 4 (Holistic Review) in `## Workflow Process`, add:

```markdown
**Step 4.5 — Update Data Contract Source Files:**

After finalizing implementation file paths in the plan:

1. For each data contract referenced in the plan's `### Data Contracts` table:
   - If `source_file` is `TBD`, update it with the actual implementation file path from the plan
   - If the contract status is `draft`, change it to `active` now that the plan has committed to implementing it
2. Update the contract document's `## Handoff` table to show `pch-planner` as the last agent to update it
3. If the plan introduces new entities not yet in any contract, create a new contract file or add the entity to an existing domain contract
```

#### Document Updates pch-planner Should Perform

1. Update `source_file` in each entity's data contract from `TBD` to the actual planned implementation path
2. Change contract status from `draft` to `active` for contracts that the plan is committing to implement
3. Create new contract entries if the plan introduces new entities not yet documented
4. Add a `### Data Contracts` table to the plan's `## Technical Design` section

#### New Section vs. Existing Section

- **Modified:** `## Context Gathering (Step 0)` — add Step 4.5 for contract discovery
- **Modified:** `## Plan Structure` → `## Technical Design` — add `### Data Contracts` subsection guidance
- **Modified:** `## Workflow Process` — add Step 4.5 for contract source_file and status updates

---

### Agent 4: pch-plan-reviewer

#### Primary Role: VALIDATE

pch-plan-reviewer's role is to check that the plan correctly references data contracts and that those contracts have the correct status before approving the plan for implementation.

**Specific checks to perform:**

1. **Contract completeness check:** Does the plan's `### Data Contracts` table exist if the feature involves data entities?
2. **Status check:** Are all referenced contracts in `active` status? A `draft` contract referenced in a plan is a Major issue — it means the schema may still change.
3. **Source file check:** Are all `source_file` fields in referenced contracts populated (not `TBD`)? A `TBD` source_file means pch-coder cannot perform drift detection.
4. **Entity coverage check:** Does every data entity mentioned in the Technical Design have a corresponding contract entry?
5. **No orphan contract check:** Are there contracts in `/docs/data-contracts/` that relate to this feature but are not referenced in the plan?

#### Exact Insertion Point in pch-plan-reviewer.md

The agent has a `## Review Checklist` section with four categories: Correctness, Clarity, Specificity, Completeness. The contract checks belong in both **Correctness** and **Completeness**.

Additionally, the `## Common Plan Defects to Watch For` section should gain a new subsection for data contract defects.

#### What Instructions to Inject (Draft Clause for pch-plan-reviewer)

In `## Review Checklist` → `### Correctness`, add:

```markdown
- [ ] All data entities in Technical Design have corresponding contract entries in the `### Data Contracts` table
- [ ] Referenced data contracts have `active` status (not `draft`) in `/docs/data-contracts/`
- [ ] All contract `source_file` fields are populated (not `TBD`) for entities in this plan's scope
```

In `## Review Checklist` → `### Completeness`, add:

```markdown
- [ ] `### Data Contracts` table present in `## Technical Design` if plan involves persistent data entities
- [ ] Contract paths in the Data Contracts table are valid (files exist in `/docs/data-contracts/`)
```

In `## Common Plan Defects to Watch For`, add a new subsection:

```markdown
### Data Contract Issues

- `### Data Contracts` table absent when plan involves database schema or DTOs
- Contracts referenced with `draft` status — schema may still change, blocking coder drift detection
- `source_file` is `TBD` — pch-coder cannot find the implementation file to check for drift
- Contract entity not found in `/docs/data-contracts/` (path in table is incorrect or file was deleted)
- New entity defined in plan but no contract created or updated
```

In `## Standards Integration` → `### Verifying Standards in Plans`, add a note that contract verification is a mandatory check regardless of the standards query result.

#### Severity Mapping for Contract Issues

| Contract Defect | Severity | Rationale |
|----------------|----------|-----------|
| `### Data Contracts` table absent when entities exist | Major | pch-coder cannot perform drift detection without it |
| Referenced contract has `draft` status | Major | Schema may change; contract is not authoritative |
| `source_file` is `TBD` | Major | pch-coder cannot locate the implementation file |
| Contract file does not exist at stated path | Critical | Broken reference; plan refers to a nonexistent artifact |
| New entity in plan with no contract | Minor | Contract can be created retroactively; does not block implementation |

#### Document Updates pch-plan-reviewer Should Perform

- Update the plan's `## Review Session Log` table with contract-specific review findings
- If a contract has `draft` status, add a `Major` issue and recommend the planner promote it to `active`
- If `source_file` is `TBD`, add a `Major` issue and recommend the planner update the contract before handoff
- Add review notes to the plan's `## Review Summary` section calling out contract-specific findings

#### New Section vs. Existing Section

- **Modified:** `## Review Checklist` → `### Correctness` — add 3 contract checks
- **Modified:** `## Review Checklist` → `### Completeness` — add 2 contract checks
- **Modified:** `## Common Plan Defects to Watch For` — add `### Data Contract Issues` subsection

---

### Agent 5: pch-coder

#### Primary Role: ENFORCE (Drift Detection)

pch-coder is the enforcement agent. Its job is to compare the declared contract against the actual code implementation and flag mismatches (drift). This happens **after each phase implementation** — specifically during the quality gate that already exists in the agent.

**Where to anchor drift detection:** The pch-coder agent already has a **`## Post-Implementation Code Review`** step that fires after all phases complete. However, drift detection should happen earlier — specifically per-phase — because:
- Drift caught immediately after implementing an entity is faster to fix
- The subagent that just created the file has it in context and can self-correct

**Precise workflow for drift detection:**

a. The subagent invocation prompt already receives the full plan document. The plan's `### Data Contracts` table lists which contracts apply and which `source_file` each entity maps to.
b. After implementing a task that creates or modifies a data entity, the subagent should:
   1. Read the relevant contract from `/docs/data-contracts/`
   2. Read the `source_file` implementation
   3. Compare fields, types, required flags, and constraints
   4. Report any drift in the `DEVIATIONS` field of the structured return format

**The 5 drift categories to check (from Phase 2 research):**
1. Field present in contract but missing from implementation
2. Field present in implementation but absent from contract (undocumented field)
3. Type mismatch between contract and implementation
4. Required field nullable in implementation (or vice versa)
5. Constraint declared in contract but not enforced in implementation

#### Exact Insertion Point in pch-coder.md

Three insertion points:

1. **`### Subagent Invocation`** prompt template — the instructions block (`## YOUR INSTRUCTIONS`) should gain a new section for drift detection when the phase involves data entities.

2. **`## Post-Implementation Code Review`** subagent prompt template — extend the review checklist to include data contract drift as a fourth category.

3. **`## Task Execution Standards`** — add a drift detection rule under the `### Code Quality Requirements` section.

#### What Instructions to Inject (Draft Clause for pch-coder)

In the **`### Subagent Invocation`** prompt template's `## YOUR INSTRUCTIONS` block, add after "Report any blockers immediately":

```markdown
**Data Contract Drift Detection (if applicable):**

If this phase creates or modifies a data entity that has a corresponding entry in the plan's `### Data Contracts` table:

1. **Identify the contract:** Find the contract file path from the plan's `### Data Contracts` table for the entity you just implemented
2. **Read the contract:** Load `/docs/data-contracts/{contract-file}` and locate the entity's field table
3. **Read the implementation:** Load the `source_file` path from the contract entity's `Source File` field
4. **Compare fields** against the contract's field table:
   - Missing field: Field in contract but not in implementation
   - Undocumented field: Field in implementation but not in contract
   - Type mismatch: `Physical Type` in contract differs from actual declared type
   - Nullable mismatch: `Required: Yes` in contract but field is nullable in code (or vice versa)
   - Missing constraint: `Constraints` in contract not enforced in implementation (e.g., `[Required]` attribute absent, `maxLength` not validated)
5. **Report drift** in the `DEVIATIONS` field of your return format:
   ```
   DEVIATIONS: [drift] Order.totalAmount — contract declares decimal, implementation uses float
   DEVIATIONS: [drift] Order — contract field 'notes' missing from implementation
   ```
6. **Report clean** if no drift detected:
   ```
   DEVIATIONS: [contract-check] Order entity matches contract dc-001. No drift detected.
   ```

**Only perform this check for entities in the plan's `### Data Contracts` table.** Do not check entities not covered by a contract.
```

In the **`## Post-Implementation Code Review`** subagent prompt template, add a fourth review category after "Decommissioned / dead code not removed":

```markdown
4. **Data contract drift on created/modified entities**
   - For each entity in the plan's `### Data Contracts` table whose `source_file` was created or modified during this implementation run:
     - Read the contract from `/docs/data-contracts/`
     - Compare the entity's declared fields against the implementation
     - Flag: missing fields, undocumented fields, type mismatches, nullable mismatches, missing constraints
   - This is a final drift sweep — it catches any drift that per-phase checks may have missed
```

The review output format should be extended to include contract drift findings under the same `FINDINGS` array:

```
- [contract-drift] src/Models/Order.cs — field 'notes' declared in contract dc-001 but absent from class
- [contract-drift] src/Models/Order.cs — field 'totalAmount' is float in code, decimal declared in contract
```

#### Orchestrator Behavior for Drift Findings

The pch-coder orchestrator (the non-subagent execution path) should treat contract drift findings as follows:

| Drift Type | Severity | Orchestrator Action |
|-----------|----------|---------------------|
| Missing field (in contract, not in code) | High | Flag as implementation gap; ask user whether to add the field or update the contract |
| Undocumented field (in code, not in contract) | Medium | Flag as contract gap; recommend updating the contract to add the field |
| Type mismatch | High | Flag as breaking risk; require user decision before continuing |
| Nullable mismatch | High | Flag as breaking risk; require user decision before continuing |
| Missing constraint | Medium | Flag as quality gap; recommend adding constraint to implementation |

Contract drift findings should be surfaced **in the same `findings` report** as the existing code review findings (DTOs, exception handling, dead code), so the user sees a unified quality report.

#### Per-Phase vs. Post-Implementation Drift Detection

| Check | When | Performed By |
|-------|------|-------------|
| Per-entity drift check | After each phase that implements a data entity | Subagent (in DEVIATIONS field) |
| Full drift sweep | After all phases complete, as part of code review | Code review subagent |

The orchestrator should NOT re-invoke the same phase subagent for drift — it should fix drift either in the code review fix cycle (existing mechanic) or by asking the user.

#### Document Updates pch-coder Should Perform

- Add contract drift findings to the plan's `## Implementation Notes` section:
  ```markdown
  **Data Contract Check:** Order entity — 1 drift finding (totalAmount type mismatch)
  ```
- If drift is resolved (user chose to fix), note resolution:
  ```markdown
  **Data Contract Check:** Order entity — drift resolved: totalAmount changed from float to decimal
  ```
- If drift is accepted (user chose not to fix), note acceptance:
  ```markdown
  **Data Contract Check:** Order entity — drift accepted: totalAmount remains float (user decision 2026-02-17)
  ```

#### New Section vs. Existing Section

- **Modified:** `### Subagent Invocation` prompt template `## YOUR INSTRUCTIONS` — add data contract drift detection block
- **Modified:** `## Post-Implementation Code Review` subagent prompt — add 4th review category for contract drift
- **No new sections needed** — all changes are additions to existing sections

---

### Orchestrator vs. Per-Subagent Contract Loading

**Question from Phase 3 scope:** Should the orchestrator handle contract loading, or should each subagent read contracts independently?

**Finding:** The existing pch-researcher and pch-coder architectures both follow the **orchestrator-embeds-context** pattern — the orchestrator reads artifacts (vision document, plan document) and embeds them verbatim in subagent prompts. The subagents do not read files independently; they receive everything in their prompt.

**Recommendation:** Follow the same pattern for data contracts:
- The **orchestrator** (pch-researcher's Orchestration Loop, pch-coder's Orchestration Loop) reads the contract files and embeds them in the subagent prompt
- The **subagent** receives the embedded contract and performs drift detection without needing file access
- This maintains isolation: subagents receive a complete, frozen snapshot of the contract at the moment the phase starts

**Exception:** pch-visionary and pch-planner are not subagent-based (they are interactive, session-based agents). These agents read contract files directly as part of their own context gathering steps.

---

### Cross-Agent Contract Update Coordination

One risk with multiple agents modifying contract files is conflicting updates. The recommended ownership model:

| Operation | Who Performs It | When |
|-----------|----------------|------|
| Create contract file | pch-visionary | Stage 5 of vision Q&A |
| Set `source_file` + promote to `active` | pch-planner | Step 4.5 after holistic review |
| Read contracts as context | pch-researcher | Orchestration Loop step 1 |
| Validate contract references | pch-plan-reviewer | Review checklist |
| Detect and report drift | pch-coder | Per-phase + post-impl code review |
| Update contract fields/version | pch-planner OR pch-coder | Planner: planned changes; Coder: resolving drift found during implementation |

No two agents write to the same contract at the same time — the workflow is sequential (visionary → researcher → planner → reviewer → coder). Race conditions are not possible in the single-session sequential workflow.

---

### Key Discoveries from Phase 3

- pch-visionary is the designated contract CREATOR — its Stage 5 (Architecture) is where entity names, types, and constraints are first known in enough detail to draft a contract
- pch-researcher should READ contracts as context but not modify them — it adds a `## DATA CONTRACT CONTEXT` block to subagent prompts, parallel to the existing `## VISION DOCUMENT CONTEXT` pattern
- pch-planner has two contract duties: REFERENCE contracts in the `### Data Contracts` table in Technical Design, and PROMOTE contracts from `draft` to `active` once implementation file paths are confirmed
- pch-plan-reviewer adds contract-specific checks to its existing `## Review Checklist` — a `draft` status or `TBD` source_file is a Major issue that blocks implementation
- pch-coder's drift detection fits into two existing mechanisms: the per-phase subagent's `DEVIATIONS` field, and the post-implementation `## Post-Implementation Code Review` (as a 4th review category)
- The orchestrator-embeds-context pattern (already used for vision and plan documents) is the correct mechanism for contract loading in researcher and coder subagents
- Contract ownership is strictly sequential — visionary creates, planner activates, coder detects drift — no simultaneous multi-agent writes possible

| File | Relevance |
|------|-----------|
| `c:\repos\.github-private\agents\pch-visionary.md` | Stage 5 Architecture Q&A is the contract creation trigger; `## Vision Completion Checklist` and `## Vision Document Template` need new contract sections |
| `c:\repos\.github-private\agents\pch-researcher.md` | `## Automatic Mode Workflow` → `### Orchestration Loop` step 1 is the contract loading insertion point; `### Subagent Invocation` template needs `## DATA CONTRACT CONTEXT` block |
| `c:\repos\.github-private\agents\pch-planner.md` | `## Context Gathering (Step 0)` needs Step 4.5 for contract discovery; `## Plan Structure → ## Technical Design` needs `### Data Contracts` guidance; `## Workflow Process` Step 4.5 for promoting contracts |
| `c:\repos\.github-private\agents\pch-plan-reviewer.md` | `## Review Checklist → ### Correctness` and `### Completeness` need contract checks; `## Common Plan Defects to Watch For` needs `### Data Contract Issues` |
| `c:\repos\.github-private\agents\pch-coder.md` | `### Subagent Invocation` `## YOUR INSTRUCTIONS` needs drift detection block; `## Post-Implementation Code Review` needs 4th review category |

**Gaps:** None for this phase scope.
**Assumptions:** pch-visionary creates contracts proactively (not only when user mentions "data model") — this is assumed based on the pattern of pch-standards-space being queried proactively at defined lifecycle points rather than only on user request.

## Phase 4: Model Drift Detection in Code Review

**Status:** ✅ Complete
**Session:** 2026-02-17

### pch-coder.md Structural Analysis

The existing `## Post-Implementation Code Review` subagent has exactly **three** review categories and a specific output format:

**Current 3 categories:**
1. Missing or incorrect properties on DTOs / models (wrong types, missing nullability, mismatched names, missing `[Required]`)
2. Bad exception handling patterns (empty catch, catching base Exception, missing await, logging ex.Message only)
3. Decommissioned / dead code not removed (old methods/classes, commented-out code, unused imports, unreachable paths)

**Current output format:**
```
REVIEW_STATUS: clean | findings
FINDINGS_COUNT: {number}

FINDINGS:
- [{category}] {file_path}:{line} — {description}
```

The `DEVIATIONS` field in the per-phase subagent return format currently stores plan-deviation entries. The design uses a `[drift-high]` or `[drift-medium]` prefix to distinguish contract entries from plan deviations. Both can coexist in the same `DEVIATIONS` field.

### Type Mapping: Contract Logical Types → C# Physical Types

| Contract `type` | Acceptable C# Physical Types | Drift if Code Uses |
|-----------------|-----------------------------|--------------------|
| `string` | `string`, `string?` | Any numeric type, `bool`, `DateTime`, `Guid` without string wrapper |
| `number` | `decimal`, `double`, `float`, `int`, `long` (+ nullable variants) | `string`, `bool`, `DateTime` |
| `integer` | `int`, `long`, `short`, `byte` (+ nullable variants) | `decimal`, `double`, `float`, `string` |
| `boolean` | `bool`, `bool?` | `int`, `string` |
| `timestamp` | `DateTime`, `DateTimeOffset` (+ nullable variants) | `string`, `long` (unix epoch — flag as warning) |
| `uuid` | `Guid`, `Guid?`, `string` (Guid-formatted) | `int`, numeric types |
| `enum` | C# `enum` type, or `string` with enum validation | `int` without explicit enum mapping |
| `object` | Custom class/record type | Primitive types |
| `array` | `List<T>`, `IEnumerable<T>`, `T[]`, `ICollection<T>` | Single-value types |

**Note:** If `physical_type` is declared in the contract, it takes precedence over `type` for drift comparison.

### Nullable Mapping

| Contract `required` | Expected C# annotation | Drift conditions |
|---------------------|------------------------|-----------------|
| `required: true` | Non-nullable type (no `?`) | Field is nullable (`?` suffix present) |
| `required: false` | Nullable type (`?` suffix) or absent | Field is non-nullable value type |

### Constraint Mapping

| Contract Constraint | Expected C# Implementation | Drift if Missing |
|--------------------|---------------------------|-----------------|
| `maxLength: N` | `[MaxLength(N)]` or `[StringLength(N)]` | Attribute absent |
| `minLength: N` | `[MinLength(N)]` | Attribute absent |
| `min: N` / `max: N` | `[Range(min, N)]` | Attribute absent |
| `pattern: "regex"` | `[RegularExpression("regex")]` | Attribute absent |
| `enum: [...]` | C# `enum` type or `[AllowedValues]` | No validation present |
| `unique: true` | EF `HasIndex().IsUnique()` or `[Key]` | Neither present (informational) |
| `primary_key: true` | `[Key]` or EF `HasKey()` | Neither present |
| `foreign_key: Entity.field` | Navigation property + `[ForeignKey]` or EF relationship | No FK relationship configured |

Constraints enforced via EF Fluent API or FluentValidation are acceptable alternatives to Data Annotation attributes.

### Drift Severity Levels and Blocking Behavior

| # | Drift Category | Severity | Per-Phase Action | Post-Sweep Action |
|---|---------------|----------|-----------------|------------------|
| 1 | Field in contract missing from code | HIGH | Report `[drift-high]` in DEVIATIONS | Block completion; require user decision |
| 2 | Field in code missing from contract | MEDIUM | Report `[drift-medium]` in DEVIATIONS | Warn; offer to update contract |
| 3 | Type mismatch | HIGH | Report `[drift-high]` in DEVIATIONS | Block completion; require user decision |
| 4 | Nullable mismatch | HIGH | Report `[drift-high]` in DEVIATIONS | Block completion; require user decision |
| 5 | Constraint declared but not enforced | MEDIUM | Report `[drift-medium]` in DEVIATIONS | Warn; offer to fix |

**Blocking rule:** Per-phase subagents NEVER block on drift — they always complete to `success`. The post-implementation sweep is the enforcement gate. HIGH severity findings pause before the final completion report and require user decision.

### Per-Phase Drift Detection Instruction Block (Ready for Injection)

```markdown
**Data Contract Drift Detection (perform if this phase creates or modifies a data entity):**

Check the plan's `### Data Contracts` table (in `## Technical Design`). If any entity's `source_file` is created or modified by this phase:

1. Read the contract file from the path in the `### Data Contracts` table
2. Find the `### Entity: {PhysicalName}` block in the contract
3. Read the `source_file` implementation
4. Compare field by field:
   - `[drift-high]` — Field in contract not found as a property in the class
   - `[drift-medium]` — Property in class not listed in contract
   - `[drift-high]` — `Physical Type` in contract differs from actual C# type
   - `[drift-high]` — `Required: Yes` but property has `?` suffix (or vice versa)
   - `[drift-medium]` — Constraint in `Constraints` column has no corresponding attribute
5. Report in DEVIATIONS: `[drift-high] Order.totalAmount — contract declares decimal, implementation uses float`
   If clean: `[contract-check] Order — matches contract dc-001. No drift detected.`
6. Do NOT fix drift — report only.

Skip if: phase does not touch any entity in `### Data Contracts`; plan has no `### Data Contracts` table; `source_file` is `TBD`.

**Name matching:** Compare case-insensitively. Contract `totalAmount` matches C# `TotalAmount`.

**Partial classes:** If entity class contains `partial` keyword, also check `{ClassName}.*.cs` files in same directory.

**Inheritance:** If entity inherits from a base class, also read the base class file (one level up) and include inherited properties.
```

### 4th Review Category for Post-Implementation Code Review (Ready for Injection)

```markdown
4. **Data contract drift (final sweep)**

   For each entity in the plan's `### Data Contracts` table:
   a. Confirm the entity's `source_file` was created or modified during this run
   b. Read the contract from `/docs/data-contracts/`; compare all declared fields against the implementation
   c. Check all five drift categories (same as per-phase, but as a comprehensive final sweep)
   d. Report using `[drift-high]` and `[drift-medium]` category labels

   If no `### Data Contracts` table exists: note "No data contracts declared — contract drift check skipped."
```

**Extended output format:**
```
REVIEW_STATUS: findings
FINDINGS_COUNT: 4
CONTRACT_DRIFT_SUMMARY: 2 high-severity drift findings; 1 medium-severity; 1 medium-severity

FINDINGS:
- [dto-properties] src/Models/Order.cs:12 — property 'Status' missing [Required] attribute
- [drift-high] src/Models/Order.cs:8 — field 'totalAmount' is float; contract declares decimal
- [drift-medium] src/Models/Order.cs:24 — property 'internalTag' not declared in contract dc-001
- [drift-medium] src/Models/Order.cs:15 — contract declares maxLength: 500 for 'notes'; no [MaxLength] attribute
```

### User-Facing Drift Report Format

When HIGH findings are present, pause before the final completion report:

```
⚠️ Data Contract Drift Detected

HIGH-SEVERITY FINDINGS (require your decision before completion):

  [1] Type mismatch — Order.totalAmount
      Contract (dc-001): Physical Type = decimal
      Implementation (src/Models/Order.cs:8): float

  [2] Nullable mismatch — Order.customerId
      Contract (dc-001): Required = Yes
      Implementation (src/Models/Order.cs:11): Guid? (nullable)

MEDIUM-SEVERITY FINDINGS (warnings):

  [3] Undocumented field — Order.internalTag
  [4] Missing constraint — Order.notes (maxLength: 500)

How would you like to proceed?
  A. Fix high-severity findings now
  B. Accept drift and complete
  C. Update the contract to match the code (initiate pch-planner session)
```

### Orchestrator Contract Embedding Pattern

The orchestrator reads contracts from the plan's `### Data Contracts` table and embeds them in each subagent prompt — same pattern as vision/plan embedding:

```markdown
## DATA CONTRACT CONTEXT

{for each contract referenced in plan whose entities are touched by this phase}
**Contract:** {contract_file_path}

{contract_document_content}
{end for each}
```

Only embed contracts whose entities are touched by the current phase to manage token budget.

### Edge Case Handling

| Scenario | Handling |
|----------|---------|
| `source_file: TBD` | Skip drift check; note in output |
| File not yet created in this phase | Skip; post-sweep catches it |
| Additional fields not in contract | `[drift-medium]` unless matching audit field pattern (`CreatedAt`, `UpdatedAt`, `IsDeleted`, `RowVersion`, etc.) |
| Entity split across partial class files | Aggregate all `{ClassName}.*.cs` properties in same directory |
| Fields defined in base class | Read one level up in inheritance chain; flag deep hierarchies for manual review |
| No contract for implemented entity | Informational note in `CONTRACT_DRIFT_SUMMARY`; not a finding |
| pch-coder asked to create a contract | Refuse; report gap and recommend pch-planner session |

**Key Discoveries:**
- The existing `[{category}] {file_path}:{line}` output format can directly accommodate `[drift-high]` and `[drift-medium]` labels — no format redesign needed
- Per-phase drift check and post-sweep are complementary: per-phase catches drift while the file is still in context; sweep catches anything missed or introduced by later phases
- pch-coder NEVER updates contract files — firm design boundary confirmed in Phase 3
- Case-insensitive name matching (camelCase↔PascalCase) is critical — without it every field comparison produces false positives
- Common audit fields (`CreatedAt`, `UpdatedAt`, `IsDeleted`, `RowVersion`) should be exempt from "undocumented field" drift category to prevent noise

| File | Relevance |
|------|-----------|
| `c:\repos\.github-private\agents\pch-coder.md` | Existing 3-category post-implementation code review; DEVIATIONS field; orchestrator clean/findings handling |

**Gaps:** C# type mapping only; non-C# language support out of scope. EF FluentAPI and FluentValidation detection may need manual fallback.
**Assumptions:** C# primary language; per-phase subagents do not block on drift; orchestrator reads and embeds contract files.

## Phase 5: End-to-End Lifecycle Design

**Status:** ✅ Complete
**Session:** 2026-02-17

### Lifecycle Narrative: From Contract Birth to Drift Enforcement

A data contract in the PCH system begins as a by-product of the visionary's architecture conversation and ends as an enforced constraint inside pch-coder's per-phase implementation loop. The lifecycle is strictly sequential and mirrors the existing agent handoff chain: no agent creates work that the previous agent did not enable, and no agent modifies artifacts owned by a downstream agent.

The narrative below traces a single feature — one that introduces a named data entity — from the moment the user begins a vision conversation to the moment pch-coder reports a clean drift check.

#### Stage 1: Contract Birth (pch-visionary, Stage 5)

The contract is born during the visionary's Stage 5 Architecture Q&A. By this point the user has answered four stages of questions covering problem space, stakeholders, goals, and functional requirements. The entity's name, business purpose, and core fields are already implicit in those answers; Stage 5 (technology decisions, integration points) is when the agent has enough to draft a complete schema.

After all Stage 5 questions are answered, pch-visionary:

1. Lists `/docs/data-contracts/` to find the next available zero-padded number (the same `max + 1` pattern used in `/docs/vision/`, `/docs/research/`, and `/docs/plans/`).
2. Creates `/docs/data-contracts/dc-NNN-{domain-name}.md` using the Phase 2 PCH data contract template.
3. Sets the contract `status` to `draft` in the front matter.
4. Populates all entity sections it can from the conversation — entity name, field names, types, constraints, relationships. Sets `source_file: TBD` because implementation file paths are not yet known.
5. Asks the user a Type C validation question to confirm entity names and field shapes before finalizing.
6. Adds the new contract to the vision document's `## Data Contracts Created` table.
7. Logs the contract creation in the vision document's `## Decisions Log`.

At this stage the contract is informational only. The `draft` status signals to all downstream agents: "schema is known but not authoritative — do not implement against this yet."

#### Stage 2: Contract as Research Context (pch-researcher, Orchestration Loop Step 1)

When pch-researcher is invoked for a phase that involves domain entities, the orchestrator reads the vision document's `## Data Contracts Created` table during its Orchestration Loop Step 1 (the mandatory pre-phase document load). For each referenced contract whose domain is relevant to the current research phase, the orchestrator:

1. Reads the full contract document from `/docs/data-contracts/`.
2. Adds a `## DATA CONTRACT CONTEXT` block to the subagent invocation prompt immediately after the existing `## VISION DOCUMENT CONTEXT` block.
3. Instructs the subagent to use the declared field names and types when describing implementation patterns and to flag any existing code that already diverges from the declared entity structure.

The researcher does not modify the contract. If a research finding reveals a gap or error in the contract, the subagent notes it as a `**Contract Gap:**` in its findings. The orchestrator surfaces that gap to the user who may direct pch-planner to correct it.

This stage ensures research findings are consistent with the declared data model before any planning begins, preventing the planner from receiving contradictory guidance from the researcher.

#### Stage 3: Contract Activation (pch-planner, Step 0 and Step 4.5)

pch-planner has two contract responsibilities in sequence:

**Step 0 (Context Gathering) — Step 4.5: Contract Discovery**

Before beginning the plan document, the planner lists `/docs/data-contracts/` and checks the vision document's `## Data Contracts Created` table to identify relevant contracts. It reads each contract, noting its `status` (`draft` or `active`) and whether `source_file` is `TBD`.

**Technical Design Section — `### Data Contracts` Table**

When writing the `## Technical Design` section, the planner populates a `### Data Contracts` table (exact format specified below). Each row links an entity to its contract file, planned implementation file path, and confirms the contract status at plan time.

**Step 4.5: Promote and Finalize**

After the holistic review of the plan (Step 4) and once implementation file paths are finalized, the planner:

1. Updates each referenced contract's `source_file` field from `TBD` to the actual planned implementation path.
2. Promotes the contract's `status` from `draft` to `active` in the contract document's front matter and in the entity's `**Status:**` line.
3. Updates the contract's `## Handoff` table to record `pch-planner` as the last updating agent.
4. If the plan introduces entities not yet in any contract, the planner creates a new contract file or adds an entity section to an existing domain contract.

The contract is now authoritative. The `active` status is the signal to pch-plan-reviewer that this contract can be relied upon for implementation.

#### Stage 4: Contract Validation (pch-plan-reviewer, Review Checklist)

pch-plan-reviewer validates the plan's contract references as part of its existing review process. The new contract-specific checks are added to `## Review Checklist → ### Correctness` and `### Completeness`. Contract defects are mapped to the existing severity scale (Minor, Major, Critical).

The reviewer does not modify contract files. If a contract is still `draft` or has `source_file: TBD`, the reviewer raises a Major issue and the plan cannot be handed off to pch-coder until it is resolved.

#### Stage 5: Contract Enforcement (pch-coder, Per-Phase + Post-Sweep)

pch-coder enforces contracts at two complementary points:

**Per-Phase (Subagent DEVIATIONS field):** Each subagent that creates or modifies an entity's `source_file` performs a drift check using the contract embedded in its prompt under `## DATA CONTRACT CONTEXT`. The five drift categories are checked (missing field, undocumented field, type mismatch, nullable mismatch, missing constraint). Results are reported in the `DEVIATIONS` field using `[drift-high]` or `[drift-medium]` prefixes. Subagents never block on drift — they always complete to `success` and report.

**Post-Sweep (Code Review Subagent, 4th category):** After all phases complete, the post-implementation code review subagent performs a final comprehensive drift check across every entity in the plan's `### Data Contracts` table. HIGH-severity findings pause the workflow before the final completion report and require user decision (Fix / Accept / Update contract).

---

### Workflow Diagram

```
USER
  │
  ▼
pch-visionary ─────────────────────────────────────────────────────────────────
  Stage 5: Architecture Q&A complete
  │
  ├── CREATE /docs/data-contracts/dc-NNN-{domain}.md  [status: draft, source_file: TBD]
  │
  └── UPDATE vision doc → ## Data Contracts Created table
  │
  ▼
pch-researcher ─────────────────────────────────────────────────────────────────
  Orchestration Loop Step 1
  │
  ├── READ vision doc → ## Data Contracts Created (find contract paths)
  ├── READ /docs/data-contracts/dc-NNN-{domain}.md
  │
  └── EMBED → ## DATA CONTRACT CONTEXT in subagent prompt
      │
      └── Subagent: use declared field names; flag any existing code divergence
  │
  ▼
pch-planner ─────────────────────────────────────────────────────────────────
  Step 0 (Context Gathering) Step 4.5: Contract Discovery
  │
  ├── LIST /docs/data-contracts/
  ├── READ relevant contracts
  │
  Technical Design: populate ### Data Contracts table
  │
  Step 4.5: Promote and Finalize
  ├── UPDATE contract: source_file = actual path
  ├── UPDATE contract: status draft → active
  └── UPDATE contract: ## Handoff → pch-planner
  │
  ▼
pch-plan-reviewer ─────────────────────────────────────────────────────────────
  Review Checklist (Correctness + Completeness)
  │
  ├── CHECK: ### Data Contracts table present (if entities exist)         [Major if absent]
  ├── CHECK: all referenced contracts have status = active               [Major if draft]
  ├── CHECK: all source_file fields populated (not TBD)                  [Major if TBD]
  ├── CHECK: every entity in Technical Design has contract entry          [Minor if missing]
  └── CHECK: contract files exist at stated paths                        [Critical if not found]
  │
  ▼
pch-coder ──────────────────────────────────────────────────────────────────────
  Orchestrator reads plan → ## Technical Design → ### Data Contracts
  │
  └── For each phase:
      Orchestrator EMBEDS contract → ## DATA CONTRACT CONTEXT in subagent prompt
      Subagent: after implementing entity, compare code vs. contract
      │
      ├── [drift-high]   Field in contract, missing from code
      ├── [drift-high]   Type mismatch
      ├── [drift-high]   Nullable mismatch
      ├── [drift-medium] Field in code, missing from contract
      └── [drift-medium] Constraint declared, not enforced
      Report in DEVIATIONS field (never block per-phase)
  │
  └── After all phases: Post-Implementation Code Review (4th category)
      Final drift sweep across all entities in ### Data Contracts table
      │
      HIGH findings present?
        YES → pause → user: Fix / Accept / Update contract
        NO  → complete with contract-clean report
```

---

### Resolved: Contract Naming Convention

**Decision: `dc-NNN-{domain-name}.md`**

The naming convention for data contract files in `/docs/data-contracts/` is:

```
dc-NNN-{domain-name}.md
```

where:
- `dc-` is a fixed prefix that distinguishes contract files from other artifact types (vision uses no prefix, research uses no prefix, plans use no prefix — the `dc-` prefix makes contracts immediately identifiable)
- `NNN` is a zero-padded three-digit sequential number (e.g., `001`, `002`, `023`) using the same `max + 1` discovery pattern as all other PCH artifact directories
- `{domain-name}` is a lowercase hyphenated domain or feature name (e.g., `order-domain`, `user-profile`, `notification-events`)

**Examples:**
```
/docs/data-contracts/dc-001-order-domain.md
/docs/data-contracts/dc-002-user-profile.md
/docs/data-contracts/dc-003-notification-events.md
```

**Why `dc-` prefix and not bare `NNN-`:** The `/docs/data-contracts/` directory has its own number series. If the prefix were omitted, `001` in data-contracts could be confused with `001` in vision or research. The `dc-` prefix prevents cross-directory numbering collisions and makes file identity unambiguous in cross-references within plan documents, vision documents, and code review output.

**Why not `data-contract-NNN-` or longer prefix:** Brevity. The `dc-` abbreviation follows the industry convention used in ODCS (`id: "dc-001"`) and keeps filenames short enough for display in plan tables without wrapping.

**Front matter `id` field:** The front matter `id` matches the filename prefix: `id: "dc-001"`. This matches the pattern used by all other PCH artifact types (vision `id: "001"`, research `id: "019"`, plan `id: "020"`).

---

### Resolved: `### Data Contracts` Table Format for Plan Documents

The `### Data Contracts` table lives inside the `## Technical Design` section of a plan document. It is the machine-readable link between the plan and the contract files, and it is the primary input pch-coder uses to know which contracts to load and which files to check for drift.

**Exact Markdown format:**

```markdown
### Data Contracts

| Contract File | Entity | Status | Source File | Contract Path |
|---------------|--------|--------|-------------|---------------|
| `dc-001-order-domain.md` | `Order` | active | `src/Models/Order.cs` | `/docs/data-contracts/dc-001-order-domain.md` |
| `dc-001-order-domain.md` | `OrderLine` | active | `src/Models/OrderLine.cs` | `/docs/data-contracts/dc-001-order-domain.md` |
| `dc-002-user-profile.md` | `UserProfile` | active | `src/Models/UserProfile.cs` | `/docs/data-contracts/dc-002-user-profile.md` |
```

**Column definitions:**

| Column | Purpose | Source | Rules |
|--------|---------|--------|-------|
| `Contract File` | Filename of the contract document | `/docs/data-contracts/` listing | Code-formatted; filename only, not full path |
| `Entity` | Physical entity name as declared in the contract's `### Entity: Name` header | Contract document | Code-formatted; must match contract exactly (case-sensitive) |
| `Status` | Contract lifecycle status at plan-writing time | Contract front matter `status` field | One of: `draft`, `active`, `deprecated` — must be `active` to pass reviewer check |
| `Source File` | Repository-relative path to the implementation file | Determined by planner from codebase analysis | Code-formatted; must not be `TBD` to pass reviewer check |
| `Contract Path` | Full repository-relative path to the contract file | `/docs/data-contracts/` | Absolute-from-repo-root path; used by pch-coder orchestrator to load the contract |

**Usage rules:**
- One row per entity per contract (not one row per contract). If `dc-001-order-domain.md` covers `Order` and `OrderLine`, they each get their own row with the same `Contract File` and `Contract Path` values.
- If no data entities are involved, write: `No data entities in scope — data contracts not applicable.` instead of an empty table.
- The table is populated during the Technical Design Q&A session and finalized (with `source_file` filled) during Step 4.5.

---

### Resolved: `## Data Contracts Created` Section Format for Vision Documents

This section is added to the `## Vision Document Template` in `pch-visionary.md` and is required in vision documents that identified data entities during Stage 5.

**Exact Markdown format:**

```markdown
## Data Contracts Created

| Contract File | Entities Covered | Status | Vision Phase | Notes |
|---------------|-----------------|--------|--------------|-------|
| `dc-001-order-domain.md` | Order, OrderLine | draft | Phase 1 | Core order aggregate |
| `dc-002-user-profile.md` | UserProfile | draft | Phase 2 | User account entity |
```

**Column definitions:**

| Column | Purpose | Rules |
|--------|---------|-------|
| `Contract File` | Filename of the contract created (links to full path in `/docs/data-contracts/`) | Code-formatted; must be created before this table is written |
| `Entities Covered` | Comma-separated list of entity names in this contract | Matches `### Entity: Name` headers in the contract document |
| `Status` | Current contract lifecycle status | Always `draft` at vision stage |
| `Vision Phase` | Which implementation phase these entities belong to (from the phasing strategy) | e.g., "Phase 1", "Phase 2", "All phases" |
| `Notes` | Brief context about why this entity was grouped here | Optional; free text |

**When the section is absent:** If no data entities were identified (documentation-only, configuration-only features), the vision document includes:

```markdown
## Data Contracts Created

No persistent data entities identified — data contracts not required for this vision.
```

This ensures the absence of contracts is an explicit decision, not an omission. pch-researcher checks this table, so the explicit "not required" note prevents the researcher from searching for contracts that do not exist.

---

### Resolved: Exact Text of New pch-plan-reviewer Checklist Items

The following items are added to `pch-plan-reviewer.md` in the locations specified in Phase 3. These are the exact strings the reviewer should check:

**Added to `## Review Checklist` → `### Correctness`:**

```markdown
- [ ] All data entities mentioned in `## Technical Design` have a corresponding row in the `### Data Contracts` table
- [ ] All referenced contracts have `status: active` in `/docs/data-contracts/` (not `draft` or `deprecated`)
- [ ] All `source_file` fields in referenced contracts are populated with a real path (not `TBD`)
```

**Added to `## Review Checklist` → `### Completeness`:**

```markdown
- [ ] `### Data Contracts` table is present in `## Technical Design` if the plan introduces or modifies any persistent data entities
- [ ] All `Contract Path` values in the `### Data Contracts` table point to files that actually exist in `/docs/data-contracts/`
```

**Added to `## Common Plan Defects to Watch For` as a new subsection:**

```markdown
### Data Contract Issues

- **Missing table:** `### Data Contracts` table absent when plan involves database schema, DTOs, or named data models — pch-coder cannot perform drift detection without it (Severity: **Major**)
- **Draft contract referenced:** Contract is listed with `status: draft` — schema may change before implementation; not authoritative (Severity: **Major**)
- **TBD source_file:** One or more `source_file` fields are `TBD` — pch-coder cannot locate the implementation file to check for drift (Severity: **Major**)
- **Broken contract path:** `Contract Path` in the table does not exist in `/docs/data-contracts/` — broken artifact reference (Severity: **Critical**)
- **Uncovered entity:** Entity is defined or modified in Technical Design tasks but has no entry in `### Data Contracts` — drift detection gap (Severity: **Minor**)
```

**Severity reference (consistent with existing reviewer defect scale):**

| Severity | Definition | Action |
|----------|-----------|--------|
| Critical | Broken reference; plan cannot be executed | Must be fixed before handoff |
| Major | Blocks safe implementation (schema uncertainty, missing detection) | Must be resolved before pch-coder handoff |
| Minor | Gaps that can be filled during implementation | Should be resolved; does not block handoff |

---

### Resolved: Handoff Chain for Contracts Across Agent Handoffs

The data contract document itself is not directly passed between agents as a handoff artifact. Instead, it is a persistent file in `/docs/data-contracts/` that each agent reads at the appropriate lifecycle point. The chain works as follows:

```
pch-visionary
  Creates: /docs/data-contracts/dc-NNN-{domain}.md  [draft, source_file: TBD]
  Records: vision doc → ## Data Contracts Created
  Handoff artifact: vision document (contains ## Data Contracts Created table)
        │
        ▼
pch-researcher
  Reads: vision doc → ## Data Contracts Created (finds contract paths)
  Reads: each relevant contract from /docs/data-contracts/
  Passes: contract content embedded in subagent prompt under ## DATA CONTRACT CONTEXT
  Does NOT write to: contract files
  Handoff artifact: research document (subagent findings reference contract field names)
        │
        ▼
pch-planner
  Reads: /docs/data-contracts/ listing + relevant contracts (Step 0, Step 4.5)
  Writes: contract source_file, contract status (draft → active)
  Writes: plan doc → ## Technical Design → ### Data Contracts table
  Handoff artifact: plan document (contains ### Data Contracts table with full paths)
        │
        ▼
pch-plan-reviewer
  Reads: plan doc → ### Data Contracts table
  Reads: each contract file at stated path (to verify status and source_file)
  Does NOT write to: contract files
  Writes: plan doc → ## Review Session Log (contract findings)
  Handoff artifact: plan document (reviewer-approved, with contract issues resolved)
        │
        ▼
pch-coder
  Reads: plan doc → ### Data Contracts table (orchestrator, before each phase)
  Reads: contract files (orchestrator embeds them in subagent prompts)
  Does NOT write to: contract files
  Reports: drift findings in DEVIATIONS field + code review output
  Handoff artifact: completion report (with contract drift summary)
```

**Key passing conventions:**

1. The `## Data Contracts Created` table in the vision document is the persistent cross-agent record of which contracts exist and which feature phases they belong to. It is the contract registry for the project.

2. The `### Data Contracts` table in the plan document is the operational contract reference for pch-coder. It contains the exact file paths the orchestrator needs to load contracts and the exact `source_file` paths the subagent compares against.

3. The orchestrator-embeds-context pattern (used for vision and plan documents) extends to contracts: the orchestrator reads the contract from disk and injects its full content into the subagent prompt. Subagents never read contract files directly — they receive them embedded.

4. Contract files are write-accessed by only two agents: pch-visionary (create) and pch-planner (activate + set source_file). All other agents are read-only consumers.

---

### Resolved: Multi-Contract Feature Handling

Features that span multiple domain modules (e.g., an order checkout feature touching Order, UserProfile, and InventoryItem) may involve multiple contracts across multiple contract files. The following conventions handle this:

**In the vision document `## Data Contracts Created` table:** Each contract file gets its own row. If a feature creates entities across two domains, two contract files are created (one per domain), each with their own `dc-NNN-` prefix.

**In the plan document `### Data Contracts` table:** Each entity gets its own row, regardless of which contract file it comes from. The `Contract File` and `Contract Path` columns will repeat the same contract file for entities in the same domain.

**Example — checkout feature spanning 3 domains:**

```markdown
### Data Contracts

| Contract File | Entity | Status | Source File | Contract Path |
|---------------|--------|--------|-------------|---------------|
| `dc-001-order-domain.md` | `Order` | active | `src/Models/Order.cs` | `/docs/data-contracts/dc-001-order-domain.md` |
| `dc-001-order-domain.md` | `OrderLine` | active | `src/Models/OrderLine.cs` | `/docs/data-contracts/dc-001-order-domain.md` |
| `dc-002-user-profile.md` | `UserProfile` | active | `src/Models/UserProfile.cs` | `/docs/data-contracts/dc-002-user-profile.md` |
| `dc-003-inventory.md` | `InventoryItem` | active | `src/Models/InventoryItem.cs` | `/docs/data-contracts/dc-003-inventory.md` |
```

**pch-coder orchestrator behavior with multiple contracts:** The orchestrator reads ALL contracts listed in the `### Data Contracts` table at the start of each phase. For each subagent invocation, it filters to only embed contracts whose entities are touched by that phase's tasks (to manage token budget). A phase that only touches `Order` and `OrderLine` receives only `dc-001-order-domain.md` embedded; `dc-002-user-profile.md` and `dc-003-inventory.md` are withheld.

**Entity ownership across contracts:** Each entity belongs to exactly one contract file. If a feature requires a new entity that spans two existing domains, a new contract file is created for the cross-domain entity rather than adding it to an existing single-domain contract. The `domain` field in the front matter can describe composite domains (e.g., `"checkout"` for a checkout-specific entity that references both orders and users).

**Version independence:** If a breaking change is made to `UserProfile` (version bump to `2.0.0`), the `Order` contract is not affected. Plan documents reference specific contract files; the `### Data Contracts` table reflects the contract status at plan-writing time. If a downstream plan re-uses an entity from an older contract, the planner reads the contract's current version and records any version mismatch as a planning concern.

---

### Resolved: Open Question — Should pch-coder Create a New Contract?

**Question (from Phase 3):** When pch-coder encounters a data entity with no corresponding contract during implementation, should it create a new contract file?

**Decision: No.**

**Rationale:**

1. **Role separation principle.** pch-coder's role is enforcement, not creation or governance. Creating a contract requires understanding business meaning, ownership, consumer relationships, and breaking change policy — information the coder does not have and cannot reliably infer from code.

2. **Timing.** If an entity has no contract by the time pch-coder runs, it means either (a) the entity was not known during visioning (and the vision document should be updated), or (b) the entity was intentionally excluded from the contract system. In either case, the correct response is to surface the gap, not to create an incomplete artifact.

3. **Contract quality.** A contract created by a coder from code inspection alone will have `owner: unknown`, `steward: unknown`, missing business descriptions, and no breaking change history. This is worse than no contract because it gives false confidence in schema governance.

4. **Existing mechanic.** The coder already has a mechanism for surfacing gaps: the `CONTRACT_DRIFT_SUMMARY` and `DEVIATIONS` fields. An entity without a contract should appear as an informational note: `"Entity {Name} (src/Models/{Name}.cs) has no corresponding data contract."` This surfaces the gap to the user without creating a low-quality artifact.

**Correct flow when pch-coder finds an uncontracted entity:**

```
pch-coder detects entity with no contract
  │
  └── Adds note to CONTRACT_DRIFT_SUMMARY:
      "Informational: Entity 'ShippingAddress' (src/Models/ShippingAddress.cs)
       has no data contract. Recommend creating dc-NNN-shipping-domain.md."
  │
  └── User reviews and decides:
        A. Accept as-is (entity is intentionally uncontracted)
        B. Initiate pch-visionary session to draft contract retroactively
        C. Initiate pch-planner session to add entity to existing domain contract
```

pch-coder does NOT create, draft, or partially populate a contract file under any circumstances.

---

### Implementation Readiness Assessment

Having synthesized all five phases, the following gap analysis determines whether a complete implementation plan can be written immediately.

#### What Is Fully Specified (Plan-Ready)

| Design Element | Specification Source | Ready? |
|----------------|---------------------|--------|
| Contract file template | Phase 2 template section | Yes |
| Contract naming convention | Phase 5 (this document) | Yes |
| pch-visionary: Stage 5 trigger and creation clause | Phase 3 injection clauses | Yes |
| pch-visionary: `## Data Contracts Created` table format | Phase 5 (this document) | Yes |
| pch-researcher: orchestration loop modification | Phase 3 injection clauses | Yes |
| pch-researcher: `## DATA CONTRACT CONTEXT` subagent block | Phase 3 injection clauses | Yes |
| pch-planner: Step 0 Step 4.5 contract discovery | Phase 3 injection clauses | Yes |
| pch-planner: `### Data Contracts` table format | Phase 5 (this document) | Yes |
| pch-planner: Step 4.5 promotion workflow | Phase 3 injection clauses | Yes |
| pch-plan-reviewer: exact checklist item text | Phase 5 (this document) | Yes |
| pch-plan-reviewer: Common Defects subsection | Phase 5 (this document) | Yes |
| pch-coder: per-phase drift detection block | Phase 4 ready-for-injection block | Yes |
| pch-coder: post-sweep 4th review category | Phase 4 ready-for-injection block | Yes |
| pch-coder: output format extension | Phase 4 (extended format section) | Yes |
| pch-coder: user-facing drift report format | Phase 4 (user-facing format section) | Yes |
| pch-coder: orchestrator embedding pattern | Phase 4 (embedding pattern section) | Yes |
| Drift severity mapping (5 categories, 2 levels) | Phase 4 severity table | Yes |
| Type mapping (C# physical types) | Phase 4 type mapping table | Yes |
| Nullable mapping | Phase 4 nullable mapping table | Yes |
| Constraint mapping | Phase 4 constraint mapping table | Yes |
| Edge case handling | Phase 4 edge case table | Yes |
| Multi-contract feature handling | Phase 5 (this document) | Yes |
| Handoff chain conventions | Phase 5 (this document) | Yes |
| pch-coder contract creation = No | Phase 5 (this document) | Yes |

#### Gaps Remaining Before Implementation

| Gap | Impact | Resolution Path |
|-----|--------|----------------|
| `/docs/data-contracts/` directory does not exist | First agent to create a contract must `mkdir` it; no agent prompt currently includes this instruction | Add one-line note to pch-visionary Stage 5 clause: "If `/docs/data-contracts/` does not exist, create it before creating the contract file." |
| No `research_source` or `plan_source` metadata fields in contract front matter | Contracts do not have a machine-readable back-reference to the vision or plan that created them | Add optional `vision_source` and `plan_source` fields to the contract front matter template (same pattern as research documents use `vision_source`) |
| Non-C# language drift detection | Phase 4 type mapping covers C# only; other languages have different type systems and annotation patterns | Out of scope for the current PCH system (C# is the primary language); can be added later |
| Draft clause text is not yet integrated into agent `.md` files | The injection clauses from Phases 3 and 4 are described in this research document but are not yet in the agent prompt files | pch-planner creates an implementation plan; pch-coder implements the agent file edits |
| pch-visionary Vision Completion Checklist: exact item text | Phase 3 describes adding a checklist item but does not give the exact string | Exact text: `- [ ] \`## Data Contracts Created\` section present (or explicitly notes "No data contracts required")` |
| Contract version bump workflow during coder drift resolution | When user chooses "Accept drift and update contract" (Option C), who updates the contract version and what does that workflow look like? | pch-planner should handle this; trigger: user selects Option C from the drift report, which should spawn a pch-planner sub-session targeted at the affected contract |

#### Summary Assessment

The design is implementation-ready for all five agents. A plan document can be written immediately to implement the agent file changes. The two substantive gaps (directory creation instruction and back-reference metadata fields) are minor additions that can be included in the plan's implementation tasks. The non-C# language gap is explicitly out of scope. The Option C (update contract) workflow gap is a known open item for a follow-on plan after this one is implemented.

**Estimated implementation scope:** 5 agent files modified (no new files created, no architecture changes). Changes are additive — new sections inserted into existing section structures. No existing behavior is removed or replaced.

---

### Key Discoveries from Phase 5

- The `dc-NNN-{domain-name}.md` naming convention with `dc-` prefix is unambiguous and consistent with the `id: "dc-001"` field pattern from ODCS and the Phase 2 template
- The `### Data Contracts` table in plan documents is the operational link between contracts and coder — it must have exactly 5 columns: Contract File, Entity, Status, Source File, Contract Path
- The `## Data Contracts Created` table in vision documents is the contract registry — it is the source that downstream agents (researcher, planner) use to discover contracts for a given feature
- pch-coder must never create contracts; the correct response to an uncontracted entity is a `CONTRACT_DRIFT_SUMMARY` informational note
- The orchestrator-embeds-context pattern (inherited from vision and plan embedding) cleanly extends to contract embedding with no new mechanism required
- Multi-contract features are handled by one row per entity in the plan's `### Data Contracts` table; the orchestrator filters which contracts to embed per phase
- The design is complete and implementation-ready; the only substantive gap is a one-line `mkdir` instruction in pch-visionary's Stage 5 clause
- The `/docs/data-contracts/` directory does not yet exist — it must be created by the first pch-visionary agent that identifies data entities for a feature

| File | Relevance |
|------|-----------|
| `c:\repos\.github-private\agents\pch-visionary.md` | Injection target: Stage 5 clause, Vision Completion Checklist, Vision Document Template |
| `c:\repos\.github-private\agents\pch-researcher.md` | Injection target: Orchestration Loop step 1, Subagent Invocation template |
| `c:\repos\.github-private\agents\pch-planner.md` | Injection target: Context Gathering Step 4.5, Plan Structure Technical Design, Workflow Process Step 4.5 |
| `c:\repos\.github-private\agents\pch-plan-reviewer.md` | Injection target: Review Checklist Correctness, Review Checklist Completeness, Common Plan Defects |
| `c:\repos\.github-private\agents\pch-coder.md` | Injection target: Subagent Invocation YOUR INSTRUCTIONS, Post-Implementation Code Review 4th category |
| `c:\repos\.github-private\docs\research\019-data-contract-management-across-pch-agents.md` | This research document; Phases 1–4 findings synthesized here |

**Gaps:** See Implementation Readiness Assessment above.
**Assumptions:** C# primary language. Sequential single-session workflow (no concurrent multi-agent writes possible). pch-planner always runs after pch-visionary — the contract is always `draft` when the planner first sees it.

## Overview

The PCH agent workflow can systematically manage data contracts across its full lifecycle using patterns already established in the system — no new mechanisms are needed. Contracts follow the same YAML front matter + Markdown body structure as vision, research, and plan documents. They are created by pch-visionary (draft) at Stage 5 Architecture Q&A, activated by pch-planner (with `source_file` filled), validated by pch-plan-reviewer (blocks handoff if draft status or TBD source_file), and enforced by pch-coder (per-phase DEVIATIONS field + post-implementation drift sweep). pch-researcher receives contracts as passive context embedded in subagent prompts.

The contract-passing mechanism mirrors the existing orchestrator-embeds-context pattern: contracts are read by the orchestrator and embedded verbatim in subagent prompts alongside the plan document. No new tooling, no new session infrastructure, and no new document formats beyond the `/docs/data-contracts/` directory and the PCH data contract Markdown template designed in Phase 2.

### Key Findings Summary

1. **All five PCH artifacts already share an identical structural pattern** (YAML front matter + Markdown body + `## Handoff` + `## Standards Applied`) — data contracts fit as a sixth artifact type with zero new conventions required
2. **ODCS v3.1.0 is the industry standard** but its YAML serialization is unsuitable for AI-agent-driven creation; the PCH Markdown table format is significantly more reliable for LLM generation and consumption
3. **`source_file` is the critical linking field** — it connects the contract's declared schema to the actual implementation file, enabling pch-coder to perform targeted drift detection without filesystem searching
4. **Each agent has a non-overlapping, single-responsibility role**: visionary=create, researcher=read, planner=reference+activate, reviewer=validate, coder=enforce
5. **Drift detection requires no new output mechanisms** — the existing `DEVIATIONS` field (with `[drift-high]`/`[drift-medium]` prefixes) and the existing 3-category post-implementation code review (with a 4th category added) handle everything
6. **The design is fully implementation-ready** — all injection clause text is written and ready to insert into the five agent `.md` files; estimated scope is 5 files modified, no new files

### Cross-Cutting Patterns

- **Orchestrator-embeds-context:** All context transfer for automated workflows uses full document embedding. Contracts follow the same pattern as vision docs (in researcher) and plan docs (in coder).
- **Document-as-truth:** Contract status changes (`draft` → `active`) must be persisted to the contract file by pch-planner before handoff — never inferred from conversation history.
- **Fixed lifecycle stages for cross-cutting awareness:** Standards queries (pch-standards-space) already fire at fixed lifecycle stages. Data contract creation fires at the same fixed point — visionary Stage 5 — regardless of whether the user has asked for it.
- **Proactive creation over reactive creation:** Contracts are created proactively when entities are identified, not when a user explicitly requests a contract. This matches the proactive standards query pattern.

### Actionable Conclusions

- Add a `## Data Contracts Created` section to pch-visionary's document template and trigger contract creation after Stage 5 Architecture Q&A
- Add Step 4.5 to pch-planner's Context Gathering and Workflow Process to read existing contracts and promote `draft` → `active` with `source_file`
- Add three Correctness and two Completeness checklist items to pch-plan-reviewer
- Add the "Data Contract Drift Detection" instruction block to pch-coder's per-phase subagent prompt
- Add a 4th review category to pch-coder's Post-Implementation Code Review subagent
- Extend pch-researcher's Orchestration Loop Step 1 to embed contracts (alongside vision docs) in subagent prompts
- Create the `/docs/data-contracts/` directory as part of pch-visionary's contract creation flow

### Open Questions

- **Option C workflow:** When pch-coder surfaces drift and the user chooses "Update contract to match code," what exact steps should the subsequent pch-planner session perform? (deferred to implementation plan)
- **Non-C# language support:** The type mapping table is C#-specific. Python, TypeScript, and Java projects require language-specific mappings. (explicitly out of scope for this research)
- **`vision_source` / `plan_source` back-references in contract front matter:** Should contracts carry optional back-references to the vision and plan that created/activated them? (recommended: yes, add as optional fields — makes the audit trail machine-readable)

## Standards Applied

| Standard | Relevance | Guidance |
|----------|-----------|----------|
| Open Data Contract Standard (ODCS) v3.1.0 | Contract template structure and field definitions | Used as reference for naming conventions (dc-NNN id format), lifecycle states (draft/active/deprecated), and breaking change classification |
| PCH Document-as-Truth principle | All contract state persisted to files; no in-memory or session-only state | Contract status changes (draft→active) must be written to the contract file by pch-planner before handoff |
| PCH Orchestrator-Embeds-Context pattern | Contract loading mechanism for researcher and coder subagents | Contracts are embedded in subagent prompts by the orchestrator; subagents never read contract files directly |

## Handoff

| Field | Value |
|-------|-------|
| Created By | pch-researcher |
| Created Date | 2026-02-17 |
| Status | ✅ Complete |
| Current Phase | 5 of 5 |
| Next Agent | pch-planner |
| Path | /docs/research/019-data-contract-management-across-pch-agents.md |
